export function useSingleChemicalLine(mapping) {
  return mapping?.controlScope === 'CHEM' && ['N1', 'N2'].includes(mapping?.machine?.machineCode);
}

export function stageLabel(stage) {
  return ({ OPEN: '開線', CLOSE: '收線' })[stage] || '未分階段';
}

export function normalizeChemicalShift(phase) {
  const value = String(phase ?? '').trim().toUpperCase();
  if (value === 'MIDDLE' || value === 'CLOSE') return value;
  return 'OPEN';
}

export function isRecordedChemicalShift(phase) {
  const value = String(phase ?? '').trim().toUpperCase();
  return value === 'OPEN' || value === 'MIDDLE' || value === 'CLOSE';
}

export function shiftLabel(phase) {
  return ({ OPEN: '早班', MIDDLE: '中班', CLOSE: '晚班' })[normalizeChemicalShift(phase)];
}

export function chartShiftLabel(phase, isN1N2 = false) {
  if (!isN1N2) return shiftLabel(phase);
  return ({ OPEN: '早班開線', MIDDLE: '中班', CLOSE: '早班收線' })[normalizeChemicalShift(phase)];
}

export function chemicalDateKey(point) {
  const raw = point?.portalDailyDate || point?.measuredAt;
  if (!raw) return '';
  const date = new Date(raw);
  if (Number.isNaN(date.getTime())) return String(raw).slice(0, 10);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function buildPairedShiftLookup(points) {
  const sorted = [...(points || [])].sort((a, b) => {
    const left = String(a?.measuredAt || a?.portalDailyDate || '');
    const right = String(b?.measuredAt || b?.portalDailyDate || '');
    return left.localeCompare(right) || Number(a?.variableMeasurementId || 0) - Number(b?.variableMeasurementId || 0);
  });
  const lookup = new Map();
  const dates = new Set();
  for (const point of sorted) {
    const date = chemicalDateKey(point);
    if (!date) continue;
    dates.add(date);
    lookup.set(`${date}:${normalizeChemicalShift(point.samplingPhase)}`, point);
  }
  return { dates: [...dates].sort(), lookup };
}
