# 技術計畫
- 功能 ID：20260910-chameleon-source-settings
- 規格版本：1
- 規格：[spec.md](spec.md)

## 實作方式
- 新增 `ChameleonSourceSetting` EF 實體與 migration；保存 SourceId、顯示名稱、BaseUrl、Enabled 與排序。
- `ChameleonStatusService` 優先讀取已設定資料庫來源，沒有時回退 appsettings，並提供快取清除方法。
- 新增 Editor-only API，前端新增 `/settings/chameleon` 管理頁與選單入口。

## 驗證與發布
- 建置 SPC API/Web，新增正規化與設定 API 測試；套用 migration 僅限測試資料庫。
- 發布測試 SpcApi/SpcWeb，正式發布不適用，除非另行授權。