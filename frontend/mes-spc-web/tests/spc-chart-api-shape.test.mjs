import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const source = fs.readFileSync(new URL('../src/views/SpcChartView.vue', import.meta.url), 'utf8');

test('SPC chart normalizes array and supported wrapper responses before filtering', () => {
  assert.match(source, /function normalizeArrayResponse\(data, label\)/);
  assert.match(source, /normalizeArrayResponse\(typesRes\.data, "管制圖類型"\)/);
  assert.match(source, /normalizeArrayResponse\(groupsRes\.data, "管制圖群組"\)/);
  assert.match(source, /normalizeArrayResponse\(mapRes\.data, "管制項目"\)/);
  assert.match(source, /\["items", "data", "results"\]/);
});

