import test from 'node:test';
import assert from 'node:assert/strict';
import { resolveActiveRawDataPpcId } from '../src/utils/spcRawDataDownload.js';

test('uses explicit ppcId first for raw data download', () => {
  assert.equal(resolveActiveRawDataPpcId({
    ppcId: '2606',
    selectedSummaryRow: { partProcessCharacteristicId: 31 },
    selectedMapping: { id: 41 }
  }), 2606);
});

test('uses summary row partProcessCharacteristicId after all-lines drilldown', () => {
  assert.equal(resolveActiveRawDataPpcId({
    ppcId: '',
    selectedSummaryRow: { partProcessCharacteristicId: 2606 },
    selectedMapping: null
  }), 2606);
});

test('keeps compatibility with summary rows exposing ppcId', () => {
  assert.equal(resolveActiveRawDataPpcId({
    ppcId: '',
    selectedSummaryRow: { ppcId: 735 },
    selectedMapping: null
  }), 735);
});

test('falls back to selected mapping for direct dropdown chart selection', () => {
  assert.equal(resolveActiveRawDataPpcId({
    ppcId: '',
    selectedSummaryRow: null,
    selectedMapping: { id: 88 }
  }), 88);
});

test('ignores empty or invalid identifiers', () => {
  assert.equal(resolveActiveRawDataPpcId({
    ppcId: '',
    selectedSummaryRow: { partProcessCharacteristicId: 'abc', ppcId: 0 },
    selectedMapping: { id: -1 }
  }), 0);
});
