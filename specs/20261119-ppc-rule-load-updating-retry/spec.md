# SPC-PPC-RULES-UPDATING-1119-TASK-001

## 需求
編輯 SPC 管制項目時，若管制規則 API 暫時回傳 IIS `Updating` HTML，前端須自動重試，不可將 HTML 顯示成公式切換錯誤。

## BDD
- Given 第一次規則請求回傳 503 與 Updating HTML，When 使用者開啟管制項目，Then 前端短暫等待後重試並正常載入規則。
- Given 暫時更新持續存在，When 重試仍失敗，Then 顯示可讀的服務更新訊息，不顯示 HTML 原文。
- 公式選擇、公式 JSON 與儲存內容不受影響。
