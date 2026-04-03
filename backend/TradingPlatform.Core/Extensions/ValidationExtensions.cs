using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TradingPlatform.Core.Models;
using TradingPlatform.Core.Validators;

namespace TradingPlatform.Core.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

        return services;
    }
}
