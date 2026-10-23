# 三語參考資料（未接入產品）
- [manifest](manifest.json)標示契約1.0.0/詞彙1.0.0，三語[zh-TW](zh-TW.json)、[zh-CN](zh-CN.json)、[en-US](en-US.json)。
- [metadata](message-metadata.json)保存逐key來源hash、context、surface、參數multiset及protectedTokens；[ledger](review-ledger.json)逐key/locale紀錄皆draft，reviewer/reviewedAt為null。
- 29個key／87個審核列，屬種子範例，並非三站完整翻譯目錄。所有locale（含繁中）均待審，未達成人工合格率或覆蓋率。
- 參考契約[來源hash/編碼/fallback](../contract.md)；字典不包含帳密、使用者資料、URL、公式或DB狀態值。
- 本次未建立產品catalog loader/localizer或CI工具；未修改三站網站。後續以module拆分與實際source inventory增補，避免把範例直接當全站翻譯覆蓋。
