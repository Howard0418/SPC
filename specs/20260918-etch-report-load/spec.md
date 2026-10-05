# 咬蝕日報載入修復
- 功能ID：20260918-etch-report-load；版本1；狀態：已實作及發布測試API，人工畫面待驗收。
- 依使用者回報：9/1顯示「日報載入失敗」。前案：[9月試匯](../20260917-etch-september-import/spec.md)。
## 規格與計畫
R1/AC1：既有日報API正常輸出完整50/100個正背面點位，避免Report→Points→Report循環JSON；日報日期/線別、同步狀態及欄位契約保留。
R2/AC2：沒有日報回空陣列；品保角色要求不變，不修改/重匯既有資料。
先以實際MVC JSON回應建回歸測試，確認失敗，再排除點位返回父日報的導覽屬性。只發布Portal測試API，保留設定，備份可回復。畫面由使用者人工驗收。
## 任務與驗證
- [x] MVC回應回歸測試及修正。
- [x] 建置、備份與Portal測試API發布。
- [x] 同步文件及必要驗證。
未修改SPC量測或正式站。

## 驗證結果（2026-09-18）
- AC1：修正前MVC兩例皆重現JsonException，路徑Points.Report.Points；在EtchAmountPoint.Report標示JsonIgnore後兩例通過，50/100點完整且不輸出父日報。
- AC2：兩例同時確認沒有日報日期回空陣列；未改控制器授權、SQL或資料。資料庫未操作。
- 隔離測試：D:/PmrPortal/tests/PmrPortal.Etch.Tests，TestResults/red.trx及green.trx。既有PmrPortal.Api.Tests有15項無關編譯錯誤，故未宣稱全套通過。
- Release publish成功；IIS PmrPortalApi實體路徑D:/PmrPortal/release/test/portal-api，Target=Test，:8091/health回200。DLL與staging雜湊一致，appsettings與web.config保留。备份路徑見../../release-staging/etch-load-20260918/deployment.json。
- 未自動操作瀏覽器，未執行已登入正式HTTP日報查詢；人工畫面驗收待使用者確認：重新整理→選2026/09/01→逐一切PT1/PT2/QE1/QE2並看A/B原始值。
