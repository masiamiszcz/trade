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

// === DATA TRANSFER OBJECTS (DTOs) ===

export const ACCOUNT_PILLARS = [
  'General',
  'Stocks',
  'Crypto',
  'Cfd',
] as const;

export type AccountPillar = typeof ACCOUNT_PILLARS[number];

export type UserRole = 'User' | 'Admin' | 'SuperAdmin';

// === INSTRUMENT (FULL DTO) ===
export interface Instrument {
  id: string;
  symbol: string;              // e.g., "AAPL", "BTC", "SPY"
  name: string;                // e.g., "Apple Inc."
  description: string;         // Admin notes
  type: InstrumentType;        // Stock|Crypto|Cfd|Etf|Forex
  pillar: AccountPillar;       // General|Stocks|Crypto|Cfd
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
  entityType: string;              // Type of entity (e.g., "Instrument")
  entityId?: string | null;        // ID of entity, nullable for Create operations
  requestedByAdminId: string;      // Admin who made the request
  approvedByAdminId?: string | null; // Admin who approved it
  action: AdminRequestActionType;  // Create, Update, Delete, Block, Unblock
  status: AdminRequestStatus;      // Pending|Approved|Rejected
  reason?: string;                 // Rejection reason or metadata
  createdAtUtc: string;            // ISO datetime
  approvedAtUtc?: string | null;   // ISO datetime when approved
}

// === ADMIN RESPONSE DTOs ===
export interface AdminRequestDto {
  id: string;
  entityType: string;
  entityId?: string | null;
  requestedByAdminId: string;
  approvedByAdminId?: string | null;
  action: AdminRequestActionType;
  status: AdminRequestStatus;
  reason?: string;
  createdAtUtc: string;
  approvedAtUtc?: string | null;
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
  adminName?: string;           // ← From old API
  adminUserName?: string;        // ← From new API
  action: string;
  entityType?: string;           // ← Optional (new API doesn't have this)
  entityId?: string;             // ← Optional (new API doesn't have this)
  details?: Record<string, any>;
  ipAddress: string;
  createdAt?: string;
  createdAtUtc?: string;         // ← New API uses this
  userAgent?: string;            // ← New API has this
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
