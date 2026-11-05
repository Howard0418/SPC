# PT1正式排程修復驗證

2026/11/05使用者另明示授權，只處理PT1四項。正式資料修復完成；瀏覽器畫面未驗，不核銷人工註記或其他父件待辦。

| PPC／項目 | 原revision | 新revision | 重用公式歷史 | 重用F歷史 |
|---|---:|---:|---:|---:|
| 2440 脫脂硫酸 | 13 | 81 | 99 | 1 |
| 2441 粗化銅離子 | 14 | 82 | 100 | 1 |
| 2443 酸洗鹽酸 | 15 | 83 | 101 | 1 |
| 2444 酸洗銅離子 | 16 | 84 | 102 | 1 |

## 環境、資料與計算

- 實際SQL目標172.16.110.16／PMR_SPC_2026，API `0.1.102/production`及health成功；來源PMR_SPC_TEST唯讀、前後70表SHA256保持。
- 正式備份`PMR_SPC_2026_PT1_SCHEDULE_20261105-114421.bak`在SQL Server預設備份目錄，COPY_ONLY／CHECKSUM與VERIFYONLY通過；原政策、全部原revision／effectivity及前後SHA證據在`.pt1-current-schedule-20261105/`，不提交。
- 四項共新增4份revision／80段（76段原十月＋4段目前）；原19段逐項日期／公式／F完全保持。台北11/03 00:00起無截止，10/31～11/02既有空缺不猜測補齊。只追加排程與切換四個policy ActiveScheduleRevisionId，IsEnabled保持。
- 公式歷史、F歷史0新增、目前主檔與啟用F不變；重用F快照8格數值與測試相同，F!B3=.99、其他七格1。F歷史1保留原標準液名稱，不把測試null說明蓋回。
- 正式69表中除三個允許表外，66表SHA256完全保持，包括全部量測、Upload、Receipt／Trace／統計與告警。三個允許表中，全部舊revision／effectivity、非目標政策及各項IsEnabled亦保持。
- 使用與Portal輸入相同的ChemicalPortalMeasurementService.PreviewAsync，四項×11/03邊界／11/05／12/01×原始值0／1／6.5／10×初驗／複驗＝96組雙區結果一致（192次引擎計算）；比對Raw／Stored／判定／調整及調整量。
- 獨立新連線重驗100項：上述96組及4筆11/02 23:59:59仍拒絕；目前revision19段歷史逐項一致，0待變更。再次實際apply新增revision0／formula0／F0／量測0，100項及保護SHA256再驗通過。
- 新核心先紅測試，9項規則測試通過；Chemical全部126通過，工具build零警告零錯誤。

## 畫面與發布

瀏覽器工具能列出Chrome測試及正式分頁，但createBrowserTab／getTab均回`Cannot read properties of undefined (reading 'timeoutMs')`；沒有實際點選輸入或截圖，不宣稱人工畫面通過。使用者可刷新正式Portal、PT1、11/05重新預覽四項。本件不新增量測、不要求補造歷史資料、不修改或發布Web／API；原正式程式版本保持。

## 進度與回復

原進度表已先備份`Voice/備份/進度表_PT1正式排程修復前_20261105_1146.xlsx`並取代原路徑；父件265列及11/05原生里程碑同步，70個原生物件保持、新增1個為71。指定儲存格、265列必要高度外，其他內容／樣式／封裝部件保持；核心旗標45／SPC22不變。OpenXML零錯誤、Artifact重開／公式錯誤0及列印99頁目視通過。Microsoft Excel人工開啟未做。原檔SHA256 `EECC59DC8E494777282C73A8BAD0D590BCA132BC944312E9EE3B9BA9DA5BC280`。

本件必要程式／測試／文件提交推送Develop，Git紀錄為據；不提交證據、設定、備份、Excel或使用者原有修改。中央既有公式一致性父件仍未DONE，核心45／SPC22、236登錄／190完成、人工11／核銷4保持。其餘正式76項仍缺11/05排程，本次未授權處理。

回復須先核對新期間是否已有正式Receipt；無新使用才可另受控切回原政策13～16，已有使用不可破壞憑證。不得直接整庫覆蓋後續正式輸入。原備份保留。
