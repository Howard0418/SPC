# TASK-03驗證（2026-10-24）
- AC-001/005：唯讀來源確認Portal cookie NameIdentifier=Users.Id，SPC JWT=Operators.Id，KM現有signed account/nonce；不能等同三站整數Id或用姓名/工號猜本人。Portal本機警衛亦有User但屬共用帳號政策風險，已分開記錄。
- AC-002/003：文件提出精確locale allowlist、本人GET/PUT/DELETE、CSRF/Origin/只本人限制及獨立UserLocalePreferences表（PK/FK/unique/CHECK/UTC/rowversion）；僅schema提案，未建立實體/EF Migration/SQL或查實庫。
- AC-004：列明本次明確選擇/本人伺服器偏好/合法同身分快取/繁中順序；匿名/換人/Test與Production隔離，保存失敗保留本頁與pending，ETag412不自動覆寫。
- 契約範例13項經本機JSON讀取、唯一caseId/路徑/method/status/locale/no-error-write/預設回應一致性檢查。這是靜態提案完整性，不是API/CSRF/並發/DB測試通過。
- 後續驗證規劃涵蓋Happy/Boundary/Invalid/Regression，包括真SQL Server rowversion/CHECK/FK/Up重跑、cookie/CSRF整合、多使用者/BOLA、UI資料保留及回復；均未執行，不以替身冒稱真DB驗證。
- 未決：local_guard帳號級偏好政策、SPC/KM安全委派與唯一subject映射需TASK-09/相關接線前決定；不阻擋TASK-03文件交付，也未視為相關實作核准。
- AC-006：僅SPC文件與參考JSON，未改Portal/SPC/KM執行碼、資料庫、角色/AD/SSO/IIS/套件；build/應用測試/測試站及正式發布不適用。文件內容/相對連結及git diff檢查通過。
- TASK-03 DONE，國際化TASK-04～21剩18包；附件掃毒受阻1、Runtime/TransFiles人工故障驗收暫緩2不變，沒有開始下一項。
