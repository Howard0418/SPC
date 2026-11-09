import { buildHistogramBins, toFiniteNumbers } from './histogram.js';

export function resolveHistogram(apiHistogram, fallbackValues = []) {
  if (apiHistogram?.bins?.length) {
    return {
      ...apiHistogram,
      bins: apiHistogram.bins.map(bin => ({
        lower: Number(bin.lower),
        upper: Number(bin.upper),
        count: Number(bin.count)
      }))
    };
  }

  const values = toFiniteNumbers(fallbackValues);
  if (!values.length) return null;
  const bins = buildHistogramBins(values, 12);
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length;
  const variance = values.length > 1
    ? values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / (values.length - 1)
    : null;
  return {
    rule: 'SPC_EQUAL_WIDTH_V1_LEGACY_FALLBACK',
    sampleSize: values.length,
    mean,
    sampleStandardDeviation: variance === null ? null : Math.sqrt(variance),
    minimum: Math.min(...values),
    maximum: Math.max(...values),
    binWidth: bins[0]?.max - bins[0]?.min || 0,
    bins: bins.map(bin => ({ lower: bin.min, upper: bin.max, count: bin.count }))
  };
}

export function buildHistogramGeometry(histogram, { limits = {}, controlLimits = {} } = {}) {
  if (!histogram?.bins?.length) return null;
  const domainMin = histogram.bins[0].lower;
  const domainMax = histogram.bins.at(-1).upper;
  const labelSlots = new Map();
  const markLines = [];
  const addLine = (value, name, color, style = 'dashed') => {
    const numericValue = Number(value);
    if (!Number.isFinite(numericValue) || numericValue < domainMin || numericValue > domainMax) return;
    const key = numericValue.toFixed(9);
    const slot = labelSlots.get(key) || 0;
    labelSlots.set(key, slot + 1);
    markLines.push({
      name,
      xAxis: numericValue,
      lineStyle: { color, width: 2, type: style },
      label: {
        formatter: `${name}: ${numericValue.toFixed(3)}`,
        color,
        position: slot % 2 === 0 ? 'end' : 'start',
        offset: [Math.floor(slot / 2) * 18, 0],
        fontSize: 10,
        fontWeight: 'bold',
        backgroundColor: 'rgba(255, 255, 255, 0.88)',
        borderRadius: 4,
        padding: [2, 4]
      }
    });
  };

  addLine(limits.lsl, 'LSL', '#ef4444');
  addLine(limits.usl, 'USL', '#ef4444');
  addLine(controlLimits.lcl ?? limits.lcl, 'LCL', '#f59e0b', 'solid');
  addLine(controlLimits.ucl ?? limits.ucl, 'UCL', '#f59e0b', 'solid');
  addLine(controlLimits.cl ?? limits.cl, 'CL', '#3b82f6', 'dotted');
  addLine(limits.target, 'Target', '#10b981', 'solid');
  addLine(histogram.mean, 'Mean', '#0ea5e9', 'dotted');

  return {
    xAxis: {
      type: 'value',
      min: domainMin,
      max: domainMax,
      axisLine: { lineStyle: { color: '#64748b' } },
      axisTick: { lineStyle: { color: '#334155' } },
      axisLabel: { color: '#94a3b8', fontSize: 10 }
    },
    barData: histogram.bins.map(bin => [bin.lower, bin.upper, bin.count]),
    markLines
  };
}

export function renderHistogramBar(params, api, graphic) {
  const lower = api.value(0);
  const upper = api.value(1);
  const count = api.value(2);
  const bottomLeft = api.coord([lower, 0]);
  const topRight = api.coord([upper, count]);
  const shape = graphic.clipRectByRect({
    x: bottomLeft[0],
    y: topRight[1],
    width: Math.max(0, topRight[0] - bottomLeft[0]),
    height: Math.max(0, bottomLeft[1] - topRight[1])
  }, params.coordSys);
  return shape ? {
    type: 'rect',
    shape,
    style: api.style({ fill: 'rgba(125, 211, 252, 0.82)', stroke: 'rgba(14, 165, 233, 0.9)', lineWidth: 1 })
  } : null;
}
