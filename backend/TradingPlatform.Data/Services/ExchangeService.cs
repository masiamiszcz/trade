using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services;

/// <summary>
/// Service for converting amounts between different currencies
/// Uses exchange rates from database with configurable margin
/// </summary>
public class ExchangeService : IExchangeService
{
    private readonly IExchangeRateRepository _exchangeRateRepository;
    
    /// <summary>
    /// Exchange margin multiplier (e.g., 1.0 = no margin, 1.05 = 5% margin)
    /// Can be modified in future for dynamic margin configuration
    /// </summary>
    private const decimal ExchangeMargin = 1m;

    public ExchangeService(IExchangeRateRepository exchangeRateRepository)
    {
        _exchangeRateRepository = exchangeRateRepository;
    }

    public async Task<decimal> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount)
    {
        if (fromCurrency == toCurrency)
        {
            return amount; // No conversion needed
        }

        // Try direct rate (fromCurrency -> toCurrency)
        var rate = await _exchangeRateRepository.GetLatestAsync(fromCurrency, toCurrency);
        
        if (rate != null)
        {
            return amount * rate.Rate * ExchangeMargin;
        }

        // Try reverse rate (toCurrency -> fromCurrency) and invert
        var reverseRate = await _exchangeRateRepository.GetLatestAsync(toCurrency, fromCurrency);
        
        if (reverseRate != null)
        {
            return amount / reverseRate.Rate * ExchangeMargin;
        }

        // No rate found - throw exception
        throw new InvalidOperationException(
            $"No exchange rate found for conversion from {fromCurrency} to {toCurrency}");
    }
}
