# Buying Service Architecture Audit

## Summary

The current backend contains a solid relational foundation for accounts, transfers, positions, instruments, and audit logs, but it does not yet implement a central transaction/buying service for buy, sell, transfer, and lock operations.

This audit documents the existing architecture, identifies gaps, and recommends a service design layered on top of the current data model.

## Existing Foundations

### Account base
- `backend/TradingPlatform.Core/Interfaces/IAccountService.cs`
- `backend/TradingPlatform.Core/Services/AccountService.cs`
- `backend/TradingPlatform.Data/Repositories/SqlAccountRepository.cs`
- `backend/TradingPlatform.Data/Entities/AccountEntity.cs`
- `backend/TradingPlatform.Data/Configurations/AccountEntityConfiguration.cs`

The code supports:
- main account creation
- retrieval of a user’s main account
- retrieval of all user accounts
- account persistence with `AvailableBalance` and `ReservedBalance`
- account hierarchy: one main account per user, one subaccount per pillar

### Transfer persistence
- `backend/TradingPlatform.Data/Entities/AccountTransferEntity.cs`
- `backend/TradingPlatform.Data/Configurations/AccountTransferEntityConfiguration.cs`

The current transfer model supports:
- `FromAccountId` / `ToAccountId`
- `Amount` and `Currency`
- `TransferType` with values: `Deposit`, `Withdrawal`, `Internal`, `Adjustment`
- `TransferStatus` with lifecycle states: `Pending`, `Completed`, `Rejected`, `Cancelled`

This is a good starting point for cash movement, but it is not yet wired to a trading domain service for buy/sell semantics.

### Position model
- `backend/TradingPlatform.Data/Entities/PositionEntity.cs`
- `backend/TradingPlatform.Data/Configurations/PositionEntityConfiguration.cs`

The position entity supports:
- `AccountId` and `InstrumentId`
- `Quantity`, `ReservedQuantity`, `AverageOpenPrice`
- non-negative quantity constraints

This matches a portfolio holdings model, but the service layer is missing the trade execution logic that would update positions on buy/sell.

### Instrument domain and approval workflow
- `backend/TradingPlatform.Data/Entities/InstrumentEntity.cs`
- `backend/TradingPlatform.Core/Enums/TradingEnums.cs`
- `backend/TradingPlatform.Core/Services/InstrumentService.cs`

Instrument workflow is already built around admin approval and lifecycle status. That is useful for asset management, but it is separate from a user trading transaction service.

### Audit logging
- `backend/TradingPlatform.Core/Models/AuditLog.cs`
- `backend/TradingPlatform.Core/Interfaces/IAuditLogRepository.cs`
- `backend/TradingPlatform.Data/Entities/AuditLogEntity.cs`
- `backend/TradingPlatform.Data/Configurations/AuditLogEntityConfiguration.cs`

Existing audit logging is admin-centric and append-only. It is suitable for compliance audit trails, but it does not currently capture user transaction event details for buy/sell/transfer processing.

### DbContext registration
- `backend/TradingPlatform.Data/Context/TradingPlatformDbContext.cs`

All key entities are registered in the DbContext, including `Accounts`, `AccountTransfers`, `Positions`, `Instruments`, and `AuditLogs`.

### API surface
- `backend/TradingPlatform.Api/Controllers/AccountController.cs`

Current REST API endpoints for account management are:
- `GET /api/account/main`
- `GET /api/account`

The API does not currently expose transaction endpoints such as `buy`, `sell`, `transfer`, `lock`, or `transactions`.

### Frontend state
- `frontend/src/hooks/useTransactions.ts`
- `frontend/src/components/user/TransactionHistory.tsx`

Frontend transaction history is currently mocked and explicitly marked as temporary, with a TODO stating the backend endpoint `/api/account/transactions` does not exist.

## Strengths

- Clear domain separation between core services, repositories, and API.
- Relational model already supports transfers and portfolio positions.
- `AccountEntity` includes strong balance constraints and account hierarchy rules.
- `PositionEntity` includes reserved quantity and average open price, which is directly relevant to trading.
- Audit log table is configured for immutable, searchable compliance records.
- DbContext and EF configuration already include the necessary tables for transaction work.

## Gaps and Risks

1. No central transaction service exists.
   - No `ITransactionService`, `IAccountTransferService`, or `TradingService` in `backend/TradingPlatform.Core/Services`.
   - Existing `AccountService` is limited to account creation and reads.

2. No buy/sell command model.
   - `TransferType` covers deposits/withdrawals/internal/adjustment only.
   - There is no dedicated trade order or settlement entity.

3. No transactional business rules in service layer.
   - There is no logic to reserve funds, execute a buy, update position quantities, or settle a sale.
   - No atomic multi-entity transaction across balance updates, transfer records, and position changes.

4. API layer missing trading endpoints.
   - User-facing end points for `POST /api/trade/buy`, `POST /api/trade/sell`, `POST /api/account/transfer`, and `POST /api/account/lock` are absent.

5. Audit trail scope is incomplete for user transactions.
   - Current audit model is admin action focused.
   - A trading service should log user transaction events and state transitions as part of compliance.

6. Frontend uses mock transaction data.
   - UI is built for transaction history, but backend support is not implemented.

## Recommended Architecture

### Core service layer
Define a centralized trading/transaction service in `backend/TradingPlatform.Core/Services` with an interface in `backend/TradingPlatform.Core/Interfaces`.
Suggested service responsibilities:
- `ExecuteDepositAsync(...)`
- `ExecuteWithdrawalAsync(...)`
- `ExecuteInternalTransferAsync(...)`
- `ExecuteBuyAsync(...)`
- `ExecuteSellAsync(...)`
- `ReserveFundsAsync(...)`
- `ReleaseFundsAsync(...)`
- `LockAccountFundsAsync(...)`
- `GetTransactionHistoryAsync(...)`

This service should orchestrate:
- account balance updates (`AvailableBalance`, `ReservedBalance`)
- creation of `AccountTransferEntity` records
- creation/modification of `PositionEntity` rows
- validation of user, account, instrument, and margin constraints
- audit logging for transaction lifecycle events

### Data persistence
Reuse and extend the current entity model:
- `AccountTransferEntity` for cash movements and transfer events.
- `PositionEntity` for holdings, reserved quantity, and average open price.
- `InstrumentEntity` for asset reference.
- `AuditLogEntity` for transaction event provenance.

Potential extension points:
- add a dedicated `TradeOrderEntity` or `TransactionEventEntity` if buy/sell lifecycle needs richer status/market details.
- add `TransactionType` values for `BUY`, `SELL`, `LOCK`, `UNLOCK` if those need explicit audit semantics.

### Transaction boundaries
Implement atomic operations with EF Core transaction scope around multi-step work:
- adjust balances
- create transfer or trade records
- update positions
- log audit/event entries
- commit or rollback as one unit

This can be implemented using `TradingPlatformDbContext.Database.BeginTransactionAsync(...)` or a unit-of-work wrapper.

### API contract
Add controller endpoints in `backend/TradingPlatform.Api/Controllers` for user trading operations:
- `POST /api/account/transfer`
- `POST /api/account/lock`
- `POST /api/trade/buy`
- `POST /api/trade/sell`
- `GET /api/account/transactions`

The endpoints should authenticate the user and route to the central trading service.

### Frontend integration
Update frontend to replace mocked transaction history with real backend data.
- `frontend/src/hooks/useTransactions.ts` should call the backend endpoint once implemented.
- `frontend/src/components/user/TransactionHistory.tsx` is already wired for a real transaction feed.

### Audit and compliance
Use the existing audit subsystem, but extend it to capture transaction events:
- record `Action` values such as `BuyExecuted`, `SellExecuted`, `TransferCompleted`, `FundsReserved`.
- include `EntityType` and `EntityId` for trade-related entities.
- persist detailed `Details` payloads for later investigation.

## Recommended next steps

1. Create `ITradingService` / `ITransactionService` in `backend/TradingPlatform.Core/Interfaces`.
2. Implement `TradingService` in `backend/TradingPlatform.Core/Services`.
3. Add repository interfaces and EF implementations for transfer, position, instrument, and audit operations if needed.
4. Add user transaction endpoints in `backend/TradingPlatform.Api/Controllers`.
5. Extend `frontend/src/hooks/useTransactions.ts` to consume real transaction history.
6. Add integration tests covering:
   - buy/sell execution
   - account balance and reservation invariants
   - position updates
   - transfer record persistence
   - audit trail creation

## Conclusion

The repo has the right persistence layers and data structures to support a central transaction service, but the actual buy/sell/transfer orchestration is not implemented. A clean design is to introduce a dedicated service layer that reuses `AccountTransferEntity`, `PositionEntity`, and `AuditLogEntity`, and exposes user-facing trading endpoints while enforcing atomic business rules.
