# 實作計畫

1. 以單元測試擴充封閉缺口插入規則。
2. 建立受控工具，環境 guard 後產生 plan，僅允許七個核准 PPC。
3. 依測試日排程的公式 JSON 與 F cell signature 對應正式既有歷史。
4. 正式 COPY_ONLY/CHECKSUM 備份及 VERIFYONLY 後，在 serializable transaction 建立七個 revision、複製原區間並加入日區間、切換 policy。
5. 驗證日期邊界、預覽、冪等、保護表與正式 API 健康；同步文件及進度。
