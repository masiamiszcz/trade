import { API_CONFIG } from '../config/apiConfig';
import type { CfdChartDrawing } from '../components/cfd/CfdChartDrawing';
import { httpClient } from './http/HttpClient';

export interface CfdChartViewConfiguration {
  version: 1;
  symbols: string[];
  highlightedSymbol: string;
  rangeMinutes: number;
  intervalMinutes: number;
  chartType: 'candles' | 'line' | 'ohlc';
  drawings: CfdChartDrawing[];
}

export interface CfdChartView {
  id: string;
  name: string;
  configuration: CfdChartViewConfiguration;
  createdAtUtc: string;
  updatedAtUtc: string;
}

class CfdChartViewService {
  getViews(): Promise<CfdChartView[]> {
    return httpClient.fetch<CfdChartView[]>({
      url: API_CONFIG.endpoints.cfd.views,
      method: 'GET',
    });
  }

  getView(id: string): Promise<CfdChartView> {
    return httpClient.fetch<CfdChartView>({
      url: API_CONFIG.endpoints.cfd.view(id),
      method: 'GET',
    });
  }

  createView(name: string, configuration: CfdChartViewConfiguration): Promise<CfdChartView> {
    return httpClient.fetch<CfdChartView>({
      url: API_CONFIG.endpoints.cfd.views,
      method: 'POST',
      body: JSON.stringify({ name, configuration }),
    });
  }

  async deleteView(id: string): Promise<void> {
    await httpClient.fetch<void>({
      url: API_CONFIG.endpoints.cfd.view(id),
      method: 'DELETE',
    });
  }
}

export const cfdChartViewService = new CfdChartViewService();
