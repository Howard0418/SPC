# TransFiles 咬蝕預覽確認匯入 v1
R1/AC1：Portal登入（品保角色＋SPC既有Admin/Editor），先逐日報唯讀預覽兩站，顯示新增/略過/衝突；明確確認後匯入。隔離資料不送出。
R2/AC2：兩端伺服器檢查測試設定、實際SQL連線PMR_SPC_TEST/PMR_PORTAL_TEST及172.16.110.16；Portal呼叫SPC限制172.16.110.27:8081；TransFiles Portal固定測試8091。正式站拒絕。
R3/AC3：Portal與SPC都採嚴格新增/相同略過，不覆寫不同資料。確認再檢查並鎖同日期線別，保留點位/來源/操作者。
R4/AC4：兩庫非分散式交易；Portal先建立Pending、SPC嚴格同步後才標SUCCESS，失敗不假報成功，可重新預覽重試，相同不新增。既有日報不同時拒絕。
R5/AC5：轉檔工具保留純轉換及既有藥液流程，新增確認操作/逐份結果，帳密/token不寫檔。必要測試後升版、打包、只發布兩測試API，不改資料庫結構或正式站。

狀態：實作、自動驗證、兩測試API發布及工具打包完成；登入後GUI/跨站confirm待人工驗收。[驗證](verification.md)。
