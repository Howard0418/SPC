# BDD 驗收案例

## 唯一配對

Given 來源列與正式自然鍵唯一
When 建立更正計畫
Then 回傳同一量測 ID 的 before／after

## 非唯一或漂移拒絕

Given 找不到量測、多筆候選、來源鍵已占用或目前值不是核對時舊值
When 建立或套用計畫
Then 整批失敗且正式資料 0 異動

## Excel 為準

Given Excel 有初驗及可選複驗
When 套用更正
Then 使用日期公式與 F 快照的 Stored 值更新；Excel 無複驗時正式複驗清空

## 防重

Given 493 列已更正完成
When 相同來源再次執行
Then 0 列更新、0 列新增，原量測 ID 保持
