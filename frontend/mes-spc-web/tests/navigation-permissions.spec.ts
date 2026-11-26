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

test('saving own page permission updates the menu without navigation or sign-in', async ({ page }) => {
  const operator = { id: 101, operatorCode: 'P101', operatorName: 'Fixture', username: 'fixture', role: 'Editor', isActive: true,
    pagePermissions: ['admin.operators', 'analysis.spc'] };
  await prepare(page, operator.role, operator.pagePermissions);
  const order: string[] = [];
  await page.route('**/api/**', async route => {
    const { pathname } = new URL(route.request().url());
    const method = route.request().method();
    if (pathname.endsWith('/auth/session')) {
      order.push('session');
      expect(route.request().headers().authorization).toBe('Bearer navigation-permission-fixture');
      return route.fulfill({ json: { user: { id: operator.id, username: operator.username, role: operator.role,
        permissions: operator.pagePermissions } } });
    }
    if (pathname.endsWith('/permissions-catalog')) return route.fulfill({ json: [
      { code: 'admin.operators', name: 'User settings' }, { code: 'data.particles', name: 'Particle entry' }
    ] });
    if (pathname === '/api/operators') return route.fulfill({ json: [operator] });
    if (method === 'PUT' && pathname === '/api/operators/101') {
      order.push('save');
      operator.pagePermissions = route.request().postDataJSON().pagePermissions;
      return route.fulfill({ json: operator });
    }
    return route.fallback();
  });
  await page.goto('/operators');
  const grantedLink = page.locator('aside a[href="/particle-monitoring/entry"]');
  await expect(grantedLink).toHaveCount(0);
  await page.getByTestId('settings-openEditModal').click();
  const dialog = page.getByTestId('settings-operator-dialog');
  await dialog.locator('input[value="data.particles"]').check();
  await dialog.locator('button[type="submit"]').click();
  await expect(grantedLink).toBeVisible();
  await expect(page).toHaveURL(/\/operators$/);
  expect(order).toEqual(['session', 'save', 'session']);
});

test('an already signed-in user picks up a grant on navigation and a revocation on reload', async ({ page }) => {
  const start = ['analysis.spc', 'master.parts'];
  await prepare(page, 'Editor', start);
  let permissions = start;
  await page.route('**/api/v1/auth/session', route => route.fulfill({ json: { user: {
    id: 101, username: 'fixture', role: 'Editor', permissions
  } } }));
  await page.goto('/parts');
  await expect(page.locator('aside a[href="/parts"]')).toBeVisible();
  permissions = [...start, 'analysis.trend'];
  await page.locator('aside a[href="/spc"]').click();
  await expect(page.locator('aside a[href="/trend-chart"]')).toBeVisible();
  permissions = ['analysis.trend'];
  await page.reload();
  await expect(page).toHaveURL(/\/trend-chart\?access=page$/);
  await expect(page.locator('aside a[href="/parts"]')).toHaveCount(0);
});

test('with no remaining access the user can recover by reloading after a grant', async ({ page }) => {
  await prepare(page, 'Editor', ['admin.operators']);
  let permissions: string[] = [];
  await page.route('**/api/v1/auth/session', route => route.fulfill({ json: { user: {
    id: 101, username: 'fixture', role: 'Editor', permissions
  } } }));
  await page.goto('/operators');
  await expect(page).toHaveURL(/\/login\?access=page$/);
  await expect(page.getByRole('alert')).toBeVisible();
  permissions = ['master.parts'];
  await page.reload();
  await expect(page).toHaveURL(/\/parts$/);
});
