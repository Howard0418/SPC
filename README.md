# MES + SPC MVP

第一版目標是「可用」的品質量測與 SPC 管制系統，採最小可行範圍。

## 技術

- Frontend: Vue 3 + Vite + Tailwind + ECharts
- Backend: ASP.NET Core Web API
- Database: SQLite (可平滑切換 SQL Server)
- ORM: Entity Framework Core

## 啟動

### 1) Backend

```powershell
dotnet run --project .\backend\MesSpc.Api\MesSpc.Api.csproj
```

預設 API 位址：`http://localhost:5243`

### 2) Frontend

```powershell
cd .\frontend\mes-spc-web
npm install
npm run dev
```

開發時前端會透過 Vite **Proxy** 呼叫 `http://[IP_ADDRESS]`（請務必先啟後端）。瀏覽器請開 **`http://[IP_ADDRESS]`**，勿用本機雙擊開啟 `index.html`。

若修改過 `vite.config.js`，請重新執行 `npm run dev`。

### 常見錯誤

- 瀏覽器或 Vite 終端出現 **`ECONNREFUSED`**、**`http proxy error`**、Dashboard 紅字「無法連線到後端」：代表 **API 尚未啟動**。請先開一個終端執行 `dotnet run --project .\backend\MesSpc.Api\MesSpc.Api.csproj`，再重新整理前端。
- 在錯誤目錄執行 `npm run dev`（例如專案根目錄沒有 `package.json`）會出現 **ENOENT**：請 **`cd frontend\mes-spc-web`** 後再執行。
- 修改 **`.env`**（例如 `VITE_AUTH_ENABLED`）後，請 **重啟** `npm run dev` 才會生效。

## CSV 匯入格式

欄位：

- `InspectionItemId`
- `SampleNo`
- `ValueNumeric`

範例檔案：`sample-data/measurement-sample.csv`

## 資料庫

- 預設改為 **SQL Server**（`DatabaseProvider: SqlServer`），啟動 API 後會自動建庫建表並種子。
- 若要切回 SQLite，將 `DatabaseProvider` 設為 `Sqlite`（細節見 [資料庫說明](./docs/database.md)）。
- 手動遷移 / `dotnet-ef` 指令同樣整理於 [資料庫說明](./docs/database.md)。

## 登入（選用）

預設 **`Auth:Enabled`** 為 **false**，API 不需 JWT。

若要啟用：

1. `backend/MesSpc.Api/appsettings.json`（或環境設定）設 **`Auth:Enabled": true`**，並設定 **`Auth:JwtKey`**（至少 32 字元）。
2. 前端 **`frontend/mes-spc-web/.env`** 設 **`VITE_AUTH_ENABLED=true`**（須重跑 `npm run dev`）。
3. 使用 `POST /api/v1/auth/login`（帳密同 `Auth:DemoUsername` / `DemoPassword`）取得 token；前端會自動附在 `Authorization` 標頭。

## 文件

- [資料庫建立／遷移](./docs/database.md)
- [資料庫 Schema](./docs/schema.md)
- [API 清單](./docs/api.md)
- [MES + SPC 開發順序](./docs/development-order.md)

## 內建種子資料

首次啟動後端會自動建立：

- Products: `P-1001`, `P-1002`
- Stations: `ST-01`, `ST-02`
- InspectionItems: `LEN-001`, `WID-001`
- ProductStationItems: 預設將第一個產品 + 第一個工站綁定上述檢測項目
