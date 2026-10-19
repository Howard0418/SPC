# 技術計畫
- 功能 ID：SPC-ETCH-PREVIEW-500-PORTAL-FORWARDING-20261015
- 規格：[spec.md](spec.md)

- Portal `EtchImportTestController` 解析 SPC 的 JSON 安全欄位並回傳至 TransFiles；解析失敗時維持既有泛用訊息。
- 新增 Portal controller 單元測試，涵蓋 503 診斷轉送與無效 JSON fallback。
- 測試通過後只發布 Portal 測試 API；不發布 SPC 或正式站。

## 2026-10-19 T-003 收尾
- 驗證工具：`tools/testing/verify_etch_preview_diagnostics.py`，透過 `--transfiles-root` 指定來源 checkout；執行實際預覽/GUI 失敗分支，替換 HTTP opener 與訊息視窗，不建立 Tk 視窗或送出請求。
- AC-002 對應：500/503 回應的 code/trace 必須同時存在於提示、操作日誌與 JSON LOG；另驗證第一筆失敗停止後續預覽、confirm 阻擋與安全遮罩。
- Portal 回歸：`PmrPortal.Etch.Tests` 的 EtchStrictTests；不變更 Portal 或 TransFiles 應用程式，因此不需打包或重新發布。
- 回復：revert 本次測試/文件提交即可；不需資料回復。
