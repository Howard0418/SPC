# 驗證狀態

## 原規劃紀錄（執行前；最新A結果見下節）
- 2026-10-27：只做截圖對照與唯讀原始碼盤點，確認既有CharacteristicNameEn欄位、主檔雙欄及部分投影未帶英文；未連業務資料庫，不宣稱英文資料完整率。
- 已檢查規格內容與本地相對連結。
- 程式／自動化測試／build／部署：未執行；本次為使用者指定的純規劃，皆不適用。
- A／B／C均未實作，不標DONE。預定AC1～6驗證見spec及plan，執行時記錄真實結果。
- 發布狀態維持現有測試Web0.1.107／API0.1.85；本次沒有新發布，正式未更新。


## I18N-CHAR-001-A 2026-10-27完成
- AC1／2／3：先執行Node核心測試，helper不存在而Red；完成後名稱選用／非法型別／空白／缺值／代碼／最後fallback／原物件與文字字面值皆通過。全前端語系124項通過，2734鍵×三語一致；新增1鍵／3列仍draft。
- AC1／4：三頁依locale重新呈現；主檔操作成功提示保存中英文名稱快照，可在顯示期間繼續切語；搜尋原中英文保留並補特性代碼OR比對。中文原主檔不自動簡繁轉換。
- AC4／6：本機6個新增名稱切換測試通過；發布後主檔21、量測40、管制項目44，共105項隔離瀏覽器測試全部通過。既有Viewer／CRUD／原DTO／量測ISO與數字／Soft Hold／LSL0.2／公式與排程／歷程等斷言保留，未呼叫真實業務寫入API。
- 手機390與桌面1280新增案例涵蓋中英文搜尋、缺英文回退、文字不執行HTML／i18n模板、DOM／選取ID／量測草稿不變，切語無額外GET或寫入。原fixture中停用第二筆也共用英文名，初次新測試錯估為一筆，修正fixture預期為兩筆；不是放寬產品條件。
- 發布後額外補驗主檔2項：等待既有抽屜入場動畫完成並確認右界在viewport內才截圖；管制項目2項：英文下拉名稱與ID51在切語後保持，均通過。手機／桌面英文編輯截圖已目視核對，中英文兩欄原值、ValueLabel及儲存按鈕可讀，原UI不是改寫主檔資料。
- build:test通過；僅前端修改，後端build／DB migration不適用，API維持0.1.85。圖表／摘要與人讀報表接線未完成，保留B／C。
- 實際IIS SpcSingleTest/指向D:/SPC/release/test/backend且AppEnvironment=test。前端發布前完整備份120檔至D:/SPC/.codex-tmp/i18n-char-a/backup-frontend並逐檔hash核對；只更新4個dist檔、assets先index後，4個設定保護檔hash不變，不重啟pool。
- 真實HTTPS version／health為200、test／ok、API0.1.85；Web0.1.108四檔SHA256與dist一致，同源14項通過。正式站未更新，原業務資料／schema／權限／API DTO不變。
- Rollback：先核對相同test IIS physicalPath與環境，把上述前端備份還原release/test/frontend，回Web0.1.107，API原0.1.85保持；核對index／assets hash與health，不需DB回復。本次未實際執行回退。
- 截圖與檢查輸出留ignored .codex-tmp/i18n-char-a，不提交；fallback新資源review-a.json是必要翻譯初稿帳冊，真人審稿未完成。
- A完成後三語化剩15件（SPC B／C兩件＋原09～21十三件）；只完成本件，不自動開始下一件。


## I18N-CHAR-001-B 2026-10-27完成
- 核心先測：新增summary欄位、monitor英文名與helper尚不存在時預期Red。修正測試本身RawDataPoints為object的型別用法後，3個真實InMemory投影測試通過；與既有圖表說明／報表／隔離HTTP合計32項必要API測試通過。
- API：summary新增CharacteristicName／CharacteristicNameEn／CharacteristicCode，monitor、警報與計量／計數追溯補英文名。原ChartName、代碼、ID、0數值、原歷史Message／Values／查詢結果不變；不寫入計算結果或原主檔，不真寄信。
- 5個後端來源檔（SpcService／AlertDashboardController／TraceabilityV1Controller／SpcController／ControlLimits）移除明示新增顯示欄位後，全文與前版024b91d9相同；原SQL／篩選／運算／角色及例外分支皆保留。
- 前端125項語系測試通過；summary helper只使用明示品質特性欄位，沒有新metadata的舊DTO／獨立chartName按原文字呈現；空白英文回退原中文，資料不突變。無新增翻譯鍵／無機器翻譯主檔。
- 本機16項名稱瀏覽器測試及4項警報英文搜尋補驗通過，390／1280、SPC monitor／摘要查詢繪圖／trend title／警報兩頁／追溯均涵蓋。直接深連結SPC的原版版型不一定顯示標題，初次測試誤假設必定存在；修正為核對monitor名稱，另以正常summary→繪圖流程驗標題，不擅改版型。
- 本機8個圖表案例實際檢查ECharts相同instance、series及zoom，locale只更新呈現；trend title與DOM標題同步。所有新名稱依文字處理，含<b>／@:／{name}不執行HTML或i18n模板。原代碼與歷史診斷保留。
- 發布後16項名稱驗收通過；原圖表查詢38、警報48、趨勢18、追溯40共144項回歸通過，合計160項隔離瀏覽器驗收，包含CSV原bytes、查詢／寫入原DTO、P值邊界、摘要cache及歷史drawer。所有業務API fixture攔截，不向真實業務API寫入或寄信。
- build:test與Release dotnet publish通過；既有Vite大chunk及設備table/tr提示未在本件擴張處理。新測試源碼／規格不含發布產物或憑證。
- preflight：test／172.16.110.16／PMR_SPC_TEST，pending migrations=[]。IIS SpcSingleTest/實際physicalPath為D:/SPC/release/test/backend，pool為SpcSingleTest；只操作該測試pool。
- 成對完整備份在D:/SPC/.codex-tmp/i18n-char-b/backup/backend與frontend，逐檔hash核對。發布94個runtime及4前端dist檔，保護4個設定資料檔hash不變，測試pool已重啟。
- 實際HTTPS /api/version、/api/health皆200，test／ok、API0.1.86；前端Web0.1.109四檔SHA256符合dist，同源14項通過。正式站未更新，無schema migration／業務DB資料修改。
- Rollback：核對相同test實際IIS目標後，只停SpcSingleTest，以本件前後端完整備份回復Web0.1.108／API0.1.85，保護設定資料並重啟驗version／health／前端hash；無DB回復需求。本次未實際執行覆寫回退。
- 原始測試／發布證據保留ignored .codex-tmp/i18n-char-b，不提交。譯文真人審稿仍在TASK20，未冒稱完成。
- B完成後三語化剩14件＝SPC C一件＋09～21十三件。化學總覽／F表等剩餘名稱使用點與報表／郵件依C完整盤點處理，本件不宣稱全SPC最後驗收完成，不自動開始C。
