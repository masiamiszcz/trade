public interface ICurrencyConversionService
{
    Task<decimal> ConvertAsync(Guid from, Guid to, decimal amount);
}