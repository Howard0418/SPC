# 正式啟用續作

- 2026-11-23 使用者授權正式 Portal 新增製程角色並指派 ihao_ting、授權啟用正式 SPC 稽核。四類收件名單後續指定。
- Given 正式帳號唯一且有效，When 執行受控角色異動，Then 只新增製程角色與指定關聯，保留其他原角色，重送無新增。
- Given 正式稽核須啟用，When 啟動前檢查目前 SQL 執行身分，Then 只有使用通過現行安全條件的受限身分才可切換開關。保留現行金鑰、站台與工作階段，直到前置條件就緒。

## 實際結果

- 正式 Portal 確認帳號唯一、啟用且為人員。PMR_PORTAL_UAT 先完成 COPY_ONLY、CHECKSUM 與 RESTORE VERIFYONLY，再以單一交易建立一筆製程角色及一筆 ihao_ting 關聯，原六筆角色關聯保留。交易後獨立查核關聯為一筆。
- 角色回復可精準撤銷該筆關聯；製程角色只有在無其他關聯時才可移除。保留交易前資料庫備份。
- 指定帳號正式資料目前無有效 Email。Portal 管制值異動通知方式與 SPC 四類收件人皆留待指定，無試寄。
- SPC 正式啟動預檢目前 SQL 執行身分為 sysadmin 與 db_owner，SpcAuditWriter 無成員，且現況需停用啟動資料庫初始化並配置正式 HMAC 金鑰。現行啟動安全檢查會拒絕此身分。保持稽核開關、連線及正式金鑰設定，待授權專屬最小權限執行帳號與正式切換。
- 本次僅 Portal 角色資料異動。兩站 Web/API 版本保持，Chromium 核對前台、主要資產及 production API 通過。

## 正式稽核切換授權與驗證

- 2026-11-23 使用者另行授權建立正式 SPC 最小權限 SQL 執行帳號、隔離驗證及正式切換。
- Given 正式驗證執行身分無伺服器管理、DDL、稽核更新與刪除權限，When 以實際新連線檢查既有主檔讀取、業務寫入權限及稽核儲存，Then 全部符合已驗收契約，且可在不啟動完整正式服務的情形下完成。
- 啟用前保護原設定與前台入口，保留原 JWT 與 Portal SSO 金鑰。發版後驗證前台新版、實際 API、匿名保護與稽核落地。回復同批設定及前台入口，保留既有事件。

## 正式稽核啟用結果

- 正式 Web 0.1.146 → 0.1.147，API 0.1.139/production。正式 API 設定切換至專用最小權限執行身分，停用 startup DB 初始化，啟用登入與瀏覽／操作稽核。JWT 與 Portal SSO 既有金鑰維持。HMAC 金鑰與 SQL 認證只在正式主機生成並保存於受保護設定。
- PMR_SPC_2026 已完成 COPY_ONLY／CHECKSUM 與 RESTORE VERIFYONLY。有限權限身分為 SpcAuditWriter 成員、無 sysadmin／db_owner／DDL、稽核 SELECT／INSERT 可用，UPDATE／DELETE／ALTER 拒絕。既有應用表以物件級授權，稽核封存與 migration history 僅讀。
- 正式前隔離執行實際 API 啟動檢查，pending migration 0，主檔與人員讀取、業務更新權限及稽核事件交易寫入／回復通過。初次在隔離設定組裝階段失敗，修正後重跑通過；當時正式設定尚未切換。
- Chromium 核對前台版本、主要 JS／CSS 的 200 與 MIME、瀏覽器實際使用的 8443 API health／version 與 production 標籤。正式匿名 SSO 無效請求回 401，依唯一 TraceId 查得一筆不含帳號的 Production 稽核拒絕事件；匿名活動稽核仍回 401。正向 SSO 與實際業務操作稽核待真人驗收。
- 前台隔離來源 56893cf3，回復點 SPC-PROD-1123-AUDIT-WEB147。可還原原設定與前台入口並 recycle SpcApi，已落地事件保留。後續正式備份與 migration 使用獨立部署身分，應用執行帳號維持最小權限。
