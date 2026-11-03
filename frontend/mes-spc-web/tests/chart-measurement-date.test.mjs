import { test } from 'node:test';
import assert from 'node:assert/strict';
import { formatChartMeasurementDate } from '../src/utils/chartMeasurementDate.js';

test('midnight labels and full measurement details show only their date', () => {
  assert.equal(formatChartMeasurementDate('2026-09-29T00:00:00', true), '09/29');
  assert.equal(formatChartMeasurementDate('2026-09-29T00:00:00'), '2026/9/29');
  assert.equal(formatChartMeasurementDate('2026-10-13T00:00'), '2026/10/13');
});
test('real nonzero hours and seconds retain the existing local clock', () => {
  const value='2026-10-13T12:34:56';
  assert.equal(formatChartMeasurementDate(value), new Date(value).toLocaleString('zh-TW',{hour12:false}));
  assert.match(formatChartMeasurementDate(value,true), /12:34/);
  assert.match(formatChartMeasurementDate('2026-10-13T00:00:01'), /00:00:01/);
  assert.match(formatChartMeasurementDate('2026-10-13T00:00:01',true), /00:00:01/);
});
test('timezone conversion uses the same local Date, never a sliced source date', () => {
  const value=new Date(2026,9,1,0,0,0).toISOString();
  assert.equal(formatChartMeasurementDate(value,true), '10/01');
  const daytime=new Date(2026,9,1,8,1,2).toISOString();
  assert.equal(formatChartMeasurementDate(daytime),new Date(daytime).toLocaleString('zh-TW',{hour12:false}));
});
test('missing or invalid values remain safe and source strings remain unchanged', () => {
  for(const value of [null,undefined,'','invalid','<img src=x>'])assert.equal(formatChartMeasurementDate(value), '-');
  const value='2026-09-29T00:00:00';formatChartMeasurementDate(value);assert.equal(value,'2026-09-29T00:00:00');
});
