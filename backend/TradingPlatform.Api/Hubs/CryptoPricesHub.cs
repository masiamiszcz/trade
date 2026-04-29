using Microsoft.AspNetCore.SignalR;
using TradingPlatform.Core.Dtos;

namespace TradingPlatform.Api.Hubs;

public class CryptoPricesHub : Hub
{
    public async Task SubscribeToSymbol(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());
    }

    public async Task UnsubscribeFromSymbol(string symbol)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol.ToUpperInvariant());
    }

    public async Task BroadcastPriceUpdate(PriceUpdateDto update)
    {
        await Clients.Group(update.Symbol.ToUpperInvariant()).SendAsync("ReceivePriceUpdate", update);
    }
}
