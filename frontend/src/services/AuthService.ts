/**
 * @deprecated Use AuthenticationService instead
 * This file is kept for backwards compatibility only
 * 
 * New import: import { authService } from './AuthenticationService';
 */

export { authService } from './AuthenticationService';

// Re-export for backwards compatibility
import { authService as newAuthService } from './AuthenticationService';
export default newAuthService;
