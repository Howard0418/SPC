# 計畫

1. 先擴充後端契約測試：固定ActionKey、成功／失敗、敏感排除、偽造拒絕及ClientEventId冪等。
2. 擴充受限活動API與安全查詢摘要，不新增任意Details欄位；以migration只放寬既有ClientEventId check constraint的固定EventCode。
3. 建立前端URL／method白名單解析器，在Axios成功及具HTTP回應的失敗後送出安全事件；排除GET、稽核端點與未列入API。
4. 活動頁及三語標籤顯示「送出業務操作」與ActionKey；新增前端純函式測試。
5. 遞增API／Web版本，跑聚焦與必要回歸、build；測試DB備份驗證後套migration，再發布SPC測試站並驗證Web／資產／同源及直連API。正式不發布。
