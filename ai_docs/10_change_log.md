# 10 Change Log

## [2026-05-15] - Formula Engine & Attribute Charts (P2)
- **Formula Engine 重構**: 將 `FormulaEngineService` 替換為 `NCalcSync` 函式庫，全面支援動態字串表達式解析。
- **內建變數綁定**: 實作動態綁定 `AVG`, `STDEV`, `MAX`, `MIN`, `RANGE`, `SUM`, `COUNT`, `USL`, `LSL` 等保留字，供表達式直接取用。
- **Attribute 管制圖**: 新增 `AttributeChartCalculator` 模組，完整支援計數型 P, NP, C, U 管制圖的界限計算。
- **單元測試**: 為 `FormulaEngineService` 與 `AttributeChartCalculator` 加入 xUnit 測試並全數通過。

## [2026-05-15] - SPC Unit Testing (P4)
- **測試防護網**: 建立 `MesSpc.Api.Tests` xUnit 測試專案。
- **單元測試**: 為 `ImrChartCalculator`, `XbarRChartCalculator` 與 `NelsonRulesValidator` 撰寫完整的覆蓋測試，涵蓋極端案例與常規計算。
- **配置優化**: 透過 `<InternalsVisibleTo>` 暴露內部模型供動態斷言，確保 API 設計乾淨。

## [2026-05-15] - SPC Engine Refactoring (P2)
- **架構解耦**: 將 SPC 核心運算從 `SpcService` 抽離至獨立的 `SpcEngine` 模組，解除 Entity Framework 相依性。
- **領域模型**: 建立 `SpcDataPoint` 與 `Subgroup` 模型，作為運算引擎的標準傳遞物件。
- **計算器模組**: 實作 `ImrChartCalculator` 與 `XbarRChartCalculator`。
- **規則引擎**: 實作 `NelsonRulesValidator`，支援 Nelson Rule 1 至 Rule 6 的自動判定，並將結果附加至回傳資料點。

## [2026-05-15] - Core Architecture Refactoring
- **核心架構重構**: 引入 `BaseEntity` 抽象類別，統一主鍵名為 `Id`。
- **自動稽核系統**: 實作 `AppDbContext` 自動填充 `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`。
- **資料安全與稽核**: 實作軟刪除 (`IsDeleted`) 與樂觀併發控制 (`RowVersion`)。
- **組織架構擴充**: 新增 `Plant`, `Factory`, `ProductionLine`, `Unit`, `Shift`, `Operator` 等主檔。
- **企業化資料庫**: 完成 SQL Server 遷移腳本 `V4_EnterpriseMasterDataAndAudit` 並成功應用。
- **文件化**: 更新 `ai_docs` 描述最新架構。

## [2026-05-14]
- 執行第一階段：專案盤點與現況分析。
- 執行第二階段：建立 `ai_docs` 基礎文件庫。
- 產出改造優先順序報告。
