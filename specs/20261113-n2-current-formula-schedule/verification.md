# 驗證紀錄

- 功能 ID：`SPC-CHEM-N2-CURRENT-SCHEDULE-1113-TASK-001`
- 規格版本：2
- 日期／環境：2026-11-13／SPC 測試站、`PMR_SPC_TEST`
- 實作狀態：完成
- 驗證狀態：通過
- 發布狀態：網站程式不適用；正式未授權

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | plan／apply 後查詢 active revision | 2516=v4/1158、2518=v3/796、F=248、11/07 起開放 | PPC2516 revision 1740、PPC2518 revision 1741；各29段，開放段各1且公式／F正確 | 通過 |
| AC-002 | 前後指紋與新增筆數核對 | 舊歷史及保護資料保持 | 精確複製56個舊區間並新增2個開放段；新增2修訂／58區間。量測、Receipt、Trace、Job、公式、F、PPC與規格指紋保持 | 通過 |
| AC-003 | 11/12 兩項服務預覽 | 唯一命中且成功 | `2026-11-12T00:00:00+08:00` 兩項 PreviewAsync 均成功，回傳公式1158／796與F248 | 通過 |
| AC-004 | verify 及重跑 plan | pending=0、無新增 | verify與第二次plan皆為`pending=0 / remaining=0 / protected=unchanged` | 通過 |
| AC-005 | test API health/version；正式隔離 | test 正常、無程式發布／正式操作 | `health=ok`、`version=0.1.130`、`environment=test`；正式未連線／未寫入，網站未發布 | 通過 |

## 未執行項目、限制及後續

- 初次 apply 在 `RESTORE VERIFYONLY` 因帳號缺少 VERIFY 所需角色而停止；備份完成但未進入資料交易，資料未變。
- 沿用既有最小權限流程暫授 `SpcAuditApiTest003` 的 `dbcreator`；`finally` 已撤除，最終狀態 `PMR_SPC_TEST|SpcAuditApiTest003|0|0|1`，未授予 `sysadmin`。
- 共用發布服務揭露 PPC2516 有27個舊匯入Receipt仍使用舊修訂662的公式／F；交易拒絕且回復。受控工具改為精確複製目前active revision並附加未來段，不修改該27個Receipt或任何舊歷史。
- 備份：`PMR_SPC_TEST_N2_CURRENT_SCHEDULE_20261113-153944.bak`，`COPY_ONLY`、`CHECKSUM`、`RESTORE VERIFYONLY`通過。
- 單元測試：`Pt1CurrentScheduleTests` 9/9通過。工具Release build 0 warning／0 error。
- 證據目錄：`release-staging/n2-current-formula-schedule-20261113/`（不提交）。
- 正式環境、網站發布及既有歷史空缺回填皆未執行。

## 基準與變更紀錄更新位置

- 已同步 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md`、`ai_docs/10_change_log.md`。
- 原進度活頁簿已新增完成列與原生里程碑；備份 `進度表-N2氯化亞錫公式排程前-20261113-154312.xlsx`，最終 SHA-256 `1F5EEF91E528A3C7543223B327C26B3C17B77CD93F432F26ED7549CCFA776503`。Artifact Tool 重讀／渲染、公式錯誤0、ZIP 13 parts、繪圖錨點105→106、OpenXML 0錯誤；核心未完成42、SPC相關19。
