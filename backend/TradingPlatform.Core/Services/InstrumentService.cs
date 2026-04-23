using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private readonly ILogger<InstrumentService> _logger;

    public InstrumentService(
        IInstrumentRepository instrumentRepository,
        IAdminRequestRepository adminRequestRepository,
        IMapper mapper,
        ILogger<InstrumentService> logger)
    {
        _instrumentRepository = instrumentRepository;
        _adminRequestRepository = adminRequestRepository;
        _mapper = mapper;
        _logger = logger;
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

    public async Task<InstrumentDto> RequestCreateAsync(CreateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Check if instrument with this symbol already exists
        var existing = await _instrumentRepository.GetBySymbolAsync(request.Symbol, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Instrument with symbol {request.Symbol} already exists.");

        // 2. Validate enum values early
        if (!Enum.TryParse<InstrumentType>(request.Type, true, out var instrumentType))
            throw new ArgumentException($"Invalid instrument type: {request.Type}");

        if (!Enum.TryParse<AccountPillar>(request.Pillar, true, out var pillar))
            throw new ArgumentException($"Invalid pillar: {request.Pillar}");

        // 3. Generate ID upfront (will be used in AdminRequest and later in actual creation)
        var instrumentId = Guid.NewGuid();

        // 4. Idempotency check: compare payload JSON strings directly
        var existingRequests = await _adminRequestRepository.GetByInstrumentIdAsync(instrumentId, cancellationToken);
        var existingCreateRequest = existingRequests.FirstOrDefault(r => r.Status == AdminRequestStatus.Pending && r.Action == AdminRequestActionType.Create);

        // Prepare payload for comparison (MUST match exactly what will be stored)
        var payloadJson = JsonSerializer.Serialize(new
        {
            symbol = request.Symbol.ToUpper(),
            name = request.Name,
            description = request.Description ?? string.Empty,
            type = instrumentType.ToString(),
            pillar = pillar.ToString(),
            baseCurrency = request.BaseCurrency.ToUpper(),
            quoteCurrency = request.QuoteCurrency.ToUpper()
        });

        if (existingCreateRequest is not null)
        {
            if (payloadJson == existingCreateRequest.PayloadJson)
            {
                _logger.LogInformation("Idempotent create request reused for instrument {InstrumentId}", instrumentId);
                // Return DTO for pending instrument (not yet created in DB)
                return new InstrumentDto(
                    Id: instrumentId,
                    Symbol: request.Symbol.ToUpper(),
                    Name: request.Name,
                    Description: request.Description ?? string.Empty,
                    Type: instrumentType.ToString(),
                    Pillar: pillar.ToString(),
                    BaseCurrency: request.BaseCurrency.ToUpper(),
                    QuoteCurrency: request.QuoteCurrency.ToUpper(),
                    Status: InstrumentStatus.Draft.ToString(),
                    IsBlocked: false,
                    CreatedBy: adminId,
                    CreatedAtUtc: DateTimeOffset.UtcNow
                );
            }
        }

        // 5. Create approval request (instrument will be created after approval)
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: instrumentId,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.Create,
            Reason: $"Requested creation by admin {adminId}",
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null,
            PayloadJson: payloadJson
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created approval request for new instrument {InstrumentId}", instrumentId);
        
        return new InstrumentDto(
            Id: instrumentId,
            Symbol: request.Symbol.ToUpper(),
            Name: request.Name,
            Description: request.Description ?? string.Empty,
            Type: instrumentType.ToString(),
            Pillar: pillar.ToString(),
            BaseCurrency: request.BaseCurrency.ToUpper(),
            QuoteCurrency: request.QuoteCurrency.ToUpper(),
            Status: InstrumentStatus.Draft.ToString(),
            IsBlocked: false,
            CreatedBy: adminId,
            CreatedAtUtc: DateTimeOffset.UtcNow
        );
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

        // 4. IDEMPOTENCY CHECK: Return existing pending request with same payload
        var existingRequests = await _adminRequestRepository.GetByInstrumentIdAsync(id, cancellationToken);
        var existingPendingRequest = existingRequests.FirstOrDefault(r => r.Status == AdminRequestStatus.Pending);
        
        if (existingPendingRequest is not null)
        {
            // Compare payloads: if same action with same structure → reuse (idempotent)
            // Payload format: { "action": "RequestApproval", "instrumentId": "...", "requestedBy": "..." }
            var currentPayloadHash = ComputePayloadHash(new { action = "RequestApproval", instrumentId = id, requestedByAdminId = adminId });
            var existingPayloadHash = ComputePayloadHash(existingPendingRequest.PayloadJson);
            
            if (currentPayloadHash == existingPayloadHash)
            {
                // Same action, same payload → return existing (idempotent)
                _logger.LogInformation("Idempotent request reused for instrument {InstrumentId}", id);
                return _mapper.Map<InstrumentDto>(instrument);
            }
            // Different payload → allow new request (different operation)
        }

        // 5. Update status
        var updated = instrument with
        {
            Status = InstrumentStatus.PendingApproval,
            ModifiedBy = adminId,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(updated, cancellationToken);
        
        // 6. Create AdminRequest for audit trail with payload JSON
        // Payload contains ONLY business data (no timestamp, no requestedByAdminId - those are in AdminRequest)
        var payloadJson = JsonSerializer.Serialize(new { });  // RequestApproval has no business data
        
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.RequestApproval,
            Reason: $"Requested approval by admin {adminId}",
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null,
            PayloadJson: payloadJson  // Full payload for audit
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(updated);
    }

    public async Task<InstrumentDto> RequestUpdateAsync(Guid id, UpdateInstrumentRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        // 2. Idempotency check: compare payload JSON strings directly
        var existingRequests = await _adminRequestRepository.GetByInstrumentIdAsync(id, cancellationToken);
        var existingUpdateRequest = existingRequests.FirstOrDefault(r => r.Status == AdminRequestStatus.Pending && r.Action == AdminRequestActionType.Update);
        
        // Prepare payload for comparison (MUST match exactly what will be stored)
        var payloadJson = JsonSerializer.Serialize(new { 
            name = request.Name,
            description = request.Description,
            baseCurrency = request.BaseCurrency,
            quoteCurrency = request.QuoteCurrency
        });
        
        if (existingUpdateRequest is not null)
        {
            if (payloadJson == existingUpdateRequest.PayloadJson)
            {
                _logger.LogInformation("Idempotent update request reused for instrument {InstrumentId}", id);
                return _mapper.Map<InstrumentDto>(instrument);
            }
        }

        // 3. Create approval request
        // payloadJson already prepared above for idempotency check
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.Update,
            Reason: $"Requested update by admin {adminId}",
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null,
            PayloadJson: payloadJson
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<InstrumentDto> RequestDeleteAsync(Guid id, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        // 2. Idempotency check
        var existingRequests = await _adminRequestRepository.GetByInstrumentIdAsync(id, cancellationToken);
        var existingDeleteRequest = existingRequests.FirstOrDefault(r => r.Status == AdminRequestStatus.Pending && r.Action == AdminRequestActionType.Delete);
        
        if (existingDeleteRequest is not null)
        {
            _logger.LogInformation("Idempotent delete request reused for instrument {InstrumentId}", id);
            return _mapper.Map<InstrumentDto>(instrument);
        }

        // 3. Create approval request
        // Payload contains ONLY business data for Delete (no timestamp, no requestedByAdminId - those are in AdminRequest)
        var payloadJson = JsonSerializer.Serialize(new { });  // Delete has no business data to store
        
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.Delete,
            Reason: $"Requested deletion by admin {adminId}",
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null,
            PayloadJson: payloadJson
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<InstrumentDto> RequestBlockAsync(Guid id, string reason, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        if (instrument.IsBlocked)
            throw new InvalidOperationException("Instrument is already blocked");

        // 2. Idempotency check
        var existingRequests = await _adminRequestRepository.GetByInstrumentIdAsync(id, cancellationToken);
        var existingBlockRequest = existingRequests.FirstOrDefault(r => r.Status == AdminRequestStatus.Pending && r.Action == AdminRequestActionType.Block);
        
        if (existingBlockRequest is not null)
        {
            _logger.LogInformation("Idempotent block request reused for instrument {InstrumentId}", id);
            return _mapper.Map<InstrumentDto>(instrument);
        }

        // 3. Create approval request
        // Payload contains ONLY business data for Block (no timestamp, no requestedByAdminId - those are in AdminRequest)
        var payloadJson = JsonSerializer.Serialize(new { 
            reason = reason
        });
        
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.Block,
            Reason: reason,
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null,
            PayloadJson: payloadJson
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(instrument);
    }

    public async Task<InstrumentDto> RequestUnblockAsync(Guid id, string reason, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        if (!instrument.IsBlocked)
            throw new InvalidOperationException("Instrument is not blocked");

        // 2. Idempotency check
        var existingRequests = await _adminRequestRepository.GetByInstrumentIdAsync(id, cancellationToken);
        var existingUnblockRequest = existingRequests.FirstOrDefault(r => r.Status == AdminRequestStatus.Pending && r.Action == AdminRequestActionType.Unblock);
        
        if (existingUnblockRequest is not null)
        {
            _logger.LogInformation("Idempotent unblock request reused for instrument {InstrumentId}", id);
            return _mapper.Map<InstrumentDto>(instrument);
        }

        // 3. Create approval request
        // Payload contains ONLY business data for Unblock (no timestamp, no requestedByAdminId - those are in AdminRequest)
        var payloadJson = JsonSerializer.Serialize(new { 
            reason = reason
        });
        
        var adminRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: null,
            Action: AdminRequestActionType.Unblock,
            Reason: reason,
            Status: AdminRequestStatus.Pending,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: null,
            PayloadJson: payloadJson
        );

        await _adminRequestRepository.AddAsync(adminRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        return _mapper.Map<InstrumentDto>(instrument);
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

    // ============ INTERNAL EXECUTION METHODS (called by ApprovalService after approve) ============

    /// <summary>
    /// Execute approved update operation. Called internally by approval workflow.
    /// </summary>
    public async Task<InstrumentDto> ExecuteApprovedUpdateAsync(Guid id, string payloadJson, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            var updated = instrument with
            {
                Name = payload.GetProperty("name").GetString() ?? instrument.Name,
                Description = payload.GetProperty("description").GetString() ?? instrument.Description,
                BaseCurrency = payload.GetProperty("baseCurrency").GetString() ?? instrument.BaseCurrency,
                QuoteCurrency = payload.GetProperty("quoteCurrency").GetString() ?? instrument.QuoteCurrency,
                ModifiedAtUtc = DateTimeOffset.UtcNow
            };

            await _instrumentRepository.UpdateAsync(updated, cancellationToken);
            await _instrumentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Executed approved update for instrument {InstrumentId}", id);
            return _mapper.Map<InstrumentDto>(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute approved update for instrument {InstrumentId}", id);
            throw;
        }
    }

    /// <summary>
    /// Execute approved delete operation. Called internally by approval workflow.
    /// </summary>
    public async Task ExecuteApprovedDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        await _instrumentRepository.DeleteAsync(id, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Executed approved delete for instrument {InstrumentId}", id);
    }

    /// <summary>
    /// Execute approved block operation. Called internally by approval workflow.
    /// </summary>
    public async Task<InstrumentDto> ExecuteApprovedBlockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var blocked = instrument with
        {
            IsBlocked = true,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(blocked, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Executed approved block for instrument {InstrumentId}", id);
        return _mapper.Map<InstrumentDto>(blocked);
    }

    /// <summary>
    /// Execute approved unblock operation. Called internally by approval workflow.
    /// </summary>
    public async Task<InstrumentDto> ExecuteApprovedUnblockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        var unblocked = instrument with
        {
            IsBlocked = false,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(unblocked, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Executed approved unblock for instrument {InstrumentId}", id);
        return _mapper.Map<InstrumentDto>(unblocked);
    }

    /// <summary>
    /// Execute approved create operation. Called internally by approval workflow.
    /// Deserializes payload JSON and creates new instrument in database.
    /// </summary>
    public async Task<InstrumentDto> ExecuteApprovedCreateAsync(Guid id, string payloadJson, Guid createdByAdminId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
            var symbol = payload.GetProperty("symbol").GetString()?.ToUpper() ?? throw new InvalidOperationException("Symbol not found in payload");
            
            // Check if instrument with this symbol already exists (double-check safety)
            var existingBySymbol = await _instrumentRepository.GetBySymbolAsync(symbol, cancellationToken);
            if (existingBySymbol is not null)
                throw new InvalidOperationException($"Instrument with symbol {symbol} already exists.");

            // Parse enums from payload
            var typeStr = payload.GetProperty("type").GetString() ?? throw new InvalidOperationException("Type not found in payload");
            var pillarStr = payload.GetProperty("pillar").GetString() ?? throw new InvalidOperationException("Pillar not found in payload");
            
            if (!Enum.TryParse<InstrumentType>(typeStr, true, out var instrumentType))
                throw new ArgumentException($"Invalid instrument type in payload: {typeStr}");
            
            if (!Enum.TryParse<AccountPillar>(pillarStr, true, out var pillar))
                throw new ArgumentException($"Invalid pillar in payload: {pillarStr}");

            // Create new instrument from payload
            var newInstrument = new Instrument(
                Id: id,  // Use the ID from AdminRequest.InstrumentId
                Symbol: symbol,
                Name: payload.GetProperty("name").GetString() ?? "",
                Description: payload.GetProperty("description").GetString() ?? string.Empty,
                Type: instrumentType,
                Pillar: pillar,
                BaseCurrency: payload.GetProperty("baseCurrency").GetString()?.ToUpper() ?? "",
                QuoteCurrency: payload.GetProperty("quoteCurrency").GetString()?.ToUpper() ?? "",
                Status: InstrumentStatus.Draft,
                IsBlocked: false,
                CreatedBy: createdByAdminId,
                CreatedAtUtc: DateTimeOffset.UtcNow,
                ModifiedBy: createdByAdminId,
                ModifiedAtUtc: DateTimeOffset.UtcNow
            );

            await _instrumentRepository.AddAsync(newInstrument, cancellationToken);
            await _instrumentRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Executed approved create for instrument {InstrumentId} with symbol {Symbol}", id, symbol);
            return _mapper.Map<InstrumentDto>(newInstrument);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute approved create for instrument {InstrumentId}", id);
            throw;
        }
    }

    /// <summary>
    /// EMERGENCY PATH: Blocks instrument IMMEDIATELY without requiring approval (for compliance/emergency scenarios).
    /// This bypasses the approval workflow - use with caution and appropriate role-based authorization in API layer.
    /// CRITICAL: Only allow SuperAdmin or specially-privileged admins to call this endpoint.
    /// </summary>
    public async Task<InstrumentDto> BlockInstrumentImmediateAsync(Guid id, string reason, Guid adminId, CancellationToken cancellationToken = default)
    {
        // 1. Fetch and validate
        var instrument = await _instrumentRepository.GetByIdAsync(id, cancellationToken);
        if (instrument is null)
            throw new InvalidOperationException($"Instrument with ID {id} not found.");

        if (instrument.IsBlocked)
            throw new InvalidOperationException("Instrument is already blocked");

        // 2. Validate reason
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5)
            throw new ArgumentException("Blocking reason must be at least 5 characters");

        // 3. Perform immediate block (NO approval request created)
        var blocked = instrument with
        {
            IsBlocked = true,
            ModifiedAtUtc = DateTimeOffset.UtcNow
        };

        await _instrumentRepository.UpdateAsync(blocked, cancellationToken);

        // 4. Create audit log entry (retroactive - after immediate execution)
        var auditRequest = new AdminRequest(
            Id: Guid.NewGuid(),
            InstrumentId: id,
            RequestedByAdminId: adminId,
            ApprovedByAdminId: adminId,  // Self-approved emergency action
            Action: AdminRequestActionType.Block,
            Reason: $"[IMMEDIATE/EMERGENCY] {reason}",
            Status: AdminRequestStatus.Approved,  // Mark as approved since it was executed immediately
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ApprovedAtUtc: DateTimeOffset.UtcNow,
            PayloadJson: JsonSerializer.Serialize(new { reason = reason, isImmediate = true })
        );

        await _adminRequestRepository.AddAsync(auditRequest, cancellationToken);
        await _instrumentRepository.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("EMERGENCY BLOCK executed immediately for instrument {InstrumentId} by admin {AdminId}. Reason: {Reason}", id, adminId, reason);
        return _mapper.Map<InstrumentDto>(blocked);
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

    /// <summary>
    /// Computes SHA256 hash of payload for idempotency comparison.
    /// Handles both JSON strings and objects.
    /// </summary>
    private string ComputePayloadHash(object payload)
    {
        try
        {
            string json = payload is string str ? str : JsonSerializer.Serialize(payload);
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
            return Convert.ToBase64String(hashBytes);
        }
        catch
        {
            // Fallback: return empty hash if serialization fails
            return string.Empty;
        }
    }
}
