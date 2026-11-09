const fields = [
  ['lsl', 'lsl'],
  ['target', 'targetValue'],
  ['usl', 'usl'],
  ['lcl', 'lcl'],
  ['cl', 'cl'],
  ['ucl', 'ucl']
];

export function normalizeLimitInput(value) {
  if (value === null || value === undefined || String(value).trim() === '') return null;
  const text = String(value).trim();
  const number = Number(text);
  return Number.isFinite(number) ? number : text;
}

export function createLimitDraft(item) {
  return Object.fromEntries(fields.map(([draftKey, sourceKey]) => [
    draftKey,
    item?.[sourceKey] === null || item?.[sourceKey] === undefined ? '' : String(item[sourceKey])
  ]));
}

export function isLimitDraftDirty(item, draft) {
  if (!draft) return false;
  return fields.some(([draftKey, sourceKey]) =>
    normalizeLimitInput(draft[draftKey]) !== normalizeLimitInput(item?.[sourceKey]));
}

export function buildLimitBatchItems(items, drafts) {
  return items
    .filter(item => isLimitDraftDirty(item, drafts[item.id]))
    .map(item => ({
      partProcessCharacteristicId: item.id,
      values: Object.fromEntries(fields.map(([draftKey]) => [draftKey, normalizeLimitInput(drafts[item.id][draftKey])]))
    }));
}
