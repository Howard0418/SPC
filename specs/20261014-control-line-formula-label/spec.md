# 小型變更：管制線計算公式文案釐清
- 功能 ID：SPC-CONTROL-LINE-FORMULA-LABEL-20261014
- 版本：1
- 狀態：DONE
- 專案／授權依據／需求基準：使用者 2026-10-14 確認將進階設定公式文案改清楚，避免與常態檢定方法混淆。

## 問題、預期行為與範圍
- 現況：SPC 管制項目編輯頁的進階設定區顯示「公式配置」與「公式設定 JSON」，容易被誤解為常態分析公式。
- 預期：進階設定區明確顯示這是「管制線計算公式」，用於 UCL／CL／LCL，不是 Adjusted Jarque-Bera 或 Anderson-Darling 常態檢定。
- 範圍：僅調整前端文案與說明。不修改 API、資料庫、JSON 契約、常態檢定方法或 SPC 計算邏輯。

## 需求與驗收
- R-001：進階設定公式區須明確標示為管制線計算公式。
- AC-001：畫面標題包含「管制線計算公式」。
- AC-002：說明文字可看出此設定影響 UCL／CL／LCL，且不是常態檢定方法。
- AC-003：前端 build 通過。

## 計畫與任務
- 受影響檔案：`frontend/mes-spc-web/src/views/PartProcessCharacteristicsView.vue`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`。
- [x] T-001：調整進階設定公式區文案。
- [x] T-002：驗證前端 build。
- [x] T-003：同步有效需求與變更紀錄。

## 驗證
- 方式、環境：`D:\SPC\frontend\mes-spc-web` 執行 `npm run build`。
- 實際結果及證據：build 通過；Vite 仍提示既有 chunk size warning。
- 發布狀態：已刷新 SPC 測試站 frontend，備份 `release/test/frontend.backup-control-line-formula-label-20261014-105857`；正式站未發布。PowerShell 對 `https://SPCTEST.pmr.com.tw/` 仍發生 SSL 連線失敗，需以瀏覽器或受信任憑證環境再做外部 smoke。
- 限制或未決問題：本次只釐清畫面文案，既有 `formulaConfigJson` 欄位名稱與資料內容不變。
