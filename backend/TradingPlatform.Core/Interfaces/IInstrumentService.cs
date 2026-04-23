
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

public interface IInstrumentService
{
    Task<InstrumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InstrumentDto> GetBySymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<InstrumentDto>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
    Task<IEnumerable<InstrumentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    // ============ FAZA 3: STATE MACHINE WORKFLOW (INCLUDING CREATE) ============

    /// <summary>
    /// Request creation of new instrument
    /// Creates AdminRequest with Action=Create, Status=Pending
    /// Payload contains: symbol, name, description, type, pillar, baseCurrency, quoteCurrency
    /// Idempotent: Same payload hash reuses existing pending request
    /// Instrument is NOT created until admin approval
    /// </summary>
    Task<InstrumentDto> RequestCreateAsync(CreateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default);

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

    // ============ ISSUE 3: APPROVAL-BASED MUTATIONS ============

    /// <summary>
    /// Request approval for instrument update
    /// Creates AdminRequest with Action=Update, Status=Pending
    /// Payload contains: name, description, baseCurrency, quoteCurrency
    /// Idempotent: Same payload hash reuses existing pending request
    /// </summary>
    Task<InstrumentDto> RequestUpdateAsync(Guid id, UpdateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request approval for instrument deletion
    /// Creates AdminRequest with Action=Delete, Status=Pending
    /// Payload contains: symbol
    /// Idempotent: Multiple delete requests for same instrument treated as duplicate
    /// </summary>
    Task<InstrumentDto> RequestDeleteAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request approval for blocking instrument
    /// Creates AdminRequest with Action=Block, Status=Pending
    /// Payload contains: reason
    /// Idempotent: Multiple block requests for same instrument treated as duplicate
    /// Throws InvalidOperationException if already blocked
    /// </summary>
    Task<InstrumentDto> RequestBlockAsync(Guid id, string reason, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request approval for unblocking instrument
    /// Creates AdminRequest with Action=Unblock, Status=Pending
    /// Payload contains: reason
    /// Idempotent: Multiple unblock requests for same instrument treated as duplicate
    /// Throws InvalidOperationException if not currently blocked
    /// </summary>
    Task<InstrumentDto> RequestUnblockAsync(Guid id, string reason, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute approved update operation (internal, called by ApprovalService after approval)
    /// </summary>
    Task<InstrumentDto> ExecuteApprovedUpdateAsync(Guid id, string payloadJson, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute approved create operation (internal, called by ApprovalService after approval)
    /// Deserializes payload and actually creates the instrument with Draft status
    /// </summary>
    Task<InstrumentDto> ExecuteApprovedCreateAsync(Guid id, string payloadJson, Guid createdByAdminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute approved delete operation (internal, called by ApprovalService after approval)
    /// </summary>
    Task ExecuteApprovedDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute approved block operation (internal, called by ApprovalService after approval)
    /// </summary>
    Task<InstrumentDto> ExecuteApprovedBlockAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute approved unblock operation (internal, called by ApprovalService after approval)
    /// </summary>
    Task<InstrumentDto> ExecuteApprovedUnblockAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// EMERGENCY PATH: Blocks instrument immediately without approval. For compliance/emergency scenarios only.
    /// CRITICAL: Only allow SuperAdmin or specially-privileged admins to call this endpoint.
    /// </summary>
    Task<InstrumentDto> BlockInstrumentImmediateAsync(Guid id, string reason, Guid adminId, CancellationToken cancellationToken = default);
}
