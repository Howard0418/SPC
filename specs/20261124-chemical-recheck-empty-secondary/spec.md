# SPC-CHEM-RECHECK-SECONDARY-1124 複驗空白第二滴定值

主系統 SPC API，Portal 藥液錄入相依。複驗只填一個滴定值時，Portal 預覽送 null，確認可送空字串。SPC 憑證內包含原始輸入，兩種表示會造成錯誤衝突。

- Given 複驗只有第一滴定值，When 預覽以缺值、確認以空字串或空白表示第二值，Then 預覽與確認建構相同輸入。
- Given 有第二滴定值或輸入確實改變，When 確認，Then 保留原始字符串與現有憑證檢核。初驗、排程、規格及量測資料無資料補正。
- 測試站遞增 API 與前台版號。回復前版 API 與 Web 入口；不涉及 migration。
