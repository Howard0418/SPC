# SPC-FORMULA-TRACE-TASK-004：Portal日期試算與原子留存
- 版本1；狀態：實作／自動化驗收／測試站發布完成；真人操作待驗。授權：使用者確認接續TASK-004；[主規格v4](../20261020-formula-effective-trace-plan/spec.md)。
- 主系統SPC（計算／交易／量測／trace），相依Portal（代理／表單）；TransFiles直寫與consumer仍TASK-005，圖表實際版本TASK-006。
- DB影響：沿用TASK-002 schema，無新migration、不啟用policy；無正式資料／權限／IIS binding變更。

## 現況
Portal VariableMeasurementEntry自行preview-custom、toFixed(3)再upload／confirm；SPC UploadService.ConfirmAsync分次SaveChanges且無整批交易，尚無receipt／trace整合。沿用既有驗證、日報upsert及統計，不建立第二套量測儲存。

## BDD與驗收
- AC-01 Given某PPC有已發布排程，When Portal初驗／複驗試算，Then送原始十進位字串及帶時區量測時間至SPC，顯示實際版號／結果；不傳自訂公式，缺版拒絕。
- AC-02 Given預覽完成，When日期／班別／階段／PPC／讀值變更，Then舊結果與token失效，確認須重預覽；伺服器重新核對排程與hash，不相信client結果／版號。
- AC-03 Given同批初驗／複驗及未排程項目，When確認，Thenreceipt、量測、初驗／複驗trace及SPC計算同交易；任一列失敗無半筆。正常重送同requestId回原結果，不重複；同ID不同內容拒絕。
- AC-04 Given已有日報，When更新複驗，Then保留歷次trace，沿用日報唯一性（台北日／PPC／班別／階段），複驗參與現有統計；查回保留原始讀值。
- AC-05 Given未發布排程／非藥液既有入口，When使用，Then不強迫日期選版；Portal所有藥液確認改走受控交易入口，尚未相容TransFiles不提前開gate。
- AC-06 GivenViewer或缺個人SPC token，When確認，Then403／401，不以系統token代替，不簽發測試憑證。

## 非範圍及發布
- 不改公式語義／統計定義、不自動重算舊資料、不回填假版本、不刪歷史。
- 新增SPC交易入口與UploadService窄範圍evidence hook；Portal使用既有個人token proxy。預覽無寫入，receipt只在確認交易發行。
- 測試通過先發布SPC測試API，再以Portal scripts/publish-all.ps1發布實際測試IIS，保留appsettings/uploads；正式不操作。
- rollback：部署前備份；停測試應用還原DLL／Web。無schema變更；trace不可刪或Down，保留既有相容資料。不可將尚未整合的Portal回退後繼續寫已啟用policy項目。

- AC-07：日期公式的規格判定使用已保存receipt的原始計算值（與預覽一致）；統計值／管制界限仍用既有儲存值，不重新套公式。例raw1.004／stored1.00、USL1.002，須NG且保留兩值；警報信只在commit後寄。
