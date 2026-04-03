namespace TradingPlatform.Core.Dtos;

public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    string Status,
    DateTimeOffset CreatedAtUtc);
