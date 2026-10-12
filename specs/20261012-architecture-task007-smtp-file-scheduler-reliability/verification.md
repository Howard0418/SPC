# ARCH-TASK-007 驗證紀錄

## 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter SpcReportScheduleProcessorTests --no-restore -p:UseSharedCompilation=false`
  - 結果：通過，3 passed。
  - 覆蓋：週報全部失敗不更新 LastSent；月報部分成功才更新 LastSent；未到排程分鐘不產生報表、不寄信、不更新 LastSent。

## 建置

- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`
  - 結果：通過，0 warnings / 0 errors。
  - 備註：第一次與測試平行執行時，測試因 build 同時寫入 `MesSpc.Api.dll` 遇到檔案鎖；改為單獨重跑測試後通過。

## 發布

- 發布：已發布 SPC 測試站 backend 至 `D:\SPC\release\test\backend`。
- 備份：`D:\SPC\release\test\backend.backup-architecture-task007-20261012-0800`；備份時排除受保護的 `private-data` 目錄，未讀取或複製資料保護金鑰。
- 套用證據：`D:\SPC\release\test\backend\_release-evidence-20261012-081554.json`。
- Smoke：
  - `http://172.16.110.27:8084/api/health`：200，`status=ok`、`environment=test`。
  - `http://172.16.110.27:8084/api/version`：200，`environment=test`。
  - `http://172.16.110.27:8084/`：200。
- 備註：`localhost` 與 `127.0.0.1` 因 IIS host binding 回 400 Invalid Hostname；依既有單站驗證方式改用 `172.16.110.27:8084` 補驗通過。第一次 release 工具 smoke 參數誤以逗號字串傳入，工具 evidence 中該筆 smoke 不作為結果判定，以上述補驗為準。
- 正式站：未發布。
