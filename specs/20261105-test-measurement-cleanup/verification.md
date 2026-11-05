# 2026-11-05 驗證與清除結果

使用者先明示清除測試機全部量測／匯入，保留主檔／公式／F／稽核；遇舊資料憑證trigger阻擋後，另明示同意修正測試純封存規則。正式未寫入。

## 結果

| 測試表 | 封存可查詢列 |
|---|---:|
| VariableMeasurements | 18,108 |
| ParticleMeasurements | 2,304 |
| SpcCalculationResults | 4,539 |
| SpcPointExclusions | 2 |
| AlertEvents | 29 |
| UploadDetails | 17,872 |
| UploadErrors | 7,479 |
| UploadBatches | 2,748 |
| AttributeMeasurements／MeasurementValues／MeasurementBatches／SpcPointRemarks | 0 |
| 合計 | 53,081 |

- 全12表查詢剩0，實體列數保持；測試70表中非白名單表SHA256保持，2,377筆統計工作身分保持，本次需停止重試0筆。
- 主檔、公式、F、45筆AuditEvents、不可變Receipt／Trace與防重歷史保持。正式69表逐主鍵排序JSON SHA256及trigger修改時間／啟用狀態保持。
- 336項啟用且有圖型的實際`SpcService.GetInteractiveChartAsync`均無資料，獨立`verify`再次0；不是瀏覽器人工驗收。
- 測試health成功、version `0.1.109/test`；未修改Web／API程式，無網站發布。正式API／Web亦未發布。

## 防護、失敗與回復

- 執行身分備份權限不足時在任何寫入前停止，改採既有獨立維護設定僅備份／授權trigger維護，不提升執行身分權限。完整COPY_ONLY／CHECKSUM及VERIFYONLY通過；成功清除前備份檔名`PMR_SPC_TEST_CLEAR_20261105-112714.bak`，SQL Server預設備份目錄保留。本機證據在`.test-cleanup-20261105/`，不提交。
- 第一次清除因51110整交易回復；純封存候選曾因CREATE／ALTER定義回傳及自動RowVersion差異中止。隔離SQL補上真rowversion驗證後修正。大量UPDATE逾時亦完整回復，改量測每200列、同一交易後成功。
- 只對既有0→1且所有業務欄位不變豁免51110；排除IsDeleted／UpdatedAt／UpdatedBy及SQL自動rowversion。新增、改值、來源改值、還原、封存後更新均仍拒絕，另兩項原憑證檢查保持，不停用trigger。正式規則未變。
- 原trigger／備份／原ID及原稽核欄位留本機；還原缺憑證舊資料須另核准維護方式，不能直接反向UPDATE或整庫覆蓋後續輸入。

## 自動化

- 核心先紅測試後實作；Chemical全部117通過，Python來源規則15通過，工具build零警告零錯誤。
- 獨立SQL10項（含舊版拒絕、純封存、改值／來源欄／新增／封存新增／還原／再次更新拒絕及資料保持）通過；唯一隔離DB已移除。
- 指定「修正版確認」Excel保持原檔與數值，48格核准線別固定G欄，未知／空白／同鍵核准不同拒絕；精確來源鍵優先，不因其他檔同工作表名套用核准。日期衝突及同核准線別不同來源值仍分開核對，本次未重匯正式資料。

## 收尾

原進度表已備份並取代原路徑，小工作總表新增第269列，2026/11/05實際日期及原生里程碑同步；原69物件保持，現在70物件。非目標儲存格／全部既有樣式／其他ZIP部件保持，表格延伸至K269，獨立旗標計數核心45／SPC22。236登錄／190完成，人工11與待核銷4另計。

OpenXML零錯誤、Artifact重開與公式錯誤0、LibreOffice列印96頁及新增項目所在第40／96頁目視通過；沿用原寬表分頁，未順手重設其他專案列印格式。Microsoft Excel人工開啟未做。原檔SHA256 `3F78AAC12C9C0503BFFB42F1DD284220DAE029BBB6BA55DDEAB8C8E912DA91B5`；備份`Voice/備份/進度表_測試資料清除前_20261105_1130.xlsx`。

本件必要程式、測試、規格及文件提交推送目前Develop；不提交設定／XLSX／備份／原始資料／證據或既有使用者變更。Git紀錄為提交與推送依據。
