export const CFD_RANGE_OPTIONS = [
  { label: '5m', value: 5 },
  { label: '15m', value: 15 },
  { label: '30m', value: 30 },
  { label: '1h', value: 60 },
  { label: '2h', value: 120 },
  { label: '4h', value: 240 },
  { label: '6h', value: 360 },
  { label: '12h', value: 720 },
  { label: '1d', value: 1440 },
  { label: '7d', value: 10080 },
  { label: '30d', value: 43200 },
  { label: '90d', value: 129600 },
  { label: '1y', value: 525600 },
];

export const CFD_INTERVAL_OPTIONS = [
  { label: '1m', value: 1 },
  { label: '5m', value: 5 },
  { label: '15m', value: 15 },
  { label: '30m', value: 30 },
  { label: '1h', value: 60 },
  { label: '2h', value: 120 },
  { label: '4h', value: 240 },
  { label: '1d', value: 1440 },
];

const resolveMinimumIntervalMinutes = (rangeMinutes: number): number => {
  const day = 1440;
  const year = 525600;
  if (rangeMinutes <= day) return 1;
  if (rangeMinutes <= 7 * day) return 5;
  if (rangeMinutes <= 14 * day) return 15;
  if (rangeMinutes <= 30 * day) return 30;
  if (rangeMinutes <= year) return 60;
  return 1440;
};

export function getAllowedCfdIntervals(rangeMinutes: number) {
  const minimumInterval = resolveMinimumIntervalMinutes(rangeMinutes);
  return CFD_INTERVAL_OPTIONS.filter(
    (option) => option.value >= minimumInterval && option.value <= rangeMinutes,
  );
}

export function getCfdRangeLabel(rangeMinutes: number): string {
  return CFD_RANGE_OPTIONS.find((option) => option.value === rangeMinutes)?.label ?? '1h';
}

export function getCfdIntervalLabel(intervalMinutes: number): string {
  if (intervalMinutes >= 1440) return `${intervalMinutes / 1440}d`;
  if (intervalMinutes >= 60) return `${intervalMinutes / 60}h`;
  return `${intervalMinutes}m`;
}
