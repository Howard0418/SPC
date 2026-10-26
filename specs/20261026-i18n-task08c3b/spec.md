# I18N-TASK-08C3B 公式／圖表／查詢動態驗證與服務例外三語
- 2026-10-26 DONE，必要驗證與測試發布完成，依[連續授權](../20261026-i18n-continuation/spec.md)，接續[固定回應](../20261026-i18n-task08c3a/spec.md)。主系統SPC；無Portal／TransFiles修改，無schema、計算／數字解析／角色變更。
- R1 在既有可觀察例外建立點附加明示安全descriptor，不替換Exception型別、Message、ParamName、InnerException或throw／catch分支；只固定key與已明示可公開的cell、代碼／版本ID／欄位label等具名資料，不含token／actor／原公式JSON／SQL。controller既有catch保留原HTTP與message／success/data，僅採已附metadata；未知例外仍原fallback，不由文字反查key。全域500安全策略不變。
- R2 ChemicalFormulaSchedule／Calculation／FTable／AnalysisVersion／PortalMeasurement／Trace、VersionHistoryDateRange、EtchSync與Particles private validation；既有機器錯誤代碼保持，單位／計算／日期ISO8601、nonce／hash／預覽與原交易不變。原匯入欄位詳細錯誤留C4逐一處理，不猜測原文；逐頁即時descriptor留C5。
- R3 舊動態string body沿原格式保留，header明示key及受限base64url UTF8 JSON args（上限4096字元），前端只解碼已知key及exact純文字args；未知／超長／非法編碼與nested args回原訊息，不eval或二次編譯。無args的C2契約保持。
- BDD Happy：Given合法公式／排程／查詢，Then原成功／計算／數值完全相同；Given既有拒絕，Then例外原型別／Message／Inner與HTTP相同，可解譯三語。
- BDD Boundary：Given ISO時間／1～200期間／500理由／decimal精度／重疊與過期預覽，Then原拒絕不寫入；原文cell等含HTML/i18n符號單次純文字插值。
- BDD Invalid：Given未知例外或不合法／超長header，Then原safe fallback，不揭露inner secret；args不包含簽章／token／原輸入／機敏值。
- BDD Regression：核心測試先行、原公式／排程／Portal批次／歷史／粒子與日期必要回歸；無需真SQL寫入，SQL高風險併發測試本次不聲稱重跑。build/test後核IIS/PMR_SPC_TEST/pending migrations並成對備份，只測試Web0.1.97/API0.1.78，正式另授權。
- 回復同前批測試pool與前後端成對備份，保護設定／資料，無DB回復需求。譯文draft待20。[任務](tasks.md)／[驗證](verification.md)。
