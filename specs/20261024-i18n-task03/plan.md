# 計畫
1. 唯讀確認Portal Users與cookie claims、local_guard/AD流程、SPC Operators JWT及KM現有簽名帳號，記錄身分映射限制。
2. 在docs/i18n定義Portal本人GET/PUT/DELETE提案、cookie/CSRF隔離、locale/條件式更新與相容性；不新增服務帳號或擴張管理角色。
3. 比較Users新增欄與獨立偏好表，提出可回復EF migration範圍，列必要DB實庫/命名/帳號映射審查。只提案，不產SQL或Migration。
4. 整理快取/失敗/並發/跨站不支援情境，提供契約範例與未執行的自動化/人工驗證規劃；文件機械檢查後同步工作池並提交。
## 影響/回復
- 主系統Portal，SPC/KM為相依；交付只改SPC文件。未實際觸發任何DB/schema/發布，不需應用build。
- 本次回復僅後續文件修訂；未來TASK-09如實作須EF/備份/隔離Up重跑及rollback驗證，正式另授權。
