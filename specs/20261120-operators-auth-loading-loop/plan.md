# 實作計畫

## 影響範圍
- 主系統：SPC。
- 前端：`frontend/mes-spc-web/src/api/client.js`、`frontend/mes-spc-web/src/views/OperatorsView.vue` 及對應測試。
- 後端：只驗證 `/operators` 與 `/operators/permissions-catalog` 的 401／403 契約；除非測試證明後端錯回 401，否則不修改 API。
- 資料庫：無影響。
- 發布：實作完成後依規則遞增 SPC Web；若 API 有修改才遞增 API。只自動發布測試站，正式站另行授權。
- 回復：回復本件前端攔截器／頁面變更與 Web 版本；不涉及資料回復。

## 實作順序
1. 先以測試建立有效 token、單一 401、雙重 401、SSO grace、403／500／逾時等請求狀態機。
2. 讓 401 重導路徑可被呼叫端終止或辨識，禁止永遠 pending；保留單次重導守衛。
3. 讓人員頁在重導後重新建立必要請求，或在不可恢復時結束 loading 並顯示訊息。
4. 驗證有效登入不誤重導、Editor／Viewer 既有權限與其他共用 API client 頁面不回歸。
5. 建置、發布測試站並核對 Web／資產／API health、version、test 標籤與瀏覽器實際 API 基底。

## 測試規劃
- Happy Path：有效 Editor 開啟人員頁並顯示清單與頁面權限。
- Boundary：兩支必要 API 同時 401，只重導一次；SSO 返回後只有一次重試。
- Invalid Input／Failure：403、500、逾時、API 離線、環境錯置均結束 loading 並顯示正確訊息。
- Regression：角色與頁面權限不變；其他頁面正常讀取；登出、登入、Portal SSO 與測試／正式環境隔離不變。

## 風險控制
- 此區域為共用登入攔截器，屬高風險；不可與其他 AD／SSO／權限工作同時實作。
- 不以增加無限等待或無上限重試掩蓋 401。
- 未取得正式發布授權前不得更新正式站。
