# TASK-001 執行紀錄
- 2026-10-28；狀態：契約與工具來源環境防護已完成測試，實際主機／帳號與正式HTTPS核對待確認，001未結案；002～008未開始。

## 已完成
- 已比對[原規劃](../20261028-transfiles-production-schedule-plan/spec.md)、connection_settings／connection_test、Portal帳密登入、ChemicalFormulaScheduleController及ChemicalDirectWriterSqlV1的actor／USER_NAME要求，寫成[正式目標／身分／最小權限契約](identity-contract.md)。沒有預先認定gMSA或既有個人測試帳號可供正式無人值守使用。
- 新增4個隔離測試方法、12個正反情境，舊碼出現12個失敗；來源修改後全部通過。現有GUI測試登入函式現在先匿名GET SPC `/api/version`，環境不符／缺失／非字串／未知／無法取得時，在Portal client建立／login／token之前拒絕。
- 正向test與production fixture維持Portal登入→SPC token→CHEM唯讀主檔；version請求headers無Authorization或帳密。不是新增正式寫入入口，也不以version環境當DB／角色／授權證據。
- 回歸：test_connection_settings／test_tls_context／test_etch_upload／test_batch_convert_gui／test_chemical_direct_writer共61通過；既有GUI故障回呼4通過（4階段×3類網路錯誤，安全提示／按鈕恢復），新增環境防護4通過。合計69項，相關py_compile通過。
- 既有GUI故障測試fixture改成先回version成功，再於所測階段失敗；新增環境核對失敗情境，並核對只發出version GET／不登入。不核銷舊EXE人工故障驗收。
- TransFiles非Git repo，實際兩檔變更保存為[最小patch](../../integrations/transfiles/production-identity/README.md)；獨立來源副本通過apply-check、apply及UTF-8／LF正規化逐字比較。CRLF來源及patch換行差異已處理，不以無法apply的產物交付。

## 尚未完成／實際環境限制
- 已詢問使用者正式執行主機及Windows／網域帳號，目前待回覆。沒有實際帳號就不能查證Portal／SPC／SQL actor一致性、最小角色及無人值守認證；此部分保留待辦。
- 使用合成輸入、Mock Portal factory及現有TLS helper，只匿名GET已知正式SPC HTTPS `/api/version`，測試誤選test應在登入前拒絕。初次沙箱未取得可確認回應；允許網路後實際HTTP406，沒有取得environment，因此只確認未知／失敗時未建立Portal client，不宣稱真實錯置環境已回production或正式登入成功。
- HTTPS406原因／核准對外profile仍需後續釐清；本件不改IIS、Proxy、TLS驗證、網址設定或正式角色來繞過。既有本機正式URL空白仍保留。
- 不建立帳號、角色／grant、資料庫連線、prepare／preview／confirm或Windows工作；沒有讀取／保存真實帳密與權杖。69測試均mock／暫存，不是正式正向寫入或排程驗收。

## 進度、交付與回復
- 原進度表已同步：TransFiles第5階段摘要／同表G29與S29、222列001狀態「進行中，待身分確認」及日期2026/10/28，新增藍色原生來源測試標記312；原25個物件及其他專案儲存格保持。
- 核心未完成仍34、完成171、登錄221、人工11／核銷4；原表結構／列印範圍保持。ZIP、OpenXML SDK0錯誤、Artifact重開計數及公式錯誤0、LibreOffice重開／12頁列印通過，未宣稱Microsoft Excel人工驗收。
- 備份Voice/備份/進度表_轉檔001身分核對前_20261028-145931.xlsx；原路徑SHA256 20F8901F4AAD1D2FAE6C94E39B16CC02B7A0FED58E82FBEFFC0BE44132951F0D。取代前核對未變，原路徑再讀及OpenXML0錯誤；需回復先核對沒有後續更新。
- 程式回復：使用本機.codex-tmp/transfiles-identity-20261028的兩個before.py，或核對無後續修改後反向patch；不刪資料／改角色。舊EXE未覆蓋、未重新打包，008才處理打包／正式驗收。
- SPC／Portal沒有產品程式變更，網站build／測試站與正式發布不適用。工具來源已實作不等於舊EXE已更新；文件與來源patch依本件提交上傳。
