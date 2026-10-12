# ARCH-TASK-010 驗證紀錄

## 自動化驗證

- 已執行：`pwsh -NoProfile -File tools\architecture\Get-LargeModules.ps1 -Threshold 700 -Top 8`
  - 結果：符合預期回傳非 0；列出 11 個超過 700 行的模組。
  - 前 8 筆：`SpcChartView.vue` 2680、`PartProcessCharacteristicsView.vue` 2514、`TrendChartView.vue` 1563、`UploadService.cs` 1493、`SpcService.cs` 1320、`MasterDataV2Controller.cs` 1185、`InstrumentCalibrationsView.vue` 1100、`ProcessesView.vue` 921。
- 已執行：`pwsh -NoProfile -File tools\architecture\Get-LargeModules.ps1 -Threshold 10000 -Top 8`
  - 結果：通過並回傳 0；前 8 筆皆未超過 10000 行。

## 文件檢查

- 已執行：`Select-String -Path docs\architecture-governance.md -Pattern "SpcController|UploadService|SpcService|ManualMeasurementsV1Controller|最低驗證"`
  - 結果：通過；治理文件包含指定候選模組與最低驗證要求。

## 發布

- 工具與文件型小工作，發布不適用。
