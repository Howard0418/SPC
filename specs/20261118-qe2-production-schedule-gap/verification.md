# 驗證紀錄

- 功能 ID：`SPC-CHEM-QE2-SCHEDULE-GAP-1118-TASK-001`
- 日期／環境：2026-11-18／正式 `PMR_SPC_2026`
- 實作：完成；驗證：通過；網站發布：不適用（資料修正）

| 驗收 | 結果 |
|---|---|
| 七項 11/02 唯一排程 | PPC2474～2480 active revision更新為295～301；各由25段精確複製並新增1段，現在各26段。 |
| 公式／F | 沿用正式既有公式42、43、44、45、47、48、49及F歷史2；計算公式、複驗公式、精度及F cells與測試該日相符，未新增公式或F。 |
| 預覽 | 七項×台北00:00／12:00／23:59:59共21組初驗，正式Raw／Stored／Judgment與測試一致；邊界區間為11/02 00:00含、11/03 00:00不含。 |
| 保護資料 | 量測、receipt／trace、PPC、規格、公式/F及非目標排程指紋保持；原revision/effectivity不可變歷史保持；policy `IsEnabled=false`原值保持。 |
| 冪等 | apply後獨立verify為`pending=0 / remaining=0 / addedRevisions=0`。 |

## 備份與回復

- 正式備份：`PMR_SPC_2026_QE2_SCHEDULE_20261118-100902.bak`，`COPY_ONLY／CHECKSUM／RESTORE VERIFYONLY`通過。
- 新增7個revision及182個effectivity（175段原歷史複製＋7段缺口），沒有UPDATE／DELETE舊排程。
- 若七個新區間尚未產生receipt，可受控把policy指回原revision 255～261；若已有receipt，不可直接切回破壞追溯，須另案評估。

## 程式與連線驗證

- `Pt1CurrentScheduleTests` 16/16通過；受控工具Release build 0 warning／0 error。完整Chemical專案135項為133通過／2失敗；失敗皆為既有`CalendarTests`日期資料篩選得到0，單獨重跑仍相同，與本件排程規則、工具及正式資料表無交集，未在本件擴張修正。
- 正式 SPC 首頁、主JS `index-BlzugLq4.js`、CSS `index-m59bBhiY.css`均HTTP200且MIME正確。
- 正式SPC API `/api/health`與`/api/version`均200，`0.1.134／production`；瀏覽器資產使用的正式API基底維持HTTPS8443。
- Portal Login與`/app-version`均200，Web `1.0.278.0`；實際API基底`https://login.pmr.com.tw:8443/health`為production、database ok。
- 本次沒有網站檔案、API、靜態資產或設定發布，因此SPC Web0.1.140／API0.1.134及Portal Web1.0.278／API1.0.253保持，不虛增版本。

## 進度同步

- 原活頁簿已備份為`進度表_QE2正式公式排程修正前_20261118_1015.xlsx`後取代；候選重讀、公式錯誤0、ZIP封裝可讀，最終SHA-256 `51AE05FD4BBEBF897D61AB48CB939F298691429A3CBB7A1F0D0FA114D8C6CCF7`。
- 本工作新增後同次完成，不增加核心未完成數；中央核心未完成維持28件。
