
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IInstrumentService
{
    Task<InstrumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstrumentDto> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<InstrumentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<InstrumentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<InstrumentDto> CreateAsync(CreateInstrumentRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentDto> UpdateAsync(Guid id, UpdateInstrumentRequest request, CancellationToken cancellationToken = default);
    Task<InstrumentDto> BlockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstrumentDto> UnblockAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
