# 05 SPC Engine Design

## 目前狀態 (Current Status)
整合在 `SpcService` 中，提供基礎計算。

## 已完成內容 (Completed Items)
- 基礎統計計算 (Mean, StdDev, Range).
- Xbar-R, Xbar-S 圖表資料產出.
- 基礎規格界限 (USL/LSL) 判定.

## 待補強項目 (Pending Items)
- **管制圖**: P, NP, C, U, EWMA, CUSUM.
- **製程能力**: Cp, Cpk, Pp, Ppk, Sigma Level.
- **常態性檢定**: Anderson-Darling, Shapiro-Wilk.
- **規則引擎**: 完整的 Nelson Rules (8 條) 與自定義規則。

## 注意事項 (Notes)
- 計算邏輯應脫離資料庫相依性，使其可單元測試。

## 後續開發建議 (Development Roadmap)
- 建立 `SpcEngine` 獨立模組。
- 實作 `RuleValidator` 鏈式校驗。
