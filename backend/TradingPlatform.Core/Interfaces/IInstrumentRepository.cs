
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IInstrumentRepository
{
    Task<Instrument?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Instrument?> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<Instrument>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Instrument>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Instrument instrument, CancellationToken cancellationToken = default);
    Task UpdateAsync(Instrument instrument, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
