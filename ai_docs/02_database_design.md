# 02 Database Design

## 核心架構 (Core Architecture)

所有實體現在均繼承自 `BaseEntity`，提供統一的稽核與併發控制：
- **Id**: 主鍵 (int/long/Guid)
- **CreatedAt / CreatedBy**: 建立資訊
- **UpdatedAt / UpdatedBy**: 修改資訊
- **IsDeleted**: 軟刪除支援 (已套用 Global Query Filter)
- **RowVersion**: 樂觀併發控制 ([Timestamp])

## 目前狀態 (Current Status)

支持 SQLite 與 SQL Server。目前 schema 包含約 40 張資料表，涵蓋以下模組：

- **組織架構 (Org Structure)**: Plant, Factory, ProductionLine, Unit.
- **主檔數據 (Master Data)**: Products, Parts, Processes, Machines, Stations, QualityCharacteristics, Shift, Operator, Customer, Supplier.
- **設定配置 (Configuration)**: PartProcessCharacteristic (規格與管制界限), ControlChartTypes, SpcRules.
- **交易數據 (Transactions)**: MeasurementBatches, MeasurementValues, VariableMeasurements, AttributeMeasurements.
- **運算邏輯 (Logic)**: FormulaDefinitions, SpcCalculationResults.
- **流程控制 (Workflow)**: AlertEvents, WorkOrders, StationOperationSessions, UploadBatches.

## 核心設計原則

1. **軟刪除**: `IsDeleted` 為 true 的資料不會被查詢出，除非明確使用 `.IgnoreQueryFilters()`。
2. **自動稽核**: `AppDbContext` 已覆寫 `SaveChangesAsync`，自動填充建立與修改者資訊。
3. **資料一致性**: 關鍵表均設有 Unique Index (例如 Code/No 欄位)。
4. **效能優化**: 已針對 `MeasuredAt` 與關聯 ID 建立複合索引。

## 後續優化建議 (Development Roadmap)

- **SQL Server 腳本**: 補齊正式環境部署用的備份與恢復自動化腳本。
- **資料清理**: 實作歷史數據封存邏輯 (Archiving Policy)。
