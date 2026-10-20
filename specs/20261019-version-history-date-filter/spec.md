# 功能規格：三類版本歷史依修改日期篩選
- 功能 ID：SPC-VERSION-HISTORY-DATE-FILTER-20261019；版本2（已獲授權並完成）。
- 狀態：DONE；2026-10-20完成測試庫migration、驗證與SPC測試站發布，正式未更新。
- 授權：使用者2026-10-19要求只規劃並加入小工作，已確認用途為「依修改日期區間篩選歷史」。
- 主系統：SPC；Portal／TransFiles／KM無程式變更。
- 相依：[公式備註與共用歷史](../20261019-chemical-formula-reference-history/spec.md)先完成；本案接續同一公式區域，不併行。

## 現況、目的及範圍
- 藥液公式已有依PPC查詢版本API，以ChangedAt記錄修改時間，目前GET未提供日期參數。
- F表前端ChemicalFTableView.vue以take=20查版本；SpcController內ChemicalFTableController的versions僅有take，須避免先截取最近20筆再篩日期而漏資料。
- 規格界線目前找到ControlLimitSegmentsController：依PPC列出StartDate／EndDate生效分段，不能直接當作「修改時間歷史」。未確認完整的規格異動快照／稽核來源，執行時必須先確認；不得把生效日冒稱修改日。
- 本案涵蓋規格界線、藥液分析公式（總覽與設定頁）、藥液F表版本歷史的查詢篩選。保留原有共用版本來源、順序與回復規則，不改各版本生效期間，不改公式套用或回算。

## 日期格式建議與需求
| 需求 | 規則 | 驗收 |
|---|---|---|
| R-001 | 起日／迄日日期選擇器，顯示YYYY-MM-DD（如2026-10-01～2026-10-19），提供查詢及清除；預設無日期限制並保留原分頁。 | AC-001：三類歷史都能查詢／清除；公式兩頁同PPC同區間結果相同。 |
| R-002 | 依伺服器記錄的修改時間，採Asia/Taipei日界，含起迄日全天；內部使用起日00:00含、迄日次日00:00不含，與儲存時區一致轉換。 | AC-002：同日、月底、跨年、00:00及迄日最後瞬間皆正確，隔日00:00排除。 |
| R-003 | 允許只填起日或迄日；起日大於迄日、無效日期或格式不符，前後端拒絕並提供可修正訊息。 | AC-003：提示「起日不可晚於迄日」或「請使用YYYY-MM-DD有效日期，例如2026-10-19」；不悄悄對調或猜日期。 |
| R-004 | 後端先按項目與日期篩選，再排序及分頁／take；維持穩定排序，查詢／清除重置頁碼。 | AC-004：超過20筆時較早區間仍查得到全部符合筆數，不因原take漏掉；無符合資料清楚提示。 |
| R-005 | 查詢唯讀、沿用權限；修改／回復權限及項目隔離不變，載入錯誤可重試。 | AC-005：不可跨項目混資料、不新增版本、不改設定或資料，既有回復功能回歸通過。 |

## BDD
- Given版本超過20筆且目標在較早日期，When指定該修改日期區間，Then後端先篩選，分頁可取得全部符合結果。
- Given迄日2026-10-19，When查詢，Then包含台北時間10/19全天，不包含10/20 00:00。
- Given起日晚於迄日或2026-02-30，When送出，Then明確指出日期錯誤，不變更歷史資料。
- Given同一公式PPC，When總覽與管制項目設定頁選相同日期，Then版本結果及備註一致。

## 限制與需先釐清的技術差異
- 規格界線若只有生效分段而無修改稽核歷史，先記錄可用來源、缺口與最小方案；不可虛構過去修改時間或直接用生效日期替代。若需新增歷史寫入／schema，更新本案規格、測試與備份計畫後再實作，正式migration另授權。
- 舊紀錄若無修改時間，不推估；需明示缺資料且說明日期篩選無法匹配。日期格式驗證與時區測試必須覆蓋現有欄位實際DateTime口徑。
- 不加版本有效期間設定、排程生效、歷史重算或任意編輯歷史。

## 版本紀錄
1：2026-10-19，使用者確認日期查詢用途後新增規劃，與前案拆成循序兩件小工作。

## v2 前置核對與具體擴充提案（2026-10-19，未執行）
- 確認ControlLimitSegment繼承BaseEntity，僅保留CreatedAt／UpdatedAt等最後狀態；ControlLimitSegmentsController.Update直接覆寫，Delete直接刪除，沒有各次快照。PPC基本USL／LSL／Target也直接覆寫。AppDbContext.HandleAuditFields只更新時間且actor預設System，非完整異動紀錄。CalibrationAuditLogs用途限校正，不挪用。
- 因此無法只加日期參數即提供規格「版本歷史」。依已同意執行計畫的條件，需先確認下列新增資料寫入／schema範圍；公式與F表本身沿用既有表，不新增。
- 建議新增SpecificationLimitVersions：Id（bigint）、PartProcessCharacteristicId、VersionNo（同PPC連續）、SourceKind（Base／Segment）、SourceSegmentId（可空，刪除仍保留識別）、ChangeType（Create／Update／Delete）、PreviousSnapshotJson／SnapshotJson、ChangedAt（UTC）、ChangedBy。PPC＋VersionNo唯一索引、PPC＋ChangedAt索引；歷史不隨分段刪除級聯清除。
- 快照只涵蓋工程規格USL／LSL／TargetValue與分段StartDate／EndDate、識別及備註；不擴大改管制線計算或生效規則。相同規格不建重複版；設定變更與建版同交易，失敗全部回復。
- 在應用程式EF寫入邊界統一捕捉PPC規格欄位與分段新增／異動／刪除，需涵蓋已發現MasterDataV2Controller、ControlLimitSegmentsController及MigrationController的EF路徑；由伺服器取得實際登入者，無真人上下文才標System。SQL庫外手動更新不冒稱已能追蹤。
- 舊資料不回填虛構的過去版號／修改時間。上線前既有設定仍保留、可查看目前內容，歷史提示「啟用版本追蹤前的異動未留存」；僅追蹤啟用後真實異動。
- 新增只讀規格版本查詢API，依同PPC與修改日期區間；不新增規格歷史回復／改寫功能。規格設定／分段視窗加入版本歷史入口，其日曆生效期欄位與本次修改日期篩選明確區分。
- 這是本案一件小工作的必要擴充，不另外新增待辦數。只有使用者同意後才實作EF migration及測試庫更新；正式DB／發布仍須另行授權。

## v2追加驗收（待同意後執行）
- AC-006：兩種規格來源的新增／修改／刪除皆保留真實前後快照、操作者及時間；修改未變不建版，版本號／時間排序穩定，同PPC隔離，交易失敗不留半筆。
- AC-007：既有規格與量測資料不變，舊歷史缺口明示，無假造回填；EF migration於正式庫複本以外的隔離測試庫演練，可驗證備份與還原，不操作正式庫。
2：2026-10-19，來源核對後發現規格未留版本，新增必要schema提案待確認，未執行DDL／資料回填。

2026-10-20：使用者同意v2擴充、測試庫migration與必要測試站發布；正式DB與正式發布不在本次範圍。

## 2026-10-20 測試庫既有F表歷史缺口
實際隔離庫SQL查詢揭露ChemicalFTableVersionHistories缺表；測試庫亦缺，既有20261007090000_AddChemicalFTableVersionHistories缺DbContext／Migration標記而未被EF辨識。為完成已授權F表日期查詢，補正既有migration註冊，納入同次隔離Up／Down與測試庫備份驗證；不改其既有欄位設計、不改F值、不補造歷史，不操作正式庫。新增核心測試鎖定migration discoverability；正式日後須額外核對舊表及migration紀錄是否一致，不能直接複製發布包執行。

## 完成狀態（2026-10-20）
已實作規格基本值／分段統一EF歷史、三類台北修改日期篩選與分頁、UTC回應及過期快照拒絕；無虛構回填。相關37測試與9瀏覽器測試通過，隔離SQL演練／交易與併發驗證通過；測試庫PMR_SPC_TEST兩筆migration與測試站API0.1.62／Web0.1.65完成。全套6既有失敗／6略過及正式未更新限制見[驗證](verification.md)。
