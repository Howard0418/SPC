# 驗證紀錄

- 功能 ID：`SPC-CHEM-N2-TIN-PRECISION-1113-TASK-001`
- 規格版本：2
- 日期／環境：2026-11-13／SPC測試站、`PMR_SPC_TEST`
- 實作狀態：完成
- 驗證狀態：通過
- 發布狀態：網站程式不適用；正式未授權

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 0.10 Preview／Prepare | Raw 0.2256、Stored 0.226、精度3 | 正式計算服務回傳Raw `0.2256`、Stored `0.226`、DecimalPlaces `3` | 通過 |
| AC-002 | 公式版本及主檔設定比對 | v4僅精度改3 | 建立公式1244/v4；以JSON語意比對，除`decimalPlaces: 2→3`外與796/v3相同，F仍為248／B9=1 | 通過 |
| AC-003 | 排程與舊證據比對 | 新revision，舊資料保持 | active revision 1743完整29段；11/07開放段改1244/F248。舊revision1741、29段、Receipt與Trace v1序列化比對未變 | 通過 |
| AC-004 | 量測及查詢驗證 | 94084目前值及有效值0.226，Trace v1/v2完整 | 量測94084為0.226並連結新Receipt；Trace共2版，HistoryQuery有效值0.226／OK | 通過 |
| AC-005 | 備份、指紋、冪等、環境 | 全部通過 | COPY_ONLY／CHECKSUM／VERIFYONLY通過；受保護資料指紋不變；verify及第二次plan pending=0；test health/version成功 | 通過 |

## 未執行項目、限制及後續

網站程式與schema未修改，因此不需重新發布；正式資料庫未連線／未異動。使用者可重新載入測試站日報確認該列為0.226。

## 基準與變更紀錄更新位置

已同步`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md`、`ai_docs/10_change_log.md`及原進度表。

## 自動化與安全驗證

- `dotnet test ... --filter FullyQualifiedName~Pt1CurrentScheduleTests`：13/13通過。
- 受控工具Release build：0警告／0錯誤；重跑plan為`pending=0`，公式1244/v4、revision1743、值0.226、Trace 2。
- 暫時`dbcreator`僅供備份，finally後狀態`PMR_SPC_TEST|SpcAuditApiTest003|0|0|1`，未授予sysadmin。
