# 小型變更：作業人員 Portal 比對與頁首導覽恢復
- 功能 ID：SPC-OPERATORS-PORTAL-COMPARE-1111-TASK-001
- 版本：1
- 狀態：測試站完成／待登入畫面驗收
- 專案／授權依據／需求基準：SPC；使用者 2026-11-11 回報返回入口網站、登出按鍵消失，且比對 Portal 品保人員失敗。

## 問題、預期行為與範圍
- 問題一：SPC 測試前端若以未啟用驗證的模式建置，頁首的返回入口網站、登入人員與全部登出會整組消失。
- 問題二：`PortalDirectory:ApiBaseUrl` 若誤指到回傳 HTML 的站台，HTTP 200 內容會在 JSON 反序列化時拋出例外，畫面只顯示未預期錯誤。
- 預期：測試站以 `testhost` 建置並保留 Portal 導覽／登出；人員比對固定連到 Portal 測試 API。上游非 JSON 或契約不符時，API 回可理解且不洩漏內容的 502。
- 範圍：SPC Web、SPC API 的人員比對讀取、防呆測試及測試站設定／發布。
- 非範圍：不寫入或同步人員、不修改權限、Portal 程式、正式站、正式資料庫及 schema。

## 需求與驗收
- R-001：驗證啟用的 SPC 頁首必須顯示返回入口網站與全部登出。
- R-002：Portal 人員名單為合法 JSON 陣列時可完成唯讀比對。
- R-003：Portal 回傳 HTML、空白或不符契約時不得拋出未處理例外，須回 502 與 `DIRECTORY_RESPONSE_INVALID`。
- AC-001：測試站實際主資產以 `testhost` 模式建置，頁首控制存在。
- AC-002：合法名單單元測試通過；不合法內容單元測試取得 502 與固定錯誤碼。
- AC-003：測試站首頁、主 JS/CSS、API health/version 與瀏覽器使用的 API 基底驗證成功；人員比對端點在登入後由使用者畫面驗收。

## 計畫與任務
- 受影響檔案：`OperatorDirectoryComparisonController.cs`、API 測試、測試站 Web/API 設定與本規格／需求／變更／工作池文件。
- 回復：還原測試站發布前 Web/API 備份與原測試設定；不涉及資料回復。
- [x] T-001：先建立合法 JSON 與非 JSON 的回歸測試。
- [x] T-002：集中安全解析 Portal 名單並回傳明確 502。
- [x] T-003：以 testhost 建置並發布測試站，修正 Portal API 來源後完成連線驗證。
- [x] T-004：同步需求、工作池、變更紀錄與原進度表。

## 驗證
- Happy Path：Portal 回 200 JSON 陣列，SPC 回比對摘要。
- Boundary／Invalid：Portal 回 200 HTML／空白／錯誤 JSON，SPC 回 502 固定錯誤碼，不保存回應內容。
- Regression：既有人員清單、權限維護與同步寫入流程不變；本次不執行同步。
- 實際結果及證據：API 合法／不合法回應與既有同步交易回歸 6/6；頁首控制 1/1；三語目錄 2,863 鍵 × 3 語及 testhost build 通過。測試站 Web／主 JS／CSS 皆 200 且 MIME 正確，API 0.1.122、environment=test；Portal 上游名單 200、JSON 陣列 22 人。匿名人員比對 401。
- 發布狀態：測試站 API 0.1.122／Web 0.1.127 已發布；正式站未授權。
- 限制或未決問題：合成登入受既有資料庫權限規則正確擋為 403，未繞過；需由已登入且具 `admin.operators` 的使用者實際點選比對，確認畫面後才能 DONE。完整舊 i18n 測試另有 2 項既存失敗，與本件頁首／人員比對範圍分離。
