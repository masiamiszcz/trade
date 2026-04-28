using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Data.Seeding;

/// <summary>
/// Provides initial seed data for currencies and rate sources
/// </summary>
public static class CurrencySeeder
{
    /// <summary>
    /// Gets initial currency data
    /// </summary>
    public static List<CurrencyDto> GetInitialCurrencies()
    {
        return new List<CurrencyDto>
        {
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Code = "PLN", Name = "Polish Zloty", IsActive = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Code = "EUR", Name = "Euro", IsActive = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Code = "USD", Name = "US Dollar", IsActive = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000004"), Code = "GBP", Name = "British Pound", IsActive = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000005"), Code = "CHF", Name = "Swiss Franc", IsActive = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000006"), Code = "JPY", Name = "Japanese Yen", IsActive = true },
            new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000007"), Code = "CNY", Name = "Chinese Yuan", IsActive = true },
        };
    }

    /// <summary>
    /// Gets initial exchange rate sources
    /// </summary>
    public static List<RateSourceDto> GetInitialRateSources()
    {
        return new List<RateSourceDto>
        {
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Code = "NBP", Name = "Polish National Bank" },
            new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Code = "ECB", Name = "European Central Bank" },
        };
    }
}

public class RateSourceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}
