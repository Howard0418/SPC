import test from 'node:test';
import assert from 'node:assert/strict';
import { buildHistogramBins, findHistogramBinIndex } from '../src/utils/histogram.js';

test('builds histogram bins from measured values without distant spec limits', () => {
  const values = [7.91, 7.92, 7.925, 7.93, 7.935, 7.94, 7.945, 7.95];
  const bins = buildHistogramBins(values, 12);
  const domainWidth = bins.at(-1).max - bins[0].min;
  const occupiedBins = bins.filter(bin => bin.count > 0).length;

  assert.ok(domainWidth < 0.1);
  assert.ok(occupiedBins > 1);
  assert.equal(findHistogramBinIndex(bins, 100), null);
});

test('keeps equal values readable with a padded single bin', () => {
  const bins = buildHistogramBins([5, 5, 5], 12);

  assert.equal(bins.length, 1);
  assert.equal(bins[0].count, 3);
  assert.ok(bins[0].min < 5);
  assert.ok(bins[0].max > 5);
});
