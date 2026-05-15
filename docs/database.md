# 資料庫建立與遷移

- **可切換引擎**：`DatabaseProvider` = `SqlServer` 或 `Sqlite`。
- **預設**：`SqlServer`（`appsettings.json` / `appsettings.Development.json`）。
- **啟動時**：
  - `Sqlite`：`Database.Migrate()` + Seed
  - `SqlServer`：`Database.EnsureCreated()` + Seed（目前專案 migration 為 SQLite 產生，故 SQL Server 採 EnsureCreated）

## SQL Server（目前預設）

請先確認 SQL Server 可連線，再設定連線字串：

```json
"DatabaseProvider": "SqlServer",
"ConnectionStrings": {
  "SqlServer": "Server=localhost;Database=MesSpcDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
}
```

然後直接啟動 API：

```powershell
dotnet run --project .\backend\MesSpc.Api\MesSpc.Api.csproj
```

第一次啟動會自動建庫建表並寫入種子資料。

## SQLite（需要時切回）

設定：

```json
"DatabaseProvider": "Sqlite",
"ConnectionStrings": {
  "Sqlite": "Data Source=mes-spc.db"
}
```

## SQLite CLI 建庫／更新（Migrations）

於已安裝 [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) 的前提下。**請務必在後端專案目錄執行**（路徑需包含 `MesSpc.Api.csproj`）：

```powershell
cd .\backend\MesSpc.Api
dotnet ef database update
```
（請先 `cd` 到本專案根目錄，再進入上述子目錄。）

若在倉庫根目錄執行而沒指定專案，可能出現找不到專案或連錯資料庫的情況。

首次安裝工具（版本請與 `Microsoft.EntityFrameworkCore.*` 套件主版一致，本專案為 10.x）：

```powershell
dotnet tool install --global dotnet-ef --version 10.0.7
```

## 新增遷移（模型變更後，SQLite）

```powershell
cd .\backend\MesSpc.Api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

## 從舊版 `EnsureCreated` 資料庫升級（SQLite）

若先前用 `EnsureGenerated` 已建立過 **`mes-spc.db`** 且**沒有** `__EFMigrationsHistory`，第一次跑 `Migrate()` 可能與既有資料表衝突。請**先備份**後刪除舊的 `mes-spc.db`，再：

```powershell
dotnet ef database update
```

或僅刪除 DB 後重新 `dotnet run`，由 `Migrate()` 建立空白庫並種子。
