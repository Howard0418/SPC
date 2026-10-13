# 驗證紀錄

- 日期：2026-10-13
- 狀態：已完成

## 結果

- Portal API build：`dotnet build src\PmrPortal.Api\PmrPortal.Api.csproj --no-restore`，0 warnings / 0 errors。
- Portal Web build：`dotnet build src\PmrPortal.Web\PmrPortal.Web.csproj --no-restore`，0 warnings / 0 errors。
- KM build：`dotnet build src\PMR.KnowledgeHub\PMR.KnowledgeHub.csproj --no-restore`，0 warnings / 0 errors。
- Portal API 內部 health：`http://172.16.110.27:8091/health` 回 200，環境為 test。
- KM 測試站：`http://172.16.110.27:8086/Account/Login` 回 200。
- Portal API `SPC_API_BASE` 統一為 `https://SPCTEST.pmr.com.tw`，`SpcSso:WebBaseUrl` 統一為 `https://SPCTEST.pmr.com.tw`。
- Portal API `KmSso:WebBaseUrl` 統一為 KM 測試站根網址 `http://172.16.110.27:8086`。
- KM `PortalApi:BaseUrl` 統一為 Portal 測試站根網址 `https://portaltest.pmr.com.tw/`。

## 限制

- 從目前 PowerShell 工作階段直接呼叫 `https://SPCTEST.pmr.com.tw/...` 與 `https://portaltest.pmr.com.tw/...` 仍遇 Windows TLS/憑證交握錯誤，未作為應用層失敗判定；站台 hostname smoke 需用瀏覽器或受信任憑證環境再驗證。
- 未操作正式站。
