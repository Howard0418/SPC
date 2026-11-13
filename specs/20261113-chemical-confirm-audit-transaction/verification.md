# 驗證紀錄

## 根因與紅燈

- 測試站 2026-11-13 14:44 的 `/api/v1/chemical-portal-measurements/confirm` 記錄 `The connection is already in a transaction and cannot participate in another transaction.`，位置為 `ChemicalPortalMeasurementService.ConfirmAsync` 原第66行。
- `BusinessDataAuditFilter` 先開啟Serializable交易，藥液確認服務為保存量測、Receipt、Trace、統計及提交後通知又建立完整交易；與前一件PPC主檔交易修正為不同API及交易擁有者。
- 新增真實Controller＋Service＋BusinessDataAuditFilter的SQLite關聯式整合測試，修正前回傳503，成功重現。

## 實作與綠燈

- 稽核政策明示藥液Confirm由Action／Service擁有完整交易；篩選器先暫存稽核事件但不再建立外層交易。
- 藥液服務在冪等與一般成功路徑提交前皆保證SaveChanges，稽核事件和量測資料在同一交易同成同敗；既有通知仍只在commit及dispose後執行。
- 整合測試驗證成功路徑的量測、初驗Receipt、Trace、統計、稽核事件各1筆，Email 1筆且非交易中送出；模擬AuditEvents寫入失敗後，既有量測、稽核數與通知數保持。
- `BusinessDataAuditFilterTests|ChemicalPortalMeasurementTests|ChemicalPortalMeasurementSqlTests`：13/13通過。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`：成功，0 warning、0 error。
- 完整API測試未重跑；前件已知10項藥液歷史判定／修復失敗仍為既有風險，本件未修改該區域。

## 測試站發布

- Release ID：`spc-chemical-confirm-audit-tx-20261113`；來源commit `b86daa82a64b88e0892060fce6cae7368b947065`。
- SPC測試API `0.1.129`；Web保持 `0.1.129`；migration 0。
- `PMR_SPC_TEST` 備份 `PMR_SPC_TEST-spc-chemical-confirm-audit-tx-20261113-8d520ed7150e46658f394e0bb4c497e0.bak` 已驗證；暫時dbcreator已撤銷，sysadmin未授予，最終狀態 `0|0|1`。
- 首頁200/text-html；目前JS 200/application-javascript；CSS 200/text-css。
- 瀏覽器實際同源API基底 `https://SPCTEST.pmr.com.tw` 的 `/api/health` 與 `/api/version` 回 `0.1.129 / test`。
- 部署DLL ProductVersion為 `0.1.129+b86daa82a64b88e0892060fce6cae7368b947065`，SHA-256為 `F4B32EB9734BFF37534955C17EEA2C5C4743A2CFAF8B15E794106EECCE354E19`。

## 待驗收與發布界線

- 未代替使用者按下目前畫面的儲存，避免寫入真實量測；請重新整理後以原資料重試。登入態實際Confirm成功及查詢回讀仍待人工確認。
- 14:44失敗請求在建立服務交易前即中止，該次未保存量測。
- 正式站未授權、未發布。

## 進度表與回復

- 中央原檔已備份為 `進度表-藥液Confirm交易修正前-20261113-145722.xlsx` 後取代；輸出SHA-256為 `09D8E66A75A6754EA09F7260C1FEE4CF66A73580A448BAAE64E8B65A24506388`。
- 「小工作總表」新增本件待登入驗收列；「專案進度表」新增2026-11-13原生單日里程碑。工作表2、table1、套件13 parts及drawing anchors 105→105保持。
- Artifact Tool重讀與變更區渲染、ZIP重讀、OpenXML SDK驗證通過（0錯誤）；Microsoft Excel人工開啟與LibreOffice列印未執行。
- 目前核心未完成42件，其中SPC相關19件；人工驗收與正式授權不視為完成。
- 回復方式：以本次更新前備份覆蓋中央原檔；程式則回復commit `b86daa82`並依測試站發布流程回復API0.1.128。
