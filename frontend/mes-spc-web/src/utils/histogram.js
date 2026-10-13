export function toFiniteNumbers(values) {
  return (values || [])
    .filter(value => value !== null && value !== undefined && !Number.isNaN(Number(value)))
    .map(value => Number(value))
    .filter(Number.isFinite);
}

export function buildHistogramBins(values, preferredBins = 12) {
  const numericValues = toFiniteNumbers(values);
  if (!numericValues.length) return [];

  const valueMin = Math.min(...numericValues);
  const valueMax = Math.max(...numericValues);

  if (valueMin === valueMax) {
    const pad = Math.abs(valueMin) > 0 ? Math.abs(valueMin) * 0.05 : 0.5;
    return [{
      min: valueMin - pad,
      max: valueMax + pad,
      count: numericValues.length,
      values: numericValues
    }];
  }

  const valueRange = valueMax - valueMin;
  const pad = valueRange * 0.08;
  const min = valueMin - pad;
  const max = valueMax + pad;
  const binCount = Math.max(5, Math.min(preferredBins, Math.ceil(Math.sqrt(numericValues.length)) + 3));
  const width = (max - min) / binCount;
  const bins = Array.from({ length: binCount }, (_, idx) => ({
    min: min + idx * width,
    max: idx === binCount - 1 ? max : min + (idx + 1) * width,
    count: 0,
    values: []
  }));

  numericValues.forEach(value => {
    const rawIndex = Math.floor((value - min) / width);
    const index = Math.max(0, Math.min(binCount - 1, rawIndex));
    bins[index].count += 1;
    bins[index].values.push(value);
  });

  return bins;
}

export function findHistogramBinIndex(bins, value) {
  if (!bins?.length || value === null || value === undefined || Number.isNaN(Number(value))) return null;
  const numericValue = Number(value);
  const lastIndex = bins.length - 1;
  const index = bins.findIndex((bin, idx) => (
    numericValue >= bin.min && (numericValue < bin.max || (idx === lastIndex && numericValue <= bin.max))
  ));
  return index >= 0 ? index : null;
}
