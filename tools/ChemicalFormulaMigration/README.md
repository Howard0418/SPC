# 藥液舊規則轉換

來源 `SourceFormula` 空白或為「直接量測值」時，輸出狀態 `SOURCE_FORMULA_UNSET`，不建立濃度公式。不以預設係數1代替缺少的公式。有明確來源公式的規則維持原轉換行為。

## 清理舊自動補式

只在 `migratedFrom` 對應未設定來源、目前濃度為精確自動生成恆等式，且複驗沒有不同公式時清理；保留其他設定並新增公式版本。預設只盤點；一般`--apply`僅允許測試庫。

```powershell
dotnet run --project tools/ChemicalFormulaMigration -- --clear-unset-formulas --config=TEST_CONFIG_PATH --rules=RULES_JSON_PATH --output=EVIDENCE_DIRECTORY
```

測試套用加 `--apply`。證據包含before／after、來源雜湊、業務名稱與保護資料指紋；相同來源重送為0異動。證據含實際資料，留於本機，不提交版本控制。

正式只支援本件已明示核准的7項清單：另加`--production-reviewed --reviewed=REVIEWED_BEFORE_JSON_PATH`，驗證固定審查證據雜湊、來源、環境及設定。加`--apply`後先做完整正式備份與還原驗證，再於交易內更新；任何不符都拒絕，不能用此模式擴張到其他項目。
