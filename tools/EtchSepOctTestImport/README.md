# 九、十月咬蝕測試匯入
僅供已授權SPC測試庫操作，不含Portal或正式路徑；使用既有`EtchReportSyncService`，不修改主檔或覆寫既有內容。不是TransFiles GUI／EXE交付。

1. 從TransFiles設定`etching_source`，以`etch_reports.extract_reports`唯讀解析原檔，再以`select_manifest.select_months`限2026-09／10，輸出本機證據目錄`manifest.json`。保留來源SHA256、逐點provenance及隔離／X排除清單。
2. `dotnet run --project tools/EtchSepOctTestImport/EtchSepOctTestImport.csproj -- --preview <證據目錄>`只預覽，錯月份／重複日線／原檔hash改變於DB前拒絕。服務預覽有來源或主檔衝突者隔離，DB連線錯誤不視為來源問題吞掉。
3. `--apply`再預覽並COPY_ONLY／CHECKSUM／VERIFYONLY，`Sync(allowUpdate:false)`逐日線僅新增；每筆再送須Unchanged且同batch。原資料核心欄位COUNT／CHECKSUM及主檔／公式政策／啟用F前後核對，不製造HTTP身分。SQL actor取實際Windows操作者。
4. `--verify`唯讀核對完整子組點數、SampleNo、前減後及速率，輸出本機`verification.json`。不以平均值代替25／50原始樣本，沒有呼叫寄信／SPC告警寫入服務；圖表依既有API動態計算。
5. 將涉及PPC的現有主檔GET結果保留本機`maps.json`，`python tools/EtchSepOctTestImport/verify_charts.py <證據目錄>`核對24組每月圖表raw ID／值、268組正背面平均與樣本S。目前來源已X排除但先前已存在的同日線資料不刪改，圖表額外點僅能明確對應來源隔離／排除鍵，並另計輸出，不算此次來源通過。

測試：本目錄`python -m unittest test_manifest`；TransFiles既有`test_etch_reports`；`dotnet test tests/MesSpc.Chemical.Tests/MesSpc.Chemical.Tests.csproj --filter FullyQualifiedName~Etch`。證據JSON／CSV／原Excel／備份／bin／obj不可提交。

回復：依本次新增batch清單受控處理；不直接刪除量測或回復整庫覆蓋其他測試成果。[授權、驗收與回復](../../specs/20261102-sep-oct-etch-test-import/spec.md)。
