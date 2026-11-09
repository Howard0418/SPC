import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

for (const file of ['SpcChartView.vue', 'TrendChartView.vue']) {
  test(`${file} uses the API histogram and one continuous x-axis`, async () => {
    const source = await readFile(new URL(`../src/views/${file}`, import.meta.url), 'utf8');

    assert.match(source, /resolveHistogram\(data\.histogram, values\)/);
    assert.match(source, /buildHistogramGeometry\(histogram/);
    assert.match(source, /renderHistogramBar/);
    assert.doesNotMatch(source, /buildHistogramBins\(values/);
    assert.match(source, /renderItem:\s*\(params, api\)\s*=>\s*renderHistogramBar/);
    assert.match(source, /type:\s*["']line["'][\s\S]{0,500}xAxisIndex:\s*0/);
  });
}
