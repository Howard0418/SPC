# 技術計畫
- 功能 ID：`SPC-CONTROL-LIMIT-METHOD-1119-TASK-001`
- 規格版本：3
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- `XbarRChartCalculator.cs`：辨識明確選擇的方法，令計算界限優先於既有固定界限。
- `XbarSChartCalculator.cs`：上方 Xbar 圖套用相同方法選擇；下方 S 圖保持既有算法。
- `SpcService.cs`：將 PPC 公式設定傳入 Xbar-S 計算器。
- `MasterDataV2Controller.cs`：儲存時拒絕不支援子組大小的標準全距法。
- `PartProcessCharacteristicsView.vue`：依圖型與子組大小只顯示可用公式，並說明不支援原因。
- `XbarRChartCalculatorTests.cs`：建立固定界限覆蓋問題的回歸測試。
- Web 不改契約；沿用既有 `uclStat/clStat/lclStat` 繪圖。

## 實作方式與需求對應
- 解析 `STANDARD_RANGE`、`MOVING_RANGE_OF_XBAR/MR_METHOD`、`SIGMA_METHOD/SAMPLE_STD_DEV`。
- 所選方法成功算出界限時，Xbar-R／Xbar-S 圖點及 OOC 判定採計算結果；未選方法或無法計算時保留原優先順序。
- 標準全距法沿用現有 A2／D3／D4 常數表 2～25；不外插 n=50 常數。Web 隱藏不適用選項，API 同步拒絕繞過 Web 的直接送出。

## API、檔案格式及資料模型
無 API schema、資料庫或 JSON 格式變更。

## 相容性與風險
- 只改明確選擇公式的 Xbar-R 項目。
- 回復方式：回復計算器與測試提交，部署前一版 API；不需資料回復。

## 驗證安排
- 先建立會重現固定界限覆蓋的失敗測試，再修正。
- 先建立 n=50 標準全距儲存失敗測試及 Web 選項／提示測試，再補實作。
- 執行計算器焦點測試、相關 API 測試與 Release publish。
- 依版本規則遞增 API／Web 版本並發布測試站，驗證首頁、JS/CSS、health、version、環境及瀏覽器 API 基底。

## 發布、備份及回復
測試站測試通過後直接發布；正式站不在本次授權範圍。
