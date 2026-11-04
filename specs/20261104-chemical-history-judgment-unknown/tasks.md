# 任務

- 功能 ID：SPC-CHEM-HISTORY-JUDGMENT-1104
- 規格版本：5
- 中央小工作：`SPC-CHEM-HISTORY-JUDGMENT-1104-TASK-001`／P0／執行中（T-001～T-003完成）
- [規格](spec.md)｜[計畫](plan.md)｜[驗證](verification.md)

依資料正確性由高到低執行，每次仍只處理這件小工作的下一個階段；本輪只執行T-003，不開始T-004。

- [x] T-001（P0）：唯讀盤點全部藥液Unknown／Unavailable，逐筆分類缺追溯、缺當時規格、計算值不符、僅紀錄模式或其他固定原因（R-001、R-002；AC-001、AC-002）。[結果](inventory-20261104.md)：正式4,109、測試1,611筆全部缺歷史判定證據，0筆可直接補判；資料寫入0。
- [x] T-002（P0）：一般計量API／Excel／CSV的CHEM列在preview以固定原因隔離，confirm再防舊批次繞過；Portal專用交易保持，通用範本移除CHEM示例。自動測試、隔離建置、測試API 0.1.107／Web 0.1.120與同源API驗證通過（R-003、R-004；AC-003、AC-004）。
- [x] T-003（P0）：受控服務、SHA-256 manifest、過期拒絕、重跑NoChange與整批rollback自動測試通過。實際測試庫dry-run為Unknown 1,611／eligible 0／`NO_TRACE_EVIDENCE` 1,611，前後指紋一致、寫入0；測試API0.1.108／Web0.1.120已驗證（R-004、R-007；AC-004、AC-007）。
- [ ] T-004（P1）：Portal顯示固定原因及調整建議，保留既有Unknown、OK／NG與分頁行為（R-005、R-006；AC-005、AC-006）。
- [ ] T-005（P1）：測試站API／Web／實際基底、畫面與遮罩截圖驗收；正式候選另列，不發布正式（AC-005～AC-007）。
- [ ] T-006：同步需求、工作池、進度表、驗證與發布狀態；提交上傳本件必要檔案。正式補正或發布仍須另行授權。

## 與既有工作的關係

- 引用`SPC-HISTORICAL-IMPORT-1102-TASK-002`的9／10月來源與追溯證據，不重做來源解析，也不以本件核銷父工作。
- 引用`SPC-CHEM-F-CONSISTENCY-1103-TASK-001`的公式F驗證，不擴張到其4項NA、另兩式、EXE或正式發布。
- 本件新增一筆中央待辦；不把每筆Unknown拆成獨立小工作。
