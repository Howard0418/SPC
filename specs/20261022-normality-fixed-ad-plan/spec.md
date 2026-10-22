# 常態分析固定Anderson-Darling與畫面精簡（僅規劃）
- 功能ID：SPC-NORMALITY-FIXED-AD-20261022；版本1；狀態：規劃完成，待執行確認；未實作。
- 授權：使用者要求移除偏峰/峰態、第二公式固定Anderson-Darling且不顯示下拉，先加入小工作，不立即執行。
- 主系統SPC；相依Portal/TransFiles無修改；無DB/schema/量測寫入影響。
## 現況、基準與取代範圍
- 「偏峰」依既有畫面解讀為偏態Skewness；另一項為峰態Kurtosis。
- 管制圖SpcChartView及量測趨勢TrendChartView均有AJB/AD下拉、偏態/峰態卡片，normalityMethod預設AJB。
- 服務NormalizeNormalityMethod目前空白/未知皆回AJB；既有AD計算已存在，不新增第三種公式。
- 前案20261014-normality-method-switch允許切換且預設AJB；本次明確新需求在未來實作完成後取代網站的切換/預設規則。原歷史規格保留，不將規劃冒稱現行已變。
## 需求與BDD驗收
- R-001/AC-001 Given 管制圖或量測趨勢圖，When 查詢常態分析，Then 畫面與可見提示/列印若引用同區均不顯示偏態/峰態數值，不出現檢定方法下拉，固定標示「常態檢定方法：Anderson-Darling」。
- R-002/AC-002 Given 網站初載/重新查詢/切換項目或日期，Then 查詢帶normalityMethod=AD，後端結果TestName為Anderson-Darling；不得只改畫面文字、實際仍算AJB。
- R-003/AC-003 Given 省略normalityMethod，Then 後端預設AD，與網站一致；已明確指定AJB的舊API呼叫相容保留，不在網站提供入口。移除整個AJB實作/欄位不在本工作。
- R-004/AC-004 Given 固定同一批未排除資料，Then 既有AD統計量/p-value算法保持，管制界線/Cp/Cpk/原始值/公式/分段資料不受影響；不重算或覆寫歷史資料。
- R-005/AC-005 Given 少於3筆、無變異或結果空值，Then 正確顯示樣本不足/無法檢定，不以0或「符合常態」代替；0.05判定門檻沿用原規則。
## 格式建議與非範圍
- 名稱統一Anderson-Darling，API參數AD；畫面術語用「偏態」而非「偏峰」。
- p-value畫面建議一般顯示4位小數，0<p<0.0001顯示<0.0001，真正0可顯示0.0000；API保留原始數值/null，不轉成格式字串、不用顯示後四捨五入值判定。
- p-value為null/缺值顯示「無法檢定」及原因；判定建議文字「未拒絕常態假設」或「偏離常態」，避免保證資料一定常態。
- 未提供新檔案或欄位格式，不虛構日期/Excel/數值輸入錯誤。現況不符的是顯示內容、可選方法與預設值，非證據證明AD算法錯誤。
- 保留Skewness/Kurtosis API欄位及相容計算，僅網站移除顯示，不破壞其他consumer；不新建匯出/報表功能。
- 本次不改程式/資料/套件/IIS/Runtime，build/發布不適用。
- [計畫](plan.md)、[小工作](tasks.md)、[規劃驗證](verification.md)。
