export const particleLocations = Object.freeze(Array.from({ length: 12 }, (_, index) => `R${index + 1}`));
export const particleSizes = Object.freeze(['0.5', '1', '5', '10']);

const expectedHeader1 = ['量測時間', ...particleLocations.flatMap(location => Array(4).fill(location))];
const expectedHeader2 = ['', ...particleLocations.flatMap(() => particleSizes.map(size => `${size} µm`))];

function columnName(index) {
  let result = '';
  for (let value = index + 1; value > 0; value = Math.floor((value - 1) / 26))
    result = String.fromCharCode(65 + ((value - 1) % 26)) + result;
  return result;
}

function normalizeCell(value) {
  return String(value ?? '').replace(/\u00a0/g, ' ').trim();
}

function sameHeader(actual, expected) {
  return actual.length === expected.length && actual.every((cell, index) => normalizeCell(cell) === expected[index]);
}

function parseTaiwanDateTime(value) {
  const match = normalizeCell(value).match(/^(\d{4})[\/-](\d{1,2})[\/-](\d{1,2})[ T](\d{1,2}):(\d{2})(?::(\d{2}))?$/);
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
  const normalized = String(text ?? '').replace(/\r\n?/g, '\n');
  const lines = normalized.split('\n');
  while (lines.length && lines.at(-1) === '') lines.pop();
  if (!lines.length) return { dataRows: [], missingCount: 0, errors: [{ code: 'EMPTY', row: 3, column: 'A' }] };
  const matrix = lines.map(line => line.split('\t'));
  const widths = new Set(matrix.map(row => row.length));
  const errors = [];
  if (widths.size > 1) errors.push({ code: 'NON_RECTANGULAR', row: 3, column: 'A' });

  let dataStart = 0;
  if (normalizeCell(matrix[0]?.[0]) === '量測時間') {
    if (matrix.length < 2 || !sameHeader(matrix[0], expectedHeader1) || !sameHeader(matrix[1], expectedHeader2))
      errors.push({ code: 'HEADER_MISMATCH', row: 1, column: 'A' });
    dataStart = 2;
  }
  const data = matrix.slice(dataStart);
  for (let index = 0; index < data.length; index++) {
    if (data[index].length !== 49) errors.push({ code: 'COLUMN_COUNT', row: index + 3, column: 'A', expected: 49, actual: data[index].length });
  }
  if (errors.length) return { dataRows: [], missingCount: 0, errors };

  let missingCount = 0;
  const dataRows = data.map((cells, rowIndex) => {
    const measurementTime = parseTaiwanDateTime(cells[0]);
    if (!measurementTime) errors.push({ code: 'TIME_INVALID', row: rowIndex + 3, column: 'A' });
    const counts = cells.slice(1).map((cell, cellIndex) => {
      const value = normalizeCell(cell);
      if (value === '') { missingCount++; return null; }
      if (!/^\d+$/.test(value)) errors.push({ code: 'COUNT_INVALID', row: rowIndex + 3, column: columnName(cellIndex + 1) });
      return value;
    });
    return { measurementTime, counts, sourceRow: rowIndex + 3 };
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
    remark: null,
    sourceSheet: 'WebPaste',
    sourceRow: row.sourceRow,
    sourceColumn: columnName(index + 1),
    rawValue: count,
    samplingVolumeUnit: null
  }]));
}

export async function sha256Hex(text) {
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(String(text)));
  return [...new Uint8Array(digest)].map(value => value.toString(16).padStart(2, '0')).join('').toUpperCase();
}
