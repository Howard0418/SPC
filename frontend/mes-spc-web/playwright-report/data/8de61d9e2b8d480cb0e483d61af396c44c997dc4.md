# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: spc-summary.spec.ts >> SPC all-lines summary >> Pure Trend Chart (chartTypeId: null) should appear in ALL-lines summary with fallback chartType
- Location: tests\spc-summary.spec.ts:213:3

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: locator.selectOption: Test timeout of 30000ms exceeded.
Call log:
  - waiting for locator('select').nth(1)
    - locator resolved to <select disabled class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500 disabled:opacity-50">…</select>
  - attempting select option action
    2 × waiting for element to be visible and enabled
      - element is not enabled
    - retrying select option action
    - waiting 20ms
    2 × waiting for element to be visible and enabled
      - element is not enabled
    - retrying select option action
      - waiting 100ms
    57 × waiting for element to be visible and enabled
       - element is not enabled
     - retrying select option action
       - waiting 500ms

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
            - link "儀表板" [ref=e23] [cursor=pointer]:
              - /url: /
              - img [ref=e24]
              - generic [ref=e29]: 儀表板
              - img [ref=e30]
            - link "SPC 管制圖" [ref=e32] [cursor=pointer]:
              - /url: /spc
              - img [ref=e33]
              - generic [ref=e36]: SPC 管制圖
              - img [ref=e37]
            - link "量測值趨勢圖" [ref=e39] [cursor=pointer]:
              - /url: /trend-chart
              - img [ref=e40]
              - generic [ref=e43]: 量測值趨勢圖
              - img [ref=e44]
        - generic [ref=e46]:
          - heading "自動化匯入與採樣" [level=3] [ref=e47]: 自動化匯入與採樣
          - generic [ref=e49]:
            - link "現場量測數據錄入" [ref=e50] [cursor=pointer]:
              - /url: /measurements
              - img [ref=e51]
              - generic [ref=e53]: 現場量測數據錄入
              - img [ref=e54]
            - link "計量型資料匯入" [ref=e56] [cursor=pointer]:
              - /url: /uploads/variable
              - img [ref=e57]
              - generic [ref=e60]: 計量型資料匯入
              - img [ref=e61]
            - link "計數型資料匯入" [ref=e63] [cursor=pointer]:
              - /url: /uploads/attribute
              - img [ref=e64]
              - generic [ref=e67]: 計數型資料匯入
              - img [ref=e68]
        - generic [ref=e70]:
          - heading "企業品質主檔設定" [level=3] [ref=e71]: 企業品質主檔設定
          - generic [ref=e73]:
            - link "工站製程主檔" [ref=e74] [cursor=pointer]:
              - /url: /processes
              - img [ref=e75]
              - generic [ref=e79]: 工站製程主檔
              - img [ref=e80]
            - link "產品料號主檔" [ref=e82] [cursor=pointer]:
              - /url: /parts
              - img [ref=e83]
              - generic [ref=e87]: 產品料號主檔
              - img [ref=e88]
            - link "SPC 管制項目設定" [ref=e90] [cursor=pointer]:
              - /url: /part-process-characteristics
              - img [ref=e91]
              - generic [ref=e96]: SPC 管制項目設定
              - img [ref=e97]
            - link "品質特性項目" [ref=e99] [cursor=pointer]:
              - /url: /characteristics
              - img [ref=e100]
              - generic [ref=e101]: 品質特性項目
              - img [ref=e102]
            - link "線別槽體設定" [ref=e104] [cursor=pointer]:
              - /url: /traceability-master
              - img [ref=e105]
              - generic [ref=e109]: 線別槽體設定
              - img [ref=e110]
        - generic [ref=e112]:
          - heading "管制圖與西方電氣規則" [level=3] [ref=e113]: 管制圖與西方電氣規則
          - generic [ref=e115]:
            - link "管制圖配置維護" [ref=e116] [cursor=pointer]:
              - /url: /control-chart-groups
              - img [ref=e117]
              - generic [ref=e121]: 管制圖配置維護
              - img [ref=e122]
            - link "SPC 異常規則維護" [ref=e124] [cursor=pointer]:
              - /url: /spc-rule-groups
              - img [ref=e125]
              - generic [ref=e127]: SPC 異常規則維護
              - img [ref=e128]
        - generic [ref=e130]:
          - heading "異常管理與追溯" [level=3] [ref=e131]: 異常管理與追溯
          - generic [ref=e133]:
            - link "異常通報總覽" [ref=e134] [cursor=pointer]:
              - /url: /alerts
              - img [ref=e135]
              - generic [ref=e137]: 異常通報總覽
              - img [ref=e138]
            - link "異常單簽核處置" [ref=e140] [cursor=pointer]:
              - /url: /alerts-workflow
              - img [ref=e141]
              - generic [ref=e143]: 異常單簽核處置
              - img [ref=e144]
            - link "產品系譜圖 (Genealogy)" [ref=e146] [cursor=pointer]:
              - /url: /genealogy
              - img [ref=e147]
              - generic [ref=e152]: 產品系譜圖 (Genealogy)
              - img [ref=e153]
            - link "多維度品質履歷查詢" [ref=e155] [cursor=pointer]:
              - /url: /spc/query
              - img [ref=e156]
              - generic [ref=e159]: 多維度品質履歷查詢
              - img [ref=e160]
        - generic [ref=e162]:
          - heading "系統管理與通報設定" [level=3] [ref=e163]: 系統管理與通報設定
          - generic [ref=e165]:
            - link "系統操作手冊" [ref=e166] [cursor=pointer]:
              - /url: /guide
              - img [ref=e167]
              - generic [ref=e169]: 系統操作手冊
              - img [ref=e170]
            - link "SMTP 郵件與預警設定" [ref=e172] [cursor=pointer]:
              - /url: /settings/smtp
              - img [ref=e173]
              - generic [ref=e174]: SMTP 郵件與預警設定
              - img [ref=e175]
            - link "系統使用者管理" [ref=e177] [cursor=pointer]:
              - /url: /operators
              - img [ref=e178]
              - generic [ref=e183]: 系統使用者管理
              - img [ref=e184]
      - generic [ref=e186]:
        - paragraph [ref=e187]: © 2026 PMR Quality System
        - paragraph [ref=e188]: v0.1.1 Enterprise SPC Edition
    - main [ref=e189]:
      - generic [ref=e191]:
        - generic [ref=e193]:
          - img [ref=e195]
          - generic [ref=e198]:
            - heading "量測值趨勢圖" [level=1] [ref=e199]
            - paragraph [ref=e200]: 顯示各量測點原始數值時序趨勢，含工程規格界限 (USL / LSL / Target)
        - generic [ref=e202]:
          - generic [ref=e203]:
            - generic [ref=e204]: 🔍 快速搜尋
            - generic [ref=e205]:
              - textbox "輸入關鍵字快速搜尋..." [ref=e206]
              - img [ref=e207]
          - generic [ref=e210]:
            - generic [ref=e211]: 線別 (Line)
            - combobox [ref=e212]:
              - option "選擇線別..." [selected]
              - option "全部 (All)"
          - generic [ref=e213]:
            - generic [ref=e214]: 檢驗項目 (Inspection Item)
            - combobox [disabled] [ref=e215]:
              - option "選擇管制點..." [disabled] [selected]
              - option "全部 (All)"
          - button "載入趨勢圖" [disabled] [ref=e216]:
            - img [ref=e217]
            - text: 載入趨勢圖
        - generic [ref=e222]:
          - img [ref=e223]
          - text: 無法載入檢驗項目基準：Request failed with status code 400
```

# Test source

```ts
  172 |     await expect(summaryTable).toBeVisible();
  173 |     await expect(summaryTable).toContainText('銅離子濃度');
  174 |     await expect(page.getByText('管制圖監控明細')).toBeHidden();
  175 |     await expect(page.locator('select').first()).toHaveValue('ALL');
  176 | 
  177 |     await page.getByRole('button', { name: '產品管制' }).click();
  178 |     await expect(page.getByText('料號 (Product)')).toHaveCount(0);
  179 |     await expect(page.getByText('線別', { exact: true })).toHaveCount(0);
  180 |     await page.getByTestId('product-query-mode').selectOption('PART');
  181 |     await expect(page.locator('input[type="date"]').first()).toBeDisabled();
  182 |     await expect(page.locator('input[type="date"]').nth(1)).toBeDisabled();
  183 |     await expect(page.getByTestId('product-part-id').locator('option')).toHaveText([
  184 |       '選擇料號...',
  185 |       '全部 (All)',
  186 |       '[P-001] 測試產品'
  187 |     ]);
  188 |     await page.getByTestId('product-part-id').selectOption('9');
  189 |     await page.getByRole('button', { name: '重新計算' }).click();
  190 | 
  191 |     expect(requestedDimension).toBe('PRODUCT');
  192 |     expect(requestedPartId).toBe('9');
  193 |     await expect(summaryTable).toBeVisible();
  194 |     await expect(summaryTable).toContainText('產品管制');
  195 |     await expect(summaryTable).toContainText('產品寬度');
  196 | 
  197 |     await page.getByTestId('draw-chart-31').click();
  198 |     expect(chartPartId).toBe('9');
  199 |     await page.getByTestId('return-to-summary').click();
  200 |     await expect(page.getByTestId('product-query-mode')).toHaveValue('PART');
  201 |     await expect(page.getByTestId('product-part-id')).toHaveValue('9');
  202 | 
  203 |     requestedPartId = 'not-requested';
  204 |     await page.getByTestId('product-part-id').selectOption('ALL');
  205 |     await page.getByRole('button', { name: '重新計算' }).click();
  206 |     expect(requestedPartId).toBe('');
  207 |     await expect(summaryTable).toBeVisible();
  208 | 
  209 |     await page.goto('/trend-chart');
  210 |     await expect(page.getByRole('button', { name: '環境管制' })).toBeVisible();
  211 |   });
  212 | 
  213 |   test('Pure Trend Chart (chartTypeId: null) should appear in ALL-lines summary with fallback chartType', async ({ page }) => {
  214 |     await page.route('**/api/part-process-characteristics*', async route => {
  215 |       await route.fulfill({
  216 |         json: [{
  217 |           id: 99,
  218 |           controlScope: 'PROCESS',
  219 |           partId: null,
  220 |           processId: 3,
  221 |           machineId: 8,
  222 |           characteristicId: 99,
  223 |           chartTypeId: null, // Pure Trend Chart
  224 |           isEnabled: true,
  225 |           process: { id: 3, processCode: 'ETCH', processName: '蝕刻製程', isEnabled: true },
  226 |           machine: { id: 8, machineCode: 'LINE-A', machineName: 'A線', isEnabled: true },
  227 |           characteristic: {
  228 |             id: 99,
  229 |             characteristicCode: 'TEMP_NO_SPC',
  230 |             characteristicName: '無SPC溫度',
  231 |             isEnabled: true,
  232 |             isSpcEnabled: false
  233 |           }
  234 |         }]
  235 |       });
  236 |     });
  237 | 
  238 |     await page.route('**/api/control-chart-categories*', async route => {
  239 |       await route.fulfill({ json: [] });
  240 |     });
  241 |     
  242 |     await page.route('**/api/v1/spc/summary*', async route => {
  243 |       await route.fulfill({
  244 |         json: [{
  245 |           partProcessCharacteristicId: 99,
  246 |           controlCategory: '製程管制',
  247 |           lineOrProcessName: '蝕刻製程 / A線',
  248 |           chartName: '無SPC溫度',
  249 |           chartType: '-', // Expected fallback for null chartType
  250 |           usl: null,
  251 |           lsl: null,
  252 |           ucl: null,
  253 |           lcl: null,
  254 |           oosCount: 0,
  255 |           oosRate: 0,
  256 |           ca: null,
  257 |           cp: null,
  258 |           cpk: null,
  259 |           pp: null,
  260 |           ppk: null,
  261 |           pic: 'User',
  262 |           remark: ''
  263 |         }]
  264 |       });
  265 |     });
  266 | 
  267 |     // Go to Trend Chart page
  268 |     await page.goto('/trend-chart');
  269 |     
  270 |     // Select Process (index 1) to enable Characteristic select (index 2)
  271 |     // The dimension is default 'PROC'
> 272 |     await page.locator('select').nth(1).selectOption('3');
      |                                         ^ Error: locator.selectOption: Test timeout of 30000ms exceeded.
  273 |     await page.locator('select').last().selectOption('ALL');
  274 |     await page.getByRole('button', { name: '載入趨勢圖' }).click();
  275 | 
  276 |     // Wait for the summary table to render
  277 |     const summaryTable = page.getByTestId('spc-summary-table');
  278 |     await expect(summaryTable).toBeVisible();
  279 | 
  280 |     // Verify it contains the pure trend chart and the chartType column shows fallback '-'
  281 |     await expect(summaryTable).toContainText('無SPC溫度');
  282 |     
  283 |     // Specifically check the row contains the fallback chartType '-'
  284 |     const row = summaryTable.locator('tbody tr', { hasText: '無SPC溫度' });
  285 |     await expect(row.locator('td').nth(3)).toHaveText('-');
  286 |   });
  287 | });
  288 | 
```