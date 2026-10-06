# 藥液分析公式版本記錄與回復任務

## 階段 1：規格與現況

- [x] TASK-001：盤點現有公式來源、修改流程與版本缺口。
- [x] TASK-002：建立規格、計畫與驗收條件。

## 階段 2：後端版本基礎

- [x] TASK-003：建立藥液公式版本 entity、DbSet、EF mapping 與 migration。
- [x] TASK-004：新增公式版本服務，負責變更比對、版本建立與回復。
- [x] TASK-005：整合 `PUT /part-process-characteristics/{id}`，公式變更時自動建立版本。
- [ ] TASK-006：新增版本查詢與回復 API。
- [ ] TASK-007：新增後端測試，涵蓋修改、無變更、非 CHEM、回復與權限。

## 階段 3：前端單筆回復入口

- [ ] TASK-008：在藥液公式編輯區加入版本紀錄入口。
- [ ] TASK-009：提供指定版本回復操作與確認提示。
- [ ] TASK-010：前端 build 與基本 UI 驗證。

## 階段 4：發布與文件

- [ ] TASK-011：發布 SPC 測試站 backend/frontend 並 smoke test。
- [ ] TASK-012：同步 `CHANGELOG_CUSTOM.md`、`TODO.md`、需求索引與驗證紀錄。
