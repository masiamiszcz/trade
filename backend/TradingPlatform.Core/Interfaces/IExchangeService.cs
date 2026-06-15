namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Service for converting amounts between different currencies with margin support
/// </summary>
public interface IExchangeService
{
    /// <summary>
    /// Converts amount from one currency to another
    /// </summary>
    /// <param name="fromCurrency">Source currency code (e.g., "USD", "EUR")</param>
    /// <param name="toCurrency">Target currency code (e.g., "BTC", "PLN")</param>
    /// <param name="amount">Amount to convert</param>
    /// <returns>Converted amount in target currency</returns>
    Task<decimal> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount);
}
