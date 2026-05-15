# 資料庫 Schema（MES + SPC MVP）

資料庫：SQLite（檔名預設 `mes-spc.db`，可由 `ConnectionStrings:Default` 調整）。  
ORM：Entity Framework Core，資料表名稱為 EF 預設複數英文（與 DbSet 一致）。

---

## 列舉值

| 列舉 | 值 | 說明 |
|------|-----|------|
| **DataType** | 1 Numeric, 2 Text, 3 Boolean | 檢測項目資料型別 |
| **SourceType** | 1 Manual, 2 Csv | 量測批次來源 |
| **AlertType** | 1 OutOfSpec, 2 OutOfControl | 異常類型（規格／管制） |

---

## Products（產品）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| ProductCode | string | 產品代碼，**唯一索引** |
| ProductName | string | 產品名稱 |
| IsActive | bool | 是否啟用 |
| CreatedAt | datetime | 建立時間（UTC） |

---

## Stations（工站）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| StationCode | string | 工站代碼，**唯一索引** |
| StationName | string | 工站名稱 |
| IsActive | bool | 是否啟用 |
| CreatedAt | datetime | 建立時間（UTC） |

---

## InspectionItems（檢測項目）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| ItemCode | string | 項目代碼，**唯一索引** |
| ItemName | string | 項目名稱 |
| DataType | int | 見 DataType 列舉 |
| Unit | string? | 單位 |
| Usl | double? | 上規（USL） |
| Lsl | double? | 下規（LSL） |
| Ucl | double? | 上管制（UCL） |
| Lcl | double? | 下管制（LCL） |
| TargetValue | double? | 目標值 |
| IsSpcEnabled | bool | 是否啟用 SPC（語意標記） |
| CreatedAt | datetime | 建立時間（UTC） |

---

## ProductStationItems（產品–工站–檢測項目）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| ProductId | int FK → Products | 產品 |
| StationId | int FK → Stations | 工站 |
| InspectionItemId | int FK → InspectionItems | 檢測項目 |
| SampleSize | int | 子組樣本數（X̄-R 等用，預設 1） |
| IsActive | bool | 是否啟用 |

**唯一索引**：`(ProductId, StationId, InspectionItemId)`。

---

## MeasurementBatches（量測批次）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| BatchNo | string | 批號 |
| ProductId | int | 產品 |
| StationId | int | 工站 |
| MeasuredAt | datetime | 量測時間 |
| OperatorName | string? | 操作者（文字） |
| SourceType | int | 見 SourceType 列舉 |
| CreatedAt | datetime | 建立時間（UTC） |

一對多關聯：`MeasurementValues`（Cascade 刪除）。

---

## MeasurementValues（量測明細）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| BatchId | int FK → MeasurementBatches | 批次 |
| InspectionItemId | int | 檢測項目 |
| SampleNo | int | 樣本序號 |
| ValueNumeric | double? | 數值 |
| ValueText | string? | 文字 |
| ValueBool | bool? | 布林 |
| CreatedAt | datetime | 建立時間（UTC） |

---

## FormulaDefinitions（公式定義）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| FormulaCode | string | 公式代碼（如 AVG、CPK），**唯一索引** |
| DisplayName | string | 顯示名稱 |
| Expression | string | 運算式文字（存 DB，供引擎／擴充） |
| IsBuiltIn | bool | 是否內建 |
| IsActive | bool | 是否啟用 |

---

## AlertEvents（異常事件）

| 欄位 | 型別 | 說明 |
|------|------|------|
| Id | int PK | 主鍵 |
| OccurredAt | datetime | 發生時間 |
| ProductId | int | 產品 |
| StationId | int | 工站 |
| InspectionItemId | int | 檢測項目 |
| ActualValue | double? | 實測值 |
| AlertType | int | 見 AlertType 列舉 |
| Message | string | 訊息 |
| BatchId | int? | 關聯批次 |
| MeasurementValueId | int? | 關聯明細 |
| IsAcknowledged | bool | 是否已確認 |

---

## 關聯與索引摘要

- `ProductStationItems`：產品＋工站＋檢測項目三鍵唯一。
- `MeasurementBatch` → `MeasurementValue`：一對多，Cascade。
- 其餘 FK 可由應用層維護（MVP 未全部設定導航屬性 FK 限制）。
