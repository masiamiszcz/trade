namespace TradingPlatform.Core.Enums;

public enum UserStatus
{
    PendingEmailConfirmation = 1,
    Active = 2,
    Suspended = 3,
    Locked = 4
}

public enum UserRole
{
    User = 1,
    Admin = 2
}

public enum AccountType
{
    Main = 1,
    Subaccount = 2
}

public enum AccountPillar
{
    General = 1,
    Stocks = 2,
    Crypto = 3,
    Cfd = 4
}

public enum AccountStatus
{
    Active = 1,
    Suspended = 2,
    Closed = 3
}

public enum InstrumentType
{
    Stock = 1,
    Crypto = 2,
    Cfd = 3,
    Etf = 4,
    Forex = 5
}

public enum TransferType
{
    Deposit = 1,
    Withdrawal = 2,
    Internal = 3,
    Adjustment = 4
}

public enum TransferStatus
{
    Pending = 1,
    Completed = 2,
    Rejected = 3,
    Cancelled = 4
}
