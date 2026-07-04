# 04 Frontend Structure

## 目前狀態 (Current Status)
Vue 3 + Vite + TailwindCSS. 單頁應用 (SPA)。

## 已完成內容 (Completed Items)
- **Directory**: `src/views` 包含所有功能頁面.
- **Charts**: 整合 ECharts 顯示 SPC 管制圖。
- **UI Framework**: 使用 TailwindCSS 進行排版。
- **Routing**: `src/router/index.js` 定義所有路由。
- **角色導覽與保護**：
  - 登入後將 Token 與最小化使用者資訊儲存在瀏覽器本機儲存空間。
  - Viewer 僅顯示戰情、圖表、警報與追溯查詢選單。
  - 主檔、匯入、異常簽核、SMTP 與使用者管理路由標記為 `editorOnly`。
  - 頁首顯示登入者名稱及「檢視者／編輯者」角色。

## 待補強項目 (Pending Items)
- **Shared Components**: 提取通用組件 (DataTable, Modal, FormFields)。
- **State Management**: 考慮引入 Pinia 管理全域狀態 (User, Permission, Config)。
- **Theme**: 支援 Dark Mode 與企業主配色。
- **I18n**: 多語言支援 (繁中、簡中、英文)。

## 注意事項 (Notes)
- 保持頁面響應式設計 (Mobile Friendly)。

## 後續開發建議 (Development Roadmap)
- 封裝 `BaseChart` 組件處理 ECharts resize 與更新。
- 引入 `Iconify` 統一代碼圖示。
