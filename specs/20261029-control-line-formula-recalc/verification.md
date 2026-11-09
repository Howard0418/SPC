# 驗證紀錄

- 2026-10-29：`dotnet test backend/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter FullyQualifiedName~PartProcessCharacteristicsControllerTests`
- 結果：4/4 通過。
- 變更：公式設定內容變更時清除舊 `SpcCalculationResults`，後續圖表查詢使用最新設定重新計算；僅 JSON 空白／格式差異不視為變更。
- 發布：已套用測試 SPC API `SpcSingleTest`（IIS 實體路徑 `D:/SPC/release/test/backend`），備份 `release/test/backend.backup-control-line-20261029-122935`；`https://spctest.pmr.com.tw:8901/api/version` 回 200、environment=test、version=0.1.92，`/api/health` 回 200。正式站未發布。

## 2026-11-02最新續驗
續驗發現JSON物件欄位僅調換順序仍誤清計算快取；新增SQLite實際Update整合測試先紅1項，再改為JSON語意比較。等價JSON保留舊計算及主檔界限；真正MR倍率3→2保存後圖表UCL由41/6變16/3，其他PPC快取、原量測及歷史分段保持。相關16通過；backend測試66通過／1略過，主要API322通過／8略過；Release publish通過。測試backend已發布0.1.97，Web/API及瀏覽器實際基底驗證通過，3設定hash保持，正式未變。總表由過時「待執行」更正「測試修正完成／待正式授權」，核心35保持。
[完整測試、限制與回復](followup-20261102.md)。

## 2026-11-09正式發布

- 使用者明示授權；隔離 API 103 候選只含本件 JSON 語意比較修正及既有正式 API 102 內容。
- 相關測試 12/12 通過；正式 API 0.1.103、environment=production、health=ok。
- Web 首頁與目前 JS／CSS 回 HTTP 200、MIME 正確；使用者端實際 API 基底 `:8443` 的 health／version 及 CORS 預檢通過。
- COPY_ONLY／CHECKSUM 備份與 RESTORE VERIFYONLY通過；五個保護資料集指紋前後相同，無 schema、公式或量測異動。
- 第一次發布腳本錯誤已在同一作業中完整回復 API 102；修正後 R2 發布成功，未遺留停站。詳細復原與 R2 證據見[續驗記錄](followup-20261102.md)。
- 尚缺正式登入後的指定 PPC 實際儲存／重繪驗收；本件保持未完成。
