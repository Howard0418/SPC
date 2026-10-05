# 咬蝕匯入管制項目調整

## 需求
咬蝕量匯入只要求 `ETCH_A_AVG`、`ETCH_B_AVG`、`ETCH_RATE`；不要求 `ETCH_LINE_SPEED`。

## 驗收
- 每條線須有 3 個啟用的 `PROCESS` 映射。
- A/B 樣本數依完整子組為 PT1/QE1 25、PT2/QE2 50；速率樣本數為 1。
- 匯入點數為完整點位加 1 個速率值。

## 實際修正
- SPC 測試庫以交易補上 8 筆 A/B 映射的 `MachineId`；正式庫未修改。
- PT1/QE1 樣本數 25，PT2/QE2 樣本數 50；`PROCESS`／啟用狀態維持。
