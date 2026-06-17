using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Core trading service for buy/sell crypto operations
/// Orchestrates account balance updates, position management, and transaction records
/// </summary>
public interface IPortfolioTransactionService
{
    /// <summary>
    /// Execute a buy crypto transaction
    /// 1. Validates account balance
    /// 2. Fetches real-time price from WebSocket
    /// 3. Validates price tolerance
    /// 4. Atomically updates Account balance and creates/updates Position
    /// 5. Records transaction in audit trail
    /// </summary>
    /// <returns>CryptoTransactionDto on success, or with error details on failure</returns>
    Task<Result<CryptoTransactionDto>> ExecuteBuyAsync(
        Guid accountId,
        string symbol,
        decimal quantity,
        decimal requestedPrice,
        decimal tolerancePercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a sell crypto transaction
    /// 1. Validates position exists and has sufficient quantity
    /// 2. Fetches real-time price from WebSocket
    /// 3. Validates price tolerance
    /// 4. Atomically updates Position quantity and Account balance
    /// 5. Records transaction in audit trail
    /// </summary>
    /// <returns>CryptoTransactionDto on success, or with error details on failure</returns>
    Task<Result<CryptoTransactionDto>> ExecuteSellAsync(
        Guid accountId,
        string symbol,
        decimal quantity,
        decimal requestedPrice,
        decimal tolerancePercent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's complete portfolio with positions and aggregate P&L
    /// </summary>
    Task<Result<PortfolioSummaryDto>> GetPortfolioAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transaction history with pagination and filtering
    /// </summary>
    Task<Result<PaginatedResult<CryptoTransactionDto>>> GetTransactionsAsync(
        Guid userId,
        int limit = 50,
        int offset = 0,
        string? type = null,
        string? status = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Generic Result wrapper for operation outcomes
/// </summary>
public sealed record Result<T>(
    bool IsSuccess,
    T? Data = default,
    string? ErrorCode = null,
    string? ErrorMessage = null)
{
    public static Result<T> Success(T data) =>
        new(true, data);

    public static Result<T> Failure(string errorCode, string errorMessage) =>
        new(false, default, errorCode, errorMessage);
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int Total,
    int Limit,
    int Offset);
