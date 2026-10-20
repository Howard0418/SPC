# 計畫
- [規格](spec.md)；六張關聯表沿用既有公式版本與F歷史，不複製公式維護系統。
- EF partial configuration與save guard；SQL Server migration加入期間／immutable／跨PPC trigger及專用fail-closed DB role。SQLite只作關聯／EF行為測試，不替代SQL驗證。
- nullable量測receipt／來源hash保留舊流程；同一量測可有多次初驗／複驗trace版本，receipt與trace永不覆寫；統計job按量測／receipt去重，consumer未實作。
- 複合FK隔離PPC；published排程凍結，期間重疊以鎖定同一修訂序列化；真正發佈／確認鎖順序與計算API留下一件。
- SQL隔離庫固定SPC_FORMULA_SCHEMA_VERIFY_前綴，先驗實際目標，資料與備份留受限release-staging，不提交憑證、連線或證據輸出。
