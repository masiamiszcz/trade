using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

public interface ICfdChartViewService
{
    Task<IReadOnlyList<CfdChartViewDto>> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CfdChartViewDto?> GetByIdForUserAsync(Guid userId, Guid viewId, CancellationToken cancellationToken = default);
    Task<CfdChartViewDto> CreateAsync(Guid userId, SaveCfdChartViewRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid userId, Guid viewId, CancellationToken cancellationToken = default);
}
