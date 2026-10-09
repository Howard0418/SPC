# BDD Feature 範本

功能 ID：<TASK-ID>  
來源規格：<spec.md path>  
版本：1

## Feature
```gherkin
Feature: <使用者可理解的功能名稱>
  為了 <業務目的>
  作為 <角色>
  我需要 <能力或結果>

  Background:
    Given <共同前提，例如使用者已登入或資料已存在>

  Scenario: <Happy Path 名稱>
    Given <前提>
    When <動作>
    Then <可觀察結果>

  Scenario: <Boundary Case 名稱>
    Given <邊界前提>
    When <動作>
    Then <可觀察結果>

  Scenario: <Invalid Input 名稱>
    Given <不合法前提或輸入>
    When <動作>
    Then <拒絕、錯誤訊息或資料不變>
```

## 驗證對應
| Scenario | 自動化測試或人工驗證 | 狀態 |
|---|---|---|
| <Happy Path> | <測試名稱、命令或人工步驟> | 待驗證 |
| <Boundary Case> | <測試名稱、命令或人工步驟> | 待驗證 |
| <Invalid Input> | <測試名稱、命令或人工步驟> | 待驗證 |

## 注意事項
- 不使用真實姓名、密碼、連線字串、正式資料或機敏資料。
- 涉及刪除時，先改寫待刪清單 MD，取得授權前不執行刪除。
- 若 Scenario 無法對應驗證方式，需回到規格補清楚可觀察結果。
