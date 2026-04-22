// Admin Types
// Admin Panel Types

// === BACKEND-ALIGNED ENUMS ===

// InstrumentType (matches backend enum)
export type InstrumentType = 'Stock' | 'Crypto' | 'Cfd' | 'Etf' | 'Forex';

// InstrumentStatus (matches backend enum - single source of truth)
export type InstrumentStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'Rejected' | 'Blocked' | 'Archived';

// AdminRequestActionType (matches backend enum)
export type AdminRequestActionType = 
  | 'Create' 
  | 'RequestApproval' 
  | 'Approve' 
  | 'Reject' 
  | 'Block' 
  | 'Unblock' 
  | 'Archive' 
  | 'RetrySubmission';

// AdminRequestStatus (matches backend - 3 states)
export type AdminRequestStatus = 'Pending' | 'Approved' | 'Rejected';

// AccountPillar (if needed on frontend)
export type AccountPillar = 'Primary' | 'Secondary' | 'Trading' | 'Investment';

export type UserRole = 'User' | 'Admin' | 'SuperAdmin';

// === INSTRUMENT (FULL DTO) ===
export interface Instrument {
  id: string;
  symbol: string;              // e.g., "AAPL", "BTC", "SPY"
  name: string;                // e.g., "Apple Inc."
  description: string;         // Admin notes
  type: InstrumentType;        // Stock|Crypto|Cfd|Etf|Forex
  pillar: AccountPillar;       // Primary|Secondary|Trading|Investment
  baseCurrency: string;        // e.g., "USD", "EUR"
  quoteCurrency: string;       // e.g., "USD", "EUR"
  status: InstrumentStatus;    // Draft|PendingApproval|Approved|Rejected|Blocked|Archived
  isActive: boolean;           // logical delete flag
  isBlocked: boolean;          // admin override block
  createdBy: string;           // admin ID who created
  createdAtUtc: string;        // ISO datetime
  modifiedBy?: string;         // admin ID who last modified
  modifiedAtUtc?: string;      // ISO datetime (null if never modified)
}

// === CREATE/UPDATE DTOs ===
export interface CreateInstrumentRequest {
  symbol: string;              // required, uppercase enforced by backend
  name: string;                // required
  description?: string;        // optional, can be empty initially
  type: string;                // send as string, backend parses enum
  pillar: string;              // send as string, backend parses enum
  baseCurrency: string;        // required
  quoteCurrency: string;       // required
}

export interface UpdateInstrumentRequest {
  name?: string;               // optional field
  description?: string;        // optional field
  baseCurrency?: string;       // optional field
  quoteCurrency?: string;      // optional field
}

export interface RejectInstrumentRequest {
  reason: string;              // required, min 10 chars (validated by backend)
}

// === ADMIN REQUEST (AUDIT LOG) ===
export interface AdminRequest {
  id: string;
  instrumentId: string;
  requestedByAdminId: string;
  approvedByAdminId?: string;
  action: AdminRequestActionType;
  status: AdminRequestStatus;  // Pending|Approved|Rejected (confusing - means "request status", not instrument status)
  reason?: string;             // rejection reason or other metadata
  createdAtUtc: string;        // ISO datetime
  approvedAtUtc?: string;      // ISO datetime when approved
}

// === ADMIN RESPONSE DTOs ===
export interface AdminRequestDto {
  id: string;
  instrumentId: string;
  requestedByAdminId: string;
  approvedByAdminId?: string;
  action: AdminRequestActionType;
  status: AdminRequestStatus;
  reason?: string;
  createdAtUtc: string;
  approvedAtUtc?: string;
}

// === HEALTH STATUS ===
export interface HealthStatus {
  status: 'Healthy' | 'Unhealthy';
  lastCheck: string;
  uptime: number;
  message?: string;
  dependencies?: {
    database: 'Healthy' | 'Unhealthy';
    cache?: 'Healthy' | 'Unhealthy';
  };
}

// === AUDIT LOG ===
export interface AuditLog {
  id: string;
  adminId: string;
  adminName: string;
  action: string;
  entityType: string;
  entityId: string;
  details: Record<string, any>;
  ipAddress: string;
  createdAt: string;
}

// === ADMIN USER ===
export interface AdminUser {
  id: string;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  role: UserRole;
  createdAt: string;
  lastLogin?: string;
}

// === PAGINATION ===
export interface PaginationParams {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageSize: number;
  currentPage: number;
  totalPages: number;
}

// === API RESPONSE WRAPPER ===
export interface ApiResponse<T> {
  data?: T;
  success: boolean;
  message?: string;
  error?: string;
}
