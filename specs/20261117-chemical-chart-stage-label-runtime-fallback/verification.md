# 驗證紀錄
- 功能 ID：SPC-CHEM-CHART-STAGE-LABEL-1113-TASK-001-FIX-001
- 規格版本：2
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
| AC-004 資料確認 | 正式資料庫唯讀查詢 PPC2587 的 2026-10-22 兩筆資料 | 舊收線欄位契約可被明確辨識 | ID79634=19.260、`OPEN/GENERAL`；ID79642=17.470、`CLOSE/GENERAL`。同類舊資料另見10/20、10/28；未修改正式資料。 | 通過 |
| AC-004 紅燈／回歸 | `node --test tests/chemical-chart-close-phase.test.mjs tests/chemical-single-line.test.mjs` | N1／N2舊`CLOSE/GENERAL`顯示`早(收)`，其他線別提示不變 | 新斷言修正前失敗（實際`收線待校正`）；最小修正後11/11通過。 | 通過 |
| 建置／測試發布 | 隔離乾淨來源 `npm run build:test`，發布測試站 | Web版本遞增且不夾帶工作區其他修改 | i18n 2863×3及test build通過；測試Web0.1.133→0.1.136。首頁200/text/html、`index-D3NDVWRT.js` 200/application/javascript且含0.1.136、CSS 200/text/css；同源health/version均為API0.1.132/test。API未更新。 | 通過 |
| 回復點 | 發布前payload備份 | 可回復測試Web0.1.133 | 初始備份命令的萬用字元誤用未複製檔案；已立即由發布前提交`f5d7bcf5`隔離重建Web0.1.133回復payload並驗證版本／檔案，未影響目前測試站。 | 通過（已補正） |
| 進度同步 FIX-002 | 備份、artifact-tool更新、重新匯入／渲染／OpenXML驗證 | 只更新本工作列與原生里程碑 | 原檔備份雜湊一致；小工作總表291與專案列317:320更新Web136及待登入驗收。公式錯誤0、OpenXML錯誤0、ZIP 13項、drawing 1、重複entry/Print_Area 0；候選與原檔取代後雜湊一致。 | 通過 |

## 未執行項目、限制及後續
- 使用者登入測試站，重新查詢 N2／化鎳／PH，確認 2026-11-04 顯示`早(開)`／`早(收)`後，才可核銷原父工作。
- 使用者登入測試站查詢 N1／表處／氫氧化鉀，確認 2026-10-22 的19.260顯示`早(開)`、17.470顯示`早(收)`後，才完成 FIX-002 人工驗收。
- Windows瀏覽器自動化管線與瀏覽器存取主機目前離線，無法代為取得登入後截圖；HTTP與同源API驗證已完成。正式未發布。

## 基準與變更紀錄更新位置
- `docs/requirements.md`
- `CHANGELOG_CUSTOM.md`
- `TODO.md`
- `ai_docs/10_change_log.md`
