# SPC-PARTICLE-EMPTY-20261022：落塵空圖與測試資料規劃

- 日期：2026-10-22；版本：1；狀態：TASK-001已完成；TASK-002資料寫入待後續執行。
- 授權：使用者最後明確要求先規劃、加入工作池，不立即執行。因此本次不寫測試資料、不改原Excel、程式或資料庫、不發布。
- 主系統：SPC；相依：TransFiles既有Particle轉檔／預覽；Portal、KM非修改範圍。
- 基準：[原Particle規格](../20260930-particle-monitoring/spec.md)、[計畫](plan.md)、[任務](tasks.md)、[本次唯讀驗證](verification.md)。
- schema／AD／角色／IIS／正式資料影響：本規劃均無；後續預期沿用既有API與資料表，不提新增migration。

## 現況與可確認證據

來源：`D:/SPC/docs/SPC文件/PD-3-257-04A-製造部落塵監控表單(黃光室) - 2026.xlsx`。
工作表 `PD-3-257-04A (0.51.0 5 10)`，832列、54欄，原始檔SHA256：`0d4d8a316354bdd27426cc6660bb2bb01050dd505fedfa936d8da36147f8b46b`；讀取前後一致。

| 觀察 | 依據／意義 |
|---|---|
| 檔名2026，量測日期實跨2023-10-25～2026-09-29 | 唯讀讀取B26:B832及既有TransFiles日期解析；不能由檔名推定只有2026資料。 |
| 截圖查詢2026-09-22～10-22／R1／0.5，檔案可解析4點 | C746=0（09/22）、C747=4（09/23）、C748=104（09/24）、C749=12（09/29）；日期取同列B欄。這是來源預期，不是已入庫筆數。 |
| 全檔R1～R9預覽候選25,301筆，發現78筆格式問題 | 依現有extract_particle_preview_rows離線解析；72筆來自2個錯誤日期，6筆為非數值說明。其餘25,223筆僅離線基本解析成功，不宣稱通過API全部驗證。 |
| 位置表頭有R1～R12，目前Particle只支援R1～R9 | TransFiles dust_reports.py的DUST_LOCATIONS及extract_particle_preview_rows篩選；不得把R10～R12改名冒充其他位置。擴充位置另案評估。 |
| 圖表0筆不能只解釋成少於20點 | ParticleCChartCalculator.cs:22起，少於20點仍回傳原始points，但不計算管制界限。若4筆已正確入庫且匹配，應保留原始趨勢。 |
| 有錯誤的Particle批次不能確認 | ParticleUploadService.ConfirmAsync，ErrorRows>0回VALIDATION_ERRORS；UploadsController.Confirm轉409。匯入預覽／舊Attribute成功，不代表已寫入ParticleMeasurements。 |
| 儀器「全部」也有查詢語意風險 | ParticleQueryService.GetSpcAsync未填deviceCode只取null，GetMeasurementsAsync未填則不過濾；前端目前提示全部。不得直接混合儀器計算；此差異未證明為本次0筆原因，因截圖原始量測也為0。 |

**實際站台、批次編號、ImportStatus、ParticleMeasurements寫入結果尚未核對；本次不連線DB、不呼叫寫入API。不把上述風險冒稱已查明本次唯一原因。**

## 格式問題與調整建議（原檔未修改）

| 位置 | 目前內容 | 建議 |
|---|---|---|
| B482 | `2025/8/26`＋換行＋`08:015` | 時分格式不合法；請核對原量測時間後，使用Excel日期時間或`yyyy/MM/dd HH:mm`。不能擅自認定為08:15。 |
| B483 | `2025/8/27`＋換行＋`08:012` | 同上，原意未確認前不自動改成08:12。 |
| C734:C739 | `量測儀外送校正` | Count只接受非負整數。未量測不能改填0；應保留為未量測／原備註。現有轉檔remark固定null，需在後續決定說明的可追溯處理方式，不假稱已有備註匯入。 |
| 表頭 | 第24列位置、第25列粒徑、第26列起資料，B欄時間 | 沿用版型；只支援R1～R9與0.5／1／5／10，Count為整數。勿任意換欄或將說明放入計數格。 |
| 採樣體積 | 現有預設轉檔未提供體積／單位 | 可規劃C圖；U圖需要每點真實正值體積且單位一致，不能補1或由UnitCount推定。 |

先另存測試副本／選定資料子集，不覆蓋或刪除原始歷史資料；有效0必須保留，空白不得改成0。

## 需求與BDD驗收

| ID | Given / When / Then |
|---|---|
| R/AC-001 | Given已回報成功匯入，When唯讀追蹤指定站台與批次，Then列明UploadType、ImportStatus、錯誤、重複及落點，區分PreviewReady／Imported與舊Attribute。 |
| R/AC-002 | Given格式錯誤，When預覽，Then能定位原工作表／儲存格與可修正建議，禁止靜默轉0、猜測日期或吞錯宣稱完成。 |
| R/AC-003 | Given同一資料序列，When查來源32點與截圖4點範圍，Then原始資料與圖表數量一致；4點只顯示不足20點提示、仍保留點，32點可計算C圖界限。 |
| R/AC-004 | Given測試資料核准寫入，When透過既有Particle預覽及確認，Then僅目標測試庫寫入，保留來源及batch；MD列頁面、輸入條件、資料範圍、筆數與回復方式。 |
| R/AC-005 | Given多儀器／多批同時間，When查詢或比較，Then不得混算／任取一批；語意依既有序列隔離基準處理。 |

## 完成定義與限制

本次僅完成規劃及來源唯讀分析；TASK-001／002未開始。後續若需修改核心，先測試、記錄目的並最小修正，測試站按AGENTS規則；正式站須另行授權。禁止藉本案改公式／統計規則、清除資料或自動開始i18n。

- 2026-10-22使用者要求優先執行：開始TASK-001，先追蹤實際批次與空圖原因；TASK-002保留相依，不並行。不授權正式寫入或schema變更。

- TASK-001最新完成：[執行驗證](task001-verification.md)。本頁規劃階段未查庫等敘述為歷史，已由新驗證更新；未開始TASK-002。

- 2026-10-22使用者明確要求寫入測試資料供實站製圖：開始TASK-002，僅SPC測試環境及有來源的32事件子集，經既有Particle API預覽→確認，記錄MD；不改原Excel或正式資料。

### TASK-002驗收追加：時間往返
Given SQL datetime2讀為Unspecified但內容為既存UTC，When取得量測／趨勢／C或U圖時間並查詢位置比較，Then回傳UTC Z且ticks不變、最新事件R1～R9均可查到；不得更新資料時間或計算公式。只發布測試API，前端及正式站不動。

- TASK-002最新完成：[輸入資料與頁面操作](task002-input-record.md)、[實站驗證及回復](task002-verification.md)。32事件1152筆僅測試庫；必要UTC標記修正已發布測試API0.1.69。原規劃階段「未開始」敘述僅保留歷史，不代表目前狀態。
