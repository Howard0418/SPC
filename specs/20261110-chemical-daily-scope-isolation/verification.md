# 驗證紀錄

## 結果
- 根因：藥液日報與月曆未限制 PPC `ControlScope=CHEM`，使 QE2 同日 50 筆咬蝕量 PROCESS 子組被誤判為藥液重複。
- 修正：日報、月曆與 PortalDaily 寫入防重只接受啟用中的 CHEM 映射；真正 CHEM 同鍵重複仍回 409 `MEASUREMENT_MORNING_DUPLICATE`。
- Portal：可辨識的 409 顯示「資料衝突」與明確修正入口；其他失敗仍顯示系統錯誤。
- 使用者已在測試站依 QE2／2026-11-05／早班／GENERAL 實際驗收正常。

## 自動化與建置
- 聚焦 SQLite 3/3；後端完整測試 92 通過、1 個既有工具案例略過；Release build 0 錯誤。
- Portal 衝突文案契約 1/1；Release build 0 錯誤。
- 測試站：SPC API `0.1.117/test`、Portal Web `1.0.267`；首頁、CSS、health/version 與 Portal 實際 API 基底均成功。

## 正式發布
- 使用者於測試正常後明確授權正式站。
- 由正式 API 103 與 Portal Web 248 隔離建立候選；結果為 SPC API `0.1.104/production`、Portal Web `1.0.249`。
- 正式 SPC／Portal 首頁 200；目前 JS／CSS 200 且 MIME 正確；8443 health/version 成功；Portal Origin CORS 預檢 204；日報端點匿名 401 保持。
- 正式 COPY_ONLY／CHECKSUM 備份與 RESTORE VERIFYONLY 完成。發布後指紋：VariableMeasurements `17475:-443366553`、PartProcessCharacteristics `129:1225167489`、migration history `59:1322868254`，與既有正式基準一致。
- 第一次停池失敗時檔案未覆蓋並恢復原服務。R2 寫入後驗證腳本因 PowerShell `$HOME` 保留名中止；兩站池受控啟動後完成上述驗證，無資料或 schema 異動。
- 正式登入後畫面由使用者最後確認；技術發布與連線驗證已完成。

## 回復
- SPC：`C:/ProgramData/PMR-Releases/Production/SPC-CHEM-DAILY-SCOPE-1110-20261110-R2/api-before`。
- Portal：`C:/ProgramData/PMR-Releases/Production/SPC-CHEM-DAILY-SCOPE-1110-20261110-R2/web-before`。
- 只還原各自五個應用程式檔並重啟池；本件不需資料庫回復。
