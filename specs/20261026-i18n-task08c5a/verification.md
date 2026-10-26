# 08C5A 驗證與發布
- 2026-10-26 DONE。共用轉接先建測試（缺captureApiErrorDisplay紅燈）再實作，115前端單元通過。受驗證API描述獨立immutable快照，namespace/args/64節點/深度3限制沿用；非法details回原訊息或已知根摘要，未知key仍原文字。純文字外部參數／0／HTML與i18n符號不重編譯，巢狀本機UI args先轉API子描述為字串。無新譯鍵，2622鍵三語draft保持。
- getApiErrorMessage保留字串介面，getApiErrorDisplay供reactive狀態；料號、品質特性、製程與管制群組四頁僅替換錯誤存取／顯示，不改API／DTO／授權／選項與業務判定。其餘頁面與逐列DTO留C5B。
- 本機45 API情境加四頁4案例通過，逐例切三語並核主DOM同一實例、搜尋值及query不變、沒有新API請求。已知父子錯誤更新、未知／非法原文保持；全部業務API mock，無資料寫入。shell17、主檔39、製程／群組相關56回歸通過（含不同視窗寬度、草稿／寫入DTO、Viewer、原始衝突資訊）。發布後49同組驗收通過。
- build:test通過，既有chunk大小與EquipmentPoints table結構提示未屬本批。API未修改、不重跑無關後端測試；CAL-VERIFY-001/002原兩項既有差異保持待辦。
- 唯讀核172.16.110.16 / PMR_SPC_TEST pending=[]，IIS SpcSingleTest/實體backend D:\SPC\release\test\backend且AppEnvironment=test。前端完整備份 .codex-tmp/i18n-task08c5a/backup-frontend 104檔hash吻合、發布4檔assets先index後，4個保護設定不變、未重啟pool。實際API version/health200/0.1.81/test/ok，Web0.1.101四檔SHA256等於dist，同源14 API驗證通過；正式及Portal未修改。
- 回復：核同一test路徑，還原backup-frontend資產後index最後，不動backend/appsettings/web.config/資料；核原版本與health。無DB回復需求。
- 剩16件直接接續C5B；真人審稿留20。附件掃毒外部受阻1、Runtime及TransFiles人工故障驗收暫緩2、校驗基準差異2另列，不冒稱完成。
