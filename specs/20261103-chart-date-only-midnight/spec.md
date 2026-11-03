# 小型變更：圖表省略零時分秒
- 功能ID：SPC-UI-1103-TASK-002；版本1；2026-11-03。
- 狀態：DONE；測試Web117、正式Web118發布及兩頁實站驗收完成。主系統SPC前端；無相依系統、DB或API變更。

## 範圍與需求
使用者截圖要求移除Xbar圖座標、tooltip標題及「實際量測」內無用的00:00／00:00:00。兩個圖表頁 `/spc`、`/trend-chart` 使用同一顯示規則；涵蓋主副圖、趨勢圖及量測點明細／異常明細的一致顯示。只在原顯示時區的時間精確為午夜時顯示日期；非零量測時間仍保留，沿用現有時區與日期格式。資料庫、匯入來源、API值、排序、分組、SampleNo、25／50子組計算及原始資料下載均不改；操作／稽核時間不屬本件。

## BDD與測試矩陣
- AC1 Given量測2026-09-29T00:00:00，When查看座標／tooltip／點明細，Then短日期為09/29、完整量測日期為2026/9/29，不顯示零時分秒。
- AC2 Given非零時間12:34或00:00:01，Then保留有意義時間；不同時區仍依既有本機顯示，不截斷UTC字串造成日期錯置。
- AC3 Given空／無效時間，Then維持缺值提示且不拋錯；非日期軸標籤P#與藥液班別／階段不受影響。
- AC4 Given XBAR_R／XBAR_S／I_MR及趨勢資料，Then數值、子組數、樣本n、控制界線／違規、縮放與原始下載保持，只有顯示文字改變；測試站實際Web／資產／API連線及瀏覽器驗證通過。
- Happy Path：午夜日期；Boundary：跨日／非零秒；Invalid：空／無效；Regression：25／50樣本、非零時間、i18n及圖表呈現。

## 計畫與任務
- [x] 先補格式及兩頁圖表行為驗證，記錄舊版失敗。
- [x] 最小顯示helper與兩頁呼叫，隔離建置排除既有未提交修改。
- [x] 相關測試／build，發布測試Web117、驗證實際連線與畫面。
- [x] 需求／變更／原進度表／原生里程碑同步及提交上傳。
- 使用者本輪已明示同意正式Web發布；保留既有正式API102及資料，不重匯。本件不改前一件咬蝕正式API／開關。
- 回復：還原該站前端入口／資產備份，不改量測資料或API。

## 驗證
- 單元14通過（格式4、既有子組6、圖表呈現4）；兩頁日期＋既有normality/i18n-trend共28個Playwright通過。午夜／非零時分秒／時區／缺值、安全輸出及原值保持覆蓋；XBAR_S樣本n25及資料向量、無API寫入已核對。舊版先因缺新helper失敗；首次新畫面測試沿用fixture隱藏點501而預期不符，換獨立點701/702後通過，未改產品排除規則。
- 隔離來源自0982e8e5套本件diff，未帶使用者TrendChartView既有11行修改；check:i18n與test build成功。正式候選從既有Web115正式來源隔離套本件，版本118／production build成功，未帶Audit及其他測試新功能，未發布。
- 2026-11-03測試實際IIS SpcSingleTest／D:/SPC/release/test/frontend發布Web117，API101保持。首頁、index-DfNkQ2Yq.js與index-BFEI4qhU.css均200/MIME正確；同源/api/health、/api/version為test／0.1.101。前端web.config與後端設定hash保持，無DB／API部署。備份frontend.backup-chart-date-20261103-125119；需回復時還原該站index與原資產，無資料回復。
- 使用者登入後實際QE1九十月33點成功載入；9/29圖軸及tooltip為09/29，實際量測2026/9/29，Xbar0.9404／n25保持。趨勢頁亦33個完整子組／825原樣本；無API離線。證據在Git外D:/SPC/.chart-date-20261103/test-spc-tooltip.png及測試／發布紀錄。
- 原Voice/進度表.xlsx已備份後取代，hash 6f2a411e561903cd56d87aa910d76e313e276729214df3d8f0644e4999b757cc；新增115～118事件與第46原生藍色里程碑、總表264列。231登錄、核心43／SPC直接21、完成187、人工11／核銷4；同時記錄前件正式API已獲授權待依序執行，無額外啟動其他工作。
- 原45物件及其他儲存格／封裝part保留；ZIP、OpenXML SDK候選／原檔0錯誤、artifact重開渲染／公式錯誤0、LibreOffice12頁列印預覽通過。原路徑重讀成功，備份Voice/備份/進度表_圖表零時分秒前_20261103-125251.xlsx；Microsoft Excel人工開啟未做。
- 授權前階段曾僅核准咬蝕API，故當時沒有發布日期前端；此待授權狀態已由使用者後續明示同意結清，現行正式結果見下節。

## 正式授權後驗收（最新，前述待授權狀態已結清）
- 2026-11-03使用者確認正式已登入並明示同意日期前端發布。既有隔離production候選118沿正式Web115基準，僅新增入口及主JS/CSS三檔；未帶工作區原11行修改／其他測試新功能。
- 實際PMR-SPC-SRV／SpcWeb／C:/inetpub/wwwroot/production/frontend及443 binding、原入口hash/API102前置核對通過，WinRM上傳／發布三檔hash吻合。正式Web118已部署，API102 DLL／appsettings與前端web.config hash保持；沒有資料庫／匯入／計算修改。
- 正式首頁、index-CZRW1epP.js及index-DHFohvvl.css皆200／MIME正確；瀏覽器實際API基底https://PMR-SPC-SRV.pmr.com.tw:8443/api的health/version為production／0.1.102、使用者電腦可達，CORS正常。Portal Web247及API218保持，健康／資料庫production及版本核對成功。
- 使用者本人正式登入後頁尾Web118／API102，九十月QE1管制圖32點正常載入；9/29軸與tooltip僅09/29、實際量測2026/9/29，Xbar0.9404／n25保持；異常清單無午夜零時間。正式趨勢圖32完整子組／800原始樣本，座標日期無零時分秒，無API離線／逾時。正式登入後API驗收亦回填前件，EXE／正式工具preview仍獨立待人工。
- 舊14單元／28畫面回歸與兩份隔離build持續有效，本輪只有正式版本與部署／文件，未重跑無新疑慮的產品測試。Git外證據D:/SPC/.chart-date-20261103/formal-spc-tooltip.png、formal-trend.png、formal-deployment.json；第一次preflight路徑陣列錯誤發生於入口更新前，修正後全部驗證通過。
- 正式前端回復備份：C:/ProgramData/PMR-Releases/Production/chart-date-20261103-132113/frontend-before；必要時還原原入口與資產，不動API／資料。沒有發布到D:/Sites/PmrPortal。
- 原Voice/進度表.xlsx已備份後原路徑取代，hash e840391b9356c0f26b176b3b189c03c49a33a3f4f934caed587d34d8c41033ec。123～126列新增正式完成事件及第48個原生綠色里程碑，原47物件／其他儲存格與封裝part保持；總表264列DONE／目前待辦否，263列回填正式瀏覽器已驗、EXE待人工。231登錄、核心42／SPC直接20、完成188、人工11／核銷4。
- ZIP、日期／內容／單一Print_Area／原生物件、OpenXML SDK候選／原檔0錯誤、artifact重開渲染／公式錯誤0通過。LibreOffice13頁列印預覽通過；為避免119～122舊事件與新完成事件跨頁分離，僅新增118列後一個原生人工分頁，不累加重複rowBreaks。更新前備份Voice/備份/進度表_圖表日期正式完成前_20261103-132504.xlsx；原檔hash護欄／取代後重讀通過，Microsoft Excel人工開啟未做。
