# 第一批驗證紀錄
- 功能ID：20260917-etch-september-import；版本1；2026-09-17。
- 盤點與文件：完成；應用程式實作/測試：未執行；資料匯入/發布：未執行。
- Excel唯讀解析22張9月表；13張有原始數據、52線日報、3900原始咬蝕量點，無部分缺點組。完整統計與欄位來源見analysis.json。
- 公式/快取/獨立算術比對：3900點一致、52個整體速率一致；26個PT背面摘要不符完整點平均、5個負值/4日報已記錄。不是已通過應用程式驗收。
- 原始Excel解析前後SHA256一致。第一次唯讀腳本因EmptyCell.coordinate中止，改用列/欄座標後成功；無檔案修改。日期cached原已是datetime，不重轉Excel序號。
- SQL唯讀：測試SPC9個咬蝕PPC為n=1/I-MR，無9月量測；PT2咬蝕PPC及線速特性缺漏。Portal9月咬蝕日報0份。首次使用實體名查表失敗，依ToTable映射改查etch_amount_reports/points成功；未寫庫。
- 必要新文件內容及相對連結檢查：完成。
- 後續AC1～AC6尚未驗收；未啟動瀏覽器、單元測試、編譯或發布。
- 第二批須等使用者核准；待確認事項見spec.md。

## 第二批（2026-09-17）
實作、必要自動驗證、0901試匯及測試站發布完成；使用者人工畫面驗收待執行。正式站/整月未執行。

### 自動驗證
- Chemical原有24案＋Etch新5案＝29案全通過；追加完整組超過1000點與常數50點兩案後，Etch7案分次通過。曾因fixture缺Process造成chart null，補正fixture後通過；沒有為此修改業務邏輯。
- 核心驗證25/50的平均與樣本S、錯誤n拒絕估線、常數樣本S=0、原ID更新、同內容重送不增batch、严格衝突拒絕、負值拒絕、完整21組1050點不截斷、不混入舊單值。
- HTTP權限3案：匿名401、Viewer403、Editor進入驗證400。獨立測試token不使用真實帳密。ASP.NET測試主機有過時API警告，不影響測試。
- Portal Node VM 3案：空白日清空、過期載入不覆蓋、同步由已保存日報讀完整points/operator/lineSpeed；先紅後綠。沒有自動操作瀏覽器。
- Python解析4案：25/50兩面完整、負值隔離、空白非零、快取不一致拒絕；來源0901四線300點來源雜湊前後一致。
- X̄-S公式依NIST https://itl.nist.gov/div898/handbook/pmc/section3/pmc321.htm ：c4(50)=0.994911304669732，A3=0.426434061730524。初始測試A3期望值筆誤，經Python Gamma独立核算更正；原n<=25表不變。50點能力估計改用Sbar/c4，不使用不適用的全距常數。
- SPC API/Portal API/Web Release publish、SPC build:test、試匯工具建置通過。NU1900弱點來源連線警告與既有前端chunk大小警告已記錄；沒有宣稱弱點掃描通過。

### 發布
- IIS實際路徑核對：SPC release/test/backend、frontend；Portal release/test/portal-api、portal-web。
- 設定核對PMR_SPC_TEST、PMR_PORTAL_TEST、環境test/Test；備份標記etch-trial-20260917-171816。
- 發布檔DLL/index.html與建置雜湊一致，設定與web.config保持原值。四站version/web/health皆200。
- 發布清單：D:/SPC/release-staging/etch-trial-20260917/deployment.json。

### 真實測試庫試匯與對帳
- dry-run：9/1四線Portal/SPC目標皆空，來源hash與已解析檔案一致。
- 首次寫入前的目標主檔/量測/日報備份：release-staging/etch-trial-20260917/run-20260917-171854/before.json；未修改其他日期、未改schema。
- Portal report IDs 1/2/3/4 對應PT1/PT2/QE1/QE2。分別50/100/50/100原始點，合計300；同步SUCCESS。
- SPC對應52/102/52/102列，合計308（300原始點＋4整體速率＋4實際線速）。原9個咬蝕PPC沿用ID，補PT2三項與四條線線速；新增PROCESS XBAR_S。
- 第二次同內容重跑：四筆Unchanged=true，ReportId與UploadBatchId相同，沒有新增量測/日報/匯入批次。證據run-20260917-171952/result.json。
- 以3分鐘效期Viewer測試憑證唯讀呼叫16個實際chart API，全部200、每系列1點；8個子組n=25/50、X̄/S與管制線有效；沒有變更人員或權限。
- 來源300個BEFORE/AFTER逐點對Portal/SPC值、16圖的X̄/S/整體速率/線速通過；Portal摘要容差1e-6、SPC及圖表1e-8；只有2026-09-01資料。
- 機器可讀證據：release-staging/etch-trial-20260917/{input,charts,database-after,reconciliation}.json。

### 限制與待辦
- 四份負值日報及其他日期未匯入。全月需另核准。
- 畫面验收與真人編輯後再同步尚待使用者依plan.md五步執行；後端更新及UI行為已有隔離測試，不代稱真人驗收通過。
- 來源中文標題損壞與PT背面漏列原公式保留，統計取完整點位。舊單值摘要保留於歷史資料，不納入新咬蝕X̄-S。
- 回復：站台回拷deployment.json的各備份；資料按首次run的before.json及來源鍵回復受影響PPC與本次日報/點位/量測，保留既有資料。若已有使用者後續修改，先核對稽核/引用，不盲目刪除或整庫覆蓋。
