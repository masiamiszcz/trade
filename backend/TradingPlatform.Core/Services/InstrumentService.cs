using AutoMapper;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class InstrumentService : IInstrumentService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly IMapper _mapper;

    public InstrumentService(IInstrumentRepository instrumentRepository, IMapper mapper)
    {
        _instrumentRepository = instrumentRepository;
        _mapper = mapper;
    }

    public async Task<InstrumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        return _mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<InstrumentDto> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetBySymbolAsync(symbol, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with symbol {symbol} not found.");

        return _mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<IEnumerable<InstrumentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var instruments = await _instrumentRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<InstrumentDto>>(instruments);
    }

    public async Task<IEnumerable<InstrumentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var instruments = await _instrumentRepository.GetAllActiveAsync(cancellationToken);
        return _mapper.Map<IEnumerable<InstrumentDto>>(instruments);
    }

    public async Task<InstrumentDto> CreateAsync(CreateInstrumentRequest request, CancellationToken cancellationToken = default)
    {
        // Check if instrument already exists
        var existing = await _instrumentRepository.GetBySymbolAsync(request.Symbol, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Instrument with symbol {request.Symbol} already exists.");

        // Parse enums
        if (!Enum.TryParse<InstrumentType>(request.Type, true, out var instrumentType))
            throw new ArgumentException($"Invalid instrument type: {request.Type}");

        if (!Enum.TryParse<AccountPillar>(request.Pillar, true, out var pillar))
            throw new ArgumentException($"Invalid pillar: {request.Pillar}");

        var instrument = new Instrument(
            Id: Guid.NewGuid(),
            Symbol: request.Symbol.ToUpper(),
            Name: request.Name,
            Type: instrumentType,
            Pillar: pillar,
            BaseCurrency: request.BaseCurrency.ToUpper(),
            QuoteCurrency: request.QuoteCurrency.ToUpper(),
            IsActive: true,
            IsBlocked: false,
            CreatedAtUtc: DateTimeOffset.UtcNow
        );

        await _instrumentRepository.AddAsync(instrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<InstrumentDto> UpdateAsync(Guid id, UpdateInstrumentRequest request, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var updatedInstrument = instrument with
        {
            Name = request.Name,
            IsActive = request.IsActive
        };

        await _instrumentRepository.UpdateAsync(updatedInstrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updatedInstrument);
    }

    public async Task<InstrumentDto> BlockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var blockedInstrument = instrument with { IsBlocked = true };
        await _instrumentRepository.UpdateAsync(blockedInstrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(blockedInstrument);
    }

    public async Task<InstrumentDto> UnblockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var unblockedInstrument = instrument with { IsBlocked = false };
        await _instrumentRepository.UpdateAsync(unblockedInstrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(unblockedInstrument);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _instrumentRepository.DeleteAsync(id, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);
    }
}
