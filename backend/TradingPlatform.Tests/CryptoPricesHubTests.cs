using Microsoft.AspNetCore.SignalR;
using Moq;
using TradingPlatform.Api.Hubs;
using TradingPlatform.Core.Interfaces;

namespace TradingPlatform.Tests;

public sealed class CryptoPricesHubTests
{
    [Fact]
    public async Task SubscribeToSymbols_NormalizesAndDeduplicatesSymbols()
    {
        var fixture = new HubFixture();

        await fixture.Hub.SubscribeToSymbols([" btcusdt ", "BTCUSDT", "ethusdt"], 7 * 24 * 60);

        fixture.Groups.Verify(groups => groups.AddToGroupAsync(
            fixture.ConnectionId, "BTCUSDT", It.IsAny<CancellationToken>()), Times.Once);
        fixture.Groups.Verify(groups => groups.AddToGroupAsync(
            fixture.ConnectionId, "BTCUSDT:5m", It.IsAny<CancellationToken>()), Times.Once);
        fixture.Groups.Verify(groups => groups.AddToGroupAsync(
            fixture.ConnectionId, "ETHUSDT", It.IsAny<CancellationToken>()), Times.Once);
        fixture.Groups.Verify(groups => groups.AddToGroupAsync(
            fixture.ConnectionId, "ETHUSDT:5m", It.IsAny<CancellationToken>()), Times.Once);
        fixture.StreamActivation.Verify(service => service.InitializeActiveIntervalAsync(
            "BTCUSDT", 5, It.IsAny<CancellationToken>()), Times.Once);
        fixture.StreamActivation.Verify(service => service.InitializeActiveIntervalAsync(
            "ETHUSDT", 5, It.IsAny<CancellationToken>()), Times.Once);

        await fixture.Hub.UnsubscribeFromSymbols(["BTCUSDT", "ETHUSDT"]);
    }

    [Fact]
    public async Task SubscribeToSymbols_WithMoreThanFourSymbols_RejectsBeforeJoiningGroups()
    {
        var fixture = new HubFixture();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => fixture.Hub.SubscribeToSymbols(["BTCUSDT", "ETHUSDT", "SOLUSDT", "XRPUSDT", "ADAUSDT"], 60));

        Assert.Contains("one and four", exception.Message);
        fixture.Groups.Verify(groups => groups.AddToGroupAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.SubscriptionRegistry.Verify(registry => registry.TryAddActiveInterval(
            It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UnsubscribeFromSymbols_RemovesOnlyRequestedSymbols()
    {
        var fixture = new HubFixture();
        await fixture.Hub.SubscribeToSymbols(["BTCUSDT", "ETHUSDT"], 60);

        await fixture.Hub.UnsubscribeFromSymbols(["BTCUSDT"]);

        fixture.Groups.Verify(groups => groups.RemoveFromGroupAsync(
            fixture.ConnectionId, "BTCUSDT", It.IsAny<CancellationToken>()), Times.Once);
        fixture.Groups.Verify(groups => groups.RemoveFromGroupAsync(
            fixture.ConnectionId, "BTCUSDT:1m", It.IsAny<CancellationToken>()), Times.Once);
        fixture.Groups.Verify(groups => groups.RemoveFromGroupAsync(
            fixture.ConnectionId, "ETHUSDT", It.IsAny<CancellationToken>()), Times.Never);
        fixture.Groups.Verify(groups => groups.RemoveFromGroupAsync(
            fixture.ConnectionId, "ETHUSDT:1m", It.IsAny<CancellationToken>()), Times.Never);

        await fixture.Hub.UnsubscribeFromSymbols(["ETHUSDT"]);
    }

    private sealed class HubFixture
    {
        public string ConnectionId { get; } = Guid.NewGuid().ToString();
        public Mock<IGroupManager> Groups { get; } = new();
        public Mock<IActiveCandleSubscriptionRegistry> SubscriptionRegistry { get; } = new();
        public Mock<ICandleStreamActivationService> StreamActivation { get; } = new();
        public CryptoPricesHub Hub { get; }

        public HubFixture()
        {
            var context = new Mock<HubCallerContext>();
            context.SetupGet(x => x.ConnectionId).Returns(ConnectionId);

            Groups
                .Setup(x => x.AddToGroupAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Groups
                .Setup(x => x.RemoveFromGroupAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            SubscriptionRegistry
                .Setup(x => x.TryAddActiveInterval(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(true);

            Hub = new CryptoPricesHub(SubscriptionRegistry.Object, StreamActivation.Object)
            {
                Context = context.Object,
                Groups = Groups.Object
            };
        }
    }
}
