# 測試／正式 SPC 功能同步

## 目的

將已在測試交付包驗證的 SPC API 功能同步至正式環境，並保留正式環境設定與回復備份。

## 差異結論

- 測試 API DLL：0.1.92；正式 API DLL：0.1.90。
- 測試前端資產含測試環境設定，不直接搬移；正式前端使用本次 production build，保留正式 8443 API 設定。
- 正式 `appsettings.json`、資料庫連線、資料保護金鑰不覆蓋。

## 驗收

- 正式 API version 回 `production` 且版本升至 0.1.92。
- 正式網站首頁與新 JS/CSS 回 200。
- `SpcWeb`、`SpcApi` App Pool 維持 Started。
- 失敗可由本次 IIS／檔案備份回復。
