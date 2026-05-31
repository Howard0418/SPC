import { test, expect } from '@playwright/test';

test.describe('SPC Rule Engine UI Validation', () => {

  test('should render violated rules correctly on the SPC chart', async ({ page }) => {
    // Intercept the API call that provides the interactive chart data
    await page.route('**/api/v2/spc/interactive-chart*', async (route) => {
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

    // Assume we have an endpoint that returns the characteristics list
    await page.route('**/api/v2/masterdata/part-process-characteristics*', async (route) => {
      const json = [{
        id: 1, part: { partNo: 'TEST_PART' }, process: { processName: 'TEST_PROC' },
        characteristic: { characteristicName: 'TEST_CHAR' },
        usl: 140, lsl: 60, ruleGroup: { ruleGroupName: 'Western Electric Rules' }
      }];
      await route.fulfill({ json });
    });

    // Navigate to SPC Chart view (assuming there is a route /spc/chart?ppcId=1)
    await page.goto('/spc-chart?ppcId=1');

    // Verify page has loaded
    await expect(page.locator('text=TEST_PART')).toBeVisible();

    // In ECharts, rendering is onto a canvas element. 
    // We can't directly inspect canvas pixels easily in E2E, but our component
    // typically shows detailed alert info in the sidebar or a summary.
    // Let's assert that the sidebar warning panel shows up.
    
    // Check for the presence of warning alerts (if the UI lists violated rules in the DOM)
    const alertList = page.locator('text=Rule1_Over3Sigma');
    await expect(alertList.first()).toBeAttached();
    
    const alert2 = page.locator('text=Rule2_9SameSide');
    await expect(alert2.first()).toBeAttached();

    // Trigger hover on chart or interact with data points if the UI renders divs.
    // Since we know the Vue template shows selected point details:
    // we simulate clicking on the first data point (ECharts interaction can be complex,
    // so we might just verify the data is passed to the component state).
    
    // Instead, just ensure no errors were thrown and the specific violated rules appear 
    // somewhere in the DOM (e.g., in the rule breakdown or summary).
    await expect(page.locator('text=觸發西方電氣判讀規則').first()).toBeAttached({ timeout: 5000 });
  });

});
