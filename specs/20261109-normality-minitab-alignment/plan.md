# 實作計畫：常態分析與 Minitab 對齊

本計畫只定義後續順序，本次不執行。

1. 固定基準：取得同一批原始值、排除狀態、規格、Minitab版本及分箱設定，建立不可變golden fixture與預期edges／counts／N／Mean／StDev。
2. 統計契約：決定Minitab相容分箱模式、邊界包含規則、樣本標準差與曲線縮放；先建立失敗測試。
3. API最小修改：由單一服務產生histogram payload，避免前後端重複推算；不改量測、規格或既有AD判定。
4. 前端共用呈現：抽出兩頁共用的連續X軸直方圖option builder，柱、曲線與界線同軸；保留淺藍柱色與三語。
5. 驗證：自動化golden fixture、邊界／無效／回歸測試、兩頁截圖及Minitab逐格對照。
6. 發布：測試通過後依規則發布測試站並驗證Web／API／MIME／實際API基底；正式站另行授權。

回復方式：回復本件API／前端提交及測試站對應備份；本案不含DB migration或資料回復。
