# 功能規格：SPC 使用者活動稽核
- 功能 ID：SPC-AUDIT-20261103
- 版本：2（TASK-001政策已核准）
- 狀態：TASK-001～004 DONE（測試階段；現行API0.1.106／Web0.1.119）；TASK-005～009未開始；正式未發布
- 涉及專案：SPC（主系統）、Portal／SSO（身分來源相依；本案不修改 Portal 登入）
- 授權依據：2026-11-03 使用者先要求規劃「誰登入、誰進入哪個頁面、操作了什麼都要留記錄」，加入小工作但不要立即執行；同日後續要求 SPC 直屬先執行，故依安全／權限順位開始 TASK-001。此指示不等同核准保存年限、敏感權限授權者或正式發布。
- 現行需求基準：[需求索引](../../docs/requirements.md)
- 前案或相關規格：[共用操作者解析器](../20261012-architecture-task008-audit-operator-traceability/spec.md)

## 目的、現況與證據

目的：在不改變既有登入、權限、資料寫入、公式、圖表與一般操作結果的前提下，建立可查明「經驗證的身分、進入頁面、具業務意義的操作及結果」的全站稽核能力。

TASK-001 的[事件契約v1](event-contract-v1.md)四項政策已明示核准；002完成[儲存基礎](task002-storage.md)，003完成[登入登出稽核](task003-authentication.md)，004完成[安全與權限稽核](task004-security.md)，現行測試API106／Web119。頁面／其他業務事件與查閱／保存仍在005～009。

現況盤點：

- SPC 本機密碼登入已停用；Portal SSO 交換成功後由 SPC 發 JWT。現行 `AuthController` 沒有持久化登入成功／失敗事件。
- `AuditActorResolver` 只提供 claim 解析；點位備註／排除等少數流程有使用，並非全站稽核。
- `CalibrationAuditLogs` 只服務校驗模組；藥液人工量測另借用 `UploadDetails` 保存部分異動。兩者都不適合擴充成全站活動紀錄。
- 一般 `CreatedBy`／`UpdatedBy` 只保存最後狀態，且共用寫入目前可能落成 `System`，不能重建完整操作歷程。
- Vue Router 只有登入與頁面權限守門，尚無成功進頁事件。
- 目前沒有全站稽核查詢／匯出頁、保存政策、清理／封存及容量監控。
- 現行權限目錄對 Editor 預設授予全部已登錄權限；稽核查閱不得沿用此預設，否則一般 Editor 可能讀到敏感紀錄。

## 範圍與非範圍

範圍：

1. SPC SSO 交換成功／失敗與登出請求。
2. 經權限檢查後成功進入的 SPC 頁面。
3. 安全、權限、量測、匯入確認、公式／F／版本、主檔、停用／刪除、查詢、篩選及匯出等已定義操作與結果。
4. 只能追加的稽核事件、最小權限查詢、匯出、保存／封存、容量與失敗監控。
5. 測試站端到端、效能、故障與遮罩驗收；有畫面需求時以遮罩後截圖作驗收證據。

非範圍：

- 不記錄每次滑鼠移動、捲動、鍵盤輸入或一般元件展開；這些事件噪音高、含個資風險且會增加容量。
- 不保存密碼、JWT、Authorization／Cookie、SSO signature／nonce 原值、連線字串、憑證、密鑰、完整 request／response body 或完整 Excel。
- 不預設替每位使用者自動截圖，也不把圖片 Base64 存進稽核資料表。
- 不修改 Portal 帳密驗證；本案只記 SPC 收到的 SSO 交換。若要追 Portal 帳密登入失敗，另立 Portal 跨系統工作。
- 本規劃不修改程式、資料庫、角色、IIS、測試站或正式站。

## 建議事件格式

| 欄位 | 規則 |
|---|---|
| `SchemaVersion`、`EventId`、`ClientEventId` | `EventId` 由伺服器產生；前端頁面事件只可帶 `ClientEventId` 供有限重送去重。 |
| `OccurredAtUtc`、`ReceivedAtUtc` | 資料庫存 UTC；畫面另顯示台北時間，不以本機字串當排序來源。 |
| `Environment` | 明確為 test／production，禁止跨環境混查。 |
| `ActorType`、`ActorAssurance`、`ActorOperatorId` 與 Actor 快照 | 一律由伺服器 JWT／SSO claim 與 Operator 主檔解析；未驗證登入不得把 request username 當可信 actor。 |
| `SessionIdHash`、`CorrelationId`／`TraceId` | 只存不可反推出權杖的識別值，用來串接登入、頁面與 API 操作。 |
| `Category`、`EventCode` | 採 `<domain>.<object>.<action>` 穩定代碼，例如 `auth.sso.exchange`；成功／失敗只放 `Outcome`。 |
| `PageKey`、`RouteTemplate` | 記 canonical page code 及 `/spc/control-chart/{ppcId}` 類 route template；不存完整 query／fragment。 |
| `TargetType`、`TargetId` | 事件語意以 `EventCode` 為單一代碼；批次操作記批次摘要，不逐列灌入相同事件。 |
| `Outcome`、`ReasonCode`、`HttpMethod`、`HttpStatus` | 區分 Succeeded／Denied／Failed；交易回復不可留下成功事件。 |
| `Source` | 固定為 Api／WebRouter／PortalSso／BackgroundJob；v1 建議不收集 IP 或原始 User-Agent。 |
| `DetailsJson` | 僅白名單欄位、含 schema 版本，建議最多 4 KB；before／after 以欄位摘要、版本 ID 或 hash 表示。 |
| `IntegrityHash` | v1 不納入；普通 SHA 無法防竄改，如需 HMAC／hash chain、金鑰輪替與驗證工具須另案。 |

格式不符依風險分流：一般前端事件回 400、丟棄原始 payload 且 4xx 不重試；關鍵伺服器事件視為程式錯誤，業務交易 rollback 並告警；不建立可能保存敏感原文的隔離區。只有離線舊稽核或正式附件匯入才可另案設計隔離、錯誤清單及修正後重送。調整原則為自由文字改結構化代碼、本機時間改 UTC、完整 URL 改 route template、操作者改由伺服器解析、敏感內容移除、過長內容改安全摘要。

## 需求與驗收

| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 事件格式、代碼表、敏感資料排除、保存與查閱權限先完成核准，才可建立 schema。 | AC-001 | Given 規格仍有未決政策，When 尚未核准，Then 後續 migration／登入／頁面／操作工作不得先行。 |
| R-002 | 稽核資料使用獨立、只能追加的 `AuditEvents`，不借用校驗或匯入明細表。 | AC-002 | Given 同一已驗證actor／session的ClientEventId重送頁面事件，When第二次寫入，Then回同一server EventId且不新增重複事件；產品API不提供更新／刪除。 |
| R-003 | 操作者由伺服器身分解析，不採前端或 request body 自報。 | AC-003 | Given client 偽造 `operatorName`，When 操作成功，Then 稽核 actor 仍是 JWT／Operator 對應身分。 |
| R-004 | SPC SSO 成功、拒絕與失敗均留原因碼，但不得保存機敏值。 | AC-004 | Given SSO 成功、逾期、錯誤簽章、重播或停用帳號，When 呼叫交換端點，Then 各有正確結果事件，且 DB／API／匯出不含 token、signature、nonce、cookie。 |
| R-005 | 安全與權限異動先於一般頁面事件納入稽核。 | AC-005 | Given 操作者啟停、角色／頁面權限變更或拒絕存取，When 結果完成，Then 事件包含 target、結果與 TraceId，且一般 Editor 預設不可讀稽核。 |
| R-006 | 量測、匯入、公式／F／版本、主檔、停用／刪除等高風險資料操作與稽核一致。 | AC-006 | Given 業務交易成功或 rollback，When 交易結束，Then 成功事件與資料同時成立；rollback 不得留下成功事件。 |
| R-007 | 成功進頁記 canonical page；查詢／篩選／匯出及必要純前端操作採允許清單。 | AC-007 | Given 已登入使用者進入動態路由、alias、重新整理或被重導，When Router 完成導覽，Then 只對成功頁面記 PageKey／route template；未登入重導不冒稱成功進頁。 |
| R-008 | 稽核查詢與匯出使用獨立最小權限，查詢／匯出本身也留痕。 | AC-008 | Given Viewer、一般 Editor、稽核查閱者與匯出者，When 查詢或匯出，Then 只有明示權限者成功；匯出事件可追溯且 Excel 公式注入字首已轉義。 |
| R-009 | 保存、封存、legal hold、清理與容量監控不可影響正常功能。 | AC-009 | Given 到期、保留中、封存失敗與工作重跑，When 清理執行，Then 不早刪、不重複、legal hold 不刪，失敗可重試並告警。 |
| R-010 | 頁面日誌故障不可造成白屏；關鍵寫入不得默默漏記。 | AC-010 | Given 稽核儲存暫時失敗，When 使用者查詢頁面，Then 原頁可用且有丟棄／重試監控；關鍵資料寫入依核准的原子化策略明確成功或失敗。 |
| R-011 | 測試與正式事件、權限、儲存及查詢完全隔離。 | AC-011 | Given 兩環境相同帳號與 EventId，When 產生及查詢事件，Then 不交叉寫入或查出。 |
| R-012 | 有截圖時只作遮罩後驗收證據；正式附件需另行核准。 | AC-012 | Given 驗收截圖，When 建立證據，Then 可對應環境、日期與事件 ID，且不含帳密／權杖／不必要個資；圖片不提交 Git。 |

## BDD 驗收行為

```gherkin
Scenario: SSO 成功後可追溯使用者
  Given Portal SSO 請求已通過簽章、時效、重播及啟用狀態檢查
  When SPC 發出 JWT
  Then 保存經驗證的 Operator、UTC 時間、環境、結果與 session correlation
  And 不保存 JWT、signature、nonce 或 cookie

Scenario: 業務交易回復不留下成功稽核
  Given 已登入使用者嘗試修改量測資料
  When 業務交易因驗證或資料庫錯誤回復
  Then 原資料保持
  And 稽核結果為 Failed 或 Denied
  And 不存在該次操作的 Succeeded 事件

Scenario: 成功進入頁面才記錄頁面事件
  Given 使用者已登入且具頁面權限
  When Vue Router 完成前往動態管制圖頁
  Then 保存 canonical PageKey 與 route template
  And 不保存完整 query、fragment 或敏感參數

Scenario: 一般 Editor 不可查看稽核紀錄
  Given 使用者是未明示取得稽核權限的一般 Editor
  When 呼叫稽核查詢或匯出
  Then 回應 403
  And 留下一筆拒絕存取事件

Scenario: 頁面稽核故障不拖垮正常查詢
  Given 頁面事件寫入服務暫時不可用
  When 使用者進入原本可查詢的頁面
  Then 頁面仍可正常顯示
  And 系統記錄有限重試、丟棄計數或告警
```

## 例外、邊界與效能

- 失敗登入無可信 actor 時，保存 `Unknown`／reason code 與必要遮罩來源，不把未驗證 username 當真實操作者。
- alias、redirect、動態 ID、多分頁與重新整理需定義去重；頁面事件是「瀏覽器回報成功進頁」，不可宣稱為不可偽造的業務交易證據。
- 批次匯入記批次 ID、成功／略過／失敗數與摘要；不逐筆複製相同 payload。
- 查詢預設 7 日、單次最多 31 日、游標分頁；大範圍另走受控匯出。
- 暫定效能門檻：一般讀取／頁面進入 p95 相較基準增幅不超過 5%，關鍵寫入不超過 10%；超過即不得宣稱「不影響正常功能」。

## TASK-001 已核准政策

技術範圍已固定為只記 SPC 收到的 SSO、成功進頁及有業務意義的操作；不擴張至 Portal 帳密驗證，不記滑鼠／按鍵／捲動，截圖只作 Git 外測試驗收證據。以下四項已於2026-11-03明示核准，完整內容見[事件契約第一節](event-contract-v1.md#一待核准的四項政策)：

1. 三項稽核權限全部明示授權、禁止一般 Editor 預設取得及自我授權。
2. 關鍵事件總保存 5 年、頁面／一般查詢總保存 1 年、測試截圖 90 日，legal hold 優先。
3. v1 不收集 IP 或原始 User-Agent。
4. 一般查詢只記已送出的欄位名稱、日期範圍與結果筆數，不記查詢值。

TASK-001已結清；TASK-002儲存基礎測試完成不代表全站活動收集已啟用。

## 規格版本紀錄

| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-11-03 | 初版規劃；只入工作池，不實作。 |
| 1 候選補充 | 2026-11-03 | 依 SPC 直屬優先指示開始 TASK-001，補齊 v1 技術契約；四項政策待核准，未升為可實作版本。 |
| 2 | 2026-11-03 | 四項政策核准；002儲存基礎已測試發布，003～009未開始。 |
