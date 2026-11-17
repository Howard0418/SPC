# 驗證紀錄

- 功能 ID：`PORTAL-CHEM-SAVE-FRESH-PREVIEW-1113-TASK-001`
- 規格版本：3
- 日期／環境：2026-11-17／Portal正式區
- 實作狀態：完成
- 驗證狀態：自動化、使用者驗收及正式環境驗證通過
- 發布狀態：Portal正式區 API 1.0.252／Web 1.0.277

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | `node --test tests/chemical-effective-ui.cjs` | 舊token後再preview並用新token confirm | 16/16通過，第二次preview token送入confirm | 通過 |
| AC-002 | `node tests/chemical-effective-trace.cjs` | 初／複驗依原始滴定值刷新 | 18項assertion通過；有複驗刷新兩階段，無複驗只刷新初驗 | 通過 |
| AC-003 | preview／confirm失敗測試 | 保留輸入、不誤報成功 | preview失敗不confirm且草稿保持；SPC 409防護未放寬 | 通過 |
| AC-004 | 直接輸入回歸 | 不呼叫日期preview | UI測試證明直接輸入直接confirm且preview 0次 | 通過 |
| BUILD | `dotnet build src/PmrPortal.Web/PmrPortal.Web.csproj -c Release --no-restore` | 無錯誤 | 0 warning／0 error | 通過 |
| RELEASE | `scripts/publish-all.ps1 -Configuration Release` | 測試站可用 | API 1.0.243／Web 1.0.284；Login、JS、CSS均HTTP 200且MIME正確 | 通過 |
| API | Portal實際API基底與SPC測試API | 健康、版本及環境正確 | Portal `172.16.110.27:8091/health`為test；SPC health/version為0.1.130/test | 通過 |
| LIVE | 登入後實際按儲存 | 不再出現過期預覽錯誤且保存成功 | 使用者確認已驗收並授權正式發布 | 通過 |
| FORMAL | 正式發布 `portal-chem-save-fresh-formal-20261117` | 正式區版本與環境正確 | API 1.0.252／Web 1.0.277；發布證據 `phase=complete`、`status=completed` | 通過 |
| FORMAL-WEB | Login、`site.js`、`site.css`、`chemical-effective-trace.js` | HTTP 200且MIME正確，功能資產存在 | Login 200；JS為`text/javascript`、CSS為`text/css`，功能內容存在 | 通過 |
| FORMAL-API | 正式 health、同源API及部署DLL | production環境、路徑可達、版本一致 | `:8443/health`為ok/production；`/api/auth/me`回401；DLL為API 1.0.252／Web 1.0.277 | 通過 |

## 回歸說明

- 藥液相關較大測試組中，日期日報判定1項、歷史欄位索引3項及舊二位精度6項仍為既有過時測試基準；本件未修改其功能或擴張範圍。
- 原公式、F、排程、資料庫、schema與既有量測均未修改。

## 回復

以正式發布工具回復本次部署前備份；無資料庫回復需求。

## 未完成

無。本件已完成使用者驗收、正式發布及正式環境驗證。
