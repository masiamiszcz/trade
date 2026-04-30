# Crypto Data Startup Order Audit

## Cel audytu

Opisuję, jakie pliki i metody są zaangażowane w uruchamianie backendu dla danych kryptowalut, w jakiej kolejności są rejestrowane usługi oraz gdzie wprowadzić zmianę, aby:

1. Uruchamiać najpierw loader CSV i wczytać dane historyczne do bazy.
2. Po zakończonym imporcie CSV zapisać flagę stanu "done" lub "failed".
3. Dopiero po tej fladze uruchamiać Binance REST API do pobrania brakujących minutowych świeczek.
4. Po ukończonym REST sync dopiero uruchamiać Binance WebSocket i stale zbierać nowe trade'y do bazy.
5. `CandleAggregationService` jest obecnie używany, ale będzie usunięty; niniejszy audyt pokazuje, gdzie jest wykorzystywany.

---

## Obecny flow uruchamiania usług

### 1. Rejestracja usług

Plik: `backend/TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs`

Rejestracja hostowanych usług odbywa się w poniższej kolejności:

- `RateFetcherHostedService`
- `BinanceStartupPollingService`
- `MarketProcessingService`
- `BinanceWebSocketService`

To jest krytyczne miejsce do kontroli kolejności startu.

### 2. Główne klasy wykonawcze

| Plik | Funkcja | Uwagi |
|---|---|---|
| `backend/TradingPlatform.Data/Services/Market/BinanceStartupPollingService.cs` | Import CSV + pobieranie brakujących Binance REST | Bazowy startup loader i sync danych historycznych |
| `backend/TradingPlatform.Data/External/BinanceApiClient.cs` | Wywołania Binance REST API klines | Używane tylko przez `BinanceStartupPollingService` |
| `backend/TradingPlatform.Data/Services/Market/BinanceWebSocketService.cs` | Połączenie WebSocket do Binance, odbiór trade | Uruchamiany zawsze niezależnie od stanu CSV/REST |
| `backend/TradingPlatform.Data/Services/Market/MarketProcessingService.cs` | Przetwarzanie trade'ów z kanału, zapis 1m candle | Odbiera dane z WS i zapisuje do DB |
| `backend/TradingPlatform.Data/Services/Market/CandleAggregationService.cs` | Agregacja 1m -> 5m/1h | Używana w `MarketProcessingService`; będzie usunięta |
| `backend/TradingPlatform.Data/Repositories/SqlCandleRepository.cs` | Operacje DB dla wszystkich candle | Zapis i pobieranie danych dla CSV, REST i WS |
| `backend/TradingPlatform.Api/Program.cs` | Rejestracja `AddDataServices` i `AddApiServices` | Nie kontroluje bezpośrednio kolejności hostowanych usług, robi tylko rejestrację kontenera DI |

---

## Gdzie znajduje się start loadera CSV i pobieranie brakujących danych

### `BinanceStartupPollingService.ExecuteAsync(...)`

To jest miejsce, gdzie obecnie wykonywany jest cały proces:

1. `await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);`
2. `await ImportBinanceCsvFilesAsync(stoppingToken);`
3. odczyt ostatniego rekordu DB:
   - `await _candleRepository.GetLastCandleTimestampAsync(...)`
4. `FetchMissingBinanceKlinesAsync(startTime, stoppingToken);`
5. `SaveKlinesAsync(klines, stoppingToken);`

To jest `sekwencja`, ale bez centralnej flaga/kooperacji z WS.

### `ImportBinanceCsvFilesAsync(...)`

Wczytuje wszystkie pliki CSV z folderu:
- `CsvFolderPath = "C:\Users\kubac\Desktop\Studia\CSV_BTCUSDT"`
- z użyciem `Directory.EnumerateFiles(folderPath, "*.csv", SearchOption.TopDirectoryOnly)`
- sortuje je alfabetycznie
- wywołuje `ProcessCsvFileAsync(filePath, cancellationToken)` dla każdego pliku

### `ProcessCsvFileAsync(...)`

Dla każdego pliku:
- pomija nagłówek
- parsuje wiersze CSV do `BinanceCsvCandleDto`
- mapuje na `CandleEntity`
- robi batchowanie w `SaveBatchAsync(...)`

Krytyczny problem do naprawy: jeśli pierwszy poprawny rekord pliku już istnieje w DB, funkcja zwraca `return` i przestaje przetwarzać cały plik.

### `SaveBatchAsync(...)`

Dla każdej batched listy:
- pobiera istniejące `OpenTime` z DB przez `GetExistingOpenTimesAsync(...)`
- filtruje duplikaty
- zapisuje tylko nowe rekordy `AddRangeAsync(...)`

---

## Gdzie znajduje się Binance REST sync

### `FetchMissingBinanceKlinesAsync(...)`

Wywołuje:
- `IBinanceApiClient.GetHistoricalKlinesAsync(Symbol, "1m", ApiFetchLimit, startTime, cancellationToken)`

`ApiFetchLimit = 1000`, co oznacza, że obecnie pobierane jest maksymalnie ~16h40m na jedno uruchomienie.

### `SaveKlinesAsync(...)`

- mapuje `BinanceKline` do `CandleEntity`
- sprawdza duplikaty przez `GetExistingOpenTimesAsync(...)`
- zapisuje nowe rekordy `AddRangeAsync(...)`

---

## Gdzie znajduje się start i zapisywanie Binance WS

### `BinanceWebSocketService.ExecuteAsync(...)`

- uruchamia natychmiast po starcie aplikacji
- łączy się do Binance WebSocket i odczytuje trade'y
- każdy trade przepuszcza do:
  - `_marketProcessor.HandleAsync(trade)`
  - `_channel.Writer.WriteAsync(trade)`

### `MarketProcessingService.ExecuteAsync(...)`

- czyta `Channel<Trade>` i wykonuje `ProcessTradeAsync(trade)`

### `MarketProcessingService.ProcessTradeAsync(...)`

- aktualizuje bieżącą 1-minutową świeczkę
- gdy nowa minuta się zaczyna:
  - `PublishClosedCandlesAsync(completedCandle)`
  - `SaveCandleAsync(completedCandle)`
- zapis do DB odbywa się w `SaveCandleAsync(...)`

### `SaveCandleAsync(...)`

- zapisuje 1m candle z source "binance" do bazy:
  - `SqlCandleRepository.AddAsync(entity)`
- uruchamia `CandleAggregationService.HandleCompletedCandleAsync(candle)`

---

## Gdzie używany jest `CandleAggregationService`

### Użycie

- `backend/TradingPlatform.Data.Services.Market.MarketProcessingService.cs`
- jedyne wywołanie:
  - `await _aggregationService.HandleCompletedCandleAsync(candle);`

### Co robi dziś

- agreguje 1m świeczki do 5m i 1h
- zapisuje agregaty jako osobne rekordy w bazie

### Dlaczego jest to istotne dla edycji

- jeśli usuwasz lub zmieniasz to zachowanie, musisz edytować `MarketProcessingService.SaveCandleAsync(...)`
- to jest jedyne powiązanie, dlatego możesz łatwo wyłączyć / zastąpić agregację bez zmiany innych modułów

---

## Gdzie skonfigurować porządek w aplikacji

### Główne miejsce

- `backend/TradingPlatform.Data.Extensions.ServiceCollectionExtensions.cs`
  - tu można ustawić, które hostowane usługi rejestrujemy i ewentualnie w jakiej kolejności są inicjowane.

### Dodatkowe miejsce

- `backend/TradingPlatform.Api.Program.cs`
  - uruchamia `builder.Services.AddDataServices(builder.Configuration)`
  - czyli jest to root, ale nie zawiera bezpośredniej logiki startu BG services.

---

## Proponowany plan techniczny (tylko analiza)

1. Dodaj centralny koordynator stanu startowego, np. `StartupDataLoadCoordinator` lub `IDataImportStateService`.
   - stan CSV: `Pending / Running / Completed / Failed`
   - stan REST sync: `Pending / Running / Completed / Failed`
2. W `BinanceStartupPollingService`:
   - po zakończeniu CSV importu ustaw flagę `CsvImportCompleted = true` lub `CsvImportFailed = true`
   - dopiero potem uruchomić REST sync
3. W `BinanceWebSocketService`:
   - blokuj start WS do momentu, gdy `CsvImportCompleted` oraz `RestSyncCompleted` będą `true`
   - jeśli import/REST sync się nie powiedzie, WS powinien nie startować albo czekać w pętli retry
4. W `MarketProcessingService`:
   - może pozostać jako hostowana usługa, która czeka na dane z kanału
   - jeśli chcesz jeszcze bezpieczniej, dodaj warunek w `ProcessTradeAsync` (nie przetwarzać, gdy sync nie zakończono)
5. W `SqlCandleRepository` i `BinanceStartupPollingService`:
   - napraw import CSV, aby nie `return` cały plik na podstawie pierwszego rekordu
   - popraw logikę paginacji REST API, aby pobierać więcej niż 1000 rekordów i wypełniać cały brakujący zakres
6. `CandleAggregationService`
   - obecnie używany tylko w `MarketProcessingService.SaveCandleAsync`
   - jeśli usuwasz go, edytuj tam tę linię i ewentualnie powiązane testy

---

## Najważniejsze pliki do edycji

- `backend/TradingPlatform.Data.Extensions.ServiceCollectionExtensions.cs`
- `backend/TradingPlatform.Data.Services.Market.BinanceStartupPollingService.cs`
- `backend/TradingPlatform.Data.Services.Market.BinanceWebSocketService.cs`
- `backend/TradingPlatform.Data.Services.Market.MarketProcessingService.cs`
- `backend/TradingPlatform.Data.Services.Market.CandleAggregationService.cs`
- `backend/TradingPlatform.Data.Repositories.SqlCandleRepository.cs`
- `backend/TradingPlatform.Api.Program.cs` (tylko do ogólnej rejestracji, nie do samej kolejności startu)

---

## Krytyczne uwagi i konkretne wskazówki

### 1. Rejestracja usług nie gwarantuje kolejności wykonania

`AddHostedService<A>()`, `AddHostedService<B>()`, `AddHostedService<C>()`
- NIE gwarantuje sekwencyjnego startu `A -> B -> C`
- hostowane usługi `IHostedService` mogą zacząć działać równolegle

✅ Wniosek:
- musimy dodać centralny koordynator stanu, a nie polegać na kolejności rejestracji
- model `bool CsvDone` / `bool RestDone` jest niewystarczający

### 2. Wymagany model state machine

Proponowany stan startowy:

```csharp
enum StartupStage
{
    NotStarted,
    CsvLoading,
    CsvCompleted,
    CsvFailed,
    RestSyncing,
    RestCompleted,
    RestFailed,
    Ready
}
```

Serwis kontrolujący:

- `Task WaitForStageAsync(StartupStage stage, CancellationToken token)`
- `void SetStage(StartupStage stage)`
- `StartupStage CurrentStage { get; }`

To daje:
- brak race condition
- prostą obserwowalność stanu
- możliwość diagnostyki w logach
- retry logicę dla usługi REST lub CSV

### 3. Kolejność pracy usług powinna być wymuszona w runtime

#### `BinanceStartupPollingService`
- ustawia stage `CsvLoading`
- wykonuje import CSV
- po imporcie ustawia `CsvCompleted` lub `CsvFailed`
- dopiero po `CsvCompleted` przechodzi do `RestSyncing`
- po REST sync ustawia `RestCompleted` lub `RestFailed`
- na końcu, jeśli wszystko przebiegło poprawnie, ustawia `Ready`

#### `BinanceWebSocketService`
Masz dwie opcje:

- opcja A (MVP): czeka na `Ready` i dopiero wtedy łączy WS
- opcja B (pro): startuje od razu, zapisuje dane do bufora, ale nie przetwarza ich do DB / strumieni, dopóki nie nastąpi `Ready`

Dla produkcyjnego tradingu opcja B jest lepsza, ale opcja A jest wystarczająca jako MVP.

### 4. Naprawa CSV importu

Krytyczny błąd:
- `if (exists) return;` w `ProcessCsvFileAsync(...)`

Powinno być:
- pomiń zduplikowany rekord
- loguj, że rekord istnieje
- kontynuuj przetwarzanie pliku
- aktualizuj audit/imprementację stanu pliku

### 5. Pagowanie REST i retry

`ApiFetchLimit = 1000` oznacza, że jedna próba REST pobiera tylko ~16h40m.

Należy dodać:
- pętlę paginacji REST po `startTime` / `endTime`
- logiczne kontynuowanie pobierania do momentu osiągnięcia celu
- retry na błędy API
- update stanu `RestSyncing` i `RestFailed`

### 6. `CandleAggregationService` - jedno miejsce użycia

Jedyna linia użycia to:
- `backend/TradingPlatform.Data.Services.Market.MarketProcessingService.cs`
- `await _aggregationService.HandleCompletedCandleAsync(candle);`

Jeśli usuwasz agregację, to jest jedyne miejsce do edycji na razie.

---

## Główne pliki do edycji

- `backend/TradingPlatform.Data.Extensions.ServiceCollectionExtensions.cs`
- `backend/TradingPlatform.Data.Services.Market.BinanceStartupPollingService.cs`
- `backend/TradingPlatform.Data.Services.Market.BinanceWebSocketService.cs`
- `backend/TradingPlatform.Data.Services.Market.MarketProcessingService.cs`
- `backend/TradingPlatform.Data.Services.Market.CandleAggregationService.cs`
- `backend/TradingPlatform.Data.Repositories.SqlCandleRepository.cs`
- `backend/TradingPlatform.Api.Program.cs`

---

## Dodatkowa uwaga

`Program.cs` nie zawiera logiki, która decyduje o sekwencji startu hostowanych usług; robi tylko rejestrację `AddDataServices(...)`. Kluczowe zmiany należy wprowadzić w `ServiceCollectionExtensions.cs` i wewnątrz `BinanceStartupPollingService` / `BinanceWebSocketService`.

## Plan naprawczy (szczegółowy)

Zobacz także:
- `audits/CRYPTO_DATA_STARTUP_ORDER_FIX_PLAN.md`
