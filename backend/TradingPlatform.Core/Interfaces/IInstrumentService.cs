
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IInstrumentService
{
    Task<InstrumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstrumentDto> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<InstrumentDto>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<InstrumentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Create new instrument - Status starts as Draft, CreatedBy set to current admin
    /// </summary>
    Task<InstrumentDto> CreateAsync(CreateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Update existing instrument - ModifiedBy set to current admin
    /// </summary>
    Task<InstrumentDto> UpdateAsync(Guid id, UpdateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Block instrument - Sets IsBlocked=true, ModifiedBy set to current admin
    /// </summary>
    Task<InstrumentDto> BlockAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Unblock instrument - Sets IsBlocked=false, ModifiedBy set to current admin
    /// </summary>
    Task<InstrumentDto> UnblockAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Delete instrument permanently - Removes from database, ModifiedBy set to current admin
    /// </summary>
    Task DeleteAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);

    // ============ FAZA 3: STATE MACHINE WORKFLOW ============

    /// <summary>
    /// Request approval for a draft instrument
    /// Transition: Draft → PendingApproval
    /// Only creator or delegating admin can request
    /// Creates AdminRequest with Action=RequestApproval, Status=Pending
    /// </summary>
    Task<InstrumentDto> RequestApprovalAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve a pending instrument
    /// Transition: PendingApproval → Approved
    /// Requires approverAdminId ≠ CreatedBy (self-approval forbidden)
    /// Updates AdminRequest with Action=Approve, Status=Approved, ApprovedByAdminId, ApprovedAtUtc
    /// Throws InvalidOperationException if self-approval detected
    /// </summary>
    Task<InstrumentDto> ApproveAsync(Guid id, Guid approverAdminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reject a pending instrument with reason
    /// Transition: PendingApproval → Rejected
    /// Requires approverAdminId ≠ CreatedBy (self-approval forbidden)
    /// rejectionReason: min 10 chars, saved in AdminRequest.Reason for audit trail
    /// Updates AdminRequest with Action=Reject, Status=Rejected (confusing naming - means request was processed)
    /// Throws InvalidOperationException if self-approval detected or reason too short
    /// </summary>
    Task<InstrumentDto> RejectAsync(Guid id, string rejectionReason, Guid rejectedByAdminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retry a rejected instrument submission
    /// Transition: Rejected → Draft (allows re-editing and re-submission)
    /// Creator or any admin can retry
    /// Creates new AdminRequest with Action=RetrySubmission
    /// </summary>
    Task<InstrumentDto> RetrySubmissionAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive an approved instrument (soft delete - not removed from DB)
    /// Transition: Approved → Archived
    /// Any admin can archive
    /// Creates AdminRequest with Action=Archive
    /// </summary>
    Task<InstrumentDto> ArchiveAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);
}
