# 功能規格：日期公式匯入 Portal／SPC 權限契約核對

- 功能 ID：FORMULA-IMPORT-004
- 版本：1
- 狀態：完成（不需新增 Portal 代理）
- 主系統：TransFiles
- 相依系統：Portal（登入及 SPC 權杖）、SPC API（主檔預檢及預覽）
- 授權依據：使用者於 2026-11-01 指示繼續下一項。
- 現行需求基準：[日期公式 DB 匯入環境與版本歷史](../20261029-chemical-formula-db-import-environment-history/spec.md)

## 目的與範圍

核對日期公式匯入是否需要新增 Portal API 代理，並確認測試環境的登入、權杖與 SPC API 呼叫界線。

本件只做程式契約及隔離測試核對，不更動 Portal、SPC、IIS、資料庫、權限或任何量測資料。

## 現況與結論

- TransFiles 先以選定環境的 Portal `/api/auth/login` 登入，接著呼叫 Portal `/api/spc-launch` 取得僅記憶體使用的 SPC Bearer 權杖。
- 取得權杖後，CHEM 主檔預檢與日期公式 preview／confirm 直接呼叫該環境白名單 SPC API，不經 Portal 轉送。
- `SpcCalculationApi` 僅接受測試 `https://spctest.pmr.com.tw/api` 或正式 `https://pmr-spc-srv.pmr.com.tw:8443/api`；正式仍須明確啟用。
- 因此不需要增加 Portal 代理。新增代理會改變已隔離的 API 邊界，且不屬本需求的最小修改範圍。

## 驗收需求

| 需求 ID | 規則 | 可觀察驗收 |
|---|---|---|
| R-001 | Portal 僅可用於登入與取得 SPC 權杖。 | AC-001：隔離測試先查 SPC `/version`，再登入 Portal、取得權杖。 |
| R-002 | CHEM 主檔預檢與公式資料操作必須直接呼叫所選環境的 SPC API。 | AC-002：主檔預檢請求為 `SPC /v1/import-precheck/master-data?scope=CHEM`，附 Bearer 權杖。 |
| R-003 | 不得因本次核對增加 Portal proxy 或跨環境路由。 | AC-003：程式／設定／IIS／資料庫均無變更，既有環境白名單測試通過。 |

## BDD

```gherkin
Scenario: 測試日期公式匯入取得權杖後直接驗證 SPC 主檔
  Given 使用者選擇測試環境
  And 設定包含測試 Portal 與測試 SPC API 根網址
  When 使用者執行唯讀登入測試
  Then 工具先對測試 SPC API 讀取 version
  And 再向測試 Portal 登入並取得 SPC 權杖
  And 主檔預檢應直接送往測試 SPC API
  And 不呼叫 preview 或 confirm

Scenario: 未配置正式 API 時不得誤用測試環境
  Given 使用者選擇正式環境但未設定完整網址
  When 工具執行登入測試
  Then 應拒絕執行
  And 不建立 Portal session 或任何資料寫入
```

## 回復

本件無程式或部署變更，不需要回復。若日後產品決定改為 Portal 代理，必須另立規格、評估權杖轉送、稽核、環境隔離與回復方案。
