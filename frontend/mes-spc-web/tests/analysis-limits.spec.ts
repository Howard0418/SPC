import { test, expect } from '@playwright/test';

test('keeps dirty rows across pages and applies one atomic batch', async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('mes_spc_token', 'mock-editor-token');
    localStorage.setItem('mes_spc_user', JSON.stringify({ id: 101, role: 'Editor', username: 'limits-editor' }));
  });
  const mappings = Array.from({ length: 6 }, (_, index) => ({
    id: index + 1, controlScope: 'PROCESS', processId: 1, characteristicId: index + 1,
    chartTypeId: 1, isEnabled: true, unit: 'mg/L', lsl: index * 10, targetValue: index * 10 + 5,
    usl: index * 10 + 10, lcl: null, cl: null, ucl: null,
    process: { processCode: 'P1', processName: 'Process 1' },
    characteristic: { characteristicCode: `Q${index + 1}`, characteristicName: `Item ${index + 1}` },
    hasLimitSegments: index === 0
  }));
  let previewBody: any = null;
  let applyBody: any = null;
  let applied = false;
  await page.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (!url.pathname.startsWith('/api/')) return route.continue();
    if (url.pathname === '/api/version') return route.fulfill({ json: { version: 'test', environment: 'unknown' } });
    if (url.pathname === '/api/part-process-characteristics') return route.fulfill({ json: mappings });
    if (url.pathname === '/api/analysis-limits/preview') {
      previewBody = route.request().postDataJSON();
      return route.fulfill({ json: { success: true, data: { isValid: true, applyToken: 'token', items: previewBody.items.map((item: any) => ({ ...item, errors: [] })) } } });
    }
    if (url.pathname === '/api/analysis-limits/apply') {
      applyBody = route.request().postDataJSON();
      applied = true;
      return route.fulfill({ json: { success: true, data: { batchId: '00000000-0000-0000-0000-000000000001', updatedCount: 2 } } });
    }
    if (url.pathname === '/api/v1/audit/activity') return route.fulfill({ status: 204 });
    return route.fulfill({ json: {} });
  });

  await page.goto('/analysis-limit-overview');
  await expect(page.getByTestId('limits-row-1')).toBeVisible();
  await page.getByLabel('USL Item 1').fill('11');
  await page.locator('select').filter({ has: page.locator('option[value="5"]') }).selectOption('5');
  await page.getByRole('button', { name: '下一頁' }).click();
  await expect(page.getByTestId('limits-row-6')).toBeVisible();
  await page.getByLabel('LCL Item 6', { exact: true }).fill('51');
  await page.getByLabel('CL Item 6', { exact: true }).fill('55');
  await page.getByLabel('UCL Item 6', { exact: true }).fill('59');
  await page.getByTestId('limits-preview').click();

  await expect(page.getByTestId('limits-preview-result')).toContainText('全部異動列均通過');
  expect(previewBody.items).toHaveLength(2);
  expect(previewBody.items.map((item: any) => item.partProcessCharacteristicId)).toEqual([1, 6]);
  await page.getByTestId('limits-apply').click();
  await expect.poll(() => applied).toBe(true);
  expect(applyBody).toEqual({ applyToken: 'token' });
  await expect(page.getByText('已成功儲存 2 筆界限異動。')).toBeVisible();
});

test('shows server correction guidance and does not expose apply for invalid values', async ({ page }) => {
  await page.addInitScript(() => {
    localStorage.setItem('mes_spc_token', 'mock-editor-token');
    localStorage.setItem('mes_spc_user', JSON.stringify({ id: 101, role: 'Editor', username: 'limits-editor' }));
  });
  await page.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (!url.pathname.startsWith('/api/')) return route.continue();
    if (url.pathname === '/api/version') return route.fulfill({ json: { version: 'test', environment: 'unknown' } });
    if (url.pathname === '/api/part-process-characteristics') return route.fulfill({ json: [{ id: 1, controlScope: 'PROCESS', isEnabled: true, lsl: 0, targetValue: 5, usl: 10, lcl: null, cl: null, ucl: null, process: { processCode: 'P1' }, characteristic: { characteristicName: 'Item 1' } }] });
    if (url.pathname === '/api/analysis-limits/preview') return route.fulfill({ json: { success: true, data: { isValid: false, applyToken: null, items: [{ partProcessCharacteristicId: 1, errors: [{ field: 'LSL', code: 'INVALID_NUMBER', message: 'LSL 不是有效數字。', suggestion: '請移除單位、百分比、逗號及 ± 文字。' }] }] } } });
    if (url.pathname === '/api/v1/audit/activity') return route.fulfill({ status: 204 });
    return route.fulfill({ json: {} });
  });

  await page.goto('/analysis-limit-overview');
  await page.getByLabel('LSL Item 1').fill('95%');
  await page.getByTestId('limits-preview').click();
  await expect(page.getByTestId('limits-preview-result')).toContainText('請移除單位、百分比、逗號及 ± 文字');
  await expect(page.getByTestId('limits-apply')).toBeDisabled();
});
