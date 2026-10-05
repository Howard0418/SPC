# 技術計畫
- 功能 ID：20260910-manual-production-package
- 規格版本：1
- 規格：[spec.md](spec.md)

## 實作方式
- 以 `dotnet publish -c Release --no-restore` 產生 SPC API、Portal API、Portal Web。
- 以 `npm run build:production` 產生 SPC Web。
- 打包腳本移除設定與持久化內容，再建立 SHA-256 `manifest.json` 和根目錄部署 README。

## 驗證與發布
- 確認四個包、每包 manifest、根 README、檔案雜湊與排除清單。
- 本次交付即「建立本機發布包」；正式 IIS 發布不適用，後續由使用者手動完成。