# SPC-FORMULA-TRACE-TASK-006：實際計算版本查閱
版本1；狀態：實作／自動化驗收／測試發布完成；正常真人操作待驗。使用者授權先完成SPC兩件，TASK-005已提交並發布；本件主系統SPC，Portal／TransFiles僅既有驗收相依，不修改KM／其他工作。

## 範圍與BDD
- AC01 Given日期分段含新舊量測，When點日期區間，Then分頁列出該PPC／日期範圍的量測，顯示實際receipt／trace公式版本、固定公式內容與參照依據、F快照／引用值、原始輸入／結果及儲存值；舊量測明示Unknown，不使用目前公式推定。
- AC02 Given同一區間保存多版本、初複驗及修正，When查閱，Then揭露多版本，初驗／複驗分別顯示，逐點可查不可變計算歷程，不能給整段假造單一公式。
- AC03 Given管制圖／藥液趨勢圖單值點，When點選，Then用VariableMeasurementId查該點證據；子組無完整成員ID時明示不可判定單一版本，改用日期分段逐筆查閱；非藥液圖不套用公式。
- AC04 Givenreceipt快照缺損或跨PPC／錯segment／無權限，When查閱，Then拒絕或明示證據不完整；不把修改日期當生效日期、不重算、不寫DB。
- AC05 GivenF或目前公式變更、舊資料混合與API暫時失敗，When查閱／重試／換日期／換點，Then已保存證據不變、舊回應不蓋新選擇；無寫入及統計回歸。
- 保存的規格值標示為計算時快照；獨立規格版本／管制界限版本仍未實作，保持明示無法確認。不更動既有統計定義、不補算歷史。

## 影響與回復
新增唯讀trace查詢及共用UI，無schema／role／AD變更。測試通過備份後發布SPC測試API與Web；正式、Portal、TransFiles與KM不更新。rollback還原測試應用／前端備份，保留trace與資料庫，不Down。
