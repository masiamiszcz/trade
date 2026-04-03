namespace TradingPlatform.Core.Models;

public sealed record LoginRequest(
    string UserNameOrEmail,
    string Password);
