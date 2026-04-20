/**
 * @deprecated Use MarketDataService instead
 * This file is kept for backwards compatibility only
 * 
 * New import: import { marketDataService } from './MarketDataService';
 */

export { marketDataService } from './MarketDataService';

// Re-export for backwards compatibility
import { marketDataService as newMarketDataService } from './MarketDataService';
export default newMarketDataService;