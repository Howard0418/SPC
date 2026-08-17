# 外部系統藥液量測資料錄入頁面－開發提示詞

> **歷史文件警示（2026-08-07）**：本文仍包含 `CHEMICAL`、顯示系統代號及舊主檔責任等過時規則，不可直接作為現行需求。請先閱讀 [SPC 現行需求基準](SPC_REQUIREMENTS_BASELINE_2026-08-07.md)，衝突時以需求基準為準。

最後更新：2026-07-13

## 文件目的

本文件可直接提供給另一個系統的開發 AI 或開發團隊，用於建立「藥液量測資料錄入」頁面，並透過 MES SPC API 將資料正式寫入 SPC 系統。

## 重要架構原則

外部系統不得直接對 `VariableMeasurements` 執行 `INSERT` 或 `UPDATE`。

正確流程：

```text
外部系統藥液錄入頁
        ↓ HTTPS + JWT
POST /api/uploads/variable
        ↓ 資料與主檔檢核
UploadBatches / UploadDetails / UploadErrors
        ↓ 確認匯入
POST /api/uploads/{uploadBatchId}/confirm
        ↓
VariableMeasurements
        ↓
SPC 計算、管制圖、趨勢圖、AlertEvents
```

使用 API 的原因：

- 解析產品、製程、機台、槽體及品質特性。
- 取得正確的 `PartProcessCharacteristicId`。
- 保留匯入批次與稽核紀錄。
- 執行資料格式與主檔檢核。
- 觸發 SPC 計算。
- 執行超規及失控判定。
- 建立異常事件。

## 可直接使用的開發提示詞

```text
請在現有系統新增一個「藥液量測資料錄入」頁面，使用目前專案既有的前端框架、元件庫、API 封裝、登入驗證、錯誤處理與版面風格，不要建立獨立技術架構。

目標：
讓現場使用者輸入藥液量測資料，透過 MES SPC API 建立匯入批次，確認後正式寫入 SPC 系統的 VariableMeasurements，並觸發 SPC 計算與異常判定。

重要限制：
1. 不得直接 INSERT 或 UPDATE VariableMeasurements。
2. 必須使用 SPC API：
   POST {SPC_API_BASE}/api/uploads/variable
   POST {SPC_API_BASE}/api/uploads/{uploadBatchId}/confirm
3. 若 API 啟用 JWT，請在 Authorization Header 傳送：
   Authorization: Bearer {token}
4. 不要在前端保存資料庫帳號、密碼或 SQL Server Connection String。
5. 所有主檔代碼必須使用 SPC 系統已存在且已啟用的資料。
6. API 失敗時不得顯示或記錄為儲存成功。

頁面名稱：
藥液量測資料錄入

必填欄位：
- 製程代碼 ProcessCode
- 線別／機台代碼 MachineCode
- 槽體代碼 TankCode
- 品質特性代碼 CharacteristicCode
- 量測值 MeasuredValue
- 量測時間 MeasuredAt
- 作業人員 Operator

選填欄位：
- 管制範圍 ControlScope，藥液固定傳 CHEMICAL
- 批號 LotNo
- 樣本編號 SampleNo，預設 1
- 複驗值 RecheckValue
- 調整方式 AdjustAction
- 調整量 AdjustAmount
- 備註 Note

畫面需求：
1. 製程、機台、槽體及品質特性優先使用下拉選單，不要讓使用者自由輸入不存在的代碼。
2. 選擇製程後，只顯示該製程的機台。
3. 選擇機台後，只顯示該機台的槽體。
4. 選擇品質特性後顯示單位、LSL、Target、USL，以及最近一次量測值與時間（若 API 可取得）。
5. 使用者輸入量測值時必須驗證為有效數字。
6. 若超過 USL 或低於 LSL，顯示紅色提示，但仍允許使用者確認後送出。
7. 複驗值與調整量若有輸入，也必須是有效數字。
8. 儲存前顯示確認摘要。
9. 防止使用者連續點擊造成重複送出。
10. API 處理中顯示 loading。
11. 成功後顯示批次編號、匯入筆數及異常結果。
12. 失敗時保留使用者輸入的資料，並顯示後端回傳的錯誤訊息。
13. 提供「再輸入一筆」及「查看 SPC 管制圖」按鈕。
14. 支援桌面與平板尺寸。
15. 所有日期送出前轉成 ISO 8601 格式。
16. 使用繁體中文介面。

POST /api/uploads/variable 的 JSON Body 必須是陣列，即使只有一筆也必須使用陣列：

[
  {
    "ControlScope": "CHEMICAL",
    "ProcessCode": "CHEM_PROC",
    "MachineCode": "LINE-01",
    "TankCode": "TANK-A",
    "CharacteristicCode": "PH",
    "MeasuredValue": "7.12",
    "RecheckValue": "",
    "AdjustAction": "",
    "AdjustAmount": "",
    "LotNo": "CHEM-20260713",
    "SampleNo": "1",
    "MeasuredAt": "2026-07-13T08:30:00+08:00",
    "Operator": "OP-001"
  }
]

第一階段 API 回應預期包含：
- uploadBatchId
- importStatus
- totalRows
- validRows
- errorRows

取得 uploadBatchId 後：

如果 errorRows > 0：
- 不得呼叫 confirm。
- 顯示「資料未正式匯入」。
- 呼叫 GET /api/uploads/{uploadBatchId}/preview。
- 將錯誤欄位與原因顯示給使用者。

如果 errorRows = 0 且 validRows = totalRows：
- 呼叫 POST /api/uploads/{uploadBatchId}/confirm。
- 確認成功後才顯示「資料已正式寫入 SPC」。

程式結構要求：
1. API 呼叫集中於 service/API module。
2. 表單驗證集中管理，不要散落在畫面事件中。
3. 使用專案既有的登入 Token 與 HTTP client。
4. 定義 request/response 型別。
5. 提供清楚的錯誤處理。
6. 不可使用硬編碼的正式 API 網址，從環境變數讀取 SPC_API_BASE。
7. 不可把 Token、密碼或連線字串提交到版本庫。
8. 保留 API response 中的 uploadBatchId，供追溯及除錯。
9. 若確認 API 失敗，不得重新建立新的上傳批次，應保留原批次供重試。
10. 請提供必要的單元測試或整合測試。

驗收案例一：正常資料
- 輸入有效 pH 值。
- 成功建立批次。
- errorRows = 0。
- confirm 成功。
- SPC 系統可從 VariableMeasurements 查到該 UploadBatchId。

驗收案例二：超出規格
- 輸入超過 USL 的值。
- 前端顯示警告。
- 使用者確認後仍可送出。
- confirm 後 SPC 系統建立對應異常結果。

驗收案例三：不存在的主檔
- 傳入不存在的 ProcessCode、MachineCode、TankCode 或 CharacteristicCode。
- API 回傳檢核錯誤。
- 不得呼叫 confirm。
- 頁面顯示具體錯誤欄位。

驗收案例四：重複送出
- 使用者快速連點儲存。
- 系統只建立一個批次。
- 收到 HTTP 409 時顯示重複資料提示。

請先分析現有專案結構和 API client，再建立頁面。完成後執行正式 build，列出修改檔案與測試結果。
```

## API 欄位確認事項

目前一般計量型匯入流程明確使用：

- `ControlScope`
- `ProcessCode`
- `MachineCode`
- `CharacteristicCode`
- `MeasuredValue`
- `RecheckValue`
- `AdjustAction`
- `AdjustAmount`
- `LotNo`
- `SampleNo`
- `MeasuredAt`
- `Operator`

### TankCode 注意事項

目前正式資料保存的是 `TankId`。在外部系統開始開發前，必須確認通用匯入 API 是否已支援從 `TankCode` 解析槽體。

若尚未支援，正確做法是在 SPC API 增加 `TankCode` 的正式解析與檢核，不要讓外部系統直接查詢 SPC 資料庫取得 `TankId`。

## 安全要求

- API 必須使用 HTTPS（正式環境）。
- 使用 JWT 或正式服務帳號驗證。
- 不得把 Token 寫死在前端程式。
- 不得將資料庫連線字串提供給外部系統。
- 所有寫入操作保留 `uploadBatchId`。
- API 錯誤訊息不得包含資料庫密碼或 Connection String。
- 正式送出按鈕需避免重複點擊。

## 完成判定

只有在下列條件全部成立後，才可視為完成：

- 外部頁面能正確取得或選擇 SPC 主檔代碼。
- 正常資料能建立批次並完成 confirm。
- 錯誤資料停留在預覽階段，不會寫入正式量測資料。
- `VariableMeasurements` 能以 `UploadBatchId` 追溯資料。
- SPC 管制圖與趨勢圖可讀取新增的量測值。
- 超規或失控資料能依 SPC 系統規則產生異常。
- 正式 build 成功。
