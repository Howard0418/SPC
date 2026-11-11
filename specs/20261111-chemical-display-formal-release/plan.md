# 技術計畫

- 功能 ID：`PORTAL-CHEM-RAW-INPUT-1111-FORMAL-RELEASE`
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- SPC：以 commit `9f76c060e0fb65658c44031944fc22429220ff13` 產生 API 0.1.125 與 Web 0.1.127 的 production 交付包。
- Portal：以已驗收的 `3290833` 來源產生 API 1.0.230 與 Web 1.0.271 的 production 交付包。
- 發布工具：`tools/release/Export-ProductionMigrationReview.ps1`、`New-ProductionReleaseBundle.ps1`、`Invoke-Release.ps1`。

## 實作方式與需求對應

1. 建立各系統完整 migration catalogue；正式預檢確認待套用 SPC 3 項（AuditEvents、AuditRetentionArchive、LimitChangeHistory）及 Portal 1 項（UserLocalePreferences），依使用者後續授權夾帶已審查 EF SQL。
2. 建立 production-mode API／Web 交付包；包內排除 appsettings、web.config、環境設定及私有資料。
3. 先以 Production／Plan 核對白名單主機、IIS、DB 與正式 EF history。
4. 先套用 SPC，再套用 Portal；每一套獨立備份、hash、COPY_ONLY／CHECKSUM／VERIFYONLY、smoke 及可回復。
5. 逐一驗證正式 Web／JS／CSS、health、version、environment 及瀏覽器實際 API base。
6. 正式 SPC 的 smoke 一律使用憑證主機名稱 HTTPS：API `https://PMR-SPC-SRV.pmr.com.tw:8443`、Web `https://PMR-SPC-SRV.pmr.com.tw`，不以會被強制轉址的 HTTP IP 驗證。

## API、檔案格式及資料模型

無 API 契約或 schema 新增。本次使用既有日報 API 的 Receipt／Trace 讀取與 `requiresSecondaryInput` 唯讀欄位；不寫入任何資料模型。

## 相容性與風險

- Portal API／Web 需與 SPC API 0.1.125 相容，故發布順序為 SPC 再 Portal。
- 不整包覆蓋未驗收的測試功能；交付包以各正式目標的白名單與版本 hash 限制。
- 回復只回復應用程式，不回復整庫；本次已依授權套用既有 migration，未執行業務資料修補。

## 驗證安排

- AC-001：Production Plan、目標 host／DB／IIS profile 及 migration history。
- AC-002：發布工具 private evidence 中的檔案／設定／SQL 備份驗證；發佈前後資料快照只讀比對。
- AC-003：以正式 API 和 Portal 完成日報讀取契約及登入後人工畫面驗收。
- AC-004：實際正式 Web、主要 JS／CSS MIME、`/api/health`、`/api/version`、Portal health／app-version、實際 API 基底及 401 探針。

## 發布、備份及回復

目標為 `PMR-SPC-SRV`／`PMR_SPC_2026` 與 `PMR-WEB`／`PMR_PORTAL_UAT`；絕不使用 `D:\Sites\PmrPortal`。每批 Apply 由白名單工具執行，包含應用程式備份、SQL COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY 與失敗自動回復。若 Portal 批次失敗，SPC 不會自動降版，需依同批 manifest 明確執行 rollback。
