# 05 SPC Engine Design

## 目前狀態 (Current Status)
已將核心運算邏輯從 `SpcService` 抽離，建立獨立的 `SpcEngine` 模組，解除對 Entity Framework 的相依性，使其可進行單元測試。

## 已完成內容 (Completed Items)
- 基礎統計計算 (Mean, StdDev, Range).
- Xbar-R, I-MR 圖表資料產出 (已抽離為 `Calculators`).
- 基礎規格界限 (USL/LSL) 判定.
- 建立 `SpcEngine` 獨立模組 (`Models`, `Calculators`, `Rules`).
- 實作 `NelsonRulesValidator` (支援 Rule 1, 2, 3, 4, 5, 6 等標準規則)。

## 待補強項目 (Pending Items)
- **管制圖擴充**: P, NP, C, U, EWMA, CUSUM.
- **製程能力**: Cp, Cpk, Pp, Ppk, Sigma Level.
- **常態性檢定**: Anderson-Darling, Shapiro-Wilk.
- **規則擴充**: 支援讀取資料庫動態設定的自定義規則。

## 注意事項 (Notes)
- `SpcEngine` 內部不可直接呼叫 `AppDbContext`。
- 所有傳入的資料必須先轉換為 `SpcDataPoint` 或 `Subgroup` 模型。

## 後續開發建議 (Development Roadmap)
- 為 `ImrChartCalculator` 與 `XbarRChartCalculator` 補齊單元測試 (Unit Tests)。
- 實作 Attribute (計數型) 管制圖的 Calculator。
