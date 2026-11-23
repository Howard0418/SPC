# 正式同步驗證

- PMR_SPC_TEST 與 PMR_SPC_2026 的 113 筆啟用 CHEM PPC 依機台代碼、槽位名稱及特性代碼全數唯一配對。預檢固定 SHA-256 A1606D4FB8477B59C376159AAC48CE39688C88AFB57D88F7CC5154735A4B1241。
- 於正式庫備份 COPY_ONLY／CHECKSUM 並驗證 RESTORE VERIFYONLY。首次保護指紋查詢以錯誤鍵名提前停止，無寫入。按實際主鍵修正並通過重新預檢後，以單一 Serializable 交易更新 48 筆共 173 欄。
- 藥液名稱 44、槽容 43、添加管制值 44、稀釋管制值 40、備註 2。零測試空白覆蓋。其他 PPC 屬性與 JSON 欄位符合原快照；本工作新增 48 筆設定版本與 48 筆稽核，正式查回均指向最新版本。重送 NoChange。
- 正式量測、Receipt／Trace、公式政策／排程／生效區間、F 版本／歷程／格值九表的交易前後指紋相同。調整與調整量公式本體、規格、其他設定未改。
- 回復證據存放於正式主機 C:/ProgramData/PMR-Releases/Production/SPC-CHEM-ADJUST-PROD-1123。使用 review.json 逐欄比對現值與本批後值，匹配才以原值追加版本與稽核；避免整庫還原覆寫後續資料。
- 正式調整參數頁 HTTP 200、API 0.1.140/production 及健康檢查通過。純資料同步，Web／API 版號與正式通知設定維持，未重新發布。不核銷其他測試公式補缺項目。
