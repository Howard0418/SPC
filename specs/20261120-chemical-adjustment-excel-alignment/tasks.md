# 任務

- 功能 ID：`20261120-chemical-adjustment-excel-alignment`
- 規格版本：1
- [規格](spec.md)｜[計畫](plan.md)｜[來源盤點](source-inventory.md)

- [x] `SPC-CHEM-ADJUST-ALIGN-1120-TASK-001`：P1／唯讀建立 11 張資料 Sheet、142 列與測試區 PPC 的逐列對帳矩陣；合併後114個來源身分、110唯一配對、4來源未配對、3網站額外，無一對多歧義。三參數唯一明確數值差異為N1氫氧化鉀稀釋22／23；公式差異及`NA`／空白均已隔離，未寫 DB、未改 Excel、未發布。[結果](reconciliation.md)（R-001～R-004；AC-001～AC-004）
- [ ] `SPC-CHEM-ADJUST-ALIGN-1120-TASK-002`：P1／先紅測試後讓 API 計算器支援槽容、添加及稀釋三個具名變數，缺值明確拒絕，既有公式輸出保持；測試通過後遞增 API 版本並發布測試站。（R-005、R-008；AC-005、AC-008；相依 TASK-001）
- [ ] `SPC-CHEM-ADJUST-ALIGN-1120-TASK-003`：P1／在線別分析項目總覽同列唯讀顯示三參數、兩式及對帳警示，匯出同步，證明公式保存不覆寫參數；測試通過後遞增 Web 版本並發布測試站。（R-006、R-008；AC-006、AC-008；相依 TASK-002）
- [ ] `SPC-CHEM-ADJUST-ALIGN-1120-TASK-004`：P1／依已核准固定矩陣受控套用測試資料，完成 before snapshot、preview、版本／稽核、重送 NoChange、計算及兩頁人工驗收；正式資料與正式發布另行授權。（R-007、R-008；AC-007、AC-008；相依 TASK-001～003）

## 執行順序

一次只執行一件：TASK-001 → TASK-002 → TASK-003 → TASK-004。任何無法唯一配對或業務語意未確認的列保持隔離，不阻擋其他可獨立驗證列完成，但不得被標為已同步。
