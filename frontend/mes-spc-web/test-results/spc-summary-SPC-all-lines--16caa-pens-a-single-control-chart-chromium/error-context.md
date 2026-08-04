# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: spc-summary.spec.ts >> SPC all-lines summary >> shows the requested columns and opens a single control chart
- Location: tests\spc-summary.spec.ts:4:3

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: locator.selectOption: Test timeout of 30000ms exceeded.
Call log:
  - waiting for getByTestId('product-query-mode')

```

# Page snapshot

```yaml
- generic [ref=e3]:
  - banner [ref=e4]:
    - generic [ref=e5]:
      - img [ref=e7]
      - generic [ref=e9]:
        - heading "PMR SPC Enterprise" [level=1] [ref=e10]
        - paragraph [ref=e11]: 智能統計製程與製造品質分析系統
    - button "切換深色模式" [ref=e13] [cursor=pointer]:
      - img [ref=e14]
  - generic [ref=e16]:
    - complementary [ref=e17]:
      - generic [ref=e18]:
        - generic [ref=e19]:
          - heading "高階戰情與分析" [level=3] [ref=e20]: 高階戰情與分析
          - generic [ref=e22]:
            - link "SPC 管制圖" [ref=e23] [cursor=pointer]:
              - /url: /spc
              - img [ref=e24]
              - generic [ref=e27]: SPC 管制圖
              - img [ref=e28]
            - link "量測值趨勢圖" [ref=e30] [cursor=pointer]:
              - /url: /trend-chart
              - img [ref=e31]
              - generic [ref=e34]: 量測值趨勢圖
              - img [ref=e35]
            - link "SPC 週月報表" [ref=e37] [cursor=pointer]:
              - /url: /monthly-control-chart
              - img [ref=e38]
              - generic [ref=e42]: SPC 週月報表
              - img [ref=e43]
        - generic [ref=e45]:
          - heading "自動化匯入與採樣" [level=3] [ref=e46]: 自動化匯入與採樣
          - link "SPC 資料匯入" [ref=e49] [cursor=pointer]:
            - /url: /uploads
            - img [ref=e50]
            - generic [ref=e53]: SPC 資料匯入
            - img [ref=e54]
        - generic [ref=e56]:
          - heading "企業品質主檔設定" [level=3] [ref=e57]: 企業品質主檔設定
          - generic [ref=e59]:
            - link "SPC 管制項目設定" [ref=e60] [cursor=pointer]:
              - /url: /part-process-characteristics
              - img [ref=e61]
              - generic [ref=e66]: SPC 管制項目設定
              - img [ref=e67]
            - link "工站製程主檔" [ref=e69] [cursor=pointer]:
              - /url: /processes
              - img [ref=e70]
              - generic [ref=e74]: 工站製程主檔
              - img [ref=e75]
            - link "產品料號主檔" [ref=e77] [cursor=pointer]:
              - /url: /parts
              - img [ref=e78]
              - generic [ref=e82]: 產品料號主檔
              - img [ref=e83]
            - link "品質特性項目" [ref=e85] [cursor=pointer]:
              - /url: /characteristics
              - img [ref=e86]
              - generic [ref=e87]: 品質特性項目
              - img [ref=e88]
            - link "線別槽體設定" [ref=e90] [cursor=pointer]:
              - /url: /traceability-master
              - img [ref=e91]
              - generic [ref=e95]: 線別槽體設定
              - img [ref=e96]
            - link "管制圖大類別維護" [ref=e98] [cursor=pointer]:
              - /url: /control-chart-groups
              - img [ref=e99]
              - generic [ref=e103]: 管制圖大類別維護
              - img [ref=e104]
            - link "SPC 異常規則維護" [ref=e106] [cursor=pointer]:
              - /url: /spc-rule-groups
              - img [ref=e107]
              - generic [ref=e109]: SPC 異常規則維護
              - img [ref=e110]
        - generic [ref=e112]:
          - heading "異常管理與追溯" [level=3] [ref=e113]: 異常管理與追溯
          - generic [ref=e115]:
            - link "異常通報總覽" [ref=e116] [cursor=pointer]:
              - /url: /alerts
              - img [ref=e117]
              - generic [ref=e119]: 異常通報總覽
              - img [ref=e120]
            - link "異常單簽核處置" [ref=e122] [cursor=pointer]:
              - /url: /alerts-workflow
              - img [ref=e123]
              - generic [ref=e125]: 異常單簽核處置
              - img [ref=e126]
            - link "產品系譜圖 (Genealogy)" [ref=e128] [cursor=pointer]:
              - /url: /genealogy
              - img [ref=e129]
              - generic [ref=e134]: 產品系譜圖 (Genealogy)
              - img [ref=e135]
            - link "多維度品質履歷查詢" [ref=e137] [cursor=pointer]:
              - /url: /spc/query
              - img [ref=e138]
              - generic [ref=e141]: 多維度品質履歷查詢
              - img [ref=e142]
        - generic [ref=e144]:
          - heading "系統管理與通報設定" [level=3] [ref=e145]: 系統管理與通報設定
          - generic [ref=e147]:
            - link "系統操作手冊" [ref=e148] [cursor=pointer]:
              - /url: /guide
              - img [ref=e149]
              - generic [ref=e151]: 系統操作手冊
              - img [ref=e152]
            - link "SMTP 郵件與預警設定" [ref=e154] [cursor=pointer]:
              - /url: /settings/smtp
              - img [ref=e155]
              - generic [ref=e156]: SMTP 郵件與預警設定
              - img [ref=e157]
            - link "SPC 週報月報設定" [ref=e159] [cursor=pointer]:
              - /url: /settings/spc-reports
              - img [ref=e160]
              - generic [ref=e164]: SPC 週報月報設定
              - img [ref=e165]
            - link "系統使用者管理" [ref=e167] [cursor=pointer]:
              - /url: /operators
              - img [ref=e168]
              - generic [ref=e173]: 系統使用者管理
              - img [ref=e174]
      - generic [ref=e176]:
        - paragraph [ref=e177]: © 2026 PMR Quality System
        - paragraph [ref=e178]: v0.1.38 Enterprise SPC Edition
    - main [ref=e179]:
      - generic [ref=e181]:
        - generic [ref=e182]:
          - button "製程管制" [ref=e183] [cursor=pointer]
          - button "藥液管制" [ref=e184] [cursor=pointer]
          - button "環境管制" [ref=e185] [cursor=pointer]
          - button "產品管制" [active] [ref=e186] [cursor=pointer]
        - generic [ref=e187]:
          - generic [ref=e188]:
            - img [ref=e190]
            - heading "SPC 即時互動管制圖" [level=1] [ref=e193]
          - generic [ref=e194]:
            - generic [ref=e195]:
              - generic [ref=e196]:
                - generic [ref=e197]: 量測起日
                - textbox [ref=e198]: 2026-04-29
              - generic [ref=e199]:
                - generic [ref=e200]: 量測迄日
                - textbox [ref=e201]: 2026-07-29
            - button "重新計算" [ref=e202] [cursor=pointer]:
              - img [ref=e203]
              - text: 重新計算
```

# Test source

```ts
  90  |       requestedPartId = requestUrl.searchParams.get('partId') || '';
  91  |       const isProduct = requestedDimension === 'PRODUCT';
  92  |       await route.fulfill({
  93  |         json: [{
  94  |           partProcessCharacteristicId: isProduct ? 31 : 21,
  95  |           controlCategory: isProduct ? '產品管制' : '藥液管制',
  96  |           lineOrProcessName: isProduct ? '組裝製程' : '蝕刻製程 / A線',
  97  |           chartName: isProduct ? '產品寬度' : '銅離子濃度',
  98  |           chartType: '單值-移動全距圖',
  99  |           usl: 10,
  100 |           lsl: 0,
  101 |           ucl: 8.2,
  102 |           lcl: 2.1,
  103 |           limitCalculationMethod: '移動全距法',
  104 |           totalCount: 8,
  105 |           oosCount: 1,
  106 |           oosPercentage: 12.5,
  107 |           ca: 0.1,
  108 |           pp: 1.4,
  109 |           ppk: 1.2,
  110 |           responsibleUser: '王工程師',
  111 |           remarks: '已完成補藥確認'
  112 |         }]
  113 |       });
  114 |     });
  115 |     await page.route('**/api/v1/spc/chart*', async route => {
  116 |       chartPartId = new URL(route.request().url()).searchParams.get('partId') || '';
  117 |       await route.fulfill({
  118 |         json: {
  119 |           chartType: 'I-MR',
  120 |           limits: { usl: 10, lsl: 0, target: 5 },
  121 |           statControlLimits: {
  122 |             iControlLimitsStat: { cl: 5, ucl: 8.2, lcl: 2.1 },
  123 |             mrControlLimitsStat: { cl: 0.5, ucl: 1.6335, lcl: 0 }
  124 |           },
  125 |           chartData: {
  126 |             points: [
  127 |               { measuredAt: '2026-06-20T08:00:00', value: 5, outOfSpec: false, outOfControl: false, violatedRules: [] },
  128 |               { measuredAt: '2026-06-20T09:00:00', value: 6, outOfSpec: false, outOfControl: false, violatedRules: [] }
  129 |             ]
  130 |           },
  131 |           secondaryChartData: { points: [{ value: 1, outOfControl: false }] },
  132 |           rawDataPoints: [
  133 |             { measuredAt: '2026-06-20T08:00:00', value: 5, isExcluded: false },
  134 |             { measuredAt: '2026-06-20T09:00:00', value: 6, isExcluded: false }
  135 |           ],
  136 |           subgroupSize: 1,
  137 |           capability: { ca: 0.1, cp: 1.5, cpk: 1.4, pp: 1.4, ppk: 1.2 }
  138 |         }
  139 |       });
  140 |     });
  141 | 
  142 |     await page.goto('/spc');
  143 |     await expect(page.getByPlaceholder('輸入關鍵字快速搜尋...')).toHaveCount(0);
  144 |     await expect(page.getByText('檢驗項目 (Inspection Item)')).toHaveCount(0);
  145 |     await expect(page.getByText('工單 / 批號過濾')).toHaveCount(0);
  146 |     await expect(page.getByRole('button', { name: '環境管制' })).toBeVisible();
  147 |     await page.getByRole('button', { name: '藥液管制' }).click();
  148 |     await page.locator('select').first().selectOption('ALL');
  149 |     await page.getByRole('button', { name: '重新計算' }).click();
  150 | 
  151 |     const summaryTable = page.getByTestId('spc-summary-table');
  152 |     await expect(summaryTable).toBeVisible();
  153 |     expect(requestedDimension).toBe('CHEM');
  154 | 
  155 |     // 確認各欄位標題文字存在（表頭現已支援排序，含有圖示元素）
  156 |     const thead = summaryTable.locator('thead');
  157 |     await expect(thead).toContainText('管制類別');
  158 |     await expect(thead).toContainText('製程線別');
  159 |     await expect(thead).toContainText('管制圖名稱');
  160 |     await expect(thead).toContainText('匯入資料數');
  161 |     await expect(thead).toContainText('USL');
  162 |     await expect(thead).toContainText('LSL');
  163 |     await expect(thead).toContainText('UCL');
  164 |     await expect(thead).toContainText('LCL');
  165 |     await expect(thead).toContainText('本期OOS');
  166 |     await expect(thead).toContainText('本期%OOS');
  167 |     await expect(thead).toContainText('本期Cpk');
  168 |     await expect(thead).toContainText('製圖');
  169 | 
  170 |     await expect(summaryTable).toContainText('銅離子濃度');
  171 |     await expect(summaryTable).toContainText('8');
  172 |     await expect(summaryTable).toContainText('12.50%');
  173 | 
  174 |     await page.getByTestId('draw-chart-21').click();
  175 | 
  176 |     await expect(summaryTable).toBeHidden();
  177 |     await expect(page.locator('canvas').first()).toBeVisible({ timeout: 10000 });
  178 |     await expect(page.getByText('管制圖監控明細')).toBeVisible();
  179 | 
  180 |     await page.getByTestId('return-to-summary').click();
  181 | 
  182 |     await expect(summaryTable).toBeVisible();
  183 |     await expect(summaryTable).toContainText('銅離子濃度');
  184 |     await expect(page.getByText('管制圖監控明細')).toBeHidden();
  185 |     await expect(page.locator('select').first()).toHaveValue('ALL');
  186 | 
  187 |     await page.getByRole('button', { name: '產品管制' }).click();
  188 |     await expect(page.getByText('料號 (Product)')).toHaveCount(0);
  189 |     await expect(page.getByText('線別', { exact: true })).toHaveCount(0);
> 190 |     await page.getByTestId('product-query-mode').selectOption('PART');
      |                                                  ^ Error: locator.selectOption: Test timeout of 30000ms exceeded.
  191 |     await expect(page.locator('input[type="date"]').first()).toBeDisabled();
  192 |     await expect(page.locator('input[type="date"]').nth(1)).toBeDisabled();
  193 |     await expect(page.getByTestId('product-part-id').locator('option')).toHaveText([
  194 |       '選擇料號...',
  195 |       '全部 (All)',
  196 |       '[P-001] 測試產品'
  197 |     ]);
  198 |     await page.getByTestId('product-part-id').selectOption('9');
  199 |     await page.getByRole('button', { name: '重新計算' }).click();
  200 | 
  201 |     expect(requestedDimension).toBe('PRODUCT');
  202 |     expect(requestedPartId).toBe('9');
  203 |     await expect(summaryTable).toBeVisible();
  204 |     await expect(summaryTable).toContainText('產品管制');
  205 |     await expect(summaryTable).toContainText('產品寬度');
  206 | 
  207 |     await page.getByTestId('draw-chart-31').click();
  208 |     expect(chartPartId).toBe('9');
  209 |     await page.getByTestId('return-to-summary').click();
  210 |     await expect(page.getByTestId('product-query-mode')).toHaveValue('PART');
  211 |     await expect(page.getByTestId('product-part-id')).toHaveValue('9');
  212 | 
  213 |     requestedPartId = 'not-requested';
  214 |     await page.getByTestId('product-part-id').selectOption('ALL');
  215 |     await page.getByRole('button', { name: '重新計算' }).click();
  216 |     expect(requestedPartId).toBe('');
  217 |     await expect(summaryTable).toBeVisible();
  218 | 
  219 |     await page.goto('/trend-chart');
  220 |     await expect(page.getByRole('button', { name: '環境管制' })).toBeVisible();
  221 |   });
  222 | 
  223 |   test('Pure Trend Chart (chartTypeId: null) should appear in ALL-lines summary with fallback chartType', async ({ page }) => {
  224 |     await page.route('**/api/part-process-characteristics*', async route => {
  225 |       await route.fulfill({
  226 |         json: [{
  227 |           id: 99,
  228 |           controlScope: 'PROCESS',
  229 |           partId: null,
  230 |           processId: 3,
  231 |           machineId: 8,
  232 |           characteristicId: 99,
  233 |           chartTypeId: null, // Pure Trend Chart
  234 |           isEnabled: true,
  235 |           process: { id: 3, processCode: 'ETCH', processName: '蝕刻製程', isEnabled: true },
  236 |           machine: { id: 8, machineCode: 'LINE-A', machineName: 'A線', isEnabled: true },
  237 |           characteristic: {
  238 |             id: 99,
  239 |             characteristicCode: 'TEMP_NO_SPC',
  240 |             characteristicName: '無SPC溫度',
  241 |             isEnabled: true,
  242 |             isSpcEnabled: false
  243 |           }
  244 |         }]
  245 |       });
  246 |     });
  247 | 
  248 |     await page.route('**/api/control-chart-categories*', async route => {
  249 |       await route.fulfill({ json: [] });
  250 |     });
  251 |     
  252 |     await page.route('**/api/v1/spc/summary*', async route => {
  253 |       await route.fulfill({
  254 |         json: [{
  255 |           partProcessCharacteristicId: 99,
  256 |           controlCategory: '製程管制',
  257 |           lineOrProcessName: '蝕刻製程 / A線',
  258 |           chartName: '無SPC溫度',
  259 |           chartType: '-', // Expected fallback for null chartType
  260 |           usl: null,
  261 |           lsl: null,
  262 |           ucl: null,
  263 |           lcl: null,
  264 |           oosCount: 0,
  265 |           oosRate: 0,
  266 |           ca: null,
  267 |           cp: null,
  268 |           cpk: null,
  269 |           pp: null,
  270 |           ppk: null,
  271 |           pic: 'User',
  272 |           remark: ''
  273 |         }]
  274 |       });
  275 |     });
  276 | 
  277 |     // Go to Trend Chart page
  278 |     await page.goto('/trend-chart');
  279 |     
  280 |     // Select Process (index 1) to enable Characteristic select (index 2)
  281 |     // The dimension is default 'PROC'
  282 |     await page.locator('select').nth(1).selectOption('3');
  283 |     await page.locator('select').last().selectOption('ALL');
  284 |     await page.getByRole('button', { name: '載入趨勢圖' }).click();
  285 | 
  286 |     // Wait for the summary table to render
  287 |     const summaryTable = page.getByTestId('spc-summary-table');
  288 |     await expect(summaryTable).toBeVisible();
  289 | 
  290 |     // Verify it contains the pure trend chart and the chartType column shows fallback '-'
```