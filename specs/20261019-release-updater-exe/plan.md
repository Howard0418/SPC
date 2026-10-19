# 計畫
1. 先建立C#核心與橋接測試；採Framework內建csc編譯，不引入NuGet或新runtime。
2. tools/release/desktop放WinForms、選擇／授權核心、內建資源抽取及Build腳本；嵌入既有固定五份PowerShell執行來源與一份桌面橋接。
3. UTF8即時輸出、背景程序、busy防重入／關閉、預檢綁定manifest hash、更新前顯示具體確認。
4. 產出單一EXE與使用說明至ignored release-staging，來源／測試／SDD文件提交上傳；無網站發布。
Rollback：退回EXE／工具前版，不需要操作IIS或DB。
