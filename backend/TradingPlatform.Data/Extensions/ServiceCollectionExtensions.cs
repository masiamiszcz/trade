using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.External;
using TradingPlatform.Data.Providers;
using TradingPlatform.Data.Repositories;
using TradingPlatform.Data.Services;
using TradingPlatform.Data.Services.Market;
using TradingPlatform.Data.UnitOfWork;
using UnitOfWorkImpl = TradingPlatform.Data.UnitOfWork.UnitOfWork;

namespace TradingPlatform.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<TradingPlatformDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            // Suppress pending model changes warning (we handle defaults in code)
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IMarketDataRepository, SqlMarketDataRepository>();
        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddSingleton<ICandleRepository, SqlCandleRepository>();
        services.AddSingleton<ICandleBulkInserter, SqlServerCandleBulkInserter>();
        services.AddScoped<IAssetMappingRepository, SqlAssetMappingRepository>();
        services.AddScoped<IUserRepository, SqlUserRepository>();
        services.AddScoped<IAccountRepository, SqlAccountRepository>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserAuthService, UserAuthService>();
        services.AddScoped<IHealthService, HealthService>();

        // Configure settings
        services.Configure<TwoFactorSettings>(c => 
        {
            var section = configuration.GetSection("TwoFactor");
            c.Issuer = section["Issuer"] ?? "TradingPlatform";
            c.QrCodeSize = int.Parse(section["QrCodeSize"] ?? "10");
        });
        
        services.Configure<EncryptionSettings>(c =>
        {
            var section = configuration.GetSection("Encryption");
            c.MasterKey = section["MasterKey"] ?? "";
        });

        // Register two-factor and encryption services (used by admin auth)
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        
        // Register admin repositories for AdminService
        services.AddScoped<IAdminRequestRepository, SqlAdminRequestRepository>();
        services.AddScoped<IAuditLogRepository, SqlAuditLogRepository>();
        services.AddScoped<IAdminAuditLogRepository, SqlAdminAuditLogRepository>();
        services.AddScoped<IInstrumentRepository, SqlInstrumentRepository>();
        
        // Register exchange rate system - MINIMAL
        services.AddScoped<Data.Repositories.IExchangeRateRepository, SqlExchangeRateRepository>();
        services.AddScoped<IExchangeRateService, Data.Services.ExchangeRateService>();
        services.AddScoped<IExchangeService, ExchangeService>();
        
        // Single HTTP client for all external APIs
        services.AddHttpClient<IExternalApiClient, ExternalApiClient>();

        // Register Binance API client for startup polling
        services.AddHttpClient<IBinanceApiClient, BinanceApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.binance.com/");
        });

        // Coordinator for startup data loading (CSV + REST)
        services.AddSingleton<IStartupLoadCoordinator, StartupLoadCoordinator>();

        // Background service for hourly rate fetching
        services.AddHostedService<RateFetcherHostedService>();
        services.AddHostedService<BinanceStartupPollingService>();

        // Register admin auth repositories FIRST (needed by ApprovalService)
        services.AddScoped<IAdminAuthRepository, AdminAuthRepository>();
        services.AddScoped<IAdminInvitationRepository, AdminInvitationRepository>();
        services.AddScoped<IAdminRegistrationLogRepository, AdminRegistrationLogRepository>();
        
        // Register approval workflow service (uses IAdminAuthRepository for self-approval check)
        services.AddScoped<IApprovalService, ApprovalService>();
        
        // Register approval handlers for different entity types
        services.AddScoped<IUserApprovalHandler, UserApprovalHandler>();
        
        // Register instrument service (uses IApprovalService for requests)
        services.AddScoped<IInstrumentService, InstrumentService>();
        
        // Register remaining admin services
        services.AddScoped<IAdminInvitationService, AdminInvitationService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminService, AdminService>();

        // Binance WebSocket Market Data
        services.AddSingleton(Channel.CreateUnbounded<Trade>());
        services.AddSingleton<MarketProcessingService>();  // Singleton - injected into BinanceWebSocketService
        services.AddSingleton<IActiveCandleSubscriptionRegistry, ActiveCandleSubscriptionRegistry>();
        services.AddSingleton<ICandleStreamActivationService>(provider => provider.GetRequiredService<MarketProcessingService>());
        
        // ✅ FIX: Register BinanceWebSocketService as Singleton for BOTH HostedService and PortfolioTransactionService
        services.AddSingleton<BinanceWebSocketService>();
        services.AddSingleton<IBinanceWebSocketService>(provider => provider.GetRequiredService<BinanceWebSocketService>());
        services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<BinanceWebSocketService>());
        
        services.AddHostedService<BinanceStartupPollingService>();  // Must be before WebSocket
        services.AddHostedService<MarketProcessingService>();

        // Crypto Portfolio Services (ETAP 4-5)
        services.AddScoped<IPositionRepository, SqlPositionRepository>();
        services.AddScoped<ICryptoTransactionRepository, SqlCryptoTransactionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWorkImpl>();
        services.AddScoped<IPortfolioTransactionService, PortfolioTransactionService>();

        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        const int maxRetries = 5;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            using var scope = services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DatabaseMigration");
            var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();

            try
            {
                logger.LogInformation("Applying database migrations (attempt {Attempt}/{MaxRetries})...", attempt, maxRetries);
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Database is ready.");
                return;
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "Database is not ready yet. Retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}