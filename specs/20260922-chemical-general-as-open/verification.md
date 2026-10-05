# 驗證紀錄：藥液 GENERAL 視同早班
- 功能 ID：20260922-chemical-general-as-open
- 日期：2026-09-22
- 資料修正：未執行

## 測試

ode --test tests/chemical-single-line.test.mjs：6 例通過。

## 發布
- SPC 測試前端：D:\\SPC\\release\\test\\frontend
- Portal 測試 Web：D:\\PmrPortal\\release\\test\\portal-web
- 正式 SPC 前端：使用者同意後已發布至 PMR-SPC-SRV C:\\inetpub\\wwwroot\\production\\frontend；即時首頁 200，JS index-DVUw7DcK.js。遠端備份 ackup-general-as-open-20260922-130359。
- 正式後端、正式 Portal、資料庫未改。

## 人工
強制重新整理正式 SPC，開 DP／過硫酸鈉、日期含 8～9 月後製圖：早班應含 GENERAL 歷史點。
