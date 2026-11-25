import { expect, test, type Page } from '@playwright/test';

const pageErrors = new WeakMap<Page, string[]>();
test.beforeEach(({ page }) => {
  const errors: string[] = [];
  pageErrors.set(page, errors);
  page.on('pageerror', error => errors.push(error.message));
});
test.afterEach(({ page }) => expect(pageErrors.get(page)).toEqual([]));

async function prepare(page: Page, role: string, permissions: string[]) {
  await page.addInitScript(({ role, permissions }) => {
    localStorage.setItem('mes_spc_token', 'navigation-permission-fixture');
    localStorage.setItem('mes_spc_user', JSON.stringify({ id: 101, role, username: 'fixture', permissions }));
  }, { role, permissions });
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith('/api/')) return route.continue();
    if (route.request().method() === 'POST' && path.endsWith('/audit/activity'))
      return route.fulfill({ status: 200, json: {} });
    return route.fulfill({ status: 200, json: path.endsWith('/version') ? { version: 'test', environment: 'test' } : [] });
  });
}

test('editor menu and direct routes use the same exact permission codes', async ({ page }) => {
  await prepare(page, 'Editor', ['analysis.trend', 'master.parts']);
  await page.goto('/parts');
  await expect(page.locator('aside a[href="/parts"]')).toBeVisible();
  await expect(page.locator('aside a[href="/trend-chart"]')).toBeVisible();
  await expect(page.locator('aside a[href="/spc"]')).toHaveCount(0);
  await expect(page.locator('aside a[href="/operators"]')).toHaveCount(0);

  await page.goto('/operators');
  await expect(page).toHaveURL(/\/trend-chart\?access=page$/);
});

test('viewer cannot enter editor pages and particle access is independent from SPC analysis', async ({ page }) => {
  await prepare(page, 'Viewer', ['analysis.particles']);
  await page.goto('/particle-monitoring');
  await expect(page.locator('aside a[href="/particle-monitoring"]')).toBeVisible();
  await expect(page.locator('aside a[href="/spc"]')).toHaveCount(0);

  await page.goto('/parts');
  await expect(page).toHaveURL(/\/particle-monitoring\?access=viewer$/);
});
