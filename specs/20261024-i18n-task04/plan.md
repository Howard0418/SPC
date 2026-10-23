# 技術計畫
- I18N-TASK-04 v1；[規格](spec.md)
## 受影響檔案與方式
SPC frontend：安裝並鎖定 Vue I18n 11；src/i18n 提供三語 catalog、精確 allowlist 與 application/environment/subject scoped localStorage adapter；main.js 註冊，App.vue 使用 stable key 即時渲染。
router-view 保持原 path key。只改標籤，不重建業務元件，不翻譯 user/role 值、版本號與路徑。語言選單標明本機，原路由權限不變。header 可換行，長選單文字換行。
## 測試
核心偏好與字典規則先建 Node 測試，再實作。測 Happy Path、三語/fallback/identity/storage 邊界、非法 locale/參數注入、Viewer/Editor/表單查詢保留回歸。Playwright 全 API 攔截，禁止業務寫入；12 個語系尺寸組合。npm run build:test。
## 發布、回復
測試通過直接更新 D:/SPC/release/test/frontend，先核對 IIS SpcSingleTest、AppEnvironment=test，備份並比對 hash、保護 web.config。只升 Web 0.1.72，API 沿用 0.1.72，無 DB 影響。不操作正式或 Portal。
回復整份前端備份並保留環境設定；本機偏好 key 可留存，舊版忽略。完成後提交/上傳 Develop，停止於此任務。
