# 00 Project Overview

## 目前狀態 (Current Status)
本專案為初版 SPC (Statistical Process Control) 系統，已具備基礎的產品、製程、機台主檔維護，支援 CSV 資料匯入與基礎管制圖顯示。

## 已完成內容 (Completed Items)
- 基礎主檔 CRUD (Product, Part, Process, Machine, Station).
- 資料收集 (手動輸入與 CSV 匯入).
- 基礎 SPC 管制圖 (Xbar-R).
- 基礎 Dashboard 看板.
- JWT 身份驗證 (可配置).

## 待補強項目 (Pending Items)
- 企業級組織架構 (Plant, Factory, Production Line).
- 完整的審核軌跡 (Audit Trail).
- 併發控制 (Optimistic Concurrency).
- 軟刪除 (Soft Delete).
- 完整的 SPC 規則引擎 (Nelson Rules).
- CAPA / OCAP 異常處理工作流.

## 注意事項 (Notes)
- 系統正從 SQLite 遷移至 SQL Server。
- 需保持與舊有資料結構的相容性。

## 後續開發建議 (Development Roadmap)
1. 強化底層 Data Persistence 層，補齊 Audit 欄位。
2. 擴充組織架構主檔。
3. 抽離並強化 SpcEngine。
