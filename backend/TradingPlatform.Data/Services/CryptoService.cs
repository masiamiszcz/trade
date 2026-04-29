using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Data.Repositories;

namespace TradingPlatform.Data.Services;

public sealed class CryptoService : ICryptoService
{
    private readonly IInstrumentService _instrumentService;
    private readonly ICandleRepository _candleRepository;

    public CryptoService(
        IInstrumentService instrumentService,
        ICandleRepository candleRepository)
    {
        _instrumentService = instrumentService;
        _candleRepository = candleRepository;
    }

    public async Task<IEnumerable<InstrumentDto>> GetAvailableCryptoInstrumentsAsync(CancellationToken cancellationToken = default)
    {
        var activeInstruments = await _instrumentService.GetAllActiveAsync(cancellationToken);
        var supportedType = InstrumentType.Crypto.ToString();

        return activeInstruments
            .Where(i => string.Equals(i.Type, supportedType, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Symbol);
    }

    public async Task<IEnumerable<CandleDto>> GetCandlesBySymbolAsync(string symbol, int limit, CancellationToken cancellationToken = default)
    {
        var normalizedSymbol = symbol.Trim().ToUpperInvariant();
        InstrumentDto instrument;

        try
        {
            instrument = await _instrumentService.GetBySymbolAsync(normalizedSymbol, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new KeyNotFoundException($"Crypto instrument '{normalizedSymbol}' not found.", ex);
        }

        var supportedType = InstrumentType.Crypto.ToString();
        if (!string.Equals(instrument.Type, supportedType, StringComparison.OrdinalIgnoreCase)
            || instrument.IsBlocked
            || !string.Equals(instrument.Status, InstrumentStatus.Approved.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new KeyNotFoundException($"Crypto instrument '{normalizedSymbol}' is not available.");
        }

        limit = Math.Clamp(limit, 1, 500);
        var candles = await _candleRepository.GetBySymbolAsync(normalizedSymbol, limit, cancellationToken);

        return candles
            .OrderBy(c => c.OpenTime)
            .Select(c => new CandleDto(
                Symbol: c.Symbol,
                OpenTime: c.OpenTime,
                CloseTime: c.CloseTime,
                Open: c.Open,
                High: c.High,
                Low: c.Low,
                Close: c.Close,
                Volume: c.Volume));
    }
}
