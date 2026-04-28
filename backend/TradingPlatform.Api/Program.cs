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
using TradingPlatform.Core.Extensions;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Mapping;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Extensions;
using TradingPlatform.Data.Mapping;

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
            .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
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
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy =>
        policy.RequireClaim("is_super_admin"));
});

builder.Services.AddDataServices(builder.Configuration);

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
