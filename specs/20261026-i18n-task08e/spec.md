# I18N-TASK-08E 人讀Excel報表、報表郵件與匯出語言
- 2026-10-27 DONE，SPC必要驗證與測試發布完成，依[連續授權](../20261026-i18n-continuation/spec.md)，接續[C6](../20261026-i18n-task08c6/spec.md)。主系統SPC，Portal/TransFiles及schema/角色無修改。
- 現況：人讀項目總覽與CPK Excel及其郵件固定繁中。機器CSV、計量／計數匯入範本、Migration指定工作表與原始資料不屬人讀報表，不翻譯。
- R1 手動項目總覽匯出／寄送依X-SPC-UI-Locale，CPK沿原window.open下載流程增加僅顯示用uiLocale query（無query時讀header），canonical之外繁中；不改Culture／Accept-Language／身分／查詢條件。預設繁中／背景報表維持既有標籤與檔名。
- R2 人讀workbook僅翻工作表名稱、標題、表頭、已知系統顯示類別／計算方式與能力判定；原料號／名稱／責任人／備註／enum／ID／數值／cell type／精度／原期間與排序／freeze／style保持。來源資料不變，缺或非法display metadata回原文字，不反查原文key。
- R3 CPK保留既有query／mean／sigma／Cp／Cpk／rounding／門檻與顏色分支，譯文只在寫入cell時呈現，不用譯文判斷color或資料。既有month目前僅標題／檔名、未篩資料的觀察已另列REPORT-DATA-VERIFY-001；本批不擅改統計業務範圍，驗證舊結果相容不代表認可其業務正確性。
- R4 手動報表Email主旨／固定內容／sender label三語，原收件人／附件內容資料／檔名／MIME與SMTP成功失敗保持；背景預設繁中，不改日期／排程／LastSent及重試。外部姓名HTML編碼，未知寄送例外保留原diagnostic；已知false新增安全顯示metadata並接前端。
- BDD Happy：Given選定三語，When下載人讀報表或fake寄送，Then標籤／已知狀態正確、data cells與原檔名相同；CSV／匯入範本不變。
- BDD Boundary：Given0、null規格、單筆／零變異、Cpk1.33／1.0／不足與raw名稱含HTML/Excel公式前綴，Then原數值／型別／公式／判定顏色保持，只換顯示文字；資料仍為原文字，不變Excel公式。
- BDD Invalid：Given非法locale／metadata／寄送false／未知ex，Then原fallback與結果保持，不因譯文或物件判斷業務，不新增資料寫入或真寄送。
- BDD Regression：核心先測workbook及CPK原結果／顏色，再測隔離controller/fake report sender及原背景排程；必要CSV bytes／匯入範本source相容驗證、前端窗口query／收件人與DTO、三語報表檢查。
- 測試Web0.1.107/API0.1.85；核PMR_SPC_TEST/IIS，成對完整備份發布，rollback回原.106/API84並保護設定資料，無DB回復需求。正式另授權，譯文draft。
- [計畫](plan.md)／[任務](tasks.md)／[驗證](verification.md)。

- 2026-10-27 恢復範圍：使用者要求先完成SPC；本次只完成08E及SPC發布收尾，Portal09～18及共用19～21維持暫停，真人審稿仍屬TASK20，不冒稱完成。
