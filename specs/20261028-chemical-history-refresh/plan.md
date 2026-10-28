# 執行計畫
1. 先補測試重現保存成功＋查詢失敗、GET重試、狀態與競態／分頁。
2. 限縮修改Portal查詢流程，不改寫入端點與SPC契約。
3. Node／Playwright相關回歸與Portal API/Web建置；核對IIS指向portaltest後用既有發布腳本發布。
4. 文件、原進度表／原生里程碑與小工作總表同步，範圍檢查後分repo提交上傳。
5. Rollback：測試發布前備份實際API/Web目錄及設定指紋，需回復時還原同一測試目錄；不涉及DB回復。
