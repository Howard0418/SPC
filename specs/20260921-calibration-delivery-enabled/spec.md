# 小型變更：啟用校正通知投遞
- 功能 ID：20260921-calibration-delivery-enabled
- 狀態：已加入測試站設定；真實通知待驗證
- 涉及專案：SPC 測試站

## 目的
啟用測試站校正通知背景工作實際投遞 Email／Synology Chat；不修改通知規則、收件人或 SMTP 設定。

## 驗收
- AC-001：`release/test/backend/appsettings.json` 的 `Calibration:DeliveryEnabled` 為 `true`。
- AC-002：JSON 格式有效，既有設定保持不變。
- AC-003：重啟 SPC API 後背景排程讀取新設定；本次不執行真實寄送驗收。

## 驗證結果
- AC-001／AC-002：PowerShell `ConvertFrom-Json` 驗證通過，值為布林 `true`。
- AC-003：已回收 `SpcApi` App Pool 且狀態為 `Started`；未寄送真實通知，待以測試資料驗收。
