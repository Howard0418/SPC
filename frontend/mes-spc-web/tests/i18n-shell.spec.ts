import { test, expect, Page } from '@playwright/test';
const browserErrors = new WeakMap<Page, string[]>();
test.beforeEach(({page}) => {
  const errors: string[] = []; browserErrors.set(page, errors);
  page.on('pageerror', error => errors.push(error.message));
});
test.afterEach(({page}) => expect(browserErrors.get(page)).toEqual([]));

async function prepare(page: Page, role = 'Editor', id = 101) {
  await page.addInitScript(({role,id}) => {
    if (sessionStorage.getItem('i18n-fixture-initialized')) return;
    sessionStorage.setItem('i18n-fixture-initialized','true');
    localStorage.setItem('mes_spc_token','mock-ui-only-token');
    localStorage.setItem('mes_spc_user',JSON.stringify({id,role,username:'i18n-fixture',displayName:'原始姓名 <b>保持文字</b>'}));
  }, {role,id});
  // All API traffic is intercepted, including on the published test site.
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    if (!path.startsWith('/api/')) return route.continue();
    expect(route.request().method()).toBe('GET');
    const body = path.endsWith('/version') ? {version:'0.1.72',environment:'test'} :
      path.endsWith('/settings/smtp') ? {smtp:{host:'fixture.invalid',port:25,recipientOperatorIds:[]},operators:[],departments:[]} : [];
    await route.fulfill({status:200,json:body});
  });
}

for (const width of [1920,1366,1280,390]) for (const locale of ['zh-TW','zh-CN','en-US']) {
  test(`${width} ${locale}: header usable and labels wrap`,async ({page})=>{
    await page.setViewportSize({width,height:900}); await prepare(page);
    await page.goto('/parts');
    const picker=page.getByTestId('locale-select');
    await picker.selectOption(locale); await expect(page.locator('html')).toHaveAttribute('lang',locale);
    await picker.focus(); await expect(picker).toBeFocused();
    const geometry=await page.locator('header').evaluate(el=>({width:el.clientWidth,scroll:el.scrollWidth}));
    expect(geometry.scroll).toBeLessThanOrEqual(width); expect(geometry.width).toBeLessThanOrEqual(width);
    const box=await picker.boundingBox(); expect(box!.x).toBeGreaterThanOrEqual(0); expect(box!.x+box!.width).toBeLessThanOrEqual(width);
    await expect(page.locator('aside a[href="/parts"]')).toHaveText(locale==='en-US'?'Product parts':locale==='zh-CN'?'产品料号主档':'產品料號主檔');
    expect(await page.locator('aside a[href="/parts"] span').evaluate(el=>getComputedStyle(el).whiteSpace)).toBe('normal');
    await expect(page.locator('header')).toContainText('原始姓名 <b>保持文字</b>');
    await expect(page.locator('header b')).toHaveCount(0);
    await expect(page.locator('header')).toContainText(locale==='en-US'?'Editor':locale==='zh-CN'?'编辑者':'編輯者');
  });
}

test('unsaved form, query, instance and machine roles survive switching',async({page})=>{
  await prepare(page); await page.goto('/settings/smtp?keep=42');
  const host=page.getByTestId('settings-form.host');
  await expect(host).toHaveValue('fixture.invalid'); await host.fill('unsaved.invalid');
  await host.evaluate(el=>{(window as any).__originalInput=el});
  const requests:string[]=[];page.on('request',r=>{if(r.url().includes('/api/'))requests.push(r.url())});
  for(const lang of ['en-US','zh-CN','zh-TW']) {
    await page.getByTestId('locale-select').selectOption(lang);
    await expect(host).toHaveValue('unsaved.invalid');
    expect(await host.evaluate(el=>el===(window as any).__originalInput)).toBe(true);
    await expect(page).toHaveURL(/\/settings\/smtp\?keep=42$/);
  }
  expect(requests).toEqual([]);
  expect(await page.evaluate(()=>JSON.parse(localStorage.getItem('mes_spc_user')!).role)).toBe('Editor');
  await page.goto('/spc/query?keep=42');
  const query=page.getByTestId('trace-queryParams.workOrderNo');await query.fill('WO-UNCHANGED');
  await page.getByTestId('locale-select').selectOption('en-US');await expect(query).toHaveValue('WO-UNCHANGED');
  await expect(page).toHaveURL(/\/spc\/query\?keep=42$/);
});

test('preference reload and subject isolation',async({page})=>{
  await prepare(page);await page.goto('/parts');
  await page.getByTestId('locale-select').selectOption('en-US');await page.reload();
  await expect(page.getByTestId('locale-select')).toHaveValue('en-US');
  await page.evaluate(()=>localStorage.setItem('mes_spc_user',JSON.stringify({id:202,role:'Editor',username:'other'})));
  await page.locator('aside a[href="/processes"]').click();
  await expect(page.getByTestId('locale-select')).toHaveValue('zh-TW');
  await page.evaluate(()=>localStorage.removeItem('mes_spc_user'));
  await page.locator('aside a[href="/parts"]').click();
  await expect(page.getByTestId('locale-select')).toHaveValue('zh-TW');
});

test('Viewer retains menu and protected route rules',async({page})=>{
 await prepare(page,'Viewer');await page.goto('/equipment-status');
 for(const lang of ['en-US','zh-CN','zh-TW']) {
  await page.getByTestId('locale-select').selectOption(lang);
  await expect(page.locator('aside a[href="/parts"]')).toHaveCount(0);
  await expect(page.locator('aside a[href="/spc"]')).toBeVisible();
 }
 await page.goto('/parts');await expect(page).not.toHaveURL(/\/parts$/);
 expect(await page.evaluate(()=>JSON.parse(localStorage.getItem('mes_spc_user')!).role)).toBe('Viewer');
});

test('local storage write failure preserves active language and draft',async({page})=>{
 await prepare(page);await page.goto('/settings/smtp');
 const host=page.getByTestId('settings-form.host');await expect(host).toHaveValue('fixture.invalid');await host.fill('unsaved.invalid');
 await page.evaluate(()=>{const original=Storage.prototype.setItem;Storage.prototype.setItem=function(key,value){if(key.startsWith('spc:locale:'))throw new DOMException('blocked','QuotaExceededError');return original.call(this,key,value)}});
 await page.getByTestId('locale-select').selectOption('en-US');
 await expect(page.getByRole('status')).toContainText('could not be saved');await expect(host).toHaveValue('unsaved.invalid');
 await expect(page.locator('html')).toHaveAttribute('lang','en-US');
});

test('local storage read failure falls back with visible notice',async({page})=>{
 await prepare(page);
 await page.addInitScript(()=>{const original=Storage.prototype.getItem;Storage.prototype.getItem=function(key){if(key.startsWith('spc:locale:'))throw new DOMException('blocked','SecurityError');return original.call(this,key)}});
 await page.goto('/parts');await expect(page.getByTestId('locale-select')).toHaveValue('zh-TW');
 await expect(page.getByRole('status')).toContainText('無法保存本機偏好');
});
