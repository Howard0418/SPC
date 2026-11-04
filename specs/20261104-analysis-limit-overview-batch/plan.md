# 技術計畫

- 功能 ID：SPC-LIMITS-BULK-1104
- 規格版本：1
- 狀態：已規劃／未執行
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- SPC frontend：新增`AnalysisLimitOverviewView.vue`、路由、導覽、i18n及Playwright測試；沿用`ChemicalAnalysisOverviewView.vue`的篩選、dirty rows、跨頁草稿、確認摘要、逐列還原及匯出互動。
- SPC backend：新增界限專用query／preview／apply DTO與服務，不直接用通用PPC PUT更新完整物件；只允許六個界限欄位。
- SPC資料層：規格欄位沿用`SpecificationLimitVersions`與既有原子寫入；核對控制界限前後值的稽核／回復證據。未確認前不新增migration。
- Portal／TransFiles：無程式修改；驗證既有量測輸入及匯入契約未被改動。

## 實作方式與需求對應

1. 先以唯讀測試建立各ControlScope／ChartType的欄位適用矩陣，鎖定自動與手動空值語意。（R-001、R-004、R-005）
2. 建立專用總覽query，回傳PPC ID、定位欄位、六界限、來源／分段提示、`UpdatedAt`或等效併發標記。（R-001、R-003）
3. 建立preview：正規化數字但不寫入，逐列回報欄位錯誤、過期與變更前後值；產生與內容綁定的短效preview token／hash。（R-003、R-004）
4. 建立apply：同交易重核token、目前值、權限與格式，只更新異動六欄；任何一筆失敗整批rollback。（R-002～R-006）
5. 新頁沿用公式總覽的表格體驗，但增加「規格／管制」欄群、自動／手動來源、分段提示、預檢步驟及全批結果。（R-001、R-002、R-005）
6. 規格變更沿用版本服務；控制界限以已核准稽核契約保存白名單前後值，若現行能力不足先更新規格再實作資料模型。（R-006）

## API與資料契約候選

- `GET /api/v1/part-process-characteristics/limit-overview`
- `POST /api/v1/part-process-characteristics/limit-overview/preview`
  - request：reason、rows[{ppcId, expectedUpdatedAt／expectedHash, lsl, targetValue, usl, lcl, cl, ucl}]。
  - response：previewToken、expiresAt、valid、rowResults[{ppcId, before, after, errors, warnings}]。
- `POST /api/v1/part-process-characteristics/limit-overview/apply`
  - request：previewToken。
  - response：batchId、updatedCount、unchangedCount、version／audit refs。

正式命名以現行API規則調整；客戶端before值不得作權威，伺服器須在apply交易中重讀並比對。

## 相容性、資料庫及風險

- 主系統SPC；資料庫影響暫定無schema，只有實際套用時更新PPC六欄及既有版本／稽核資料。
- 最大風險是整筆PUT覆蓋其他主檔、空白自動界限被回寫0、規格與管制界限混用、分段值被誤改及批次部分成功；以專用契約、預檢、併發比對與交易rollback處理。
- 快取／統計：實作前確認界限變更後哪些計算結果應失效或即時重算，不能沿用公式變更的清除策略猜測；既有歷史量測及保存判定不得重算。
- 權限沿用Editor及後端授權；前端隱藏不能取代API拒絕。每批保存操作者、成功／失敗結果及敏感資料白名單。

## 驗證安排

- Backend單元／整合：合法單邊規格、零／負數、Target邊界、自動空值、非法順序、NaN／Infinity、重複PPC、過期版本、跨項目、權限、交易rollback、重送。（AC-003～AC-007）
- Frontend／Playwright：篩選、5／10／20／50分頁、跨頁dirty、還原、預檢、確認取消、成功、錯誤、過期、深色／窄版及匯出。（AC-001、AC-002、AC-004、AC-008）
- Regression：公式JSON／F、規格版本歷史、分段、單筆PPC編輯、化學保存、量測輸入、圖表與既有API。（AC-006、AC-007）
- 測試站：build/test通過後依規則直接發布；驗證首頁及主JS／CSS MIME、API health/version／環境標籤、瀏覽器實際API基底，再由登入畫面完成截圖。（AC-008）

## 發布、備份及回復

- 本次只規劃，無建置、資料備份、migration或發布。
- 未來實作若無schema：測試DB先備份PPC、SpecificationLimitVersions及相關AuditEvents；以批次ID／版本快照還原異動六欄。
- 若需schema：先在隔離測試庫演練Up／Down、備份及相容性；不可與其他schema／權限高風險工作併行。
- 測試站通過後依專案規則發布；正式站、正式DB與正式資料變更另行授權並核對實際目標。禁止發布到`D:\Sites\PmrPortal`。
