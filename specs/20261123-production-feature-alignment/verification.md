# 正式驗證紀錄

- SPC 正式 Web 0.1.147 → 0.1.148，API 0.1.139 → 0.1.140/production，正式來源 3ae8861e。正式日報歷史日期、舊備註行為及暫時維護回應重試與測試版對齊。API 焦點 7 項與 Chromium 2 項通過，完整前端及 API 建置通過。另一組日報測試 2 通過／4 失敗，原正式控制器重跑同四項 GroupBy 轉譯失敗，未認列為全數通過。
- SPC 應用設定與稽核執行身分維持。使用獨立維護連線完成 COPY_ONLY／CHECKSUM／VERIFYONLY，正式 Web 入口與 API 應用檔留存。正式瀏覽器 Web 版號、首頁與主資產 MIME、實際跨來源 8443 API health/version 與 production 通過。回復點 SPC-PROD-1123-ALIGN。
- Portal 正式品保 SSO 權杖續發，焦點測試 6 項、建置與正式瀏覽器連線通過。API 1.0.262 → 1.0.263，Web 1.0.287 → 1.0.288，來源 f317390，回復點 PORTAL-PROD-1123-ALIGN-SSO。
- Portal 正式錯誤紀錄移交管理員，焦點 4 項通過。預檢確認 supervisor 無指派、無稽核引用，備份驗證後交易移除一筆舊角色 ID 4，查回為 0。回復時須先確認 ID 與角色名稱無衝突。API 1.0.263 → 1.0.264，Web 1.0.288 → 1.0.289，來源 be46d38，回復點 PORTAL-PROD-1123-ALIGN-ROLE。
- Portal 正式三語、人事／生日、門禁與安衛等本批頁面對齊。API 1.0.264 → 1.0.265，Web 1.0.289 → 1.0.290，來源 a3b8a49，回復點 PORTAL-PROD-1123-ALIGN-I18N。正式 Release build、Web 焦點 8 項與 API 生日焦點 30 項通過，正式登入頁、前台版號、CSS／JS、語言資源 MIME 與瀏覽器 8443 production health 均通過。品保與藥液、訪客資料相關頁面保留各自已部署流程。
- 待辦：Portal 角色通知保留原設定。正式製程有效 Email 人數為零，分類與通知收件名單仍待指定。角色、SSO 與三語涉及的真人登入驗收未執行。測試庫刷新按鈕與執行器仍無可發布版本，未觸發刷新。測試站未重新部署。

## 2026-11-23 正式站技術複驗

- Chromium 實測 SPC 正式 Web 0.1.152、主 JS／CSS、總覽資產及瀏覽器使用的 8443 API health/version，結果為 `0.1.140/production`。以攔截回應驗證總覽排序，未重新登入操作業務資料。
- Chromium 實測 Portal 正式登入頁、app-version 1.0.290.0、主要 CSS／JS MIME 及瀏覽器 8443 API health，資料庫健康及 production 標籤正常。匿名品保 SSO launch 和錯誤紀錄查詢皆返回 401。
- 此次沒有執行品保權杖續發、管理員錯誤紀錄查閱及登入後三語驗收。通知收件人仍待指定。無新部署或業務資料異動。
- 後續真人操作從 Portal 正式品保專區開啟 SPC。確認正式 SPC `/spc` 顯示已登入編輯者、Web 0.1.152、API 0.1.140。正式庫查回同帳號近期 `auth.sso.exchange` 為 `Succeeded`、`PortalSso`、HTTP 200。正向進站通過；刻意使權杖過期再續發的情境未執行。
