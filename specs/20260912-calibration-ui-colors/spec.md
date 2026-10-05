# 小型變更：儀器校正到期管理亮色配色
- ID: 20260912-calibration-ui-colors
- 狀態：已完成
- 依使用者「顏色太暗、太重」
- 需求基準：[需求索引](../../docs/requirements.md)、[基準 3.6](../../docs/SPC_REQUIREMENTS_BASELINE_2026-08-07.md)

## 問題、預期行為與範圍
- 校正管理頁改為亮底青／天空／琥珀卡片與按鍵，解決過暗過重。不改欄位、權限、匯入或通知。
- 回歸：校正匯入 Playwright 4 通過。已發布 SPC 測試站前端（備份 `20260912-215320`）。正式站未發布。

## 需求與驗收
- R-001 / AC-001: light cyan/sky/amber UI; button names unchanged

## 計畫與任務
- InstrumentCalibrationsView.vue, InstrumentImportPanel.vue
- [x] T-001
- [x] T-002 Playwright + testhost frontend
- [x] T-003 docs

## 驗證
- Playwright calibration-import; SPC Web test only
- Playwright 4 passed; testhost build ok; backup 20260912-215320; 8083 HTTP 200; evidence page-light-theme.png
