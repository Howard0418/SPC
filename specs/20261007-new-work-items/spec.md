# Portal 生日/團保與 SPC 咬蝕 X- 小工作規劃

功能 ID：WORK-POOL-20261007  
狀態：DONE（僅排程與發想，未實作）  
日期：2026-10-07

## Goal

將使用者提出的 Portal 生日快樂通知、團保專區，以及 SPC 咬蝕量 X- 資料不列入 SPC 的需求加入小工作池，依輕重緩急排序，供後續逐項確認與實作。

## Scope

- 只新增小工作構想、排序、初步驗收條件與風險。
- 更新 `TODO.md`、需求索引與變更紀錄。
- 不修改 Portal 或 SPC 業務程式。

## Out of Scope

- 不建立資料表、migration、API 或 UI。
- 不讀取 `D:\PmrPortal\GroupInsurance` 內容。
- 不發布 Portal、SPC 測試站或正式站。
- 不開始任何後續小工作。

## Current Behavior

- Portal 已有公告角色隔離與人事角色基礎，但生日資料與團保專區尚未排入工作池。
- SPC 咬蝕量可匯入資料並進入 SPC 計算；目前未明確定義 X- 或不生產資料如何保留但排除 SPC。

## Expected Behavior

- Portal 生日通知、團保專區、SPC X- 排除需求都有清楚的小工作入口。
- 排序優先依資料正確性、權限與高頻操作安排。
- 後續每個小工作仍需各自建立完整 Spec/BDD 後才實作。

## Business Rules

- 每次只處理一個小工作。
- Portal 生日資料屬個資，需限制人事管理並避免一般使用者看到他人生日完整資料。
- 團保專區所有登入使用者可看，人事可管理；資料來源初步為 `D:\PmrPortal\GroupInsurance`。
- SPC 咬蝕量 X- 或不生產資料需可保留匯入紀錄，但不得列入 SPC 統計、管制圖與異常判定。

## Technical Impact

- 本次只有文件與工作池更新。
- 後續實作可能影響 Portal 權限/資料模型/附件服務，以及 SPC 匯入、預覽、確認、查詢與圖表計算。

## API Impact

本次無 API 影響。後續可能新增 Portal 生日/團保管理 API，與 SPC 匯入排除欄位或狀態 API。

## Database Impact

本次無資料庫影響。後續可能需要 Portal 生日欄位/表、團保文件索引表，以及 SPC 匯入排除原因或狀態欄位。

## UI Impact

本次無 UI 影響。後續可能新增生日彈窗、人事生日管理、團保專區與團保管理頁，SPC/TransFiles 匯入預覽需顯示 X- 排除狀態。

## Acceptance Criteria

- AC-001：`TODO.md` 已加入 Portal 生日通知、團保專區與 SPC 咬蝕 X- 小工作。
- AC-002：每個小工作標示狀態、理由、初步範圍、BDD 驗收方向與風險。
- AC-003：需求索引與變更紀錄已同步。
- AC-004：本次未修改業務功能、API、資料庫或 UI。

## BDD Acceptance Criteria

### Scenario: Portal 生日通知排入工作池
Given 使用者要求登入時顯示生日快樂通知
When 本次只做小工作排程
Then TODO 必須包含生日資料管理與登入生日通知的小工作，且不得直接實作

### Scenario: Portal 團保專區排入工作池
Given 使用者提供團保資料路徑 `D:\PmrPortal\GroupInsurance`
When 本次只做小工作排程
Then TODO 必須包含所有人可看、人事可管理的團保專區小工作

### Scenario: SPC X- 不列入 SPC 排入工作池
Given 咬蝕量存在 X- 或不生產資料
When 匯入資料但不應列入 SPC
Then TODO 必須包含保留匯入紀錄但排除 SPC 統計與圖表的小工作

## Risks

- 生日資料涉及個資與可見性，後續需確認資料來源、是否顯示年齡、通知日期規則與首次登入/每日一次規則。
- 團保路徑涉及檔案存取與權限，後續需確認檔案格式、上傳方式、版本保留與下載紀錄。
- SPC X- 可能橫跨 TransFiles/Portal/SPC 匯入契約，後續需確認來源欄位、X- 判定字串與既有歷史資料是否補標。
