import { test, expect, Page } from '@playwright/test';

const row = (n: number, status = 'Ready') => ({ rowNumber: n, code: `QA-${n}`, name: `量規 ${n}`, location: '發泡實驗室', calibrationMethod: '外校', measurementSpecification: '0~150 mm', precision: '0.01 mm', remarks: '測試備註', calibrationStandard: 'SOP-01', acceptanceCriteria: '±0.02 mm', rawCycle: '1 次/年',
  rawLastDate: '46098', rawNextDate: '46462', cycleMonths: 12, lastCalibrationDate: '2026-03-17', nextCalibrationDate: '2027-03-16',
  errors: status === 'Invalid' ? ['校驗週期無法辨識'] : [], warnings: ['下次校正日使用公式儲存值，請確認 Excel 已重算儲存。'], status });

async function setup(page: Page, canManage = true, commitFails = false) {
  await page.addInitScript(() => {
    localStorage.setItem('mes_spc_token', 'isolated-test-token');
    localStorage.setItem('mes_spc_user', JSON.stringify({ username: 'qa', role: 'Editor', permissions: ['calibration.manage'] }));
  });
  // Deny unexpected network requests; only local Vite assets and mocked API are used.
  await page.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.hostname !== '127.0.0.1') return route.abort();
    if (!url.pathname.startsWith('/api/')) return route.continue();
    let json: any = { success: true, data: [] };
    if (url.pathname.endsWith('/version')) json = { environment: 'test', version: 'test' };
    else if (url.pathname.endsWith('/import/access')) json = { success: true, data: { canManage } };
    else if (url.pathname.endsWith('/import/preview')) json = { success: true, data: { hash: 'test-sha', sheetName: 'QA-2-003-02', rows: [row(6), row(7), row(8, 'Invalid'), row(9, 'Existing')] } };
    else if (url.pathname.endsWith('/import/commit')) {
      const optionsText = route.request().postData() || '';
      expect(optionsText).toContain('"selectedRows":[6]');
      expect(optionsText).toContain('"department":"品保"');
      expect(optionsText).toContain('"custodianOperatorId":1');
      json = { success: !commitFails, message: commitFails ? '整批已回復' : '', data: { added: commitFails ? 0 : 1, skipped: 0, failed: commitFails ? 1 : 0, rows: [{ rowNumber: 6, code: 'QA-6', status: commitFails ? 'Failed' : 'Added', message: commitFails ? '整批已回復' : '已新增' }] } };
      return route.fulfill({ status: commitFails ? 409 : 200, json });
    }
    else if (url.pathname.endsWith('/operators')) json = [{ id: 1, operatorName: '品保人員', operatorCode: 'QA', isActive: true }];
    else if (url.pathname.endsWith('/summary')) json = { success: true, data: { asOfDate: '2026-09-11', windowDays: 30, upcomingCount: 0, overdueCount: 0, dueTodayCount: 0, inCalibrationCount: 0 } };
    else if (url.pathname.endsWith('/instrument-calibrations/settings')) json = { success: true, data: { reminderDays: [30, 7, 0], isEnabled: false } };
    else if (url.pathname.endsWith('/instrument-calibrations/test-email')) json = { success: true, data: { recipient: 'qa@example.invalid', instrumentCode: 'I-TEST' }, message: '測試通知已寄出。' };
    else if (url.pathname.endsWith('/instruments')) json = { success: true, data: [], total: 0 };
    await route.fulfill({ json });
  });
  await page.goto('/calibration-instruments');
}
async function preview(page: Page) {
  await page.getByRole('button', { name: 'Excel 批次匯入', exact: true }).click();
  await page.getByLabel('儀器 Excel 檔案').setInputFiles({ name: 'template.xlsx', mimeType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet', buffer: Buffer.from('mocked-upload') });
  await page.getByRole('button', { name: '解析／重新預覽' }).click();
  await expect(page.getByText('工作表：QA-2-003-02')).toBeVisible();
  await expect(page.getByRole('dialog').getByRole('columnheader', { name: '校驗方式' })).toBeVisible();
  await expect(page.getByRole('dialog').getByText('外校').first()).toBeVisible();
  for (const label of ['量測規格','精度','備註','校驗規範','允收標準']) await expect(page.getByRole('dialog').getByRole('columnheader',{name:label,exact:true})).toBeVisible();
  await expect(page.getByRole('dialog').getByText('0~150 mm').first()).toBeVisible();
}
async function choose(page: Page) {
  await page.getByLabel('選取第 6 列', { exact: true }).check();
  await page.getByLabel('匯入部門').fill('品保');
  await page.getByLabel('匯入保管人').selectOption('1');
  await page.getByLabel('匯入使用狀態').selectOption('Active');
  await page.getByLabel('確認匯入資料').check();
}

test('preview excludes invalid/existing rows, requires explicit fields and submits selected rows only', async ({ page }) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await setup(page); await preview(page);
  await expect(page.getByLabel('選取第 8 列', { exact: true })).toBeDisabled();
  await expect(page.getByLabel('選取第 9 列', { exact: true })).toBeDisabled();
  await expect(page.getByRole('button', { name: '確認匯入 0 筆' })).toBeDisabled();
  await page.getByLabel('全選可匯入儀器').check();
  await expect(page.getByLabel('選取第 7 列', { exact: true })).toBeChecked();
  await page.getByLabel('全選可匯入儀器').uncheck();
  await choose(page);
  await page.screenshot({ path: 'test-results/calibration-import/preview-desktop.png', fullPage: true });
  await page.getByRole('button', { name: '確認匯入 1 筆' }).click();
  await expect(page.getByText('新增 1 筆／略過 0 筆／失敗 0 筆')).toBeVisible();
  await expect(page.getByRole('button', { name: '確認匯入 1 筆' })).toBeDisabled();
  expect(errors).toEqual([]);
});

test('revoked permission hides import action', async ({ page }) => {
  await setup(page, false);
  await expect(page.getByRole('heading', { name: '儀器校正到期管理' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Excel 批次匯入', exact: true })).toHaveCount(0);
  await page.screenshot({ path: 'test-results/calibration-import/page-light-theme.png', fullPage: true });
});

test('mobile preview and rollback results remain usable', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await setup(page, true, true); await preview(page); await choose(page);
  await page.screenshot({ path: 'test-results/calibration-import/preview-mobile.png', fullPage: true });
  await page.getByRole('button', { name: '確認匯入 1 筆' }).click();
  await expect(page.getByText('新增 0 筆／略過 0 筆／失敗 1 筆')).toBeVisible();
  await expect(page.getByRole('dialog').getByRole('alert')).toContainText('整批已回復');
});

test('settings modal can send a labelled test calibration email', async ({ page }) => {
  await setup(page);
  await page.getByRole('button', { name: '提醒天數設定' }).click();
  await page.getByLabel('測試通知收件人').fill('qa@example.invalid');
  await page.getByRole('button', { name: '發送測試校正通知' }).click();
  await expect(page.getByText('已寄出測試通知至 qa@example.invalid（儀器 I-TEST）。')).toBeVisible();
});
test('reminder picker preserves custom days and saves selected numeric days', async ({ page }) => {
  await setup(page);
  await page.route('**/api/v1/instrument-calibrations/settings', route => route.fulfill({ json: { success: true, data: { reminderDays: [45, 7, 0], isEnabled: false } } }));
  await page.getByRole('button', { name: '提醒天數設定' }).click();
  await expect(page.getByLabel('提前 45 天', { exact: true })).toBeChecked();
  await page.getByLabel('提前 14 天', { exact: true }).check();
  await page.getByLabel('自訂提前天數').fill('366');
  await page.getByRole('button', { name: '加入', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('0～365');
  await page.getByLabel('自訂提前天數').fill('60');
  await page.getByRole('button', { name: '加入', exact: true }).click();
  const request = page.waitForRequest(r => r.method() === 'PUT' && r.url().endsWith('/instrument-calibrations/settings'));
  await page.getByRole('button', { name: '儲存設定', exact: true }).click();
  expect((await request).postDataJSON()).toEqual({ reminderDays: [60, 45, 14, 7, 0], isEnabled: false, notificationChannel: "Email" });
});
test('reminder picker requires a selection and restores defaults on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await setup(page);
  await page.getByRole('button', { name: '提醒天數設定' }).click();
  for (const label of ['提前 30 天', '提前 7 天', '到期當天']) await page.getByLabel(label, { exact: true }).uncheck();
  await page.getByRole('button', { name: '儲存設定', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('1～12');
  await page.getByRole('button', { name: '恢復預設', exact: false }).click();
  for (const label of ['提前 30 天', '提前 7 天', '到期當天']) await expect(page.getByLabel(label, { exact: true })).toBeChecked();
  await page.screenshot({ path: 'test-results/calibration-import/reminder-mobile.png', fullPage: true });
});
test('chat selection sends a draft webhook, shows failures and saves the channel', async ({ page }) => {
  await setup(page);
  await page.route('**/api/v1/instrument-calibrations/test-chat', route => route.fulfill({ json: { success: false, message: '傳送結果不確定，請先查看 Chat 群組。' } }));
  await page.getByRole('button', { name: '提醒天數設定' }).click();
  await page.getByRole('radio', { name: 'Synology Chat', exact: true }).check();
  await expect(page.getByLabel('測試通知收件人')).toHaveCount(0);
  await page.getByRole('button', { name: '發送測試 Chat 通知' }).click();
  await expect(page.getByText('請先貼上 Synology Chat Webhook 網址。')).toBeVisible();
  const url = 'https://nas.example.invalid/webapi/entry.cgi?api=SYNO.Chat.External&method=incoming&version=2&token=test-only';
  await page.getByLabel('Synology Chat Webhook 網址', { exact: true }).fill(url);
  const sent = page.waitForRequest(r => r.url().endsWith('/test-chat'));
  await page.getByRole('button', { name: '發送測試 Chat 通知' }).click();
  expect((await sent).postDataJSON()).toEqual({ chatWebhookUrl: url });
  await expect(page.getByText('傳送結果不確定，請先查看 Chat 群組。')).toBeVisible();
  await page.getByRole('radio', { name: 'Email', exact: true }).check();
  await expect(page.getByText('傳送結果不確定，請先查看 Chat 群組。')).toHaveCount(0);
  await page.getByRole('radio', { name: 'Synology Chat', exact: true }).check();
  const saved = page.waitForRequest(r => r.method() === 'PUT' && r.url().endsWith('/settings'));
  await page.getByRole('button', { name: '儲存設定', exact: true }).click();
  expect((await saved).postDataJSON()).toEqual({ reminderDays: [30, 7, 0], isEnabled: false, notificationChannel: 'SynologyChat', chatWebhookUrl: url });
});
test('saved chat webhook can be tested on mobile without exposing URL', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await setup(page);
  await page.route('**/api/v1/instrument-calibrations/settings', route => route.fulfill({ json: { success: true, data: { reminderDays: [30, 7, 0], isEnabled: false, notificationChannel: 'SynologyChat', chatWebhookConfigured: true } } }));
  await page.route('**/api/v1/instrument-calibrations/test-chat', route => route.fulfill({ json: { success: true, message: '測試通知已送至 Synology Chat 群組。' } }));
  await page.getByRole('button', { name: '提醒天數設定' }).click();
  await expect(page.getByRole('radio', { name: 'Synology Chat', exact: true })).toBeChecked();
  await expect(page.getByLabel('Synology Chat Webhook 網址', { exact: true })).toHaveValue('');
  await page.getByRole('button', { name: '發送測試 Chat 通知' }).click();
  await expect(page.getByText('測試通知已送至 Synology Chat 群組。')).toBeVisible();
  await page.screenshot({ path: 'test-results/calibration-import/chat-mobile.png' });
});
