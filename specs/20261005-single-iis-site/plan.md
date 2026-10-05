# 技術計畫
- 功能 ID：20261005-single-iis-site
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
建議目標：

```text
IIS Single Site: https://目標網址/
PhysicalPath: D:\SPC\release\test\backend

ASP.NET Core
├─ /api/*  -> controllers
├─ /       -> sibling frontend 靜態檔
└─ fallback -> frontend index.html

Sibling:
D:\SPC\release\test\frontend
```

預計受影響檔案：
- `frontend/mes-spc-web/src/api/client.js`
- `frontend/mes-spc-web/.env.testhost`
- `frontend/mes-spc-web/.env.production`
- `frontend/mes-spc-web/.env.example`
- `frontend/mes-spc-web/src/views/SpcQueryView.vue`
- 視驗證結果可能調整 `scripts/publish-environments.ps1` 或新增測試部署腳本。

不預計修改：
- API controller route。
- AD/Portal SSO/JWT 建立邏輯。
- SQL Server migration 或資料表。

## 實作方式與需求對應
1. 讓 API base 預設為 `/api`，保留開發環境 proxy 能力。（R-002）
2. 修正會手動組 URL 的匯出功能，避免 `/api/api`。（R-003）
3. 保留 `Program.cs` 現有 static file/fallback 模式，先以打包目錄驗證 sibling `frontend` 可用。（R-001、R-004）
4. IIS 測試站新增或調整單一 HTTPS Site 指向 backend 目錄，不使用 `/api` child Application。（R-001、R-003、R-007）
5. CORS 第一輪不收斂，以降低行為變更；單站 smoke 通過後再評估是否移除寬鬆 CORS。（R-005）

## API、檔案格式及資料模型
- API 外部路徑維持既有 `/api/*`。
- 無新 API。
- 無資料模型或 schema 變更。
- 前端建置產物仍輸出至 `frontend/mes-spc-web/dist`，發布至 `release/test/frontend`。

## 相容性與風險
- 風險最高：IIS `/api` Application 造成 `/api/api`。本計畫避免此做法。
- HTTPS mixed content：需清除前端 SPC API 絕對 `http://IP:Port`。
- Portal SSO：若 Portal 仍回跳舊 SPC URL，使用者仍可能被導到舊站，需 Portal 設定配合。
- `UseHttpsRedirection` 在反向代理或非 443 binding 下可能需要確認 `X-Forwarded-Proto`；本次若 IIS 直接終止 HTTPS 且 ASP.NET Core in-process，風險較低。
- `Program.cs` 目前有多段啟動自我修復 SQL；部署前需確認只接測試庫，避免誤連正式庫。

## 驗證安排
必要驗證：
- `npm run build:test`
- 後端 build 或 publish。
- 打包後搜尋前端資產是否仍含 SPC API `http://172.16...:8081/api` 或 `:8082/api`。
- `GET https://目標網址/api/version`
- 未登入呼叫受保護 API 預期 401。
- Viewer 寫入 API 預期 403。
- Portal SSO 登入後可進入 `/spc`。
- 重新整理 Vue 子頁面：`/spc`、`/calibration-instruments`、`/particle-monitoring`。
- 匯出 Cpk summary 不產生 `/api/api`。
- 瀏覽器 Network 無 mixed content。
- API `AppEnvironment` 與 Web `VITE_APP_ENV` 一致。

## 發布、備份及回復
發布僅限測試站，且需先確認：
- IIS 目標 Site、Physical Path、App Pool、binding。
- `appsettings.json` 指向 PMR_SPC_TEST。
- 不指向 `D:\Sites\PmrPortal`。

發布前備份：
- `D:\SPC\release\test\backend`
- `D:\SPC\release\test\frontend`
- IIS Site/App Pool/binding 設定截圖或匯出。
- `appsettings*.json`、`web.config`、`private-data`、`uploads`、`logs` 不覆寫或另備份。

Rollback：
- 將 IIS binding/站台指回原 `SpcWeb :8083` 與 `SpcApi :8081` 分站台。
- 還原 backend/frontend 備份。
- 還原原本 `.env` build 產物或上一版 dist。
- 確認 `http://172.16.110.27:8083/` 與 `http://172.16.110.27:8081/api/version` 回復。
