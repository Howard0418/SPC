# SPC-AUDIT-1103-TASK-009 測試站整體驗收

## 範圍

- 主系統SPC；只驗證`https://spctest.pmr.com.tw`與`PMR_SPC_TEST`，正式站／正式DB不連線、不發布、不寫入。
- 驗證001～008的登入、進頁、一般查詢、稽核查閱／匯出、代表性高風險操作留痕、遮罩、冪等、故障封閉、環境隔離及保存狀態。
- 測試畫面只截取稽核頁面與必要導覽；帳號、姓名、業務值、query、token及開發工具內容須遮罩，圖片留Git外90日。
- 不重做已完成的正式發布、不啟用retention執行開關、不建立正式發布包；正式發布仍須另行授權。

## BDD

### E2E追溯

- Given 使用者由測試Portal登入並進入SPC
- When 進入稽核紀錄頁、執行查詢與Xlsx匯出
- Then 頁面只在明示權限下可見，事件具有Test環境、可信actor、固定PageKey／EventCode及成功結果，且不含查詢值或敏感原文。

### 故障與拒絕

- Given 無JWT、缺權限、無效filter或稽核保存失敗
- When 呼叫敏感查閱／匯出或關鍵寫入
- Then 分別拒絕401／403／400或安全503／rollback，不回傳事件內容、不放行無痕成功。

### 效能與相容

- Given 相同測試資料與請求
- When 比較關閉／啟用一般活動稽核的讀取與關鍵寫入基準
- Then 一般讀取／進頁p95增幅不超過5%，關鍵寫入不超過10%；若無法建立可信同機基準，不宣稱通過並明列限制。

### 截圖證據

- Given 實際測試站畫面
- When 保存驗收截圖
- Then 遮罩不必要個資與業務內容，移除EXIF，另記SHA-256、Test環境、UTC日期、對應事件ID及90日到期；證據與metadata不進Git。

## 完成條件

- 自動化回歸、實站HTTP／API基底、測試DB唯讀查核與瀏覽器實際畫面均有結果。
- 必要驗收未完成時標示阻擋或待人工，不得將TASK-009標為DONE。

## 測試安全管理者指定

- 使用者於2026-11-09明示指定`ihao_ting`；資料庫唯讀核對為Operator ID 27、Editor、啟用且未刪除。
- 只將27加入測試API私有`Audit:SecurityAdministratorOperatorIds`，正式設定、角色、PagePermissions及稽核資料不變。
- 保留禁止安全管理者替自己新增敏感權限；本次指定不等於授予`admin.audit.read`或`admin.audit.export`。
- 第二安全管理者採既有有效Editor `ID 26/admin`；白名單保留27並追加26，後續由26透過正常網站授權27，不直接更新PagePermissionsJson。
- 因使用者無法登入`admin`，改採其指定且可登入的`ID 1079/Administrator`取代26；最終白名單只保留27、1079，避免保留不可用的額外敏感管理身分。
