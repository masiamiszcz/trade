

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
    /// Get all users (ADMIN only)
    /// Query param: status (Active, Blocked, Deleted, or null for all)
    /// </summary>
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserListItemDto>>> GetAllUsers(
        [FromQuery] Core.Enums.UserStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Admin requesting users with status filter: {Status}", status?.ToString() ?? "All");

            var users = await _adminUserService.GetUsersAsync(status, cancellationToken);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Failed to retrieve users" });
        }
    }
}