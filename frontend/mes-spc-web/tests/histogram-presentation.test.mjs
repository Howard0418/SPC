import test from 'node:test';
import assert from 'node:assert/strict';
import { buildHistogramGeometry, resolveHistogram } from '../src/utils/histogramPresentation.js';

const apiHistogram = {
  rule: 'SPC_EQUAL_WIDTH_V1',
  sampleSize: 8,
  mean: 19.236,
  sampleStandardDeviation: 1.0871172889803566,
  minimum: 17.472,
  maximum: 20.608,
  binWidth: 0.6062933333333333,
  bins: [
    { lower: 17.22112, upper: 17.827413333333336, count: 1 },
    { lower: 17.827413333333336, upper: 18.43370666666667, count: 2 },
    { lower: 18.43370666666667, upper: 19.04, count: 0 },
    { lower: 19.04, upper: 19.646293333333332, count: 2 },
    { lower: 19.646293333333332, upper: 20.252586666666666, count: 2 },
    { lower: 20.252586666666666, upper: 20.85888, count: 1 }
  ]
};

test('uses API histogram as the authoritative contract', () => {
  const result = resolveHistogram(apiHistogram, [999, 1000]);

  assert.equal(result.rule, 'SPC_EQUAL_WIDTH_V1');
  assert.equal(result.sampleSize, 8);
  assert.equal(result.mean, 19.236);
  assert.deepEqual(result.bins.map(bin => bin.count), [1, 2, 0, 2, 2, 1]);
});

test('builds continuous-axis bars and positions limits at their real values', () => {
  const result = buildHistogramGeometry(apiHistogram, {
    limits: { lsl: 18, target: 20, usl: 22 },
    controlLimits: { cl: 19.236 }
  });

  assert.equal(result.xAxis.type, 'value');
  assert.equal(result.xAxis.min, 17.22112);
  assert.equal(result.xAxis.max, 20.85888);
  assert.deepEqual(result.barData[0], [17.22112, 17.827413333333336, 1]);
  assert.equal(result.markLines.find(line => line.name === 'LSL').xAxis, 18);
  assert.equal(result.markLines.find(line => line.name === 'Target').xAxis, 20);
  assert.equal(result.markLines.find(line => line.name === 'Mean').xAxis, 19.236);
  assert.equal(result.markLines.some(line => line.name === 'USL'), false);
});

test('falls back to the legacy values only when an older API omits histogram', () => {
  const result = resolveHistogram(null, [1, 2, 3, 4, 5]);

  assert.equal(result.sampleSize, 5);
  assert.equal(result.bins.reduce((sum, bin) => sum + bin.count, 0), 5);
  assert.equal(result.rule, 'SPC_EQUAL_WIDTH_V1_LEGACY_FALLBACK');
});
