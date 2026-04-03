using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var userDto = await _userService.RegisterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Register), new { id = userDto.Id }, userDto);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var token = await _userService.LoginAsync(request, cancellationToken);
        return Ok(new { token });
    }
}
