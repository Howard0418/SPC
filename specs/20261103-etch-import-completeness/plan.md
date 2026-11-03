# 實作與回復計畫

1. 備份TransFiles必要原碼，先建立解析／清冊、未處理列、資料庫既有鍵、後置驗證與dry-run重跑失敗測試。
2. etch_reports.py新增嚴格線別、範圍、四區塊清冊與摘要警告；不變更舊輸出模式，完整日報輸出沿既有格式增加核對清單。
3. SPC服務新增唯讀Inspect與受控匯入方法；新增有授權的inspect端點，舊strict端點保持相容回應並按明確正式開關／環境白名單放行。依使用者補充加入第一次→第二次交易與Before追溯。Portal僅調整strict流程：必須由SPC可信回應提供第一次完整原值指紋，且Portal舊資料吻合才可取代；不接受client自行宣告任意覆寫，不變更schema及一般保存PUT。
4. etch_upload.py保留Portal登入與雙站新資料流程；加入SPC具名契約核對、已存在略過、未處理列與確認後查回；GUI顯示日期範圍、來源完整性及待補清單。scheduled_import僅修正咬蝕來源hash誤當已匯入，不改藥液／落塵或啟用排程。
5. 執行mock／SQLite及隔離SQL／實際測試唯讀核對、來源九十月重新dry-run；只在專用隔離測試日線做必要新資料驗證，不再次寫入正式歷史。
6. 發布SPC測試API並完成Web／資產MIME／health/version／瀏覽器實際API驗證；TransFiles新EXE獨立交付及GUI啟動確認，正式API候選包需從實際正式基準僅取本次差異，不將稽核等未授權功能一併發布。
7. 同步規則、四份SDD、工作池、原進度表及同表總表；TransFiles無Git，僅於SPC integrations保存實際必要patch／測試與hash追溯，不提交整個工具、EXE、設定或證據。

Happy：四線全樣本／新資料確認查回；Boundary：日期起迄、25／50、重送／舊hash；Invalid：錯線、來源變動、缺點、NaN、回應不完整、環境錯置；Regression：藥液／落塵、既有保存PUT、原始檔、歷史主檔及正式資料保持。

回復：TransFiles回用舊EXE及必要原碼備份；測試API還原備份payload、不變runtime設定。無schema rollback；正式候選未獲發布授權不部署、不開開關。單元測試／preview／inspect不寫入；必要隔離測試新增依具名batch追溯，不刪業務資料。
