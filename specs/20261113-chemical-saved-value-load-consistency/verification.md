# 驗證紀錄

- 狀態：完成（正式站未發布）

- 先以保存 `151.640`、滴定值 `12.4` 的日報情境重現：日期情境變更後舊程式錯誤重算為 `2.47`。
- 修正後 Portal UI 11/11 通過，該情境保留 `151.640` 且不呼叫 preview；使用者直接輸入滴定值仍會重新預覽。
- `dotnet build src/PmrPortal.Web/PmrPortal.Web.csproj --no-restore -v:q` 通過；Portal 測試 API 1.0.235／Web 1.0.276 已發布。正式未發布，無資料庫與歷史資料修改。
