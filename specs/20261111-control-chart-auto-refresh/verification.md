# 驗證紀錄

- 功能 ID：`SPC-CONTROL-CHART-AUTO-REFRESH-1111-TASK-001`
- 規格版本：1
- 日期／環境：2026-11-11／Test
- 實作狀態：完成
- 驗證狀態：自動化通過；實際畫面待測試站發布後驗收
- 發布狀態：測試站發布受阻；正式未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 儲存成功後以同 PPC 圖表接收通知 | 自動查詢並重繪 | `control-chart-refresh*.test.mjs`：儲存成功只通知一次；兩種圖表訂閱後只在目前 PPC 相符時重載。 | 通過 |
| AC-002 | 不同 PPC／儲存失敗測試 | 無額外圖表查詢 | 靜態整合測試確認通知位於成功 PUT 後；不同 PPC 不呼叫重載，卸載時解除訂閱。 | 通過 |
| AC-003 | 通道例外與 production build | 保存不受阻、建置通過 | storage fallback 對無效訊息安全忽略；4 項 Node 測試與 `npm run build:production`（0.1.128）通過。 | 通過 |

## 未執行項目、限制及後續

- 測試站登入後的跨分頁實際畫面驗收待發布後執行。
- 2026-11-11 已建立測試發布包 `spc-chart-auto-refresh-20261111`，測試站發布 Plan 連線遭 `PC-PA16560048.pmr.com.tw` PowerShell Remoting 回覆「存取被拒」阻擋；未傳送檔案、未寫測試或正式資料庫，正式站未發布。待既有測試站維護身分取得 WinRM 發布權限後，以同一 manifest 重試 Plan／Apply。

## 基準與變更紀錄更新位置

- 已同步 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md`；原進度表因本機無法取得規定的 artifact operation 標記工具且測試發布尚受阻，列為待發布結果一併更新，不宣稱已同步。
