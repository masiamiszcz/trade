using System.Xml.Linq;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Data.Providers;

/// <summary>
/// Fetches exchange rates from European Central Bank (ECB)
/// API: https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml
/// </summary>
public class EcbRateProvider : IEcbRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICurrencyRepository _currencyRepository;
    private const string EcbApiUrl = "https://www.ecb.europa.eu/stats/eurofxref/eurofxref-daily.xml";
    private static readonly Guid EcbSourceId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public EcbRateProvider(HttpClient httpClient, ICurrencyRepository currencyRepository)
    {
        _httpClient = httpClient;
        _currencyRepository = currencyRepository;
    }

    public async Task<List<ExchangeRateDto>> FetchAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(EcbApiUrl);
            if (!response.IsSuccessStatusCode)
                return new List<ExchangeRateDto>();

            var xmlContent = await response.Content.ReadAsStringAsync();
            var doc = XDocument.Parse(xmlContent);

            // ECB XML namespace
            var ns = XNamespace.Get("http://www.ecb.int/vocabulary/2002-08-01/eurofxref");
            
            var rateElements = doc.Descendants(ns + "Cube")
                .Where(c => c.Attribute("currency") != null)
                .ToList();

            var rates = new List<ExchangeRateDto>();
            var eurCurrency = await _currencyRepository.GetByCodeAsync("EUR");
            
            if (eurCurrency == null)
                return rates;

            var timeElement = doc.Descendants(ns + "Cube")
                .FirstOrDefault(c => c.Attribute("time") != null);
            
            if (timeElement == null)
                return rates;

            var dateStr = timeElement.Attribute("time")?.Value ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            foreach (var rateElement in rateElements)
            {
                var code = rateElement.Attribute("currency")?.Value;
                var rateValue = decimal.Parse(rateElement.Attribute("rate")?.Value ?? "0");

                if (code != null && rateValue > 0)
                {
                    var currency = await _currencyRepository.GetByCodeAsync(code);
                    if (currency != null)
                    {
                        rates.Add(new ExchangeRateDto
                        {
                            Id = Guid.NewGuid(),
                            BaseCurrencyId = eurCurrency.Id,
                            QuoteCurrencyId = currency.Id,
                            Rate = rateValue,
                            EffectiveAt = DateTime.Parse(dateStr),
                            SourceId = EcbSourceId
                        });
                    }
                }
            }

            return rates;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ECB Rate Fetch Error: {ex.Message}");
            return new List<ExchangeRateDto>();
        }
    }
}
