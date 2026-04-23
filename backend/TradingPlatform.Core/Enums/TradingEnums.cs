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

/// <summary>
/// Instrument Workflow Status - Canonical source of truth
/// Enterprise rule: One status, not scattered booleans
/// </summary>
public enum InstrumentStatus
{
    Draft = 1,              // Admin edytuje, nie publikowany
    PendingApproval = 2,    // Oczekujący zatwierdzenia przez innego admina
    Approved = 3,           // Zatwierdzony, dostępny dla userów
    Rejected = 4,           // Odrzucony, admin może edytować i wysłać ponownie
    Blocked = 5,            // Zablokowany - nie może być kupowany/sprzedawany
    Archived = 6            // Archiwizowany (soft deleted)
}

/// <summary>
/// Admin Request Action Type - Workflow transitions
/// Part of FAZA 3 State Machine Engine
/// </summary>
public enum AdminRequestActionType
{
    Create = 1,             // Initial creation (audit only)
    RequestApproval = 2,    // Draft → PendingApproval (admin requests review)
    Update = 3,             // Update instrument (requires approval)
    Delete = 4,             // Delete instrument (requires approval)
    Approve = 5,            // PendingApproval → Approved (admin approves)
    Reject = 6,             // PendingApproval → Rejected (admin rejects)
    Block = 7,              // Approved → Blocked (admin blocks trading)
    Unblock = 8,            // Blocked → Approved (admin unblocks trading)
    Archive = 9,            // Approved → Archived (admin archives)
    RetrySubmission = 10    // Rejected → Draft (admin/creator resubmits)
}

/// <summary>
/// Admin Request Status - Workflow state for approval requests
/// Part of FAZA 3 State Machine Engine
/// </summary>
public enum AdminRequestStatus
{
    Pending = 1,            // Awaiting approval decision
    Approved = 2,           // Request approved (action succeeded)
    Rejected = 3            // Request rejected (action declined or failed)
}
