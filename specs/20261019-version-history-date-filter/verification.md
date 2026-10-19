# 驗證紀錄
- 功能 ID：SPC-VERSION-HISTORY-DATE-FILTER-20261019；版本1；2026-10-19。
- 實作狀態：未執行；AC-001～005均未驗收，未build／測試／DB／IIS操作。
- 已唯讀核對：公式版本GET無日期參數，F表前端take20及後端versions參數，規格ControlLimitSegments的生效起迄不等於修改日期。
- 限制：規格異動歷史來源尚待實作前確認；不把技術假設當已完成需求。
- 文件內容／連結及diff檢查於規劃收尾執行；測試／正式發布不適用（僅文件）。
- 後續依[任務](tasks.md)記錄實際功能證據；未操作正式環境。

## 前置核對完成（v2）
- 已核對Models.cs BaseEntity／ControlLimitSegment、AppDbContext.HandleAuditFields、MasterDataV2Controller.Update、ControlLimitSegmentsController及MigrationController相關寫入。只有目前值／最後更新，不是逐次版本快照。
- 已形成新增SpecificationLimitVersions提案與隔離migration／交易／回復驗證計畫；沒有新增程式或schema。AC-001～007均未執行功能驗證。
- 首件公式備註已獨立提交0a5c5b44並發布測試站API0.1.61／Web0.1.64；本案尚未發布。正式未操作。
