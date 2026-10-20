# 計畫
- 核心測試先行：SQL整合fixture及consumer單元測試；Python純輸入／流程mock測試先紅後綠。
- 用EF migration管理CommitChemicalMeasurementWithTrace及只讀status procedure；固定SQL物件名、參數化值、不任意SQL。
- 沿用ChemicalFormulaScheduleService、ChemicalCalculationReceipts/Traces/StatisticsJobs與SpcService；不另建公式系統。
- TransFiles新增模組及GUI入口，既有轉檔匯入不順手改寫；新格式每列PPC、ISO日期、原始滴定值、班別／階段、來源檔列定位，錯誤給修正建議。
- Happy：初複驗／跨日／consumer；Boundary：到期／重送／並發；Invalid：缺值／衝突／權限；Regression：Portal交易／藥液及咬蝕。
- 每件完成同步specs、需求、CHANGELOG及TODO，再提交SPC並發布測試站；TransFiles非git根，先查受管repo再決定可提交範圍，不建立新repo。完成TASK005後依本次授權接TASK006。
