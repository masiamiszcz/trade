

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]

public sealed class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(IAdminUserService adminUserService, ILogger<AdminUsersController> logger)
    {
        _adminUserService = adminUserService;
        _logger = logger;
    }

    /// <summary>
    /// Helper: Extract admin ID from JWT token
    /// </summary>
    private Guid GetCurrentAdminId()
    {
        var userIdClaim = User.FindFirst("userId") ?? User.FindFirst("sub") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var adminId))
        {
            _logger.LogWarning("❌ [AUTH] Could not extract admin ID from JWT token");
            throw new UnauthorizedAccessException("Could not extract admin ID from token");
        }
        return adminId;
    }

    /// <summary>
    /// Get all users (ADMIN only)
    /// Query param: status (Active, Blocked, Deleted, or null for all)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserListItemDto>>> GetAllUsers(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Admin requesting all users");

            var users = await _adminUserService.GetAllUsersAsync(cancellationToken);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve users" });
        }
    }

    /// <summary>
    /// Block a user (prevent login, trading, etc.)
    /// </summary>
    [HttpPost("users/{userId}/block")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserListItemDto>> BlockUser(
        Guid userId,
        [FromBody] BlockUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔒 [API] Block request received for user {UserId}", userId);

            // Extract admin ID from JWT
            var currentAdminId = GetCurrentAdminId();
            _logger.LogInformation("👤 [API] Request from admin {AdminId}", currentAdminId);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("❌ [API] Invalid user ID: {UserId}", userId);
                return BadRequest(new { error = "Invalid user ID" });
            }

            if (request == null)
            {
                _logger.LogWarning("❌ [API] Request body is null");
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation(
                "📋 [API] Block request details - Reason: {Reason}, Duration: {Duration}ms",
                request.Reason,
                request.DurationMs);

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                _logger.LogWarning("❌ [API] Reason is missing or empty");
                return BadRequest(new { error = "Reason is required" });
            }

            if (request.DurationMs < 0)
            {
                _logger.LogWarning("❌ [API] Invalid duration: {Duration}ms", request.DurationMs);
                return BadRequest(new { error = "Duration must be non-negative" });
            }

            _logger.LogInformation(
                "🚀 [API] Calling AdminUserService.BlockUserAsync for user {UserId} by admin {AdminId}",
                userId,
                currentAdminId);

            var blockedUser = await _adminUserService.BlockUserAsync(
                userId, 
                request.Reason, 
                request.DurationMs,
                currentAdminId);
            
            _logger.LogInformation(
                "✅ [API] User blocked successfully. Mapping to DTO...");
            
            // Map to DTO for response
            _logger.LogInformation(
                "🔄 [API] Creating UserListItemDto - Status: {Status}, Blocked: {IsBlocked}",
                blockedUser.Status,
                blockedUser.IsBlocked);

            var dto = new UserListItemDto(
                blockedUser.Id,
                blockedUser.UserName,
                blockedUser.Email,
                blockedUser.FirstName,
                blockedUser.LastName,
                blockedUser.Role.ToString(),
                blockedUser.Status.ToString(),
                blockedUser.BlockReason,
                blockedUser.BlockedUntilUtc,
                blockedUser.DeletedAtUtc,
                blockedUser.LastLoginAtUtc,
                blockedUser.CreatedAtUtc
            );

            _logger.LogInformation(
                "✅ [API] Returning blocked user DTO for {UserName}",
                dto.UserName);

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "❌ [API] User not found: {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [API] Error blocking user {UserId}", userId);
            _logger.LogError(ex, "❌ [API] Exception message: {Message}", ex.Message);
            _logger.LogError(ex, "❌ [API] Stack trace: {StackTrace}", ex.StackTrace);
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "❌ [API] Inner exception: {Message}", ex.InnerException.Message);
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to block user" });
        }
    }

    /// <summary>
    /// Unblock a user (restore access)
    /// </summary>
    [HttpPost("users/{userId}/unblock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserListItemDto>> UnblockUser(
        Guid userId,
        [FromBody] UnblockUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔓 [API] Unblock request received for user {UserId}", userId);

            // Extract admin ID from JWT
            var currentAdminId = GetCurrentAdminId();
            _logger.LogInformation("👤 [API] Request from admin {AdminId}", currentAdminId);

            if (userId == Guid.Empty)
            {
                _logger.LogWarning("❌ [API] Invalid user ID: {UserId}", userId);
                return BadRequest(new { error = "Invalid user ID" });
            }

            if (request == null)
            {
                _logger.LogWarning("❌ [API] Request body is null");
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                _logger.LogWarning("❌ [API] Reason is missing or empty");
                return BadRequest(new { error = "Reason is required" });
            }

            _logger.LogInformation(
                "🚀 [API] Calling AdminUserService.UnblockUserAsync for user {UserId} by admin {AdminId}",
                userId,
                currentAdminId);

            var unblockedUser = await _adminUserService.UnblockUserAsync(userId, request.Reason, currentAdminId);
            
            // Map to DTO for response
            var dto = new UserListItemDto(
                unblockedUser.Id,
                unblockedUser.UserName,
                unblockedUser.Email,
                unblockedUser.FirstName,
                unblockedUser.LastName,
                unblockedUser.Role.ToString(),
                unblockedUser.Status.ToString(),
                unblockedUser.BlockReason,
                unblockedUser.BlockedUntilUtc,
                unblockedUser.DeletedAtUtc,
                unblockedUser.LastLoginAtUtc,
                unblockedUser.CreatedAtUtc
            );

            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User not found: {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unblocking user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to unblock user" });
        }
    }

    /// <summary>
    /// Request user deletion (creates approval workflow)
    /// Actual deletion only happens after admin approval
    /// </summary>
    [HttpDelete("users/{userId}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminRequestDto>> RequestUserDeletion(
        Guid userId,
        [FromBody] DeleteUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (userId == Guid.Empty)
                return BadRequest(new { error = "Invalid user ID" });

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { error = "Reason is required" });

            var adminId = GetAdminIdFromToken();
            
            _logger.LogInformation("Admin {AdminId} requesting deletion of user {UserId} for reason: {Reason}", 
                adminId, userId, request.Reason);

            var approvalRequest = await _adminUserService.CreateDeleteApprovalAsync(userId, request.Reason, adminId);

            return Accepted($"/api/admin/approvals/{approvalRequest.Id}", approvalRequest);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid deletion request for user {UserId}", userId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting user deletion for {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to request user deletion" });
        }
    }

    /// <summary>
    /// Extract admin ID from JWT token claims
    /// </summary>
    private Guid GetAdminIdFromToken()
    {
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim?.Value == null || !Guid.TryParse(userIdClaim.Value, out var adminId))
            throw new InvalidOperationException("Unable to extract admin ID from token");

        return adminId;
    }
}
