import assert from 'node:assert/strict';
import { chromium } from '@playwright/test';
const target = new URL(process.argv[2] || 'http://127.0.0.1:5196');
assert.ok(['127.0.0.1', 'spctest.pmr.com.tw'].includes(target.hostname.toLowerCase()), 'Use a local preview or SPC test site.');
const browser = await chromium.launch({ headless: true });
try {
  const context = await browser.newContext({ ignoreHTTPSErrors: true });
  // This fixture verifies the served bundle's routing only. No API request reaches the backend.
  await context.addInitScript(() => {
    localStorage.setItem('mes_spc_token', 'mock-ui-only-token');
    localStorage.setItem('mes_spc_user', JSON.stringify({ role: 'Editor' }));
  });
  const requests = [];
  await context.route('**/*', route => {
    const url = new URL(route.request().url());
    if (url.pathname.startsWith('/api/')) {
      requests.push(url);
      if (route.request().method() !== 'GET') return route.abort();
      return route.fulfill({ json: url.pathname.endsWith('/version') ? { version: 'test', environment: 'test' } : [] });
    }
    return url.origin === target.origin ? route.continue() : route.abort();
  });
  const page = await context.newPage();
  await page.goto(`${target.origin}/part-process-characteristics`);
  await page.getByRole('heading', { name: 'SPC 管制項目設定與規格維護', exact: true }).waitFor();
  await page.waitForFunction(() => !document.body.innerText.includes('載入中...'));
  assert.ok(requests.some(url => url.pathname === '/api/part-process-characteristics'), 'Master data request was not made.');
  assert.deepEqual(requests.filter(url => url.origin !== target.origin).map(url => url.href), [], 'Bundle attempted to use a separate API origin.');
  assert.equal(await page.getByText(/無法載入檢驗項目基準及管制圖配置/).count(), 0);
  console.log(`BUILT_BROWSER_API_SAME_ORIGIN=True ORIGIN=${target.origin} API_REQUESTS=${requests.length}`);
} finally { await browser.close(); }
