import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';

const router = readFileSync(new URL('../src/router/index.js', import.meta.url), 'utf8');
const shell = readFileSync(new URL('../src/App.vue', import.meta.url), 'utf8');
const view = readFileSync(new URL('../src/views/AuditEventsView.vue', import.meta.url), 'utf8');

test('audit route and navigation require the explicit read permission', () => {
  assert.match(router, /explicitPermission:\s*"admin\.audit\.read"/);
  assert.match(router, /!user\?\.permissions\?\.includes\(to\.meta\.explicitPermission\)/);
  assert.match(shell, /permission:\s*"admin\.audit\.read"/);
  assert.match(shell, /items\.filter\(item => !item\.permission/);
});

test('export is independently gated and audit API calls do not generate duplicate activity telemetry', () => {
  assert.match(view, /admin\.audit\.export/);
  assert.match(view, /v-if="canExport"/);
  assert.match(view, /auditActivityInternal:\s*true/g);
  assert.match(view, /nextCursor/);
});
