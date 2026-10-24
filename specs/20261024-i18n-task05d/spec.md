# 功能規格：I18N-TASK-05D 量測輸入UI三語
- 版本：1；日期：2026-10-24；狀態：完成
- 授權：使用者「開始下一項」，依[主任務](../20261022-i18n-task01/tasks.md)單件執行05D。
- 主系統SPC；相依其他系統無變更；不改DB/API/schema；僅SPC測試frontend發布。
- 基準：[國際化契約](../../docs/i18n/contract.md)、既有MeasurementEntryView的數值/日期/DTO/權限；Portal仍負責量測入口，本次只翻譯SPC既有/measurements管理補登修改頁，不新增Portal功能或恢復新入口。

## 範圍與限制
既有量測頁、工站綁定及Soft Hold視窗固定文案/驗證/提示三語；API與主檔原文、產品/工站/特性/槽名、單位、批次/工單/序號、量測數值、時間轉換/日期query/樣本順序、工站localStorage key/value、Editor路由及InProgress處置權限不變。
模態內復用LocalePicker原handler，切語言不得重載、重建輸入或清草稿/選項/卡控/發寫入。API錯誤保留原文，固定前綴與前端錯誤即時切語言。預設仍繁中，翻譯draft留TASK20真人審稿。
不調整日期算法（含既有新批次初始化）、預設批號格式、數值parse、OOS比較/統計引擎、卡控完成條件及回圈處置。

## 需求／BDD
| R | AC | Given / When / Then |
|---|---|---|
| R1 | AC1 | Given三類scope與工站綁定，When切三語，Then固定UI切換，機器值/名稱/篩選結果/綁定保存值保持。 |
| R2 | AC2 | Given工單/追溯/作業員/日期/樣本草稿，When切語言，ThenDOM/焦點/選取/URL保持，無額外讀寫或換樣本。 |
| R3 | AC3 | Given新量測，When填數值或Enter換欄及末欄送出，Then原樣本順序/number/ISO時間/null/批號fallback/DTO保持；缺作業員/空樣本阻擋，0有效。 |
| R4 | AC4 | Given載入當日資料，When查詢後修改儲存，Then原ppcId/date/timezoneOffsetMinutes query及轉回本地時間/PUT batchId保持。 |
| R5 | AC5 | GivenOOS/Soft Hold，When切語言或提交處置，Then邊界判定保持、卡控不解除，原alert/type/value不翻譯；缺必填不送出，成功逐筆InProgress/原reason/operator後才解除，失敗保留草稿。 |
| R6 | AC6 | Given1920/1366/1280/390三語/空態/API錯誤/Viewer，When操作，Then表單與兩視窗可達/可捲至按鈕，原guard保持。 |

## 測試規劃
Happy：三scope POST/既有PUT/工站綁定/soft hold逐筆；Boundary：0、LSL/USL相等邊界及超界、空工單fallback、空態/首綁定；Invalid：空白作業員/空樣本/處置必填/API失敗；Regression：Enter順序、UTC/台北日期query、卡控不因切語言消失、後端欄位與status不翻譯。
單元字典/hash/模板/插值，瀏覽器全mock讀寫對DTO/DOM/日期/卡控比對、build及發布後同源/hash/真匿名health/version；mock不是真SSO/DB寫入驗收。
必要手機排版限此頁及兩視窗，僅/measurements沿用既有stackedMobileNav，不改其他頁面。

## 發布與回復
測試Web0.1.78/API沿用0.1.72；核SpcSingleTest vdir及test環境，備份frontend核hash，assets先index最後/保留設定、不重啟pool；rollback先回備份資產再index，核health/hash。無正式發布。無阻擋未決。
