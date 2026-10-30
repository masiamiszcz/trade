using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Data.UnitOfWork;

namespace TradingPlatform.Data.Services
{
    public class PortfolioTransactionService : IPortfolioTransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBinanceWebSocketService _priceService;
        private readonly IExchangeService _exchangeService;
        private readonly ILogger<PortfolioTransactionService> _logger;

        public PortfolioTransactionService(
            IUnitOfWork unitOfWork, 
            IBinanceWebSocketService priceService,
            IExchangeService exchangeService,
            ILogger<PortfolioTransactionService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
            _exchangeService = exchangeService ?? throw new ArgumentNullException(nameof(exchangeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<CryptoTransactionDto>> ExecuteBuyAsync(Guid accountId, string symbol, decimal quantity, decimal requestedPrice, decimal tolerancePercent, CancellationToken cancellationToken)
        {
            if (quantity <= 0) return Result<CryptoTransactionDto>.Failure("InvalidQuantity", "Quantity must be greater than zero");
            if (requestedPrice <= 0) return Result<CryptoTransactionDto>.Failure("InvalidPrice", "Requested price must be greater than zero");

            var executionPriceNullable = await _priceService.GetLatestPriceAsync(symbol, cancellationToken);
            if (!executionPriceNullable.HasValue || executionPriceNullable.Value <= 0)
                return Result<CryptoTransactionDto>.Failure("PriceUnavailable", "Could not obtain execution price");

            var executionPrice = executionPriceNullable.Value;
            if (!ValidatePriceTolerance(requestedPrice, executionPrice))
                return Result<CryptoTransactionDto>.Failure("PriceTolerance", $"Execution price differs by more than 0.01%. Requested: {requestedPrice}, Current: {executionPrice}");

            return await ExecuteBuyAsyncImpl(accountId, symbol, quantity, requestedPrice, executionPrice, cancellationToken);
        }

        private async Task<Result<CryptoTransactionDto>> ExecuteBuyAsyncImpl(Guid accountId, string symbol, decimal quantity, decimal requestedPrice, decimal executionPrice, CancellationToken cancellationToken)
        {
            var txId = Guid.NewGuid();
            var totalValue = Math.Round(quantity * executionPrice, 8);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
                
                if (account == null)
                {
                    _logger.LogError("❌ Account NOT FOUND for ID: {AccountId}", accountId);
                    return Result<CryptoTransactionDto>.Failure("AccountNotFound", "Account not found");
                }

                if (account.AvailableBalance < totalValue)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result<CryptoTransactionDto>.Failure("InsufficientFunds", "Not enough available balance");
                }

                var amountInAccountCurrency = await _exchangeService.ConvertCurrencyAsync("USD", account.Currency, totalValue);

                var instrument = await _unitOfWork.Instruments.GetBySymbolAsync(
                    symbol,
                    InstrumentType.Crypto,
                    cancellationToken);
                if (instrument == null)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result<CryptoTransactionDto>.Failure("InstrumentNotFound", $"Instrument '{symbol}' not found. Please contact admin to add this instrument.");
                }
                var instrumentId = instrument.Id;

                var completedTx = new Core.Interfaces.CryptoTransaction(
                    txId,
                    account.UserId,
                    accountId,
                    instrumentId,
                    symbol,
                    TransactionType.Buy.ToString(),
                    quantity,
                    requestedPrice,
                    executionPrice,
                    totalValue,
                    TransactionStatus.Completed.ToString(),
                    "USD",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null
                );

                await _unitOfWork.CryptoTransactions.AddAsync(completedTx, cancellationToken);

                var updatedAccount = account with { AvailableBalance = account.AvailableBalance - amountInAccountCurrency };
                await _unitOfWork.Accounts.UpdateAsync(updatedAccount, cancellationToken);

                var position = await _unitOfWork.Positions.GetByUserAndSymbolAsync(account.UserId, symbol, cancellationToken);
                if (position == null)
                {
                    var newPosition = new Core.Interfaces.Position(
                        Guid.NewGuid(),
                        account.UserId,
                        accountId,
                        instrumentId,
                        symbol,
                        quantity,
                        0m,
                        executionPrice,
                        DateTimeOffset.UtcNow,
                        DateTimeOffset.UtcNow
                    );

                    await _unitOfWork.Positions.AddAsync(newPosition, cancellationToken);
                }
                else
                {
                    var newQty = position.Quantity + quantity;
                    var avg = (position.AverageOpenPrice * position.Quantity + executionPrice * quantity) / newQty;
                    var updatedPosition = position with { Quantity = newQty, AverageOpenPrice = avg, LastUpdatedAt = DateTimeOffset.UtcNow };
                    await _unitOfWork.Positions.UpdateAsync(updatedPosition, cancellationToken);
                }

                await _unitOfWork.CommitAsync(cancellationToken);

                var dto = new CryptoTransactionDto(
                    completedTx.Id,
                    completedTx.Symbol,
                    completedTx.Type,
                    completedTx.Quantity,
                    completedTx.RequestedPrice,
                    completedTx.ExecutionPrice,
                    completedTx.TotalValue,
                    completedTx.Status,
                    completedTx.Currency,
                    completedTx.CreatedAt,
                    completedTx.CompletedAt,
                    completedTx.ErrorReason);

                return Result<CryptoTransactionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                try { await _unitOfWork.RollbackAsync(cancellationToken); } catch { }
                return Result<CryptoTransactionDto>.Failure("TransactionError", ex.Message);
            }
        }

        public async Task<Result<CryptoTransactionDto>> ExecuteSellAsync(Guid accountId, string symbol, decimal quantity, decimal requestedPrice, decimal tolerancePercent, CancellationToken cancellationToken)
        {
            if (quantity <= 0) return Result<CryptoTransactionDto>.Failure("InvalidQuantity", "Quantity must be greater than zero");
            if (requestedPrice <= 0) return Result<CryptoTransactionDto>.Failure("InvalidPrice", "Requested price must be greater than zero");

            var executionPriceNullable = await _priceService.GetLatestPriceAsync(symbol, cancellationToken);
            if (!executionPriceNullable.HasValue || executionPriceNullable.Value <= 0)
                return Result<CryptoTransactionDto>.Failure("PriceUnavailable", "Could not obtain execution price");

            var executionPrice = executionPriceNullable.Value;
            if (!ValidatePriceTolerance(requestedPrice, executionPrice))
                return Result<CryptoTransactionDto>.Failure("PriceTolerance", $"Execution price differs by more than 0.01%. Requested: {requestedPrice}, Current: {executionPrice}");

            return await ExecuteSellAsyncImpl(accountId, symbol, quantity, requestedPrice, executionPrice, cancellationToken);
        }

        private async Task<Result<CryptoTransactionDto>> ExecuteSellAsyncImpl(Guid accountId, string symbol, decimal quantity, decimal requestedPrice, decimal executionPrice, CancellationToken cancellationToken)
        {
            var txId = Guid.NewGuid();
            var totalValue = Math.Round(quantity * executionPrice, 8);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, cancellationToken);
                
                if (account == null)
                {
                    _logger.LogError("❌ Account NOT FOUND for ID: {AccountId}", accountId);
                    return Result<CryptoTransactionDto>.Failure("AccountNotFound", "Account not found");
                }

                var position = await _unitOfWork.Positions.GetByUserAndSymbolAsync(account.UserId, symbol, cancellationToken);
                if (position == null || position.Quantity < quantity)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result<CryptoTransactionDto>.Failure("InsufficientPosition", "Not enough quantity to sell");
                }

                var amountInAccountCurrency = await _exchangeService.ConvertCurrencyAsync("USD", account.Currency, totalValue);

                var instrument = await _unitOfWork.Instruments.GetBySymbolAsync(
                    symbol,
                    InstrumentType.Crypto,
                    cancellationToken);
                if (instrument == null)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    return Result<CryptoTransactionDto>.Failure("InstrumentNotFound", $"Instrument '{symbol}' not found. Please contact admin to add this instrument.");
                }
                var instrumentId = instrument.Id;

                var completedTx = new Core.Interfaces.CryptoTransaction(
                    txId,
                    account.UserId,
                    accountId,
                    instrumentId,
                    symbol,
                    TransactionType.Sell.ToString(),
                    quantity,
                    requestedPrice,
                    executionPrice,
                    totalValue,
                    TransactionStatus.Completed.ToString(),
                    "USD",
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow,
                    null
                );

                await _unitOfWork.CryptoTransactions.AddAsync(completedTx, cancellationToken);

                var newQty = position.Quantity - quantity;
                if (newQty == 0)
                {
                    var zeroPos = position with { Quantity = 0m, ReservedQuantity = 0m, LastUpdatedAt = DateTimeOffset.UtcNow };
                    await _unitOfWork.Positions.UpdateAsync(zeroPos, cancellationToken);
                }
                else
                {
                    var updatedPosition = position with { Quantity = newQty, LastUpdatedAt = DateTimeOffset.UtcNow };
                    await _unitOfWork.Positions.UpdateAsync(updatedPosition, cancellationToken);
                }

                var updatedAccount = account with { AvailableBalance = account.AvailableBalance + amountInAccountCurrency };
                await _unitOfWork.Accounts.UpdateAsync(updatedAccount, cancellationToken);

                await _unitOfWork.CommitAsync(cancellationToken);

                var dto = new CryptoTransactionDto(
                    completedTx.Id,
                    completedTx.Symbol,
                    completedTx.Type,
                    completedTx.Quantity,
                    completedTx.RequestedPrice,
                    completedTx.ExecutionPrice,
                    completedTx.TotalValue,
                    completedTx.Status,
                    completedTx.Currency,
                    completedTx.CreatedAt,
                    completedTx.CompletedAt,
                    completedTx.ErrorReason);

                return Result<CryptoTransactionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                try { await _unitOfWork.RollbackAsync(cancellationToken); } catch { }
                return Result<CryptoTransactionDto>.Failure("TransactionError", ex.Message);
            }
        }

        public async Task<Result<PortfolioSummaryDto>> GetPortfolioAsync(Guid userId, CancellationToken cancellationToken)
        {
            var positions = (await _unitOfWork.Positions.GetByUserIdAsync(userId, cancellationToken)).ToList();
            var items = new List<PortfolioPositionDto>();

            foreach (var p in positions)
            {
                var priceNullable = await _priceService.GetLatestPriceAsync(p.Symbol, cancellationToken);
                var currentPrice = priceNullable ?? p.AverageOpenPrice;
                var marketValue = currentPrice * p.Quantity;
                var pnl = (currentPrice - p.AverageOpenPrice) * p.Quantity;
                var pnlPercent = p.AverageOpenPrice == 0m ? 0m : (currentPrice - p.AverageOpenPrice) / p.AverageOpenPrice * 100m;

                items.Add(new PortfolioPositionDto(p.Symbol, p.Quantity, p.AverageOpenPrice, currentPrice, marketValue, pnl, pnlPercent));
            }

            var totalValue = items.Sum(i => i.TotalValue);
            var totalPnL = items.Sum(i => i.PnL);
            var costBasis = items.Sum(i => i.AverageOpenPrice * i.Quantity);
            var totalPnLPercent = costBasis == 0m ? 0m : totalPnL / costBasis * 100m;

            var summary = new PortfolioSummaryDto("USD", items, new PortfolioAggregateDto(totalValue, totalPnL, totalPnLPercent));

            return Result<PortfolioSummaryDto>.Success(summary);
        }

        public async Task<Result<PaginatedResult<CryptoTransactionDto>>> GetTransactionsAsync(Guid userId, int limit, int offset, string? type, string? status, CancellationToken cancellationToken)
        {
            var txs = (await _unitOfWork.CryptoTransactions.GetByUserIdAsync(userId, limit, offset, cancellationToken)).ToList();

            if (!string.IsNullOrWhiteSpace(type)) txs = txs.Where(t => t.Type == type).ToList();
            if (!string.IsNullOrWhiteSpace(status)) txs = txs.Where(t => t.Status == status).ToList();

            var items = txs.Select(t => new CryptoTransactionDto(
                t.Id,
                t.Symbol,
                t.Type,
                t.Quantity,
                t.RequestedPrice,
                t.ExecutionPrice,
                t.TotalValue,
                t.Status,
                t.Currency,
                t.CreatedAt,
                t.CompletedAt,
                t.ErrorReason)).ToList();

            var result = new PaginatedResult<CryptoTransactionDto>(items, items.Count, limit, offset);
            return Result<PaginatedResult<CryptoTransactionDto>>.Success(result);
        }

        private bool ValidatePriceTolerance(decimal requestedPrice, decimal executionPrice)
        {
            const decimal TOLERANCE = 0.0001m;  
            
            var diff = Math.Abs(requestedPrice - executionPrice);
            var relDiff = executionPrice > 0 ? diff / executionPrice : diff;
            
            return relDiff <= TOLERANCE;
        }


    }
}
