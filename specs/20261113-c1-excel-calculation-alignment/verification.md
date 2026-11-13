# 驗證紀錄

- 功能 ID：`SPC-CHEM-C1-EXCEL-ALIGN-1113-TASK-001`
- 規格版本：2
- 日期／環境：2026-11-13／PMR_SPC_TEST
- 實作狀態：完成
- 驗證狀態：通過
- 發布狀態：網站程式無變更，不需發布；API 0.1.130／test健康；正式未授權且未異動

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 六組Preview／Prepare與Excel逐格比對 | Stored三位相同 | 7.10→172.211、8.50→53.125、0.198→30.096、6.30→152.807、19.60→122.500、0.220→33.440 | 通過 |
| AC-002 | 查詢新Receipt與有效滴定值 | 0.198／30.096、0.220／33.440 | 現行Receipt的Primary與Stored逐筆相符；HistoryQuery六筆皆OK | 通過 |
| AC-003 | 核對調整／調整量 | 20.7 L、3.6 Kg、10.2 ml、15.8 ml | 鍍銅1硫酸20.7 L、硫酸銅3.6 Kg、氯離子10.2 ml；鍍銅2氯離子15.8 ml | 通過 |
| AC-004 | 比對新舊版本與Trace | 舊證據保持、新Trace v2有效 | 新公式1246～1251、revision1745～1750／F247；六筆Trace各2，舊Receipt／Trace雜湊保持 | 通過 |
| AC-005 | 備份、指紋、冪等、API健康 | 全部通過 | COPY_ONLY／CHECKSUM／VERIFYONLY、非目標指紋、verify與重跑plan pending=0；health=ok、version0.1.130／test | 通過 |

## 未執行項目、限制及後續

正式資料與正式網站不在本次授權範圍。第一次apply因排程發布與啟用同批次而被觸發器拒絕，Serializable交易完整rollback；改為先發布、再切換後重跑plan仍為原始六筆，第二次apply成功。暫時dbcreator已撤除，sysadmin未授予。

## 基準與變更紀錄更新位置

已更新`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md`與原進度表。進度表備份：`進度表-C1藥液Excel對齊前-20261113-172427.xlsx`；輸出SHA-256 `C328C55D931399E7613BBFF9E5B0BB70C4C3DB7801EDAB7E74BB401A4354616A`，登錄258／完成215／核心未完成43／SPC相關19，OpenXML 0錯誤、公式錯誤0、原生里程碑與渲染通過；Microsoft Excel人工開啟未執行。
