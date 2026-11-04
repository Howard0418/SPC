# BDD 驗收

## 場景一：既有報表日不重匯

Given 正式庫匯入前已有相同 PPC 與 PortalDailyDate
When 建立十月正式匯入計畫
Then 該 PPC／日的所有來源列皆標示略過
And 不以另一班別、時間或來源鍵建立新量測

## 場景二：新報表日依授權規則寫入

Given 來源列屬1,199筆公式F已確認範圍且正式 PPC／日不存在
When 正式匯入
Then MeasuredAt採報表日12:00台北時間
And 中班檔為MIDDLE、其餘OPEN
And N1／N2開收依工作表，其他為GENERAL
And 來源追溯明示歷史匯入時間不是實際量測時間

## 場景三：衝突保持隔離

Given ST2於10/01或10/14同PPC同日有不同完整F
When 建立或執行計畫
Then兩個來源都不寫入
And不得選OPEN、MIDDLE或目前F作勝者

## 場景四：冪等重送

Given 本件正式寫入已完成
When 使用相同manifest再次執行
Then 新增量測、Receipt、Trace、UploadBatch與統計工作皆為0
And 已建立ID與既有資料不變

## 場景五：既有項目缺管制圖型別

Given 新增量測與追溯已成功但正式PPC沒有ChartTypeId
When 統計後處理執行
Then 量測保持已新增且可由追溯與圖表服務查回
And 統計工作明確保留失敗狀態供補正後重跑
And 不猜測管制圖型別、不重匯或覆寫量測
