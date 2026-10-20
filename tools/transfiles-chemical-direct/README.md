# TransFiles日期公式DB直寫來源交付
此目錄保存TASK-005新增模組及最小GUI patch的版本化來源；TransFiles既有工作區不是Git repo，不建立新repo，也不將無關檔案／資料提交。現場模組與本目錄hash一致。

- 將兩個chemical_direct_*檔案放至既有TransFiles來源根；以`git apply --check --unidiff-zero gui-integration.patch`核對，再用`git apply --unidiff-zero gui-integration.patch`套用至`batch_convert_gui.py`（已整合的工作區不要重複套用）。Python測試依既有環境的openpyxl與tls_context，無新套件。
- 打包沿用既有BatchConvertTool.spec，datas加入`('chemical_direct_write.ps1', '.')`；不把SQL連線字串或密碼放入spec或EXE。GUI會用內建腳本，PowerShell執行原則僅對該子程序設定，不修改系統原則。
- API只支援`https://spctest.pmr.com.tw/api`；SQL連線環境變數`SPC_CHEMICAL_DB_CONNECTION`須指定`PMR_SPC_TEST`及Windows整合認證。缺設定或非測試庫拒絕，不改走API匯入。DBA須建立與個人Portal AD帳號相同的DB principal，僅加入`SpcChemicalDirectWriter`，不得用dbo／db_owner／管理者憑證。群組DB principal與個人actor不同時拒絕。
- SQL錯誤18452表示Windows信任／網域尚未打通；不得拿API／管理者SQL帳密繞過。不自動建立正式登入或改網域設定。
- 使用GUI「建立原始值範本」，填PpcId、帶offset的MeasuredAt、Primary、Secondary（適用時）、RecheckPrimary／Secondary（適用時）、SamplingPhase=OPEN/MIDDLE、SamplingStage=GENERAL（N1/N2為OPEN/CLOSE）。原始值儲存為文字，不能用已取位濃度冒充滴定值；無效來源有工作表／列號與修正提示。
- 「日期公式DB匯入（測試）」先正常Portal登入、SPC預覽，再經使用者確認呼叫prepare及SQL程序。Pending僅表示量測落地，不代表統計完成；用結果JSON的「查詢日期公式統計狀態」查Completed／Failed。
- 同檔同列同內容重送回原ID；不同內容／同日已存在拒絕，明確修正走Portal。receipt／trace不可變，不能刪資料來重送。

[主規格](../../specs/20261020-formula-trace-task005/spec.md)；[驗證](../../specs/20261020-formula-trace-task005/verification.md)。
