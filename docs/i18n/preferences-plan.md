# Portal本人語言偏好提案 v1（尚未實作）
2026-10-24，I18N-TASK-03；依[共用locale契約](contract.md)。僅規劃，不新增API/表/Migration或修改SSO/帳號/角色。
## 1. 來源證據與本人身分
| 來源 | 已觀察現況 | 設計限制 |
|---|---|---|
| Portal Api/Entities/User.cs、AuthController.BuildClaims | Users.Id、AdAccount、可空AdObjectGuid；cookie NameIdentifier=Users.Id | 偏好以Portal Users.Id為唯一FK，AD改名不搬偏好 |
| Portal AuthController登入與LocalPortalAccount | local_guard亦掛在Users，另外有account_type claim；停用User拒絕登入 | 共用警衛帳號不是每位實際操作者，不能假稱個人持久化 |
| Portal AuthController.Me | 由NameIdentifier查Users | 新端點獨立確認目前User存在/啟用，不只信任舊cookie或前端回傳/me內容 |
| SPC AuthController.CreateTokenResult | JWT NameIdentifier=Operators.Id，Name=normalizedUsername | 不等於Portal UserId；不接受作Portal cookie或跨站userId |
| Portal SpcSsoTokenService/KmSsoTicketService | signed normalized帳號/角色等現有交換，未傳Portal UserId/AD GUID/locale | 不能假稱已有穩定跨站偏好claim；不更動簽名格式/nonce |
| KM KmSsoTicketValidator | 驗證簽名/nonce後建立PMR\account及角色 | 此task未建立至Portal Users.Id的委派映射 |
- 來源只讀程式，未查實庫、實際cookie/session或帳號資料，不宣稱所有AD GUID/帳號唯一性已驗證。
- Portal本地偏好端點以受信cookie NameIdentifier解析int後查啟用Users；不得由query/body/header傳userId/adAccount/employeeNo來選對象，未知欄位拒絕。連admin也只改本人。
- 個人AD/NFC登入若最後落在同一Users.Id，採同一偏好。姓名/Email/工號不作主鍵；AD GUID可幫助日後重整帳號映射，但現有claim未提供，不憑空依賴或猜測。
- 偏好無法讀寫不能增加SPC/KM角色或繞過文件ACL；新偏好沒有獨立權限角色需求。
## 2. 建議資料模型（待TASK-09具體核准）
首選新增Portal `UserLocalePreferences`，而非把偏好塞入AD/SMTP/SSO設定或以帳號字串作key。獨立表讓舊User匯入/同步不用承擔語系欄位生命週期，且無資料時自然回預設。
| 欄位 | 提案SQL型別/規則 | 用途 |
|---|---|---|
| Id | int identity、PK | 沿用新表命名規範 |
| UserId | int、NOT NULL、FK Users.Id、UNIQUE | 一位Portal使用者最多一筆 |
| Locale | nvarchar(10)、NOT NULL | 只允精確zh-TW/zh-CN/en-US |
| CreatedAt | datetime2、NOT NULL、UTC | 首次保存時間 |
| UpdatedAt | datetime2、NOT NULL、UTC | 最後實際變更時間 |
| RowVersion | rowversion、NOT NULL | optimistic concurrency與ETag；不是時間戳 |
- Locale需應用allowlist與DB CHECK雙重驗證；SQL預設可能不分大小寫，因此具體migration須明確binary比較並限制DATALENGTH為合法5字元，避免大小寫/尾端空白在DB比較中被默認放行。
- FK刪除採Restrict/NoAction，停用使用者不刪偏好；不要在帳號刪除流程隱含新增cascade。若未來真要刪帳號，需另行決定偏好清理策略，不由本Task順手修改。
- 初次migration不回填、不建全員預設列、不修改Users現有資料；沒有偏好列即null/zh-TW。若已存在同名物件/類似欄位，先停止審查，不手動覆蓋。
- 可替代方案是Users新增nullable PreferredLocale，較少表但會碰User同步/更新範圍；本提案不採兩套同時寫入，也不實作雙向同步。
## 3. Portal本人API契約提案
下列端點不存在於本次交付；回傳沿共用success/data/message並可增code/messageKey/args。舊auth/me/登入DTO不移除欄位。
| 操作 | 路徑/驗證 | 行為 |
|---|---|---|
| GET | /api/me/locale，既有Portal cookie | 回preferredLocale（無列null）、effectiveLocale、source=user/default；讀取不建立資料列/修復資料 |
| PUT | /api/me/locale，body僅locale字串 | 精確allowlist，不接受null/alias/額外userId；保存成功回effective/locale及新ETag |
| DELETE | /api/me/locale，無body | 清除本人偏好回204，之後GET回null/zh-TW；不是刪除User |
- 所有回應Cache-Control:no-store；ETag對應RowVersion，視為opaque字串且不可當授權token。無列GET沒有ETag。
- 首次PUT用If-None-Match:*；已有列PUT/DELETE用If-Match:目前ETag。缺寫入前置條件428；版本過期/同時建立衝突412；不吞掉並發例外改無條件覆寫。
- 已有列PUT相同locale仍要驗證ETag，成功可維持原RowVersion/UpdatedAt；不存在時DELETE回204。DELETE並發列已更新須412，不刪掉別台剛存的值。
- 未登入401；已驗證但User不存在/停用或不符合本人成員條件403；locale/額外欄位/不合法前置header 400；DB暫不可用503安全提示。未知locale寫入不默默存繁中。
- 若既存Locale無效，GET回effectiveLocale=zh-TW及安全診斷、不在GET偷偷改庫，保留revision使使用者能明確修正；此情境需專用錯誤/測試，不假定CHECK永遠不可能被歷史資料繞過。
- 共用local_guard提案先只允session本地語言，持久化PUT/DELETE回403 code=PERSONAL_LOCALE_UNAVAILABLE，不讓一位操作者更動所有警衛畫面；是否要帳號級共用需TASK-09前確認。判斷以已驗證account_type與User資料，不信前端宣稱Person。
## 4. Cookie/CSRF與相容邊界
- Portal Web既有same-origin /api proxy及UseCookies=false handler保留；每個request轉送目前cookie/必要防偽資訊，不在共享HttpClient DefaultRequestHeaders或全域Culture塞使用者語言。
- Cookie驗證的PUT/DELETE必須有適用Web/API proxy的anti-forgery驗證與可信Origin檢查；只用SameSite=Lax或Content-Type不足以宣稱CSRF完成。具體token取得/轉送/伺服器驗證由TASK-09接線並測試，拒絕跨使用者/跨來源請求。
- 不放寬CORS、不設定父網域共用cookie、不降低SameSite/憑證驗證、不接受SPC JWT/任意UserId直接寫Portal；不新增共用高權限服務帳號。
- 操作可記UserId/時間/locale/結果code，禁止記錄cookie/token/密碼；不把譯文當業務值。語言更新不能觸發SSO登入、角色同步、郵件或資料重算。
## 5. 客戶端優先序、快取與失敗
1. 同一登入工作階段的明確選擇立即更新UI（語系resource已存在前提），保留表單/焦點/查詢/圖表狀態；保存中顯示pending。
2. 登入/入口取得本人伺服器偏好；若請求晚於新的明確選擇才返回，不得覆蓋新選擇。成功保存後刷新ETag與本人快取。
3. 伺服器無偏好時可使用相同登入身分/同環境下的合法快取，最後zh-TW；但已登入身分切換時不把匿名或前使用者快取自動上傳新帳號。
4. 快取須同時綁application、Test/Production及已驗證subject（各系統自己的本機subject，不當作Portal id）。未登入用獨立anonymous key，登出清掉pending/本人快取引用，不與下一人沿用。
5. 保存失敗保留本頁語言與未送出業務資料，標「尚未同步」並可重試；GET失敗可退同身分快取/繁中，不把失敗當不存在。401/403提示需重新驗證/無法持久化，不自動清空表單或靜默標saved。
6. 412先重新GET最新revision，不自動覆寫對方選擇；提供採用伺服器/保留本頁並再確認保存。只有使用者確認才以新revision重送，避免兩分頁互相洗回。
- TASK-04 SPC先本機偏好必須標為本機，不冒稱帳號持久化；跨已開分頁推送/離線佇列/排程通知語言不是本次功能。
## 6. 跨站階段與未決決策
- Portal端點只能辨識Portal cookie本人；SPC/KM各自憑證與origin不同。不能宣稱從SPC fetch Portal(cookie)或傳本地JWT就完成三站帳號偏好。
- TASK-09只需先完成Portal本人持久化。TASK-10/15及19要另外審查安全的每人委派/入口偏好承載：須綁audience/subject、短效/防重放、依既有登入唯一映射Portal帳號並只允本人locale，不擴大權限。
- 以上是候選信任條件，不是本Task建立新ticket協定的授權；現有SSO沒有Portal UserId/GUID/locale，若需加claim或新端點，須在單一後續Task先訂明確契約/相容測試。未通過前SPC/KM維持本機語言，不把可編輯帳號字串當已驗證跨站映射。
- 身分對不到/多筆時不得用顯示姓名或工號猜對應，保留本機語言並安全診斷；不新增跨站DB直讀/共用sa連線。
## 7. Migration、發布與回復提案
- TASK-09實作前唯讀核對目標schema/Users FK與既有遷移歷史、共用帳號政策；以EF Core Migration產生，含註解/約束/unique/FK/rowversion審查，不手工改正式表。
- 測試庫或隔離複本先Up、重跑、舊版app相容/Down與資料保留決策、無效locale/重複UserId/FK/並發驗證；不能以SQLite替身聲稱SQL Server rowversion/約束已過。
- 有備份且必要測試通過後僅測試站發布；正式需具體schema及應用授權，核對實際環境，不自動套用到正式。
- 回復應用時保留新增偏好表/資料，舊應用忽略它即可；不要自動Down/drop或整庫還原，避免刪掉發布後偏好。若要撤表另行審查/匯出/授權。
- 語系JSON/應用成對回復，缺locale仍繁中fallback；絕不因回復偏好而回復角色、密碼/金鑰或量測資料。
