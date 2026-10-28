import { test, expect } from '@playwright/test';
import { prepare } from './fixtures/chart-query';

test('monthly view separates chemical trend and exports the selected leap month', async ({ page }) => {
  const { reads, writes } = await prepare(page, '/monthly-control-chart');
  await page.route('**/api/control-chart-groups*', route => route.fulfill({ json: [
    { id: 1, groupCode: 'PROCESS', groupName: '製程管制', groupType: 'CONTROL_CHART', isEnabled: true },
    { id: 2, groupCode: 'CHEM', groupName: '藥液管制', groupType: 'CONTROL_CHART', isEnabled: true },
    { id: 3, groupCode: 'CHEM', groupName: '藥液趨勢', groupType: 'TREND_CHART', isEnabled: true }
  ] }));
  await page.reload();
  await page.getByTestId('monthly-chart-kind').selectOption('TREND_CHART');
  await expect(page.getByTestId('chart-dimension-CHEM')).toHaveText('藥液趨勢');
  await page.getByTestId('chart-dimension-CHEM').click();
  await page.getByTestId('chart-query-reportMonth').fill('2024-02');
  let exported: URL | undefined;
  await page.route('**/api/v1/spc-report-settings/export*', route => {
    exported = new URL(route.request().url());
    return route.fulfill({ body: 'workbook-test', contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
  });
  await page.getByTestId('monthly-export').click();
  await expect.poll(() => exported?.searchParams.get('start')).toBe('2024-02-01T00:00:00.000Z');
  expect(exported?.searchParams.get('end')).toBe('2024-03-01T00:00:00.000Z');
  expect(writes).toEqual([]);
});
