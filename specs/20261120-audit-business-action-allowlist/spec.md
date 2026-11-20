# SPC-AUDIT-BUSINESS-ACTIONS-1120-TASK-001

- 狀態：測試站完成，待人工驗收
- 規格版本：2
- 主系統：SPC Web／API；沿用既有`AuditEvents`欄位，以migration擴充ClientEventId既有check constraint

## 目的與範圍

在既有進頁、查詢、匯出及伺服器端高風險資料稽核之外，補上「使用者在Web送出哪一類指定業務按鈕」的安全摘要。只記白名單動作、頁面、成功／失敗、HTTP狀態與可信登入者；不記每次點擊、按鍵、捲動、輸入原文、request body、網址query或敏感值。

第一版白名單：一般儲存／刪除、匯入預覽／確認／刪除、公式儲存／還原、F表套用／還原、人員儲存／刪除、異常確認／處置儲存、設定儲存、通知測試及報表立即寄送。只有對應API實際送出並收到回應才記錄；純前端驗證未通過、不送API的按鈕不記。

前端事件是操作體驗證據，採best effort，不取代伺服器端與業務資料同交易的權威稽核。正式站仍須另行授權。

## BDD

- Given 已登入使用者在白名單頁面成功送出公式儲存，When API回成功，Then 稽核新增固定事件`interaction.business_action`，摘要只含固定`ActionKey=formula.save`，不含公式、輸入值或request body。
- Given 白名單API回400／409／500，When 前端收到回應，Then 記相同ActionKey、`Failed`及合法HTTP狀態，不保存錯誤本文。
- Given 非白名單API、GET、稽核端點或只在文字框輸入尚未送出，When 使用者操作，Then 不新增業務按鈕事件。
- Given 稽核上傳因暫時錯誤重試，When相同ClientEventId再次送達，Then只保留一筆；不得阻擋原業務頁面。
- Given 有稽核查閱權限者查詢或匯出，When事件包含ActionKey，Then活動頁與Xlsx顯示安全動作代碼，不回傳原始DetailsJson。

## 風險與回復

- Happy Path：白名單成功動作及活動頁摘要。
- Boundary：REST版本前綴、動態ID、204回應、一次重試冪等。
- Invalid Input：偽造ActionKey／PageKey／Outcome／HTTP狀態、任意文字與敏感欄位皆拒絕。
- Regression：既有進頁、查詢、匯出、交易式業務稽核、401重導與寫入環境保護保持。
- 資料庫：不新增欄位或改業務資料；只讓既有ClientEventId約束接受固定的業務操作事件，以支援重試冪等。測試庫先建立COPY_ONLY／CHECKSUM備份並核對msdb完成紀錄再migration；目前最小權限帳號不能執行RESTORE VERIFYONLY，限制須明示保留。
- 回復：回復前版Web／API並保留既有append-only事件；若需DB回復，以migration Down還原原約束，須先確認沒有新事件仍帶ClientEventId。

- 2026-11-21 既有測試站已完成成功、失敗、未送出與安全摘要驗收。[驗證](verification.md)
