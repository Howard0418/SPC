# 計畫
1. 先測純workbook與InMemory CPK：三語文字、欄序／數據／型別／style／color及原範圍相容。
2. Overview僅抽取workbook呈現以便隔離驗證，原summary query完整保持；已知chart kind／計算方式由穩定代碼建立display metadata。CPK原status控制分支保留，另加key供cell翻譯。
3. 手動request明示語言／CPK僅顯示query，Report Builder與Sender末尾optional locale、背景預設TW；報表Email純內容／HTML編碼，必要fake同步。
4. draft資源、必要API／前端／CSV及背景回歸／人讀workbook核對、build／只test發布／文件及提交上傳，本次完成後停止；其餘維持暫停。
