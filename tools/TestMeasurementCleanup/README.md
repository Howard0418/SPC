# 測試量測／匯入清理維護工具

僅用於使用者明確核准的 SPC 測試區清除，不是網站功能或排程。僅接受環境 `test`、伺服器 `172.16.110.16`、資料庫 `PMR_SPC_TEST`；正式連線只可唯讀比對。設定檔與輸出證據不能提交。

命令：`TEST_CONFIG PROD_CONFIG OUTPUT_DIR plan|gate-test|apply|verify [TEST_MAINTENANCE_CONFIG]`。

1. 先在輸出目錄提供已驗證的 `approved-lines.json`：核准來源活頁簿完整路徑、SHA256、G欄、48筆精確檔案／工作表／格位／核准線別；產生時使用 `integrations/transfiles/production-reconciliation/approved_lines.py`。來源不同或雜湊改變拒絕執行，不覆蓋來源檔。
2. `plan`：實際DB／API環境、主鍵排序SHA256、12表可查詢列數、受保護表及正式資料唯讀盤點。
3. 另獲測試trigger修正授權才可 `gate-test`／`apply`。既有獨立維護設定只用於備份與授權封存規則，不提升應用程式身分權限。隔離SQL測試建立唯一 `SPC_FORMULA_SCHEMA_VERIFY_CLEANUP_` 資料庫，驗證後移除；十項行為、目前trigger雜湊及一小時有效證據是apply前提。
4. `apply`：COPY_ONLY／CHECKSUM備份、VERIFYONLY後，保留原trigger定義，只加入純封存例外。0→1、非新增且所有業務欄位相同；只排除封存／更新稽核欄及SQL自動rowversion。新增、改值、還原與封存後修改仍檢查原憑證。不停用trigger、不改主檔／角色／正式trigger。SQL Server將ALTER定義回傳為CREATE，僅正規化此語法及換行再核對。
5. 單一Serializable交易內封存白名單12表，量測每批200筆避免trigger二次方負荷；先停止未完成統計重試並留原因，不假標完成。受保護表SHA256及總實體列數須保持；任一檢查失敗全交易rollback。
6. `verify`：另一連線重驗12表可查詢0、全部啟用且有圖型項目的實際SPC圖表服務均無資料、API健康／版本及正式SHA256保持。

回復：依備份與before證據的逐表原ID、原UpdatedAt／UpdatedBy及工作狀態進行受控回復；原trigger定義可單獨ALTER回復。缺憑證舊資料還原會被保護規則拒絕，須另核准及評估維護回復，不可關閉trigger或偽造憑證。整庫回復僅能在確認無後續寫入並另核准時進行。不得直接整庫覆蓋後續資料。

封存保留不可變Receipt／Trace、AuditEvents、來源防重證據及實體量測列；清除後相同來源仍受既有防重限制。本工具不重匯、不啟用來源排程、不改Portal或網站發布。
