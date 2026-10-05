# 技術計畫
- [規格](spec.md)，版本1
- 修改 ManualMeasurementsV1Controller、UploadService；保持API契約。
- 日期使用半開區間 [當日,次日)，僅缺 PortalDailyDate 回退 MeasuredAt；calendar 的 hasData 不再要求日報日期存在。
- 先增SQLite行為測試：日期回退、優先順序、日界、來源/階段、衝突、同ID更新；再實作。
- 執行 Chemical 測試與 API publish。發布前核對 IIS 和 PMR_SPC_TEST，备份release/test/backend、保留設定；失敗還原。只發布SPC測試API。
