# MES + SPC 開發順序（全集）

本專案第一版目標為**可用 MVP**（品質量測 + SPC + 異常），完整 MES 留待後續。下列順序兼顧**依賴關係**與**可驗收里程碑**；**粗體**為建議專案級門檻。

---

## 階段 0 — 專案初始化

1. 建立 **Backend**：ASP.NET Core Web API、資料夾結構（Controllers / Services / Domain / Infrastructure）。
2. 建立 **Frontend**：Vue 3 + Vite + Tailwind + ECharts、Router、API client。
3. 設定 **EF Core**（SQLite 先行）、`DbContext`、連線字串。
4. 設定 **CORS**、開發用 **Vite Proxy**（`/api` → 後端），避免本機 HTTPS 轉址與跨埠問題。
5. **EF Core Migrations**（已採用）：`InitialCreate` + 啟動時 `Database.Migrate()`；細節見 [database.md](./database.md)。

**驗收**：前後端可啟動、`/api/v1` 可從瀏覽器經 Proxy 存取。

---

## 階段 1 — 基礎主檔（MES 最小子集）

1. **Product** 實體 + CRUD API + 前端頁。
2. **Station** 實體 + CRUD API + 前端頁。
3. **InspectionItem** 實體（含 ItemCode、DataType、Unit、USL/LSL、UCL/LCL、Target、IsSpcEnabled）+ CRUD + 前端頁。
4. **ProductStationItem**（產品–工站–檢測項目–SampleSize）+ API + 種子或簡易畫面。

**驗收**：可維護產品／工站／檢測項目與綁定關係。

---

## 階段 2 — 量測資料

1. **MeasurementBatch** + **MeasurementValue** 實體與關聯（一批多筆）。
2. **POST 建立批次** API（手動輸入 body）+ 前端表單。
3. **CSV 匯入** API（檔案上傳解析）+ 前端上傳頁。
4. 定 CSV 欄位規格與範例檔。

**驗收**：同一產品／工站可重複建批次、寫入多檢測項目與多樣本序號。

---

## 階段 3 — 公式引擎（可擴充）

1. **FormulaDefinition** 表與種子（AVG、STDEV、RANGE、MAX、MIN、COUNT、SUM、CP、CPK）。
2. **FormulaEngineService**：依 `FormulaCode`（或由 DB Expression 對應）計算；CP/Cpk 需 USL/LSL。
3. **POST /formulas/evaluate** 從 DB 讀公式再計算（不寫死於程式）。

**驗收**：可透過 API 帶入數值陣列與規格，回傳計算結果；公式可於 DB 調整 Expression（內建代碼對應邏輯可先固定、日後換通用解析器）。

---

## 階段 4 — SPC 與異常（核心閉環）

1. **儲存／讀取**檢測項目界限（與量測數值比對）。
2. 量測寫入後觸發：**USL/LSL**（OutOfSpec）、**UCL/LCL**（OutOfControl）。
3. **AlertEvent** 寫入（時間、產品、工站、檢測項目、實測值、類型、訊息、批次／明細關聯）。
4. **GET /spc/chart**：I-MR、X̄-R 所需資料結構；**MVP** 可就 X̄ 與主檔界限簡化比對，後續再改為 A2/D3/D4 等標準管制線。
5. 前端 **SPC 頁**：ECharts、界限線（USL/LSL/UCL/LCL/Target）、異常點標示。

**驗收**：輸入超限資料後，**異常清單**與 **Dashboard** 可見；管制圖可顯示界限與紅點。

---

## 階段 5 — Dashboard 與操作體驗

1. **GET /dashboard/summary**（今日批次、異常、比例、最近異常）。
2. **GET /alerts** + **POST …/ack**。
3. 前端 **Dashboard**、**異常清單**；API 失敗時友善錯誤（連線、後端未啟）。
4. README：啟動步驟、**必須使用 `http://localhost:5173`**、先啟後端、種子資料與 Demo CSV 路徑。

**驗收**：非技術使用者可依 README 完成一輪 Demo。

---

## 階段 6 — 整體驗收與文件

1. 匯出 **Schema**、**API 清單**、**開發順序**（本檔與 `docs/schema.md`、`docs/api.md`）。
2. 走通 **E2E Demo**：主檔 → 量測／CSV → SPC → 異常 → Dashboard。
3. （選擇性）**SQL Server** 連線切換、**建置／部署**說明。

---

## 後續擴充（非 MVP，但建議排序）

| 順序 | 項目 | 狀態 |
|------|------|------|
| 1 | EF **Migrations**、環境設定分離 | Migrations 已採用 |
| 2 | X̄-R **A2/D3/D4 統計管制線**（n=2～10） | **已實作**（`SpcConstants` + API／圖表） |
| 3 | I-MR／MR **MR 圖管制線**（移動全距常數） | 待定 |
| 4 | 主檔／量測 **編輯刪除 UX**、表單驗證 | 待定 |
| 5 | **簡易 JWT 登入**（可選 `Auth:Enabled`） | **已實作**（帳密設定檔） |
| 6 | **角色／權限**、API Key | 待定 |
| 7 | **SPC 規則擴充**（Western Electric 等） | 待定 |
| 8 | 審計、報表、**MES 模組** | 待定 |

---

## 本 repo 目前狀態（對照）

階段 0～6 **MVP 主線**已完成，並已加上 **X̄-R 統計管制線** 與 **可選 JWT**；其餘見上表「待定」。
