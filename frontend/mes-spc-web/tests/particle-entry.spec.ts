import { test, expect } from '@playwright/test';

async function pasteIntoGrid(page, text: string) {
  await page.getByTestId('particle-paste-grid').evaluate((element, value) => {
    const clipboardData = new DataTransfer();
    clipboardData.setData('text/plain', value);
    element.dispatchEvent(new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData }));
  }, text);
}

test('particle entry shows the bordered floor plan before the bordered paste area', async ({ page }) => {
  await page.route('**/api/version', route => route.fulfill({ json: { version: '0.1.test', environment: 'unknown' } }));

  await page.goto('/particle-monitoring/entry');

  const floorPlanSection = page.getByTestId('particle-floor-plan-section');
  const floorPlan = page.getByTestId('particle-floor-plan');
  const pasteSection = page.getByTestId('particle-paste-section');

  await expect(floorPlanSection).toBeVisible();
  await expect(floorPlan).toBeVisible();
  await expect(floorPlan).toHaveAttribute('src', '/assets/particle-floor-plan.png');
  await expect.poll(() => floorPlan.evaluate((image: HTMLImageElement) => image.naturalWidth)).toBeGreaterThan(0);

  const floorPlanBox = await floorPlanSection.boundingBox();
  const pasteBox = await pasteSection.boundingBox();
  expect(floorPlanBox).not.toBeNull();
  expect(pasteBox).not.toBeNull();
  expect(floorPlanBox!.y + floorPlanBox!.height).toBeLessThanOrEqual(pasteBox!.y);

  const floorPlanBorder = await floorPlanSection.evaluate(element => getComputedStyle(element).borderTopWidth);
  const pasteBorder = await pasteSection.evaluate(element => getComputedStyle(element).borderTopWidth);
  expect(floorPlanBorder).not.toBe('0px');
  expect(pasteBorder).not.toBe('0px');
});

test('particle entry shows the fixed yellow 49-column grid and maps pasted values before preview', async ({ page }) => {
  let previewRequests = 0;
  await page.route('**/api/version', route => route.fulfill({ json: { version: '0.1.test', environment: 'unknown' } }));
  await page.route('**/api/v1/uploads/particle/preview', route => {
    previewRequests++;
    return route.fulfill({ json: { uploadBatchId: '22222222-2222-2222-2222-222222222222', importStatus: 'PreviewReady', totalRows: 48, validRows: 48, errorRows: 0, duplicateRows: 0 } });
  });

  await page.goto('/particle-monitoring/entry');

  const grid = page.getByTestId('particle-paste-grid');
  await expect(grid).toBeVisible();
  await expect(page.locator('#particle-paste')).toHaveCount(0);
  await expect(grid.locator('thead tr').first().getByRole('columnheader')).toHaveCount(13);
  await expect(grid.locator('thead tr').nth(1).getByRole('columnheader')).toHaveCount(48);
  await expect(grid.getByRole('columnheader', { name: 'R1', exact: true })).toBeVisible();
  await expect(grid.getByRole('columnheader', { name: 'R12', exact: true })).toBeVisible();
  await expect(grid.getByRole('columnheader', { name: /日期時間.*位置/ })).toBeVisible();
  const headerColor = await grid.getByRole('columnheader', { name: 'R1', exact: true }).evaluate(element => getComputedStyle(element).backgroundColor);
  expect(headerColor).not.toBe('rgba(0, 0, 0, 0)');

  const values = Array.from({ length: 48 }, (_, index) => String(index));
  await pasteIntoGrid(page, `2026/11/25 08:30\t${values.join('\t')}`);

  await expect(page.getByTestId('particle-time-0')).toContainText('2026/11/25');
  await expect(page.getByTestId('particle-time-0')).toContainText('08:30');
  await expect(page.getByTestId('particle-cell-0-0')).toHaveText('0');
  await expect(page.getByTestId('particle-cell-0-47')).toHaveText('47');
  expect(previewRequests).toBe(0);

  await page.getByRole('button', { name: '清除未確認草稿' }).click();
  await expect(page.getByTestId('particle-grid-empty')).toBeVisible();
});

test('particle entry pastes R1-R12, previews, and confirms without writing during paste', async ({ page }) => {
  let previewPayload: any = null;
  let confirmed = false;
  await page.route('**/api/version', route => route.fulfill({ json: { version: '0.1.test', environment: 'unknown' } }));
  await page.route('**/api/v1/uploads/particle/preview', async route => {
    previewPayload = await route.request().postDataJSON();
    await route.fulfill({ json: { uploadBatchId: '11111111-1111-1111-1111-111111111111', importStatus: 'PreviewReady', totalRows: 48, validRows: 48, errorRows: 0, duplicateRows: 0 } });
  });
  await page.route('**/api/v1/uploads/11111111-1111-1111-1111-111111111111/confirm*', async route => {
    confirmed = true;
    await route.fulfill({ json: { uploadBatchId: '11111111-1111-1111-1111-111111111111', insertedRows: 48, skippedRows: 0, errorRows: 0 } });
  });

  await page.goto('/particle-monitoring/entry');
  const values = Array.from({ length: 48 }, (_, index) => String(index));
  await pasteIntoGrid(page, `2026/11/25 08:30\t${values.join('\t')}`);
  await expect(page.getByText('有效值 48 筆')).toBeVisible();
  expect(previewPayload).toBeNull();
  await page.getByTestId('particle-preview').click();
  await expect(page.getByText('有效值 48 筆')).toBeVisible();
  await expect.poll(() => previewPayload).not.toBeNull();
  expect(previewPayload.sourceType).toBe('WebPaste');
  expect(previewPayload.rows).toHaveLength(48);
  expect(previewPayload.rows[0]).toMatchObject({ location: 'R1', particleSize: '0.5', count: '0', sourceColumn: 'B' });
  expect(previewPayload.rows[47]).toMatchObject({ location: 'R12', particleSize: '10', count: '47', sourceColumn: 'AW' });
  expect(confirmed).toBe(false);
  await page.getByTestId('particle-confirm').click();
  await expect(page.getByText('已確認寫入 48 筆，略過 0 筆。')).toBeVisible();
  expect(confirmed).toBe(true);
});
