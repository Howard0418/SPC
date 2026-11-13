# 技術計畫
- 功能 ID：SPC-CHEM-PRECISION-1113-TASK-001
- 規格版本：1
- 規格：[spec.md](spec.md)

1. SPC 預覽回應帶回本次公式 `decimalPlaces`，不改公式計算／保存邏輯。
2. Portal 精度工具接受指定小數位；公式預覽依回傳值顯示，直接輸入依項目設定顯示及送出。
3. 將 payload 的固定 `toFixed(3)` 改為保留精度原字串，避免四位遺失。
4. 建立前後端測試、建置並發布測試站；不改資料庫或正式站。
