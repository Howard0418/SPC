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
