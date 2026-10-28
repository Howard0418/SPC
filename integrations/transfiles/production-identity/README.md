# TASK-001 唯讀登入環境防護交付
TransFiles工作區不是Git repo，此處只版控實際套用的最小patch，不複製整個工具或其設定／EXE。功能仍由既有GUI測試登入使用connection_test.py；不啟用正式寫入。

- [environment-preflight.patch](environment-preflight.patch)僅修改connection_test.py及既有test_connection_settings.py：先匿名查SPC `/api/version`，實際環境不一致／未知／無法取得則在建立Portal client與登入前停止；正向流程維持。
- 新增隔離測試在[tools](../../../tools/transfiles-production-identity/test_environment_preflight.py)，既有GUI故障回歸在[login-validation](../../../tools/transfiles-login-validation/README.md)。設定TRANSFILES_SOURCE為既有工具來源目錄後，用該工具Python執行；不安裝套件，不用真實帳密。
- 獨立副本已通過`git apply --check --unidiff-zero --ignore-space-change`及同參數實際apply，UTF-8／LF正規化內容逐字一致。Windows來源含CRLF；patch以LF儲存，須先核對下列基準hash，不強制套用其他版本。
- 修改前正規化SHA256：connection_test.py=`dacfe8da1cc00859a5336da24fd5abf73937c1e8dee26999f263ef9848100df3`；test_connection_settings.py=`e20d836dce8e03e1d293c86a4c99f47e17de6482a3445d138f604243b63d0dfa`。
- 修正後正規化SHA256：connection_test.py=`f216ea38286bbcd91cc6be75caf25bb9d631d2d7af9af038a1a86b0d74854b3b`；test_connection_settings.py=`9f52e1a9ecf592013c2f84152cf2583e4b8cc1e5d2db201a3b9d1f8d989a02b5`。
- 本機來源已套用，不要再套。其他副本先備份並核對基準；不同修改範圍先停查，不盲目套用。回復只還原這兩檔或在確認無後續改動後反向patch，保留來源資料／原EXE／設定。
- 未重打包舊EXE、未發布SPC／Portal、未改認證與正式角色；實際主機／帳號待確認，001未結案。[規格與契約](../../../specs/20261028-transfiles-production-identity/spec.md)。
