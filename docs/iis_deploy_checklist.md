# IIS 部署檢查清單（VS Code + ASP.NET Core）

## 1. 伺服器前置

- [ ] 已安裝 IIS（含管理主控台）
- [ ] 已安裝對應版本 .NET Runtime
- [ ] 已安裝 ASP.NET Core Hosting Bundle
- [ ] 安裝完成後已執行 `iisreset`

## 2. 專案發佈（在 VS Code）

- [ ] 執行任務：`publish:iis-release`
- [ ] 發佈輸出路徑為 `publish/backend`
- [ ] 發佈資料夾中可看到 `web.config` 與主程式 dll

## 3. IIS 站台設定

- [ ] Application Pool 已建立
- [ ] Application Pool 設為 `No Managed Code`
- [ ] Site 實體路徑指向發佈目錄（例如 `publish/backend`）
- [ ] Site Binding 已設定（http/https、port、host）

## 4. 權限與網路

- [ ] App Pool 身分對發佈資料夾有讀取權限
- [ ] 若需要寫入（上傳、log、暫存），已給必要寫入權限
- [ ] Windows 防火牆已放行站台 port

## 5. 啟動驗證

- [ ] IIS 站台可正常啟動
- [ ] 首頁或健康檢查端點可回應（例如 `/health`）
- [ ] 若失敗，查看 Event Viewer（Application）
- [ ] 若失敗，查看 ASP.NET Core stdout log（若有啟用）

## 6. 常見問題快速排除

- [ ] 未安裝 Hosting Bundle（最常見）
- [ ] App Pool 未設為 `No Managed Code`
- [ ] 發佈目錄權限不足
- [ ] Runtime 版本不符
- [ ] `web.config` 缺失或內容錯誤
