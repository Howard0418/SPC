# 執行計畫

1. 固定 formal／primary／secondary manifest、2026-11-04 audit 雜湊及正式環境身分。
2. 先建立 matcher／冪等／排除範圍單元測試，再實作專用正式更正工具。
3. 唯讀預檢 493 個自然鍵、目前 rowversion、初複驗、Receipt／Trace、統計及警報，輸出待核對計畫。
4. 建立正式 COPY_ONLY＋CHECKSUM 備份並 RESTORE VERIFYONLY。
5. Serializable 單一交易重新試算、更新原量測、寫 Receipt／Trace／批次稽核，軟刪舊衍生結果並排入重算。
6. 逐筆查回、重送 0 異動、圖表／API 連線驗證；產出更正後 Excel 核對報告。
7. 同步規格、驗證、需求、變更紀錄、TODO 及原進度表，提交並推送。
