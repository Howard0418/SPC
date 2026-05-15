# 01 Current Architecture

## 目前狀態 (Current Status)
採用前後端分離架構。

## 已完成內容 (Completed Items)
- **Frontend**: Vue 3 + Vite + TailwindCSS. 使用 Vue Router 進行導覽。
- **Backend**: .NET 10.0 Web API. 採用分層架構 (Controller -> Service -> Data).
- **ORM**: Entity Framework Core.
- **Calculations**: 使用 `MathNet.Numerics` 進行統計計算。

## 待補強項目 (Pending Items)
- 統一的 `BaseEntity` 與 `BaseService`。
- 獨立的 `SpcEngine` 領域模型。
- SignalR 即時推播。
- 分離的 Test Project。

## 注意事項 (Notes)
- 需注意 .NET 10 的新特性相容性。
- 前端 ECharts 組件需封裝以提高複用性。

## 後續開發建議 (Development Roadmap)
- 引入 Repository Pattern (選配) 或強化 Service 層。
- 實作全域異常處理 (Global Exception Handling)。
