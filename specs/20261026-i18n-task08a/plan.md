# 計畫
1. 先建catalog組合／API訊息解析及.NET資源／錯誤／validation測試，記錄紅燈，再實作。
2. frontend src/i18n/api三語模組作單一來源；index與check-i18n共用不覆蓋組合器，主coverage測試改讀合併runtime。backend csproj只embed此模組，嚴格key／參數驗證與單次具名插值。
3. request顯示locale白名單集中處理。既有全域例外response加key/args；保留MVC原InvalidModelStateResponseFactory並包裝其ProblemDetails擴充欄位，不改原error/title/type／status。
4. client附明示locale，已知descriptor按當前locale解譯；保留原API錯誤字串回傳介面與401/403/409、網路與環境隔離既有判斷。先不大量重寫頁面catch或控制器。
5. 無DB Kestrel HTTP測試驗證model field keys／JSON數字／locale隔離；既有例外及授權回歸、前端unit／shell與錯誤情境驗證，不連SQL／Portal／真通知。
6. build後檢查測試後端實際IIS／AppEnvironment／SQL目標及startup flag；只將必要runtime payload更新測試，不覆設定或資料。文件與發布結果同步，僅本批檔案提交上傳。
