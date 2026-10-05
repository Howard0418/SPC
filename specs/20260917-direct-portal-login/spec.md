# SPC 直接導向入口網站登入
- 功能 ID：20260917-direct-portal-login；版本 1；狀態：完成，測試前端已發布。
- 授權：使用者確認移除 SPC「使用 AD 帳號或工號登入」過渡畫面，直接到入口登入，完成後回 SPC。
- 基準：CHANGELOG_CUSTOM.md 2026-09-08 統一 Portal SSO；維持此登入機制。
## 範圍與驗收
- R-001/AC-001：SPC /login 不呈現登入卡片、AD 按鈕或登入說明，直接進既有 Portal /Spc/Launch，由其導向登入頁或沿用有效登入。
- R-002/AC-002：未登入進受保護頁面時保存安全的站內路徑，SSO 完成回原頁；不接受 // 外站回傳路径。
- 僅改 SPC LoginView 呈現與導向時機，不恢復本機帳密、不改 Portal、後端或身份驗證。
## 計畫與任務
- [x] 移除過渡模板與未使用 import，掛載前發起既有導向。
- [x] Playwright 驗證直接導向/原頁返回/外站 redirect 拒絕；build:test；備份並發布 SPC 測試前端。
- [x] 同步索引及變更。
## 驗證與發布
- Playwright `npx playwright test --config playwright.login.config.ts`：3 passed，exit 0。驗證無 AD 卡片、模擬 SSO 回到原受保護頁、拒絕外站 redirect。測試專用 Vite 5193 收尾需終止本次測試程序後正常退出，未重跑。
- `npm run build:test` 成功；沿用既有大型 chunk 與無關 EquipmentPoints table 結構警告，本次不擴充修正。
- 發布目標核對 IIS SpcWeb=D:/SPC/release/test/frontend；備份 direct-portal-login-20260917-102853。保留 web.config，index 與產物 hash 一致；未發布後端/Portal/正式站。
- 真實未登入瀏覽器自測：SPC `http://172.16.110.27:8083/login` 自動抵達 `http://172.16.110.27/Login?ReturnUrl=%2FSpc%2FLaunch`，不需按 AD 登入按鈕。
- 真实公司帳密登入未執行；SSO 返回原頁以模擬登入資訊驗證，Portal 驗證機制維持既有實作。
- [測試程式](../../frontend/mes-spc-web/tests/direct-login.spec.ts)，本次文件連結檢查通過。
