# I18N-TASK-08C1 管制項目業務驗證三語
- 2026-10-26 DONE，必要驗證與測試發布完成；[連續授權](../20261026-i18n-continuation/spec.md)。08C依現有範圍分C1管制項目、C2其餘主檔、C3公式／圖表與查詢、C4匯入／設備／校驗／設定、C5逐頁即時錯誤descriptor，每次單批。
- 主系統SPC，無相依系統修改／DB schema／角色變更；只PartProcessCharacteristicsController錯誤metadata與API資源。保留所有scope／欄位正規化／重複鍵排除自己／公式與歷史／交易／刪除限制及HTTP，原繁中message必須相同。
- R1 驗證私有helper由string?改安全typed descriptor?，return null成功契約與分支順序不變。固定key與空args／原group名稱具名args，群組原文只當資料，不翻譯／不重編譯。
- R2 create/update/delete/rules/purge原有錯誤message附加code/key/args；不更動成功DTO、數字／日期、資料庫／公式。reason長度同500，無新驗證規則。前端仍沿08A字串介面，C5才逐頁即時更新。
- BDD Happy：Given編輯原項目只把LSL改0.2，When儲存，Then排除自己、資料儲存與歷史行為相同，不出現假重複。
- BDD Boundary：Given實際重複鍵、不同group要求、槽位、chart/data category／500長度、既有量測或結案異常，Then原400/409與繁中message不變，新增安全descriptor。
- BDD Invalid：Given無process/characteristic／無效JSON／錯group，Then先拒絕、不寫入，不洩露原JSON／內部例外；group名稱可含HTML與i18n符號，只純文字。
- BDD Regression：既有PPC maintenance全測、API契約與typed args測試，build/test後只發布Web0.1.94/API0.1.75。先唯讀核test DB與pending migration、成對備份。正式另授權。
- 回復同08A測試pool與成對備份策略，保護設定與資料；無migration回復。譯文draft留20真人審稿。[任務](tasks.md)／[驗證](verification.md)。
