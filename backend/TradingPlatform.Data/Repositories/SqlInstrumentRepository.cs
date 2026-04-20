
using Microsoft.EntityFrameworkCore;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Context;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Repositories;

public sealed class SqlInstrumentRepository : IInstrumentRepository
{
    private readonly TradingPlatformDbContext _dbContext;

    public SqlInstrumentRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Instrument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Instruments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Instrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Instruments.FirstOrDefaultAsync(x => x.Symbol == symbol, cancellationToken);
        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<Instrument>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Instruments.ToListAsync(cancellationToken);
        return entities.Select(MapToDomain);
    }

    public async Task<IEnumerable<Instrument>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Instruments
            .Where(x => x.IsActive && !x.IsBlocked)
            .ToListAsync(cancellationToken);
        return entities.Select(MapToDomain);
    }

    public async Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default)
    {
        var entity = new InstrumentEntity
        {
            Id = instrument.Id,
            Symbol = instrument.Symbol,
            Name = instrument.Name,
            Type = instrument.Type,
            Pillar = instrument.Pillar,
            BaseCurrency = instrument.BaseCurrency,
            QuoteCurrency = instrument.QuoteCurrency,
            IsActive = instrument.IsActive,
            IsBlocked = instrument.IsBlocked,
            CreatedAtUtc = instrument.CreatedAtUtc
        };

        _dbContext.Instruments.Add(entity);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Instruments.FirstOrDefaultAsync(x => x.Id == instrument.Id, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException($"Instrument with ID {instrument.Id} not found.");

        entity.Symbol = instrument.Symbol;
        entity.Name = instrument.Name;
        entity.Type = instrument.Type;
        entity.Pillar = instrument.Pillar;
        entity.BaseCurrency = instrument.BaseCurrency;
        entity.QuoteCurrency = instrument.QuoteCurrency;
        entity.IsActive = instrument.IsActive;
        entity.IsBlocked = instrument.IsBlocked;

        _dbContext.Instruments.Update(entity);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Instruments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        _dbContext.Instruments.Remove(entity);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    private static Instrument MapToDomain(InstrumentEntity entity)
        => new Instrument(
            entity.Id,
            entity.Symbol,
            entity.Name,
            entity.Type,
            entity.Pillar,
            entity.BaseCurrency,
            entity.QuoteCurrency,
            entity.IsActive,
            entity.IsBlocked,
            entity.CreatedAtUtc);
}
