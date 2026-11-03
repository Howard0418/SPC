# 驗證紀錄

2026-11-03：測試程式與資料修正已部署，使用者本人登入後實站驗收通過；4項NA、工具EXE及正式待辦，整件未DONE。正式與歷史資料不在寫入範圍。

後續最新F核對已完成：[目前B3=.99續作](f-b3-alignment.md)。本輪開始時F239／歷史248已是.99，dry-run為0格異動，故未重複套用；F表兩種寫法與C5均86.13，兩環境12張保護表與完整F資料不變。此結果補充目前F表與既有Excel快照的相同性，不把既有F248建立誤記為本輪寫入。

## 驗收與證據

|AC|結果|
|---|---|
|AC1|來源107項／214條濃度式保留 F 引用，對應87唯一啟用業務PPC；配置修正87筆、83項建立11/03起日期政策。服務實際DB預覽166次；C5 PPC2425 Primary10、F快照247的B3=.99，初複驗raw/stored均86.13，版本291。使用者本人登入後Portal UI亦為86.13；SPC總覽實際顯示Primary*8.7*F!$B$3，未以.99取代參照。|
|AC2|Portal新增6個失敗案例先紅（原先缺F／必要輸入仍回OK），修正後9項全綠。登入驗收再發現C5零值因IF未選分支除0而失敗；新增4案例先紅3，修正IF懶惰選式及IFERROR完整參數隔離後全綠，核心引擎portal-excel8-v3-lazy。API105實站C5有效0=0.00、NG及添加建議，空Primary清除濃度；缺F／未知參數仍在分支運算前拒絕。|
|AC3|新增Phase與可選recheckConcentrationFormula；保留舊版複驗fallback。5項實質差異（C1鍍銅1／2、C3剝掛、C5剝掛、QE2鎳槽銅離子）各自原係數；Portal同值初複驗token不能互換，SQLite確認6.354／31.77保存正確；實際UI顯示6.35／31.77。複驗獨有F缺快照禁止發布。另先紅測試確認舊TransFiles未標Phase仍可預覽，現於preview／prepare拒絕缺階段，來源初驗／複驗都明確傳送；本件不改SQL程序，舊EXE未重包。|
|AC4|最終v3核心重新跑3610案例：3008數值與Excel獨立Decimal在1e-8內吻合，602缺F／空值／非數值全拒絕，0差異。最終主要API全套366通過／10條件略過；Portal全214通過；9畫面回歸、TransFiles來源15及版本化交付6通過；純JS原始值／Phase契約11 assertions通過。|
|AC5|SPC隔離HEAD＋本件檔建置，測試最終API105／Web119；Portal取實際測試API217的a86fbba＋本件controller，測試API219，Web260未更新。兩站Web／主JS／CSS HTTP200及MIME、SPC同源/api/health和version測試標籤、Portal實際8091/health的test DB標籤及API DLL219、Web/app-version260均核對。登入後Portal實際代理預覽、SPC SSO／總覽與頁尾Web119/API105連線正常，無API離線；C5與PT1／QE2已核對，未按確認送出。|

來源矩陣與修正前清單／套用／166次預覽／部署／DB指紋為 Git 外證據，位於 `D:/SPC/.chemical-concentration-validation-20261103/`。不提交JSON、截圖、資料、設定或密鑰。未重新匯入，也沒有新量測、prepare、receipt、confirm或排程工作。

## 待核對4項與調整方式

|PPC|線別／槽位／項目|缺少的調整條件|
|---|---|---|
|2619|N1／催化／P400|U13（原Excel文字NA）|
|2515|N2／催化／P400|U13（NA）|
|2516|N2／催化／氯化亞錫，調整藥品安定劑|U14（NA）|
|2518|N2／促進／氯化亞錫|U16（NA）|

這4項濃度配置已依Excel補正；其他兩式保留，不啟用日期排程。需確認上限數字，或明示NA代表該項稀釋／更槽條件不適用，才能正確轉換；不能將NA當0、USL或任意上限。原全資料／三式對照附件的其他差異仍保留，不能宣稱三式全部修正。38個直接量測／未設濃度式項目未強加公式。

## 資料安全與回復

87主檔只改enabled／初複驗濃度欄位，其餘配置及規格保持。追加87個公式版本、83日期排程、獨立F歷史247（不切換啟用F版本）。正式9表指紋完全相同；測試VariableMeasurements、UploadBatches、UploadDetails、F版本／現值、Receipts、Traces等7表完全相同。

測試生效日為2026-11-03；缺舊日期適用版本時明示拒絕，不以目前F回填10月，更不重算舊資料。回復先核對before image及新增revision，僅回復本件配置／政策指標；保留追加歷史，有新憑證即不可回溯變更。

部署過程：第一次備份範圍含release歷史備份，於網站更新前中止並改限實際backend/frontend；部分備份已清除。Portal基準舊publish-all不支援ApiOnly，已在Web DLL發布前中止；短暫維護標記移除，Web260 DLL仍為12:29原檔，HTTP200／8資產恢復。API219已透過要求的publish-all部署。現行腳本支援ApiOnly；後續隔離發布必須同時採用該已核對腳本，不能沿用舊基準的發布腳本。

## 進度表

原 `Voice/進度表.xlsx` 已同步測試及登入驗收；最終SHA256 `26c37280418586ee559f9ca47142b4f2f5579a75b170ce8d6a45bb8cf66bf8b5`。新增主表127～130及小工作265，48原生物件保持、新增11/03橙色里程碑49，登入驗收後僅更新該物件狀態名稱。核心43／SPC直接21、完成188／人工11／核銷4；其餘專案及已有完成紀錄保持。ZIP／OpenXML0、artifact重開渲染、LibreOffice13頁列印及第5／13頁檢查通過；Microsoft Excel人工開啟未做。最近備份 `Voice/備份/進度表_藥液F登入驗收同步前_20261103-153411.xlsx`；首次修正前備份仍保留。

實站原始截圖留在 `outputs/chemical-f-fix-20261103/`（C5濃度86.13與公式保留F）。API104補上舊工具缺Phase拒絕，API105修正IF／IFERROR後再驗C5有效0及10；受控停止只供SPC兩個測試站使用的集區以解除檔案鎖定，隨即恢復並重驗Web／資產MIME及實際同源API健康／版本。沒有修改binding、JWT key或角色。
