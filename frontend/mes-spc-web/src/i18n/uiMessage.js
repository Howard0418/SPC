const apiDisplay = Symbol('validated-api-display');

// Called only after API namespace, parameters and tree limits have been validated.
export function snapshotApiDisplay(node) {
  return Object.freeze({
    [apiDisplay]: true,
    key: node.key,
    args: Object.freeze({ ...node.args }),
    details: Object.freeze((node.details || []).map(snapshotApiDisplay)),
  });
}

// Local UI descriptors are authored in code; external API text stays literal.
export function displayUiMessage(t, message) {
  if (typeof message === 'string') return message;
  if (!message) return '';
  const args = Object.fromEntries(Object.entries(message.args || {}).map(([key, value]) =>
    [key, value?.[apiDisplay] ? displayUiMessage(t, value) : value]));
  const text = t(message.key, args);
  return message[apiDisplay]
    ? [text, ...message.details.map(child => displayUiMessage(t, child))].join('\n')
    : text;
}
