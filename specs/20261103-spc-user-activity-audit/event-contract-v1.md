# SPC 使用者活動稽核事件契約 v1（已核准）

- 工作代號：`SPC-AUDIT-1103-TASK-001`
- 日期：2026-11-03
- 狀態：2026-11-03 使用者回覆「同意」，四項政策已核准；TASK-001完成
- 適用系統：SPC；Portal 只提供既有 SSO assertion，本契約不修改 Portal 帳密驗證
- 後續限制：各工作依[tasks.md](tasks.md)目前狀態逐件執行；正式發布須另行授權

<a id="一待核准的四項政策"></a>

## 一、已核准的四項政策

以下四項於2026-11-03經使用者明示同意，作為v1實作基準：

1. 稽核權限：`admin.audit.read`、`admin.audit.export`、`admin.audit.evidence` 均採明示授權，Viewer／一般 Editor 不因角色預設取得；`export` 必須同時具備 `read`，`evidence` 必須同時具備 `read`。授權者必須列入安全管理者白名單，禁止替自己授權；白名單為空時無人可授權。
2. 保存期限：關鍵事件線上 24 個月、其後封存 36 個月，總保存 5 年；頁面進入與一般查詢／篩選線上 180 日、封存至事件滿 1 年；測試驗收截圖保存 90 日。legal hold 優先於任何到期清理。
3. 網路資訊：v1 不收集 IP 或原始 User-Agent。日後若確有核准用途，再另版加入 trusted proxy、IPv4 `/24`／IPv6 `/64` 遮罩、正規化瀏覽器／OS 與最多 90 日保存。
4. 一般查詢：只記實際送到伺服器的查詢／篩選，不記使用者輸入中的每次變動；只存篩選欄位名稱、日期範圍與結果筆數，不存 lot、工單、搜尋詞或完整 query string。

## 二、範圍基準

- 記錄 SPC 收到的 Portal SSO 交換全部結果、已停用本機登入的呼叫、SPC 登出請求及 API 拒絕存取。
- 記錄權限檢查後成功進入的 canonical 頁面。
- 記錄安全／操作者／權限、量測、匯入確認、公式／F／版本、規格、主檔、停用／刪除、查詢、篩選、匯出、稽核查閱及保存作業。
- 不記滑鼠移動、每次按鍵、捲動、一般元件展開或尚未送出的篩選輸入。
- `auth.logout.request` 只表示 SPC 收到登出請求；現行 JWT 沒有伺服器撤銷能力，不宣稱「登出成功」。
- Portal 帳密頁實際登入失敗不在 SPC 可見範圍；如需追蹤，另立 Portal 工作。

## 三、事件欄位

| 群組 | 欄位 | v1 規則 |
|---|---|---|
| 識別 | `Id`、`EventId`、`ClientEventId`、`SchemaVersion` | `EventId` 由伺服器產生；`ClientEventId` 只供頁面事件有限重送去重，可空。 |
| 時間／環境 | `OccurredAtUtc`、`ReceivedAtUtc`、`Environment` | 伺服器使用 UTC；環境只允許 `Test`／`Production`，不得跨環境混查。 |
| 分類 | `Category`、`EventCode`、`Outcome`、`ReasonCode` | `EventCode` 不含成功／失敗；`Outcome` 固定 `Succeeded`／`Denied`／`Failed`。 |
| Actor | `ActorType`、`ActorAssurance`、`ActorOperatorId` | `ActorType` 固定 `User`／`Service`／`Anonymous`；Operator ID 只來自伺服器。 |
| Actor 快照 | `ActorAccountSnapshot`、`ActorOperatorCodeSnapshot`、`ActorDisplayNameSnapshot`、`ActorRoleSnapshot` | 只有身分已驗證時保存；不得採 request body／query 自報值。 |
| 未驗證登入 | `AttemptedAccountHash` | 只以各環境專用 HMAC 產生；不保存未驗證 username 原文，也不使用可猜測的普通 hash。 |
| 關聯 | `SessionIdHash`、`CorrelationId`、`TraceId` | `SessionIdHash` 由新 JWT 的隨機 `jti` 衍生，不雜湊完整 JWT；既有 token 沒有 `jti` 時可空。 |
| 頁面 | `PageKey`、`RouteTemplate` | 由後端依 allowlist 重建；不保存 query、fragment 或動態 ID 原文。 |
| 目標 | `TargetType`、`TargetId` | 使用不可變 ID；批次事件記 batch ID 與摘要，不逐列複製 payload。 |
| HTTP | `HttpMethod`、`HttpStatus`、`Source` | `Source` 固定 `Api`／`WebRouter`／`PortalSso`／`BackgroundJob`。 |
| 明細 | `DetailsJson` | 固定 schema、白名單先遮罩再序列化，上限 4 KB；不可保存任意 request／response／exception。 |

v1 不包含 `IntegrityHash`。一般 SHA hash 不能防竄改；若需要 HMAC／hash chain，須另案定義金鑰輪替與驗證工具。

TASK-002儲存補充：`ActorServiceCode`保存明確的背景服務代碼；`DeduplicationHash`只用於同client retry key的語意衝突比較，不作防竄改或來源認證，也不存每次會改變的transport trace。`ClientEventId`去重以環境、OperatorId、SessionIdHash與client id共同限定，皆由server重建可保存欄位；前端不可提供EventId或環境。

## 四、Actor 可信度

| 情境 | Actor 規則 |
|---|---|
| 已驗證 API | 以簽章有效 JWT 的 `NameIdentifier` 對應 Operator；帳號、代碼、姓名與角色由 DB／claim 受控快照。 |
| SSO 成功 | 使用已解析 Operator，`ActorAssurance=PortalAssertion`。 |
| SSO 簽章有效但帳號停用 | 可保存已確認的 Operator，`Outcome=Denied`。 |
| 簽章未驗證或錯誤 | `ActorType=Anonymous`、Operator 欄位為空，只可保存 `AttemptedAccountHash` 與固定原因碼。 |
| 背景工作 | `ActorType=Service` 並使用明確服務代碼，不使用模糊的 `System`。 |
| 頁面事件 | actor 僅由事件 API 的 JWT 解析；前端只能送 `ClientEventId` 與 allowlist 中的 `PageKey`。 |

使用者活動稽核啟用時若 `Auth:Enabled=false`，應在啟動時拒絕或明確失敗；不得以 request body 補造身分並宣稱可追溯「誰」。查閱敏感稽核資料時還須即時核對 Operator 仍啟用及 DB 現行權限，不能只信任未到期 JWT。

## 五、事件代碼

代碼格式固定為 `<domain>.<object>.<action>`，結果只放 `Outcome`：

| Category | EventCode |
|---|---|
| Authentication | `auth.sso.exchange`、`auth.logout.request`、`auth.local_login.request` |
| Security | `security.access.check`、`security.operator.create`、`security.operator.update`、`security.operator.change_status`、`security.operator.change_role`、`security.operator.change_permissions`、`security.setting.change` |
| Navigation | `navigation.page.enter` |
| Read | `read.query.execute`、`read.filter.apply`、`read.export.generate`、`read.download.authorize` |
| Measurement | `measurement.record.create`、`measurement.record.update`、`measurement.record.delete`、`measurement.record.recheck`、`measurement.batch.exclude` |
| Import | `import.batch.preview`、`import.batch.confirm`、`import.batch.delete` |
| Configuration | `configuration.formula.update`、`configuration.formula.restore`、`configuration.ftable.apply`、`configuration.ftable.restore`、`configuration.specification.change`、`configuration.rule.change` |
| MasterData | `masterdata.record.create`、`masterdata.record.update`、`masterdata.record.deactivate`、`masterdata.record.delete` |
| Audit | `audit.events.query`、`audit.events.export`、`audit.evidence.access`、`audit.retention.execute`、`audit.legal_hold.change` |

`ReasonCode` 優先沿用既有固定 API 代碼，例如 `AUTH_SSO_EXPIRED`、`AUTH_SSO_SIGNATURE`、`AUTH_SSO_REPLAYED`、`AUTH_ACCOUNT_DISABLED`。不得保存 exception、stack trace、SQL 或設定原文。後續自動化測試須列舉非唯讀 Controller action；未對應已註冊 EventCode 時失敗。

## 六、canonical PageKey／Route 對照

登入交換頁、redirect／alias 不另記頁面事件；只在 router 成功完成最終 canonical route 後回報一次。

| PageKey | RouteTemplate |
|---|---|
| `calibration.instruments` | `/calibration-instruments` |
| `measurement.entry` | `/measurements` |
| `analysis.spc.overall` | `/spc` |
| `analysis.particle.monitoring` | `/particle-monitoring` |
| `analysis.etch` | `/etch-analysis` |
| `analysis.capability` | `/process-analysis/capability` |
| `analysis.violations` | `/process-analysis/violations` |
| `analysis.spc.control_chart` | `/spc/control-chart/{ppcId?}` |
| `analysis.trend.overall` | `/trend-chart` |
| `analysis.trend.ppc` | `/spc/trend/{ppcId?}` |
| `report.monthly_control_chart` | `/monthly-control-chart` |
| `alert.overview` | `/alerts` |
| `alert.workflow` | `/alerts-workflow` |
| `master.parts` | `/parts` |
| `master.processes` | `/processes` |
| `master.characteristics` | `/characteristics` |
| `master.ppc` | `/part-process-characteristics` |
| `chemical.analysis.overview` | `/chemical-analysis-overview` |
| `chemical.ftable` | `/chemical-f-table` |
| `spc.control_chart.groups` | `/control-chart-groups` |
| `spc.rule.groups` | `/spc-rule-groups` |
| `data.upload` | `/uploads` |
| `data.upload.preview` | `/uploads/{batchId}/preview` |
| `traceability.query` | `/spc/query` |
| `spc.alerts` | `/spc/alerts` |
| `settings.smtp` | `/settings/smtp` |
| `settings.spc_reports` | `/settings/spc-reports` |
| `admin.operators` | `/operators` |
| `genealogy.overview` | `/genealogy` |
| `traceability.master` | `/traceability-master` |
| `help.guide` | `/guide` |

不記錄的路由：`/login`、`/portal-sso`。redirect／alias 對照：`/`、`/chemicals`→`/spc`；`/products`→`/parts`；`/stations`、`/machines`→`/processes`；`/inspection-items`→`/characteristics`；`/csv-import`、`/uploads/variable`、`/uploads/attribute`→`/uploads`；`/v2/work-orders`、`/v2/station-ops`→`/measurements`；`/v2/traceability`→`/spc/query`；`/v2/alerts-workflow`→`/alerts-workflow`；`/control-chart-categories`、`/control-chart-types`→`/part-process-characteristics`。

未來新增 route 時，必須明示 `auditPageKey`、redirect／alias 或 `auditIgnore`；未分類 route 的測試應失敗。

## 七、DetailsJson 白名單

| 事件類別 | 可保存明細 | 不可保存 |
|---|---|---|
| Authentication | `FlowVersion`、`ProvisioningResult`、`LegacyAssertion` 布林值 | username 原文（未驗證時）、密碼、JWT、signature、nonce、cookie、email、部門 |
| Security | `ChangedFields[]`、`PermissionCodes[]`、`PreviousStatus`、`NewStatus` | 權杖、完整 Operator 物件、任意 request body |
| Navigation | `NavigationKind`、`PreviousPageKey` | 完整 URL、query、fragment、動態 ID 原文 |
| Read／Filter | `FilterFields[]`、`DateFromUtc`、`DateToUtc`、`ResultCount` | lot、工單、搜尋詞、篩選值、完整 query string |
| Export | `ExportFormat`、`ResultCount`、`DateFromUtc`、`DateToUtc` | 匯出內容、檔案路徑、下載 token |
| Measurement | `ChangedFields[]`、`Revision`、`Recheck`、`RecordCount` | 完整 before／after、原始量測 payload |
| Import | `SourceType`、`TotalCount`、`SucceededCount`、`SkippedCount`、`FailedCount`、`ContentHash` | 原檔、完整檔名／路徑、Excel 內容、逐列錯誤 payload |
| Formula／F | `VersionId`、`EffectiveDate`、`ChangedFields[]`、`CanonicalHash` | 完整公式、F 表內容、密鑰或未遮罩設定 |
| MasterData | `ChangedFields[]`、`BeforeHash`、`AfterHash`、`RecordCount` | 完整資料列或任意序列化實體 |
| Audit／Retention | `FilterFields[]`、日期範圍、筆數、archive checksum、legal hold ID | 查出的事件內容、`DetailsJson` 複本、加密金鑰 |

明細超過 4 KB 時，不可截成無效 JSON。改存安全摘要、前 N 個非敏感 ID、`Truncated=true` 及總筆數。遮罩器失敗時不得退回未遮罩原文。

## 八、敏感資料負面清單

任何事件、站外補報、匯出或截圖均不得包含：

- 密碼、JWT、Authorization header、Cookie、SSO signature／nonce 原值。
- 連線字串、憑證、金鑰、環境秘密或完整應用設定。
- 完整 request／response body、完整 exception／stack trace／SQL。
- 完整 Excel、匯出檔、來源檔內容或使用者本機／UNC 路徑。
- 未經核准的完整 IP、原始 User-Agent、email、部門及未驗證帳號原文。
- 查詢中的 lot、工單、搜尋詞、完整 query／fragment。

遮罩必須在序列化與限長前完成；不得先保存原文再事後清理。

## 九、失敗策略

| 類型 | 範例 | 策略 |
|---|---|---|
| 關鍵 DB 寫入 | 權限、角色、量測修改／刪除、匯入確認、公式／F／規格、主檔、legal hold | 業務資料與事件使用同一 DbContext／交易；事件失敗即 rollback 並 fail closed。 |
| 關鍵成功入口 | SSO 成功、稽核查閱／匯出、敏感資料匯出 | 必要事件持久化後才發 JWT／資料；稽核不可用回安全 503。 |
| 已拒絕／已失敗 | 錯誤簽章、401／403、驗證失敗、交易 rollback | 維持拒絕／rollback；事件失敗以站外 structured log、TraceId、計數及告警補報，不得改成放行。 |
| 一般活動 | 成功進頁、普通查詢／篩選、無副作用 preview | fail open；有限重試、`ClientEventId` 去重，耗盡後計數／告警，不在瀏覽器長期保存 payload。 |
| 登出 | `auth.logout.request` | 不阻擋本機清除 token／離站；寫入失敗只告警。 |
| 外部副作用 | Email／Chat、檔案產生、外部網路呼叫 | 先保存 intent／outbox 再執行；結果另追加並可靠重試，不宣稱 SQL transaction 可涵蓋外部系統。 |
| 保存／封存 | 到期封存、清理、legal hold | 對一般網站 fail open；對刪除 fail closed。checksum、抽樣還原或告警失敗時不得刪線上資料。 |
| 權限／actor／遮罩不明 | 無法即時核對權限、actor 解析失敗、遮罩器例外 | 關鍵事件 fail closed；一般頁面事件丟棄並告警，不保存原文。 |

頁面事件屬 best-effort 瀏覽器回報；突然關閉或網路中斷可能缺漏。契約不宣稱頁面事件零遺漏。登入、敏感查閱與關鍵業務寫入則不得無痕成功。

格式錯誤不建立原文隔離區：一般前端事件回 400、丟棄 payload，4xx 不重試；關鍵伺服器事件視為程式錯誤並 rollback／告警；只有離線舊稽核或正式附件匯入才可另案設計隔離與修正清單。

## 十、保存、封存及 legal hold

| 類別 | 線上 | 封存 | 總保存 |
|---|---:|---:|---:|
| 登入、安全、拒絕存取、角色／權限、量測／公式／F／匯入／刪除、稽核查詢／匯出 | 24 個月 | 後續 36 個月 | 5 年 |
| 頁面進入、一般查詢／篩選 | 180 日 | 至事件滿 1 年 | 1 年 |
| 測試驗收截圖 | 90 日 | 不封存 | 90 日 |

- 以 `OccurredAtUtc` 計算期限，不因顯示時區提前刪除。
- 封存筆數、範圍、checksum、加密金鑰可用性及抽樣還原全部通過後，才可刪線上資料。
- 正式自動清理啟用前先 dry-run，輸出預計處理範圍與數量；不得輸出 `DetailsJson`。
- legal hold 記錄套用人、原因碼、範圍、開始／解除 UTC；有效 hold 優先於到期政策。
- runtime 身分只允許受控 INSERT／SELECT，不得 DDL、UPDATE、DELETE；migration、封存／清理使用分離身分。

## 十一、截圖證據

- v1 截圖只作測試／驗收證據，不是 `AuditEvents` 的一部分，也不自動擷取正式使用者畫面。
- 原則上只擷取測試站、測試帳號與隔離資料；裁切無關區域，遮罩帳號、姓名、量測值、lot／工單、query、token 與開發工具內容。
- 移除 EXIF；另存 SHA-256、環境、日期、事件 ID、建立者與到期日；放在受 ACL 控制的 Git 外證據區，90 日清除。
- 截圖不能作為身分或完整操作歷程的唯一證據。
- 若需正式附件，另立 `AuditEvidence` 工作，處理惡意檔掃描、MIME／大小白名單、加密、下載稽核及案件保存政策。

## 十二、BDD 核准門檻

```gherkin
Scenario: 一般 Editor 不因角色預設取得稽核權限
  Given 啟用中的 Editor 未被明示授予 admin.audit.read
  When 查詢稽核事件
  Then API 回應 403 且不回傳事件欄位
  And 保存拒絕存取事件

Scenario: 撤銷稽核權限立即生效
  Given 使用者持有尚未到期且曾包含稽核權限的 JWT
  And Operator 主檔已撤銷 admin.audit.read
  When 再次查詢稽核事件
  Then 伺服器依目前主檔回應 403

Scenario: 成功登入的稽核故障時不發出權杖
  Given Portal SSO 驗證全部通過
  And 關鍵稽核儲存無法接受事件
  When SPC 嘗試完成交換
  Then 回應 503 且不發 JWT
  And 觸發安全告警

Scenario: 關鍵資料修改與稽核保持原子性
  Given 使用者具備修改 F 版本的權限
  And 稽核事件寫入失敗
  When 使用者送出修改
  Then F 版本及相關資料保持原值
  And API 不回應成功

Scenario: 頁面事件故障不阻擋一般頁面
  Given 使用者具備頁面權限
  And 頁面事件端點暫時不可用
  When Router 完成導覽
  Then 原頁正常顯示並只做有限重試
  And 失敗計數增加且不保存未遮罩 payload

Scenario: 未驗證登入內容不成為可信身分
  Given SSO 請求帶有偽造 username 且簽章錯誤
  When SPC 拒絕請求
  Then ActorOperatorId 為 null
  And 不保存 username 原文、signature、nonce 或 token
  And ReasonCode 為 AUTH_SSO_SIGNATURE

Scenario: 查詢操作不保存敏感篩選值
  Given 使用者用 lot、工單與日期查詢資料
  When 查詢完成
  Then 事件只保存篩選欄位名稱、日期範圍及結果筆數
  And 不保存 lot、工單或完整 query string

Scenario: 封存驗證失敗時不得刪除線上事件
  Given 一批事件已到保存期限
  And 封存 checksum 或抽樣還原失敗
  When 清理工作執行
  Then 線上事件筆數保持不變
  And 工作失敗並產生告警
```

## 十三、核准與版本規則

- 2026-11-03使用者明示核准第一節四項政策；TASK-001已完成，本契約v1為TASK-002～009共同基準，整體規格版本升為2。
- 任何新增敏感欄位、延長保存、正式截圖附件、Portal 帳密事件或 IP／User-Agent 蒐集，都必須升版並重新做隱私／容量／權限評估。
