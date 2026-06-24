import { test, expect } from '@playwright/test';

test.describe('SPC Rule Engine UI Validation', () => {

  test('should render violated rules correctly on the SPC chart', async ({ page }) => {
    // Intercept the API call that provides the interactive chart data
    await page.route('**/api/v1/spc/chart*', async (route) => {
      const json = {
        chartType: 'I-MR',
        limits: { ucl: 130, cl: 100, lcl: 70, target: 100 },
        statControlLimits: {
          iControlLimitsStat: { cl: 100, ucl: 130, lcl: 70 },
          mrControlLimitsStat: { cl: 10, ucl: 32.67, lcl: 0 }
        },
        chartData: {
          points: [
            // Rule 1: Point 0 over 3 Sigma
            { measuredAt: '2026-05-28T10:00:00', value: 140, outOfControl: true, violatedRules: ['Rule1_Over3Sigma'] },
            // Rule 2: 9 points on the same side
            ...Array.from({ length: 9 }).map((_, i) => ({
              measuredAt: `2026-05-28T10:0${i + 1}:00`, value: 110, 
              outOfControl: i === 8, 
              violatedRules: i === 8 ? ['Rule2_9SameSide'] : []
            })),
            // Rule 3: 6 consecutive points steadily increasing
            ...Array.from({ length: 6 }).map((_, i) => ({
              measuredAt: `2026-05-28T10:1${i}:00`, value: 100 + i * 2, 
              outOfControl: i === 5, 
              violatedRules: i === 5 ? ['Rule3_6Trend'] : []
            })),
            // Rule 4: 14 points alternating up and down
            ...Array.from({ length: 14 }).map((_, i) => ({
              measuredAt: `2026-05-28T10:2${i}:00`, value: 100 + (i % 2 === 0 ? 5 : -5), 
              outOfControl: i === 13, 
              violatedRules: i === 13 ? ['Rule4_14Alternating'] : []
            })),
            // Normal point
            { measuredAt: '2026-05-28T10:50:00', value: 100, outOfControl: false, violatedRules: [] }
          ]
        },
        secondaryChartData: { points: [] },
        subgroupSize: 1,
        capability: { cp: 1.5, cpk: 1.4 }
      };
      await route.fulfill({ json });
    });

    await page.route('**/api/part-process-characteristics*', async (route) => {
      const json = [{
        id: 1,
        controlScope: 'PRODUCT',
        partId: 1,
        processId: 1,
        characteristicId: 1,
        chartTypeId: 1,
        isEnabled: true,
        part: { id: 1, partNo: 'TEST_PART', partName: 'Test Part', isEnabled: true },
        process: { id: 1, processCode: 'TEST_PROC', processName: 'TEST_PROC', isEnabled: true },
        characteristic: { id: 1, characteristicCode: 'TEST_CHAR', characteristicName: 'TEST_CHAR', isEnabled: true, isSpcEnabled: true },
        usl: 140,
        lsl: 60,
        ruleGroup: { ruleGroupName: 'Western Electric Rules' }
      }];
      await route.fulfill({ json });
    });

    await page.route('**/api/control-chart-types*', async (route) => {
      await route.fulfill({ json: [{ id: 1, chartCategoryId: 1, chartTypeCode: 'I_MR', chartTypeName: 'I-MR' }] });
    });
    await page.route('**/api/control-chart-categories*', async (route) => {
      await route.fulfill({ json: [{ id: 1, chartGroupId: 1, categoryCode: 'VAR_PROD', categoryName: '產品管制' }] });
    });
    await page.route('**/api/control-chart-groups*', async (route) => {
      await route.fulfill({ json: [{ id: 1, groupCode: 'PROD', groupName: '產品管制' }] });
    });

    await page.goto('/spc?ppcId=1');

    await expect(page.getByText('料號 (Product)')).toHaveCount(0);
    await expect(page.getByText('線別', { exact: true })).toHaveCount(0);

    await expect(page.locator('canvas').first()).toBeVisible({ timeout: 10000 });
    await expect(page.locator('text=規格/管制界限失控點')).toBeVisible();
  });

});
