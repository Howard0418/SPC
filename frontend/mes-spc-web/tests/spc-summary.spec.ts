import { test, expect } from '@playwright/test';

test.describe('SPC all-lines summary', () => {
  test('shows the requested columns and opens a single control chart', async ({ page }) => {
    let requestedDimension = '';
    let requestedPartId = '';
    let chartPartId = '';

    await page.route('**/api/part-process-characteristics*', async route => {
      await route.fulfill({
        json: [{
          id: 21,
          controlScope: 'CHEMICAL',
          partId: null,
          processId: 3,
          machineId: 8,
          characteristicId: 5,
          chartTypeId: 2,
          isEnabled: true,
          process: { id: 3, processCode: 'ETCH', processName: '蝕刻製程', isEnabled: true },
          machine: { id: 8, machineCode: 'LINE-A', machineName: 'A線', isEnabled: true },
          characteristic: {
            id: 5,
            characteristicCode: 'CU',
            characteristicName: '銅離子濃度',
            isEnabled: true,
            isSpcEnabled: true
          }
        }, {
          id: 31,
          controlScope: 'PRODUCT',
          partId: 9,
          processId: 4,
          characteristicId: 6,
          chartTypeId: 4,
          isEnabled: true,
          part: { id: 9, partNo: 'P-001', partName: '測試產品', isEnabled: true },
          process: { id: 4, processCode: 'ASSY', processName: '組裝製程', isEnabled: true },
          characteristic: {
            id: 6,
            characteristicCode: 'WIDTH',
            characteristicName: '產品寬度',
            isEnabled: true,
            isSpcEnabled: true
          }
        }, {
          id: 41,
          controlScope: 'PROCESS',
          partId: null,
          processId: 3,
          characteristicId: 7,
          chartTypeId: 3,
          isEnabled: true,
          process: { id: 3, processCode: 'ETCH', processName: '蝕刻製程', isEnabled: true },
          characteristic: {
            id: 7,
            characteristicCode: 'TEMP',
            characteristicName: '環境溫度',
            isEnabled: true,
            isSpcEnabled: true
          }
        }]
      });
    });
    await page.route('**/api/control-chart-types*', async route => {
      await route.fulfill({ json: [
        { id: 2, chartCategoryId: 2, chartTypeCode: 'I_MR', chartTypeName: '單值-移動全距圖' },
        { id: 3, chartCategoryId: 3, chartTypeCode: 'ENV_I_MR', chartTypeName: '環境單值圖' },
        { id: 4, chartCategoryId: 4, chartTypeCode: 'PROD_I_MR', chartTypeName: '產品單值圖' }
      ] });
    });
    await page.route('**/api/control-chart-categories*', async route => {
      await route.fulfill({ json: [
        { id: 2, chartGroupId: 2, categoryCode: 'CHEM_VAR', categoryName: '藥液計量管制' },
        { id: 3, chartGroupId: 3, categoryCode: 'ENV_VAR', categoryName: '環境計量管制' },
        { id: 4, chartGroupId: 4, categoryCode: 'PROD_VAR', categoryName: '產品計量管制' }
      ] });
    });
    await page.route('**/api/control-chart-groups*', async route => {
      await route.fulfill({ json: [
        { id: 1, groupCode: 'PROC', groupName: '製程管制', isEnabled: true },
        { id: 2, groupCode: 'CHEM', groupName: '藥液管制', isEnabled: true },
        { id: 3, groupCode: 'ENV', groupName: '環境管制', isEnabled: true },
        { id: 4, groupCode: 'PROD', groupName: '產品管制', isEnabled: true }
      ] });
    });
    await page.route('**/api/v1/spc/summary*', async route => {
      const requestUrl = new URL(route.request().url());
      requestedDimension = requestUrl.searchParams.get('dimension') || '';
      requestedPartId = requestUrl.searchParams.get('partId') || '';
      const isProduct = requestedDimension === 'PRODUCT';
      await route.fulfill({
        json: [{
          partProcessCharacteristicId: isProduct ? 31 : 21,
          controlCategory: isProduct ? '產品管制' : '藥液管制',
          lineOrProcessName: isProduct ? '組裝製程' : '蝕刻製程 / A線',
          chartName: isProduct ? '產品寬度' : '銅離子濃度',
          chartType: '單值-移動全距圖',
          usl: 10,
          lsl: 0,
          ucl: 8.2,
          lcl: 2.1,
          limitCalculationMethod: '移動全距法',
          oosCount: 1,
          oosPercentage: 12.5,
          ca: 0.1,
          pp: 1.4,
          ppk: 1.2,
          responsibleUser: '王工程師',
          remarks: '已完成補藥確認'
        }]
      });
    });
    await page.route('**/api/v1/spc/chart*', async route => {
      chartPartId = new URL(route.request().url()).searchParams.get('partId') || '';
      await route.fulfill({
        json: {
          chartType: 'I-MR',
          limits: { usl: 10, lsl: 0, target: 5 },
          statControlLimits: {
            iControlLimitsStat: { cl: 5, ucl: 8.2, lcl: 2.1 },
            mrControlLimitsStat: { cl: 0.5, ucl: 1.6335, lcl: 0 }
          },
          chartData: {
            points: [
              { measuredAt: '2026-06-20T08:00:00', value: 5, outOfSpec: false, outOfControl: false, violatedRules: [] },
              { measuredAt: '2026-06-20T09:00:00', value: 6, outOfSpec: false, outOfControl: false, violatedRules: [] }
            ]
          },
          secondaryChartData: { points: [{ value: 1, outOfControl: false }] },
          rawDataPoints: [
            { measuredAt: '2026-06-20T08:00:00', value: 5, isExcluded: false },
            { measuredAt: '2026-06-20T09:00:00', value: 6, isExcluded: false }
          ],
          subgroupSize: 1,
          capability: { ca: 0.1, cp: 1.5, cpk: 1.4, pp: 1.4, ppk: 1.2 }
        }
      });
    });

    await page.goto('/spc');
    await expect(page.getByPlaceholder('輸入關鍵字快速搜尋...')).toHaveCount(0);
    await expect(page.getByText('檢驗項目 (Inspection Item)')).toHaveCount(0);
    await expect(page.getByText('工單 / 批號過濾')).toHaveCount(0);
    await expect(page.getByRole('button', { name: '環境管制' })).toBeVisible();
    await page.getByRole('button', { name: '藥液管制' }).click();
    await page.locator('select').first().selectOption('ALL');
    await page.getByRole('button', { name: '重新計算' }).click();

    const summaryTable = page.getByTestId('spc-summary-table');
    await expect(summaryTable).toBeVisible();
    expect(requestedDimension).toBe('CHEMICAL');

    // 確認各欄位標題文字存在（表頭現已支援排序，含有圖示元素）
    const thead = summaryTable.locator('thead');
    await expect(thead).toContainText('管制類別');
    await expect(thead).toContainText('圖表類型');
    await expect(thead).toContainText('製程線別');
    await expect(thead).toContainText('管制圖名稱');
    await expect(thead).toContainText('管制圖種類');
    await expect(thead).toContainText('USL');
    await expect(thead).toContainText('LSL');
    await expect(thead).toContainText('UCL');
    await expect(thead).toContainText('LCL');
    await expect(thead).toContainText('管制界線計算方式');
    await expect(thead).toContainText('本期OOS');
    await expect(thead).toContainText('本期%OOS');
    await expect(thead).toContainText('本期Ppk');
    await expect(thead).toContainText('工程負責人');
    await expect(thead).toContainText('備註');
    await expect(thead).toContainText('製圖');

    await expect(summaryTable).toContainText('銅離子濃度');
    await expect(summaryTable).toContainText('12.50%');

    await page.getByTestId('draw-chart-21').click();

    await expect(summaryTable).toBeHidden();
    await expect(page.locator('canvas').first()).toBeVisible({ timeout: 10000 });
    await expect(page.getByText('管制圖監控明細')).toBeVisible();

    await page.getByTestId('return-to-summary').click();

    await expect(summaryTable).toBeVisible();
    await expect(summaryTable).toContainText('銅離子濃度');
    await expect(page.getByText('管制圖監控明細')).toBeHidden();
    await expect(page.locator('select').first()).toHaveValue('ALL');

    await page.getByRole('button', { name: '產品管制' }).click();
    await expect(page.getByText('料號 (Product)')).toHaveCount(0);
    await expect(page.getByText('線別', { exact: true })).toHaveCount(0);
    await page.getByTestId('product-query-mode').selectOption('PART');
    await expect(page.locator('input[type="date"]').first()).toBeDisabled();
    await expect(page.locator('input[type="date"]').nth(1)).toBeDisabled();
    await expect(page.getByTestId('product-part-id').locator('option')).toHaveText([
      '選擇料號...',
      '全部 (All)',
      '[P-001] 測試產品'
    ]);
    await page.getByTestId('product-part-id').selectOption('9');
    await page.getByRole('button', { name: '重新計算' }).click();

    expect(requestedDimension).toBe('PRODUCT');
    expect(requestedPartId).toBe('9');
    await expect(summaryTable).toBeVisible();
    await expect(summaryTable).toContainText('產品管制');
    await expect(summaryTable).toContainText('產品寬度');

    await page.getByTestId('draw-chart-31').click();
    expect(chartPartId).toBe('9');
    await page.getByTestId('return-to-summary').click();
    await expect(page.getByTestId('product-query-mode')).toHaveValue('PART');
    await expect(page.getByTestId('product-part-id')).toHaveValue('9');

    requestedPartId = 'not-requested';
    await page.getByTestId('product-part-id').selectOption('ALL');
    await page.getByRole('button', { name: '重新計算' }).click();
    expect(requestedPartId).toBe('');
    await expect(summaryTable).toBeVisible();

    await page.goto('/trend-chart');
    await expect(page.getByRole('button', { name: '環境管制' })).toBeVisible();
  });

  test('Pure Trend Chart (chartTypeId: null) should appear in ALL-lines summary with fallback chartType', async ({ page }) => {
    await page.route('**/api/part-process-characteristics*', async route => {
      await route.fulfill({
        json: [{
          id: 99,
          controlScope: 'PROCESS',
          partId: null,
          processId: 3,
          machineId: 8,
          characteristicId: 99,
          chartTypeId: null, // Pure Trend Chart
          isEnabled: true,
          process: { id: 3, processCode: 'ETCH', processName: '蝕刻製程', isEnabled: true },
          machine: { id: 8, machineCode: 'LINE-A', machineName: 'A線', isEnabled: true },
          characteristic: {
            id: 99,
            characteristicCode: 'TEMP_NO_SPC',
            characteristicName: '無SPC溫度',
            isEnabled: true,
            isSpcEnabled: false
          }
        }]
      });
    });

    await page.route('**/api/control-chart-categories*', async route => {
      await route.fulfill({ json: [] });
    });
    
    await page.route('**/api/v1/spc/summary*', async route => {
      await route.fulfill({
        json: [{
          partProcessCharacteristicId: 99,
          controlCategory: '製程管制',
          lineOrProcessName: '蝕刻製程 / A線',
          chartName: '無SPC溫度',
          chartType: '-', // Expected fallback for null chartType
          usl: null,
          lsl: null,
          ucl: null,
          lcl: null,
          oosCount: 0,
          oosRate: 0,
          ca: null,
          cp: null,
          cpk: null,
          pp: null,
          ppk: null,
          pic: 'User',
          remark: ''
        }]
      });
    });

    // Go to Trend Chart page
    await page.goto('/trend-chart');
    
    // Select Process (index 1) to enable Characteristic select (index 2)
    // The dimension is default 'PROC'
    await page.locator('select').nth(1).selectOption('3');
    await page.locator('select').last().selectOption('ALL');
    await page.getByRole('button', { name: '載入趨勢圖' }).click();

    // Wait for the summary table to render
    const summaryTable = page.getByTestId('spc-summary-table');
    await expect(summaryTable).toBeVisible();

    // Verify it contains the pure trend chart and the chartType column shows fallback '-'
    await expect(summaryTable).toContainText('無SPC溫度');
    
    // Specifically check the row contains the fallback chartType '-'
    const row = summaryTable.locator('tbody tr', { hasText: '無SPC溫度' });
    await expect(row.locator('td').nth(3)).toHaveText('-');
  });
});
