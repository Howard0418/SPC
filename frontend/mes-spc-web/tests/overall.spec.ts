import { test, expect } from '@playwright/test';
import { prepare } from './fixtures/chart-query';

const summary = { controlItemCount:10,registeredItemCount:12,disabledItemCount:2,oocItemCount:2,oocCount:5,
  oosItemCount:1,oosCount:1,abnormalCount:5,handledCount:2,unhandledCount:3,unanalyzedItemCount:0 };

for (const width of [1366,390]) test(`Overall shows independent deduplicated counts at ${width}px`, async ({page}) => {
  await page.setViewportSize({width,height:1000});
  const errors:string[]=[]; page.on('pageerror',e=>errors.push(e.message));
  const {writes}=await prepare(page);
  const requests:URL[]=[];
  await page.route('**/api/v1/spc/overall?**',route=>{
    const u=new URL(route.request().url()); requests.push(u);
    return route.fulfill({json:{success:true,data:{startDate:u.searchParams.get('startDate'),endDate:u.searchParams.get('endDate'),summary}}});
  });
  await page.reload();
  await expect(page.getByTestId('overall-items')).toContainText('10 項');
  await expect(page.getByTestId('overall-items')).toContainText('登錄 12 項，其中停用 2 項');
  await expect(page.getByTestId('overall-ooc')).toContainText('5 件');
  await expect(page.getByTestId('overall-ooc')).toContainText('涉及 2 個管制項目');
  await expect(page.getByTestId('overall-oos')).toContainText('1 件');
  await expect(page.getByTestId('overall-handled')).toContainText('2 件');
  await expect(page.getByTestId('overall-unhandled')).toContainText('3 件');
  await expect(page.getByTestId('overall-total')).toContainText('5 件');
  expect(Object.fromEntries(requests[0].searchParams)).toEqual({startDate:'2026-09-27',endDate:'2026-10-26'});
  await page.getByTestId('overall-start').fill('2026-10-01');
  await page.getByTestId('overall-end').fill('2026-10-28');
  await page.getByTestId('overall-query').click();
  await expect(page.getByTestId('overall-period')).toHaveText('2026-10-01 ～ 2026-10-28');
  const calls=requests.length;
  await page.getByTestId('chart-dimension-CHEM').click();
  expect(requests.length).toBe(calls);
  await page.getByTestId('locale-select').selectOption('en-US');
  await expect(page.getByTestId('overall-ooc')).toContainText('Affects 2 control items');
  await expect(page.getByTestId('overall-total')).toHaveText('Distinct abnormal cases: 5');
  await expect(page.getByTestId('overall-start')).toHaveValue('2026-10-01');
  expect(requests.length).toBe(calls);
  expect(await page.getByTestId('overall').evaluate(el=>el.scrollWidth<=el.clientWidth)).toBe(true);
  expect((await page.getByTestId('overall-start').boundingBox())!.width).toBeGreaterThan(140);
  await page.evaluate(()=>window.scrollTo(0,0));
  await page.screenshot({path:`../../.codex-tmp/overall-${width}.png`,fullPage:true});
  expect(writes).toEqual([]);expect(errors).toEqual([]);
});

test('Overall validates dates, shows read failures without zero counts, and retries empty and partial results',async({page})=>{
  await prepare(page);
  let fail=false, partial=false, calls=0;
  await page.route('**/api/v1/spc/overall?**',route=>{
    calls++;
    if(fail)return route.fulfill({status:503,json:{message:'總覽讀取失敗'}});
    return route.fulfill({json:{success:true,data:{startDate:'2026-10-01',endDate:'2026-10-28',summary:{...summary,
      oocItemCount:0,oocCount:0,oosItemCount:0,oosCount:0,abnormalCount:0,handledCount:0,unhandledCount:0,unanalyzedItemCount:partial?1:0}}}});
  });
  await page.reload();
  await expect(page.getByTestId('overall-total')).toContainText('0 件');
  await page.getByTestId('overall-start').fill('2026-10-29');
  const before=calls;
  await page.getByTestId('overall-query').click();
  await expect(page.getByTestId('overall').getByRole('alert')).toContainText('起日不可晚於迄日');
  expect(calls).toBe(before);
  await page.getByTestId('overall-start').fill('2026-01-01');
  await page.getByTestId('overall-query').click();expect(calls).toBe(before);
  await page.getByTestId('overall-start').fill('2026-10-01');
  fail=true;await page.getByTestId('overall-query').click();
  await expect(page.getByTestId('overall').getByRole('alert')).toContainText('總覽讀取失敗');
  await expect(page.getByTestId('overall-total')).toHaveCount(0);
  fail=false;partial=true;await page.getByTestId('overall-query').click();
  await expect(page.getByTestId('overall').getByRole('status')).toContainText('沒有可用圖表');
  await expect(page.getByTestId('overall-total')).toContainText('0 件');
});
