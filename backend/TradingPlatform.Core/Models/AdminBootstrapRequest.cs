
using System.ComponentModel.DataAnnotations;

namespace TradingPlatform.Core.Models;

/// <summary>
/// Request model for one-time Super Admin bootstrap
/// This is the ONLY endpoint that doesn't require authentication
/// </summary>
public class AdminBootstrapRequest
{
    /// <summary>
    /// Super Admin username (3-50 characters)
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Super Admin email address
    /// </summary>
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Super Admin password (minimum 8 characters, must contain uppercase, lowercase, digits, special chars)
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(256, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 256 characters")]
    public string Password { get; set; } = string.Empty;
}
