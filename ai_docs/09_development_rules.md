# 09 Development Rules

## 規範事項
1. **不可直接修改核心邏輯**: 必須先建立對應的單元測試。
2. **修改前說明**: 每次修改前需在 `ai_docs/10_change_log.md` 記錄目的。
3. **資料庫修改**: 嚴禁手動改表，必須透過 EF Core Migration。
4. **API 文件**: 新增 API 必須包含完整的 Swagger 註解。
5. **代碼規範**: 遵循 C# Coding Conventions 與 Vue 3 Style Guide。
6. **文件同步**: 重大邏輯修改必須更新 `ai_docs` 相關文件。
7. **異常處理**: 不可吞掉 Exception，需有適當的日誌紀錄。
8. **進度表同步**: SPC工作收尾須依[進度表更新規則](../docs/spc-progress-update-rules.md)同步指定原Excel的實際日期、狀態與甘特圖；不能只更新文字。保留其他專案，先備份與副本驗證，OpenXML相容性／內容／圖形／列印檢查通過後直接取代；未更新或不適用須明示原因。
