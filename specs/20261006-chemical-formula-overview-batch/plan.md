# 技術計畫
- 功能 ID：20261006-chemical-formula-overview-batch
- 規格版本：1
- 規格：[spec.md](spec.md)

## 最小修改方案

優先採前端擴充既有 `ChemicalAnalysisOverviewView.vue`，讓使用者在總覽表格直接編輯公式欄位，儲存時逐筆呼叫既有 `PUT /api/v1/part-process-characteristics/{id}`。

理由：
- 後端單筆 PUT 已完成公式版本紀錄，不需新增資料表或重做版本邏輯。
- 改動最小，風險集中在一個前端頁面。
- 若某筆失敗，可保留其他成功結果與錯誤訊息，不需要新增批次交易語意。

## 受影響檔案

前端：
- `frontend/mes-spc-web/src/views/ChemicalAnalysisOverviewView.vue`

後端：
- 第一版原則上不新增後端 API。
- 若既有 PUT 無法滿足部分欄位更新，才考慮新增批次 API；新增前需先補測試。

文件：
- `TODO.md`
- `docs/requirements.md`
- `CHANGELOG_CUSTOM.md`
- `specs/20261006-chemical-formula-overview-batch/`

## UI/行為設計

- 總覽頁保留既有篩選、摘要卡、匯出 Excel 與導向單筆編輯。
- 表格新增可編輯欄位：
  - 濃度公式 `concentrationFormula`
  - 調整公式 `adjustmentFormula`
  - 調整量公式 `adjustmentAmountFormula`
  - 小數位 `decimalPlaces`
  - 必要時顯示滴定值標籤但不在第一版批次改名。
- 已變更列需有明確標示。
- 儲存按鈕顯示變更筆數。
- 儲存前以確認視窗或 modal 顯示變更摘要：線別、槽位、分析項目、變更欄位。
- 儲存後顯示成功/失敗筆數；失敗列保留錯誤訊息。

## 儲存策略

第一版：
1. 載入 `/part-process-characteristics`。
2. 前端保留原始 `chemicalAnalysisConfigJson` 與 editable draft。
3. 使用者修改 draft 後比較原始 JSON。
4. 儲存時只針對異動列組回完整 `PartProcessCharacteristic` payload。
5. 逐筆呼叫既有 `PUT /part-process-characteristics/{id}`。
6. 既有後端會清理 JSON、驗證主檔、建立版本紀錄。
7. 全部完成後重新載入。

暫不新增後端批次 API。若後續遇到效能或一致性需求，再新增：
- `PUT /api/v1/part-process-characteristics/chemical-analysis-formulas/batch`
- 後端交易策略與逐筆版本建立測試。

## 相容性與風險

- 不改資料庫，沿用 `ChemicalAnalysisConfigJson`。
- 不改計算公式語法。
- 不改既有單筆編輯。
- 逐筆 PUT 會更新完整 PPC payload，需確保前端組 payload 時保留原本欄位，不遺漏 `controlScope`、關聯 ID、規格界線與啟用狀態。
- 大量筆數一次儲存可能較慢；第一版以可控小批次與結果回報處理。

## 驗證安排

- 前端 build：`npm run build -- --mode testhost`。
- 靜態檢查：
  - 確認總覽頁存在可編輯公式欄位。
  - 確認只送出 dirty rows。
  - 確認儲存呼叫既有 `PUT /part-process-characteristics/{id}`。
- 測試站 smoke：
  - 前端首頁 200。
  - JS MIME 為 `application/javascript`。
  - 後端 `/api/version` 200/test。
- 人工驗收：
  - 修改 1 筆公式並儲存，確認版本紀錄新增。
  - 修改 2 筆公式並儲存，確認兩筆各自新增版本。
  - 不改任何資料時，儲存按鈕不可送出或顯示無變更。

## 發布與回復

- 測試通過後發布 SPC 測試站 frontend；若未改後端則不發布 backend。
- 正式站需另行授權。
- 回復方法：
  - 程式回復：退回 commit 並重新發布測試站 frontend。
  - 資料回復：用前案公式版本紀錄逐筆回復。
