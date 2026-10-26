const apiDisplay = Symbol('validated-api-display');
const joinedDisplay = Symbol('ui-message-lines');

export function joinUiMessages(messages, separator = '\n') {
  return Object.freeze({ [joinedDisplay]: true, messages: Object.freeze([...messages]), separator });
}

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
  if (message[joinedDisplay]) return message.messages.map(item => displayUiMessage(t, item)).join(message.separator);
  const args = Object.fromEntries(Object.entries(message.args || {}).map(([key, value]) =>
    [key, value?.[apiDisplay] || value?.[joinedDisplay] ? displayUiMessage(t, value) : value]));
  const text = t(message.key, args);
  return message[apiDisplay]
    ? [text, ...message.details.map(child => displayUiMessage(t, child))].join('\n')
    : text;
}
