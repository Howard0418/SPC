export function resolveActiveRawDataPpcId({ ppcId, selectedSummaryRow, selectedMapping } = {}) {
  const candidates = [
    ppcId,
    selectedSummaryRow?.partProcessCharacteristicId,
    selectedSummaryRow?.ppcId,
    selectedMapping?.id
  ];

  for (const candidate of candidates) {
    const numeric = Number(candidate);
    if (Number.isInteger(numeric) && numeric > 0) return numeric;
  }

  return 0;
}
