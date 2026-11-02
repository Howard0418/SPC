# 驗證紀錄

- 2026-10-29：`dotnet test backend/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter FullyQualifiedName~PartProcessCharacteristicsControllerTests`
- 結果：4/4 通過。
- 變更：公式設定內容變更時清除舊 `SpcCalculationResults`，後續圖表查詢使用最新設定重新計算；僅 JSON 空白／格式差異不視為變更。
- 發布：已套用測試 SPC API `SpcSingleTest`（IIS 實體路徑 `D:/SPC/release/test/backend`），備份 `release/test/backend.backup-control-line-20261029-122935`；`https://spctest.pmr.com.tw:8901/api/version` 回 200、environment=test、version=0.1.92，`/api/health` 回 200。正式站未發布。

## 2026-11-02最新續驗
續驗發現JSON物件欄位僅調換順序仍誤清計算快取；新增SQLite實際Update整合測試先紅1項，再改為JSON語意比較。等價JSON保留舊計算及主檔界限；真正MR倍率3→2保存後圖表UCL由41/6變16/3，其他PPC快取、原量測及歷史分段保持。相關16通過；backend測試66通過／1略過，主要API322通過／8略過；Release publish通過。測試backend已發布0.1.97，Web/API及瀏覽器實際基底驗證通過，3設定hash保持，正式未變。總表由過時「待執行」更正「測試修正完成／待正式授權」，核心35保持。
[完整測試、限制與回復](followup-20261102.md)。
