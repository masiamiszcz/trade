// Admin Types
// Admin Panel Types

export type AdminRequestType = 'Create' | 'Update' | 'Delete' | 'Approve';
export type AdminRequestStatus = 'pending' | 'approved' | 'rejected';
export type InstrumentType = 'Forex' | 'Commodity' | 'Crypto' | 'Stock' | 'CFD';
export type InstrumentStatus = 'draft' | 'pending' | 'approved' | 'rejected';
export type UserRole = 'User' | 'Admin';

// Admin Request (do zatwierdzenia)
export interface AdminRequest {
  id: string;
  requestedBy: string;
  action: AdminRequestType;
  status: AdminRequestStatus;
  entityType: string;
  entityId: string;
  details: Record<string, any>;
  reason?: string;
  approvedBy?: string;
  createdAt: string;
  approvedAt?: string;
}

// Instrument
export interface Instrument {
  id: string;
  name: string;
  symbol: string;
  type: InstrumentType;
  description?: string;
  status: InstrumentStatus;
  createdBy: string;
  createdAt: string;
  submittedAt?: string;
  rejectionReason?: string;
  isActive: boolean;
}

// Health Status
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

// Audit Log
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

// Admin User
export interface AdminUser {
  id: string;
  username: string;
  email: string;
  role: UserRole;
  createdAt: string;
  lastLogin?: string;
}

// Pagination
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

// API Responses
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}
