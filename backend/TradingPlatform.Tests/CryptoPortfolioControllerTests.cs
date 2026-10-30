using Moq;
using TradingPlatform.Api.Controllers;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace TradingPlatform.Tests;

public class CryptoPortfolioControllerTests
{
    private readonly Mock<IPortfolioTransactionService> _mockService;
    private readonly CryptoPortfolioController _controller;
    private readonly Guid _testAccountId;

    public CryptoPortfolioControllerTests()
    {
        _mockService = new Mock<IPortfolioTransactionService>();
        _testAccountId = Guid.NewGuid();
        var accountService = new Mock<IAccountService>();
        accountService
            .Setup(service => service.GetMainAccountByUserIdAsync(
                _testAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account(
                _testAccountId,
                _testAccountId,
                null,
                "TEST",
                "Test account",
                AccountType.Main,
                AccountPillar.Crypto,
                AccountStatus.Active,
                "USD",
                10000m,
                0m,
                DateTimeOffset.UtcNow));
        _controller = new CryptoPortfolioController(
            _mockService.Object,
            accountService.Object,
            Mock.Of<Microsoft.Extensions.Logging.ILogger<CryptoPortfolioController>>());

        // Setup JWT claims context
        var claims = new List<Claim> { new("sub", _testAccountId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task BuyAsync_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var request = new BuyCryptoRequest("BTCUSDT", 0.5m, 42500m, 1.0m);
        var transaction = new CryptoTransactionDto(
            Guid.NewGuid(), "BTCUSDT", "Buy", 0.5m, 42500m, 42510m, 21255m,
            "Completed", "USD", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);

        _mockService.Setup(x => x.ExecuteBuyAsync(
            _testAccountId, "BTCUSDT", 0.5m, 42500m, 1.0m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CryptoTransactionDto>.Success(transaction));

        // Act
        var response = await _controller.BuyAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task BuyAsync_WithNullRequest_Returns400BadRequest()
    {
        // Act & Assert
        var response = await _controller.BuyAsync(null, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task BuyAsync_WithEmptySymbol_Returns400BadRequest()
    {
        // Arrange
        var request = new BuyCryptoRequest("", 0.5m, 42500m, 1.0m);

        // Act
        var response = await _controller.BuyAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task BuyAsync_WithNegativeQuantity_Returns400BadRequest()
    {
        // Arrange
        var request = new BuyCryptoRequest("BTCUSDT", -0.5m, 42500m, 1.0m);

        // Act
        var response = await _controller.BuyAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task BuyAsync_WithInsufficientFunds_Returns400BadRequest()
    {
        // Arrange
        var request = new BuyCryptoRequest("BTCUSDT", 10m, 50000m, 1.0m);

        _mockService.Setup(x => x.ExecuteBuyAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(), 
            It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CryptoTransactionDto>.Failure("InsufficientFunds", "Not enough balance"));

        // Act
        var response = await _controller.BuyAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task SellAsync_WithValidRequest_Returns200Ok()
    {
        // Arrange
        var request = new SellCryptoRequest("BTCUSDT", 0.5m, 42500m, 1.0m);
        var transaction = new CryptoTransactionDto(
            Guid.NewGuid(), "BTCUSDT", "Sell", 0.5m, 42500m, 42490m, 21245m,
            "Completed", "USD", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null);

        _mockService.Setup(x => x.ExecuteSellAsync(
            _testAccountId, "BTCUSDT", 0.5m, 42500m, 1.0m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<CryptoTransactionDto>.Success(transaction));

        // Act
        var response = await _controller.SellAsync(request, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetPortfolioAsync_ReturnsPortfolioSummary()
    {
        // Arrange
        var positions = new List<PortfolioPositionDto>
        {
            new("BTCUSDT", 0.5m, 42500m, 42500m, 21250m, 100m, 0.47m)
        };
        var aggregate = new PortfolioAggregateDto(21250m, 100m, 0.47m);
        var summary = new PortfolioSummaryDto("USD", positions, aggregate);

        _mockService.Setup(x => x.GetPortfolioAsync(_testAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PortfolioSummaryDto>.Success(summary));

        // Act
        var response = await _controller.GetPortfolioAsync(CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithValidPagination_Returns200Ok()
    {
        // Arrange
        var transactions = new List<CryptoTransactionDto>
        {
            new(Guid.NewGuid(), "BTCUSDT", "Buy", 0.5m, 42500m, 42510m, 21255m,
                "Completed", "USD", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null)
        };
        var paginated = new PaginatedResult<CryptoTransactionDto>(transactions, 1, 10, 0);

        _mockService.Setup(x => x.GetTransactionsAsync(
            _testAccountId, 10, 0, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PaginatedResult<CryptoTransactionDto>>.Success(paginated));

        // Act
        var response = await _controller.GetTransactionsAsync(10, 0, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(response);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithInvalidLimit_Returns400BadRequest()
    {
        // Act
        var response = await _controller.GetTransactionsAsync(600, 0, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task GetTransactionsAsync_WithNegativeOffset_Returns400BadRequest()
    {
        // Act
        var response = await _controller.GetTransactionsAsync(10, -1, null, null, CancellationToken.None);

        // Assert
        Assert.IsType<BadRequestObjectResult>(response);
    }
}
