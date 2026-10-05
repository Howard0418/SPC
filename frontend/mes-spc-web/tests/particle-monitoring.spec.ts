import { test, expect } from '@playwright/test';

test('Particle Monitoring supports C/U selection and linked views', async ({ page }) => {
  await page.route('**/api/version', route => route.fulfill({ json: { version: 'test', environment: 'test' } }));
  await page.route('**/api/v1/particles/spc*', route => route.fulfill({ json: {
    chartType: 'C_CHART', samplingBasis: 'constant-assumed', warning: '目前採固定採樣基準。',
    pointCount: 2, controlStatus: 'insufficientData', statControlLimits: null,
    points: [
      { measurementId: 1, time: '2026-10-01T00:00:00Z', count: 12, value: 12, isOutOfControl: false, violatedRules: [] },
      { measurementId: 2, time: '2026-10-02T00:00:00Z', count: 15, value: 15, isOutOfControl: false, violatedRules: [] }
    ]
  }}));
  await page.route('**/api/v1/particles/location-comparison*', route => route.fulfill({ json: {
    isAmbiguous: false, candidates: [], items: Array.from({ length: 9 }, (_, index) => ({
      location: `R${index + 1}`, count: index + 10, measurementId: index + 1, isMissing: false
    }))
  }}));
  await page.route('**/api/v1/particles/measurements*', route => route.fulfill({ json: {
    total: 1, page: 1, pageSize: 50, items: [{ id: 1, measurementTime: '2026-10-01T00:00:00Z', location: 'R1', particleSize: 0.5, count: 12, sourceSheet: '10月', sourceRow: 26, sourceColumn: 'C' }]
  }}));

  await page.goto('/particle-monitoring');
  await expect(page.getByRole('heading', { name: 'Particle Monitoring' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'C-chart' })).toHaveAttribute('aria-pressed', 'true');
  await page.getByRole('button', { name: '查詢' }).click();
  await expect(page.locator('canvas')).toHaveCount(2);
  await expect(page.getByText('10月 / C26')).toBeVisible();
  await page.getByRole('button', { name: 'U-chart' }).click();
  await expect(page.getByRole('button', { name: 'U-chart' })).toHaveAttribute('aria-pressed', 'true');
});
