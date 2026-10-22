# 落塵空圖與測試資料計畫

- 功能ID：SPC-PARTICLE-EMPTY-20261022；[規格](spec.md)。狀態：待確認，無實作或寫入。

## 執行順序

1. TASK-001：先核對使用者實際SPC網址、匯入途徑、批次與部署版本。唯讀查UploadBatches／UploadDetails／UploadErrors及ParticleMeasurements；比對預覽、確認、舊Attribute、日期／時區、位置／粒徑／儀器。先找實際原因，必要最小程式修正納入本件驗證；若涉及新業務規則、schema或其他系統擴充，另列規格，不逕改。
2. TASK-002：前件原因及修正確認後，才建立有來源的測試子集並經既有Particle API預覽→確認。不直接SQL插入，不改原Excel、不重複塞入已存在點；先核對既有批次與DUPLICATE_FILE／量測重複機制，不偽造原檔hash來繞過。

## 測試資料方案

- 目標先規劃SPC測試站 `/particle-monitoring`；執行前必須核對實際站台、資料庫及現有數據，不將使用者截圖逕認正式或測試。
- 使用原檔2026-08-01～09-30的有效事件：R1／0.5可取32個點；以同一組32個事件保留R1～R9、四粒徑，最多1,152筆，供管制趨勢及R1～R9比較。預覽確認時須重新核對缺值、重複及實際可寫筆數，不承諾一定新增1,152。
- 窄範圍2026-09-22～10-22：預期R1／0.5四點0、4、104、12；來源列746～749。原圖若4點已入庫便不需再寫，先解決查詢原因。
- 不將C734:C739說明當Count或補0；也不改歷史錯誤時間。測試副本若建立，要保留原檔SHA256、原座標及子集自己的真實hash，清楚標為測試用途。
- C-chart：依原固定採樣假設評估展示；若測試只是展示，不把結果當正式製程判定。U-chart：沒有真實SamplingVolume及單位時，預期422明確提示；不為出圖偽造分母。
- MD操作紀錄至少包含：站台及版本、匯入頁／TransFiles實際按鍵、檔案／hash、batchId、輸入日期／位置／粒徑／儀器／圖型、預覽／確認／新增／略過數、來源座標、圖表點數與使用者自行清除方式。執行後填實際值，不預填成功。

## 影響範圍與相容性

- SPC：`frontend/mes-spc-web/src/views/ParticleMonitoringView.vue`、`backend/MesSpc.Api/Controllers/ParticlesController.cs`、`UploadsController.cs`、`Services/ParticleQueryService.cs`、`ParticleUploadService.cs`及對應既有測試；依根因選必要檔案，不全部修改。
- TransFiles相依來源：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/TransFiles/dust_reports.py`、`batch_convert_gui.py`；目前只讀，不預授權重寫整個轉檔工具。
- 表／欄位沿用既有ParticleMeasurements及UploadBatch staging；保留MeasurementTime UTC、來源原值、bigint Count、DeviceCode序列及C/U規則。不改舊Attribute既有資料、不將其批次狀態當Particle驗收。
- 儀器空值目前只取預設儀器；若調整畫面提示，須先測預設與指定儀器、不得把所有儀器合成同一管制序列。

## 測試與回復

| 類別 | 規劃驗證 |
|---|---|
| Happy | 有效預覽／確認、32點C圖、原始量測／最新事件比較一致。 |
| Boundary | 0／1／19／20點、跨月／日末、有效0、同時間不同批次、重複上傳。 |
| Invalid | B482／B483錯時分、C734說明、負數／小數Count、不支援位置、U圖缺分母。 |
| Regression | 舊Attribute可用、來源追溯、預覽不等於匯入、儀器隔離、權限與其他圖表不變。 |

本次不跑程式測試／build／發布，只有文件內容、連結及原檔hash檢查。後續程式修正才依必要範圍測試及發布測試站，無新變更不反覆跑全套。

寫入前保留批次及既有資料範圍，必要備份；測試資料獨立batch，記錄產生檔及ID，使用者可辨識清除。清除需另獲授權並核對FK／既有紀錄，不以日期區間大量DELETE，也不整庫還原覆蓋他人新資料。程式rollback採前版應用，不刪業務資料。正式站完全不納入本次執行授權。
