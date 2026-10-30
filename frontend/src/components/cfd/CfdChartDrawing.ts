export type CfdDrawingTool = 'select' | 'horizontal' | 'vertical' | 'trend' | 'rectangle' | 'ruler';

export interface CfdDrawingPoint {
  time: number;
  price: number;
}

export interface CfdChartDrawing {
  id: string;
  type: Exclude<CfdDrawingTool, 'select'>;
  points: CfdDrawingPoint[];
  locked: boolean;
}
