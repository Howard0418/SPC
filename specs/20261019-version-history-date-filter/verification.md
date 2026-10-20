# 三類歷史日期篩選驗證
- 功能ID：SPC-VERSION-HISTORY-DATE-FILTER-20261019；規格v2；2026-10-20。
- 狀態：實作／必要驗證／SPC測試站發布完成；正式未操作。

## 授權與實作
使用者2026-10-20同意新增規格版本表與測試庫migration。規格基本值與分段歷史以統一EF邊界捕捉；immutable歷史、真實actor、UTC修改時間、同PPC版號、過期快照拒絕，交易失敗不留半筆。規格來源刪除不級聯抹除歷史。日期篩選不改版本生效期間或計算規則。
補正既有20261007090000_AddChemicalFTableVersionHistories缺EF識別標記問題；原測試庫及還原隔離庫都缺該表，已以原migration補齊，沒有改F值或虛構回填。

## AC與證據
| AC | 方法 | 結果 |
|---|---|---|
| AC-001 | 三類共用起迄日／查詢／清除／分頁；設定頁與總覽相同PPC API | Playwright通過；線上三類GET200 |
| AC-002 | 台北日期轉UTC含起日、迄日次日排除；跨年與迄日最後tick；回應UTC帶Z、前端台北顯示 | 日期／查詢自動化通過；SQL Server實際查詢可執行 |
| AC-003 | 三API反向／無效日期拒絕，單邊／空值保留；前端錯誤提示 | 單元及瀏覽器通過；線上三API無效日期均400 |
| AC-004 | 超過20筆的較早區間，先WHERE再排序／Skip／Take；清除重置頁碼 | 三來源查詢測試及瀏覽器分頁通過，未只篩最近20筆 |
| AC-005 | 查詢唯讀／PPC隔離／錯誤重試；保留既有公式／F表回復與權限 | 相關回歸通過；線上只讀與無效查詢不建版，未真人寫入業務值 |
| AC-006 | 基本規格、分段新增／修改／刪除、同步及async、多筆EF路徑、immutability、actor、過期快照與交易失敗 | InMemory／SQLite測試通過；真實SQL隔離庫驗證回復、同PPC並行版號及過期拒絕通過 |
| AC-007 | EF migration新表／索引、Up兩次、Down後Up、保留既有值、可驗證備份 | 兩類隔離演練通過；測試庫備份VERIFYONLY與前後規格／F資料SHA256相同；兩歷史表0筆，無假造舊版 |

## 測試命令與實際結果
- 相關後端：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-build --no-restore --filter "FullyQualifiedName~VersionHistory|FullyQualifiedName~SpecificationLimitVersion|FullyQualifiedName~ChemicalAnalysisFormulaVersion|FullyQualifiedName~ChemicalFTableServiceTests|FullyQualifiedName~ApiErrorResponseFactory"`：37 passed。
- 瀏覽器：`node frontend/mes-spc-web/node_modules/@playwright/test/cli.js test --config frontend/mes-spc-web/playwright.chemical-history.config.ts`：9 passed；127.0.0.1及mock API封鎖外部請求，含390px、前案6例及日期查詢／反向／清除／分頁／三類入口，不修改真實資料。
- 完整後端：132例，120 passed、6 failed、6 skipped。以HEAD aa994a9a封存的原碼隔離還原／建置完整基準，115例，103 passed、相同6 failed、6 skipped。失敗名稱集合完全一致，新增17例全部通過，無新增失敗。
- 6既有失敗：PortalSso_WhenNonceIsReplayed_ReturnsUnauthorized、Summary_Should_Normalize_Chem_Dimension_And_Exclude_Excluded_Batches、Create_Should_Use_Configured_Group_FieldRequirements、Update_Should_Save_Unit_And_ItemSpecificRules、Calculation_Should_Prefer_ItemRuleGroup_Over_ChartTypeRuleGroup、DeleteItem_RemovesPortalDailyMeasurementAndDerivedRecords。分別為既有SSO回應、摘要篩選、舊斷言／主檔驗證／規則及InMemory交易問題，本案不順手修改。
- dotnet Release publish與npm run build:test成功。既有Pkcs9.0.4 NU1902／NU1903、工具10.0.5與runtime10.0.9提示、chunk大小警告保留；受限網路還原簽章曾失敗，以已授權官方NuGet還原完成基準，不宣稱依賴安全檢查全通過。

## SQL演練、發布與備份
- EF產生20261019235606_AddSpecificationLimitVersions（時間戳UTC，實作日台北2026-10-20），diff僅規格歷史表與2索引；既有F歷史migration只補DbContext／Migration識別屬性。
- COPY_ONLY/CHECKSUM＋VERIFYONLY還原為SPC_HISTORY_VERIFY_20261020_b43c80，規格Up重跑／Down／Up與F歷史Up重跑／Down／Up均成功，原規格及F值不變。隔離SQL測試演練故意失敗交易、同PPC並行版號與過期拒絕；fixture僅在隔離庫，未寫原測試或正式。
- 核對SQL伺服器與PMR_SPC_TEST，再取得新備份；使用既有維運連線執行備份／migration，不提高應用帳號權限。原應用帳號新表SELECT／INSERT驗證通過。
- 實際IIS：SpcSingleTest／pool同名／D:/SPC/release/test/backend，同級frontend；沒有改其他站台、bindings或D:/Sites/PmrPortal。
- 檔案備份：D:/SPC/release/backup-version-history-20261020-080815（ACL Administrators／SYSTEM）；IIS備份SPC-VersionHistory-Test-20261020-080815。SQL .bak在伺服器預設備份路徑，COPY_ONLY／CHECKSUM＋VERIFYONLY成功，受限本機apply.json保留確切路徑。
- 兩筆migration在同一SQL交易套用；原規格及F資料hash前後一致，無資料回填。保留appsettings*.json／web.config；保護設定hash不變，payload逐檔匹配。
- 最終 /api/version=0.1.62、environment=test、/api/health=ok，首頁200；三類區間GET200，三類無效日期400。前端0.1.65，HTTP資產index-Cmg1eq0d.js SHA256與dist相同，pool Started，apply.status=completed。
- 未提交連線、SQL備份、發布產物、私有helper、baseline副本、TRX／JSON等證據；只提交必要原碼／測試／規格。

## 回復與限制
失敗回復同批backend／frontend及原設定，卸載測試應用後再啟動並查健康；新增歷史表保留，不自動Drop或整庫還原。Down只在隔離庫演練；真實回復DB需另核對新資料。隔離庫與備份保留，不自動清除。
既有資料啟用前的逐次異動不可還原，本案不猜測日期；EF以外外部SQL更新不在追蹤範圍。正式站真人操作與正式DB／發布未執行，KM持續暫緩。此案完成後SPC本批待辦0件，KM暫緩3件，合計未完成3件。
