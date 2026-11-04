# 計畫

1. 建立純規劃器與單元測試，固定 12:00+08:00、班別／stage、F衝突隔離、匯入前 PPC／日防重及來源鍵冪等。
2. 以正式 manifest、正式 PPC／排程及量測快照產生不可變計畫；預期1,199合格列中864略過、335新增候選。
3. 驗證production API／SQL實體目標、Windows整合操作者與低權限procedure；建立COPY_ONLY／CHECKSUM備份並VERIFYONLY。
4. 每列先由正式日期排程產生Preview／Receipt，再由`CommitChemicalMeasurementWithTrace`寫入；保存初複驗、公式F、來源及歷史時間註記。
5. 處理統計工作，逐列查回、保護指紋及圖表/API驗證；使用相同manifest重送，確認0新增。
6. 同步需求、TODO、CHANGELOG、驗證及原進度表；只提交本件必要程式／測試／文件並推送。

## 測試矩陣

- Happy：全新PPC／日、一般線與N1/N2開收、初複驗。
- Boundary：10/01、10/30、同一新PPC／日合法開收兩列、F=.99／.995／1.02、B5=.983／1。
- Invalid：非正式環境、來源hash漂移、PPC日已存在、同來源鍵異內容、F衝突、排程缺口、重疊規格。
- Regression：既有量測、目前PPC、啟用F、公式F歷史、Portal／SPC網站版本與其他月份保持。
