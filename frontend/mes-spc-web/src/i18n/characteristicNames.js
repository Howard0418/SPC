// Presentation only: never mutate master data, lookup identity, parsing or sorting.
const usable = value => typeof value === 'string' && value.trim().length > 0;
export function characteristicDisplayName(item, locale, unnamed) {
  const names = locale === 'en-US'
    ? [item?.characteristicNameEn, item?.characteristicName]
    : [item?.characteristicName, item?.characteristicNameEn];
  return [...names, item?.characteristicCode].find(usable) ?? unnamed;
}

// Older summary payloads can contain a distinct chart title. Do not translate or reinterpret it.
export function summaryCharacteristicDisplayName(row, locale, unnamed) {
  if (row && ['characteristicName', 'characteristicNameEn', 'characteristicCode'].some(key => Object.hasOwn(row, key))) {
    return characteristicDisplayName(row, locale, unnamed);
  }
  return usable(row?.chartName) ? row.chartName : unnamed;
}
