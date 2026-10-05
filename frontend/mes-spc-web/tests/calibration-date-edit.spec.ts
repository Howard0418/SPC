import { test, expect, Page } from '@playwright/test';

async function setup(page: Page, reject = false) {
  let instrument = { id: 1, code: 'EDIT-UI', name: '日期編輯量規', department: '品保', custodianOperatorId: 1,
    measurementSpecification: '0~150 mm', precision: '0.01 mm', remarks: '原備註', calibrationStandard: 'SOP-01', acceptanceCriteria: '±0.02 mm',
    custodianName: 'QA', cycleMonths: 12, lastCalibrationDate: '2026-08-01', nextCalibrationDate: '2027-08-01',
    usageStatus: 'Active', latestResult: 'Passed', includeCustodian: true, recipientOperatorIds: [], version: 'original' };
  await page.addInitScript(() => {
    localStorage.setItem('mes_spc_token', 'local-test-token');
    localStorage.setItem('mes_spc_user', JSON.stringify({ username: 'qa', role: 'Editor', permissions: ['calibration.manage'] }));
  });
  await page.route('**/*', async route => {
    const url = new URL(route.request().url());
    if (url.hostname !== '127.0.0.1') return route.abort();
    if (!url.pathname.startsWith('/api/')) return route.continue();
    if (url.pathname === '/api/v1/instruments/1' && route.request().method() === 'PUT') {
      const data = route.request().postDataJSON();
      expect(data.lastCalibrationDate).toBe('2026-09-01');
      expect(data.nextCalibrationDate).toBe('2027-08-01');
      expect(data.version).toBe('original');
      expect(data.reason).toBeNull();
      if (reject) return route.fulfill({ status: 409, json: { success: false, message: '資料已更新，請重新載入。' } });
      instrument = { ...instrument, ...data, version: 'updated' };
      return route.fulfill({ json: { success: true, data: instrument } });
    }
    let json: any = { success: true, data: [] };
    if (url.pathname.endsWith('/version')) json = { environment: 'test', version: 'test' };
    else if (url.pathname.endsWith('/import/access')) json = { success: true, data: { canManage: true } };
    else if (url.pathname.endsWith('/operators')) json = [{ id: 1, operatorName: 'QA', operatorCode: 'QA', isActive: true }];
    else if (url.pathname.endsWith('/summary')) json = { success: true, data: { asOfDate: '2026-09-14', windowDays: 30, upcomingCount: 0, overdueCount: 0, dueTodayCount: 0, inCalibrationCount: 0 } };
    else if (url.pathname.endsWith('/instruments')) json = { success: true, data: [instrument], total: 1 };
    await route.fulfill({ json });
  });
  await page.goto('/calibration-instruments');
  await page.getByTitle('編輯儀器', { exact: true }).click();
  const dialog = page.getByRole('dialog', { name: '編輯儀器主檔' });
  await dialog.getByLabel('上次校正日期', { exact: true }).fill('2026-09-01');
  return dialog;
}

test('editing last calibration date saves and refreshes list without changing due date', async ({ page }) => {
  const dialog = await setup(page);
  await expect(dialog.getByText('可直接修改上次與下次校正日期；儲存會留下異動紀錄，不會改寫校正歷史或自動重算下次日期。')).toBeVisible();
  await dialog.getByRole('button', { name: '儲存儀器', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await expect(page.getByRole('cell', { name: '2026-09-01', exact: true })).toBeVisible();
  await expect(page.getByRole('cell', { name: '2027-08-01', exact: true })).toBeVisible();
  await page.screenshot({ path: 'test-results/calibration-date-edit/date-edited.png', fullPage: true });
});

test('version conflict remains visible and does not close edit form', async ({ page }) => {
  const dialog = await setup(page, true);
  await dialog.getByRole('button', { name: '儲存儀器', exact: true }).click();
  await expect(dialog.getByText('資料已更新，請重新載入。')).toBeVisible();
  await expect(dialog.getByLabel('上次校正日期', { exact: true })).toHaveValue('2026-09-01');
});

test('instrument details load, edit, save and display in list', async ({page}) => {
  const dialog=await setup(page);
  await expect(dialog.getByLabel('量測規格',{exact:true})).toHaveValue('0~150 mm');
  await expect(dialog.getByLabel('精度',{exact:true})).toHaveValue('0.01 mm');
  await expect(dialog.getByLabel('校驗規範',{exact:true})).toHaveValue('SOP-01');
  await expect(dialog.getByLabel('允收標準',{exact:true})).toHaveValue('±0.02 mm');
  await dialog.getByLabel('備註',{exact:true}).fill('更新備註\n保留換行');
  const request=page.waitForRequest(r=>r.method()==='PUT' && r.url().endsWith('/instruments/1'));
  await dialog.getByRole('button',{name:'儲存儀器',exact:true}).click();
  expect((await request).postDataJSON()).toMatchObject({measurementSpecification:'0~150 mm',precision:'0.01 mm',remarks:'更新備註\n保留換行',calibrationStandard:'SOP-01',acceptanceCriteria:'±0.02 mm'});
  for (const value of ['0~150 mm','0.01 mm','SOP-01','±0.02 mm']) await expect(page.getByRole('cell',{name:value,exact:true})).toBeVisible();
  await page.screenshot({path:'test-results/calibration-details/list-desktop.png',fullPage:true});
});
test('instrument details form remains usable on mobile', async ({page}) => {
  await page.setViewportSize({width:390,height:844});
  const dialog=await setup(page);
  await dialog.getByLabel('允收標準',{exact:true}).scrollIntoViewIfNeeded();
  await expect(dialog.getByLabel('允收標準',{exact:true})).toBeVisible();
  await page.screenshot({path:'test-results/calibration-details/form-mobile.png'});
});
