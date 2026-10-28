# TASK-001 正式目標、身分與權限契約
- 工作TRANSFILES-PROD-1028-TASK-001；版本1；執行中。使用者2026/10/28回覆「好，開始」，依[8件計畫](../20261028-transfiles-production-schedule-plan/tasks.md)僅開始001，不自動進002。
- 主系統TransFiles，相依SPC／Portal；保留既有來源與EXE。此件不建立帳號／正式角色、不改DB或正式profile、不移除三類測試守衛、不匯入／排程／發布網站。

## 現況與最小實作
- connection_test目前只核對設定URL格式，再登入Portal取得SPC token並讀主檔，未先核對SPC實際environment。增加匿名GET `/api/version`核對，必須在Portal登入／取得token前完成；錯環境、缺欄、非JSON結構或讀取失敗即停止，不帶帳密或Authorization至環境查詢。
- 這是現有GUI「測試登入」實際呼叫的來源函式，不新增未使用的平行框架；舊EXE未重包，不能宣稱其已包含新防護。
- 三類正式身分／權限矩陣與阻擋條件寫入[契約](identity-contract.md)。目前Portal帳密登入及化學DB actor綁定不足以自行推定無人值守方案，實際主機與帳號已向使用者詢問；待確認前不授權或冒稱正式預檢通過。

## BDD與測試
- AC-001 Given設定test或production，WhenSPC version回報不符／缺失／失敗，Then Portal client／login／token／主檔讀取都不執行；錯誤顯示安全階段，沒有preview／confirm。
- AC-002 Givenversion環境一致，When測試登入，Then維持既有Portal→token→CHEM唯讀主檔順序；環境查詢不帶機敏標頭。
- AC-003 Given正式排程身分尚未指定或與API／DB actor不一致，Then契約列為未通過，不建立執行權限、不自動換管理者憑證；實際身分驗證未執行時001保持未完成。
- Happy Path：兩種環境正向；Boundary：大小寫／空白／空回應、正式URL空白；Invalid：錯環境、無效型別及GET失敗；Regression：TLS、既有登入錯誤安全分類、無網路mock／無資料寫入。
- 核心先新增測試確認舊碼失敗，再修改；必要py_compile／相關unittest。無網站變更，SPC測試發布不適用；打包屬008，不提前執行。

## 任務
- [x] 契約與既有環境／身分限制核對。
- [x] 環境錯置先拒絕測試與來源修正。
- [ ] 確認執行主機／帳號及非互動認證可行性；未完成不結案。
- [x] 文件、原Excel狀態與原生記錄同步，依本件提交上傳。
- 結果：[執行紀錄](verification.md)。001仍待實際身分與正式HTTPS核對，不標DONE。
- rollback：保存修改前檔案與最小patch，回復本件connection_test與其測試；不改舊EXE、Windows角色或任何資料庫狀態。
