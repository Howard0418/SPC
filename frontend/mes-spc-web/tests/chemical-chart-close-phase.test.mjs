import test from 'node:test';
import assert from 'node:assert/strict';
import { chartStageShiftLabel, shiftLabel } from '../src/utils/chemicalChart.js';

test('chemical shift labels only expose early or middle shifts', () => {
  assert.equal(shiftLabel('OPEN'), '早班');
  assert.equal(shiftLabel('MIDDLE'), '中班');
  assert.notEqual(shiftLabel('CLOSE'), '晚班');
});

test('legacy CLOSE means close stage rather than a night shift', () => {
  assert.equal(chartStageShiftLabel('CLOSE', 'GENERAL', false), '收線待校正');
  assert.equal(chartStageShiftLabel('OPEN', 'CLOSE', true), '早(收)');
  assert.equal(chartStageShiftLabel('MIDDLE', 'CLOSE', true), '中(收)');
});
