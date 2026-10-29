# 驗證紀錄

- 2026-10-29：`dotnet test backend/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter FullyQualifiedName~PartProcessCharacteristicsControllerTests`
- 結果：4/4 通過。
- 變更：公式設定內容變更時清除舊 `SpcCalculationResults`，後續圖表查詢使用最新設定重新計算；僅 JSON 空白／格式差異不視為變更。
- 發布：已套用測試 SPC API `SpcSingleTest`（IIS 實體路徑 `D:/SPC/release/test/backend`），備份 `release/test/backend.backup-control-line-20261029-122935`；`https://spctest.pmr.com.tw:8901/api/version` 回 200、environment=test、version=0.1.92，`/api/health` 回 200。正式站未發布。
