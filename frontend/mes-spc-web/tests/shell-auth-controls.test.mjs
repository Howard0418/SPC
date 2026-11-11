import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const shell = readFileSync(new URL('../src/App.vue', import.meta.url), 'utf8');
const testhost = readFileSync(new URL('../.env.testhost', import.meta.url), 'utf8');

test('authenticated shell keeps Portal return and logout controls', () => {
  assert.match(testhost, /^VITE_AUTH_ENABLED=true$/m);
  assert.match(shell, /@click="returnToPortal"/);
  assert.match(shell, /@click="logout"/);
  assert.match(shell, /t\("spc\.shell\.portal"\)/);
  assert.match(shell, /t\("spc\.shell\.logout"\)/);
});
