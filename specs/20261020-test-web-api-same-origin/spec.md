# 小型變更：修正測試站前端誤連舊API位址
- 功能ID：SPC-TEST-WEB-API-SAME-ORIGIN-20261020；版本1；狀態：DONE。
- 使用者回報https://spctest.pmr.com.tw/主檔讀取失敗，訊息指向http://172.16.110.27:8081/api；已確認正是測試站。
- 基準：[單一網址規範](../20261013-unified-web-api-endpoints/spec.md)，瀏覽器API必須同站/api。

## 現況與原因
前次testhost建置仍讀取未追蹤.env.testhost的舊VITE_API_BASE；發布的前端亦包含此舊API。先前HTTP健康與同站API驗證沒有涵蓋瀏覽器實際baseURL，是發布驗證缺口，不將此歸咎使用者快取。

## 需求與驗收
- R-001／AC-001：API client固定使用/api，開發仍由Vite proxy處理；即使環境留有舊IP，瀏覽器主檔與其他API請求也必須同源，避免HTTPS混用HTTP。
- R-002／AC-002：保留JWT、攔截器、環境隔離與原HTTP錯誤處理，不改權限／資料庫／公式。
- R-003／AC-003：真實testhost建置產物在瀏覽器中載入，確認主檔請求沒有8081/API舊主機；最終發布HTTP bundle hash匹配且API健康。
- Given測試HTTPS頁面且舊環境VITE_API_BASE為HTTP私有IP，When開啟檢驗基準頁，Then瀏覽器以目前站台/api發送請求。

## 計畫、任務與風險
- [x] T-001：先補舊環境影響的瀏覽器回歸，再固定api/client.js同源基址。
- [x] T-002：testhost build、既有公式歷史瀏覽器回歸及實際bundle請求驗證。
- [x] T-003：備份並僅發布SPC測試站前端，API0.1.62／DB維持，更新文件並提交上傳。
- 主系統SPC，其他系統無變更，無migration；阻擋測試使用，優先於暫緩KM工作。
- 回復：保留前端備份、必要時還原同批index與assets；不操作正式站與D:/Sites/PmrPortal。
- Boundary：舊環境IP不得覆蓋同源；Invalid：API故障保留錯誤；Regression：認證與版本／環境保護照舊。未執行項目不得記通過。

## 驗證與發布
實作及功能驗證待執行；本案只需前端build／瀏覽器驗證，backend build/test不適用（未改後端）。正式站未授權更新。


## 完成驗證（2026-10-20）
- 先以明確舊VITE_API_BASE環境執行瀏覽器測試，重現跨站API請求失敗；修正API client固定/api後同例通過。未依賴被git忽略的.env修正，後續舊環境不再覆蓋API base。
- 新增playwright.api-same-origin.config.ts與api-same-origin.spec.ts：1 passed；既有公式／日期歷史Playwright：9 passed。
- npm run build:test成功，Web0.1.66（既有chunk大小警告保留）。本次沒有改動API／DB，backend build/test不適用。
- 新增npm run verify:built-api -- <網址>，以實際served bundle驗證同源API。此檢查使用本機UI登入fixture並攔截API、封鎖外部資源，不向後端送出偽造認證或寫入資料，不冒稱真人SSO驗收。
- 本機dist預覽及已發布https://spctest.pmr.com.tw/各通過，皆14個API請求同源；線上index-BajPkjJP.js下載位元組SHA256與dist相同，未包含舊8081 API位址。
- 另以真正線上API唯讀核對檢驗基準依賴的12個端點（PPC、parts、processes、characteristics、chart types、rules/groups、categories、machines、traceability lines/tanks），全部HTTP200；無主檔寫入。
- 僅替換D:/SPC/release/test/frontend，備份release/frontend.backup-api-same-origin-20261020-081604；API0.1.62、appsettings.json及API DLL hash不變。未重啟API、未做migration，正式／KM未操作。
- 本案完成，未完成小工作3件，全部KM暫緩。後續發布前須執行實際bundle的verify:built-api，不能只以手動/api/health成功宣稱瀏覽器API正常。
