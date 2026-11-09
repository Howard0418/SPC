# 驗證紀錄

狀態：測試階段 DONE；正式未授權。

## 結果

- 一般導覽／查詢採線上180日、總保存365日；其餘關鍵事件採線上730日、總保存1,825日。
- 新增加密封存批次、逐筆SHA-256、批次checksum、逐筆解密還原驗證、legal hold及容量摘要；重跑依原事件ID防重。
- 受控維護要求`admin.audit.evidence`與安全管理者，且目前只允許Test環境；封存執行及排程開關預設停用，正式環境固定拒絕。
- migration只新增三表、索引及兩個保護trigger；archive不可更新，刪除需`SpcAuditMaintenance`，Down遇封存／hold資料即拒絕。

## 驗證

- 核心保存測試9項通過；完整後端回歸531通過、11個既有明示隔離測試略過、0失敗；API build 0警告／0錯誤。
- 隔離SQL資料庫因帳號缺`CREATE DATABASE`遭拒，確認未建立或修改任何隔離庫；改依既有授權先對`PMR_SPC_TEST`做COPY_ONLY／CHECKSUM備份及RESTORE VERIFYONLY，再套migration。
- migration前後既有AuditEvents皆199筆，指紋皆`FD09ED952188C62BC33C12880D7EA4E4D722C36741431A81EEB1C0F87C014E3E`；新三表皆0筆、migration 1、trigger 2。
- 測試API 0.1.111；首頁、目前JS／CSS、health、version皆HTTP 200且MIME正確，環境為test。前端實際API基底為同源`/api`；匿名容量端點回401。
- 原進度表已備份、重算、公式錯誤掃描0、原生drawing數1保持、重開與兩處渲染通過；核心未完成42、SPC相關18、完成196，人工11／待核銷4另計。

## 回復

- 資料庫備份：SQL Server備份目錄`PMR_SPC_TEST_audit_retention_20261109.bak`，CHECKSUM／VERIFYONLY通過。
- API備份：`release/test/backend.backup-audit-retention-20261109`及最終補正前`backend.backup-audit-retention-20261109-r2`。
- 停用方式為保持`Audit:Retention:Enabled=false`與`AllowCurrentConnectionForMaintenance=false`；回復API時保留新增空表。若須回復migration，必須先確認封存與hold皆0。

正式站／正式資料庫：未連線、未發布、未異動。
