// Presentation only: never mutate master data, lookup identity, parsing or sorting.
const usable = value => typeof value === 'string' && value.trim().length > 0;
export function characteristicDisplayName(item, locale, unnamed) {
  const names = locale === 'en-US'
    ? [item?.characteristicNameEn, item?.characteristicName]
    : [item?.characteristicName, item?.characteristicNameEn];
  return [...names, item?.characteristicCode].find(usable) ?? unnamed;
}
