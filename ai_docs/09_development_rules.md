# 09 Development Rules

## 規範事項
1. **不可直接修改核心邏輯**: 必須先建立對應的單元測試。
2. **修改前說明**: 每次修改前需在 `ai_docs/10_change_log.md` 記錄目的。
3. **資料庫修改**: 嚴禁手動改表，必須透過 EF Core Migration。
4. **API 文件**: 新增 API 必須包含完整的 Swagger 註解。
5. **代碼規範**: 遵循 C# Coding Conventions 與 Vue 3 Style Guide。
6. **文件同步**: 重大邏輯修改必須更新 `ai_docs` 相關文件。
7. **異常處理**: 不可吞掉 Exception，需有適當的日誌紀錄。
