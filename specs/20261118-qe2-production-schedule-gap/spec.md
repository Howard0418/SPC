# QE2 正式區 2026-11-02 公式排程缺口修正

- 功能 ID：`SPC-CHEM-QE2-SCHEDULE-GAP-1118-TASK-001`
- 狀態：執行中
- 主系統：SPC；相依系統：Portal 藥液量測資料錄入
- 資料庫影響：正式區公式排程三表；不修改量測、公式版本、F 版本或主檔
- 發布影響：資料修正，不變更網站程式；正式資料操作已由使用者明示授權
- rollback：若新排程尚未產生 receipt，可把七項 policy 指回原 revision；原 revision 與 effectivity 不刪除

## 現況與需求

正式 Portal 載入 QE2、2026-11-02 資料時回傳 `CHAR-000830 計算失敗：量測日期沒有適用公式`。唯讀比對確認測試區 QE2 主槽及酸洗七個公式項目在該日各有唯一排程，正式區皆為零；相同公式 JSON 與 F 快照已存在正式區。添加桶在測試區該日本來沒有排程，不納入修正。

- R-001：只補 QE2 七項在台北 2026-11-02 的封閉日期區間。
- R-002：計算公式、複驗公式、精度與 F 依測試區該日生效資料，以計算欄位／F 儲存格語意匹配正式既有歷史；版本名稱、輸入標籤及正式既有調整文案不作為計算版本差異，不新增或猜測公式及 F。
- R-003：建立不可變新 revision，精確複製正式原 active revision 全部區間再加入缺口；不修改舊 revision/effectivity。
- R-004：保留 policy `IsEnabled` 原值，只切換 `ActiveScheduleRevisionId`。
- R-005：備份正式資料庫並驗證後才套用；操作須可重跑且不得改動非目標資料。

## BDD 驗收

- Given 正式七項 2026-11-02 無適用排程，When 套用修正，Then 每項在該日只能命中一段，且 Portal 預覽不再回傳排程空缺。
- Given 既有正式歷史排程，When 建立新 revision，Then 所有原區間、公式與 F 關聯保持不變。
- Given 同一修正再次執行，When 比對 active revision，Then 不新增 revision/effectivity。
- Given 添加桶與其他線別未授權補值，When 套用修正，Then其 policy、排程及資料完全不變。

## 驗證面向

- Happy Path：七項初驗預覽成功且與測試結果一致。
- Boundary：11/02 00:00 含、11/03 00:00 不含。
- Invalid Input：來源排程非唯一、找不到正式相同公式/F、已有重疊、環境錯置均停止。
- Regression：量測、receipt/trace、公式/F歷史、PPC與非目標排程指紋保持。
