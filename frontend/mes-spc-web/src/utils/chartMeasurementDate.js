// Presentation only. Keep the existing local timezone and never alter measurement keys or values.
export function formatChartMeasurementDate(value, short = false) {
  if (!value) return '-';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '-';
  const dateOptions = short ? { month: '2-digit', day: '2-digit' } : {};
  const midnight = date.getHours() === 0 && date.getMinutes() === 0
    && date.getSeconds() === 0 && date.getMilliseconds() === 0;
  if (midnight) return date.toLocaleDateString('zh-TW', dateOptions);
  if (!short) return date.toLocaleString('zh-TW', { hour12: false });
  const options = { ...dateOptions, hour: '2-digit', minute: '2-digit', hour12: false };
  // Preserve a real nonzero second even when a minute-only label would look like midnight.
  if (date.getHours() === 0 && date.getMinutes() === 0 && date.getSeconds() !== 0) options.second = '2-digit';
  return date.toLocaleString('zh-TW', options);
}
