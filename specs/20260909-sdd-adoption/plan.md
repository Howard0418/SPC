# 技術計畫
- 功能 ID：20260909-sdd-adoption
- 規格版本：1
- [規格](spec.md)

## 實作與需求對應
- R-001：SPC docs/sdd-workflow.md 與 docs/templates/sdd/ 作為單一主來源。
- R-002：建立 SPC/TransFiles AGENTS.md；於 Portal 原 AGENTS.md 附加 SDD 區段，各專案建立需求索引。
- R-003：主規格放 SPC；Portal、TransFiles specs/ 同 ID 只保留引用。保留原有文件內容。
- R-004：三專案記錄變更；本案完成後仍需下一個實際功能試行。

## API、資料模型與相容性
不涉及 API、程式或 schema。採 Markdown，無新增依賴。
跨磁碟以目前工作區絕對路徑引用，移動時須維護。

## 驗證
檢查本次檔案存在、Markdown 本機連結目標、必要欄位、ID/版本與引用、變更紀錄以及 Portal 原規則保留。
檢查 Git whitespace；Portal 如有 ownership 限制使用單次命令 safe.directory，不修改全域設定。
純文件導入不執行應用程式編譯、業務測試、外部資料寫入。

## 發布與回復
發布不適用。回復時只撤回本次新增文件及附加段落，保留工作區原有變更。
