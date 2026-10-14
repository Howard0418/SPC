export function useSingleChemicalLine(mapping) {
  const scope = String(
    mapping?.controlScope
    ?? mapping?.scope
    ?? mapping?.controlScopeName
    ?? mapping?.scopeName
    ?? mapping?.category
    ?? ''
  ).trim().toUpperCase();
  const machineCode = String(
    mapping?.machine?.machineCode
    ?? mapping?.machine?.machineName
    ?? mapping?.machineCode
    ?? mapping?.machineName
    ?? ''
  ).trim().toUpperCase();
  const isChemical = scope === 'CHEM' || scope === 'CHEMICAL' || scope.includes('藥液');
  return isChemical && ['N1', 'N2'].includes(machineCode);
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
  return ({ OPEN: '早班(開)', MIDDLE: '中(開)', CLOSE: '早(收)' })[normalizeChemicalShift(phase)];
}

export function chartStageShiftLabel(phase, stage, isN1N2 = false) {
  if (!isN1N2) return shiftLabel(phase);
  const normalizedPhase = normalizeChemicalShift(phase);
  const normalizedStage = String(stage ?? '').trim().toUpperCase();
  if (normalizedPhase === 'MIDDLE' && normalizedStage === 'CLOSE') return '中(收)';
  return chartShiftLabel(normalizedPhase, true);
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
