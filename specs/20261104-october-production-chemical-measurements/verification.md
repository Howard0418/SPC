# 驗證紀錄

- 日期：2026-11-04
- 狀態：正式資料匯入與防重驗證完成；統計後處理部分受既有主檔阻擋，整件未標 DONE
- 正式資料庫：`172.16.110.16 / PMR_SPC_2026`
- 正式 API：`0.1.102 / production`，`/api/health`與`/api/version`成功
- 網站發布：不適用（本件未修改網站程式或IIS）

## 執行結果

1. 三份已核准來源以固定 SHA-256 讀取，十月1,211列中保留12列F衝突隔離；正式公式／F逐列試算1,211次，寫入候選初複驗試算358次，與Excel差異均在`1e-8`內。
2. 寫入前計畫固定為335筆新增、864筆既有略過、12筆衝突隔離；正式資料庫先完成`COPY_ONLY + CHECKSUM`備份及`RESTORE VERIFYONLY`。備份為`PMR_SPC_2026_pre_oct_chemical_20261104-201019.bak`。
3. 正式新增量測335筆（ID `86787`～`87121`），重複0；新增358份Receipt／Trace（335初驗、23複驗）及335個受控UploadBatch／統計工作。匯入時間皆為報表日12:00+08:00，來源鍵明示非實際取樣時間。
4. 新增資料逐筆335筆查回：PPC、報表日、班別、N1／N2開收線、初複驗原值、Excel結果、公式版本及F快照一致；75個涉及分析項目的正式圖表服務查詢成功。
5. 相同清單再次驗證為新增0、已存在1,199、衝突12；正式量測總數、Receipt／Trace／Batch／統計工作在第二次驗證前後均保持，證明不會重複匯入。
6. 寫入前最大量測ID以內指紋、PPC、啟用F及公式／排程指紋保持不變；未覆寫既有資料。

## 未完成與格式修正

- 統計工作240筆完成；95筆分布於[24個既有分析項目](statistics-blockers.md)，皆因正式PPC的`ChartTypeId`為空而失敗。量測、Receipt、Trace及圖表查詢本身已存在且一致，不可重匯。
- 後續須由業務確認這24項應採用的管制圖型別；補齊主檔後只重跑既有95個統計工作，不再匯入量測。未確認前不可猜測圖型或把失敗工作改為完成。
- 12筆ST2完整F衝突仍隔離；須先修正來源同一PPC／日期的OPEN與MIDDLE F值矛盾，再另案僅新增，不可重送本次335筆。

## 測試

- `dotnet test ... --filter "OctoberProductionChemicalImportPlannerTests|OctoberProductionFormulaFPlannerTests|ChemicalFormulaScheduleTests|ChemicalDirectWriterSqlTests"`：30通過／0失敗。
- `dotnet build tools/OctoberProductionChemicalImport/OctoberProductionChemicalImport.csproj -c Release --no-restore`：0警告／0錯誤。
- 正式唯讀重送：`include=0; skip=1199; conflicts=12; taskMeasurements=335`。
- 原進度表同步：備份後更新15個指定儲存格，保留60個原生圖形並新增第61個11/04里程碑；Artifact重開／公式錯誤0、OpenXML SDK 0錯誤、LibreOffice A3列印PDF 14頁。Microsoft Excel人工開啟未執行。

## 回復

若必須回復，只能依本件保存的335個量測ID及其Batch／Receipt／Trace／統計ID，在停寫窗口內以受控交易刪除；整庫還原前須先核對備份後是否已有其他正式寫入。現無回復需求。
