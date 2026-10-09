# 全專案 AI + BDD 共用指引

版本：1.0  
日期：2026-10-09  
適用：已接入 `docs/sdd-workflow.md` 的十個專案。

## 共用流程
Requirement → Spec → BDD → Implementation → Test → Verify → 文件同步。

每個 Task 先確認需求、範圍與所屬專案，再建立或更新 `specs/`。重要功能必須在規格或 `features/` 以 Given / When / Then 描述驗收行為。AI Coding 只處理目前 Task 必要檔案、測試與文件，不自動開始下一個 Task。

## 共用段落
後續各專案可依專案語氣納入 AGENTS.md 或本地開發入口：

```markdown
## SDD + BDD + AI Coding
- 新增或修改功能時遵循 Requirement → Spec → BDD → Implementation → Test → Verify → 文件同步。
- 每個 Task 必須先有對應 specs/ 規格；重要功能需以 Given / When / Then 描述可觀察驗收行為。
- AI Coding 只處理目前 Task 必要檔案、測試與文件；不得順手重構未授權範圍或自動開始下一個 Task。
- 涉及資料寫入、權限、計算、匯入、發布或跨系統契約時，驗收需能對應自動化測試或明確人工驗證。
- 未完成 Spec、BDD、必要驗證與文件同步，不得標示 DONE。
```

## BDD 寫法
- `Given` 描述前提、角色、資料狀態或權限。
- `When` 描述使用者動作、系統事件或 API 呼叫。
- `Then` 描述可觀察結果，例如畫面文字、資料列、HTTP 狀態、檔案產出、稽核紀錄或錯誤訊息。
- 避免把實作細節當驗收條件；必要時在 Verification 補測試名稱或人工檢查方式。
- 涉及個資、機敏資料或真實姓名時，BDD 與測試資料使用測試資料與測試名字。

## STOP RULE
遇到以下情況先停止實作，記錄狀態或詢問使用者：

- 沒有對應規格，且不是純問答或只讀檢查。
- 需求會影響正式站、正式資料、schema migration、角色權限、AD/SSO、IIS binding 或憑證。
- 需要刪除資料、檔案或歷史紀錄；依使用者規則改寫待刪清單 MD。
- 發現可能的機敏資料、密碼、連線字串、真實姓名或個資；不得在回覆或文件中重述。
- 當前 Task 以外的重構、新功能或跨專案修改。
- 測試、建置或發布失敗，且無法在目前授權範圍內修復。

## 完成定義
- 規格、BDD、驗證紀錄與需求/變更文件已同步。
- 必要測試或人工驗證已執行；未執行項目明確標示原因。
- 文件型 Task 可將 build/發布標示不適用。
- 程式型 Task 依專案規則完成 build/test/發布與 smoke test。
- Git 提交只包含本 Task 必要檔案，不納入暫存、輸出、機敏設定或無關未追蹤檔案。

## 分批導入策略
- 第一批高頻專案：PmrPortal、TransFiles、KM、SPC 對齊 AI+BDD 入口、features 目錄或規格 BDD 寫法。
- 第二批支援/工具專案：Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib 依規模採輕量導入。
- 不補造歷史規格；只在新 Task 或使用者指定回補時建立。
