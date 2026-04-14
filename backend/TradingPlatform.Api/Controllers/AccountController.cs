using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IMapper _mapper;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, IMapper mapper, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get user's main account with balance
    /// </summary>
    [HttpGet("main")]
    public async Task<IActionResult> GetMainAccount(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid or missing user ID in token." });
            }

            var account = await _accountService.GetMainAccountByUserIdAsync(userId, cancellationToken);
            if (account is null)
            {
                return NotFound(new { message = "Main account not found." });
            }

            var accountDto = _mapper.Map<AccountDto>(account);
            return Ok(accountDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving main account");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the account." });
        }
    }

    /// <summary>
    /// Get all user accounts
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAccounts(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserIdFromToken();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new { message = "Invalid or missing user ID in token." });
            }

            var accounts = await _accountService.GetUserAccountsAsync(userId, cancellationToken);
            var accountDtos = _mapper.Map<IEnumerable<AccountDto>>(accounts);
            return Ok(accountDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving accounts." });
        }
    }

    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }
}
