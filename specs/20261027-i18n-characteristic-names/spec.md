# SPC品質特性名稱隨語言切換
- 功能ID：I18N-CHAR-001；版本1；日期2026-10-27。
- 狀態：原規劃入池後，2026-10-27依後續「繼續下一項」、「SPC繼」及「繼續」授權完成A／B／C，必要驗證與測試發布完成。原要求只規劃的歷史保留，不視為全案完成。
- 主系統SPC；Portal／KM／TransFiles無修改。既有三語化批次完成紀錄保留；本案為新增的主檔名稱顯示需求。
- 基準：[語系契約](../../docs/i18n/contract.md)、[三語化主任務](../20261022-i18n-task01/tasks.md)、[報表前案](../20261026-i18n-task08e/spec.md)。新需求覆蓋「原名稱固定顯示」在品質特性呈現層的限制，原名稱資料及編輯值仍須保持。

## 截圖與現況
使用者截圖為英文介面的Edit quality characteristic：中文名稱「比重」、英文名稱「Proportion」，英文欄位被框選；Value field label仍為「量測值」。沒有附本機圖片檔，僅以本次對話圖片作驗收參考，不虛構檔案連結。
QualityCharacteristic已有CharacteristicName／nullable CharacteristicNameEn，新增／編輯API已清理英文可選值。CharacteristicsView、PartProcessCharacteristicsView、MeasurementEntryView有兩個名稱，但其他圖表／摘要／追溯DTO仍只有中文；不代表實際所有主檔英文都完整，實作時先做唯讀缺值盤點。

## 需求
- R1 英文en-US介面：有非空白英文名時顯示英文名，否則中文名；zh-TW／zh-CN介面：優先原中文名，否則英文名；兩者皆無時顯示穩定特性代碼，代碼亦無時用本地化「未命名特性」。不得因此隱藏項目或用ID冒充名稱。只在判定空白時trim，不自動改寫儲存資料。
- R2 中文名稱是共用原始主檔值，zh-CN不自動簡繁轉換；英文名不由AI／機器翻譯補寫，不另建語系JSON逐筆主檔。名稱含單位、括號、縮寫、重音、HTML／插值字樣皆按文字安全呈現。
- R3 切語言立即更新主檔清單、管制項目清單與選單、量測輸入、SPC／趨勢圖標題與tooltip／摘要、警報／追溯等可觀察品質特性名稱；實作前盤點每個使用點，依最小範圍分三件，不能只修截圖一欄而宣稱全站完成。保留選取ID、草稿、篩選、排序基準、圖表縮放／series、API請求與資料關聯；搜尋仍比對代碼、中英文，不因當前語言失去可搜尋性。
- R4 截圖編輯視窗仍同時顯示中／英文兩欄及各自原值；切語言只換周邊UI與讀取顯示，不把Proportion填回中文欄、不改變儲存DTO／必填／權限。ValueLabel「量測值」是獨立欄位，不從特性英文名推導或翻譯，若要雙語值標籤另立需求。
- R5 缺英文的API投影以additive nullable欄位補足，原CharacteristicName／ChartName／錯誤／診斷等原欄位保持；不得把使用者命名的獨立圖表名稱當成品質特性名替換。既有entity欄位足夠，預期不需schema migration／資料批次更新。
- R6 人讀報表、手動報表郵件中出現的特性名沿用同一選用規則；背景預設繁中。CSV／匯入範本／識別欄位／原始歷史快照／稽核資料不重寫，CPK計算與原月份範圍問題仍分案。

## 格式建議
使用既有兩欄即可：CharacteristicName=比重，CharacteristicNameEn=Proportion。英文欄勿放「比重 / Proportion」這類中英合字或JSON；單位沿用獨立unit，不為顯示重複加單位。既有名稱有括號或符號不據此拒絕，缺值只提示補齊、不偷偷翻譯。Proportion是否為企業採用的「比重」正式術語須由業務詞彙表／真人審稿確認，本案保留現有值，不自行改成其他英文。

## BDD驗收
- AC1 Happy：Given比重／Proportion，When TW→US→CN，Then讀取名稱為比重→Proportion→比重，原表單兩欄值及選取ID不變。
- AC2 Boundary：Given英文null／空白、中文空值、兩者皆缺及代碼缺，When切換，Then依R1逐層回退，0值／數值／單位／統計保持。
- AC3 Invalid：Given名稱含<b>、@:、{name}、Excel公式前綴，When頁面／圖表／報表呈現，Then按文字處理，不執行HTML、i18n模板或Excel公式，不修改來源。
- AC4 Regression：Given已選特性、未存草稿與圖表zoom，When切語言，Then無額外寫入／寄信／重新查圖、不重置狀態；中英文搜尋結果相容，編輯儲存DTO與原文相同。
- AC5 API／報表：Given原與新client／三語locale，When取得摘要或下載人讀報表，Then舊欄位／數值／原檔名及機器格式保持，新增英文欄位僅供呈現；缺欄位舊資料也可回退。
- AC6 Responsive：390／1280／1920下長英文與中文皆可讀，表單與按鈕可操作；依截圖核對雙欄，不覆蓋原輸入。

[計畫](plan.md)／[任務與排序](tasks.md)／[驗證狀態](verification.md)。

- A已完成[單件範圍](task-a.md)及[驗證](verification.md)，測試Web0.1.108/API0.1.85；圖表／摘要／警報／追溯仍待B，報表／郵件仍待C。

- B已完成[單件範圍](task-b.md)及[驗證](verification.md)，測試Web0.1.109/API0.1.86；其餘頁面盤點與人讀報表／郵件仍待C，原A的發布紀錄保留歷史。

- C完成[單件範圍及排除盤點](task-c.md)與[驗證](verification.md)，最新測試Web0.1.110/API0.1.87，正式未更新；前述各批版本與未完成敘述為當時紀錄。現有主檔名稱依語系呈現，原始歷史文字不改，真人審稿及跨站總驗收仍獨立待辦。
