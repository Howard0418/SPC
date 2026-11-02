# FORMULA-IMPORT-005 矩陣日報解析修補

- TransFiles外部目錄沒有Git repo，本件沿既有integrations方式只保留必要最小patch，不搬移整個工具或附件。
- 主規格：[T3b預覽與驗收](../../../specs/20261101-formula-import-test-source-regression/preview-20261102.md)。
- 修改：A欄明確分組線別、空白量測列上下文、切換線別清除槽位；矩陣改列迭代與F表快取，避免read_only逐格重掃XML。保留工作表總標籤與線別來源格，parser不自動把PT改成PT1。
- 原碼先紅2項，修正後68項相關測試通過；111筆真實單檔優化前後逐筆相同，約4.2秒降至0.141秒。
- 來源Excel、API帳密／權杖、報告、設定、EXE與測試輸出不提交；未打包／排程／發布網站或匯入。

## 重現與回復
本機原碼已套用；另一份同基準工具先備份，於工具根目錄執行git apply --unidiff-zero --check後以git apply --unidiff-zero套用（零context patch須先核SHA256基準）。已在獨立副本check/apply並逐字比對成功。不同基準不強制套用；回復只反向套本patch，不刪業務資料。
測試：python -m unittest test_chemical_daily_parser.py test_scheduled_import.py test_batch_convert_gui.py test_chemical_versioning.py test_chemical_direct_writer.py

UTF-8正規化來源SHA256：
- chemical_daily_parser.py：修改前 `a4299847b2514dcc4dba0c3d19aa0f759ac5595bb9466a1bbd4e45c7d5d50125`；修改後 `57636c62ddbec4da8ba85a0f263127a509246a7b9e6157eeecd59df8dadc37b8`。
- test_chemical_daily_parser.py：修改前 `11038c845cb4e6a5f7154f83e57d4cc8ef1734a89dc1394555cd3eb3d60142d8`；修改後 `888e5e3347044418bce017d220ae844ea023f398baeffc4b3c26cb72968df4fc`。
