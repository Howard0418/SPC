# 本次規劃驗證（2026-10-22）
- 已閱讀SDD、需求索引、相關常態檢定前案、前端兩頁/服務/常態單元測試；第二選項確為Anderson-Darling，預設目前仍AJB。
- 已比對前案可切換規則，本次明確需求將於實作完成後取代網站切換及預設；不覆寫歷史驗證。
- 已建立1件P2小工作，涵蓋同一功能的兩頁/後端一致性，測試規劃含Happy/Boundary/Invalid/Regression。
- 本次僅新增規格與工作池/需求/變更紀錄；內容/相對連結/git diff檢查。未執行任何應用測試/build/站台發布，均不適用於本次文件型Task。
- 未修改正式/測試站、原始量測、API計算、DB、套件、IIS或Runtime；不啟動下一工作。
- 工作池：本件1＋i18n20包＝21件待執行/待確認；另附件掃毒受阻1、Runtime及TransFiles人工故障驗收暫緩2。

## 2026-10-23 實作與驗證（最新狀態）
- 使用者明確要求發布測試站；先記目的及v2範圍，再新增預設AD/別名/空白/未知/排除資料及不足樣本測試。修正前常態19項中6失敗/13通過，修後19項全通過；AJB專屬固定案例改為明確傳AJB，未刪除舊計算回歸。
- 後端只修改方法選擇：明確AJB/AdjustedJarqueBera沿用AJB，其餘（含省略/空白/未知）預設AD；既有AD算法、DTO、偏態/峰態回傳與計算不變。未修改管制線/能力/資料/歷史。
- 前端兩頁固定傳AD、移除方法下拉及偏態/峰態（趨勢頁原標「峰度」），保留方法固定文字/直方圖/曲線；p-value格式、缺值說明與判定共用normalityPresentation.js。
- 先建Node測試（模組不存在失敗），實作後3項通過；涵蓋null/undefined/NaN/Infinity/範圍外/非數值、0/極小正值/0.0001/0.05/1，以及未四捨五入判定。
- tests/MesSpc.Api.Tests全套187 passed/8 conditional skipped；backend/MesSpc.Api.Tests全套47 passed/1 skipped，略過不算通過。後者既有UnitTest1兩個CS8602警告未擴張修正。
- 兩頁Playwright共8情境：一般、390px極小p-value、無變異、缺結果/樣本不足；固定AD請求/無下拉/無偏態峰態/正確提示/無pageerror/無寫入通過。截圖目視確認桌面兩頁版面正常。
- npm build:test與API Release publish通過，前端保留既有大chunk警告；Vite在其他頁EquipmentPointsView的table/tr既有提示未順手修正。
- Windows本機Playwright測試通過後清理等待Vite5197；僅辨識並停止本次測試專用程序後runner正常exit0。沒有停止IIS或使用者瀏覽器。
## 測試站發布與產物驗證
- 目標IIS SpcSingleTest，實際D:/SPC/release/test/backend（含frontend靜態目錄）且AppEnvironment=test；正式無操作。
- 備份D:/SPC/tmp_production_config_plan/normality-fixed-ad-20261023/backup，backend/frontend逐檔SHA256驗證；部署後來源payload及全部appsettings/web.config SHA256吻合，現場資料與舊assets保留。
- 停止/啟動僅SpcSingleTest pool，測試API0.1.72.0/Web0.1.71已部署；實際version/health200、environment test、status ok。
- 已發布資產另跑同8項Playwright全部通過，API路由產物檢查14個請求皆同源。此驗證攔截所有API並使用合成localStorage token/查詢資料，只驗證已部署資產，沒有把mock token送到後端或冒稱真人SSO/真DB常態計算驗收。
- 初次產物檢查缺本機preview5196服務而失敗，啟動指定preview後通過；正式發布後畫面fixture初次缺登入佔位而進登入頁，補回僅本機攔截用途token後8項通過，未降低實際驗證/權限。
- 真實API正常回報另以不帶mock的request核對。未建立測試量測/預覽/匯入或改寫歷史資料；應用啟動沿用既有行為，無新migration/DB備份或整庫還原。
- rollback：停止測試pool後回復同批backend/frontend應用備份，保留appsettings/web.config/uploads/現場資料，再啟動並核對API0.1.70與原Web0.1.70；不整庫還原。
- 正式仍API0.1.71（先前PPC單行修正版）/Web0.1.70，本次AD功能未正式發布。
## 完成與限制
- 本件Spec/BDD/必要測試/build/測試發布與文件同步完成，TASK-001 DONE；真人登入後可再確認正常業務查詢，不冒稱已真人驗收。
- 原規劃格式建議已納入本次顯示；未確認/不在本件的其他報表/三語/歷史算法不擴張。
- 僅新增和變更必要程式/測試/文件，截圖/測試報告/備份/產物不提交；剩i18n20包，另受阻1/暫緩2。
