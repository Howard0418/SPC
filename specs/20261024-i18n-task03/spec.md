# I18N-TASK-03：Portal本人語言偏好與資料模型提案
- 日期2026-10-24；版本1；狀態：DONE（僅設計；API/schema未實作）。
- 使用者指示開始下一項，依[工作池](../20261022-i18n-task01/tasks.md)只執行TASK-03；不開始TASK-04/09，不授權schema/角色/SSO/站台變更。
- 主系統Portal，主規格置SPC（跨站共用偏好）；相依SPC/KM僅來源/信任邊界分析，不修改三站repo執行碼或實庫。
- 基準：[字典/locale契約](../../docs/i18n/contract.md)、[前案](../20261022-i18n-task01/analysis.md)。
## 需求/BDD
- R-001/AC-001 Given Portal cookie/Users.Id、SPC JWT/Operators.Id、KM既有簽名帳號，When 設計偏好，Then Portal本人僅由已驗證NameIdentifier→啟用Users決定，不接受前端userId/工號/顯示姓名；不能跨系統直接等同整數ID。
- R-002/AC-002 Given 有效Portal個人帳號，When 規劃偏好讀寫，Then 只允zh-TW/zh-CN/en-US，無資料預設繁中；非法輸入不保存，語系不改資料/角色/時區或格式解析。
- R-003/AC-003 Given 新增偏好資料模型，Then 列清欄位/PK/FK/唯一/locale約束/版本並發、migration前置與回復；本次只提案不執行或宣稱實庫無此表。
- R-004/AC-004 Given 更新失敗/兩裝置競爭/換人登入，Then 不丟表單、不自動切回舊選擇、不把前人快取同步給後人；需有待同步/條件式更新策略。
- R-005/AC-005 Given SPC/KM與Portal不同origin/驗證，Then 不假設跨站cookie/localStorage或SPC token可直接用Portal；完整跨站同步是後續已驗證委派方案，不暗中更改SSO。
- R-006/AC-006 Given 完成TASK-03，Then 文件/契約範例/驗證設計及工作池同步，未建立端點/Migration/偏好功能，停止不進入下一Task。
## 範圍與未決
- 來源檢視、Portal本人API提案、獨立UserLocalePreferences表提案、快取隔離/跨站階段與驗證設計。
- 共用local_guard是否帳號級共用偏好及安全跨站委派仍須後續具體決策；不阻擋本設計交付，但不得未決就啟用相關寫入/聲稱三站同步。
- 不查實庫/讀連線字串/安裝套件/改程式/DDL/正式資料/AD/nonce/角色/發布。
- [計畫](plan.md)｜[任務](tasks.md)｜[驗證](verification.md)。

- 交付：[本人偏好提案](../../docs/i18n/preferences-plan.md)、[驗證設計](../../docs/i18n/preferences-validation.md)。
