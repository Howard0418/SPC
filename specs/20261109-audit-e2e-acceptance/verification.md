# 驗證紀錄

狀態：部分完成；T3已完成，T4受瀏覽器服務阻擋，TASK-009仍列未完成。

## 已完成

- 完整後端回歸531通過、11個既有明示隔離測試略過、0失敗；稽核查詢前端2項通過，測試版三語系build通過。
- 測試站首頁、目前JavaScript／CSS、`/api/health`、`/api/version`皆成功且MIME、版本及`test`環境正確；瀏覽器實際API基底為同源`/api`，匿名稽核查詢回401。
- `PMR_SPC_TEST`唯讀核對201筆AuditEvents：環境錯置0、Details格式錯誤0、敏感關鍵字命中0、ClientEventId重複0；高風險主檔新增／刪除2筆仍可追溯。封存批次、封存事件及legal hold目前皆0，符合執行開關停用狀態。
- 失敗封閉、401／403／400／503、交易回復、遮罩、冪等及環境隔離由既有自動化測試覆蓋並通過。
- T3同機效能基準完成：同一候選API、同一`PMR_SPC_TEST`以稽核開／關雙程序交替量測100次讀取及30次關鍵寫入，共3輪。讀取新增延遲為0.384／0.166／-0.170ms，關鍵寫入新增延遲為5.316／9.572／12.729ms，均通過低延遲基準的絕對門檻（讀取新增不超過5ms且總p95不超過20ms；寫入新增不超過25ms且總p95不超過50ms）。
- 針對基準發現的額外成本，以先紅後綠測試修正：非稽核讀取提前短路；可信Operator、業務資料與AuditEvent改為同一交易／同一次`SaveChanges`，稽核插入失敗仍整筆rollback並回503。35項焦點測試、113項稽核回歸通過，另3項SQL隔離測試依目前環境明示略過；Release build 0錯誤。
- 測試API 0.1.116已發布並完成Web首頁、目前JS／CSS MIME、health、version、test環境、同源API基底及匿名401驗證。以實際測試API新增並刪除唯一Part，業務暫存0且精確刪除稽核事件1筆、可信Operator 27、成功結果一致。正式站／正式資料庫未操作。

## 未完成與原因

- T4：Windows操作工具的原生管線及內建瀏覽器服務均不可用，重設／重試仍無法開啟或讀取登入後頁面；因此未執行登入後稽核頁查詢／匯出畫面驗收，亦未產生遮罩截圖。不能以API或資料庫結果冒充實際畫面操作。
- 因T4未通過，TASK-009不標DONE；目前SPC未完成小工作11件，數量不因本次部分完成而減少。

## 測試安全管理者指定

- 使用者指定`ihao_ting`；唯讀核對為Operator ID 27、Editor、啟用且未刪除。測試API私有設定原本沒有安全管理者屬性，首次更新安全失敗且設定SHA-256保持；工具補上缺值處理後，`Audit:SecurityAdministratorOperatorIds=[27]`驗證通過。
- 更新前私有設定備份為`release/test/backend/appsettings.backup-security-admin-20261109.json`；只回收`SpcSingleTest`，API 0.1.111、health ok、環境test。正式設定與正式站未操作。
- 既有禁止自我升權保持：指定27為安全管理者不等於授予`admin.audit.read`／`admin.audit.export`，仍須另一位白名單安全管理者替27授權，故稽核頁面驗收仍阻擋。
- 原進度表已更新，備份為`備份/進度表_20261109_ihao安全管理者指定前.xlsx`；更新後SHA-256為`B0F89BB37C58CD1A3CBF2938CC27515A5E918F411E71A3850AE9EBA169EB942A`，OpenXML 0錯誤、原生drawing 1保持、重開與兩處渲染通過。Microsoft Excel人工開啟未執行。
- 依使用者「執行」追加`ID 26/admin`為第二安全管理者。首次工具將27與26誤作數字加總成53；API未重啟、錯誤未生效，立即由`appsettings.backup-security-admin-26-20261109.json`恢復原雜湊與ID27。修正陣列合併及整數驗證後，回歸確認`26,27`，只回收SpcSingleTest，API 0.1.111／health ok／test。
- 白名單完成仍不直接改PagePermissions：後續須由27登入替26授予read／export，再由26登入替27授予read／export；其後重新登入27完成稽核頁面E2E。
- 原進度表同步雙安全管理者狀態，更新前備份`備份/進度表_20261109_雙安全管理者前.xlsx`，更新後SHA-256為`781A48B94580E8A0CCE9BF51665CF5AD5280F05E0BAF1E6258C1FC4372AC64B6`；OpenXML 0錯誤、原生drawing 1保持、重開與兩處渲染通過。
- 使用者指定可登入的`Administrator`取代無法登入的`admin`；唯讀確認為ID1079、Editor、啟用。精確白名單工具首次因比較語法在寫入前失敗，設定雜湊保持；修正後白名單為27、1079並移除26，只回收SpcSingleTest，API 0.1.111／health ok／test。
- 原進度表同步替換結果，更新前備份`備份/進度表_20261109_Administrator替換前.xlsx`，更新後SHA-256為`FE23FC621B3AFB01CE230BB62059939E91F07A27EAD663BFB44414246DF81A02`；OpenXML 0錯誤、原生drawing 1保持、重開與兩處渲染通過。

## 發布與回復

- 本次新增稽核效能與交易原子性程式修正，僅發布測試API0.1.116；無schema變更。測試用唯一Part已刪除且暫存0。正式站／正式資料庫未連線、未發布、未異動。
- 測試發布備份為`release/test/backend.backup-audit-performance-20261110`；回復時停止`SpcSingleTest`、以該備份回復backend並重新啟動，再驗證health／version／test環境及Web實際API基底。
- 原進度表已同步效能完成／登入畫面待驗狀態，更新前備份`備份/進度表_20261110_TASK009效能同步前.xlsx`；更新後SHA-256為`689C3E5100707CDB2E42D7A037D174F84BC5EEAD6E6AEF2034402D36046B965D`，Artifact重開、公式錯誤0、原生里程碑與兩處渲染通過。
- 原進度表只更新TASK-009為「部分完成／外部阻擋」，不核銷待辦。更新前備份為`備份/進度表_20261109_TASK009部分完成前.xlsx`，原檔取代後SHA-256為`75883DCE7DFAAF84A1CFE11C9E972F170B112245AF731742498C62DD98386180`；OpenXML 0錯誤、ZIP必要part及原生drawing 1保持，重開與兩處渲染通過。Microsoft Excel人工開啟未執行。

正式站／正式資料庫：未授權，不操作。
