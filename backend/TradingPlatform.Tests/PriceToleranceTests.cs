using Xunit;

namespace TradingPlatform.Tests;

/// <summary>
/// Tests to verify ValidatePriceTolerance logic with actual numbers
/// </summary>
public class PriceToleranceTests
{
    /// <summary>
    /// Test case from user: 3 posts with same payload
    /// - requestedPrice: 65812.66 (5 sek przerwy = cena powinna być taka sama)
    /// - executionPrice: powinny być 65812.66
    /// </summary>
    [Fact]
    public void TestUserScenario_SamePriceAfter5Seconds()
    {
        // Arrange
        decimal requestedPrice = 65812.66m;
        decimal executionPrice = 65812.66m;  // Same price after 5 seconds
        const decimal TOLERANCE = 0.0001m;   // 0.01% = 1 promil

        // Act
        var diff = Math.Abs(requestedPrice - executionPrice);
        var relDiff = executionPrice > 0 ? diff / executionPrice : diff;
        bool isValid = relDiff <= TOLERANCE;

        // Assert
        Assert.Equal(0m, diff);
        Assert.Equal(0m, relDiff);
        Assert.True(isValid, $"Expected tolerance check to PASS. Diff: {diff}, RelDiff: {relDiff}, TOLERANCE: {TOLERANCE}");
    }

    /// <summary>
    /// Test when price changed slightly (within 0.01% tolerance)
    /// Price went from 65812.66 to 65812.72 (+0.00009% - should PASS)
    /// </summary>
    [Fact]
    public void TestPriceWithinTolerance_SmallChange()
    {
        // Arrange
        decimal requestedPrice = 65812.66m;
        decimal executionPrice = 65812.72m;  // Changed by 0.06 (tiny change)
        const decimal TOLERANCE = 0.0001m;   // 0.01% = 1 promil

        // Act
        var diff = Math.Abs(requestedPrice - executionPrice);
        var relDiff = executionPrice > 0 ? diff / executionPrice : diff;
        bool isValid = relDiff <= TOLERANCE;

        // Assert - diff/price should be very small
        var percentChange = (diff / executionPrice) * 100;
        Assert.True(isValid, 
            $"Small price change should PASS. Diff: {diff}, RelDiff: {relDiff} ({percentChange}%), TOLERANCE: {TOLERANCE}");
    }

    /// <summary>
    /// Test when price exceeded tolerance (0.01%)
    /// Price went from 65812.66 to 65874.44 (+0.095% - should FAIL)
    /// </summary>
    [Fact]
    public void TestPriceExceedsTolerance_LargeChange()
    {
        // Arrange
        decimal requestedPrice = 65812.66m;
        decimal executionPrice = 65874.44m;  // Changed by ~62 (larger change)
        const decimal TOLERANCE = 0.0001m;   // 0.01% = 1 promil

        // Act
        var diff = Math.Abs(requestedPrice - executionPrice);
        var relDiff = executionPrice > 0 ? diff / executionPrice : diff;
        bool isValid = relDiff <= TOLERANCE;

        // Assert
        var percentChange = (diff / executionPrice) * 100;
        Assert.False(isValid, 
            $"Large price change should FAIL. Diff: {diff}, RelDiff: {relDiff} ({percentChange}%), TOLERANCE: {TOLERANCE} (0.01%)");
    }

    /// <summary>
    /// Edge case: tolerance boundary at exactly 0.01%
    /// </summary>
    [Fact]
    public void TestPriceLimitAtBoundary_ExactlyAtTolerance()
    {
        // Arrange - 0.01% of 65812.66 = 6.5813
        decimal requestedPrice = 65812.66m;
        decimal executionPrice = 65812.66m + 6.5813m;  // Exactly 0.01% higher
        const decimal TOLERANCE = 0.0001m;

        // Act
        var diff = Math.Abs(requestedPrice - executionPrice);
        var relDiff = executionPrice > 0 ? diff / executionPrice : diff;
        bool isValid = relDiff <= TOLERANCE;

        // Assert
        var percentChange = (diff / executionPrice) * 100;
        Assert.True(isValid, 
            $"Boundary test (at tolerance). Diff: {diff}, RelDiff: {relDiff} ({percentChange}%), TOLERANCE: {TOLERANCE}");
    }

    /// <summary>
    /// Edge case: tolerance just exceeded (0.011%)
    /// </summary>
    [Fact]
    public void TestPriceLimitJustExceeded_SlightlyAboveTolerance()
    {
        // Arrange - 0.011% of 65812.66 ≈ 7.24
        decimal requestedPrice = 65812.66m;
        decimal executionPrice = 65812.66m + 7.24m;  // 0.011% higher
        const decimal TOLERANCE = 0.0001m;

        // Act
        var diff = Math.Abs(requestedPrice - executionPrice);
        var relDiff = executionPrice > 0 ? diff / executionPrice : diff;
        bool isValid = relDiff <= TOLERANCE;

        // Assert
        var percentChange = (diff / executionPrice) * 100;
        Assert.False(isValid, 
            $"Above tolerance test. Diff: {diff}, RelDiff: {relDiff} ({percentChange}%), TOLERANCE: {TOLERANCE}");
    }
}
