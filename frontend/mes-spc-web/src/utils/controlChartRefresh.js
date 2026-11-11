const EVENT_NAME = 'spc-control-chart-refresh';
const STORAGE_KEY = 'spc-control-chart-refresh';
const CHANNEL_NAME = 'spc-control-chart-refresh-v1';

function normalizePpcId(value) {
  const id = Number(value);
  return Number.isInteger(id) && id > 0 ? id : null;
}

function parseMessage(value) {
  const ppcId = normalizePpcId(value?.ppcId ?? value);
  return ppcId ? { ppcId } : null;
}

function dispatchLocal(message) {
  window.dispatchEvent(new CustomEvent(EVENT_NAME, { detail: message }));
}

/** Notify current and same-origin browser tabs after a PPC formula is saved. */
export function notifyControlChartRefresh(ppcId) {
  const message = parseMessage({ ppcId });
  if (!message || typeof window === 'undefined') return;

  dispatchLocal(message);
  try {
    const channel = typeof BroadcastChannel === 'undefined' ? null : new BroadcastChannel(CHANNEL_NAME);
    channel?.postMessage(message);
    channel?.close();
  } catch {
    // A browser privacy policy must never make a successful PPC save fail.
  }
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...message, sentAt: Date.now() }));
    window.localStorage.removeItem(STORAGE_KEY);
  } catch {
    // BroadcastChannel remains the preferred cross-tab transport.
  }
}

/** Subscribe to valid PPC refresh messages and return an idempotent cleanup callback. */
export function subscribeControlChartRefresh(handler) {
  if (typeof window === 'undefined') return () => {};
  const receive = value => {
    const message = parseMessage(value);
    if (message) handler(message.ppcId);
  };
  const onLocal = event => receive(event.detail);
  const onStorage = event => {
    if (event.key !== STORAGE_KEY || !event.newValue) return;
    try { receive(JSON.parse(event.newValue)); } catch { /* ignore malformed external storage */ }
  };
  let channel = null;
  try {
    if (typeof BroadcastChannel !== 'undefined') {
      channel = new BroadcastChannel(CHANNEL_NAME);
      channel.onmessage = event => receive(event.data);
    }
  } catch {
    channel = null;
  }
  window.addEventListener(EVENT_NAME, onLocal);
  window.addEventListener('storage', onStorage);
  return () => {
    window.removeEventListener(EVENT_NAME, onLocal);
    window.removeEventListener('storage', onStorage);
    channel?.close();
  };
}
