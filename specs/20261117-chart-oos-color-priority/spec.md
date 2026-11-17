# 小型變更：管制圖 OOS 紅點優先
- 功能 ID：SPC-CHART-OOS-COLOR-1117-TASK-001
- 版本：1
- 狀態：測試站已修正，待登入畫面驗收
- 專案／授權依據／需求基準：SPC Web；使用者於2026-11-17明示「如果該點是OOS，就秀紅色」；沿用既有OOS紅點與N1／N2開收線單線圖需求。

## 問題、預期行為與範圍
- 問題：藥液點有班別／階段樣式時，前端先採班別顏色，導致`outOfSpec=true`仍顯示藍色或其他正常色。
- 預期：排除點維持灰色；未排除且OOS、OOC或規則失控的點一律紅色；正常點才依班別／階段著色。OOS點可保留原階段符號與放大尺寸。
- 範圍：只調整SPC管制圖前端點色優先順序與回歸測試。API、資料庫、量測、規格、判定、排序、MR與統計均不修改；正式站不發布。

## 需求與驗收
- R-001：N1／N2開線或收線點只要`outOfSpec=true`且未排除，必須顯示紅色。
- R-002：排除點灰色優先；正常開／收線顏色保持。
- AC-001：Given N1收線17.470且OOS，When繪製I-MR，Then點色為`#ef4444`、邊框`#991b1b`，數值與標籤保持。
- AC-002：正常開線仍為藍色；排除點仍為灰色。

## 計畫與任務
- 受影響檔案：`frontend/mes-spc-web/src/views/SpcChartView.vue`、`frontend/mes-spc-web/tests/chemical-single-line.test.mjs`、Web版本檔。
- [x] T-001：先新增OOS＋收線紅點紅燈測試。
- [x] T-002：最小修正顏色優先順序。
- [x] T-003：執行焦點測試、建置、測試站發布與連線驗證。
- [x] T-004：同步需求、變更紀錄、TODO及進度表。

## 驗證
- 方式、環境：Node圖表實際renderer測試、i18n、Vite test build、SPC測試站HTTP／MIME／同源API。
- 實際結果及證據：新增N1正常開線19.040藍色與收線17.472／`outOfSpec=true`紅色斷言；修正前實際為藍色而紅燈失敗，修正後焦點11/11通過。i18n 2863×3及test build通過。
- 發布狀態：測試Web0.1.136→0.1.137；首頁200/text/html、`index-31c38NHO.js` 200/application/javascript且含0.1.137、CSS 200/text/css；同源health/version為API0.1.132/test。API、DB與正式站未更新；回復點`D:/SPC/.oos-color-fix-rollback-20261117`為Web0.1.136。
- 進度同步：原活頁簿小工作總表291及專案列317:320更新Web137與待登入驗收；備份雜湊一致，候選／原檔取代後雜湊一致。公式錯誤0、OpenXML錯誤0、ZIP 13項、drawing 1、重複entry 0，渲染通過。
- 限制或未決問題：瀏覽器自動化native pipe與browser host仍離線，未取得登入後截圖；使用者需在測試站確認10/22與11/09的17.470為紅點後才完成人工驗收。正式未發布。
