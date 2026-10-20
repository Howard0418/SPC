# 實作計畫
- [規格](spec.md)；主系統SPC，Portal／TransFiles只讀來源，不修改兩專案。
- 新增tools/testing/Invoke-FormulaTraceBaseline.ps1及test_formula_trace_sources.py；Python只用標準庫、AST選取已檢查的純函數，不匯入GUI，封鎖socket並使用-B避免寫來源__pycache__。
- SPC測試專案以明確MSBuild opt-in連結Portal單一無外部相依的ExcelFormulaEvaluator.cs，不複製實作／硬編碼個人路徑；預設不連結且明確Skip。runner先驗路徑再執行，不安裝套件。
- 公式測試只使用InMemory EF及合成F資料，日期測試固定現有解析行為；反例測試是相容性風險的證據，不是修改業務規則。
- 先建立測試再補測試載入／runner；不修改運行中核心邏輯。補齊 [contract.md](contract.md) 的來源位置、DTO與後續DB驗證責任。
- build／發布：僅編譯測試，不發布應用程式；測試站仍API0.1.63／Web0.1.67。DB／migration／IIS均不操作。
- rollback：revert本次必要測試／文件提交；無應用或資料回復需求。
