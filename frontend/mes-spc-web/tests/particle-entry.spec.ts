import { test, expect } from '@playwright/test';

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
  await page.getByTestId('particle-paste').fill(`2026/11/25 08:30\t${values.join('\t')}`);
  await expect(page.getByText('有效值 48 筆')).toHaveCount(0);
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
