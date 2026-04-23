import { httpClient } from '../http/HttpClient';
import { API_CONFIG } from '../../config/apiConfig';
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
    url: API_CONFIG.endpoints.adminInstruments.all,
    method: 'GET',
  });
};

/**
 * GET /api/admin/instruments/{id}
 * Get single instrument by ID
 */
export const getById = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: API_CONFIG.endpoints.adminInstruments.byId(id),
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
    url: API_CONFIG.endpoints.adminInstruments.create,
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
    url: API_CONFIG.endpoints.adminInstruments.byId(id),
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
    url: API_CONFIG.endpoints.adminInstruments.byId(id),
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
    url: API_CONFIG.endpoints.adminInstruments.requestUpdate(id),
    method: 'POST',
  });
};

/**
 * POST /api/admin/requests/{id}/approve
 * Approve pending admin request (instrument workflow)
 * Transition: PendingApproval → Approved
 * Backend validates: approver ≠ creator (no self-approval)
 */
export const approve = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: API_CONFIG.endpoints.adminRequests.approve(id),
    method: 'POST',
  });
};

/**
 * POST /api/admin/requests/{id}/reject
 * Reject pending admin request (instrument workflow) with reason
 * Transition: PendingApproval → Rejected
 * Backend validates: approver ≠ creator, reason ≥10 chars
 */
export const reject = async (
  id: string,
  request: RejectInstrumentRequest
): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: API_CONFIG.endpoints.adminRequests.reject(id),
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
    url: API_CONFIG.endpoints.adminInstruments.requestUpdate(id),
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
    url: API_CONFIG.endpoints.adminInstruments.requestDelete(id),
    method: 'POST',
  });
};

// ============ ADMINISTRATIVE OPERATIONS ============

/**
 * POST /api/admin/instruments/{id}/request-block
 * Request block of instrument (administrative override, prevent trading)
 * Sets IsBlocked=true after approval
 */
export const block = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: API_CONFIG.endpoints.adminInstruments.requestBlock(id),
    method: 'POST',
  });
};

/**
 * POST /api/admin/instruments/{id}/request-unblock
 * Request unblock of instrument (administrative override)
 * Sets IsBlocked=false after approval
 */
export const unblock = async (id: string): Promise<Instrument> => {
  return httpClient.fetch<Instrument>({
    url: API_CONFIG.endpoints.adminInstruments.requestUnblock(id),
    method: 'POST',
  });
};

// ============ ADMIN REQUEST (AUDIT TRAIL) ============

/**
 * GET /api/admin/requests
 * Get all admin requests (audit trail for instruments)
 */
export const getAllAdminRequests = async (): Promise<AdminRequest[]> => {
  return httpClient.fetch<AdminRequest[]>({
    url: API_CONFIG.endpoints.adminRequests.all,
    method: 'GET',
  });
};

/**
 * GET /api/admin/requests/pending
 * Get pending admin requests (not yet approved)
 */
export const getPendingAdminRequests = async (): Promise<AdminRequest[]> => {
  return httpClient.fetch<AdminRequest[]>({
    url: API_CONFIG.endpoints.adminRequests.pending,
    method: 'GET',
  });
};

/**
 * GET /api/admin/requests/{id}
 * Get single admin request by ID
 */
export const getAdminRequestById = async (id: string): Promise<AdminRequestDto> => {
  return httpClient.fetch<AdminRequestDto>({
    url: API_CONFIG.endpoints.adminRequests.byId(id),
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
