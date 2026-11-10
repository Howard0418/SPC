# 驗證紀錄
- 紅燈測試確認daily API原本不含保存判定；修正後ManualMeasurementsChemicalScopeTests 4/4通過。
- API以ChemicalHistoryQueryService回傳initialJudgment、judgment、judgmentSource、reason/code；不套用目前主檔重算。
- Portal靜態契約4/4通過；已保存OK／NG直接還原，Unknown顯示「無法判定」與原因，新輸入預覽流程保持。
- SPC API與Portal API／Web Release build均0警告0錯誤。
- 測試站發布：SPC API0.1.118，Portal API1.0.228／Web1.0.269；SPC首頁、health/version 200及test，Portal首頁／CSS 200 MIME正確、app-version 200，實際Portal API基底health 200且DB environment=test。
- 第一次Portal隔離發布因缺project.assets.json於寫入前停止，app_offline已清除；restore後標準publish-all成功。
- 中央進度表已同步：小工作總表新增本件、核心未完成34件／SPC相關10件；候選公式錯誤0、XLSX必要OpenXML項目檢查通過，原檔已備份後取代。
- 正式站、正式資料、schema、量測、公式與F表未修改。待使用者登入載入截圖案例確認畫面後才能DONE。
