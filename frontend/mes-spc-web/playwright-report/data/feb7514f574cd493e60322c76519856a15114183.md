# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: spc-ui.spec.ts >> SPC Rule Engine UI Validation >> should render violated rules correctly on the SPC chart
- Location: tests\spc-ui.spec.ts:5:3

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: locator('text=TEST_PART')
Expected: visible
Timeout: 5000ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 5000ms
  - waiting for locator('text=TEST_PART')

```

```yaml
- banner:
  - heading "PMR SPC Enterprise" [level=1]
  - paragraph: 智能統計製程與製造品質分析系統
  - button "切換深色模式"
- complementary:
  - heading "高階戰情與分析" [level=3]
  - link "戰情儀表板":
    - /url: /
  - link "SPC 管制圖":
    - /url: /spc
  - heading "自動化匯入與採樣" [level=3]
  - link "計量型資料匯入":
    - /url: /uploads/variable
  - link "計數型資料匯入":
    - /url: /uploads/attribute
  - link "單筆量測資料輸入":
    - /url: /measurements
  - heading "企業品質主檔設定" [level=3]
  - link "產品管理":
    - /url: /products
  - link "產品料號主檔":
    - /url: /parts
  - link "工站製程主檔":
    - /url: /processes
  - link "生產機台主檔":
    - /url: /machines
  - link "品質特性項目":
    - /url: /characteristics
  - link "料號檢驗基準設定":
    - /url: /part-process-characteristics
  - heading "管制圖與西方電氣規則" [level=3]
  - link "管制圖分類總管":
    - /url: /control-chart-groups
  - link "管制圖分類維護":
    - /url: /control-chart-categories
  - link "管制圖參數配置":
    - /url: /control-chart-types
  - heading "異常管理與追溯" [level=3]
  - link "異常通報總覽":
    - /url: /alerts
  - link "多維度品質履歷與查詢":
    - /url: /spc/query
  - link "異常單簽核處置":
    - /url: /v2/alerts-workflow
  - heading "系統管理與通報設定" [level=3]
  - link "作業工程師與權限":
    - /url: /operators
  - link "SMTP 郵件與預警設定":
    - /url: /settings/smtp
  - paragraph: © 2026 PMR Quality System
  - paragraph: v10.0 Enterprise SPC Edition
- main
```

# Test source

```ts
  1  | import { test, expect } from '@playwright/test';
  2  | 
  3  | test.describe('SPC Rule Engine UI Validation', () => {
  4  | 
  5  |   test('should render violated rules correctly on the SPC chart', async ({ page }) => {
  6  |     // Intercept the API call that provides the interactive chart data
  7  |     await page.route('**/api/v2/spc/interactive-chart*', async (route) => {
  8  |       const json = {
  9  |         chartType: 'I-MR',
  10 |         limits: { ucl: 130, cl: 100, lcl: 70, target: 100 },
  11 |         statControlLimits: {
  12 |           iControlLimitsStat: { cl: 100, ucl: 130, lcl: 70 },
  13 |           mrControlLimitsStat: { cl: 10, ucl: 32.67, lcl: 0 }
  14 |         },
  15 |         chartData: {
  16 |           points: [
  17 |             // Rule 1: Point 0 over 3 Sigma
  18 |             { measuredAt: '2026-05-28T10:00:00', value: 140, outOfControl: true, violatedRules: ['Rule1_Over3Sigma'] },
  19 |             // Rule 2: 9 points on the same side
  20 |             ...Array.from({ length: 9 }).map((_, i) => ({
  21 |               measuredAt: `2026-05-28T10:0${i + 1}:00`, value: 110, 
  22 |               outOfControl: i === 8, 
  23 |               violatedRules: i === 8 ? ['Rule2_9SameSide'] : []
  24 |             })),
  25 |             // Rule 3: 6 consecutive points steadily increasing
  26 |             ...Array.from({ length: 6 }).map((_, i) => ({
  27 |               measuredAt: `2026-05-28T10:1${i}:00`, value: 100 + i * 2, 
  28 |               outOfControl: i === 5, 
  29 |               violatedRules: i === 5 ? ['Rule3_6Trend'] : []
  30 |             })),
  31 |             // Rule 4: 14 points alternating up and down
  32 |             ...Array.from({ length: 14 }).map((_, i) => ({
  33 |               measuredAt: `2026-05-28T10:2${i}:00`, value: 100 + (i % 2 === 0 ? 5 : -5), 
  34 |               outOfControl: i === 13, 
  35 |               violatedRules: i === 13 ? ['Rule4_14Alternating'] : []
  36 |             })),
  37 |             // Normal point
  38 |             { measuredAt: '2026-05-28T10:50:00', value: 100, outOfControl: false, violatedRules: [] }
  39 |           ]
  40 |         },
  41 |         secondaryChartData: { points: [] },
  42 |         subgroupSize: 1,
  43 |         capability: { cp: 1.5, cpk: 1.4 }
  44 |       };
  45 |       await route.fulfill({ json });
  46 |     });
  47 | 
  48 |     // Assume we have an endpoint that returns the characteristics list
  49 |     await page.route('**/api/v2/masterdata/part-process-characteristics*', async (route) => {
  50 |       const json = [{
  51 |         id: 1, part: { partNo: 'TEST_PART' }, process: { processName: 'TEST_PROC' },
  52 |         characteristic: { characteristicName: 'TEST_CHAR' },
  53 |         usl: 140, lsl: 60, ruleGroup: { ruleGroupName: 'Western Electric Rules' }
  54 |       }];
  55 |       await route.fulfill({ json });
  56 |     });
  57 | 
  58 |     // Navigate to SPC Chart view (assuming there is a route /spc/chart?ppcId=1)
  59 |     await page.goto('/spc-chart?ppcId=1');
  60 | 
  61 |     // Verify page has loaded
> 62 |     await expect(page.locator('text=TEST_PART')).toBeVisible();
     |                                                  ^ Error: expect(locator).toBeVisible() failed
  63 | 
  64 |     // In ECharts, rendering is onto a canvas element. 
  65 |     // We can't directly inspect canvas pixels easily in E2E, but our component
  66 |     // typically shows detailed alert info in the sidebar or a summary.
  67 |     // Let's assert that the sidebar warning panel shows up.
  68 |     
  69 |     // Check for the presence of warning alerts (if the UI lists violated rules in the DOM)
  70 |     const alertList = page.locator('text=Rule1_Over3Sigma');
  71 |     await expect(alertList.first()).toBeAttached();
  72 |     
  73 |     const alert2 = page.locator('text=Rule2_9SameSide');
  74 |     await expect(alert2.first()).toBeAttached();
  75 | 
  76 |     // Trigger hover on chart or interact with data points if the UI renders divs.
  77 |     // Since we know the Vue template shows selected point details:
  78 |     // we simulate clicking on the first data point (ECharts interaction can be complex,
  79 |     // so we might just verify the data is passed to the component state).
  80 |     
  81 |     // Instead, just ensure no errors were thrown and the specific violated rules appear 
  82 |     // somewhere in the DOM (e.g., in the rule breakdown or summary).
  83 |     await expect(page.locator('text=觸發西方電氣判讀規則').first()).toBeAttached({ timeout: 5000 });
  84 |   });
  85 | 
  86 | });
  87 | 
```