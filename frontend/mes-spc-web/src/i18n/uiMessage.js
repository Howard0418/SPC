// Translate UI-owned key/args only. API text remains literal display data.
export function displayUiMessage(t, message) {
  return typeof message === 'string' ? message : message ? t(message.key, message.args || {}) : '';
}
