# 驗證、根因與發布狀態
## 正式唯讀根因
- 使用者正式項目2620，將LSL改0.2後409；正式資料為PROCESS/Process195/Machine247/Characteristic870/Unitμm/ChartType1046，Part/Tank/Slot null，原LSL約0.2。
- ChartType1046的大類24/PROC之RequiresMachine=false。ValidateScopeAsync錯將「線別選填」正規化成MachineId=null，再與2610（空線別、其餘相同）衝突。重複攔截本身正確，錯在存檔前抹除識別欄位。
- 修正僅刪除該MachineId清空行並加註解；保留必填與真正重複409，不改Part/Tank/Slot規則及唯一索引，未刪改正式資料。畫面長小數與本次409無關，不順手改精度。
## 回歸驗證
- 先建PpcOptionalMachineEditTests，以SQLite實際Controller.Update建立2620/2610型資料：修改LSL保留線別、真正重複409、選填null自身編輯、必填null400。
- 修正前1 failed/3 passed；修正後API專案全套47 passed/1 conditional skipped（略過不計通過），含本次4項。既有UnitTest1兩個CS8602警告保留，未宣稱零警告。
- API Release publish成功。無前端變更，不重跑前端測試；未操作正式儲存/通知。
## 測試站發布
- 已核對SpcSingleTest/D:/SPC/release/test/backend及AppEnvironment=test；備份在tmp_production_config_plan/ppc-optional-machine/test-backup。
- 停止/啟動僅SpcSingleTest pool，複製Release backend並排除appsettings/web.config/uploads/private-data/logs；全部環境設定SHA256不變。
- 已部署DLL0.1.70.0；實際/api/version與/api/health均200、test、0.1.70/ok。無DB/schema/資料修補。
## 正式具體待授權方案
- 使用正式R2基準a376290fd3da244397a6b04ce9270342eed80798隔離來源，只更動MasterDataV2Controller的同一行及API版號0.1.71；逐檔內容比對（排除CRLF差異）只有此兩檔。沒有夾帶測試0.1.69的ParticleQueryService變更。
- 此正式隔離來源另跑同4項SQLite回歸，4 passed；Release產包成功。NuGet還原曾遭沙箱網路拒絕，既有套件經授權網路還原後成功，無新套件變更。
- bundle：release-staging/ppc-optional-machine-20261022/bundle-r2/manifest.json；releaseId=SPC-PPC-OPTIONAL-MACHINE-20261022-R2。
- manifest sourceCommit記錄正式基準a376290f，實際來源為此基準加本次已記錄單行patch與版號，並非宣稱基準原commit已含修正。
- 目標API0.1.68→0.1.71，Web維持0.1.70；前端payload逐檔與原正式bundle完全一致，API差異只有dll/exe/pdb/deps四個二進位/版本檔，無migration SQL。
- Invoke-Release Production Plan已通過，正式主機PMR-SPC-SRV、SpcApi/SpcWeb及PMR_SPC_2026吻合；只Plan，未停止正式服務、備份、DDL或Apply。
- 待使用者正式發布授權後，才可用同manifest Apply並進行IIS/應用/DB COPY_ONLY/CHECKSUM/VERIFYONLY備份及health/版本/hash核對。正常啟動範圍不含任何新migration。
- rollback：同manifest Rollback回API0.1.68/Web0.1.70，保留設定/量測；不整庫還原。測試站回復使用同批test-backup，保留設定。
## 尚未完成
- 正式發布及使用者2620實際儲存驗收；不以SQLite回歸或唯讀Plan冒稱正式已修復。
- 原NULL/並發索引全面審查不屬此單行缺陷，不宣稱已完成；沒有改動schema。

- 正式API採三段版號0.1.71，因/api/version使用ToString(3)，不可用四段patch作版本smoke。版號調整未改行為，不重跑已通過同來源4項回歸。
