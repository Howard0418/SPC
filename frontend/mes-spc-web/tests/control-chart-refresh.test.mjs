import test from 'node:test';
import assert from 'node:assert/strict';
import { notifyControlChartRefresh, subscribeControlChartRefresh } from '../src/utils/controlChartRefresh.js';

test('a valid saved PPC formula notifies the current chart exactly once', () => {
  const originalWindow = globalThis.window;
  const originalChannel = globalThis.BroadcastChannel;
  const target = new EventTarget();
  const values = new Map();
  globalThis.window = Object.assign(target, { localStorage: { setItem: (key, value) => values.set(key, value), removeItem: key => values.delete(key) } });
  globalThis.BroadcastChannel = undefined;
  const received = [];
  const stop = subscribeControlChartRefresh(id => received.push(id));
  notifyControlChartRefresh(2437);
  notifyControlChartRefresh('not-an-id');
  stop();
  globalThis.window = originalWindow;
  globalThis.BroadcastChannel = originalChannel;
  assert.deepEqual(received, [2437]);
});

test('storage fallback ignores malformed and accepts valid cross-tab messages', () => {
  const originalWindow = globalThis.window;
  const originalChannel = globalThis.BroadcastChannel;
  const target = new EventTarget();
  globalThis.window = Object.assign(target, { localStorage: { setItem() {}, removeItem() {} } });
  globalThis.BroadcastChannel = undefined;
  const received = [];
  const stop = subscribeControlChartRefresh(id => received.push(id));
  const malformed = new Event('storage'); Object.defineProperties(malformed, { key: { value: 'spc-control-chart-refresh' }, newValue: { value: '{bad' } }); target.dispatchEvent(malformed);
  const valid = new Event('storage'); Object.defineProperties(valid, { key: { value: 'spc-control-chart-refresh' }, newValue: { value: '{"ppcId":2437}' } }); target.dispatchEvent(valid);
  stop();
  globalThis.window = originalWindow;
  globalThis.BroadcastChannel = originalChannel;
  assert.deepEqual(received, [2437]);
});
