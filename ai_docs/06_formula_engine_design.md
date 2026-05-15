# 06 Formula Engine Design

## 目前狀態 (Current Status)
已有 `FormulaDefinition` 資料表與 `FormulaEngineService`。

## 已完成內容 (Completed Items)
- 基礎公式儲存。
- 內建公式與自定義公式區分。

## 待補強項目 (Pending Items)
- **動態解析**: 使用 `NCalc` 或 `Roslyn` 實作動態運算。
- **版本化**: 保存公式歷史版本。
- **綁定機制**: 綁定產品、製程、特性與管制圖類型。
- **預覽功能**: 提供測試界面輸入值並預覽結果。
