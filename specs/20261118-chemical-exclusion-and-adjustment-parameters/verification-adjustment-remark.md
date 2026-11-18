# PORTAL-CHEM-ADJUST-REMARK-1118-TASK-001 驗證

- SPC 核心：3/3 通過，覆蓋去除空白、保存重載、超過 500 字拒絕且原值不變。
- Portal API：288/288 通過；Portal Web：47/47 通過。
- SPC API、Portal API、Portal Web Release 建置均 0 warning / 0 error。
- 發布：Portal 測試 API `1.0.263`／Web `1.0.301`、SPC 測試 API `0.1.136`／Web `0.1.142`。
- 連線：Portal health 為 `database=ok`、`dbEnvironment=test`，SPC health/version 為 `0.1.136`、`test`；兩站首頁、JS、CSS 均 HTTP 200 且 MIME 正確，Portal 同源匿名 API 401 符合權限保護。
- 回復：SPC 備份 `D:\SPC\release\backup-adjust-remark-test-20261118-200701`。
- 人工驗收：待確認新增、修改、清空及重載備註；正式未授權、未發布。
