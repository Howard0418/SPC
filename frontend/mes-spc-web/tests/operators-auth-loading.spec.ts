import { expect, test, type Page } from '@playwright/test';

const seedEditor = async (page: Page) => {
  await page.addInitScript(() => {
    localStorage.setItem('mes_spc_token', 'expired-token');
    localStorage.setItem('mes_spc_user', JSON.stringify({
      id: 101,
      role: 'Editor',
      username: 'operator-admin',
      permissions: ['admin.operators']
    }));
  });
};

test('operators page ends loading when an earlier auth redirect guard is still active', async ({ page }) => {
  await seedEditor(page);
  await page.addInitScript(() => {
    sessionStorage.setItem('mes_spc_auth_redirecting_until', String(Date.now() + 5000));
  });
  await page.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (!url.pathname.startsWith('/api/')) return route.continue();
    if (!url.pathname.startsWith('/api/operators')) {
      return route.fulfill({ json: url.pathname.endsWith('/version') ? { version: 'test', environment: 'test' } : [] });
    }
    await new Promise(resolve => setTimeout(resolve, 100));
    await route.fulfill({ status: 401, json: { message: '未授權' } });
  });

  await page.goto('/operators');

  await expect(page.getByTestId('settings-load')).toBeEnabled({ timeout: 1500 });
  await expect(page.locator('.bg-red-50')).toBeVisible();
  await expect(page).toHaveURL(/\/operators$/);
});

test('operators page partial API failure ends loading and shows a readable error', async ({ page }) => {
  await seedEditor(page);
  await page.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (!url.pathname.startsWith('/api/')) return route.continue();
    if (!url.pathname.startsWith('/api/operators')) {
      return route.fulfill({ json: url.pathname.endsWith('/version') ? { version: 'test', environment: 'test' } : [] });
    }
    await new Promise(resolve => setTimeout(resolve, 100));
    if (url.pathname.endsWith('/operators/permissions-catalog')) {
      return route.fulfill({ status: 500, json: { message: '權限目錄暫時無法載入' } });
    }
    return route.fulfill({ json: [] });
  });

  await page.goto('/operators');

  await expect(page.getByTestId('settings-load')).toBeEnabled();
  await expect(page.locator('.bg-red-50')).toBeVisible();
});
