# TASK-003 Antigravity 操作案例
- 站台：https://spctest.pmr.com.tw/；版本API0.1.64／Web0.1.68。
- 範圍：公式排程與試算；不代表Portal／TransFiles已改用日期選版，也不代表管制圖已顯示實際匯入版號。
- 以下為人工驗收案例，尚未代替使用者操作。僅用測試專用管制項目，不使用正式資料、不點匯入。

## 測試準備
使用Editor登入，進「管制項目設定」→測試藥液項目→編輯→「藥液公式」。
在原公式維護依序儲存兩版：A為`Primary*2`、B為`Primary*3`，啟用公式、精度2，參照文件填`TEST-DOC-001`。記下實際歷史版號；重新開啟編輯後在「公式生效排程」選這兩版。不要把版本修改日期當生效日期。

| 案例 | 操作 | 預期 |
|---|---|---|
| AG-01 發布 | A期間2026-10-01～2026-10-31；B自2026-11-01、迄日留空；填發布依據，按「發布公式排程」 | 成功、有修訂編號；重開後日期／版號一致，提示尚未切換既有量測寫入 |
| AG-02 切日 | Primary填1.2345，量測依序2026-10-31T23:59:59+08:00及2026-11-01T00:00:00+08:00；各按「依量測日期試算」 | A原始2.469／儲存2.47；B原始3.7035／儲存3.70（畫面可顯示3.7） |
| AG-03 UTC等價 | 改量測2026-10-31T16:00:00Z | 與AG-02第二筆相同版號及結果；改輸入後舊結果立即清除 |
| AG-04 錯誤日期／輸入 | 分別用無時區2026-11-01T00:00:00、無效2/30、Primary空白或NaN | 顯示可讀錯誤，不產生量測、不以現在日期或0替代 |
| AG-05 重疊／空缺 | 先把B起日設10/31發布；再改11/02發布並試算11/01 | 重疊拒絕且保留草稿；空缺可發布、有空缺日期提示；11/01試算明確無適用版本 |
| AG-06 兩分頁競爭 | 兩分頁載入同修訂；第一頁發布，第二頁再發布 | 第二頁顯示其他人已發布，須重新載入，不覆蓋第一頁 |
| AG-07 F快照 | 另用含`Primary*F!B3`的公式版，先在F維護建立含B3=2的歷史；排程指定該快照 | Primary1.234得2.468；後來改目前F表不影響舊快照；不指定F快照發布時拒絕 |
| AG-08 Viewer | 改用Viewer登入，同樣開啟此設定 | 無發布按鈕；即使直接呼叫發布／prepare API也403；唯讀試算允許 |

## API自動化補充
所有端點為`/api/part-process-characteristics/{ppcId}/chemical-formula-schedule`，須沿用已授權登入，不保存密碼或JWT。
- GET：目前revisionId及periods／gaps（UTC時間帶Z）。
- POST：`{baseRevisionId,reason,periods:[{formulaVersionId,fTableHistoryId,fromDate,toDate}]}`。
- POST `/preview`：`{measuredAt:"2026-11-01T00:00:00+08:00",primary:"1.2345",secondary:null,sourceKey:"AG-unique-source"}`。
- POST `/prepare`：`{input:原preview輸入,token:preview回傳token}`。只建立10分鐘計算憑證，不寫量測。
- 使用preview token後改primary／日期／sourceKey／操作者，或等待10分鐘、重新發布排程，再prepare，預期409且要求重預覽。偽造token也409。
- 正常prepare後不可把該量測時點改配其他公式或F快照；可為尚未使用的未來日期發布新版。

復原：未建立receipt的測試排程可發布原先記錄的完整排程；發行過receipt者保留該時點公式／F，勿刪表或執行Down。正式站仍未發布。
