# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: spc-summary.spec.ts >> SPC all-lines summary >> shows the requested columns and opens a single control chart
- Location: tests\spc-summary.spec.ts:4:3

# Error details

```
Error: expect(received).toBe(expected) // Object.is equality

Expected: "CHEMICAL"
Received: "CHEM"
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
        - generic [ref=e192]:
          - button "製程管制" [ref=e193] [cursor=pointer]
          - button "藥液管制" [ref=e194] [cursor=pointer]
          - button "環境管制" [ref=e195] [cursor=pointer]
          - button "產品管制" [ref=e196] [cursor=pointer]
        - generic [ref=e197]:
          - generic [ref=e198]:
            - img [ref=e200]
            - heading "SPC 即時互動管制圖" [level=1] [ref=e203]
          - generic [ref=e204]:
            - generic [ref=e205]:
              - generic [ref=e206]:
                - generic [ref=e207]: 量測起日
                - textbox [ref=e208]: 2026-04-07
              - generic [ref=e209]:
                - generic [ref=e210]: 量測迄日
                - textbox [ref=e211]: 2026-07-07
              - generic [ref=e212]:
                - generic [ref=e213]: 線別
                - combobox [ref=e214]:
                  - option "選擇線別..."
                  - option "全部 (All)" [selected]
                  - option "[ETCH] 蝕刻製程"
            - button "重新計算" [ref=e215] [cursor=pointer]:
              - img [ref=e216]
              - text: 重新計算
        - generic [ref=e221]:
          - generic [ref=e222]:
            - generic [ref=e223]:
              - heading "SPC 管制項目總覽" [level=3] [ref=e224]:
                - img [ref=e225]
                - text: SPC 管制項目總覽
              - paragraph [ref=e226]: 共 1 筆量測資料受管制
            - generic [ref=e227]: 線別：全部 (All)
          - table [ref=e229]:
            - rowgroup [ref=e230]:
              - row "管制類別 製程線別 管制圖名稱 管制圖種類 USL LSL UCL LCL 管制界線計算方式 OOS件數 % OOS Ca Pp Ppk 工程負責人 備註 製圖" [ref=e231]:
                - columnheader "管制類別" [ref=e232]
                - columnheader "製程線別" [ref=e233]
                - columnheader "管制圖名稱" [ref=e234]
                - columnheader "管制圖種類" [ref=e235]
                - columnheader "USL" [ref=e236]
                - columnheader "LSL" [ref=e237]
                - columnheader "UCL" [ref=e238]
                - columnheader "LCL" [ref=e239]
                - columnheader "管制界線計算方式" [ref=e240]
                - columnheader "OOS件數" [ref=e241]
                - columnheader "% OOS" [ref=e242]
                - columnheader "Ca" [ref=e243]
                - columnheader "Pp" [ref=e244]
                - columnheader "Ppk" [ref=e245]
                - columnheader "工程負責人" [ref=e246]
                - columnheader "備註" [ref=e247]
                - columnheader "製圖" [ref=e248]
            - rowgroup [ref=e249]:
              - row "藥液管制 蝕刻製程 / A線 銅離子濃度 單值-移動全距圖 10.000 0.000 8.200 2.100 移動全距法 1 12.50% 0.10 1.40 1.20 王工程師 已完成補藥確認 製圖" [ref=e250]:
                - cell "藥液管制" [ref=e251]
                - cell "蝕刻製程 / A線" [ref=e252]
                - cell "銅離子濃度" [ref=e253]
                - cell "單值-移動全距圖" [ref=e254]
                - cell "10.000" [ref=e255]
                - cell "0.000" [ref=e256]
                - cell "8.200" [ref=e257]
                - cell "2.100" [ref=e258]
                - cell "移動全距法" [ref=e259]
                - cell "1" [ref=e260]
                - cell "12.50%" [ref=e261]
                - cell "0.10" [ref=e262]
                - cell "1.40" [ref=e263]
                - cell "1.20" [ref=e264]
                - cell "王工程師" [ref=e265]
                - cell "已完成補藥確認" [ref=e266]
                - cell "製圖" [ref=e267]:
                  - button "製圖" [ref=e268] [cursor=pointer]:
                    - img [ref=e269]
                    - text: 製圖
```

# Test source

```ts
  52  |           chartTypeId: 3,
  53  |           isEnabled: true,
  54  |           process: { id: 3, processCode: 'ETCH', processName: '蝕刻製程', isEnabled: true },
  55  |           characteristic: {
  56  |             id: 7,
  57  |             characteristicCode: 'TEMP',
  58  |             characteristicName: '環境溫度',
  59  |             isEnabled: true,
  60  |             isSpcEnabled: true
  61  |           }
  62  |         }]
  63  |       });
  64  |     });
  65  |     await page.route('**/api/control-chart-types*', async route => {
  66  |       await route.fulfill({ json: [
  67  |         { id: 2, chartCategoryId: 2, chartTypeCode: 'I_MR', chartTypeName: '單值-移動全距圖' },
  68  |         { id: 3, chartCategoryId: 3, chartTypeCode: 'ENV_I_MR', chartTypeName: '環境單值圖' },
  69  |         { id: 4, chartCategoryId: 4, chartTypeCode: 'PROD_I_MR', chartTypeName: '產品單值圖' }
  70  |       ] });
  71  |     });
  72  |     await page.route('**/api/control-chart-categories*', async route => {
  73  |       await route.fulfill({ json: [
  74  |         { id: 2, chartGroupId: 2, categoryCode: 'CHEM_VAR', categoryName: '藥液計量管制' },
  75  |         { id: 3, chartGroupId: 3, categoryCode: 'ENV_VAR', categoryName: '環境計量管制' },
  76  |         { id: 4, chartGroupId: 4, categoryCode: 'PROD_VAR', categoryName: '產品計量管制' }
  77  |       ] });
  78  |     });
  79  |     await page.route('**/api/control-chart-groups*', async route => {
  80  |       await route.fulfill({ json: [
  81  |         { id: 1, groupCode: 'PROC', groupName: '製程管制', isEnabled: true },
  82  |         { id: 2, groupCode: 'CHEM', groupName: '藥液管制', isEnabled: true },
  83  |         { id: 3, groupCode: 'ENV', groupName: '環境管制', isEnabled: true },
  84  |         { id: 4, groupCode: 'PROD', groupName: '產品管制', isEnabled: true }
  85  |       ] });
  86  |     });
  87  |     await page.route('**/api/v1/spc/summary*', async route => {
  88  |       const requestUrl = new URL(route.request().url());
  89  |       requestedDimension = requestUrl.searchParams.get('dimension') || '';
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
  104 |           oosCount: 1,
  105 |           oosPercentage: 12.5,
  106 |           ca: 0.1,
  107 |           pp: 1.4,
  108 |           ppk: 1.2,
  109 |           responsibleUser: '王工程師',
  110 |           remarks: '已完成補藥確認'
  111 |         }]
  112 |       });
  113 |     });
  114 |     await page.route('**/api/v1/spc/chart*', async route => {
  115 |       chartPartId = new URL(route.request().url()).searchParams.get('partId') || '';
  116 |       await route.fulfill({
  117 |         json: {
  118 |           chartType: 'I-MR',
  119 |           limits: { usl: 10, lsl: 0, target: 5 },
  120 |           statControlLimits: {
  121 |             iControlLimitsStat: { cl: 5, ucl: 8.2, lcl: 2.1 },
  122 |             mrControlLimitsStat: { cl: 0.5, ucl: 1.6335, lcl: 0 }
  123 |           },
  124 |           chartData: {
  125 |             points: [
  126 |               { measuredAt: '2026-06-20T08:00:00', value: 5, outOfSpec: false, outOfControl: false, violatedRules: [] },
  127 |               { measuredAt: '2026-06-20T09:00:00', value: 6, outOfSpec: false, outOfControl: false, violatedRules: [] }
  128 |             ]
  129 |           },
  130 |           secondaryChartData: { points: [{ value: 1, outOfControl: false }] },
  131 |           rawDataPoints: [
  132 |             { measuredAt: '2026-06-20T08:00:00', value: 5, isExcluded: false },
  133 |             { measuredAt: '2026-06-20T09:00:00', value: 6, isExcluded: false }
  134 |           ],
  135 |           subgroupSize: 1,
  136 |           capability: { ca: 0.1, cp: 1.5, cpk: 1.4, pp: 1.4, ppk: 1.2 }
  137 |         }
  138 |       });
  139 |     });
  140 | 
  141 |     await page.goto('/spc');
  142 |     await expect(page.getByPlaceholder('輸入關鍵字快速搜尋...')).toHaveCount(0);
  143 |     await expect(page.getByText('檢驗項目 (Inspection Item)')).toHaveCount(0);
  144 |     await expect(page.getByText('工單 / 批號過濾')).toHaveCount(0);
  145 |     await expect(page.getByRole('button', { name: '環境管制' })).toBeVisible();
  146 |     await page.getByRole('button', { name: '藥液管制' }).click();
  147 |     await page.locator('select').first().selectOption('ALL');
  148 |     await page.getByRole('button', { name: '重新計算' }).click();
  149 | 
  150 |     const summaryTable = page.getByTestId('spc-summary-table');
  151 |     await expect(summaryTable).toBeVisible();
> 152 |     expect(requestedDimension).toBe('CHEMICAL');
      |                                ^ Error: expect(received).toBe(expected) // Object.is equality
  153 | 
  154 |     const expectedHeaders = [
  155 |       '管制類別', '製程線別', '管制圖名稱', '管制圖種類',
  156 |       'USL', 'LSL', 'UCL', 'LCL', '管制界線計算方式',
  157 |       'OOS件數', '% OOS', 'Ca', 'Pp', 'Ppk',
  158 |       '工程負責人', '備註', '製圖'
  159 |     ];
  160 |     await expect(summaryTable.locator('thead th')).toHaveText(expectedHeaders);
  161 |     await expect(summaryTable).toContainText('銅離子濃度');
  162 |     await expect(summaryTable).toContainText('12.50%');
  163 | 
  164 |     await page.getByTestId('draw-chart-21').click();
  165 | 
  166 |     await expect(summaryTable).toBeHidden();
  167 |     await expect(page.locator('canvas').first()).toBeVisible({ timeout: 10000 });
  168 |     await expect(page.getByText('管制圖監控明細')).toBeVisible();
  169 | 
  170 |     await page.getByTestId('return-to-summary').click();
  171 | 
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
```