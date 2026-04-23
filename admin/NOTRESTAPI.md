# NOT REST API - WebSocket & SignalR Audit dla CFD Real-Time Data

**Data**: April 22, 2026  
**Kontekst**: Trading Platform - potrzeba real-time aktualizacji cen CFD (kilkaset ms opóźnień jest krityczne)

---

## 1. PROBLEM DIAGNOSTYKA

### REST API - Aktualne Podejście
```
Timeline dla CFD Quote Update:
┌─────────────────────────────────────────┐
│ Client requestuje /api/cfd/prices       │ (0ms)
├─────────────────────────────────────────┤
│ Network latency (round-trip)            │ +20-100ms
├─────────────────────────────────────────┤
│ Server processing                       │ +10-50ms
├─────────────────────────────────────────┤
│ Response serialization                  │ +5-20ms
├─────────────────────────────────────────┤
│ Client receives data                    │ ~100-200ms
├─────────────────────────────────────────┤
│ Browser paint/render                    │ +16-33ms
└─────────────────────────────────────────┘
TOTAL: 116-253ms per quote (niewystarczające dla CFD)
```

### CFD Trading Problem
- **Price accuracy window**: 100-200ms (zmiana ceny może być warta $$)
- **Slippage**: Użytkownik chce kupić na 1.2500, a wykonanie na 1.2510 (10 pips = czasami strata)
- **Latency arbitrage**: Szybsze systemy mogą "front-run" wolniejszych traderów
- **Polling cost**: Co 5 sekund = 720 requestów/godzinę per user

---

## 2. WEBSOCKET - Deep Dive

### Jak Działa
```
1. Initial Connection:
   Client → HTTP UPGRADE → Server
   └─ Switch z HTTP do TCP persistent connection
   
2. Persistent Two-Way Channel:
   ┌─────────────────────┐
   │   Browser (Client)  │
   └─────────┬───────────┘
             │ Full-duplex TCP
             │ (both directions simultaneously)
             │
   ┌─────────▼───────────┐
   │ Server (Node/C#/..) │
   └─────────────────────┘
   
3. Message Flow:
   Server → Client: "EURUSD:1.2500"    (0ms latency if on same frame)
   Client → Server: "BUY EURUSD 1lot"  (sent instantly, no waiting)
```

### Performance Characteristics
```
Connection Setup:
- Initial handshake: ~50-100ms (one-time)
- Reconnection: <10ms

Message Latency (after connected):
- Publish latency: 1-5ms (server sends immediately)
- Transport latency: 5-20ms (over network)
- Receive-to-render: 5-20ms (client processing)
─────────────────────────
TOTAL: 11-45ms ✅ (2-5x lepiej niż REST!)

Memory/CPU (per 100 connected clients):
- Connections: ~100 * 64KB = 6.4MB
- CPU per message: ~0.1ms per client
- Network throughput: ~1KB/s per client (low!)
```

### Zalety WebSocket
✅ **Low latency** - one-way push, no request-response cycle  
✅ **Efficient bandwidth** - no HTTP headers on each message  
✅ **Bidirectional** - client can send orders while receiving quotes  
✅ **Real-time updates** - immediate push to all subscribers  
✅ **Scalable** - handles thousands of concurrent connections  

### Wady WebSocket
❌ **Stateful** - server must track client connections  
❌ **Memory overhead** - thousands of open connections = memory cost  
❌ **Complex reconnection** - network drops require handling  
❌ **Firewall issues** - some corporate networks block WebSocket  
❌ **No built-in RPC** - manual message routing needed  
❌ **Browser compatibility** - older IE/browsers not supported (rare)  

### Implementacja - Raw WebSocket
```csharp
// Server (ASP.NET WebSocket endpoint)
var webSocket = await context.WebSockets.AcceptWebSocketAsync();
var buffer = new byte[1024 * 4];

while (webSocket.State == WebSocketState.Open) {
    // Get price update from market data source
    var price = await GetCFDPrice("EURUSD");
    var message = Encoding.UTF8.GetBytes($"EURUSD:{price}");
    
    // Push to client immediately (no polling!)
    await webSocket.SendAsync(
        new ArraySegment<byte>(message),
        WebSocketMessageType.Text,
        true,
        CancellationToken.None);
}

// Client (Browser)
const ws = new WebSocket('wss://api.tradingplatform.com/prices');
ws.onmessage = (event) => {
    const [symbol, price] = event.data.split(':');
    updatePriceDisplay(symbol, price); // instant UI update
};
```

---

## 3. SIGNALR - Enterprise WebSocket

### Jak Działa
```
SignalR = Transport Abstraction Layer nad WebSocket

Fallback chain:
1. Try WebSocket (preferred)
   │
   ├─ Success? Use it (best performance)
   │
   └─ Blocked? Fall back to:
      ├─ Server-Sent Events (SSE) - one-way, HTTP
      ├─ Long-polling - pseudo real-time, HTTP
      └─ Forever-frame - legacy, HTTP
      
SignalR Hub Model:
┌──────────────────┐
│ Server Hub Class │
├──────────────────┤
│ SendPrice()      │─→ Client receives instantly
│ PlaceOrder()     │← Client calls this
│ BroadcastTrade() │─→ All clients notified
└──────────────────┘

// Automatic client reconnection
// Automatic message buffering if offline
// Strongly-typed method calls
```

### Performance Characteristics
```
Same as WebSocket when using WS transport:
- Latency: 11-45ms
- But adds SignalR overhead: +2-5ms (negligible)

Connection overhead:
- Handshake: ~100ms (includes WebSocket + SignalR negotiation)
- Reconnection attempt: <50ms (automatic with backoff)

Memory per client:
- Pure WebSocket: 64KB
- SignalR: ~150KB (includes buffering, state management)
```

### Zalety SignalR
✅ **Automatic fallback** - works if WebSocket blocked  
✅ **Reconnection handling** - automatic retry with exponential backoff  
✅ **Message buffering** - if client disconnects, queues messages  
✅ **Hub pattern** - clean, RPC-like API (method calls instead of JSON)  
✅ **Groups/Rooms** - easy to broadcast to subsets (e.g., EURUSD traders)  
✅ **Built-in heartbeat** - keeps connection alive, detects stale clients  
✅ **TypeScript support** - automatic client type generation  

### Wady SignalR
❌ **Higher overhead** - ~2-5ms extra per message  
❌ **Heavier client** - larger JavaScript library (~50KB)  
❌ **.NET specific** - primarily for C# backend (Node.js support exists but secondary)  
❌ **Complexity** - more abstractions = more to learn/debug  

### Implementacja - SignalR
```csharp
// Server - Create Hub
public class PricesHub : Hub {
    private readonly IPriceService _priceService;
    
    public async Task SubscribeToCFD(string symbol) {
        await Groups.AddToGroupAsync(Connection.ConnectionId, symbol);
        // Client is now in "EURUSD" group
    }
    
    // Called periodically or on price change
    public async Task BroadcastPrice(string symbol, decimal price) {
        // Send ONLY to clients subscribed to this symbol (efficient!)
        await Clients.Group(symbol).SendAsync("PriceUpdate", new {
            Symbol = symbol,
            Price = price,
            Timestamp = DateTime.UtcNow
        });
    }
}

// Client (TypeScript)
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/pricesHub")
    .withAutomaticReconnect() // ← automatic!
    .build();

await connection.start();
await connection.invoke("SubscribeToCFD", "EURUSD");

connection.on("PriceUpdate", (data) => {
    console.log(`${data.Symbol}: ${data.Price}`); // instant
});

// Also can send back:
await connection.invoke("PlaceOrder", {symbol: "EURUSD", amount: 100});
```

---

## 4. PORÓWNANIE: WebSocket vs SignalR vs REST

| Metryka | REST API | Raw WebSocket | SignalR |
|---------|----------|---------------|---------|
| **Latency** | 100-200ms | 11-45ms ✅ | 13-50ms ✅ |
| **Reconnection** | Manual polling | Manual handling | Automatic ✅ |
| **Fallback** | ✅ Always works | ❌ May fail | ✅ Multi-layer |
| **Bandwidth** | High (headers) | Low ✅ | Low ✅ |
| **Server Memory** | Low | Medium | Medium |
| **Implementation** | Simple | Medium | Harder |
| **CFD Suitability** | ❌ Too slow | ✅✅ Ideal | ✅✅ Ideal |
| **Stocks/Crypto** | ✅ OK (5s) | ✅ Overkill | ✅ Overkill |

---

## 5. ARCHITECTURE REKOMENDACJE

### Hybrid Approach dla Trading Platform
```
┌────────────────────────────────────────────┐
│         Frontend (Browser)                  │
├────────────────────────────────────────────┤
│  Stocks/Crypto      │        CFD           │
│  ↓                  │        ↓             │
│  REST API (5s)      │   SignalR (30ms)     │
│  Update PortfolioGrid   Real-time Chart    │
└────────────────────────────────────────────┘
         │                      │
         ↓                      ↓
    ┌────────────────────────────────┐
    │    Backend (ASP.NET Core)      │
    ├────────────────────────────────┤
    │ REST endpoints                 │
    │  GET /api/stocks               │
    │  GET /api/crypto               │
    │  GET /api/cfd/:symbol (cache)  │
    │                                │
    │ SignalR Hub (PricesHub)        │
    │  Groups: EURUSD, GBPUSD, etc   │
    │  Methods: SubscribeToCFD()     │
    │  Events: PriceUpdate           │
    └────────────────────────────────┘
         │
         ├─→ Stock API (delayed feed)
         ├─→ Crypto API (delayed feed)
         └─→ CFD Market Data (real-time)
```

### Why This Split?
1. **REST for slow markets** - Stocks/Crypto: 5s old price is fine, lower bandwidth
2. **SignalR for CFD** - Millisecond-critical, bidirectional (place orders)
3. **Cost efficient** - Not all 1000 users need real-time CFD prices
4. **Easy migration** - Stocks can upgrade to SignalR later if needed

---

## 6. IMPLEMENTATION ROADMAP

### Phase 1: CFD Real-Time (Priority)
```
Timeline: 1-2 weeks

Tasks:
✓ Create PricesHub in Backend
✓ Implement price subscription groups
✓ Add CFDDashboard WebSocket consumer
✓ Test reconnection handling
✓ Monitor memory usage at scale
```

### Phase 2: Order Execution Real-Time
```
Timeline: 2-3 weeks

Tasks:
✓ Add order placement via SignalR (lower latency)
✓ Implement order confirmation push
✓ Add trade execution stream
✓ Implement stop-loss real-time monitoring
```

### Phase 3: Advanced Features
```
Timeline: 3-4 weeks

Tasks:
✓ Market depth stream (bid/ask updates)
✓ News feed for CFDs (real-time alerts)
✓ Economic calendar countdown
✓ Volatility alerts (automatic disconnect/reconnect)
```

---

## 7. POTENTIAL ISSUES & SOLUTIONS

### Problem 1: Network Interruption
```
Issue: Client WiFi drops, WebSocket closes
Solution: SignalR automatic reconnection
  └─ Exponential backoff: 0s, 2s, 10s, 30s
  └─ Auto-resubscribe to groups after reconnect
```

### Problem 2: Memory Leak (1000+ concurrent users)
```
Issue: Server memory grows unbounded
Risk: 1000 connections × 150KB = 150MB (acceptable)
      1000 connections × old implementation = 10GB (bad!)
      
Solution:
✓ Use connection.onclose() to cleanup groups
✓ Monitor group membership (no zombies)
✓ Set max connection timeout: 30 min idle = disconnect
✓ Use SignalR's built-in heartbeat
```

### Problem 3: Price Feed Stale (network lag)
```
Issue: Server sends prices, but network buffered
Solution: Add timestamp to each price
  {symbol: "EURUSD", price: 1.2500, timestamp: 1713787200100}
  
Client-side:
  const lag = Date.now() - data.timestamp;
  if (lag > 500) console.warn("Price may be stale");
  
UI: Show visual indicator (red/green border) for lag > 1s
```

### Problem 4: Scalability (10,000 CFD traders)
```
Current estimate:
- 10,000 users × 150KB = 1.5GB RAM (one server)
- Each price update → 10,000 message sends
- CPU: ~50% sustained (acceptable)

Solutions if overloaded:
1. Scale horizontally (2+ servers)
   └─ Use Redis backplane for group messaging
   
2. Implement price aggregation
   └─ Send batch updates every 50ms instead of every update
   
3. Implement tiers
   └─ Free users: prices every 100ms
   └─ Premium: prices every 10ms
```

---

## 8. CODE ARCHITECTURE SKETCH

### Backend Structure
```
TradingPlatform.Api/
├── Hubs/
│   ├── PricesHub.cs
│   ├── OrdersHub.cs
│   └── NotificationsHub.cs
├── Services/
│   ├── CFDPriceService.cs (publishes to PricesHub)
│   ├── OrderExecutionService.cs
│   └── MarketDataService.cs
└── Middleware/
    └── WebSocketCompression.cs (optional: reduce bandwidth)

// PricesHub publishes whenever price updates from market data
```

### Frontend Structure
```
frontend/src/
├── hooks/
│   ├── useSignalR.ts (custom hook for SignalR connection)
│   ├── useCFDPrices.ts (subscribes to prices)
│   └── useCFDOrders.ts (place/cancel orders)
├── components/
│   └── CFDDashboard.tsx (uses useCFDPrices)
└── services/
    └── signalrService.ts (connection management)
```

---

## 9. SECURITY CONSIDERATIONS

### Problem: Anyone could subscribe to prices
```
Risk: Unauthorized users get real-time price feed
Solution: Add authorization to hub methods

[Authorize] // ← require JWT token
public class PricesHub : Hub {
    public override async Task OnConnectedAsync() {
        var user = Context.User;
        // Only premium users can subscribe to CFD real-time
        if (!user.IsInRole("PremiumTrader")) {
            Context.Abort();
        }
    }
}
```

### Problem: Man-in-the-middle could intercept orders
```
Solution: Use WSS (WebSocket Secure)
  wss:// instead of ws://
  └─ Same TLS/SSL as HTTPS
  
Verify in Program.cs:
  builder.Services.AddSignalR();
  // IIS will handle WSS automatically if HTTPS configured
```

---

## 10. MONITORING & ALERTING

### Key Metrics to Track
```
1. Active Connections
   └─ Alert if > 5000 (scaling threshold)

2. Average Latency
   └─ Alert if > 100ms (indicates network issues)

3. Message Queue Length
   └─ Alert if > 1000 (consumers not keeping up)

4. Memory per Connection
   └─ Alert if > 500KB (leak detected)

5. Group Membership
   └─ Alert if orphaned groups (cleanup failure)

Tools:
- Application Insights (Azure)
- Grafana + Prometheus (open source)
- Custom logging to database
```

---

## 11. VERDICT & RECOMMENDATION

### For Trading Platform:
```
✅ USE: SignalR for CFD real-time data
   Reason: 
   - Latency: 13-50ms (acceptable for CFD)
   - Automatic reconnection (critical for retail traders)
   - Easy bidirectional communication (orders)
   - Fallback if WebSocket blocked (corporate networks)

✅ KEEP: REST API for Stocks/Crypto
   Reason:
   - 5 second updates sufficient
   - Lower bandwidth (many users)
   - Simpler caching strategy
   - Can migrate to SignalR later if needed

❌ AVOID: Raw WebSocket
   Reason:
   - Manual reconnection is error-prone
   - No fallback if blocked
   - Trading platform = reliability > speed
   - Extra 2-5ms not worth losing fallback
```

### Implementation Cost
```
Backend (C#/.NET):
- PricesHub: 200 lines
- Integration: 300 lines
- Total: ~2-3 days work

Frontend (TypeScript/React):
- useSignalR hook: 150 lines
- CFDDashboard update: 100 lines
- Total: ~2 days work

Testing & Monitoring:
- Load testing (1000+ connections): 3 days
- Production monitoring setup: 2 days

Total: 2 weeks (including all testing)
```

---

## 12. GLOSSARY

| Term | Meaning |
|------|---------|
| **WebSocket** | Protocol that upgrades HTTP to persistent TCP connection |
| **SignalR** | Microsoft's abstraction layer over WebSocket with fallbacks |
| **Hub** | Server class that clients call methods on (RPC-like) |
| **Group** | Collection of clients (e.g., all subscribed to EURUSD) |
| **Fallback** | Alternative transport if primary fails (SSE, long-polling) |
| **Heartbeat** | Periodic ping/pong to keep connection alive |
| **Slippage** | Difference between expected price and execution price |
| **Latency** | Time from action to result (how many milliseconds) |
| **WSS** | WebSocket Secure (encrypted with TLS) |
| **Front-running** | Trading ahead of others because of speed advantage |

---

**Document Status**: ✅ Complete Audit  
**Recommendation**: Implement SignalR for CFD real-time, keep REST for Stocks/Crypto  
**Risk Level**: Low (SignalR is battle-tested, used by financial platforms)  
**Estimated ROI**: High (improves CFD trader experience significantly, competitive advantage)
