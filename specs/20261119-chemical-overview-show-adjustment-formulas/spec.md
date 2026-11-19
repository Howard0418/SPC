# 線別分析項目總覽恢復顯示調整公式

工作代號：`SPC-CHEM-SHOW-ADJUST-1119-TASK-001`

## 需求與範圍

- 主系統：SPC Web；無 Portal、API 或資料庫變更。
- 在線別分析項目總覽的螢幕畫面恢復顯示「調整公式」與「調整量公式」兩欄。
- 兩欄在檢視及編輯模式皆可見，沿用既有讀取、編輯、保存、版本歷史與 Excel 匯出行為。
- 不恢復 Portal 藥液新增／查詢頁已隱藏的「調整、調整量」欄位。
- Rollback：恢復 screen media 下第6、7欄隱藏樣式；不需資料回復。

## BDD 驗收

- Given 使用者開啟線別分析項目總覽
- When 使用桌面或手機寬度查看資料列
- Then 表頭與資料列均顯示11欄
- And 調整公式與調整量公式顯示既有內容或「未設定」
- When 使用者進入編輯模式
- Then 兩式輸入框均可見且原值保持
- And 保存仍提交濃度公式、調整公式、調整量公式與既有設定

## 驗證與發布

- 更新既有 Playwright 回歸，涵蓋桌面／手機欄位數、兩式可見與保存不回歸。
- 執行相關畫面測試、i18n 檢查與 test build。
- 遞增 SPC Web 版本並發布測試站；API版本不變。正式站須另行授權。

## 驗證結果（2026-11-19）

- 原始碼檢查：兩式控制存在且screen隱藏selector已移除。
- `npm run build:test`：通過；i18n 2,865 keys × 3 locales通過。
- Playwright目標案例已更新為桌面／手機11欄及兩式可見；執行時本機Vite WebServer啟動後連線中斷，`page.goto`回`ERR_FAILED`，未進入欄位斷言，故不冒稱瀏覽器案例通過。
- 已發布SPC測試站Web 0.1.150；API未修改，維持0.1.142／test。
- 測試站首頁、主JS、主CSS、API health/version皆200且MIME／test環境正確；發布資產包含兩式控制與Web 0.1.150。
- 備份：`release/test/frontend.backup-show-adjustment-20261119-133837`。正式站未發布。

## 2026-11-19 再驗證

- 重新執行 `chemical-formula-history.spec.ts` 的 3 個 overview 案例，Vite WebServer 啟動後仍在 `page.goto('/chemical-analysis-overview')` 回 `ERR_FAILED`，三案皆未進入欄位斷言；未發現產品程式錯誤證據。
- Windows 原生電腦操作 pipe 與 browser access host 均不可用，無法以自動瀏覽器替代人工登入驗收。
- 原始碼及測試站發布狀態維持：兩式欄位存在且 screen 隱藏規則已移除；本輪無程式修改、不重複發布，仍待使用者人工畫面驗收。

## 2026-11-19 人工驗收

- 使用者已確認線別分析項目總覽畫面，調整公式與調整量公式顯示結果通過。
- 本次只核銷測試區人工驗收狀態，沒有程式、API、資料庫、設定或網站發布變更；正式站仍須另行授權。
