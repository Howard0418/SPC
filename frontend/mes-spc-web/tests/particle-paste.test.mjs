import assert from 'node:assert/strict';
import test from 'node:test';
import { buildParticlePreviewRows, parseParticlePaste } from '../src/particle/particlePaste.js';

const locations = Array.from({ length: 12 }, (_, index) => `R${index + 1}`);
const sizes = ['0.5', '1', '5', '10'];
const header1 = ['量測時間', ...locations.flatMap(location => [location, location, location, location])].join('\t');
const header2 = ['', ...locations.flatMap(() => ['0.5 µm', '1 µm', '5 µm', '10 µm'])].join('\t');
const excelHeader1 = ['日期\\時間\\位置', ...locations.flatMap(location => [location, '', '', '']), '備註', '', '', ''].join('\t');
const excelHeader2 = ['', ...locations.flatMap(() => ['0.5 um', '1', '5', '10']), '', '', '', ''].join('\t');

test('parses optional two-row header and expands 49 columns to long format', () => {
  const values = Array.from({ length: 48 }, (_, index) => index === 1 ? '' : String(index));
  const parsed = parseParticlePaste(`${header1}\r\n${header2}\r\n2026/11/25 08:30\t${values.join('\t')}`);
  assert.equal(parsed.errors.length, 0);
  assert.equal(parsed.dataRows.length, 1);
  assert.equal(parsed.missingCount, 1);
  const rows = buildParticlePreviewRows(parsed.dataRows);
  assert.equal(rows.length, 47);
  assert.deepEqual(rows[0], {
    measurementTime: '2026-11-25T08:30:00+08:00', location: 'R1', particleSize: '0.5', count: '0',
    samplingVolume: null, samplingDurationSeconds: null, deviceCode: null, remark: null,
    sourceSheet: 'WebPaste', sourceRow: 3, sourceColumn: 'B', rawValue: '0', samplingVolumeUnit: null
  });
  assert.equal(rows.at(-1).location, 'R12');
  assert.equal(rows.at(-1).sourceColumn, 'AW');
});

test('rejects mismatched headers, invalid dates, negative and decimal counts atomically', () => {
  const badHeader = header1.replace('R12', 'R13');
  const counts = Array(48).fill('0');
  counts[0] = '-1';
  counts[1] = '1.5';
  const parsed = parseParticlePaste(`${badHeader}\n${header2}\nbad-date\t${counts.join('\t')}`);
  assert.ok(parsed.errors.some(error => error.code === 'HEADER_MISMATCH'));
  assert.equal(parsed.dataRows.length, 0);
});

test('rejects non-rectangular or non-49-column input and preserves blank as missing instead of zero', () => {
  const parsed = parseParticlePaste(`2026-11-25 09:00\t0\t\n2026-11-25 10:00\t1`);
  assert.ok(parsed.errors.some(error => ['COLUMN_COUNT', 'NON_RECTANGULAR'].includes(error.code)));
  assert.equal(parsed.dataRows.length, 0);
});

test('parses the referenced Excel 53-column form with quoted date newline and merged remark cells', () => {
  const values = Array.from({ length: 48 }, (_, index) => String(index));
  const excelRow = ['"2026/11/25\n08:30"', ...values, '作業前', '', '請品保校驗儀器', ''].join('\t');
  const parsed = parseParticlePaste(`${excelHeader1}\r\n${excelHeader2}\r\n${excelRow}`);

  assert.equal(parsed.errors.length, 0);
  assert.equal(parsed.dataRows.length, 1);
  assert.equal(parsed.dataRows[0].rawMeasurementTime, '2026/11/25\n08:30');
  assert.equal(parsed.dataRows[0].remark, '作業前 / 請品保校驗儀器');
  const rows = buildParticlePreviewRows(parsed.dataRows);
  assert.equal(rows.length, 48);
  assert.ok(rows.every(row => row.remark === '作業前 / 請品保校驗儀器'));
});

test('accepts the website 50-column format and keeps the legacy 49-column format compatible', () => {
  const values = Array.from({ length: 48 }, () => '0');
  const withRemark = parseParticlePaste(['2026/11/25 09:00', ...values, '作業前'].join('\t'));
  const withoutRemark = parseParticlePaste(['2026/11/25 09:00', ...values].join('\t'));

  assert.equal(withRemark.errors.length, 0);
  assert.equal(withRemark.dataRows[0].remark, '作業前');
  assert.equal(withoutRemark.errors.length, 0);
  assert.equal(withoutRemark.dataRows[0].remark, null);
});
