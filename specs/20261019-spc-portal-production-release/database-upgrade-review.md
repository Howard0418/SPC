# 正式資料庫更新審查（待另行授權）

應用程式正式發布已授權；以下正式 DB schema 變更另行確認，尚未執行。

| 系統 | EF Migration | 實際變更 |
| --- | --- | --- |
| SPC | 20261008183026_AddSpcPointRemarks | 新增 SpcPointRemarks、索引及對既有量測/批次/管制項目的限制型外鍵；不改寫既有量測值。 |
| Portal | 20261012031000_ExpandFeatureSettingsMessage | feature_settings.message 從 nvarchar(500) 放寬為 nvarchar(max)，保留既有內容。 |
| Portal | 20261015053702_AddGroupInsuranceWorkflow | 新增 group_insurance_applications、group_insurance_import_batches、group_insurance_mail_results、group_insurance_import_errors、group_insurance_members 5 表與索引/新表關聯；不匯入人員/團保名冊、不寄送通知。 |

## 已核對
- 以上新表在正式 DB 不存在，message 欄長度現為 1000 bytes（nvarchar(500)）。
- 以 EF 實際 GetMigrations 與 IMigrator 產生供審查的 idempotent SQL，未執行 SQL。SQL 雜湊列於隔離交付 manifest。
- F表 3 表已存在；其舊 migration 類別沒有 DbContext 標註，不被現行 EF 探索，因此不補記、不重建、不列本次遷移。
- 來源沒有、正式 history 已有的舊 Migration ID 只記錄差異，不刪除或篡改。

## 執行順序與回復界線
1. 取得此三筆 schema 更新的明確授權；必要測試通過。
2. 記錄維護開始，核對站台目標，停止兩系統業務寫入；備份正式網站/IIS及兩個正式 DB。
3. DB 使用獨立新檔 COPY_ONLY/CHECKSUM 備份，完成 RESTORE VERIFYONLY 才可進行遷移；備份失敗不得覆寫網站。
4. 依 EF 產生的 SQL 套用並核對 migration history，接著先發布 SPC，再 Portal API/Web；保留環境/SSO/附件與儲存設定。
5. smoke 不通過時先停止新寫入，回復同批網站備份；新增表與放寬欄位優先保留，不自動 Down/drop 或截短欄位，以免資料遺失。
6. 若需整庫還原，須另外核對備份後新增資料及停寫時間點，不自行覆蓋正式業務資料。

## 套件與環境
- SPC API 0.1.60 / Web 0.1.63；Portal API 1.0.178 / Web 1.0.221 已隔離產包。
- 正式 SPC 前端沿用已部署包的既有正式 API 位址與 HTTP SSO 拓撲，不新增 IIS binding、代理模組或安全降級；VITE_APP_ENV=production，Portal 回跳改為現有正式 HTTPS 登入。
- 這不宣稱既有 SPC HTTPS 前端的跨協定 API 限制已改造；同源 API/IIS 拓撲升級若需實施，另立小工作。
- Portal Web 既有正式 PortalApi:BaseUrl 可被新版辨識；SPC 既有 JWT 金鑰存在且長度通過，只保留原值、不輸出金鑰。
- 四份交付 ZIP 排除 appsettings*.json、web.config、上傳/私有資料/keys/logs；逐檔清單與 SHA-256 已建立。未傳送至正式主機或套用。
