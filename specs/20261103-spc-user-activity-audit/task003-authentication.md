# TASK-003：SPC登入與登出稽核

- 狀態：進行中；2026-11-03使用者「執行下一項」。
- 需求R-003、R-004、R-010、R-011；基準[契約v1](event-contract-v1.md)，依002儲存基礎。
- 主系統SPC；Portal只提供既有簽章assertion，本件不改其帳密驗證。無新增業務schema；測試API／前端登出呼叫及必要測試環境設定。正式另授權。

## 規格與實作計畫

1. `Audit:AuthenticationEnabled` 預設false；啟用須Auth:Enabled=true、AppEnvironment正確、獨立環境HMAC key、SQL runtime非sysadmin/db_owner、AuditEvents僅SELECT／INSERT且無DDL，startup migration停用並已由部署者完成schema。
2. SSO成功新增不可預測jti；只存由環境＋jti衍生的session hash，不存JWT／nonce／signature。Provisioning與成功事件同AppDbContext／transaction；成功事件保存後才回JWT，稽核失敗rollback並回安全503。
3. 簽章／逾期／重播／身分衝突／停用及未設定等分支保存固定結果與原因；未驗證內容只可HMAC帳號，不冒充已驗證operator。已核對但停用／刪除的帳號可記PortalAssertion快照；profile／email／部門不進Details。
4. 已拒絕／失敗事件採best effort，不改原拒絕；稽核失敗只記安全事件代碼與告警，不寫原始request或例外。SSO配置、簽章與時間邊界保持，避免long timestamp運算溢位。
5. 新增明確Authorize的POST `/api/v1/auth/logout`，從有效JWT取得operatorId／jti；舊JWT無jti可空。這只代表登出請求，現行JWT未提供server撤銷。Viewer只豁免此受限端點及既有AllowAnonymous SSO，不放寬其他寫入。
6. 前端登出以最多1.5秒best effort回報後，finally清除本機token並沿原路徑離開；重複點擊不重複送。自動401清除不當成使用者登出。
7. SQL測試執行身分獨立於遷移身分，保留既有業務表CRUD與現有受控procedure執行，AuditEvents透過SpcAuditWriter DENY變更。環境帳號／憑證不進repo或交付包；備份設定，隔離測試實際權限後才啟用。

## BDD與四面測試

- Happy：Given有效assertion與啟用帳號，When交換，Then成功事件與身分更新同時保存，JWT含jti而事件不含JWT；登出事件session hash與登入一致。
- Boundary：Givenlegacy簽章、無jti舊JWT、重播、±60秒及long極值，When驗證，Then既有結果保持／安全拒絕，不溢位、不重複建帳。
- Invalid：Given偽造帳號／簽章、停用／刪除、衝突、Audit配置或儲存失敗，Then不發JWT；未驗證actor快照空，原文不入事件／log。
- Regression：GivenViewer登出或其他寫入，Then登出可回報，其他寫入仍403；登出稽核失敗不阻止清除與原Portal轉址；Portal既有bearer呼叫正常。
- SQL：隔離runtime不能UPDATE／DELETE／ALTER AuditEvents或建立表；可完成身分交易與事件insert；插入故障回復身分與成功事件，啟動檢查可辨識權限錯置。

## 驗證、發布與回復

待測試；必要build/test通過後直接發測試API／Web，核對實際兩站／共用SPC pool、PMR_SPC_TEST、test標籤及瀏覽器實際API基底。使用隔離來源避免夾帶既有TrendChartView修改。備份API98、Web115、私有設定及SQL；回復舊檔／設定並保留新增事件，必要時停用AuthenticationEnabled，不刪證據。003完成不代表頁面／其他業務操作已串接；004～009保留。

先紅測試確認缺少AuthenticationAudit／logout helper，再實作。主回歸358通過／8略過、另一後端66通過／1略過、身分相容性12通過、前端登出3通過、原驗證啟動2通過；三語2768 keys檢查通過。隔離SQL實際受限principal通過啟動檢查、SSO交易與失敗回復，拒絕audit修改及CREATE TABLE；建立的隔離DB僅合成資料並於finally移除。

實際測試環境核對發現JWT仍是預設值，已告知需要更換及一次測試Portal重登。已準備專用test runtime login與受控私有候選設定，實際權限檢查通過；尚未套用到站台。環境principal／金鑰配置屬部署身分管理，不新增或手動改業務schema。Portal既有呼叫帶Bearer；本件不修改其權杖快取／帳密驗證。切換後原測試Portal session需重新登入取得新JWT。
