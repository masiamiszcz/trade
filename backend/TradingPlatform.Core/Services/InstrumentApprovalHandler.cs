using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Executes approved actions on Instrument entities (Create, Update, Delete, Block, Unblock, RequestApproval).
/// 
/// This handler owns the complete domain logic for instrument lifecycle:
/// - Validates instrument state
/// - Executes service operations
/// - Creates audit logs
/// - Manages persistence (ATOMIC operations)
/// 
/// ApprovalService calls this handler, not the service directly.
/// This ensures clean separation: ApprovalService is only orchestrator.
/// </summary>
public class InstrumentApprovalHandler : IInstrumentApprovalHandler
{
    private readonly IInstrumentService _instrumentService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<InstrumentApprovalHandler> _logger;

    public InstrumentApprovalHandler(
        IInstrumentService instrumentService,
        IAuditLogRepository auditLogRepository,
        ILogger<InstrumentApprovalHandler> logger)
    {
        _instrumentService = instrumentService ?? throw new ArgumentNullException(nameof(instrumentService));
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute an approved action on an Instrument entity.
    /// 
    /// Supports actions: Create, Update, Delete, Block, Unblock, RequestApproval
    /// 
    /// Flow:
    /// 1. Validate instrument exists (if action requires it)
    /// 2. Validate status is appropriate for the action
    /// 3. Execute service operation via IInstrumentService
    /// 4. Create audit log entry
    /// 5. ATOMIC: Save changes + audit log together
    /// </summary>
    public async Task<InstrumentDto> ExecuteAsync(
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing Instrument action {Action} on instrument {InstrumentId}",
            request.Action, request.EntityId);

        // For CREATE action, EntityId is null (entity doesn't exist yet)
        // For other actions, EntityId must exist
        if (request.Action != AdminRequestActionType.Create && request.EntityId == null)
        {
            throw new InvalidOperationException($"Cannot execute {request.Action} action without an entity ID");
        }

        InstrumentDto result;

        switch (request.Action)
        {
            case AdminRequestActionType.Create:
                result = await ExecuteCreateAsync(request, cancellationToken);
                break;

            case AdminRequestActionType.Update:
                result = await ExecuteUpdateAsync(request, cancellationToken);
                break;

            case AdminRequestActionType.Block:
                result = await ExecuteBlockAsync(request, cancellationToken);
                break;

            case AdminRequestActionType.Unblock:
                result = await ExecuteUnblockAsync(request, cancellationToken);
                break;

            case AdminRequestActionType.Delete:
                result = await ExecuteDeleteAsync(request, cancellationToken);
                break;

            case AdminRequestActionType.RequestApproval:
                result = await ExecuteApproveAsync(request, approvedByAdminId, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unknown action type: {request.Action}");
        }

        _logger.LogInformation(
            "Instrument action {Action} executed and audited for instrument {InstrumentId}",
            request.Action, request.EntityId);

        return result;
    }

    /// <summary>
    /// Execute CREATE action on an instrument.
    /// </summary>
    private async Task<InstrumentDto> ExecuteCreateAsync(
        AdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _instrumentService.ExecuteApprovedCreateAsync(
            request.EntityId ?? Guid.Empty,
            request.PayloadJson ?? string.Empty,
            request.RequestedByAdminId,
            cancellationToken);

        _logger.LogInformation("Instrument created successfully: {InstrumentId}", result.Id);

        // Create audit log
        var auditLog = CreateAuditLogEntry(
            adminId: request.RequestedByAdminId,
            action: "INSTRUMENT_CREATE",
            instrumentId: result.Id,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Execute UPDATE action on an instrument.
    /// </summary>
    private async Task<InstrumentDto> ExecuteUpdateAsync(
        AdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _instrumentService.ExecuteApprovedUpdateAsync(
            request.EntityId!.Value,
            request.PayloadJson ?? string.Empty,
            cancellationToken);

        _logger.LogInformation("Instrument updated successfully: {InstrumentId}", result.Id);

        // Create audit log
        var auditLog = CreateAuditLogEntry(
            adminId: request.RequestedByAdminId,
            action: "INSTRUMENT_UPDATE",
            instrumentId: result.Id,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Execute BLOCK action on an instrument.
    /// </summary>
    private async Task<InstrumentDto> ExecuteBlockAsync(
        AdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _instrumentService.ExecuteApprovedBlockAsync(
            request.EntityId!.Value,
            cancellationToken);

        _logger.LogInformation("Instrument blocked successfully: {InstrumentId}", result.Id);

        // Create audit log
        var auditLog = CreateAuditLogEntry(
            adminId: request.RequestedByAdminId,
            action: "INSTRUMENT_BLOCK",
            instrumentId: result.Id,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Execute UNBLOCK action on an instrument.
    /// </summary>
    private async Task<InstrumentDto> ExecuteUnblockAsync(
        AdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _instrumentService.ExecuteApprovedUnblockAsync(
            request.EntityId!.Value,
            cancellationToken);

        _logger.LogInformation("Instrument unblocked successfully: {InstrumentId}", result.Id);

        // Create audit log
        var auditLog = CreateAuditLogEntry(
            adminId: request.RequestedByAdminId,
            action: "INSTRUMENT_UNBLOCK",
            instrumentId: result.Id,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Execute DELETE action on an instrument.
    /// </summary>
    private async Task<InstrumentDto> ExecuteDeleteAsync(
        AdminRequest request,
        CancellationToken cancellationToken)
    {
        await _instrumentService.ExecuteApprovedDeleteAsync(
            request.EntityId!.Value,
            cancellationToken);

        _logger.LogInformation("Instrument deleted successfully: {InstrumentId}", request.EntityId);

        // Return empty DTO for delete
        var result = new InstrumentDto(
            Id: request.EntityId!.Value,
            Symbol: "DELETED",
            Name: "DELETED",
            Description: string.Empty,
            Type: string.Empty,
            Pillar: string.Empty,
            BaseCurrency: string.Empty,
            QuoteCurrency: string.Empty,
            Status: "Deleted",
            IsBlocked: false,
            CreatedBy: Guid.Empty,
            CreatedAtUtc: DateTimeOffset.UtcNow);

        // Create audit log
        var auditLog = CreateAuditLogEntry(
            adminId: request.RequestedByAdminId,
            action: "INSTRUMENT_DELETE",
            instrumentId: request.EntityId.Value,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Execute REQUEST_APPROVAL (approval) action on an instrument.
    /// </summary>
    private async Task<InstrumentDto> ExecuteApproveAsync(
        AdminRequest request,
        Guid approvedByAdminId,
        CancellationToken cancellationToken)
    {
        var result = await _instrumentService.ApproveAsync(
            request.EntityId!.Value,
            approvedByAdminId,
            cancellationToken);

        _logger.LogInformation("Instrument approved successfully: {InstrumentId}", result.Id);

        // Create audit log
        var auditLog = CreateAuditLogEntry(
            adminId: approvedByAdminId,
            action: "INSTRUMENT_APPROVE",
            instrumentId: result.Id,
            request: request);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <summary>
    /// Create audit log entry for instrument action.
    /// </summary>
    private AuditLog CreateAuditLogEntry(
        Guid adminId,
        string action,
        Guid instrumentId,
        AdminRequest request)
    {
        var details = System.Text.Json.JsonSerializer.Serialize(new
        {
            instrumentId = instrumentId,
            actionType = request.Action.ToString(),
            approvalRequestId = request.Id,
            reason = request.Reason,
            executionTimestamp = DateTimeOffset.UtcNow
        });

        return new AuditLog(
            Id: Guid.NewGuid(),
            AdminId: adminId,
            Action: action,
            EntityType: "Instrument",
            EntityId: instrumentId,
            Details: details,
            IpAddress: "Internal", // Internal action, no client IP
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }
}
