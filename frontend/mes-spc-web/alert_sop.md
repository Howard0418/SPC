# MES + SPC 異常通報與處理流程教學手冊

## 簡介
本手冊專為產線主管與品管工程師 (QE) 編寫，介紹在 MES + SPC 系統中，如何透過系統自動發送的異常通報進行追蹤，以及進行後續的狀態更新與簽核處置。

---

## 異常觸發情境
當系統在匯入檢驗數據時，若是計算出以下兩種狀況，系統將會**自動發佈異常通報 (Alerts)**：
1. **OOS (Out of Spec)**：量測數值超出了設定的上下規格界限 (USL / LSL)。
2. **OOC (Out of Control)**：管制圖運算結果違反了西方電氣規則 (Western Electric Rules) 或超出了統計管制界限 (UCL / LCL)。

---

## 步驟一：檢視異常清單

1. 在左側導覽列中，點擊 **異常管理與追溯** -> **異常通報總覽**。
2. 在異常清單頁面中，您可以即時查看所有的異常事件，包含發生時間、引發異常的料號製程以及具體的異常訊息。

<div style="text-align: center; margin: 20px 0;">
  <img src="C:/Users/ihao_ting.PMR.000/.gemini/antigravity/brain/6ab77e97-a632-4b94-9c5b-d5818191cfd1/alerts_list_general_1779080905528.png" alt="異常清單總覽" style="max-width: 100%; border: 1px solid #ccc; border-radius: 8px;">
</div>

---

## 步驟二：更新異常處置狀態（異常單簽核處置）

1. 當工程師著手處理異常時，需要更新異常的處理進度。
2. 前往左側導覽列的 **異常單簽核處置**。
3. 頁面上方提供 **狀態篩選 (Status)** 功能，方便您尋找特定狀態（例如 `Open`, `In Progress`）的異常通報。
4. 在表格中找到您負責的異常單號，您會看到「訊息」欄位詳細記載了觸發異常的具體數值（例如：`Variable measurement violates spec limit. Value=34.8485`）。
5. 於「**處理階段**」欄位選擇新的進度（例如：`InProgress` 或 `Closed`）。
6. 點擊「**儲存處置紀錄**」按鈕，系統將自動儲存並追蹤處置狀態。

<div style="text-align: center; margin: 20px 0;">
  <img src="C:/Users/ihao_ting.PMR.000/.gemini/antigravity/brain/6ab77e97-a632-4b94-9c5b-d5818191cfd1/alerts_workflow_1779080881207.png" alt="更新異常處置流程" style="max-width: 100%; border: 1px solid #ccc; border-radius: 8px;">
</div>

---

## 異常追溯建議

* **即時戰情室分析**：對於發生異常的檢驗項目，強烈建議主管與工程師前往 **SPC 即時互動管制圖戰情室**，輸入指定批次 UUID 進行歷史數據追溯，以確認是否為系統性偏移。
* **閉環管理**：現場人員可先將異常單更新為 `InProgress` 並填寫初步原因與對策；最終仍必須由負責工程師或主管確認後將狀態更新為 `Closed`，以滿足 IATF 16949 等車用供應鏈的品質稽核要求。

---
*文件產生時間：2026/05/18*
