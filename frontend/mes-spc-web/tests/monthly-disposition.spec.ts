import { test, expect } from '@playwright/test';
import { prepare } from './fixtures/chart-query';

test('monthly table presents current disposition and preserves counts across languages', async ({ page }) => {
  await prepare(page, '/monthly-control-chart');
  await page.route('**/api/v1/spc/summary*', route => route.fulfill({ json: [{
    partProcessCharacteristicId: 1, controlCategory: '製程管制', lineOrProcessName: '測試製程',
    chartName: '測試項目', totalCount: 1001, oosCount: 4, oosPercentage: 0.4,
    abnormalCount: 4, handledCount: 2, unhandledCount: 2
  }] }));
  await page.getByTestId('chart-query-selectedProcessId').selectOption('ALL');
  await page.getByTestId('chart-query-submit').click();
  const table=page.getByTestId('spc-summary-table');
  await expect(table.getByTestId('summary-handled-1')).toHaveText('2');
  await expect(table.getByTestId('summary-unhandled-1')).toHaveText('2');
  await expect(table).toContainText('已處置件數');await expect(table).toContainText('未處置件數');
  await expect(table.getByTestId('summary-disposition-note')).toContainText('非月底快照');
  await page.getByTestId('locale-select').selectOption('en-US');
  await expect(table.getByTestId('summary-handled-1')).toHaveText('2');
  await expect(table.getByTestId('summary-disposition-note')).toContainText('current');
  await page.getByTestId('locale-select').selectOption('zh-TW');
  await table.getByTestId('summary-handled-1').scrollIntoViewIfNeeded();
  await table.screenshot({path: test.info().outputPath('monthly-disposition-fixture.png')});
});
