using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Repositories;
using TradingPlatform.Data.Services;

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
        services.AddScoped<IInstrumentService, InstrumentService>();
        
        // Register admin auth services and repositories
        services.AddScoped<IAdminAuthRepository, AdminAuthRepository>();
        services.AddScoped<IAdminInvitationRepository, AdminInvitationRepository>();
        services.AddScoped<IAdminRegistrationLogRepository, AdminRegistrationLogRepository>();
        services.AddScoped<IAdminInvitationService, AdminInvitationService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();
        services.AddScoped<IAdminService, AdminService>();

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