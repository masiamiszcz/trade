namespace TradingPlatform.Core.Dtos;

/// <summary>
/// Request to block a user
/// </summary>
public sealed record BlockUserRequest(
    string Reason,
    long DurationMs,
    bool IsPermanent);

/// <summary>
/// Request to unblock a user
/// </summary>
public sealed record UnblockUserRequest(
    string Reason);

/// <summary>
/// Request to delete a user (creates approval workflow)
/// </summary>
public sealed record DeleteUserRequest(
    string Reason);
