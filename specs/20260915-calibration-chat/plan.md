# 技術計畫
- 功能 ID：20260915-calibration-chat
- 規格：[spec.md](spec.md)
## 實作
設定表新增 NotificationChannel（預設 Email）、ChatWebhookProtected；通知工作新增 Channel（預設 Email）。EF migration 生成並核對只有新增欄。
使用 Data Protection 加密，金鑰存 backend/private-data/data-protection-keys（發布保留），Windows DPAPI 保護。API 只回傳 configured 狀態。
Chat sender 使用不含日誌 handler 的 HttpClient、form payload JSON text、30 秒 timeout、不重新導向；只接受 success=true 確認送達。
排程依管道產生收件鍵、重核設定後投遞。新 /test-chat 端點及舊 /test-email 保留。
## 驗證
先寫 sender、排程及 HTTP 測試，再修改核心。執行校正測試專案、API build、Playwright 與 build:test，mock 外部 HTTP。
## 發布／備份／回復
核對 IIS SPC release/test 與 PMR_SPC_TEST、DeliveryEnabled 未啟用。先備份 backend/frontend（包含既有設定與 private-data），發布 API 啟動套用 additive migration，再發布 frontend。
回復舊應用包即可，新增欄預設相容，保留資料欄與金鑰；不逆向刪資料。正式庫/Portal/正式站不動。
## 來源
[Synology 官方 incoming webhook 文件](https://kb.synology.com/en-ro/DSM/tutorial/How_to_configure_webhooks_and_slash_commands_in_Chat_Integration)
