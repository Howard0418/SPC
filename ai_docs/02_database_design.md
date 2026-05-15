# 02 Database Design

## 目前狀態 (Current Status)
支持 SQLite 與 SQL Server。目前 schema 包含約 30 張資料表。

## 已完成內容 (Completed Items)
- **Master Data**: Products, Parts, Processes, Machines, Stations, QualityCharacteristics.
- **Config**: PartProcessCharacteristic, ControlChartTypes, SpcRules.
- **Transaction**: MeasurementBatches, MeasurementValues, VariableMeasurements, AttributeMeasurements.
- **Logic**: FormulaDefinitions, SpcCalculationResults.
- **Workflow**: AlertEvents, WorkOrders, StationOperationSessions.

## 待補強項目 (Pending Items)
- **組織架構**: Plant, Factory, ProductionLine.
- **基礎主檔**: Unit, Shift, Operator, Customer, Supplier.
- **Audit Fields**: Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, RowVersion (Timestamp).
- **Index 優化**: 針對查詢頻繁的欄位建立索引。

## 注意事項 (Notes)
- SQL Server 需確保 `RowVersion` 欄位正確對應 `byte[]`。
- 遷移 (Migration) 需同步更新。

## 後續開發建議 (Development Roadmap)
- 建立 `BaseEntity` 抽象類別。
- 實作 `DbContext.SaveChangesAsync` 自動填充 Audit 欄位。
