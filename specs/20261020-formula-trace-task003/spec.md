# SPC-FORMULA-TRACE-TASK-003：日期選版與計算契約
- 版本1；狀態：完成（自動化通過、測試站已發布；人工案例待操作）；主系統SPC，相依Portal／TransFiles後續整合。
- 授權：使用者確認接續TASK-003；[主規格v3](../20261020-formula-effective-trace-plan/spec.md)。
- 沿用既有ChemicalAnalysisFormulaVersions、F表CellsJson歷史及TASK-002 schema，不另建公式維護來源。

## 需求與BDD
- AC-01 Given兩個相鄰台北日期區間，When各量測時點預覽（含UTC等價），Then選各自版本；缺版／重疊／無時區／錯誤日期拒絕。
- AC-02 Given管理者以舊修訂發布完整排程，When另一人已先發布，Then409要求重載；序列化鎖定PPC，發布及切換指標同交易。已發行receipt量測時點的公式／F不得改變；舊修訂不變，新修訂可在未使用日期切未來版。
- AC-03 Given既有Excel公式，When新契約計算，Then沿用Portal基準IF／ROUND／八位輸出與AwayFromZero儲存；缺參數拒絕，F只取指定不可變歷史，不取目前啟用F。
- AC-04 Given預覽token，When排程／日期／輸入／操作者變更或到期，Then準備receipt拒絕，不寫量測、不靜默換版。token由伺服器保護；receipt不可變且含原始字串、版本、來源hash及結果。
- AC-05 Given未整合客戶端，When發布排程，Then僅供新契約預覽，IsEnabled維持false，不能提早阻擋既有寫入。Viewer不能發布或準備receipt。
- AC-06 Given設定頁，When開啟藥液公式，Then可見已發布期間、版本、空缺提示及發布／試算錯誤。

## 範圍、相容與風險
- 不改既有匯入／圖表統計；規格判定沿現行分段優先／主檔fallback，快照及hash留存；獨立界限有效版本仍後案。
- 保留Portal計算語義的SPC相容解析器，明示引擎識別；TASK-004將Portal切共用契約，不修改Portal來源。
- 不啟用policy、不建TransFiles SQL commit、不更新正式站、不回填歷史。
- 資料庫：沿用TASK-002 migration，無新增schema；測試站需先可驗證備份才部署。rollback需停站還原部署備份；若需回DB須確認無新追溯資料後依備份恢復，禁止直接Down刪資料。
