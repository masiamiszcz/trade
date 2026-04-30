# Crypto Data Startup Order Fix Plan

## Cel

Doprecyzować, co konkretnie trzeba edytować w kodzie, aby uruchamianie backendu działało zgodnie z logiką:

1. CSV loader startuje jako pierwszy.
2. Po imporcie CSV ustawiana jest flaga stanu.
3. Binance REST sync wykonuje się dopiero po sukcesie CSV.
4. Po zakończeniu REST sync startuje WebSocket lub WS buforuje dane i czeka na `Ready`.
5. Agregacja `CandleAggregationService` jest tylko dodatkowym elementem i może być usunięta później.

## Główne pliki do zmiany

- `backend/TradingPlatform.Data.Extensions.ServiceCollectionExtensions.cs`
- `backend/TradingPlatform.Data.Services.Market.BinanceStartupPollingService.cs`
- `backend/TradingPlatform.Data.Services.Market.BinanceWebSocketService.cs`
- `backend/TradingPlatform.Data.Services.Market.MarketProcessingService.cs`
- `backend/TradingPlatform.Data.Repositories.SqlCandleRepository.cs`
- `backend/TradingPlatform.Data.Services.Market.CandleAggregationService.cs`

## Konkretny plan zmian

### 1. Dodaj centralny koordynator stanu startupu

Nowy serwis:
- `StartupStateCoordinator` lub `StartupDataLoadCoordinator`
- `StartupStage CurrentStage { get; }`
- `Task WaitForStageAsync(StartupStage expectedStage, CancellationToken cancellationToken)`
- `Task SetStageAsync(StartupStage stage, CancellationToken cancellationToken)`

Proponowane etapy:
- `NotStarted`
- `CsvLoading`
- `CsvCompleted`
- `CsvFailed`
- `RestSyncing`
- `RestCompleted`
- `RestFailed`
- `Ready`

### 2. Rejestracja kolejek i usług w DI

W `backend/TradingPlatform.Data.Extensions.ServiceCollectionExtensions.cs`:
- zostaw rejestrację `AddHostedService<RateFetcherHostedService>()`
- zarejestruj `StartupStateCoordinator` jako singleton
- `AddHostedService<BinanceStartupPollingService>()`
- `AddHostedService<MarketProcessingService>()`
- `AddHostedService<BinanceWebSocketService>()`

> Pamiętaj: kolejność `AddHostedService(...)` nie gwarantuje kolejności startu.

### 3. `BinanceStartupPollingService` powinien kontrolować etapy

W `ExecuteAsync(...)`:
- ustaw `CsvLoading`
- wykonaj `ImportBinanceCsvFilesAsync(...)`
  - na koniec ustaw `CsvCompleted` lub `CsvFailed`
- dopiero po `CsvCompleted` ustaw `RestSyncing`
- wykonaj `FetchMissingBinanceKlinesAsync(...)`
  - po zakończeniu ustaw `RestCompleted` lub `RestFailed`
- jeśli oba etapy zakończą się sukcesem, ustaw `Ready`

Dodatkowo:
- `ImportCsvFolderAsync(...)` nie może przerywać pliku po pierwszym istniejącym rekordzie
- `ProcessCsvFileAsync(...)` powinien skipować duplikaty, a nie zwracać cały plik
- `SaveBatchAsync(...)` powinien zapisać `newBatch` i kontynuować

### 4. `BinanceWebSocketService` powinien uwzględniać stan startupu

Opcja A (prostszym MVP):
- w `ExecuteAsync(...)` przed `RunWebSocketAsync(...)` wywołaj `await coordinator.WaitForStageAsync(StartupStage.Ready, stoppingToken);`
- dopiero potem stwórz połączenie WS

Opcja B (bardziej zaawansowane):
- startuj WS natychmiast
- zapisuj otrzymane trade'y do bufora w pamięci lub osobnej tabeli
- przetwarzaj je dopiero po osiągnięciu `Ready`
- przywróć/replay'uj dane po `Ready`

### 5. `MarketProcessingService` i buforowanie danych

- `MarketProcessingService` może pozostać uruchomiony, ale warto dodać ochronę:
  - jeśli `StartupStage` < `Ready`, nie zapisuj lub zapisuj z flagą bufora
  - po osiągnięciu `Ready` możesz włączyć normalne przetwarzanie

### 6. Naprawa `BinanceApiClient`/REST paginacji

- `FetchMissingBinanceKlinesAsync(...)` powinien obsłużyć kilka serii po 1000 rekordów
- w pętli kontynuuj pobieranie kolejnych `startTime` do osiągnięcia celu lub do braku nowych danych

### 7. `CandleAggregationService`

- obecne użycie znajduje się tylko w `MarketProcessingService.SaveCandleAsync(...)`
- jeśli planujesz usunąć agregację, edytuj tę metodę jako pierwszą

## Szczegóły do sprawdzenia w kodzie

### `backend/TradingPlatform.Data.Services.Market.BinanceStartupPollingService.cs`

- `ExecuteAsync(...)`
- `ImportBinanceCsvFilesAsync(...)`
- `ProcessCsvFileAsync(...)`
- `SaveBatchAsync(...)`
- `FetchMissingBinanceKlinesAsync(...)`
- `SaveKlinesAsync(...)`

### `backend/TradingPlatform.Data.Services.Market.BinanceWebSocketService.cs`

- `ExecuteAsync(...)`
- `RunWebSocketAsync(...)`

### `backend/TradingPlatform.Data.Services.Market.MarketProcessingService.cs`

- `ExecuteAsync(...)`
- `ProcessTradeAsync(...)`
- `SaveCandleAsync(...)`

### `backend/TradingPlatform.Data.Repositories.SqlCandleRepository.cs`

- `GetLastCandleTimestampAsync(...)`
- `GetExistingOpenTimesAsync(...)`
- `AddRangeAsync(...)`

## Co dokładnie można edytować teraz

1. Dodaj singleton stanu startowego w `AddDataServices(...)`
2. Zmodyfikuj `BinanceStartupPollingService` do pracy etapowej
3. Zmodyfikuj `BinanceWebSocketService` do czekania na `Ready` lub buforowania
4. Zaktualizuj csv loader, aby skipował istniejące rekordy, zamiast zwracać
5. Dodaj pętlę paginacji w rest sync
6. Usuń wywołanie `CandleAggregationService` jeśli chcesz całkowicie zrezygnować z jego logiki

---

## Uwaga

`backend/TradingPlatform.Api/Program.cs` nie zawiera kolejności startu usług. Kontrolę stanu i kolejność należy zrealizować w serwisach oraz w koordynatorze stanu.
