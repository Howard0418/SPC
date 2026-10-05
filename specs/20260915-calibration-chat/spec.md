# 功能規格：校正通知選用 Email 或 Synology Chat
- 功能 ID：20260915-calibration-chat
- 版本：1
- 狀態：測試站已發布；真實 NAS 端到端待使用者操作
- 涉及專案：SPC
- 授權：使用者要求加入 Synology Chat 並可選 Email 或 Chat。
- 基準：[校正通知](../20260911-instrument-calibration/spec.md)、[提醒天數](../20260915-calibration-reminder-picker/spec.md)。

## 目的、現況與範圍
現有設定及持久化排程僅支援 Email。本次增加全域二選一管道及單一 Chat incoming webhook 群組。保留天數、08:00、使用狀態、重試與 Unknown 不重寄規則。Portal、正式環境不在範圍。
## 需求與驗收
| 需求 | 規則 | 驗收 | 通過條件 |
|---|---|---|---|
| R-001 | 預設 Email，切換 Chat 須已設定 webhook | AC-001 | 舊設定相容、無效管道或網址拒絕、既有網址可保留 |
| R-002 | Webhook 加密儲存，不回傳至 API 或稽核、日誌 | AC-002 | GET/PUT 不含明文或密文、空白保留，權限驗證 |
| R-003 | Chat 每儀器每階段只送群組一次，不依 Email 人數重複 | AC-003 | 缺 Email 仍可 Chat、重掃防重、切換取消舊待送、重試/Unknown 保留 |
| R-004 | 手動測試依所選管道，可用未儲存 webhook | AC-004 | 授權、【測試】訊息、不寫排程紀錄、明確區分拒絕與不確定結果 |
| R-005 | 簡單管道選擇及中文設定說明 | AC-005 | 桌面/手機可操作、Email 回歸、Chat 成功與失敗顯示 |
## 例外與邊界
僅接受 HTTP(S) 的 Synology /webapi/entry.cgi incoming webhook；不可含 userinfo/fragment，必須有 api=SYNO.Chat.External、method=incoming、version、token。不跟隨重新導向，不略過 TLS 驗證。完整 URL 不寫日誌。HTTP/應用層明確拒絕為 Failed，傳輸中斷與未知回應為 Unknown。
自動投遞仍須 Calibration:DeliveryEnabled，手動測試不受此開關控制。切換為 Chat 後不寄 Email；Chat 目的地以 URL 雜湊防重鍵隔離，舊工作重新核對管道/目的地。
## 假設
採整體通知設定二選一及單一群組，符合現有全域提醒設定。使用者日後於畫面貼上 NAS webhook；本次不取得真實 token、不發真訊息。
## 版本紀錄
- 1：2026-09-15 初版。
