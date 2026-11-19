# 計畫

1. 先以 Portal 契約測試與 SPC API／Web 測試固定新行為。
2. Portal 由下拉值推導 `IsExcluded`，移除 checkbox 及其事件／驗證。
3. SPC API 以合法固定原因推導排除狀態；SPC Web 將 exclusion reason 與 point remark 分開投影至圖點、提示及備註對話框。
4. 執行焦點測試、建置與回歸，分別遞增 Portal／SPC API、Web 版本並發布測試區。
5. 驗證 Web／資產／API health/version／實際 API 基底；正式不發布。
