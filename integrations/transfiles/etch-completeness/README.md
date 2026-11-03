# TRANSFILES-ETCH-1103-TASK-001 已套用差異

主規格：[咬蝕完整性](../../../specs/20261103-etch-import-completeness/spec.md)，[驗證／發布／回復](../../../specs/20261103-etch-import-completeness/verification.md)。

TransFiles是獨立非Git工具；本目錄只保存本次**已實際套用**的最小patch及新增行為測試，並非另一份工具原始碼。來源檔位於 `C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/TransFiles`，既有版本的修改前／後SHA256見[source-hashes](source-hashes.md)。

patch包含四個模組、兩個既有測試與規則；新增測試另存 `test_etch_completeness.py`。目前來源已更新，勿再重複套用。回復時使用本機修改前備份與舊EXE；若需重建，先在隔離副本核對原版hash與 `git apply --check transfiles-etch-completeness.patch`，再套用、放入新增測試及執行相關109測試後打包。不要將舊的解析／上傳模組混用新GUI。

不含EXE、來源Excel、匯入回報、權杖、憑證、環境設定或其他未授權功能。正式相依API另候授權；未更新時工具須停止，不繞過一般保存。
