# 正式驗證紀錄

- 正式 SPC 啟用 Audit:SecurityEnabled，僅變更該設定，保留登入與操作稽核、既有最小權限連線、金鑰與原有安全管理者名單（0 筆）。未授予任何個人權限。
- SQL 隔離探針檢查啟動條件、pending 0、主檔、人員和稽核交易可寫／回復。安全稽核測試 36 項通過，1 項需獨立 SQL 環境略過。正式 Web build 及 2,899 鍵三語檢查通過。
- 正式 Web 0.1.149→0.1.150，API 0.1.140/production 維持。Chromium 確認首頁、主 JS/CSS MIME、前台版本及實際跨來源 8443 health/version。正式 SpcApi pool 為 Started，匿名 Operators 401。正式檔案雜湊與已驗證候選一致。
- 安全管理者尚未指定，無實站真人敏感授權，禁止自授權規則維持。測試站原已啟用，本次未部署。按鈕與資料庫內容均未更動。
- 前台版本來源 1214061d，回復點 SPC-PROD-1123-SECURITY-AUDIT-WEB150。可同批恢復原設定與入口並 recycle API。

## Administrator 指派

- 正式唯讀核對 administrator 為唯一有效 Editor，內部 ID 1078。本次只新增既有白名單設定中的該 ID，安全稽核保持，未修改人員主檔及個人稽核權限。
- 權限回歸 13 項通過，含敏感授權禁止自行增加；正式 Web 0.1.150 → 0.1.151／API 0.1.140/production 保持。建置、三語、首頁、主資產 MIME、版本及瀏覽器實際 8443 health/version 通過。已核對正式啟用設定雜湊。
- 前台隔離來源 e5d5ad34，回復點 SPC-PROD-1123-SECURITY-ADMIN-WEB151。實際授權須由 administrator 登入並明示替他人儲存。
