# SPC-FORMULA-TRACE-TASK-005：受控DB直寫與統計佇列
版本2；狀態：實作／自動化／個人低權限連線／測試發布完成；正常業務真人操作待驗。授權：使用者要求先完成SPC剩餘兩項；逐件執行，不做KM或其他系統工作。主系統SPC；相依TransFiles。沿用[主規格](../20261020-formula-effective-trace-plan/spec.md)及[TASK-001契約](../20261020-formula-trace-task001/contract.md)。

## 最小範圍
- SPC既有日期preview/prepare發可信receipt；TransFiles只用API計算，不用API落量測。新增受控SQL程序從receipt寫量測、trace、批次與Pending統計工作，專用role僅execute／狀態查詢。
- 每來源列獨立原子交易；跨日期批次逐列選版。重送同來源同內容回既有量測，不同內容衝突；明確修正沿Portal，不由檔案匯入靜默覆蓋既有日報。
- 生效版、到期、PPC、原始輸入、操作者及來源鍵由receipt核對，不信client結果。只支援CHEM，未有排程／缺raw的來源隔離，不改咬蝕／落塵既有入口。
- TransFiles提供可操作的原始值Excel直接匯入入口與報告，保留字串、不先round；以明確Asia/Taipei日期產生offset。專用Windows整合SQL連線由環境提供，不保存管理者密碼；缺連線時拒絕、不退回API匯入。
- SPC consumer依工作與PPC交易鎖計算、結果／警報與Completed原子保存，失敗可重試，commit後通知。DB提交與統計完成分開顯示。

## BDD／驗收
- AC01 Given同批跨日期原始值，When preview/prepare再DB確認，Then各自正確版本、可信結果、初複驗trace及Pending工作，原始值未截短。
- AC02 Given同來源同內容或不同內容，When重送，Then前者回相同ID、後者衝突，無重複；到期未消耗receipt／變更排程拒絕。
- AC03 Given缺raw／日期／跨PPC／錯階段或故障注入，When確認，Then整列量測／trace／batch／job全部回復，其他列依既有隔離規則回報。
- AC04 Given低權限writer與已啟用gate，When直接DML，Then拒絕；透過程序成功；隔離SQL先驗證權限；使用者指定PMR\ihao_ting後，可建立該既有Windows身分的SQL登入／測試DB user並僅加入直寫role，不改AD、不授予正式庫或管理者角色。
- AC05 GivenPending／重試／多consumer，When處理，Then統計與警報只產生一次、Completed可查、錯誤安全且可重試，舊項目無額外重算。

## DB／發布／rollback
新EF migration只部署受控procedure／必要完整性防護，不新增重複公式表；先隔離Up/Down/Up及故障／並發／權限，再驗證備份後測試站發布。共用policy保持不啟用，不新增正式設定／帳號；只為使用者指定的PMR\ihao_ting設定測試SQL連線所需登入／DB user與低權限role。正式另授權。
回復停新入口／consumer，還原應用，保留不可變trace與Pending工作；不刪資料或對已落地資料做破壞性Down。GUI來源入口可停用，缺適用版不走舊計算。
