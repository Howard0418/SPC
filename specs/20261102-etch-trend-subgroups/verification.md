# 咬蝕趨勢子組顯示修正驗證
- 2026-11-02使用者截圖確認舊趨勢折線逐原始樣本畫點；前次只驗raw載入，不能當作25／50樣本子組業務驗收，已更正紀錄。資料庫逐點、匯入防重及原管制圖Xbar-S對帳不因此失效。
- 原因：TrendChartView直接讀rawDataPoints。修正只在咬蝕正背面呈現，以伺服器chartData.points既有xbar／n為來源；PT1／QE1每面25、PT2／QE2每面50，一完整子組一點。不依日期自行合併、不任意切原始樣本、不回退raw掩蓋缺組。
- 摘要的N／平均／標準差／全距用完整子組平均值；Cp／Cpk沿伺服器原始子組內變異指標，不用平均值標準差重算。原始樣本直方圖及AD／常態曲線維持原始樣本口徑且明示，子組平均不當作可修改的單筆樣本做排除或備註寫入。

## 測試與實際畫面
- 先新增測試缺實作紅燈，再完成helper；6項子組測試＋2項三語模板／參數測試共8項通過：25／50與0、同日不同子組及B面、不足樣本／空平均／缺chartData、非咬蝕／速率不變、樣本數設定不一致、舊特性代碼以明確A／B名稱及線別辨識。
- 16組既有9／10月真實圖表資料驗證：10300個原始樣本呈現270個伺服器子組平均，全部xbar吻合且無缺組。包含原已存在的X排除日報50個舊樣本／2個子組，未刪改既有量測，不與本次合格來源10250／268混算。
- 測試Chrome：QE1九月500樣本→20點，每組25；PT2十月750→15點，每組50；普通藥液PPC2883仍20點且原始趨勢不變。測試Web114／API97，首頁／主JS／CSS均200且MIME正確，health/version及實際同源API載圖成功。最終JS index-B0ur2fmG.js。
- 使用隔離HEAD工作樹建置測試，不打包既有未提交normalizeSummaryResponse修改；原工作區該11行保留未提交，其他工作區修改亦保持。三語2767keys、build:test成功；既有bundle體積及Browserslist提示未擴張處理。

## 正式發布與回復
- 使用者後續明確回覆「發布正式SPC網站」，授權本次前端修正，不涉及重匯或資料庫修改。正式目標WinRM核對PMR-SPC-SRV／SpcWeb／C:/inetpub/wwwroot/production/frontend，API production／0.1.92。
- 首次從目前開發HEAD建包，檔案核對誤把應保留的web.config也當替換內容而報hash不符；實際web.config與備份一致、其餘資產匹配，已修正包清單排除設定檔。其後正式UI發現該包帶入測試區既有設備導覽移除差異，立即以原備份index回復正式113，沒有改資料。
- 為避免未授權回歸，正式改從當前正式月報005基準bfb9ef91另建隔離工作樹，只套本次8個前端檔案差異（helper／測試／view／三語／版本），App.vue、權限及其他程式不變。基準的8項測試及build:production再次通過；正式CSS仍index-CLEpViWr.css，保留舊正式直方圖樣式與設備導覽，不把舊TASK-006測試配色偷偷發布。
- 最終正式前端114：index-DP4UXC11.js，Web首頁／主JS／CSS均200且MIME正確；8443 API health/version為production／0.1.92。remote內容hash與交付包一致、web.config與備份一致，沒有發布API或修改IIS／Portal／DB。
- 正式Chrome最終QE1九月325樣本→13個平均點，每組25；標題、子組數與樣本數分開，設備三個既有入口仍在。PT2十月750→15點，每組50。正式實際API基底載入成功，沒有API離線或跨連接埠問題；畫面保留本機formal-qe1-subgroups.png／formal-pt2-subgroups.png，不提交截圖。
- 最終正式回復備份：`C:/ProgramData/PMR-Releases/Production/etch-trend-20261102-202220/frontend-before`；回復前核對是否有後續發布，僅回復本次前端入口／資產，不動API或DB。測試原始113備份為release/test/frontend.backup-etch-trend-subgroups-20261102-200912。
- 正式只套正式基準的本次修正；開發分支保留原測試區既有程式，需重製正式包時以bfb9ef91加本次前端差異，不能整包混發當前測試站。前端差異SHA256 `f8e8951b4ba21fccc5156d38eba2ce507f66ce226b72bf18050d38de0de405b8`。

## 狀態
- 本次子組呈現修正的Spec／BDD／build／測試與測試、正式發布驗證完成；既有TASK-002仍有GUI交付／日期時區與來源例外，核心35／SPC13保持，不新增或重複核銷工作。
- 原Excel同步後補記；沒有重複匯入、刪除或更改任何量測、公式F、規格或權限。

原Excel已同步250／252列、平台版本、摘要及同表明細；新增可編輯綠色正式修正里程碑（40→41物件），原PT2正式資料綠色標記與其他全部物件保持。未授權儲存格／parts保持；ZIP／OpenXML候選與原路徑0錯誤、作者重讀渲染／公式錯誤掃描與LibreOffice11頁、相關4／11頁目視通過。未做Excel人工開啟。備份`進度表_咬蝕趨勢子組修正前_20261102-202349.xlsx`，輸出SHA256 `61d2edc7f586911f86078ad5b6d0c47b652a41fa9ab270e744e1327aaf003a99`。
