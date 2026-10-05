# 小型變更：儀器校正測試通知按鍵
- 功能 ID：20260912-calibration-test-email
- 版本：1
- 狀態：已發布 SPC 測試站；實際 SMTP 端到端待操作
- 專案：SPC
- 授權依據：2026-09-12 使用者「能建立測試按鍵嗎」，延續前案儀器校正到期通知；僅測試站驗證，不開啟每日自動寄信、不寫正式庫。
- 需求基準：[需求索引](../../docs/requirements.md)、[基準 3.6](../../docs/SPC_REQUIREMENTS_BASELINE_2026-08-07.md)
- 前案：[儀器校正 v4.1](../20260911-instrument-calibration/spec.md)

## 問題、預期行為與範圍
測試站每日校正信關閉（`Calibration:DeliveryEnabled` 未開），無法用真信確認範本。需在儀器校正管理提供手動測試按鍵。
範圍：SPC Web 提醒設定＋一組測試寄信 API。非範圍：不改每日掃描、防重寄、Portal 摘要、不對保管人清單群發、不啟用自動寄信。

## 需求與驗收
- R-001：具 `calibration.manage` 的 Editor 可對指定有效 Email 寄一封標示【測試】的校正通知樣式信。AC-001：成功回傳收件人；主旨含【測試】；未登入 401、Viewer／無權限 403、無效 Email 400。
- R-002：測試信不寫入 `CalibrationNotification`，不占用每日階段防重寄。AC-002：寄送後通知表筆數不變。
- R-003：沿用 SMTP／本機備份路徑；不依賴 `Calibration:DeliveryEnabled`。AC-003：開關關閉仍可測試；失敗時仍回傳 outbox 路徑。

## 計畫與任務
- 受影響檔案：`IEmailNotificationService`、`SmtpEmailNotificationService`、`InstrumentCalibrationsController`、`InstrumentCalibrationsView.vue`、校正測試專案。
- [x] T-001：先寫 HTTP／權限／不寫通知表測試（R-001～002）。
- [x] T-002：實作測試寄信 API 與設定畫面按鍵（R-001～003）。
- [x] T-003：跑測試與建置；同步需求與變更紀錄。

## 驗證
- 方式、環境：隔離 SQLite／JWT HTTP；Playwright mock UI；不連正式庫。
- 實際結果及證據：校正測試 **77 通過**（含 5 項測試寄信 HTTP）；Playwright **4 通過**；SPC API 建置 0 警告／0 錯誤。測試站備份 `20260912-154446`；`test-email` 未登入 401。
- 發布狀態：已發布 SPC 測試站；未開啟每日自動寄信；未寫正式庫。
- 限制：實際 SMTP／本機 outbox 端到端待測試站操作。