# 驗證紀錄
- 2026-11-17，SPC-PPC-HIDE-CHEM-TAB-1117-TASK-001。程式／測試／測試發布完成；使用者已完成畫面驗收。2026-11-18確認已隨正式Web0.1.140整併生效並結案。
- 依截圖隱藏每個分析項目的整個藥液公式分頁及排程／版本內容掛載；其他四頁保留，前後步驟跳過隱藏分頁。不刪公式JSON／版本／排程／F／API或資料。
- 新增回歸先重現CHEM仍有第4頁（1紅／1綠）；修正後單元／模板／三語檢查8/8通過，覆蓋三種scope、步驟邊界與公式JSON原文保存（含未知欄位）。

## 人工驗收狀態（2026-11-18）
- 使用者已確認測試畫面可驗收並授權更新正式區。
- 2026-11-18唯讀追查確認，本功能其實已隨後續正式Web整併發布生效，不需要重複部署；以下列正式證據結案。
- Playwright公式歷史／畫面10/10通過：兩個分析項目、1280與390寬度皆無藥液分頁；其他分頁可切換，正常保存原公式JSON逐字相同，規格版本／總覽歷史／F表回歸保持。舊測試中進入已隱藏分頁的預期已依新需求更新，修正fixture缺processId造成下拉無機台問題後通過，未繞過產品驗證。
- 原公式排程7/7透過獨立測試HTML掛載原元件驗證：日期、發布、衝突草稿、無效範圍、預覽、Viewer權限及重試保持；該HTML僅為Vite測試fixture，不是產品入口。
- 從乾淨HEAD只封存frontend並加入本件Vue／package版本，排除工作區既有TrendChartView修改。隔離source與本件Vue SHA256相同894E660FD8A611A3F908D2C232F2CFF8B073FD362AB15714EE6254175B90920B。
- npm run build:test及2863×3語系檢查通過；既有chunk-size／Browserslist資料年齡提示保留，未更新無關套件。
- 測試Web0.1.130已更新至D:/SPC/release/test/frontend，備份D:/SPC/.ppc-hide-chemical-tab-20261117/backup-frontend。前端原App版本顯示取package.json，本次主JS包含0.1.130。
- 實際https://SPCTEST.pmr.com.tw首頁200/text/html；主JS index-CA9niPek.js為200/application/javascript、CSS index-ClNDrLOs.css為200/text/css。同源實際API基底/api之health、version均200，API0.1.132／environment=test；無後端更新。
- 進度表已備份、原檔同步小工作307與11/17原生測試里程碑；原118圖形名稱保持、新增1個。17,718個非本件儲存格值一致，公式0錯誤、候選及原檔OpenXML0錯誤，重讀／渲染通過；未做Excel人工開檔或列印。
- 目前核心未完成46件、Portal相關17件；本件待使用者登入測試區開啟任一分析項目確認；未代替使用者登入正式畫面，正式未發布。

## 正式生效核對與結案（2026-11-18）
- 正式Web版本0.1.140的建置來源 `PartProcessCharacteristicsView.vue` SHA-256為`894E660FD8A611A3F908D2C232F2CFF8B073FD362AB15714EE6254175B90920B`，與本件測試驗收來源完全相同。
- 正式首頁與主JavaScript `index-BlzugLq4.js` 均HTTP 200；主資產為`application/javascript`、SHA-256 `B2FC53B5815853860CC137188C905FA6B30ACC7B9484BCC397A5AA6DAB5EA84D`。CSS HTTP 200／`text/css`。
- 正式主資產內`chemicalFormulas`僅存在三語語系字串，分頁陣列、排程元件與內容掛載程式均已由建置器移除，符合固定隱藏行為；公式JSON／F／API／資料庫未修改。
- 實際正式API基底`https://PMR-SPC-SRV.pmr.com.tw:8443/api`的health／version成功，版本0.1.134、environment=`production`。瀏覽器操作服務本輪不可用，未新增正式登入點擊證據；沿用使用者既有測試畫面驗收，並以正式來源與實際資產交叉確認。
- 此功能由後續正式Web整併發布一併生效，本輪未傳檔、未停站、未修改API／DB／設定，也未重複遞增版本。中央進度核銷後核心未完成32→31、SPC相關12→11、完成247→248。
