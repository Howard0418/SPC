# TASK-004 Antigravity 測試案例
- Portal：https://portaltest.pmr.com.tw/Spc/VariableMeasurementEntry
- SPC：https://spctest.pmr.com.tw/part-process-characteristics
- 僅使用測試專用管制項目與既有合法帳號；不操作正式站、不改角色、不產生測試JWT。
- 本件測日期預覽與初驗／複驗留存；TransFiles DB直寫與圖表實際版本整合仍為後續工作。

## 準備
在SPC測試專用藥液項目建立兩筆公式歷史：A=`Primary*2`、B=`Primary*3`，啟用、精度2，參照依據填`TEST-DOC-004`；規格／界限涵蓋案例值。公式生效排程A設2026-10-01～10-18，B自2026-10-19、迄日空白，發布。記下實際公式版號及排程修訂；Portal重新整理主檔後才開始。日期不可晚於今天，請用日曆選擇，不直接填唯讀日期欄。

| 代號 | 操作 | 預期結果 |
|---|---|---|
| AG4-01 日期選版 | Portal選該線別／槽位，日期10/18，初驗原始滴定值1.2345；再改10/19重新預覽 | 10/18顯示A、分析值2.47；10/19顯示B、分析值3.70；日期變更舊結果清除，顯示實際版號／修訂 |
| AG4-02 初驗＋複驗 | 日期10/19，初驗1.2345、複驗原始值2.3456，等待兩者預覽後確認送出 | 同筆日報初驗3.70、複驗7.04；SPC保存兩階段receipt／trace及未先取位的原始字串；不接受單填複驗濃度冒充原始值 |
| AG4-03 日報查回／修正 | 載入10/19當日日報 | 查回原始滴定值與濃度，載入本身不重新計算；如需修正，修改原始值，按該列「重新預覽原始值」再送出；沿用同量測ID並新增追溯版本，不覆寫舊trace |
| AG4-04 過期／改排程 | 先預覽；在SPC另一分頁用相同日期內容重新發布一份修訂，再回Portal確認 | 409提示重新預覽，保留草稿，無半筆量測／receipt；重新預覽後可確認；超過10分鐘也須重新預覽 |
| AG4-05 欄位格式 | 清空必填原始值，或含Secondary的版本漏第二讀值；再嘗試送出 | 顯示缺欄位／公式輸入提示，不以0或系統日期替代；整批不得部分成功。只讀日期一律由日曆選擇 |
| AG4-06 未排程相容 | 另一個未設定排程的藥液項目，以原有直接值／舊公式輸入 | 保留原規則與三位儲存相容；沒有server公式版號或假造trace；同批可與已排程項目一起原子確認 |
| AG4-07 角色與重送 | 使用既有SPC Viewer對應的Portal品保帳號嘗試確認；Editor正常確認重試相同requestId | Viewer由SPC拒絕403，缺個人token回401；相同ID／同內容／同actor回原成功結果不重複，不同內容或actor回409 |
| AG4-08 規格精度邊界 | 專用項目採`Primary*2`、精度2、USL1.002，輸入0.502 | 原始1.004／儲存1.00，預覽及規格判定NG；統計值仍1.00，receipt／trace可重現原始結果。此例可能產生正常警報，應在已隔離通知的測試項目驗證 |

## 自動化與資料庫證據
- API：`POST /api/spc/chemical-effective/preview`傳PpcId、量測ISO8601（含offset）、Primary／Secondary十進位字串、SamplingPhase及SamplingStage；不傳自訂算式。
- 確認：`POST /api/spc/chemical-effective/confirm`，body為`{requestId,rows}`；初驗`ChemicalPreviewToken`、複驗`ChemicalRecheckPreviewToken`由本次預覽取得，所有row欄位仍為字串。
- 量測／初驗及複驗trace／receipt在同一SQL交易。已在全新隔離SQL庫驗證晚期複驗trace故障全部rollback、啟用gate可用、兩人重送只新增一次；不要在共用測試庫建立故障trigger。
- 不將token保存到UploadDetails；保留原始值、結果、版本及引擎識別。實際逐點／分段版本畫面仍待TASK-006。
- 既有日期公式日報請以載入→原始值預覽→確認修正；舊直接數值寫入端尚未支援trace，不應用來覆蓋這些量測。TransFiles切換留TASK-005。

上述為交付案例，完整真人／Antigravity跨站操作尚待執行，不將本機mock測試冒稱真實使用者驗收。
