# SEC-FOLLOWUP-006 SPC正式DDL授權範圍
2026-10-21；只審查，尚未執行、尚未取得本範圍授權。

| 遷移 | 新增範圍 |
|---|---|
| 20261007090000_AddChemicalFTableVersionHistories | F表歷史1表、4索引、自關聯及F主檔外鍵 |
| 20261019235606_AddSpecificationLimitVersions | 規格歷史1表、2索引 |
| 20261020040256_AddChemicalFormulaTraceSchema | 日期/trace/job等6表、15索引、量測2可空欄、公式複合唯一約束、外鍵/check、9 trigger、1直寫角色及9表DML DENY |
| 20261020052530_AddChemicalDirectWriterProtocol | CommitChemicalMeasurementWithTrace、GetChemicalDirectImportStatus兩程序與角色EXECUTE權限 |

## 資料與權限影響
- 總計8表、21 CreateIndex、9 trigger、2程序；另有PK/unique/FK/check，不把約束漏稱為沒有索引影響。
- VariableMeasurements新增ChemicalReceiptId uniqueidentifier NULL、ChemicalSourceKeyHash nvarchar(64) NULL；來源去重索引僅限SourceKeyHash非NULL。既有資料不由此填入receipt，不假造歷史版本。
- 已讀所有4筆Up與ChemicalTraceSqlV1.Up/ChemicalDirectWriterSqlV1.Up：沒有執行於遷移時的業務資料回填、清除或重算。程序內INSERT為日後被授權呼叫才執行，不是遷移即匯入。
- 日期policy表初始為空；未自動開啟任一既有PPC日期公式。9 trigger保護排程、引用版本、證據與量測完整性，發布後既有更新流程仍需複本回歸，不宣稱完全無行為影響。
- 建立SpcChemicalDirectWriter角色及2程序EXECUTE，DENY其直接INSERT/UPDATE/DELETE九表。不建立登入、不新增AD帳號、不加入PMR\ihao_ting或任何正式角色成員；個人正式直寫權限不在本次自動授權範圍。
- 新欄/索引/外鍵/trigger會取得schema lock，不能把無回填誤稱零停機或零風險。正式庫容量及DDL耗時尚未演練。

## 正式唯讀前置結果
- 既有7張前置表ChemicalFTableVersions、ChemicalAnalysisFormulaVersions、ControlLimitSegments、PartProcessCharacteristics、UploadBatches、UploadDetails、VariableMeasurements均存在。
- 本次必要新增8表與2程序前次查無；本次補查兩量測欄、TR_Chemical% trigger及公式複合唯一約束亦未列出。查詢只讀sys目錄，不輸出業務值或連線機密。
- 實際DB為PMR_SPC_2026，正式API0.1.60；不得只從晚於10/08開始生成遷移，否則可能漏掉缺失的10/07 F歷史。應依全量migration集合差集並含條件歷史判斷；禁止手工冒填EF歷史。

## 執行前與回復
1. 使用者明確同意本4筆DDL/角色範圍，先核對最新COPY_ONLY/CHECKSUM備份與VERIFYONLY。
2. 以正式備份還原獨立驗證庫，驗證4筆Up/冪等或EF歷史、既有資料/公式/規格/角色保持、新舊匯入與圖表相容；不將測試庫正常升級代替正式複本演練。
3. 004與正式維護窗/發布順序核對完成才正式執行；Portal相依契約與正式金鑰持久化仍是未完成前置，不能以本DDL審查通過代替全006驗收。
4. 逐步保存前後指紋及同批應用備份；應用回復保留新增schema與原設定/資料，不自動Down/drop/整庫還原。正式直寫帳號仍不啟用。

本文件為可審查範圍，不是可直接執行的SQL包。源碼依據為backend/MesSpc.Api/Migrations/上表四個cs及ChemicalTraceSqlV1.cs、ChemicalDirectWriterSqlV1.cs。原始程式與資料庫未修改，build/發布不適用；複本與正式DDL尚未執行。
