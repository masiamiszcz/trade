import { useCallback, useEffect, useState } from 'react';
import { cfdChartViewService } from '../services/CfdChartViewService';
import type { CfdChartView } from '../services/CfdChartViewService';

export function useCfdChartViews() {
  const [views, setViews] = useState<CfdChartView[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setViews(await cfdChartViewService.getViews());
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : String(loadError));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const remove = useCallback(async (id: string) => {
    await cfdChartViewService.deleteView(id);
    setViews((previous) => previous.filter((view) => view.id !== id));
  }, []);

  return { views, loading, error, refresh, remove };
}
