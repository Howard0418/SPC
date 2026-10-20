# 直接DB寫入契約與實測基準
- 狀態：TASK-001契約基準；DTO／SQL名稱為後續設計名稱，尚未存在或套用。
- 決策：2026-10-20使用者確認「仍需TransFiles直接寫資料庫」。不得把既有API匯入或僅匯出Excel當成目標完成。
- 後續進度：[TASK-002](../20261020-formula-trace-task002/verification.md)已完成schema與隔離SQL防護／rollback／權限驗證；本文件的實際匯入procedure、receipt發行與consumer仍屬後續，不把schema完成視為直寫可用。

## 1. 實際來源位置
路徑根：SPC=D:/SPC；Portal=D:/PmrPortal；TransFiles=使用者既有SPC開發/TransFiles工作區。來源路徑可透過runner參數指定，未驗證其他電腦部署EXE。

| 來源 | 類別／方法／位置 | 已檢查行為 |
|---|---|---|
| TransFiles batch_convert_gui.py | ConversionGUI.upload_existing_excel 約1396、confirm_spc_batches 約1465；一般批次upload約1361 | POST /v1/uploads/variable/excel?portalDaily=true，再POST /v1/uploads/{id}/confirm?mode=upsert；不是DB直寫。 |
| TransFiles direct_import.py | 模組說明與run_direct_import_plan | 僅規劃／本機staging，不連SPC／Portal DB。已查Python來源未找到pyodbc／pymssql／INSERT INTO／SqlConnection；這不是對封裝EXE或其他部署版的保證。 |
| TransFiles batch_convert_gui.py | parse_single_file 約1505、parse_filename_datetime 約2349、round_measurement 約2382 | data_only=True讀Excel快取濃度；初驗／複驗滴定值與濃度匯出前ROUND_HALF_UP取2位，檔名提供無時區日期與時間。 |
| SPC Controllers/UploadsController.cs | variable/excel 約72、confirm 約117 | 對接UploadService，現有API寫入邊界可留舊模式相容；不是新直寫入口。 |
| SPC Services/UploadService.cs | TryParseMeasurementDate 約1449、TryDateTime 約1459、確認約567 | 無時區字串可解析為Unspecified；失敗回退傳入值，確認呼叫曾傳UtcNow；新契約不可沿用為缺日期補值。 |
| Portal Controllers/ChemicalAnalysisCatalogController.cs | CustomPreviewRequest、PreviewCustom 約64～90 | ExcelFormulaEvaluator結果轉八位文字後取decimal，依設定顯示／儲存取位；頁面VariableMeasurementEntry.cshtml約2856再toFixed(3)提交。 |
| Portal Services/ExcelFormulaEvaluator.cs | EvaluateNumber、Value.AsText、ParseFunction | decimal運算，但數字輸出最多8位；Excel ROUND為AwayFromZero；未知變數以空值參與算術可能成0。 |
| SPC Services/ChemicalFTableService.cs | EvaluateChemicalFormulaAsync 約317 | NCalc與目前啟用F表；F!B3替換成參數，無日期選版。與統計用FormulaEngineService不同，不混用。 |

## 2. 已實測結果（現況，不代表新功能驗收）
| 案例 | 實測現況 | 後續要求 |
|---|---|---|
| Primary*2、Primary+Secondary、Primary*F!$B$3 | 指定合成輸入兩引擎相同 | 可作共同基本fixture，但不能推論全部語法相容。 |
| 1/3 | Portal得到0.33333333；SPC保留不同的較長結果 | 保留EngineId／版本與精度政策；以已核准引擎語意相容，不直接換算引擎或宣稱完整decimal精度。 |
| ROUND(2.5,0)、IF(Primary>2,10,20) | Portal ROUND得3、IF正常；目前SPC同樣的Excel大寫IF字串拒絕 | 先完成公式語法基準；不可把Excel公式直接交給NCalc就啟用。 |
| 缺Primary | Portal算Primary*2可得0；SPC拒絕缺參數 | 新模式在引擎之前檢查必填原始輸入，不以0代替。 |
| 切日前後檔名 | 10/31-2359、11/01-0000可解析，但沒有時區；僅日期預設09:10 | TransFiles以明確來源Asia/Taipei規則產生帶+08:00量測時間；不依匯入機器時區。 |
| 錯誤檔名／2026-2-30-2460 | 錯檔名回退20260424 09:10；無效日期時間可被組成整數／字串 | 新直寫需真正日曆／時鐘驗證，無法識別拒絕；不採固定日或現在時間。 |
| 1.23456789滴定值、正負1.235 | 匯出可能先成1.23；中點為1.24／-1.24 | 原始字串／單位／值與計算／顯示值分開，直寫不得以已截短匯出值冒稱原始輸入。 |

上述日期回退、缺參數及精度是已揭露的既有風險，本件沒有修正；由TASK-003／005的新模式輸入驗證及來源保留處理，歷史不自動補算。

## 3. 後續直寫資料契約（唯一業務落地為TransFiles→DB）
### 輸入與來源
- PPCId與已解析的製程／機台／槽／特性映射、SourceFileHash／Sheet／Row／Stage／來源唯一鍵。
- MeasuredAtIso8601須帶Z或offset；Excel無時區只可依已宣告Asia/Taipei解讀，不猜使用者／主機時區。原始日期／時間字串一併保存。
- 初驗／複驗Primary／Secondary原始字串、canonical decimal字串、單位，不能預先round_measurement。decimal解析禁止NaN／Infinity、缺值、溢位或無效格式，不讓JSON float先損失精度。
- 預覽回傳FormulaVersionId、EffectivityRevision、不可變F引用快照、EngineId／版本、PrecisionPolicyId、原始結果／儲存結果／判定、CanonicalInputHash。缺版與缺原始值的舊Excel需隔離或外部未知模式，不自動套最新版。

### 唯讀預覽與確認準備
1. 純預覽呼叫SPC解析／計算，不寫業務量測或追溯。
2. 使用者確認後，SPC準備短效不可變CalculationReceipt（暫存計算憑證，名稱暫定），綁定操作者／來源鍵／輸入／量測時間／排程修訂／結果及F快照。不能讓SQL端只相信TransFiles自報版號與結果。
3. Receipt的建立只保留計算暫存，不算量測匯入成功；計算服務API不寫VariableMeasurements。此為新增契約，尚未實作。失敗可保留未消耗receipt供稽核，到期策略須TASK-002定義。

### DB原子寫入與閘門
- TransFiles用參數化、專用低權限連線呼叫SPC管理的受控SQL程序（暫名CommitChemicalMeasurementWithTrace），實際量測／trace由此DB交易落地，不改回API匯入。
- 同交易鎖定PPC目前排程修訂及receipt，核對有效期、PPC／來源／操作者、輸入hash、到期及是否重複。過期／衝突拒絕並要求重預覽；不得默換版本。
- 從可信receipt取得計算值與快照，原子寫UploadBatch／Detail、VariableMeasurement、初驗／複驗Trace及消耗receipt；任一步驟失敗全部rollback。相同來源與hash重送回原成功結果，不重複；同鍵不同hash回衝突。
- 必須禁止新直寫帳號直接DML核心表，只授權受控程序；舊API與有表寫入權限的其他帳號也要檢查已啟用PPC的缺trace寫入。只新增API驗證不足以防繞過；SQL權限／程序／並行驗證皆留TASK-002／005，尚未驗證可部署。
- 不是讓TransFiles執行任意SQL、建立表或保存管理者密碼；連線機敏值不進版控／日誌。

### SPC統計與警報不能遺漏
現有API確認會呼叫SpcService.CalculateVariableAsync。直接寫DB會繞過此步，故同筆DB交易須保存可重試統計工作（outbox或經證實可沿用的既有佇列）；SPC消費後算統計／警報，不在匯入後漏算或以再次匯入補算。
DB已落量測但統計未完成須明示狀態，不回報完整成功；統計重試以量測／工作識別去重。outbox實作、批次狀態／UI相容及失敗回復由TASK-002／005／006完成；本件沒有建立佇列。

## 4. 不可省略的後續實證
- TASK-002：SQL Server隔離庫，測receipt不可變、原子rollback、PPC隔離、並行發布／寫入、權限繞過、兩次重送與備份回復；確認所需附加表，不只建立有效期兩張表。
- TASK-003：相容引擎與精度政策、輸入驗證、選版／過期預覽、receipt發行及授權；不能把本基準反例當可接受的新計算結果。
- TASK-005：真正DB連線／受控程序client、原始滴定值未取位留存、初驗／複驗同交易、版本過期重預覽及來源錯誤報告。
- TASK-006：統計／警報／批次狀態、圖表實際版本及Antigravity端到端，不以SQL寫入成功冒稱統計完成。
- 目前無正式機DB權限、schema或部署相容性實證；這些不是本件測試已通過事項。其他電腦EXE來源版亦待後續部署核對。
