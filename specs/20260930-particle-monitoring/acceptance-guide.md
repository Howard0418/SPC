# Particle Monitoring 測試站驗收指南

## 測試前準備
1. 使用 `TransFiles/藥液分析轉檔工具.exe`，標題應為 `2026.10.01.1`。
2. SPC 目標環境保持「測試資料庫」，不可切換正式環境。
3. 準備一份落塵 Excel，至少包含一個有效日期及 R1-R9 的 0.5/1/5/10um 數值。
4. 從 Portal 測試環境登入，再由工具取得 SPC 登入狀態。

## A. TransFiles 轉換與預覽
1. 資料類型選「落塵監控」，選擇來源檔並執行轉換。
2. 應產生 `批次轉換結果_落塵監控_計數型匯入檔*.xlsx`；這是相容輸出，不是 Particle 的主要匯入格式。
3. 執行「轉換並上傳 SPC 預覽」。每一個完整量測事件最多應有 36 筆：R1-R9 × 四種粒徑。
4. 預覽資料應只含 R1-R9，不含 R10-R12；來源工作表、列、欄與原值應可追溯。
5. 無效日期、文字 Count、負數 Count 應顯示逐筆錯誤，且不可確認匯入。
6. 零錯誤批次確認後，再次確認同檔案不得重複寫入；重送已完成批次應維持冪等。

## B. 原始資料與序列隔離
1. 開啟測試站 `http://172.16.110.27:8083/particle-monitoring`。
2. 設定涵蓋匯入資料的日期，選擇 Location 與 ParticleSize 後查詢。
3. 原始資料應顯示時間、位置、粒徑、Count、抽樣體積、儀器與來源座標。
4. 分別查 R8／0.5um 與 R9／0.5um；兩者筆數及數值應依來源各自呈現，不得混成同一序列。
5. 切換粒徑 0.5、1、5、10um，資料應只顯示所選粒徑。

## C. C-chart
1. 圖型選 C-chart 後查詢。
2. 少於 20 點：應顯示「資料不足 20 點」，不顯示虛構管制界線。
3. 20 點以上：應顯示 Count、UCL、CL、LCL；LCL 不得小於 0。
4. 超出界線或觸發 Western Electric 規則的點應以異常狀態呈現。
5. 畫面應顯示固定採樣基準警示；規格線與統計管制線不可混用。

## D. U-chart
1. 現行落塵 Excel 未提供 SamplingVolume；直接切 U-chart 查詢時，預期顯示「U-chart 要求每筆 SamplingVolume 與單位皆有效」，這是正確阻擋。
2. 成功路徑需以 Particle preview API 匯入每筆皆有正值 `samplingVolume`、且整段使用相同 `samplingVolumeUnit` 的測試資料。
3. 再選 U-chart 查詢，應顯示 `count / samplingVolume`、逐點動態 UCL/LCL、抽樣體積單位及「已依 SamplingVolume 正規化」。
4. 任一筆體積空白、零、負數或單位不一致時，查詢必須被阻擋，不得自動用 1 代替。

## E. R1-R9 位置比較
1. 查詢後右側應以最新量測事件顯示 R1-R9 九個位置。
2. 缺測位置應保持空值／缺測狀態，不可補 0。
3. 同時間若存在多個來源事件，API 應回候選事件而非任選一組；以批次、工作表及來源列指定後才可比較。

## F. 回歸與發布確認
- `GET http://172.16.110.27:8081/health` 應為 HTTP 200。
- `GET http://172.16.110.27:8081/api/version` 應回 `environment=test`。
- Particle 頁面及 JS/CSS 資產應為 HTTP 200。
- 不應改動正式站；工具環境選項仍須預設測試。

## 自動驗證結果
- Particle 後端測試：12 passed。
- 後端 build：0 warnings／0 errors。
- TransFiles 相關測試：39 passed。
- SPC 前端 testhost build：通過。
- TransFiles EXE：GUI 啟動、版本標題及程序回應通過。
