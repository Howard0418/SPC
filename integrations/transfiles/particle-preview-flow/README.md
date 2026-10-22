# TransFiles 落塵預覽流程修補來源

- 主規格：[SPC-PARTICLE-EMPTY](../../../specs/20261022-particle-empty-chart-plan/spec.md)；執行：[TASK-001驗證](../../../specs/20261022-particle-empty-chart-plan/task001-verification.md)。
- 外部TransFiles目錄目前沒有Git repo；不擅建遠端、不把整個工具及附件複製到SPC。此處僅保存實際已套用的最小來源patch及4個流程測試，供追溯／重現。
- [particle-preview-flow.patch](particle-preview-flow.patch)僅修改`run_dust_conversion`分支，新增`test_dust_conversion_flow.py`。原有Particle JSON預覽、錯誤顯示及確認函式重用；不自動confirm。
- 基準來源（UTF-8文字正規化）SHA256：`34e8399b998559c07cd721dba8e8c991b8ba4521d80c8a74c2e044cebc4aba38`；修正後：`1066411c496c430f7d288143f95519ffd09fd673bc8f3636bafb1ef18c08c635`。
- 已在獨立暫存目錄驗證`git apply --check`、實際apply及修正來源／測試逐字一致。這是原始碼差異，不是發布二進位或測試輸出。

## 重現與回復

本機TransFiles已套用，不可重複套用。另一份相同基準工具可先備份，再於該工具根目錄執行`git apply --check <patch路徑>`，成功才`git apply <patch路徑>`；不同基準停止，不強制套用。

測試命令：`python -m unittest test_dust_conversion_flow test_dust_particle_preview test_batch_convert_gui test_etch_upload -q`。均為mock／暫存檔測試，不發送真實預覽或確認。回復可在確認只有本patch變更後反向套用；本次另存新EXE，原EXE未覆蓋，可直接回用原工具。不要刪除業務資料來回復程式。
