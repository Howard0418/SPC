# 計畫
1. 在SPC docs/i18n維護版本1.0.0共用契約；nested物件/字串leaf，範例不隱含產線依賴。
2. 統一key/placeholder/fallback規範及JSON範例；詞彙對照與逐key metadata/來源hash/審核欄位。
3. 設計未來缺漏檢查的輸入/退出碼/錯誤分類/測試矩陣；本次用臨時驗證腳本檢查範例及故障fixture，不新增產品檢查器或CI接線。
4. 同步TASK-02與工作池，文件連結/staged diff檢查、繁中提交上傳後STOP。
## 相容與回復
- 不改三站Runtime/驗證/帳號/資料模型；DTO/DB值仍原格式，named placeholders不使用String.Format或eval。
- 本Task文件型，不需應用build/test/部署；範例以JSON解析/hash/結構/故障fixture驗證。
- 回復方式為後續文件修訂；沒有站台或資料庫回復需求。
