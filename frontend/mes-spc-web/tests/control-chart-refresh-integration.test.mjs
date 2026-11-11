import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

const source = relative => fs.readFileSync(new URL(relative, import.meta.url), 'utf8');

test('PPC save broadcasts a chart refresh only after a successful update', () => {
  const view = source('../src/views/PartProcessCharacteristicsView.vue');
  assert.match(view, /import \{ notifyControlChartRefresh \} from ['"]\.\.\/utils\/controlChartRefresh/);
  assert.match(view, /await api\.put\(`\/part-process-characteristics\/\$\{currentId\.value\}`, payload\)[\s\S]{0,300}notifyControlChartRefresh\(savedItem\.id\)/);
});

test('both chart views subscribe, only reload their active PPC, and unsubscribe on unmount', () => {
  for (const file of ['../src/views/SpcChartView.vue', '../src/views/TrendChartView.vue']) {
    const view = source(file);
    assert.match(view, /subscribeControlChartRefresh/);
    assert.match(view, /Number\(refreshedPpcId\) !== Number\(activePpcIdValue\.value\)/);
    assert.match(view, /unsubscribeControlChartRefresh\?\.\(\)/);
  }
});
