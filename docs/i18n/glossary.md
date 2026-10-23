# 詞彙初稿 v1.0.0
- 日期2026-10-24；所有三語對照均為AI初稿，審核者/日期未指定，禁止視為已核准術語。
- 同詞跨情境可有不同key；槽位英文、製程/產品用語及人事福利法律用詞需業務確認。不靠字典變更既有業務定義。

| zh-TW | zh-CN | en-US | 情境與不可改值 |
|---|---|---|---|
| 管制圖 | 控制图 | Control chart | SPC圖表，不同於趨勢圖 |
| 量測趨勢圖 | 测量趋势图 | Measurement trend chart | 不隱含SPC規則 |
| 管制界線 | 控制限 | Control limits | UCL/CL/LCL保留 |
| 規格界線 | 规格限 | Specification limits | USL/LSL保留 |
| 中心線 | 中心线 | Center line | CL |
| 規格下限 | 规格下限 | Lower specification limit | LSL，不同於LCL |
| 管制下限 | 控制下限 | Lower control limit | LCL，不同於LSL |
| 製程能力指數 | 过程能力指数 | Process capability index | Cp/Cpk原代碼保留 |
| 常態檢定 | 正态性检验 | Normality test | Anderson-Darling名稱不翻譯 |
| 未拒絕常態假設 | 未拒绝正态假设 | Normality hypothesis not rejected | 不寫成保證常態 |
| 抽樣數 | 抽样数 | Sample size | 與量測列數/子群數分開 |
| 量測值 | 测量值 | Measured value | 原始數值不改 |
| 已排除點 | 已排除点 | Excluded points | 不是刪除資料 |
| 日期分段 | 日期分段 | Date segment | 有效日/時區不隨語言改 |
| 檢驗特性 | 检验特性 | Inspection characteristic | 既有主檔名稱可能是業務原文 |
| 槽體 | 槽体 | Tank | 不得與槽位合併 |
| 槽位 | 槽位 | Slot | 需業務審核英文情境 |
| 工號 | 工号 | Employee ID | 值保持原樣 |
| 訪客預約 | 访客预约 | Visitor reservation | 預約申請 |
| 員工進出廠區 | 员工进出厂区 | Employee site entry and exit | 進/出方向不可反譯 |
| 團體保險 | 团体保险 | Group insurance | 福利/人事語境待審 |
| 文件櫃 | 文档库 | Document library | KM導覽，不改文件原文 |
| 簽核 | 审批 | Approval | 工作流狀態值不翻譯 |
| 權限 | 权限 | Permission | 不改ACL或角色 |
| 版本 | 版本 | Version | 版號原字串保留 |

- 統計/公式、安全權限、刪除/送出警告為必須全量人工審核類別；本稿不含可直接啟用的高風險操作譯文。
- 變更詞彙時提升glossaryVersion，查出依賴key並重審；以[ledger欄位](contract.md)記錄，不用AI自評替代業務簽核。
