# 任務
- 功能 ID：20261118-chemical-exclusion-and-adjustment-parameters
- 規格版本：3
- [規格](spec.md)｜[計畫](plan.md)

- [x] `PORTAL-SPC-CHEM-EXCLUSION-1118-TASK-001`（P0／正式發布完成）：端到端加入排除、三項備註、追溯、查詢標示及所有藥液統計／圖表排除；共用既有排除資料表，無 migration。正式版本與唯讀端點驗證通過。（R-001～004、R-006）
- [x] `PORTAL-PROCESS-ROLE-1118-TASK-001`（P0／先）：新增製程人員 `process_engineering` 角色及權限管理顯示，只作通知收件身分，不自動增加公式頁修改權。（R-011）測試區 API1.0.259／Web1.0.297 已發布；登入畫面因電腦操作通道不可用待使用者目視確認。
- [ ] `PORTAL-CHEM-ADJUST-PARAMETERS-1118-TASK-001`（P1／待人工驗收）：核心 API、Portal 代理及頁面已完成並發布測試站；槽位、分析項目、規格、範圍唯讀共用主檔，只保存槽容、添加／稀釋管制值並建立版本。自動測試與發布連線驗證通過。（R-005～006、R-008～009）
- [x] `PORTAL-CHEM-ADJUST-REMARK-1118-TASK-001`（P1／測試區驗收完成）：每個分析項目有選填備註，最多 500 字，保存重載、版本及稽核一致，且不參與計算。2026-11-20 使用者確認人工驗收通過，正式發布待另行授權。（R-006～007）[驗證](verification-adjustment-remark.md)
- [x] `PORTAL-CHEM-ADJUST-CONTROL-NOTIFY-1118-TASK-001`（P1／測試區完成並於2026-11-20人工收信驗收）：添加／稀釋管制值有效值改變且提交成功後，以既有 SMTP Email 通知品保及製程角色並列出前後值；同值、只改備註或失敗交易不通知。Email版最終測試 Portal API1.0.281／Web1.0.319；正式區未發布。（R-010～011）
- [ ] T-003：實作時更新需求、TODO、CHANGELOG、ai_docs及跨專案引用。
- [ ] T-004：逐件記錄自動化、人工畫面、版本、發布與回復證據。
