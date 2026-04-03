using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using TradingPlatform.Api.Middleware;
using TradingPlatform.Core.Extensions;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Mapping;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Services;
using TradingPlatform.Data.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddValidators();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IUserService, UserService>();
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

    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
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

builder.Services.AddDataServices(builder.Configuration);

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
