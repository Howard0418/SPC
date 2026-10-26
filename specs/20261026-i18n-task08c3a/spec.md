# I18N-TASK-08C3A 公式／圖表／查詢固定API訊息三語
- 2026-10-26 DONE，必要驗證與測試發布完成，依[連續授權](../20261026-i18n-continuation/spec.md)。C3分A控制器固定回應、B服務例外與動態驗證，範圍不增加。主系統SPC，無相依系統／schema／公式演算法／角色／資料格式變更。
- R1 僅Spc/SpcV2/V2、ControlLimitSegments、SpecificationLimitVersions、ChartSegmentDetails、ChemicalAnalysisFormulaVersions／FormulaSchedule／TraceQuery／PortalMeasurements、Genealogy／Traceability、EtchReports、Particles控制器可觀察固定message/body附加metadata；不更動分支與寫入、計算、DTO資料與原message（包含原英文）。
- R2 舊string body沿C2明示安全header，物件原success/data等欄位不動，只加code/key/args。成功提示也附key，不更動status／data。既有enum／識別／scope／日期／數值與保護token保持；無明示language的API舊message仍原文。
- BDD Happy：Given正常查詢或點位操作，When回應，Then原成功／資料結果保持，顯示metadata可三語解譯。
- BDD Boundary：Given起迄日期、3個月、分段重疊、500字、粒子10,000／單位與chartType，Then原驗證／HTTP不變，原繁中或英文message/body仍保留，無效時不寫入。
- BDD Invalid：Given不存在項目／無效分頁／JSON等，Then原拒絕／header或descriptor安全可解譯，未知例外仍沿原路留C3B，不猜譯文、不暴露內部錯誤。
- BDD Regression：原點位排除／備註、分段、AD／粒子／藥液必要測試，核心先測再實作；計算／匯入／權限保持。逐頁動態顯示留C5，服務例外留C3B。
- 測試Web0.1.96/API0.1.77；build/test後核IIS/PMR_SPC_TEST/pending migrations、完整前後端備份，只test pool發布；正式未授權。回復前後端成對、保護設定／資料，無DB回復需求。譯文draft待20。
- [任務](tasks.md)／[驗證](verification.md)。
