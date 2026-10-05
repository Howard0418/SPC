import test from 'node:test';
import assert from 'node:assert/strict';
import { CONTROL_SCOPE, normalizeControlScope } from '../src/utils/controlScope.js';

test('DUST control scope stays in dust monitoring dimension', () => {
  assert.equal(normalizeControlScope('DUST'), CONTROL_SCOPE.DUST);
  assert.equal(normalizeControlScope('dust'), CONTROL_SCOPE.DUST);
  assert.equal(normalizeControlScope('particle'), CONTROL_SCOPE.DUST);
  assert.equal(normalizeControlScope('dust_monitoring'), CONTROL_SCOPE.DUST);
});
