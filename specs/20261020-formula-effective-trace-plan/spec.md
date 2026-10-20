# 功能規格：藥液公式生效日期、自動選版與版本留存
- 功能 ID：SPC-FORMULA-EFFECTIVE-TRACE-PLAN-20261020
- 版本：1
- 狀態：規劃完成，建議規則待確認；尚未實作
- 涉及專案：SPC主責；Portal量測輸入、TransFiles轉檔／直接匯入相依。
- 授權：使用者選擇「規劃公式生效日期、自動選版與版本留存」。本次僅文件，不改程式、資料庫、migration或站台。
- 基準：[需求索引](../../docs/requirements.md)、[TransFiles直接匯入契約](../../docs/imports/transfiles-direct-import-contract.md)。
- 前案：[唯讀分段明細v2](../20261020-chart-segment-version-detail/spec.md)，測試API0.1.63／Web0.1.67；不把歷史修改日期當生效日期。

## 現況與證據
- `backend/MesSpc.Api/Domain/Entities/Models.cs`：ChemicalAnalysisFormulaVersion有ConfigJson、VersionNo、ChangedAt，沒有生效期間；VariableMeasurement未記公式版本／計算原始輸入。
- `Services/ChemicalAnalysisFormulaVersionService.cs` RecordChangeAsync／RestoreAsync：保存修改快照與目前配置，尚未排程；還原目前配置不應改寫歷史生效紀錄。
- `Services/ChemicalFTableService.cs` GetActiveVersionAsync／EvaluateChemicalFormulaAsync：取目前IsActive版本；ApplyCellsAsync可更動同VersionId的儲存格。只存FTableVersionId不足以重現，需固定CellsJson歷史快照或使用值快照。
- `Services/UploadService.cs`：接收MeasuredValue，日期解析有UtcNow回退；尚未執行藥液日期選版。`Services/SpcService.cs`取量測／複驗值，分段以MeasuredAt選界限，不重算濃度。
- Portal `src/PmrPortal.Api/Controllers/ChemicalAnalysisCatalogController.cs` CustomPreviewRequest／PreviewCustom：接收公式字串但無量測日期，使用ExcelFormulaEvaluator；SPC另有NCalc計算器，不假設兩者語法／精度相容。
- TransFiles `direct_import.py` 開頭註明僅規劃、不連DB；尚未確認實際藥液直寫入口。正式契約規定TransFiles直接匯入，不能在本案暗改成SPC網頁匯入。

## 範圍與非範圍
沿用現有藥液公式版本、維護頁與F表歷史；新增的有效期間與計算追溯是關聯資料，不另建公式內容系統。首批只處理CHEM藥液計算，不改咬蝕、落塵、P／NP／C／U業務或圖型統計定義。
規格／管制界限獨立有效期與能力分析跨段計算留後案；本案只保留其來源關聯的擴充位置，不宣稱已整合獨立界限版本。
不自動回填舊資料實際版本、不自動重算歷史、不更換匯入主入口、不上正式站。

## 建議規則與BDD（待使用者確認後實作）
| ID | 建議規則 | Given / When / Then驗收 |
|---|---|---|
| R-001／AC-001 | 依PPC業務映射＋量測時間選唯一已發布公式，與匯入／系統時間無關 | Given v1到10/31、v2自11/01，When 同批匯入兩日量測，Then 各自使用v1／v2並留存ID；延遲匯入不改選版。 |
| R-002／AC-002 | UI用台北生效起日／迄日（含迄日）；內部轉成UTC半開區間[start,endExclusive)，無迄日為null | Given 11/01台北00:00切換，When 量測為前一秒／切換瞬間／帶UTC或+08:00的同一瞬間，Then 選版一致且無重疊。無效／未定義時區日期拒絕，禁止用UtcNow補量測日期。 |
| R-003／AC-003 | 同PPC已發布區間不可重疊，缺版明確隔離，不默用最新；草稿不參與 | Given 缺版、停用或雙匹配，When 預覽／確認，Then 回可讀錯誤與修正建議且該筆不寫入；並行發布重疊只允許一筆成功。空缺可存在，但畫面需警示受影響期間。 |
| R-004／AC-004 | 公式內容及F表使用快照不可變；排程與修改歷史分開 | Given v1含F儲存格快照，When 後來編輯F表、公式或按還原，Then 舊結果可重現；還原作為新版本／新排程提案，不篡改過去。 |
| R-005／AC-005 | SPC提供共用日期解析及計算契約，Portal／TransFiles不得各自猜選版 | Given 相同PPC、輸入、量測時點，When 經不同入口預覽，Then 版號、未四捨五入值、儲存值及判定一致；既有公式語法先以基準資料驗證，不直接換算引擎。 |
| R-006／AC-006 | 保存原始輸入、結果、不可變公式／F快照來源與計算器版本，與量測原子寫入 | Given 正常／故障／重送／複驗，When 確認寫入，Then 無半筆量測或追溯；不重複，初驗與複驗各保留來源；外部僅提供濃度者標ExternallyCalculatedUnknown，不冒稱ServerCalculated。 |
| R-007／AC-007 | 預覽到確認期間重新驗證排程修訂與輸入雜湊 | Given 預覽v1後管理者改排程或使用者改日期／輸入，When 確認，Then 拒絕過期預覽並要求重預覽，不靜默換版。 |
| R-008／AC-008 | 舊量測保留原結果與Unknown；查圖不執行最新公式 | Given 舊歷史與新完整追溯量測，When 查圖／點分段，Then 舊版未知明示、新版展示實際版本；同區間多版本逐點／子區間揭露。重新計算只能另行明確操作，保留原結果。 |
| R-009／AC-009 | 依PPC逐項啟用，未相容寫入端不得送入已啟用項目 | Given 未啟用舊客戶端與已啟用日期選版項目，When 寫入，Then 舊項目相容、已啟用項目拒絕缺追溯寫入；直接DB路徑也必須有閘門，不能只有API驗證。 |

## 邊界與待確認事項
- 本版建議只以日期維護；同日多次定時切版不在首批範圍。若需要到時分秒，須先調整UI／驗收與來源時間契約。
- 建議啟用時由管理者明確選第一個適用起日；不得把現有ChangedAt或最早量測時間自動當成起日。
- 建議對已被量測引用的有效期間禁止原地回溯修改；必要修正需另案影響清單、理由與授權，不改舊量測追溯。
- 基準精度：先沿用已確認的未四捨五入判定、顯示／儲存精度；實作前核對Portal／Excel／SPC的差異，不能順便改公式語法、精度或單位。
- 阻擋實作整合：TransFiles實際寫入程式／部署版本與權限、各來源時間語意、F歷史不可變性，以及直寫閘門可行性尚待TASK-001實證；本次規劃不宣稱已確認。
- 使用者尚未批准上述新生效／缺版／回溯政策及schema，規劃完成後停止。

## 版本
- v1／2026-10-20：沿用既有架構的建議規格與序列化計畫，未實作。
