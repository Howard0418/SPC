# PPC編輯保留選填線別
- ID：SPC-PPC-OPTIONAL-MACHINE-20261022；版本1；狀態：正式API0.1.71發布完成，2620真人儲存待確認。
- 使用者要求解決正式編輯受阻，提供2620與LSL改0.2；先測試修正，正式發布仍另行授權。
- 主系統SPC；無Portal/TransFiles/DB schema影響；本次最小修正RequiresMachine=false時不清除已選MachineId。
- R-001：非必填線別表示可不填，不得抹除已填線別；必填檢查及真正重複攔截保留。
- AC-001 Given PROCESS/RequiresMachine=false、2620型資料Machine247且另一2610型空線別，When 只改LSL0.2，Then 儲存成功且MachineId不變。
- AC-002 Given 真正相同識別鍵，Then 仍409；必填但未填線別仍400。
- AC-003 Given 選填且原無線別，Then 保留null；不修改Part/Tank/Slot規則、不刪併既有主檔。
- 根因：正式2620 ScopePROCESS/Process195/Machine247/Characteristic870/Unitμm/ChartType1046，group24 PROC RequiresMachine=false；ValidateScopeAsync會清成null，與2610衝突。正式LSL已約0.2，畫面長小數顯示屬另一呈現議題，不納本次。

- [計畫](plan.md)、[任務](tasks.md)、[驗證與正式具體方案](verification.md)。
