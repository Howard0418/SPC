import test from 'node:test';
import assert from 'node:assert/strict';
import {
  createLimitDraft,
  isLimitDraftDirty,
  buildLimitBatchItems,
  normalizeLimitInput
} from '../src/utils/analysisLimits.js';

test('blank remains null while zero remains zero', () => {
  assert.equal(normalizeLimitInput(''), null);
  assert.equal(normalizeLimitInput('   '), null);
  assert.equal(normalizeLimitInput('0'), 0);
  assert.equal(normalizeLimitInput('-1.25'), -1.25);
});

test('unit text and non-finite values remain invalid instead of being coerced', () => {
  assert.equal(normalizeLimitInput('95%'), '95%');
  assert.equal(normalizeLimitInput('1 mg/L'), '1 mg/L');
  assert.equal(normalizeLimitInput('Infinity'), 'Infinity');
});

test('dirty rows survive independent pagination and only changed rows are submitted', () => {
  const source = [
    { id: 1, lsl: 0, targetValue: 5, usl: 10, lcl: null, cl: null, ucl: null },
    { id: 2, lsl: 10, targetValue: 15, usl: 20, lcl: null, cl: null, ucl: null }
  ];
  const drafts = Object.fromEntries(source.map(item => [item.id, createLimitDraft(item)]));
  drafts[1].usl = '11';
  drafts[2].lcl = '12';
  drafts[2].cl = '15';
  drafts[2].ucl = '18';

  assert.equal(isLimitDraftDirty(source[0], drafts[1]), true);
  assert.equal(isLimitDraftDirty(source[1], drafts[2]), true);
  assert.deepEqual(buildLimitBatchItems(source, drafts), [
    { partProcessCharacteristicId: 1, values: { lsl: 0, target: 5, usl: 11, lcl: null, cl: null, ucl: null } },
    { partProcessCharacteristicId: 2, values: { lsl: 10, target: 15, usl: 20, lcl: 12, cl: 15, ucl: 18 } }
  ]);
});
