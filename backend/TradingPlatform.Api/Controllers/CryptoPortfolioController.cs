using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Api.Controllers
{
    /// <summary>
    /// Crypto Portfolio Management Controller
    /// Handles buy/sell operations and portfolio queries
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CryptoPortfolioController : ControllerBase
    {
        private readonly IPortfolioTransactionService _transactionService;
        private readonly IAccountService _accountService;
        private readonly ILogger<CryptoPortfolioController> _logger;

        public CryptoPortfolioController(
            IPortfolioTransactionService transactionService,
            IAccountService accountService,
            ILogger<CryptoPortfolioController> logger)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute a buy transaction
        /// POST /api/cryptoportfolio/buy
        /// </summary>
        /// <param name="request">Buy request with symbol, quantity, price, tolerance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completed transaction details</returns>
        /// <response code="200">Transaction completed successfully</response>
        /// <response code="400">Validation failed (insufficient balance, price tolerance, etc.)</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Server error</response>
        [HttpPost("buy")]
        [ProducesResponseType(typeof(CryptoTransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BuyAsync(
            [FromBody] BuyCryptoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                return BadRequest(new ErrorResponse("InvalidRequest", "Request body cannot be null"));

            // Validate request parameters
            if (string.IsNullOrWhiteSpace(request.Symbol))
                return BadRequest(new ErrorResponse("InvalidSymbol", "Symbol cannot be empty"));

            if (request.Quantity <= 0)
                return BadRequest(new ErrorResponse("InvalidQuantity", "Quantity must be greater than zero"));

            if (request.RequestedPrice <= 0)
                return BadRequest(new ErrorResponse("InvalidPrice", "Requested price must be greater than zero"));

            if (request.TolerancePercent < 0)
                return BadRequest(new ErrorResponse("InvalidTolerance", "Tolerance percent cannot be negative"));

            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("oid")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ErrorResponse("InvalidUser", "User ID not found in token"));
            }

            var account = await _accountService.GetMainAccountByUserIdAsync(userId, cancellationToken);
            if (account == null)
            {
                return NotFound(new ErrorResponse("AccountNotFound", "Main account not found"));
            }

            var result = await _transactionService.ExecuteBuyAsync(
                account.Id,
                request.Symbol.ToUpper(),
                request.Quantity,
                request.RequestedPrice,
                request.TolerancePercent,
                cancellationToken);

            if (!result.IsSuccess)
            {
                // Map known error codes to appropriate HTTP status codes
                var errorCode = result.ErrorCode ?? "UnknownError";
                var errorMessage = result.ErrorMessage ?? "An unexpected error occurred";
                return errorCode switch
                {
                    "InsufficientFunds" => BadRequest(new ErrorResponse(errorCode, errorMessage)),
                    "PriceTolerance" => BadRequest(new ErrorResponse(errorCode, errorMessage)),
                    "PriceUnavailable" => BadRequest(new ErrorResponse(errorCode, errorMessage)),
                    "AccountNotFound" => NotFound(new ErrorResponse(errorCode, errorMessage)),
                    _ => StatusCode(500, new ErrorResponse(errorCode, errorMessage))
                };
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Execute a sell transaction
        /// POST /api/cryptoportfolio/sell
        /// </summary>
        /// <param name="request">Sell request with symbol, quantity, price, tolerance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Completed transaction details</returns>
        /// <response code="200">Transaction completed successfully</response>
        /// <response code="400">Validation failed (insufficient position, price tolerance, etc.)</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Server error</response>
        [HttpPost("sell")]
        [ProducesResponseType(typeof(CryptoTransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SellAsync(
            [FromBody] SellCryptoRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                return BadRequest(new ErrorResponse("InvalidRequest", "Request body cannot be null"));

            // Validate request parameters
            if (string.IsNullOrWhiteSpace(request.Symbol))
                return BadRequest(new ErrorResponse("InvalidSymbol", "Symbol cannot be empty"));

            if (request.Quantity <= 0)
                return BadRequest(new ErrorResponse("InvalidQuantity", "Quantity must be greater than zero"));

            if (request.RequestedPrice <= 0)
                return BadRequest(new ErrorResponse("InvalidPrice", "Requested price must be greater than zero"));

            if (request.TolerancePercent < 0)
                return BadRequest(new ErrorResponse("InvalidTolerance", "Tolerance percent cannot be negative"));

            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("oid")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new ErrorResponse("InvalidUser", "User ID not found in token"));
            }

            var account = await _accountService.GetMainAccountByUserIdAsync(userId, cancellationToken);
            if (account == null)
            {
                return NotFound(new ErrorResponse("AccountNotFound", "Main account not found"));
            }

            var result = await _transactionService.ExecuteSellAsync(
                account.Id,
                request.Symbol.ToUpper(),
                request.Quantity,
                request.RequestedPrice,
                request.TolerancePercent,
                cancellationToken);

            if (!result.IsSuccess)
            {
                var errorCode = result.ErrorCode ?? "UnknownError";
                var errorMessage = result.ErrorMessage ?? "An unexpected error occurred";
                return errorCode switch
                {
                    "InsufficientPosition" => BadRequest(new ErrorResponse(errorCode, errorMessage)),
                    "PriceTolerance" => BadRequest(new ErrorResponse(errorCode, errorMessage)),
                    "PriceUnavailable" => BadRequest(new ErrorResponse(errorCode, errorMessage)),
                    "AccountNotFound" => NotFound(new ErrorResponse(errorCode, errorMessage)),
                    _ => StatusCode(500, new ErrorResponse(errorCode, errorMessage))
                };
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Get user's portfolio with current positions and P&L
        /// GET /api/cryptoportfolio/portfolio
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Portfolio summary with all positions</returns>
        /// <response code="200">Portfolio retrieved successfully</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Server error</response>
        [HttpGet("portfolio")]
        [ProducesResponseType(typeof(PortfolioSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPortfolioAsync(CancellationToken cancellationToken = default)
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("oid")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse("InvalidUser", "User ID not found in token"));

            var result = await _transactionService.GetPortfolioAsync(userId, cancellationToken);

            if (!result.IsSuccess)
            {
                var errorCode = result.ErrorCode ?? "UnknownError";
                var errorMessage = result.ErrorMessage ?? "An unexpected error occurred";
                return errorCode switch
                {
                    "AccountNotFound" => NotFound(new ErrorResponse(errorCode, errorMessage)),
                    _ => StatusCode(500, new ErrorResponse(errorCode, errorMessage))
                };
            }

            return Ok(result.Data);
        }

        /// <summary>
        /// Get paginated transaction history
        /// GET /api/cryptoportfolio/transactions?limit=50&amp;offset=0&amp;type=Buy&amp;status=Completed
        /// </summary>
        /// <param name="limit">Number of transactions per page (default 50, max 500)</param>
        /// <param name="offset">Pagination offset (default 0)</param>
        /// <param name="type">Filter by transaction type: 'Buy' or 'Sell' (optional)</param>
        /// <param name="status">Filter by status: 'Completed', 'Pending', 'Rejected', 'Cancelled' (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of transactions</returns>
        /// <response code="200">Transactions retrieved successfully</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Server error</response>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(TransactionHistoryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransactionsAsync(
            [FromQuery] int limit = 50,
            [FromQuery] int offset = 0,
            [FromQuery] string? type = null,
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            // Validate pagination parameters
            if (limit <= 0 || limit > 500)
                return BadRequest(new ErrorResponse("InvalidLimit", "Limit must be between 1 and 500"));

            if (offset < 0)
                return BadRequest(new ErrorResponse("InvalidOffset", "Offset must be non-negative"));

            // Validate optional filter parameters
            if (!string.IsNullOrWhiteSpace(type) && type != "Buy" && type != "Sell")
                return BadRequest(new ErrorResponse("InvalidType", "Type must be 'Buy' or 'Sell'"));

            if (!string.IsNullOrWhiteSpace(status) && 
                status != "Completed" && status != "Pending" && status != "Rejected" && status != "Cancelled")
                return BadRequest(new ErrorResponse("InvalidStatus", "Status must be 'Completed', 'Pending', 'Rejected', or 'Cancelled'"));

            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("oid")?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ErrorResponse("InvalidUser", "User ID not found in token"));

            var result = await _transactionService.GetTransactionsAsync(
                userId,
                limit,
                offset,
                type,
                status,
                cancellationToken);

            if (!result.IsSuccess)
            {
                var errorCode = result.ErrorCode ?? "UnknownError";
                var errorMessage = result.ErrorMessage ?? "An unexpected error occurred";
                return errorCode switch
                {
                    "AccountNotFound" => NotFound(new ErrorResponse(errorCode, errorMessage)),
                    _ => StatusCode(500, new ErrorResponse(errorCode, errorMessage))
                };
            }

            // Map to response
            var response = new TransactionHistoryResponse(
                result.Data.Total,
                result.Data.Limit,
                result.Data.Offset,
                result.Data.Items);

            return Ok(response);
        }
    }

    /// <summary>
    /// Standard error response format
    /// </summary>
    public class ErrorResponse
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public DateTimeOffset Timestamp { get; set; }

        public ErrorResponse(string errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            Timestamp = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Transaction history response with pagination metadata
    /// </summary>
    public class TransactionHistoryResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("total")]
        public int Total { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("limit")]
        public int Limit { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("offset")]
        public int Offset { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("transactions")]
        public IReadOnlyList<CryptoTransactionDto> Transactions { get; set; }

        public TransactionHistoryResponse(int total, int limit, int offset, IReadOnlyList<CryptoTransactionDto> transactions)
        {
            Total = total;
            Limit = limit;
            Offset = offset;
            Transactions = transactions;
        }
    }
}
