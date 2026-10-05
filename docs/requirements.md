# SPC 現行需求索引

- 小型修正（測試 API 已發布）：[PPC 重複鍵儲存防護](../specs/20260929-ppc-duplicate-key-guard/spec.md)；SPC 管制項目新增/編輯若同鍵其他列已存在，後端先回 409 可讀訊息，不再外洩 SQL 唯一索引錯誤。
- 測試與正式 SPC 前端已發布：[藥液 GENERAL 視同早班](../specs/20260922-chemical-general-as-open/verification.md)；班別 GENERAL／空白當早班，圖上不丟歷史點；開／收線仍只限 N1／N2。資料未改；正式 Portal 未發布。
- 正式已轉換：[N1／N2 舊 CLOSE 轉收線](../specs/20260922-chemical-close-stage-apply/verification.md)；`PMR_SPC_2026` 198 筆 CLOSE＋GENERAL → OPEN＋CLOSE，剩餘 0。9 月前僅開收線、9 月後才分早中班；`OPEN`＋`GENERAL` 開線列與 MIDDLE 未改。[盤點](../specs/20260922-chemical-close-stage-inventory/verification.md)
- 正式匯入完成：[2026 年 9 月咬蝕日報正式匯入](../specs/20260922-etch-production-import/verification.md)；Portal `PMR_PORTAL_UAT` 48 份／3,650 點，SPC `PMR_SPC_2026` 3,746 筆／16 mappings；逐點、16 圖、隔離與 48 份防重跑均通過。
- 盤點（未寫入）：[咬蝕正式庫唯讀盤點](../specs/20260922-etch-production-inventory/spec.md)；測試 48 份 vs 正式 0；Portal Production 連線 `PMR_PORTAL_UAT` 已由使用者確認為正式。
- SSO 防重複（測試 API 已發布、歷史資料關係待確認）：[AD／工號單一操作者](../specs/20260917-sso-single-operator/spec.md)；可信工號與 AD 沿用既有 ID/權限，legacy 不再首次另建帳號；衝突不猜測合併。

- 登入小型變更（測試站已發布）：[直接導向 Portal 登入](../specs/20260917-direct-portal-login/spec.md)；不顯示 AD 按鈕/過渡卡片，保留既有 SSO 與原頁返回。

- 設備狀態修復（測試站已發布）：[Chameleon 來源表 migration](../specs/20260917-chameleon-source-schema/spec.md)；補齊既有模型需要的表與唯一索引，空表沿用設定檔來源，設備狀態 API 恢復 200。

- 跨專案（測試站已發布）：[藥液班別與 N1/N2 取樣階段](../specs/20260917-chemical-shift-stage/spec.md)；早/中班保留，N1/N2 開/收線独立儲存；12 核心及 3 UI 案通過，真人登入驗收待執行。

- 開發流程：[一句話需求預設流程](../specs/20260916-short-request-defaults/spec.md)；簡短開發需求自動依 AGENTS.md 執行 SDD 與必要驗證。

- 小型變更（測試站已發布）：[儀器列表捲動改善](../specs/20260916-calibration-table-scroll/spec.md)；固定表頭、60dvh 表格內捲動、上方左右按鈕；手機導覽與內容上下排列。
- 小型變更（測試站已發布）：[校正管理列表隱藏保管人欄位](../specs/20260921-calibration-hide-custodian-column/spec.md)；列表不顯示保管人，主檔編輯、保管人收件通知與後端契約不變。

- 儀器欄位（測試站已發布）：[量測規格、精度、備註、校驗規範、允收標準](../specs/20260916-calibration-details/spec.md)；主檔/表單/匯入一致，原 Excel 補回測試庫 59 筆空白欄，保留日期與既有值。

- 新功能（測試站已發布，真實 NAS 待驗證）：[校正通知 Email／Synology Chat](../specs/20260915-calibration-chat/spec.md)；全域二選一、加密 Webhook、手動測試與群組防重送。

- 小型變更（測試站已發布）：[校正提醒天數簡化設定](../specs/20260915-calibration-reminder-picker/spec.md)；常用天數勾選、自訂 0～365 天與恢復預設，保留既有設定及通知規則。
- 小型變更（測試站已發布）：[儀器校正到期管理亮色配色](../specs/20260912-calibration-ui-colors/spec.md)；亮底青／琥珀卡片，不改業務規則。備份 `20260912-215320`。
- 新增功能（測試站已發布，登入端到端待驗證）：[儀器 Excel 批次匯入 v1.1](../specs/20260911-instrument-calibration-import/spec.md)；第一工作表預覽勾選、僅新增、批次補齊主檔、交易與稽核。72 後端及 3 介面測試通過；SPC 測試站備份 `20260912-145908`；實際登入/SQL Server 端到端待驗證。正式庫與真信未執行。
- 小型變更（測試站已發布）：[未校列先建檔後補日期](../specs/20260912-calibration-import-pending-dates/spec.md)；12 筆可匯入，日期留空由使用者後補。備份 `20260912-170311`。
- 小型變更（測試站已發布）：[校驗方式公式讀儲存值](../specs/20260912-calibration-method-formula/spec.md)；正式 Excel J 欄公式可匯入。備份 `20260912-164138`。
- 小型變更（測試站已發布）：[儀器列表依編號排序](../specs/20260912-calibration-list-sort/spec.md)；校正管理列表依儀器編號，不改到期摘要。備份 `20260912-162830`。
- 小型變更（測試站已發布）：[儀器主檔校驗方式](../specs/20260912-calibration-method/spec.md)；列表／表單／匯入預覽顯示 Excel J 欄，不改週期判定。備份 `20260912-161409`。
- 小型變更（測試站已發布）：[儀器主檔放置地點](../specs/20260912-calibration-location/spec.md)；列表／表單／匯入預覽顯示 Excel H 欄，不推定部門。備份 `20260912-160201`。
- 小型變更（測試站已發布）：[儀器校正測試通知按鍵](../specs/20260912-calibration-test-email/spec.md)；提醒設定可寄【測試】信，不寫入每日防重寄、不開啟自動寄信。備份 `20260912-154446`。
- 規格定案（測試站已發布，待端到端／正式上線）：[儀器校正到期通知 v4.1](../specs/20260911-instrument-calibration/spec.md)；R-001～R-016 已確認。基準 3.6 已摘要。正式庫、真信與正式發布未執行。
- 業務主基準：[SPC 現行需求基準](SPC_REQUIREMENTS_BASELINE_2026-08-07.md)。
- 現況整理：[2026-08-31 程式與需求](CURRENT_CODE_AND_REQUIREMENTS_2026-08-31.md)。
- 後續已完成變更：[CHANGELOG_CUSTOM.md](../CHANGELOG_CUSTOM.md)。
- 開發流程：[SDD v1.1](sdd-workflow.md)；[十專案導入登錄](sdd-projects.md)。
- 流程導入：[20260909-sdd-adoption](../specs/20260909-sdd-adoption/spec.md)。

基準日期早於部分變更紀錄；下一次修改相關功能前，須先對照已確認的新需求並同步該範圍。
本次未全面驗證或重寫業務基準；2026-08-31 現況整理也不代表最新程式驗收結果。

- 資料同步（已完成；41 張業務表同步，18 張校正／人員／設定表保留）：[正式業務資料同步測試庫](../specs/20260917-production-data-refresh/spec.md)。


- 歷史查詢（測試站已發布，真人登入畫面驗收通過）：[藥液歷史完整查詢](../specs/20260917-chemical-history-complete/spec.md)；按日期先篩選、分批載入全部結果，解除每製程200筆截斷。


- 日期狀態（測試站已發布，真實畫面驗收通過）：[藥液日期日曆](../specs/20260917-chemical-date-status/spec.md)；依線別／班別／階段顯示日報、歷史及無資料，選日期保留草稿、不自動載入。


- 舊班別對應（測試API已發布，實際載入驗收通過）：[未標班別視為早班](../specs/20260917-legacy-morning-shift/spec.md)；GENERAL／空白日報納入早班，修改沿用ID，階段不推定。

- 舊量測載入（測試API已發布，真人頁面載入通過）：[量測日期回退](../specs/20260917-legacy-measurement-date/spec.md)；缺日報日期改用量測日期、沿用ID更新，N1/N2舊CLOSE暫緩處理。

- 第一批盤點完成、待第二批核准：[9月咬蝕量匯入與X̄-S](../specs/20260917-etch-september-import/spec.md)；只完成Excel/既有程式/測試庫唯讀分析，未開發、匯入或發布。

- 咬蝕第二批（測試站已發布、0901四線試匯完成，人工畫面待驗收）：[規格](../specs/20260917-etch-september-import/spec.md)；原始25/50點X̄-S、整體ER與實際線速；負值隔離，全月未匯入。

- 咬蝕載入修復：[規格與驗證](../specs/20260918-etch-report-load/spec.md)；排除點位返回父日報的JSON循環，完整點位契約保留。

- 9月咬蝕補匯：[規格](../specs/20260918-etch-month-import/spec.md)；工具及預覽完成，等待第3批明確核准，尚未寫入。

- 9月咬蝕第3批已核准並完成：新增44份、共48份/3650原始點；4份負值與9/18四份未完整仍隔離。[驗證](../specs/20260918-etch-month-import/verification.md)。

- [TransFiles日報預覽確認匯入](../specs/20260918-transfiles-etch-upload/spec.md)：僅兩測試庫、完整子組、相同略過/衝突拒絕、Pending重試；API與工具已更新，登入後人工驗收待完成。

- [藥液舊開收線修正](../specs/20260918-chemical-stage-repair/spec.md)：測試460筆已分階段（開線262/收線198），其他線GENERAL與已知中班保留；正式缺欄位，待相容性範圍確認。

- [N2中班歸收線](../specs/20260918-n2-middle-close/spec.md)：2026/9/11、9/14、9/16測試庫各10筆已歸MIDDLE+CLOSE，API載入通過。

- [N1/N2藥液開收線單線圖](../specs/20260918-chemical-single-line/spec.md)：同管制項目保留全部點位、日期/階段排序、班別提示及MR對齊；API0.1.59/前端0.1.58測試站已發布。

- 子組大小編輯上限由25修正為50：[規格](../specs/20260918-subgroup-input-50/spec.md)。既有X̄-S 50點支援不變，前端0.1.59已發布測試站。

- [正式完整升級相容性](../specs/20260918-production-compatibility/spec.md)：9項隔離契約及5項發布檢核通過；額外校正/設備schema範圍待確認，完整遷移演練未完成，不可發布。
# Particle Monitoring
- [落塵／Particle 粒子監控 Long Format](../specs/20260930-particle-monitoring/spec.md)：R1-R9、0.5/1/5/10 µm Long Format，支援 preview/confirm、查詢、位置比較及 C/U-chart；測試站已發布，正式站未發布。

