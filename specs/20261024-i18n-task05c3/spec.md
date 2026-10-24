# 功能規格：I18N-TASK-05C3 管制項目／規格與排程三語
- 版本：1；日期：2026-10-24；狀態：完成
- 授權：使用者「開始下一項」，依[主任務](../20261022-i18n-task01/tasks.md)單件執行05C3。
- 主系統SPC；其他系統無調整；DB/schema/API無變更，僅發布SPC測試前端。
- 基準：[國際化契約](../../docs/i18n/contract.md)、既有管制項目識別/規格編輯/WE八規則、[前案](../20261024-i18n-task05c2/verification.md)。

## 目的與範圍
PartProcessCharacteristicsView主清單/新增編輯/所有頁籤/刪除確認/分段與歷史試算、SpecificationLimitHistory及ChemicalFormulaSchedule固定介面補繁中/簡中/英文。
資料名稱、來源API文字、units/enum/ID、原公式/JSON、原理由、日期與台北日界、比較/計算、DTO、權限與路由保持；永久清除的typed label仍使用原字串，不能因翻譯改變比對。保留固定WE規則與固定AD，不新增方法選單；舊「常態檢定方法切換」說明依已確認基準更正為固定AD。
切語言不得觸發重載/清草稿/重建表單/寫入或重算；模態內沿用既有LocalePicker，以免App語言選單被遮蔽。手機僅此路徑沿用既有stackedMobileNav，必要排版換行/容器限寬。

## 需求／BDD驗收
| R | AC | Given / When / Then |
|---|---|---|
| R1 | AC1 | Given清單及三種管制類型，When切三語，Then固定UI更新，資料名稱/原始公式/數字維持。 |
| R2 | AC2 | Given編輯未存規格/公式/篩選/頁籤，When切語言，ThenDOM/草稿/查詢/URL保留，無額外API或寫入。 |
| R3 | AC3 | Given產品/製程/藥液項目，When新增或將LSL改0.2儲存，Then原DTO/JSON與數值維持，驗證及API衝突原文可辨；不改識別演算法。 |
| R4 | AC4 | Given分段與試算，When試算/帶入/新增/編輯/刪除與歷史查詢，Then日期/精度/分段payload/原備註保持。 |
| R5 | AC5 | Given公式版本及排程，When回復/發布/依日期預覽，Then原版本ID/revision/原因/日期/Primary/Secondary/sourceKey保持，無量測寫入；無效期間阻擋。 |
| R6 | AC6 | Given刪除/永久清除/Viewer，When取消/typed不符/已結案阻擋/合法確認，Then既有權限及防呆與清除範圍保持，僅mock驗證。 |
| R7 | AC7 | Given1920/1366/1280/390三語，When操作兩視窗/頁籤/排程，Then可操作、可捲至儲存且資料表可橫捲。 |

## 驗證規劃／邊界
Happy：三類CRUD/規格/公式/分段/排程；Boundary：LSL0.2與原JSON、空態、500字、空迄日/Secondary、版本選項舊ID；Invalid：上下限/必填/反向日期/無發布原因/API失敗；Regression：原顯示模式清除規則、固定WE與AD、typed清除、舊9歷史/既有排程回歸。
字典/hash/完整模板/參數單元，Playwright攔截讀寫比對payload與草稿，build後測試站回歸及真實唯讀health/version/hash。翻譯draft留TASK20真人審查；mock不冒稱真DB/SSO驗收。

## 發布與回復
測試Web0.1.77，API沿用0.1.72；核對SpcSingleTest實體及AppEnvironment=test，備份frontend與hash，assets先index最後，保留設定不重啟pool。Rollback回備份資產再index並驗hash/health。無正式發布。無阻擋未決。
