import test from 'node:test';
import assert from 'node:assert/strict';
import { createLogoutHandler } from '../src/utils/logout.js';

test('logout reports once before clearing and redirecting', async () => {
  const calls=[];
  const logout=createLogoutHandler({report:async()=>calls.push('report'),clear:()=>calls.push('clear'),redirect:()=>calls.push('redirect')});
  await Promise.all([logout(),logout()]);
  assert.deepEqual(calls,['report','clear','redirect']);
});
test('failed reporting never prevents clearing or redirecting', async () => {
  const calls=[];
  await createLogoutHandler({report:async()=>{throw new Error('offline')},clear:()=>calls.push('clear'),redirect:()=>calls.push('redirect')})();
  assert.deepEqual(calls,['clear','redirect']);
});
test('hanging reporting is bounded and still clears', async () => {
  let cleared=false,redirected=false;
  await createLogoutHandler({report:()=>new Promise(()=>{}),clear:()=>{cleared=true},redirect:()=>{redirected=true},timeoutMs:20})();
  assert.ok(cleared&&redirected);
});
