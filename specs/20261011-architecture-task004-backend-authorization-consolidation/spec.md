# ARCH-TASK-004-BACKEND-AUTHORIZATION-CONSOLIDATION-20261011

## 目的

收斂 SPC 後端授權模型的重複判斷，先將校正管理相關的角色、頁面權限與操作者資料庫回查集中到共用服務，降低後續新增端點時 Viewer/Editor 與頁面權限判斷不一致的風險。

## 範圍

- 主系統：SPC backend。
- 相依系統：無。
- 資料庫影響：不新增 migration、不變更 schema。
- 發布影響：後端變更，測試通過後發布 SPC 測試站 backend；正式站不發布。
- Rollback：還原本次後端程式與測試站 backend 備份。

## 現況

校正儀器管理與匯入 controller 目前各自讀取 `ClaimTypes.Role`、`role`、`permissions`，再視情況回查 `Operators`。部分邏輯預設值不同，且權限字串與 `CalibrationRules.CanManage` 分散，未來新增端點容易漏掉一致檢查。

## 需求

- R-001：建立共用後端授權存取服務，統一解析目前登入者的角色與頁面權限。
- R-002：校正管理權限判斷需集中呼叫共用服務，不再於 controller 重複組角色/權限邏輯。
- R-003：未登入、非 Editor、停用操作者、缺少 `calibration.manage` 權限時，應一致判定為不可管理。
- R-004：本次不改登入流程、不改 JWT claims 格式、不改資料庫 schema。

## BDD 驗收

```gherkin
Feature: Backend authorization consolidation

  Scenario: Editor with calibration permission can manage calibration data
    Given an authenticated Editor operator with calibration.manage permission
    When backend checks calibration management access
    Then access is allowed

  Scenario: Viewer cannot manage calibration data
    Given an authenticated Viewer operator
    When backend checks calibration management access
    Then access is denied

  Scenario: Disabled operator cannot manage calibration data
    Given an authenticated Editor operator that is inactive
    When backend checks calibration management access
    Then access is denied
```

## 驗證規劃

- Happy Path：Editor 且具 `calibration.manage` 可管理。
- Boundary Case：Claims 未帶 permissions 時可回查 `Operators.PagePermissionsJson`。
- Invalid Input：Viewer、停用操作者、無登入者皆不可管理。
- Regression Risk：既有 Viewer 寫入防護與角色正規化測試需維持通過；後端 build 通過。

## 狀態

已完成。授權測試與後端 build 通過；已發布 SPC 測試站 backend，正式站未發布。
