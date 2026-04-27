using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Executes approved actions on Instrument entities (Create, Update, Delete, Block, Unblock, RequestApproval).
/// 
/// Responsibilities:
/// - Execute instrument-specific approval actions
/// - Validate instrument state (double-check before execution)
/// - Create audit logs for actions
/// - Manage instrument service orchestration
/// 
/// Domain Ownership:
/// This handler owns all instrument lifecycle rules and execution logic.
/// ApprovalService doesn't know about instrument details - only calls handler.Execute()
/// </summary>
public interface IInstrumentApprovalHandler
{
    /// <summary>
    /// Execute an approved action on an Instrument entity.
    /// 
    /// Flow:
    /// 1. Validate instrument exists (if action requires it)
    /// 2. Validate status is appropriate for the action
    /// 3. Execute service operation via IInstrumentService
    /// 4. Create audit log entry
    /// 5. ATOMIC: Save changes + audit log together
    /// </summary>
    /// <param name="request">The approved admin request</param>
    /// <param name="approvedByAdminId">Admin who approved the request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>InstrumentDto with updated state</returns>
    Task<InstrumentDto> ExecuteAsync(
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken);
}
