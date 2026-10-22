# SPC-PARTICLE-EMPTY-TASK-001 執行驗證

2026-10-22；狀態：完成原因追查與最小流程修正；TASK-002資料寫入／實站製圖未執行。

## 已確認原因

1. 使用者明確確認使用`https://spctest.pmr.com.tw`。`/api/version`與`/api/health`均200，API0.1.68／environment=test；`/particle-monitoring`200。
2. 依實際`release/test/backend/appsettings.json`核對SqlServer／test及固定測試profile，連線只用於SELECT，`DB_NAME()`為`PMR_SPC_TEST`。ParticleMeasurements 0筆；Particle批次0；同原檔hash或含257檔名批次0；Particle錯誤0。既有UploadBatches為Variable Imported336／PreviewReady3。不存在可追蹤的該檔Particle預覽／確認結果。
3. TransFiles的`run_conversion_thread`進落塵分支後return；`run_dust_conversion`只有相容Attribute Excel匯出，完全沒有依`upload_after_conversion`呼叫既有Particle uploader。這是已重現的來源控制流程缺口，解釋工具可報轉換成功但未建立Particle批次。未取得使用者先前EXE雜湊／操作日誌，不把其過往每個按鍵當作已證實。
4. 原檔格式問題仍存在：全檔候選25,301筆，已識別78筆日期／Count錯誤；修補後全檔預覽會面臨驗證問題，不宣稱修補即等於已匯入或圖表已有資料。TASK-002先用有效子集，不把未量測改0或猜測時間。

## 修改與驗證

- 只改TransFiles `batch_convert_gui.py`落塵轉換結尾：要求上傳時呼叫既有Particle preview並交既有確認畫面；純轉換不變。SPC計算器、API、網站程式、Schema／角色不改。
- 核心前先新增4個流程測試；舊碼3項失敗／1項通過，實際重現未呼叫上傳及誤報成功；修正後4項通過。
- `python -m unittest test_dust_conversion_flow test_dust_particle_preview test_batch_convert_gui test_etch_upload -q`：51通過。mock驗證工作執行緒上傳、純轉換不送API、上傳失敗不顯示成功、錯誤預覽保留且不自動confirm。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore --filter FullyQualifiedName~Particle --verbosity quiet`：12通過，包含既有模型／預覽／重複／C/U／查詢。
- 來源patch保存於[integrations](../../integrations/transfiles/particle-preview-flow/README.md)，獨立目錄apply-check／apply及來源逐字重現通過；外部工具無repo，不把程式複本錯放SPC src下。

## 工具交付與發布狀態

- 使用現有PyInstaller6.21.0及既有`BatchConvertTool.spec`獨立產包，未安裝套件，未覆蓋原EXE。
- 新EXE：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/TransFiles/發布版_20261022_落塵預覽修正/BatchConvertTool.exe`，30,219,217 bytes。
- SHA256：`CA636A73ACB54B4DBDBDDAC0CEC1B6126902F487EC24E4D2EDEB7A0424941329`。交付資料夾及hash為本修補識別；GUI共用VERSION未更動，不以其他模組版號假裝業務更新。
- 新EXE程序啟動後存活，再只關閉本次程序；依隱藏啟動規則未取得可見視窗，不冒稱真人GUI／登入／匯入驗收通過。原工具未關閉或替換，後續可用新EXE按案例驗收。
- SPC網站程式未改，測試API0.1.68／Web0.1.70維持現況；不因外部工具修正而重新發布無變更網站。正式站未操作。
- 沒有對測試／正式API發送真實preview或confirm，沒有寫入Particle資料；畫面此時仍可顯示0筆，不能說已用測試資料出圖。

## 下一件與回復

TASK-002待依序執行：2026-08-01～09-30有效32事件子集、Particle預覽→確認、實際查圖與MD逐步輸入紀錄；截圖窄範圍預期4點。完整78筆格式問題不可用假值掩蓋。

回復採原EXE／來源patch反向套用（先核對來源），本次無資料寫入可回復。新EXE不提交Git；只提交必要來源patch、測試與規格。剩本案1件＋i18n20包＝21件；受阻／暫緩2件與TransFiles舊驗收1件分列。
