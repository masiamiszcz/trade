using AutoMapper;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class InstrumentService : IInstrumentService
{
    private readonly IInstrumentRepository _instrumentRepository;
    private readonly IAdminRequestRepository _adminRequestRepository;
    private readonly IMapper _mapper;

    public InstrumentService(
        IInstrumentRepository instrumentRepository,
        IAdminRequestRepository adminRequestRepository,
        IMapper mapper)
    {
        _instrumentRepository = instrumentRepository;
        _adminRequestRepository = adminRequestRepository;
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

    public async Task<IEnumerable<InstrumentDto>> GetAllAsync(int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 1000) pageSize = 1000; // Cap at 1000 to prevent abuse

        // Get all instruments (could optimize with repository GetPagedAsync if needed)
        var instruments = await _instrumentRepository.GetAllAsync(cancellationToken);
        
        // Apply pagination in memory (for now; could optimize with DB-level pagination)
        var paginated = instruments
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return _mapper.Map<IEnumerable<InstrumentDto>>(paginated);
    }

    public async Task<IEnumerable<InstrumentDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        // For public API: only approved and not blocked
        var instruments = await _instrumentRepository.GetAllAsync(cancellationToken);
        var filtered = instruments
            .Where(x => x.Status == InstrumentStatus.Approved && !x.IsBlocked);
        
        return _mapper.Map<IEnumerable<InstrumentDto>>(filtered);
    }

    public async Task<InstrumentDto> CreateAsync(CreateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default)
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
            Description: request.Description ?? string.Empty,
            Type: instrumentType,
            Pillar: pillar,
            BaseCurrency: request.BaseCurrency.ToUpper(),
            QuoteCurrency: request.QuoteCurrency.ToUpper(),
            Status: InstrumentStatus.Draft,
            IsBlocked: false,
            CreatedBy: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ModifiedBy: null,
            ModifiedAtUtc: null,
            RowVersion: 0
        );

        await _instrumentRepository.AddAsync(instrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<InstrumentDto> UpdateAsync(Guid id, UpdateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var updatedInstrument = instrument with
        {
            Name = request.Name,
            Description = request.Description ?? instrument.Description,
            BaseCurrency = request.BaseCurrency ?? instrument.BaseCurrency,
            QuoteCurrency = request.QuoteCurrency ?? instrument.QuoteCurrency,
            ModifiedBy = adminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(updatedInstrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updatedInstrument);
    }

    public async Task<InstrumentDto> BlockAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var blockedInstrument = instrument with 
        { 
            IsBlocked = true,
            ModifiedBy = adminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };
        await _instrumentRepository.UpdateAsync(blockedInstrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(blockedInstrument);
    }

    public async Task<InstrumentDto> UnblockAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var unblockedInstrument = instrument with 
        { 
            IsBlocked = false,
            ModifiedBy = adminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };
        await _instrumentRepository.UpdateAsync(unblockedInstrument, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(unblockedInstrument);
    }

    public async Task DeleteAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        await _instrumentRepository.DeleteAsync(id, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);
    }

    // ============ FAZA 3: STATE MACHINE WORKFLOW IMPLEMENTATION ============

    public async Task<InstrumentDto> RequestApprovalAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        // 2. Validate transition: Draft → PendingApproval
        ValidateTransition(instrument.Status, InstrumentStatus.PendingApproval);

        // 3. Validate preconditions
        if (string.IsNullOrWhiteSpace(instrument.Description))
            throw new InvalidOperationException("Instrument description is required before requesting approval.");

        // 4. Update status
        var updated = instrument with
        {
            Status = InstrumentStatus.PendingApproval,
            ModifiedBy = adminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(updated, cancellationToken);
        
        // 5. Create AdminRequest for audit trail
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.RequestApproval,
            Reason: $"Requested approval by admin {adminId}",
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updated);
    }

    public async Task<InstrumentDto> ApproveAsync(Guid id, Guid approverAdminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        // 2. Validate transition: PendingApproval → Approved
        ValidateTransition(instrument.Status, InstrumentStatus.Approved);

        // 3. Self-approval check (CRITICAL RULE)
        ValidateSelfApproval(approverAdminId, instrument.CreatedBy, "Self-approval is not allowed.");

        // 4. Update status
        var updated = instrument with
        {
            Status = InstrumentStatus.Approved,
            ModifiedBy = approverAdminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(updated, cancellationToken);

        // 5. Update/Create AdminRequest for audit trail
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: approverAdminId,
            ApprovedByAdminId: approverAdminId,
            Action: AdminRequestActionType.Approve,
            Reason: $"Approved by admin {approverAdminId}",
            Status: AdminRequestStatus.Approved,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: DateTimeOffset.UtcNow
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updated);
    }

    public async Task<InstrumentDto> RejectAsync(Guid id, string rejectionReason, Guid rejectedByAdminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        // 2. Validate transition: PendingApproval → Rejected
        ValidateTransition(instrument.Status, InstrumentStatus.Rejected);

        // 3. Self-approval check (CRITICAL RULE)
        ValidateSelfApproval(rejectedByAdminId, instrument.CreatedBy, "Self-rejection is not allowed.");

        // 4. Validate rejection reason
        if (string.IsNullOrWhiteSpace(rejectionReason) || rejectionReason.Length < 10)
            throw new ArgumentException("Rejection reason must be at least 10 characters.");

        // 5. Update status
        var updated = instrument with
        {
            Status = InstrumentStatus.Rejected,
            ModifiedBy = rejectedByAdminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(updated, cancellationToken);

        // 6. Create AdminRequest for audit trail (stores rejection reason)
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: rejectedByAdminId,
            ApprovedByAdminId: rejectedByAdminId,
            Action: AdminRequestActionType.Reject,
            Reason: rejectionReason, // Store the rejection reason for audit
            Status: AdminRequestStatus.Rejected,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: DateTimeOffset.UtcNow
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updated);
    }

    public async Task<InstrumentDto> RetrySubmissionAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        // 2. Validate transition: Rejected → Draft
        ValidateTransition(instrument.Status, InstrumentStatus.Draft);

        // 3. Update status back to Draft
        var updated = instrument with
        {
            Status = InstrumentStatus.Draft,
            ModifiedBy = adminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(updated, cancellationToken);

        // 4. Create AdminRequest for audit trail
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.RetrySubmission,
            Reason: "Resubmitting after rejection",
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updated);
    }

    public async Task<InstrumentDto> ArchiveAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        // 2. Validate transition: Approved → Archived
        ValidateTransition(instrument.Status, InstrumentStatus.Archived);

        // 3. Update status
        var updated = instrument with
        {
            Status = InstrumentStatus.Archived,
            ModifiedBy = adminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(updated, cancellationToken);

        // 4. Create AdminRequest for audit trail
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: adminId,
            Action: AdminRequestActionType.Archive,
            Reason: $"Archived by admin {adminId}",
            Status: AdminRequestStatus.Approved,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: DateTimeOffset.UtcNow
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updated);
    }

    // ============ HELPER METHODS: STATE MACHINE VALIDATION ============

    /// <summary>
    /// Validates if a state transition is legal according to state machine rules.
    /// Throws InvalidOperationException if transition is not allowed.
    /// </summary>
    private static void ValidateTransition(InstrumentStatus fromStatus, InstrumentStatus toStatus)
    {
        var isAllowed = (fromStatus, toStatus) switch
        {
            // Explicit allowed transitions
            (InstrumentStatus.Draft, InstrumentStatus.PendingApproval) => true,
            (InstrumentStatus.PendingApproval, InstrumentStatus.Approved) => true,
            (InstrumentStatus.PendingApproval, InstrumentStatus.Rejected) => true,
            (InstrumentStatus.Rejected, InstrumentStatus.Draft) => true,
            (InstrumentStatus.Approved, InstrumentStatus.Blocked) => true,
            (InstrumentStatus.Blocked, InstrumentStatus.Approved) => true,
            (InstrumentStatus.Approved, InstrumentStatus.Archived) => true,

            // All others are forbidden
            _ => false
        };

        if (!isAllowed)
            throw new InvalidOperationException(
                $"Cannot transition instrument status from {fromStatus} to {toStatus}. " +
                $"This transition is not allowed by the state machine rules.");
    }

    /// <summary>
    /// Validates that the approver is not the creator (self-approval prevention).
    /// Throws InvalidOperationException if approverAdminId equals creatorAdminId.
    /// </summary>
    private static void ValidateSelfApproval(Guid approverAdminId, Guid creatorAdminId, string message = "Self-approval is not allowed.")
    {
        if (approverAdminId == creatorAdminId)
            throw new InvalidOperationException(message);
    }
}
