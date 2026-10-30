import React, { useEffect, useRef, useState } from 'react';
import type { CfdChartDrawing, CfdDrawingPoint, CfdDrawingTool } from './CfdChartDrawing';
import './CfdDrawingOverlay.css';

interface CfdDrawingOverlayProps {
  width: number;
  plotWidth: number;
  height: number;
  tool: CfdDrawingTool;
  drawings: CfdChartDrawing[];
  selectedId: string | null;
  timeToX: (time: number) => number | null;
  priceToY: (price: number) => number | null;
  pointAt: (x: number, y: number) => CfdDrawingPoint | null;
  onCreate: (drawing: CfdChartDrawing) => void;
  onSelect: (id: string | null) => void;
  onMove: (id: string, original: CfdDrawingPoint[], deltaTime: number, deltaPrice: number, commit: boolean) => void;
}

interface DrawingDrag {
  id: string;
  start: CfdDrawingPoint;
  last: CfdDrawingPoint;
  original: CfdDrawingPoint[];
}

const formatPriceDelta = (value: number, basePrice: number) => {
  const sign = value > 0 ? '+' : '';
  const percent = basePrice === 0 ? 0 : (value / basePrice) * 100;
  const percentSign = percent > 0 ? '+' : '';
  return `${sign}${value.toLocaleString(undefined, { maximumFractionDigits: 6 })} (${percentSign}${percent.toFixed(2)}%)`;
};

export const CfdDrawingOverlay: React.FC<CfdDrawingOverlayProps> = ({
  width,
  plotWidth,
  height,
  tool,
  drawings,
  selectedId,
  timeToX,
  priceToY,
  pointAt,
  onCreate,
  onSelect,
  onMove,
}) => {
  const drawingStartRef = useRef<CfdDrawingPoint | null>(null);
  const drawingTypeRef = useRef<CfdDrawingTool>('select');
  const dragRef = useRef<DrawingDrag | null>(null);
  const svgRef = useRef<SVGSVGElement>(null);
  const [preview, setPreview] = useState<CfdDrawingPoint[] | null>(null);

  const pointFromEvent = (event: React.PointerEvent): CfdDrawingPoint | null => {
    const bounds = svgRef.current?.getBoundingClientRect();
    return bounds ? pointAt(event.clientX - bounds.left, event.clientY - bounds.top) : null;
  };

  useEffect(() => {
    if (tool !== 'select') return;
    drawingStartRef.current = null;
    dragRef.current = null;
    setPreview(null);
  }, [tool]);

  const onPointerDown = (event: React.PointerEvent<SVGSVGElement>) => {
    if (tool === 'select') {
      if (event.target === event.currentTarget) onSelect(null);
      return;
    }
    const point = pointFromEvent(event);
    if (!point) return;
    event.preventDefault();
    event.currentTarget.setPointerCapture(event.pointerId);
    drawingStartRef.current = point;
    drawingTypeRef.current = tool;
    if (tool === 'horizontal' || tool === 'vertical') {
      onCreate({
        id: crypto.randomUUID(),
        type: tool,
        points: [point],
        locked: false,
      });
      drawingStartRef.current = null;
      return;
    }
    setPreview([point, point]);
  };

  const onPointerMove = (event: React.PointerEvent<SVGSVGElement>) => {
    const drag = dragRef.current;
    const point = pointFromEvent(event);
    if (!point) return;
    if (drag) {
      drag.last = point;
      onMove(drag.id, drag.original, point.time - drag.start.time, point.price - drag.start.price, false);
      return;
    }
    if (drawingStartRef.current) setPreview([drawingStartRef.current, point]);
  };

  const onPointerUp = (event: React.PointerEvent<SVGSVGElement>) => {
    const start = drawingStartRef.current;
    const drag = dragRef.current;
    const end = pointFromEvent(event) ?? drag?.last ?? null;
    if (drag && end) {
      onMove(drag.id, drag.original, end.time - drag.start.time, end.price - drag.start.price, true);
    }
    const type = drawingTypeRef.current;
    if (start && end && type !== 'select' && type !== 'horizontal' && type !== 'vertical') {
      onCreate({ id: crypto.randomUUID(), type, points: [start, end], locked: false });
    }
    drawingStartRef.current = null;
    dragRef.current = null;
    setPreview(null);
  };

  const beginMove = (event: React.PointerEvent<SVGElement>, drawing: CfdChartDrawing) => {
    if (tool !== 'select') return;
    onSelect(drawing.id);
    if (drawing.locked) return;
    const start = pointFromEvent(event);
    if (!start) return;
    event.preventDefault();
    event.stopPropagation();
    event.currentTarget.setPointerCapture(event.pointerId);
    dragRef.current = { id: drawing.id, start, last: start, original: drawing.points };
  };

  const renderDrawing = (drawing: CfdChartDrawing, isPreview = false) => {
    const [first, second] = drawing.points;
    const selected = !isPreview && drawing.id === selectedId;
    const stroke = isPreview ? '#f4b860' : selected ? '#f4b860' : '#9fc7ff';
    const commonProps = {
      stroke,
      strokeWidth: selected ? 2 : 1.5,
      vectorEffect: 'non-scaling-stroke' as const,
      className: 'cfd-drawing-shape',
      onPointerDown: (event: React.PointerEvent<SVGElement>) => {
        if (!isPreview) beginMove(event, drawing);
      },
    };

    if (drawing.type === 'horizontal') {
      const y = priceToY(first.price);
      if (y === null) return null;
      return <line key={drawing.id} {...commonProps} x1={0} x2={plotWidth} y1={y} y2={y} strokeDasharray="7 4" />;
    }
    if (drawing.type === 'vertical') {
      const x = timeToX(first.time);
      if (x === null) return null;
      return <line key={drawing.id} {...commonProps} x1={x} x2={x} y1={0} y2={height} strokeDasharray="7 4" />;
    }
    const firstX = timeToX(first.time);
    const firstY = priceToY(first.price);
    if (firstX === null || firstY === null) return null;
    const secondX = second ? timeToX(second.time) : null;
    const secondY = second ? priceToY(second.price) : null;
    if (secondX === null || secondY === null) return null;
    if (drawing.type === 'trend' || drawing.type === 'ruler') {
      return (
        <g key={drawing.id}>
          <line {...commonProps} x1={firstX} y1={firstY} x2={secondX} y2={secondY} />
          {drawing.type === 'ruler' && (
            <text x={(firstX + secondX) / 2 + 8} y={(firstY + secondY) / 2 - 8} className="cfd-drawing-label">
              {formatPriceDelta(second.price - first.price, first.price)}
            </text>
          )}
        </g>
      );
    }
    return (
      <rect
        key={drawing.id}
        {...commonProps}
        x={Math.min(firstX, secondX)}
        y={Math.min(firstY, secondY)}
        width={Math.abs(secondX - firstX)}
        height={Math.abs(secondY - firstY)}
        fill="rgba(159, 199, 255, 0.08)"
      />
    );
  };

  const previewDrawing = preview && drawingTypeRef.current !== 'select'
    ? { id: 'preview', type: drawingTypeRef.current, points: preview, locked: false } as CfdChartDrawing
    : null;

  return (
    <svg
      ref={svgRef}
      className={`cfd-drawing-overlay ${tool === 'select' ? 'is-select' : 'is-drawing'}`}
      width={width}
      height={height}
      viewBox={`0 0 ${width} ${height}`}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={onPointerUp}
      onPointerCancel={() => {
        drawingStartRef.current = null;
        dragRef.current = null;
        setPreview(null);
      }}
      onDoubleClick={(event) => {
        if (event.target === event.currentTarget) onSelect(null);
      }}
      aria-label="Warstwa rysunków wykresu"
    >
      {drawings.map((drawing) => renderDrawing(drawing))}
      {previewDrawing && renderDrawing(previewDrawing, true)}
    </svg>
  );
};
