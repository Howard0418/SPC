# 小型變更：管制圖 OOS 紅點優先
- 功能 ID：SPC-CHART-OOS-COLOR-1117-TASK-001
- 版本：1
- 狀態：已完成／正式已發布
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

## 正式發布授權與計畫
- 2026-11-17 使用者明示「測試確認，可更新正式區」，授權本件及同一測試版已驗收的開／收線相容標籤正式發布。
- 以正式Web0.1.135隔離來源為基準，只套入`CLOSE/GENERAL`顯示`早(收)`與異常紅點優先，Web升0.1.138；排除其他測試站未授權功能及工作區既有修改。
- 正式API維持0.1.127／production，不更新API、資料庫、量測、公式或設定。發布前備份正式frontend並核對來源／目標；失敗以同批備份回復。
- 發布後驗證首頁、主JS／CSS MIME、前台版本、API health/version/environment、正式Web實際API基底與CORS／用戶端連線。

## 正式發布結果
- 以正式Web0.1.135隔離來源建置Web0.1.138；焦點測試11/11、i18n 2863×3及production build通過。只含`CLOSE/GENERAL`相容標籤與OOS紅點優先；API、DB、量測、公式與統計不變。
- 正式目標核對為`PMR-SPC-SRV.pmr.com.tw`／`SpcWeb`／`C:/inetpub/wwwroot/production/frontend`，發布前完整備份位於`C:/ProgramData/PMR-Releases/Production/SPC-CHART-OOS-COLOR-20261117-222150/backup-frontend`，發布檔案雜湊核對通過。
- 第一輪首頁驗證回500，原因是隔離production build的`web.config`仍為測試站反向代理設定；立即恢復本批備份中的正式`web.config`，未回復新版JS／CSS，也未觸碰API或資料。修正發布腳本與本次紀錄均保留此事實。
- 重驗首頁、`index-C-0By_IR.js`、`index-m59bBhiY.css`皆HTTP 200且MIME正確，前台可見Web版本0.1.138；API health/version為0.1.127／production，CORS回應正常。Chromium由正式Web來源直接fetch正式8443 API，health/version皆200且production。
- 原進度表已備份後更新小工作總表291及專案列317:320；公式錯誤0、OpenXML錯誤0、ZIP 13項、drawing 1、重複entry 0，候選與取代後雜湊一致。
