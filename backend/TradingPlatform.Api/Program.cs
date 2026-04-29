using System.Text;
using AutoMapper;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TradingPlatform.Api.Middleware;
using TradingPlatform.Api.Hubs;
using TradingPlatform.Core.Extensions;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Mapping;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Extensions;
using TradingPlatform.Data.Mapping;
using TradingPlatform.Core.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddValidators();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddAutoMapper(typeof(MappingProfile), typeof(DataMappingProfile));

builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers(options =>
{
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(defaultPolicy));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost",
                "http://127.0.0.1",
                "http://localhost:3000",
                "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
        ?? throw new InvalidOperationException("JWT settings are not configured properly.");

    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();  // ✅ Allow HTTP in development, enforce HTTPS in production
    options.SaveToken = true;
    options.MapInboundClaims = false;  // ❌ Do NOT map - preserve custom claims (sub, is_super_admin) used in controllers
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // 🔐 SECURITY: Validate user exists in database
    // This is the critical security fix - JWT alone is not enough
    // Every token must be backed by an active user in the database
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            // 1. Extract userId from 'sub' claim
            var userIdClaim = context.Principal?.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                context.Fail("Invalid token - no user id");
                return;
            }

            // 2. Try parse GUID safely
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                context.Fail("Invalid token - malformed user id");
                return;
            }

            // 3. Determine if user is admin or regular user
            var roleClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            bool isAdmin = roleClaim == "Admin";

            try
            {
                User? user = null;

                if (isAdmin)
                {
                    // Fetch admin user (using scoped service)
                    var adminAuthRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IAdminAuthRepository>();
                    user = await adminAuthRepository.GetAdminByIdAsync(userId);
                }
                else
                {
                    // Fetch regular user (using scoped service)
                    var userRepository = context.HttpContext.RequestServices
                        .GetRequiredService<IUserRepository>();
                    user = await userRepository.GetByIdAsync(userId);
                }

                // 4. Validate user exists in database
                if (user == null)
                {
                    context.Fail($"User {userId} does not exist in database");
                    return;
                }

                // 5. Validate user is active
                if (user.Status != UserStatus.Active)
                {
                    context.Fail($"User {userId} is not active (status: {user.Status})");
                    return;
                }
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "🔐 Token validation error for user {UserId}: {ErrorMessage}", userIdClaim, ex.Message);
                
                context.Fail("Token validation failed");
            }
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireClaim("is_super_admin"));
});

builder.Services.AddDataServices(builder.Configuration);
builder.Services.Configure<TradingPlatform.Core.Models.BinanceSettings>(builder.Configuration.GetSection("Binance"));
builder.Services.AddSignalR();
builder.Services.AddSingleton<IPriceUpdatePublisher, TradingPlatform.Api.Services.PriceUpdatePublisher>();
builder.Services.AddScoped<ICryptoService, TradingPlatform.Data.Services.CryptoService>();

// 🔐 Enable HttpContext access for 2FA wrapper (extracts userId from JWT claims)
builder.Services.AddHttpContextAccessor();

// 🔐 Redis for 2FA session management
// Stores TOTP secrets temporarily (not in JWT)
// Manages rate limiting (failed attempts, lockout)
var redisConnection = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException(
        "Redis connection string is not configured. Add 'ConnectionStrings:Redis' to appsettings.json");

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var connection = ConnectionMultiplexer.Connect(redisConnection);
        return connection;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"Failed to connect to Redis at {redisConnection}. Make sure Redis is running.", ex);
    }
});

// Register Redis session service for 2FA
builder.Services.AddScoped<IRedisSessionService, RedisSessionService>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TradingPlatform.Data.Context.TradingPlatformDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<CryptoPricesHub>("/hubs/prices").RequireCors("Frontend");

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TradingPlatformDbContext>();
    dbContext.Database.Migrate();
}

app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

// Run database migrations in background
_ = Task.Run(async () =>
{
    try
    {
        await app.Services.ApplyDatabaseMigrationsAsync();
    }
    catch (Exception ex)
    {
        // Log error but don't crash the app
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply database migrations");
    }
});

app.Run();

public partial class Program { }
