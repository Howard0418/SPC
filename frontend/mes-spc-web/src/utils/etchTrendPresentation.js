// Presentation only: never replace raw samples or infer groups by slicing a date's rows.
export function etchTrendPresentation(chart, mapping) {
  const raw = chart?.rawDataPoints || [];
  const characteristic = mapping?.characteristic || {};
  const code = String(characteristic.characteristicCode || '').toUpperCase();
  const name = String(characteristic.characteristicName || '').toUpperCase();
  const side = /^ETCH_[AB]_AVG$/.test(code) || /[AB]\s*SIDE.*咬蝕量/.test(name);
  const line = String(mapping?.machine?.machineCode || mapping?.process?.processCode || '').toUpperCase();
  const size = { PT1: 25, QE1: 25, PT2: 50, QE2: 50 }[line];
  if (!chart || !side || !size) return { isSubgroup: false, points: raw, rawSampleCount: raw.length, incompleteCount: 0 };
  const groups = chart.chartData?.points;
  if (Number(mapping.sampleSize) !== size || !Array.isArray(groups))
    return { isSubgroup: true, sampleSize: size, points: [], rawSampleCount: raw.length, incompleteCount: 1 };
  const points = groups.filter(p => Number(p.n) === size && p.xbar !== null && p.xbar !== undefined && Number.isFinite(Number(p.xbar)))
    .map(p => ({ ...p, value: Number(p.xbar), groupSampleCount: size, isSubgroupAverage: true,
      variableMeasurementId: null, attributeMeasurementId: null }));
  return { isSubgroup: true, sampleSize: size, points, rawSampleCount: raw.length, incompleteCount: groups.length - points.length };
}
