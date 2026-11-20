import test from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import { resolveBusinessAction } from '../src/utils/activityAudit.js';
import { flattenCatalog } from '../scripts/check-i18n.mjs';

test('maps only approved business endpoints to fixed action keys', () => {
  const cases = [
    ['post', '/settings/smtp', 'settings.save'],
    ['post', '/settings/smtp/test-alert', 'notification.test'],
    ['post', '/v1/chemical-f-table/apply', 'ftable.apply'],
    ['post', '/v1/chemical-f-table/versions/12/restore', 'ftable.restore'],
    ['put', '/part-process-characteristics/2587', 'formula.save'],
    ['post', '/part-process-characteristics/2587/chemical-analysis-formula-versions/9/restore', 'formula.restore'],
    ['post', '/uploads/00000000-0000-0000-0000-000000000001/confirm', 'import.confirm'],
    ['delete', '/uploads/00000000-0000-0000-0000-000000000001', 'import.delete'],
    ['put', '/v1/alerts/8/workflow', 'alert.disposition.save'],
    ['post', '/alerts/8/ack', 'alert.acknowledge'],
    ['put', '/v1/operators/27', 'operator.save'],
    ['delete', '/v1/operators/27', 'operator.delete']
  ];
  for (const [method, url, actionKey] of cases)
    assert.equal(resolveBusinessAction({ method, url }), actionKey, `${method} ${url}`);
});

test('does not collect reads, arbitrary writes, audit calls, query values or bodies', () => {
  assert.equal(resolveBusinessAction({ method: 'get', url: '/parts?search=secret' }), null);
  assert.equal(resolveBusinessAction({ method: 'post', url: '/v1/audit/activity' }), null);
  assert.equal(resolveBusinessAction({ method: 'post', url: '/unknown/save', data: { password: 'secret' } }), null);
  assert.equal(resolveBusinessAction({ method: 'post', url: '/settings/smtp?token=secret', data: { password: 'secret' } }), 'settings.save');
});

test('business action translation review matches all runtime locales', () => {
  const locales = ['zh-TW', 'zh-CN', 'en-US'];
  const flat = Object.fromEntries(locales.map(locale => [locale, flattenCatalog(JSON.parse(readFileSync(new URL(`../src/i18n/locales/${locale}.json`, import.meta.url), 'utf8')))]));
  const review = JSON.parse(readFileSync(new URL('../../../specs/20261120-audit-business-action-allowlist/translation-review.json', import.meta.url), 'utf8'));
  const sha = value => createHash('sha256').update(value, 'utf8').digest('hex');
  assert.equal(review.metadata.length, 2);
  assert.equal(review.ledger.length, 6);
  for (const row of review.ledger) {
    assert.equal(row.sourceHash, sha(flat['zh-TW'][row.key]));
    assert.equal(row.translationHash, sha(flat[row.locale][row.key]));
  }
});
