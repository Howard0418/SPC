# 後續安全與正式更新工作
- ID：SECURITY-PRODUCTION-FOLLOWUPS-20261020；日期：2026-10-20；狀態：執行中。
- 授權：使用者要求將附件掃毒、多機nonce、共用Runtime評估及已驗證新版正式更新等後續事項執行；附件指定評估Windows Defender，不外送附件。
- 主協調SPC；附件/nonce主系統KM，Portal為SSO相依；Runtime影響共用IIS；正式發布獨立核對。高風險工作不併行。

## 工作池順序
1. SEC-FOLLOWUP-001：主機安全/版本/發布相依盤點及Runtime升級評估（2026-10-21已完成唯讀評估）。本機及兩台正式IIS的Runtime、ANCM、站台、Defender能力、官方修補與回復。
2. SEC-FOLLOWUP-002：KM附件隔離/Defender。先定格式與失敗拒絕契約、測試掃描再實作/測試發布；未掃成功不入正式storage，不變更防毒策略或排除設定。
3. SEC-FOLLOWUP-003：KM共享nonce，評估既有SQL避免新增Redis；一次性原子消費、TTL與雙節點併發，schema/SSO先隔離演練。
4. SEC-FOLLOWUP-004：Runtime/Hosting Bundle實際升級，依001結果建立逐機維護及回復，與網站發布分開；原事項為更新評估，不以評估完成假稱已安裝。
5. SEC-FOLLOWUP-005：KM正式1.6.6或後續已驗證版，保留附件/角色/設定。
6. SEC-FOLLOWUP-006：SPC/Portal最新已驗證版正式更新，先比對schema、policy、writer權限與API代理相容性。必要migration具體另核准，禁止覆蓋啟動未知migration。

## BDD及測試
- Given三主機 When唯讀盤點 Then記錄版本/服務/非機密路徑；連線失敗明記未知，不輸出密碼/金鑰/業務文件。
- Given相依未完成或未知migration When發布前檢查 Then不覆寫正式站，明列缺項。
- GivenRuntime評估 When核對官方來源 Then分清套件/shared runtime/ANCM及x86/x64；網站備份不能替代runtime回復。
- Happy三機可達；Boundary本機32位/64位IIS；Invalid服務不存在/目標錯；Regression只讀不停止站台、無DB寫入。

## 任務與回復
- [x] 001盤點/官方來源/決策/驗證，見[verification.md](verification.md)。
- [ ] 002附件；[x] 003nonce；[ ] 004runtime實施；[ ] 005KM正式；[ ] 006SPC/Portal正式。
- 每次只執行一件。001純文件回復為revert，build/發布不適用；其餘各自先建詳細規格及驗證再實作，不標示未做項目完成。

## 2026-10-21 001結案/需求比對
- 三主機盤點與官方修補/Hosting維護決策完成，001為文件型DONE；未安裝、未停站/改策略、未寫DB。當時其餘002～006共5件未完成，不自動開始下一件。
- 本機實際登錄WithSecure Client Security Premium且相關服務運行，與先前「沒有其他防毒」答覆不同；不否定使用者已知情況，以實際唯讀證據補正，Defender仍Stopped，不能逕行啟用。
- 002保留「附件隔離/掃描」需求，但先確認現有WithSecure受支援掃描能力及維運管理方式；Defender只有在相容及管理方式確認後才評估，不更改排除/策略。
- Runtime與ANCM及應用套件分開；目前官方8.0.32/10.0.13，8及9於2026-11-10到期。Portal major版本遷移屬另案評估，不能以安裝10代替net8相容性修正。
- 正式Portal公告hotfix API1.0.182.1/Web225已完成；並非006所有新版正式更新完成。測試Portal190/233之SMTP修復/實際收件已驗收，由Portal文件追溯。
## 2026-10-21 003完成測試站
- 使用者確認先做003，002因無法確認不外送政策受阻；SQL共享nonce/60秒DBUTC窗口/121秒TTL與失敗拒絕完成，81項及真實Portal入口SSO通過，023測試庫備份驗證後套用，KM-Test1.6.7。
- [KM驗證與回復](C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/KM/specs/20261021-shared-sso-nonce/verification.md)；不等於整套KM已完成多機部署，Cookie/共享storage仍需部署規劃。正式未動。
- 目前剩4件：002受阻、004/005/006待執行。不自動開始下一件。
