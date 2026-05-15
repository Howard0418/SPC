# API 清單（REST，`/api/v1`）

基底路徑範例：開發時前端 Proxy 為同網址 `/api/v1`；直接呼叫後端為 `http://localhost:5243/api/v1`。

當後端 **`Auth:Enabled`** 為 **true** 時，除登入外之 API 須帶表頭：`Authorization: Bearer <token>`。

---

## 認證 Auth（選用）

| 方法 | 路徑 | 說明 |
|------|------|------|
| POST | `/api/v1/auth/login` | Body：`{ "username", "password" }`；成功回傳 `token`、`expiresAt`。帳密見 `appsettings` 之 `Auth:DemoUsername` / `Auth:DemoPassword`。 |

---

## 產品 Products

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/products` | 列表 |
| POST | `/api/v1/products` | 新增（Body: Product） |
| PUT | `/api/v1/products/{id}` | 更新 |
| DELETE | `/api/v1/products/{id}` | 刪除 |

---

## 工站 Stations

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/stations` | 列表 |
| POST | `/api/v1/stations` | 新增 |
| PUT | `/api/v1/stations/{id}` | 更新 |
| DELETE | `/api/v1/stations/{id}` | 刪除 |

---

## 檢測項目 InspectionItems

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/inspection-items` | 列表 |
| POST | `/api/v1/inspection-items` | 新增 |
| PUT | `/api/v1/inspection-items/{id}` | 更新 |
| DELETE | `/api/v1/inspection-items/{id}` | 刪除 |

---

## 產品工站檢測項目 ProductStationItems

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/product-station-items?productId=&stationId=` | 列表（查詢可選） |
| POST | `/api/v1/product-station-items` | 新增 |
| PUT | `/api/v1/product-station-items/{id}` | 更新 |
| DELETE | `/api/v1/product-station-items/{id}` | 刪除 |

---

## 量測 MeasurementBatches

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/measurement-batches` | 最近批次列表（含 Values） |
| GET | `/api/v1/measurement-batches/{id}` | 單筆批次 |
| POST | `/api/v1/measurement-batches` | 建立批次＋多筆明細；寫入後觸發 SPC 判定與 Alert |

---

## CSV 匯入

| 方法 | 路徑 | 說明 |
|------|------|------|
| POST | `/api/v1/measurements/import-csv?productId=&stationId=&measuredAt=` | `multipart/form-data` 欄位 `file`；CSV 欄位見 README |

---

## 公式 Formulas

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/formulas` | 公式定義列表 |
| PUT | `/api/v1/formulas/{id}` | 更新（DisplayName、Expression、IsActive） |
| POST | `/api/v1/formulas/evaluate` | Body：`FormulaCode`、`Values[]`、`Usl`、`Lsl`；依 DB 公式計算 |

---

## SPC

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/spc/chart?inspectionItemId=&productId=&stationId=&chartType=` | 見下段 **SPC 回傳說明**；檢測項目不存在為 **404** |

**SPC 回傳（摘要）**：`limits` 為主檔規格／欄位界限；**X̄-R** 另含 **`xbarControlLimits` / `rControlLimits`**（A2、D3、D4 與 \(\bar R\)、雙重平均 \(\overline{\bar x}\)）；**`subgroupSize`** 為子組 n；不一致時可能含 **`subgroupSizeNote`**。

---

## 異常 Alerts

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/alerts?isAcknowledged=&from=&to=` | 異常列表 |
| POST | `/api/v1/alerts/{id}/ack` | 確認（Ack） |

---

## Dashboard

| 方法 | 路徑 | 說明 |
|------|------|------|
| GET | `/api/v1/dashboard/summary` | 今日批次數、今日異常數、異常率、最近異常 |

---

## 開發用（OpenAPI）

Development 環境可透過 ASP.NET Core OpenAPI 產生文件（專案已註冊 OpenAPI）。
