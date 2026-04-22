import { httpClient } from '../http/HttpClient';
import {
  Instrument,
  CreateInstrumentRequest,
  UpdateInstrumentRequest,
  RejectInstrumentRequest,
  AdminRequest,
  AdminRequestDto,
} from '../../types/admin';

/**
 * INSTRUMENTS SERVICE
 * 
 * Pure HTTP client — NO domain logic, NO state management.
 * All business rules validated by backend.
 * Maps backend responses to frontend types.
 * 
 * SINGLE RESPONSIBILITY: API communication only
 */

// ============ CRUD OPERATIONS ============

/**
 * GET /api/admin/instruments
 * Returns ALL instruments (all statuses) for admin management
 */
export const getAll = async (): Promise<Instrument[]> => {
  return httpClient.fetch<Instrument[]>({
    url: '/admin/instruments',
    method: 'GET',
  });
};

/**
 * GET /api/admin/instruments/{id}
 * Get single instrument by ID
 */
export const getById = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}`,
    method: 'GET',
  });
};

/**
 * POST /api/admin/instruments
 * Create new instrument
 * Status will be set to "Draft" by backend
 * CreatedBy will be set to current admin from JWT token
 */
export const create = async (request: CreateInstrumentRequest): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: '/admin/instruments',
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
};

/**
 * PUT /api/admin/instruments/{id}
 * Update existing instrument
 * Only updates provided fields (partial update)
 * ModifiedBy will be set to current admin
 */
export const update = async (id: string, request: UpdateInstrumentRequest): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}`,
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
};

/**
 * DELETE /api/admin/instruments/{id}
 * Permanently delete instrument from database
 * Only works on Draft status (business rule enforced by backend)
 */
export const delete_ = async (id: string): Promise<void> => {
  await httpClient.fetch({
    url: `/admin/instruments/${id}`,
    method: 'DELETE',
  });
};

// ============ WORKFLOW STATE MACHINE OPERATIONS ============
// All transitions validated by ValidateTransition() on backend

/**
 * POST /api/admin/instruments/{id}/request-approval
 * Request approval for instrument
 * Transition: Draft → PendingApproval
 * Backend validates: description not empty
 */
export const requestApproval = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}/request-approval`,
    method: 'POST',
  });
};

/**
 * POST /api/admin/instruments/{id}/approve
 * Approve pending instrument
 * Transition: PendingApproval → Approved
 * Backend validates: approver ≠ creator (no self-approval)
 */
export const approve = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}/approve`,
    method: 'POST',
  });
};

/**
 * POST /api/admin/instruments/{id}/reject
 * Reject pending instrument with reason
 * Transition: PendingApproval → Rejected
 * Backend validates: approver ≠ creator, reason ≥10 chars
 */
export const reject = async (
  id: string,
  request: RejectInstrumentRequest
): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}/reject`,
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
};

/**
 * POST /api/admin/instruments/{id}/retry-submission
 * Retry rejected instrument (move back to Draft for re-editing)
 * Transition: Rejected → Draft
 */
export const retrySubmission = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}/retry-submission`,
    method: 'POST',
  });
};

/**
 * POST /api/admin/instruments/{id}/archive
 * Archive approved instrument (soft delete - not removed from DB)
 * Transition: Approved → Archived
 */
export const archive = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}/archive`,
    method: 'POST',
  });
};

// ============ ADMINISTRATIVE OPERATIONS ============

/**
 * POST /api/admin/instruments/{id}/block
 * Block instrument (administrative override, prevent trading)
 * Sets IsBlocked=true
 */
export const block = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}/block`,
    method: 'POST',
  });
};

/**
 * POST /api/admin/instruments/{id}/unblock
 * Unblock instrument (administrative override)
 * Sets IsBlocked=false
 */
export const unblock = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: `/admin/instruments/${id}/unblock`,
    method: 'POST',
  });
};

// ============ ADMIN REQUEST (AUDIT TRAIL) ============

/**
 * GET /api/admin/admin-requests
 * Get all admin requests (audit trail for instruments)
 */
export const getAllAdminRequests = async (): Promise<AdminRequest[]> => {
  return httpClient.fetch<AdminRequest[]>({
    url: '/admin/admin-requests',
    method: 'GET',
  });
};

/**
 * GET /api/admin/admin-requests/pending
 * Get pending admin requests (not yet approved)
 */
export const getPendingAdminRequests = async (): Promise<AdminRequest[]> => {
  return httpClient.fetch<AdminRequest[]>({
    url: '/admin/admin-requests/pending',
    method: 'GET',
  });
};

/**
 * GET /api/admin/admin-requests/{id}
 * Get single admin request by ID
 */
export const getAdminRequestById = async (id: string): Promise<AdminRequestDto> => {
  return httpClient.fetch<AdminRequestDto>({
    url: `/admin/admin-requests/${id}`,
    method: 'GET',
  });
};

// ============ EXPORT AS SERVICE OBJECT ============
export const instrumentsService = {
  // CRUD
  getAll,
  getById,
  create,
  update,
  delete: delete_,

  // Workflow (state machine)
  requestApproval,
  approve,
  reject,
  retrySubmission,
  archive,

  // Administrative
  block,
  unblock,

  // Audit trail
  getAllAdminRequests,
  getPendingAdminRequests,
  getAdminRequestById,
};
