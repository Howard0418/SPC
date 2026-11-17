# 驗證紀錄
- 功能 ID：SPC-CHEM-CHART-STAGE-LABEL-1113-TASK-001-FIX-001
- 規格版本：1
- 日期／環境：2026-11-17／SPC 測試站
- 實作狀態：已完成
- 驗證狀態：自動化與連線驗證通過；登入畫面待使用者確認
- 發布狀態：測試 Web 已發布；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | `node --test tests/chemical-single-line.test.mjs` | `11/04 早(開)`、`11/04 早(收)` | 新增紅燈案例先失敗為兩個`11/04 早班`；修正後9/9通過，8.420／8.400 與 MR 0.02 保持。 | 通過 |
| AC-002 | 同一測試套件 | 不完整／一般線別顯示不變 | GENERAL 與 DP 混合階段回歸通過；回退只接受同日完整 OPEN/CLOSE。 | 通過 |
| AC-003 | 同一測試套件 | 值、點數與 MR 不變 | I-MR 同一序列、兩點及 MR 對齊回歸通過。 | 通過 |
| 發布 | `npm run build`、`npm run build:test`及 HTTPS smoke | 測試站資產與 API 均為 test | i18n 2863×3、兩種 Vite build 通過；首頁200、`index-B3cdgzbq.js`為200/application/javascript、CSS為200/text/css、同源`/api/health`為ok、`/api/version`為0.1.132/test。 | 通過 |
| 進度同步 | 備份、以 artifact-tool 更新原活頁簿後再匯入檢查 | 僅本工作狀態、實際日期與原生里程碑更新，檔案可開啟且無公式錯誤 | 已備份原檔；`小工作總表`保留核心未完成「是」，更新為2026-11-17測試站已修正／待登入驗收；`專案進度表`原有里程碑更新為11/17 Web132。重新匯入、公式錯誤掃描0筆、OpenXML ZIP檢查13項及渲染檢查通過。 | 通過 |

## 未執行項目、限制及後續
- 使用者登入測試站，重新查詢 N2／化鎳／PH，確認 2026-11-04 顯示`早(開)`／`早(收)`後，才可核銷原父工作。

## 基準與變更紀錄更新位置
- `docs/requirements.md`
- `CHANGELOG_CUSTOM.md`
- `TODO.md`
- `ai_docs/10_change_log.md`
