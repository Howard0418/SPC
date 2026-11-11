# 技術計畫

1. 唯讀確認兩站目前共同使用 `SpcSingleTest` 集區及來源設定。
2. 在測試主機以 IIS 原生 `appcmd` 建立唯一備份。
3. 建立 `SpcApiTest` 集區，複製已盤點的非機敏相容設定，不複製環境變數或帳密。
4. 只將 `SpcApiTest` 站台指定到新集區、啟動新集區，保留 `SpcSingleTest` 站台及其集區不變。
5. 驗證兩站狀態、HTTPS 前端與 API health/version；失敗即依規格回復。
6. 同步修正測試發布設定，使 API package 指向實際的 `SpcApiTest` site/pool；先以發布模組測試驗證。
7. 隔離通過後，重試既有 `spc-chart-auto-refresh-20261111` 測試發布 Plan／Apply，再驗收實際目標環境。

## 風險控制

- 僅測試主機及單一站台異動；不變更 binding、資料庫或正式環境。
- 使用 IIS 原生備份及前後集區指紋；不刪除舊集區。
- 若健康檢查失敗，立即回復站台集區指派，不繼續網站發布。
