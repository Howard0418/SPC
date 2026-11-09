# 驗證紀錄

狀態：部分完成；T3、T4受阻，TASK-009仍列未完成。

## 已完成

- 完整後端回歸531通過、11個既有明示隔離測試略過、0失敗；稽核查詢前端2項通過，測試版三語系build通過。
- 測試站首頁、目前JavaScript／CSS、`/api/health`、`/api/version`皆成功且MIME、版本及`test`環境正確；瀏覽器實際API基底為同源`/api`，匿名稽核查詢回401。
- `PMR_SPC_TEST`唯讀核對201筆AuditEvents：環境錯置0、Details格式錯誤0、敏感關鍵字命中0、ClientEventId重複0；高風險主檔新增／刪除2筆仍可追溯。封存批次、封存事件及legal hold目前皆0，符合執行開關停用狀態。
- 失敗封閉、401／403／400／503、交易回復、遮罩、冪等及環境隔離由既有自動化測試覆蓋並通過。

## 未完成與原因

- T3：測得測試站目前30次請求p95為首頁6.9ms、health 15.6ms、version 7.9ms；但沒有同一主機、同一資料量、關閉稽核的可信基準，不能判定讀取增加不超過5%／關鍵寫入不超過10%。
- T4：Chrome已登入並停留`https://spctest.pmr.com.tw/spc`，操作工具可列出頁籤但重設後仍無法讀取DOM或截圖；因此未執行登入後稽核頁查詢／匯出畫面驗收，亦未產生遮罩截圖。資料庫對應的`audit.events.query`／`audit.events.export`為0，不能以自動化結果冒充實際畫面操作。
- 因T3、T4未通過，TASK-009不標DONE，核心未完成42、SPC相關18、完成196維持不變。

## 測試安全管理者指定

- 使用者指定`ihao_ting`；唯讀核對為Operator ID 27、Editor、啟用且未刪除。測試API私有設定原本沒有安全管理者屬性，首次更新安全失敗且設定SHA-256保持；工具補上缺值處理後，`Audit:SecurityAdministratorOperatorIds=[27]`驗證通過。
- 更新前私有設定備份為`release/test/backend/appsettings.backup-security-admin-20261109.json`；只回收`SpcSingleTest`，API 0.1.111、health ok、環境test。正式設定與正式站未操作。
- 既有禁止自我升權保持：指定27為安全管理者不等於授予`admin.audit.read`／`admin.audit.export`，仍須另一位白名單安全管理者替27授權，故稽核頁面驗收仍阻擋。
- 原進度表已更新，備份為`備份/進度表_20261109_ihao安全管理者指定前.xlsx`；更新後SHA-256為`B0F89BB37C58CD1A3CBF2938CC27515A5E918F411E71A3850AE9EBA169EB942A`，OpenXML 0錯誤、原生drawing 1保持、重開與兩處渲染通過。Microsoft Excel人工開啟未執行。
- 依使用者「執行」追加`ID 26/admin`為第二安全管理者。首次工具將27與26誤作數字加總成53；API未重啟、錯誤未生效，立即由`appsettings.backup-security-admin-26-20261109.json`恢復原雜湊與ID27。修正陣列合併及整數驗證後，回歸確認`26,27`，只回收SpcSingleTest，API 0.1.111／health ok／test。
- 白名單完成仍不直接改PagePermissions：後續須由27登入替26授予read／export，再由26登入替27授予read／export；其後重新登入27完成稽核頁面E2E。
- 原進度表同步雙安全管理者狀態，更新前備份`備份/進度表_20261109_雙安全管理者前.xlsx`，更新後SHA-256為`781A48B94580E8A0CCE9BF51665CF5AD5280F05E0BAF1E6258C1FC4372AC64B6`；OpenXML 0錯誤、原生drawing 1保持、重開與兩處渲染通過。

## 發布與回復

- 本件沒有產品程式、schema或業務資料變更；只更新測試API私有安全管理者設定並回收測試集區。正式站／正式資料庫未連線、未發布、未異動。
- 原進度表只更新TASK-009為「部分完成／外部阻擋」，不核銷待辦。更新前備份為`備份/進度表_20261109_TASK009部分完成前.xlsx`，原檔取代後SHA-256為`75883DCE7DFAAF84A1CFE11C9E972F170B112245AF731742498C62DD98386180`；OpenXML 0錯誤、ZIP必要part及原生drawing 1保持，重開與兩處渲染通過。Microsoft Excel人工開啟未執行。

正式站／正式資料庫：未授權，不操作。
