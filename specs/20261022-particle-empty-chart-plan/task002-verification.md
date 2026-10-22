# TASK-002 驗證與發布

## 結果
- AC-003／004：已完成測試API預覽→確認1152筆、真實瀏覽器32／4點及9位置比較。資料與操作條件見[輸入記錄](task002-input-record.md)。
- AC時間往返：首次實站查到SQL datetime2時間未標UTC，右側比較空白、顯示時間偏移。先新增SqlUtcTimes回歸，修改前1項失敗；ParticleQueryService四處輸出SpecifyKind(Utc)，保持ticks及計算不變。修正後Particle13項通過。
- API測試176通過、8條件式略過（不計通過）；Release publish成功。無新增migration／套件／schema或統計修改。
- 使用現有Editor身分、既有Portal SSO端點；無建立帳號／角色，token只在記憶體與stdin，未保存。
- Playwright對真實測試站無mock，1366×900、Asia/Taipei：32點／32原始列及4點／4原始列，兩張canvas；兩案例比較API均9筆非缺測、與來源計數一致；所有圖表時間含Z。實際截圖目視確認趨勢、界限、位置長條及08:08時間正常。
- 無採樣體積時U圖422／SAMPLING_VOLUME_REQUIRED；沒有補造體積。其他格式錯誤及R10～R12不屬本批修復。

## 發布與回復
- SPC測試API 0.1.69，IIS SpcSingleTest，`D:/SPC/release/test/backend`；既有前端未更新。
- 發布前驗證IIS實際路徑／pool及test／PMR_SPC_TEST；先備份backend並限制備份ACL，保留appsettings與web.config且SHA256不變。首次32位元IIS管理呼叫在讀取站台前失敗，未異動；改64位元工具完成。
- 備份：`D:/SPC/release/backup-particle-utc-20261022-095416/backend`。回復：只停SpcSingleTest pool，將備份應用檔還原至原backend，保留環境設定後啟動，核對version／health；不操作正式站或其他IIS站台。
- version=0.1.69／test、health=ok。發布後唯讀再確認Particle仍1152筆、Imported批次僅1筆、無Particle匯入錯誤，沒有重複寫入。
- 正式站、Portal、KM、Runtime未更新；本案無新migration。既有SQL Server啟動migration檢查路徑沿用，未新增升級內容。

## 本機證據（不提交Git）
`D:/SPC/.codex-tmp/particle-plan/`：task002-result.json、task002-browser.json、deploy-result.json、particle-live-32-points.png、particle-live-4-points.png。JSON、截圖、衍生資料及發布產物不入版控。

## 測試風險覆蓋
Happy Path：32點及九位置；Boundary：不足20點的4點仍顯示與有效0保留；Invalid：無體積U圖拒絕；Regression：SQL型態時間UTC往返與API既有測試。未新增不必要全系統回歸；未聲稱使用者已親自驗收。

狀態：DONE；資料保留供使用者確認。其餘20包i18n未執行，外部受阻／Runtime暫緩2項與舊TransFiles人工驗收1項另列；STOP。
