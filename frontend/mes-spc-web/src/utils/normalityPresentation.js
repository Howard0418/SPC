export function isValidNormalityPValue(value) {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0 && value <= 1;
}
export function formatNormalityPValue(value) {
  if (!isValidNormalityPValue(value)) return 'N/A';
  return value > 0 && value < 0.0001 ? '<0.0001' : value.toFixed(4);
}
export function normalityConclusion(result) {
  if (!isValidNormalityPValue(result?.pValue)) return '無法檢定';
  return result.pValue >= 0.05 ? '未拒絕常態假設' : '偏離常態';
}
export function normalityUnavailableReason(result) {
  if (isValidNormalityPValue(result?.pValue)) return '';
  return result?.note || (result ? '沒有有效的檢定結果，無法進行常態判定。' : '有效樣本不足，常態檢定至少3筆未排除量測值。');
}
