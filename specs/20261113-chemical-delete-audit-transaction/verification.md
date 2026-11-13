# 藥液量測刪除沿用稽核交易驗證

- 功能 ID：SPC-CHEM-DELETE-TRANSACTION-1113-TASK-001
- 日期：2026-11-13
- 實作狀態：完成
- 驗收狀態：自動化與測試站連線通過；待使用者登入實際刪除
- 發布狀態：SPC測試API 0.1.132；正式未發布

## 根因與修正
- 2026-11-13 22:38:33，測試站刪除量測94112時，`BusinessDataAuditFilter` 已開啟交易，控制器又呼叫 `BeginTransactionAsync`，於刪除前回傳503。
- 控制器現在只在沒有外層關聯式交易時自行建立交易；外層稽核交易存在時直接沿用。
- 量測改為 `IsDeleted=true` 的邏輯刪除，正常查詢不可見；不可變 Receipt／Trace 保留，避免破壞追溯證據或外鍵。
- SPC計算結果與警示仍依原行為清除，UploadDetail刪除證據與Business Audit仍在同一交易保存。

## 驗收結果
| 驗收 | 方法 | 結果 |
| --- | --- | --- |
| AC-001 | SQLite關聯式整合測試先在舊程式重現503，再驗證外層稽核交易 | 通過；刪除成功、一般查詢0筆、IgnoreQueryFilters為IsDeleted、Receipt／Trace及AuditEvent保留 |
| AC-002 | `ManualMeasurementsV1ControllerTests` | 通過；無外層交易刪除成功，非Portal日報維持Conflict |
| AC-003 | BusinessDataAudit、ManualMeasurements與ChemicalPortalMeasurement焦點回歸 | 36/36通過 |
| AC-004 | Release publish與實際測試站連線 | 通過；API 0.1.132，Web首頁／JS／CSS 200且MIME正確，同源與8901 health/version均為ok／test |

## 保護與限制
- 沒有schema migration、公式、F、Receipt／Trace或既有量測批次修補。
- 沒有代替使用者刪除量測94112；該次失敗發生在刪除前，資料未變更。
- 測試站發布前版API已備份於本機交付暫存；回復時還原前版API檔案並重做health/version驗證。
- 登入後以原畫面實際點擊刪除仍待使用者驗收，因此本件保留核心待辦。

## 進度表
- 原檔已備份後同步：登錄261、核心未完成45、完成216、人工註記12、待核銷4。
- 新增本件單日測試站里程碑與「小工作總表」列；其他專案內容保持。
- Artifact Tool重算、錯誤掃描0、重新匯入／渲染、ZIP 13 entries及OpenXML 0錯誤通過。
