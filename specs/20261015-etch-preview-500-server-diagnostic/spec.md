# 功能規格：咬蝕量預覽伺服器錯誤可追蹤化
- 功能 ID：SPC-ETCH-PREVIEW-500-SERVER-DIAGNOSTIC-20261015
- 版本：3
- 狀態：DONE（2026-10-19 補完人工預覽與製圖驗證）
- 涉及專案：SPC（主系統）；TransFiles（呼叫端，不修改）
- 授權依據：使用者 2026-10-15 確認處理咬蝕量匯入 123 份預覽 HTTP 500。
- 現行需求基準：`docs/requirements.md` 的 SPC 咬蝕量預覽可讀錯誤規則。
- 前案或相關規格：`specs/20261014-etch-preview-500-diagnostics/spec.md`。

## 目的、現況與證據
2026-10-15 TransFiles 對 SPC 測試站 `POST /api/v1/etch-import-test/preview` 的 123 次咬蝕量預覽皆收到 HTTP 500。IIS 記錄顯示請求已進入 `SpcSingleTest`，因此不是 Excel 格式、Portal、DNS 或 TLS 連線問題。前案已把可預期的主檔與既有資料衝突處理為 409；本次是未處理的伺服器例外，尚無可識別錯誤碼。

## 範圍與非範圍
- 範圍：咬蝕量預覽／確認端點的資料庫例外與未預期例外回應、結構化伺服器紀錄、測試與測試站發布。
- 非範圍：不變更咬蝕量計算、主檔、資料庫 schema、來源 Excel、Portal、TransFiles 解析或任何匯入確認寫入。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 資料庫交易或鎖定例外不得只回泛用 500。 | AC-001 | 端點回 503，訊息含 `ETCH_PREVIEW_DATABASE_FAILURE` 與 Trace ID，且不含 SQL 或連線機敏內容。 |
| R-002 | 其他未預期例外需可由使用者提供追蹤值給系統管理者。 | AC-002 | 端點回 500，訊息含 `ETCH_PREVIEW_SERVER_FAILURE` 與 Trace ID，不含例外詳情。 |
| R-003 | 預覽仍不寫入資料。 | AC-003 | 既有預覽測試維持 UploadBatch、VariableMeasurement 均為 0。 |

## 例外與邊界
- 可預期輸入、主檔及既有資料衝突仍沿用 400/409，不改變前案行為。
- 例外詳細內容只寫伺服器 `ILogger`，回應只提供錯誤代碼與 Trace ID。

## 假設與未決問題
實際資料庫例外原因需由測試站的一筆預覽回應代碼與伺服器結構化紀錄確認；此項不阻擋先提供安全診斷能力。

2026-10-15 程式檢查發現 `PreviewAsync` 與確認匯入共用序列化交易及 SQL `sp_getapplock`。預覽不寫入 UploadBatch 或量測值，該鎖只需要保留在確認匯入；所有日報在第一個鎖定步驟失敗，與本次全數 HTTP 500 的一致現象相符。

2026-10-15 IIS 事件紀錄確認實際根因為：`Auth:Enabled=false` 時未註冊 Authentication Scheme，但 `EtchTestImportController` 的 `[Authorize]` 仍要求授權挑戰，導致 `No authenticationScheme was specified` 未處理例外。需在 JWT 金鑰存在時始終註冊 JWT 驗證，並保留 `Auth:Enabled` 僅控制全站預設授權。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-15 | 初版。 |
| 2 | 2026-10-15 | 預覽改為真正唯讀，不取得確認匯入專用的 SQL 應用程式鎖。 |
| 3 | 2026-10-15 | 確認授權方案未註冊為實際 500 根因，新增 JWT 啟動設定修正。 |

## 2026-10-19 驗收補充
Given TransFiles 已登入 Portal 測試站且採日報 25/50 點模式，When 完成兩站預覽，Then 130/130 份預覽完成且不再發生 HTTP 500。使用者後續另行執行確認匯入，結果 130 SUCCESS；該寫入步驟與唯讀預覽分開記錄，不變更預覽不寫入的要求。
