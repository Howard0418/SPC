# 驗證紀錄
- 紅燈：新增最近規格版本測試後，原程式回傳 Unknown，測試如預期失敗。
- 綠燈：同距離的 11/09 與 11/11 規格版本，11/10 量測採過去的 11/09 版本；初驗依 LSL／USL 判定 NG，並回傳來源與版本號。ManualMeasurementsChemicalScopeTests 5/5 通過。
- 回歸：既有保存規格仍優先；缺保存證據時才使用最近版本。Chemical 篩選測試 5/5 通過。
- Release build 0 警告、0 錯誤。
- 測試站 SPC API 0.1.119 已發布；Web 首頁、主 JavaScript、API health/version 均 HTTP 200，MIME 與 environment=test 正確。
- 第一次發布因 DLL 鎖定失敗且保持 0.1.118；第二次在已核對測試目標與 PMR_SPC_TEST 後成功，維護頁已清除。正式站及正式資料未變更。
- 中央進度表已備份後更新：登錄246、完成210、核心未完成34、SPC相關10；公式錯誤掃描0、XLSX必要封裝項目與更新區域渲染通過。備份為 `進度表-20261110-160115-SPC-CHEM-NEAREST-SPEC-1110.xlsx`。
