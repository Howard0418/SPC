# 驗證紀錄

- 功能 ID：`SPC-CHEM-N2-P400-SCHEDULE-1113-TASK-001`
- 規格版本：2
- 日期／環境：2026-11-13／SPC 測試站、`PMR_SPC_TEST`
- 實作狀態：完成
- 驗證狀態：通過
- 發布狀態：網站程式不適用；正式未授權

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | plan／apply 後查詢 active revision | 2515=v4/1157、F248、11/07 起開放 | active revision 1742／revision no 4，共29段；唯一開放段自 UTC 11/06 16:00 起，公式1157、F248 | 通過 |
| AC-002 | 前後指紋與新增筆數核對 | 舊歷史及保護資料保持 | 精確複製28段並新增1開放段；新增1修訂／29區間。原55份Receipt及Trace、量測、Job、公式、F、PPC與規格保持 | 通過 |
| AC-003 | 11/12 Preview／Prepare | 0.17 可完成且不留測試 Receipt | `2026-11-12T00:00:00+08:00`、Primary 0.17 的 PreviewAsync／PrepareAsync 成功，命中公式1157與F248；savepoint回復後Receipt仍55份 | 通過 |
| AC-004 | verify 及重跑 plan | pending=0、無新增 | verify與第二次plan均為`pending=0 / remaining=0 / protected=unchanged` | 通過 |
| AC-005 | test API health/version；正式隔離 | test 正常、無網站／正式操作 | `health=ok`、`version=0.1.130`、`environment=test`；正式未連線／未寫入，網站未發布 | 通過 |

## 未執行項目、限制及後續

- 備份：`PMR_SPC_TEST_N2_P400_CURRENT_SCHEDULE_20261113-155429.bak`，`COPY_ONLY`、`CHECKSUM`、`RESTORE VERIFYONLY`通過。
- 沿用既有最小權限流程暫授 `SpcAuditApiTest003` 的 `dbcreator`；`finally` 已撤除，最終狀態 `PMR_SPC_TEST|SpcAuditApiTest003|0|0|1`，未授予 `sysadmin`。
- 單元測試 `Pt1CurrentScheduleTests` 9/9通過；受控工具 Release build 0 warning／0 error。
- 證據目錄：`release-staging/n2-p400-current-formula-schedule-20261113/`（不提交）。
- 正式環境、網站發布及 2026-11-07 前歷史空缺回填皆未執行。

## 基準與變更紀錄更新位置

- 已同步 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md`、`ai_docs/10_change_log.md`。
- 原進度活頁簿已新增完成列與原生里程碑；備份 `進度表-N2-P400公式排程前-20261113-161200.xlsx`，最終 SHA-256 `CAF45454285E6D1431B498AF64B977ACA7C971EC92B8E9079D9EE7A33269D223`。Artifact Tool 重讀／渲染、公式錯誤0、ZIP 13 parts／重複0、drawing anchors 105→106、OpenXML 0錯誤；核心未完成42、SPC相關19。
