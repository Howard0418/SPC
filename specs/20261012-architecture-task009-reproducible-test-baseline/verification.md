# ARCH-TASK-009 驗證紀錄

## 自動化驗證

- `pwsh -NoProfile -File tools\testing\Invoke-SpcBaseline.ps1 -TestFilter UserAccessTests -SkipFrontend`
  - 結果：通過。
  - 後端測試：`UserAccessTests` 10 passed。
  - 後端 build：0 errors；`NU1900` 套件弱點資料來源無法載入，屬外部網路/弱點資料查詢警告。
  - 前端 build：依 `-SkipFrontend` 未執行。
- `pwsh -NoProfile -File tools\testing\Invoke-SpcBaseline.ps1 -SkipTests -SkipFrontend -SmokeUrl http://172.16.110.27:8084/api/health`
  - 結果：通過。
  - 後端測試：依 `-SkipTests` 未執行。
  - 後端 build：0 errors；同上有 `NU1900` 警告。
  - Smoke：`http://172.16.110.27:8084/api/health` 通過。

## 發布

- 工具與文件型小工作，發布不適用。
