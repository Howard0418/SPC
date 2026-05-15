# 06 Formula Engine Design

## 目前狀態 (Current Status)
已將 `FormulaEngineService` 升級為使用 `NCalc` 運算引擎，支援動態表達式解析。

## 已完成內容 (Completed Items)
- 基礎公式儲存。
- 內建公式與自定義公式區分。
- **動態解析**: 使用 `NCalc` 實作動態運算，支援複雜數學函數與變數綁定。
- **內建變數**: 支援 `AVG`, `STDEV`, `MAX`, `MIN`, `RANGE`, `SUM`, `COUNT`, `USL`, `LSL` 等保留字自動綁定。

## 待補強項目 (Pending Items)
- **版本化**: 保存公式歷史版本。
- **綁定機制**: 綁定產品、製程、特性與管制圖類型。
- **預覽功能**: 提供測試界面輸入值並預覽結果。
