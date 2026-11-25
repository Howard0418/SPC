export const particleLocations = Object.freeze(Array.from({ length: 12 }, (_, index) => `R${index + 1}`));
export const particleSizes = Object.freeze(['0.5', '1', '5', '10']);

const acceptedColumnCounts = new Set([49, 50, 53]);

function columnName(index) {
  let result = '';
  for (let value = index + 1; value > 0; value = Math.floor((value - 1) / 26))
    result = String.fromCharCode(65 + ((value - 1) % 26)) + result;
  return result;
}

function normalizeCell(value) {
  return String(value ?? '').replace(/\u00a0/g, ' ').trim();
}

function parseTsv(text) {
  const rows = [];
  let row = [];
  let cell = '';
  let quoted = false;
  let malformed = false;
  const source = String(text ?? '');
  for (let index = 0; index < source.length; index++) {
    const character = source[index];
    if (quoted) {
      if (character === '"') {
        if (source[index + 1] === '"') { cell += '"'; index++; }
        else quoted = false;
      } else if (character === '\r') {
        cell += '\n';
        if (source[index + 1] === '\n') index++;
      } else cell += character;
      continue;
    }
    if (character === '"' && cell === '') { quoted = true; continue; }
    if (character === '\t') { row.push(cell); cell = ''; continue; }
    if (character === '\r' || character === '\n') {
      row.push(cell); rows.push(row); row = []; cell = '';
      if (character === '\r' && source[index + 1] === '\n') index++;
      continue;
    }
    cell += character;
  }
  if (quoted) malformed = true;
  row.push(cell);
  rows.push(row);
  while (rows.length && rows.at(-1).length === 1 && rows.at(-1)[0] === '') rows.pop();
  return { rows, malformed };
}

function normalizeSizeHeader(value) {
  return normalizeCell(value).toLowerCase().replace(/µ/g, 'u').replace(/\s+/g, '').replace(/um$/, '');
}

function matchesHeaders(first, second) {
  if (!acceptedColumnCounts.has(first.length) || first.length !== second.length) return false;
  const firstCell = normalizeCell(first[0]);
  if (!['量測時間', '日期時間\\位置', '日期\\時間\\位置'].includes(firstCell)) return false;
  for (let locationIndex = 0; locationIndex < particleLocations.length; locationIndex++) {
    const start = 1 + locationIndex * 4;
    const location = particleLocations[locationIndex];
    const group = first.slice(start, start + 4).map(normalizeCell);
    if (group[0] !== location || group.slice(1).some(value => value !== '' && value !== location)) return false;
    const sizeGroup = second.slice(start, start + 4).map(normalizeSizeHeader);
    if (!sizeGroup.every((value, index) => value === particleSizes[index])) return false;
  }
  if (first.length > 49 && normalizeCell(first[49]) !== '備註') return false;
  return true;
}

function parseTaiwanDateTime(value) {
  const match = normalizeCell(value).match(/^(\d{4})[\/-](\d{1,2})[\/-](\d{1,2})[ T\r\n]+(\d{1,2}):(\d{2})(?::(\d{2}))?$/);
  if (!match) return null;
  const [, y, m, d, h, minute, second = '00'] = match;
  const parts = [Number(y), Number(m), Number(d), Number(h), Number(minute), Number(second)];
  const probe = new Date(Date.UTC(parts[0], parts[1] - 1, parts[2], parts[3], parts[4], parts[5]));
  if (probe.getUTCFullYear() !== parts[0] || probe.getUTCMonth() !== parts[1] - 1 || probe.getUTCDate() !== parts[2]
      || parts[3] > 23 || parts[4] > 59 || parts[5] > 59) return null;
  const pad = number => String(number).padStart(2, '0');
  return `${parts[0]}-${pad(parts[1])}-${pad(parts[2])}T${pad(parts[3])}:${pad(parts[4])}:${pad(parts[5])}+08:00`;
}

export function parseParticlePaste(text) {
  const { rows: matrix, malformed } = parseTsv(text);
  if (!matrix.length) return { dataRows: [], missingCount: 0, errors: [{ code: 'EMPTY', row: 3, column: 'A' }] };
  const widths = new Set(matrix.map(row => row.length));
  const errors = [];
  if (malformed || widths.size > 1) errors.push({ code: 'NON_RECTANGULAR', row: 3, column: 'A' });

  let dataStart = 0;
  if (['量測時間', '日期時間\\位置', '日期\\時間\\位置'].includes(normalizeCell(matrix[0]?.[0]))) {
    if (matrix.length < 2 || !matchesHeaders(matrix[0], matrix[1]))
      errors.push({ code: 'HEADER_MISMATCH', row: 1, column: 'A' });
    dataStart = 2;
  }
  const data = matrix.slice(dataStart);
  for (let index = 0; index < data.length; index++) {
    if (!acceptedColumnCounts.has(data[index].length))
      errors.push({ code: 'COLUMN_COUNT', row: index + 3, column: 'A', expected: '49、50 或 53', actual: data[index].length });
  }
  if (errors.length) return { dataRows: [], missingCount: 0, errors };

  let missingCount = 0;
  const dataRows = data.map((cells, rowIndex) => {
    const rawMeasurementTime = normalizeCell(cells[0]);
    const measurementTime = parseTaiwanDateTime(rawMeasurementTime);
    if (!measurementTime) errors.push({ code: 'TIME_INVALID', row: rowIndex + 3, column: 'A' });
    const counts = cells.slice(1, 49).map((cell, cellIndex) => {
      const value = normalizeCell(cell);
      if (value === '') { missingCount++; return null; }
      if (!/^\d+$/.test(value)) errors.push({ code: 'COUNT_INVALID', row: rowIndex + 3, column: columnName(cellIndex + 1) });
      return value;
    });
    const remarkParts = cells.slice(49).map(normalizeCell).filter(Boolean);
    const remark = remarkParts.length ? remarkParts.join(' / ') : null;
    if (remark?.length > 500) errors.push({ code: 'REMARK_TOO_LONG', row: rowIndex + 3, column: 'AX' });
    return { measurementTime, rawMeasurementTime, counts, remark, sourceRow: rowIndex + 3 };
  });
  return errors.length ? { dataRows: [], missingCount, errors } : { dataRows, missingCount, errors: [] };
}

export function buildParticlePreviewRows(dataRows) {
  return dataRows.flatMap(row => row.counts.flatMap((count, index) => count === null ? [] : [{
    measurementTime: row.measurementTime,
    location: particleLocations[Math.floor(index / 4)],
    particleSize: particleSizes[index % 4],
    count,
    samplingVolume: null,
    samplingDurationSeconds: null,
    deviceCode: null,
    remark: row.remark ?? null,
    sourceSheet: 'WebPaste',
    sourceRow: row.sourceRow,
    sourceColumn: columnName(index + 1),
    rawValue: count,
    samplingVolumeUnit: null
  }]));
}

function encodeTsvCell(value) {
  const normalized = String(value ?? '');
  return /["\t\r\n]/.test(normalized) ? `"${normalized.replace(/"/g, '""')}"` : normalized;
}

export function serializeParticleRows(dataRows) {
  return dataRows.map(row => [
    row.rawMeasurementTime ?? row.measurementTime ?? '',
    ...row.counts.map(value => value ?? ''),
    row.remark ?? ''
  ].map(encodeTsvCell).join('\t')).join('\r\n');
}

export async function sha256Hex(text) {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(String(text)));
  return [...new Uint8Array(digest)].map(value => value.toString(16).padStart(2, '0')).join('').toUpperCase();
}
