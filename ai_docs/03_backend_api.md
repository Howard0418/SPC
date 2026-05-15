# 03 Backend API

## 目前狀態 (Current Status)
標準 ASP.NET Core Web API，整合 Swagger。

## 已完成內容 (Completed Items)
- **Controllers**: CRUD APIs for all entities.
- **Special APIs**: `/api/Spc/Chart` (計算圖表資料), `/api/Upload/Variable` (匯入).
- **Security**: JWT Bearer token authentication.

## 待補強項目 (Pending Items)
- **Pagination**: 實作統一的分頁、搜尋與排序 (QueryBuilder)。
- **Excel Export**: 補齊主檔匯出 Excel 功能。
- **SignalR**: 異常警報即時通知。
- **Validators**: 引入 `FluentValidation` 強化輸入驗證。

## 注意事項 (Notes)
- 確保 API 回傳格式統一 (`ApiResponse<T>`)。

## 後續開發建議 (Development Roadmap)
- 建立 API 版本的概念 (如 `/api/v1/...`)。
- 實作 API 權限控制 (Role-based / Policy-based)。
