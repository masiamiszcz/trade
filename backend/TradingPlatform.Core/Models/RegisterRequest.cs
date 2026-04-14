namespace TradingPlatform.Core.Models;

public sealed record RegisterRequest(
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string BaseCurrency = "PLN");
