# 驗證紀錄

狀態：測試站完成，待使用者登入人工驗收；正式未發布。

## 自動驗證

- 後端 `NavigationReadAuditTests` 9/9；稽核聚焦回歸122通過、3項需獨立SQL設定而略過、0失敗。
- 前端白名單、敏感排除、稽核標籤與本次三語審查6/6；i18n 2897鍵×3語通過。
- API Release build／publish及Web test build通過。完整舊i18n master ledger仍有既有未登錄鍵與既有畫面文字雜湊差異，未把歷史問題納入本件修改。
- 白名單只接受固定Method＋路由組合；GET、任意write、稽核端點、query值與body均不產生動作摘要。後端再驗ActionKey、PageKey、Outcome、Method與HTTP status。

## 資料庫

- `PMR_SPC_TEST`已套用migration `20261120134446_AllowBusinessActionClientEvent`，只重建`CK_AuditEvents_ClientEvent`；未新增欄位或修改業務資料。
- 套用前建立COPY_ONLY／CHECKSUM備份，msdb確認copy-only、checksum與完成狀態；應用與Windows帳號皆因最小權限無`CREATE DATABASE`而不能執行`RESTORE VERIFYONLY`，此限制未隱藏。
- `AuditEvents`套用前後指紋保持`7767|1|7767|-1197157158`；migration history存在，約束啟用且受信任。

## 測試站發布

- Web `0.1.173`→`0.1.174`；API `0.1.162`→`0.1.163`，環境`test`。
- 同源與`:8901`直連health均`ok`，API版本／環境一致；首頁、主JS與CSS均HTTP 200，MIME分別為`application/javascript`與`text/css`。
- 發布前站台備份：`D:\SPC\release\backups\audit-business-action-20261120-215446`。正式站未異動。

## 人工驗收

1. 在測試站送出一項低風險白名單動作，例如SMTP「測試寄信」。
2. 開啟「使用者活動紀錄」，查詢事件`interaction.business_action`；應顯示「送出業務操作」、固定ActionKey、頁面、Succeeded及HTTP狀態。
3. 以安全方式造成一次合法API失敗時，應顯示相同ActionKey、Failed與狀態碼，但不含錯誤本文。
4. 確認畫面與Xlsx都沒有輸入的Email、公式、密碼、token、request body或query值；只在文字框輸入而未送出時不應新增事件。
