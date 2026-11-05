# 正式與測試三項已審核業務主檔對齊

[規格](../../specs/20261105-production-align-test-excel/spec.md)。此工具僅限PPC2425、2592、2593：規格、啟用狀態及來源定位；不複製量測、F表、排程或測試製程。實際濃度／調整／調整量公式差異會拒絕。

`dotnet run --project tools/ProductionTestBusinessAlign -- TEST_CONFIG PROD_CONFIG EVIDENCE_DIR plan|apply|verify`

正式操作須使用者授權；固定環境、資料庫及伺服器guard。apply要求恰三項差異、重複主檔無量測、正式API健康；COPY_ONLY／CHECKSUM與RESTORE VERIFYONLY後，以Serializable交易處理並保留規格／公式版本。verify應為0差異，冪等時不再次apply。受保護量測／Receipt／Trace／F／排程指紋須保持。無schema或網站發布。

回復需使用當次before清單受控還原三項主檔並追加歷史；備份整庫回復前必須處理其後其他正式寫入，不可直接蓋回。證據目錄只留本機，不提交連線設定、JSON或備份。
