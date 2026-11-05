# 驗證紀錄

## 結果

- 2026-11-05 已依使用者授權，以固定 Excel 來源原地更正正式既有量測 493 筆；待更正 0、已新增量測 0，正式量測總數更正前後均為 14,993。
- 初驗 493 筆與來源相符；其中 24 筆含複驗，初複驗均依相同日期的歷史公式與 F 排程重算並與固定 Excel 值相符。
- 建立 517 份不可變 Receipt、517 份 Trace，以及 493 份含 before／after 的更正批次明細；重送驗證為待更正 0，原量測 ID 保持。
- 非目標量測、PPC、公式與 F 指紋更正前後一致；64 個涉及 PPC 的圖表服務查詢成功。

## 備份與回復

- 更正前正式完整備份：`C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_2026_pre_oct_493_correction_20261105-075842.bak`。
- `COPY_ONLY`、`CHECKSUM` 與 `RESTORE VERIFYONLY` 均成功。若需回復，優先依更正批次的 before 證據受控還原 493 筆；整庫還原前須評估備份後其他正式寫入。

## 自動化驗證

- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter OctoberProductionChemicalCorrectionPlannerTests`：6／6 通過。
- `dotnet build tools/OctoberProductionChemicalCorrection/OctoberProductionChemicalCorrection.csproj`：0 error；僅 NuGet 漏洞來源無法連線警告，不影響既有套件編譯。
- 正式執行器在套用前後驗證 production API health／version 與環境標籤；正式資料庫為 `PMR_SPC_2026`。
- 更正報告重開、公式錯誤掃描與四工作表視覺檢查通過；進度表公式錯誤 0，原 64 個圖形保留並新增本次 1 個里程碑，備份後取代原檔。

## 尚未完成但不得重匯量測

- 333 筆統計工作完成；160 筆統計工作失敗，集中於 25 個 `ChartTypeId` 空值的既有管制項目。
- 這 160 筆的量測值與追溯已正確；後續須確認管制圖型別後只重跑統計，不可再次匯入量測。
- 109 筆班別／階段身分無法唯一配對與 12 筆來源 F 衝突仍維持隔離，不在本次 493 筆授權範圍。
- 原 UNC 檔案目前不可重讀；本次使用前次核對時已固定 SHA-256 的匯入來源與完整抽取，不擴張至其他版本。

## 發布

- 本件只更正正式資料並新增受控更正工具，未修改或發布 SPC／Portal Web、API 或 IIS。
