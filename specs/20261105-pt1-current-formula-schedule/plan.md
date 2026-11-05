# 計畫

1. 先建純排程合併／環境／漂移拒絕測試，再實作本件專用工具，重用ChemicalFormulaScheduleService.PublishAsync及PreviewAsync，不改核心API。
2. 重新唯讀核對兩區四項業務鍵、完整目前配置、測試11/03起排程、八格F；正式只重用完全相符既有公式歷史／F快照。
3. 正式完整備份CHECKSUM及VERIFYONLY，Serializable交易內固定原revision、保留19段，再發布4份完整revision共80區間；所有保護表及非目標資料在commit前SHA256保持。
4. 初複驗0／一般／較大值、11/03前後、11/05與後續日期及原十月實際日期均驗證；另一連線重跑為0新增，不建立量測或Receipt。
5. 正式health/version及Portal同一服務純預覽驗證；若瀏覽器可用才另外實際預覽，不能冒稱人工驗收。無Web/API更新，發布不適用。
6. 原進度表父件265列及11/05原生里程碑同步，核心45／SPC22保持；文件提交推送Develop，不上傳設定／證據／XLSX。
