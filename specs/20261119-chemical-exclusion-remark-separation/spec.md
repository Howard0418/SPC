# PORTAL-SPC-CHEM-REMARK-SEPARATION-1119-TASK-001

## 目的與範圍

- 主系統：Portal 藥液量測資料錄入；相依系統：SPC API 與 I-MR 管制圖。
- Portal 移除每列「不列入」勾選；固定備註下拉只保存量測備註，無論有無備註都正常列入統計與繪圖。
- SPC 圖點提示顯示量測錄入保存的固定備註。
- 「新增／編輯備註」顯示固定備註來源，另以自由文字欄保存圖點備註；兩者使用既有 `SpcPointExclusion.Reason` 與 `SpcPointRemark.Remark` 分離，不互相覆蓋。
- 無 schema migration；不修改量測值、公式或管制線。固定備註由 Portal 日報 `UploadDetail.PayloadJson` 讀取，不建立 SPC 排除。

## BDD 驗收

- Given 藥液量測列尚未選固定備註，When 查看錄入列，Then 不顯示「不列入」勾選且該列列入計算。
- Given 使用者選擇固定備註並儲存，When SPC 查詢該點，Then 該點正常列入計算及繪圖，固定備註可由圖點提示看見。
- Given 圖點已有 Portal 固定備註，When 開啟新增／編輯備註，Then 對話框分區顯示固定備註及獨立自由文字備註。
- Given 使用者保存或清空自由文字備註，When 重新載入，Then 只異動 `SpcPointRemark`，固定下拉對應的排除原因與排除狀態不變。
- Given 圖點自由備註已儲存且量測 ID 相符，When 使用者不重新查詢而直接移到圖點，Then 提示框立即顯示「圖點自由備註」及內容。
- Given 固定備註或自由備註超過 20 個字，When 顯示圖點提示，Then 每 20 個 Unicode 字元自動換行，提示內容使用 14px 字級且不橫向撐滿圖表。
- Given 使用者新增、編輯或清除圖點自由備註成功，When 儲存完成，Then 編輯層、右鍵選單及舊 tooltip 立即關閉，目前管制項目重新查詢並重繪，日期、線別與查詢條件保持不變。
- Given 固定備註有值，When SPC 確認批次，Then 強制 `IsExcluded=false`，保存備註但不得建立排除。

## 驗證與回復

- Happy path：三個固定選項、圖點提示、對話框分離保存。
- Boundary：空白固定備註列入；500字自由備註不影響固定原因。
- Invalid：未知固定原因拒絕。
- Regression：固定備註點正常納入計算、人工右鍵排除、圖點自由備註 CRUD、量測保存。
- 回復：回復 Portal 列輸入與 SPC 顯示邏輯；資料欄位與既有資料不需回復。
- 發布：測試區自動發布；正式區另行授權。
