import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { firstAccessiblePath, pageRegistry, permissionForPath } from '../src/navigation/pageRegistry.js';

test('every navigable page has a unique permission, route and translation key', () => {
  assert.ok(pageRegistry.length > 0);
  assert.equal(new Set(pageRegistry.map(x => x.permission)).size, pageRegistry.length);
  assert.equal(new Set(pageRegistry.map(x => x.path)).size, pageRegistry.length);
  for (const page of pageRegistry) {
    assert.match(page.labelKey, /^spc\.navigation\./);
    assert.match(page.categoryKey, /^spc\.navigation\./);
  }
});

test('frontend registry matches the backend navigable permission contract', () => {
  const source = readFileSync(new URL('../../../backend/MesSpc.Api/Services/Security/SpcPagePermissions.cs', import.meta.url), 'utf8');
  const backendPages = [...source.matchAll(/Page\("([^"]+)",\s*"[^"]+",\s*"([^"]+)",\s*"([^"]+)",\s*"([^"]+)"/g)]
    .map(match => ({ permission: match[1], labelKey: `spc.navigation.${match[2]}`, path: match[3], categoryKey: `spc.navigation.${match[4]}` }));
  if (source.includes('Page(AuditPermissions.Read, "使用者活動紀錄", "auditEvents", "/audit-events", "administration"'))
    backendPages.push({ permission: 'admin.audit.read', labelKey: 'spc.navigation.auditEvents', path: '/audit-events', categoryKey: 'spc.navigation.administration' });
  assert.deepEqual(
    backendPages,
    pageRegistry.map(({ permission, labelKey, path, categoryKey }) => ({ permission, labelKey, path, categoryKey }))
  );
});

test('dynamic child routes inherit their registered page permission', () => {
  assert.equal(permissionForPath('/spc/control-chart/2587'), 'analysis.spc');
  assert.equal(permissionForPath('/spc/trend/2587'), 'analysis.trend');
  assert.equal(permissionForPath('/uploads/ABC/preview'), 'data.upload');
  assert.equal(permissionForPath('/particle-monitoring'), 'analysis.particles');
});

test('fallback selects only a page explicitly granted to the current role', () => {
  assert.equal(firstAccessiblePath({ role: 'Viewer', permissions: ['analysis.trend'] }), '/trend-chart');
  assert.equal(firstAccessiblePath({ role: 'Viewer', permissions: [] }), null);
  assert.equal(firstAccessiblePath({ role: 'Viewer', permissions: ['admin.operators'] }), null);
});
