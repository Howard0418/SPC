import {test,expect,Page} from '@playwright/test';
import {prepare} from './fixtures/chart-query';
import fs from 'node:fs';
const catalog=(locale:string)=>JSON.parse(fs.readFileSync(new URL(`../src/i18n/api/${locale}.json`,import.meta.url),'utf8')).spc.api;
async function inspect(page:Page,path:string){
 if(process.env.SPC_I18N_TEST_URL)return null;
 return page.evaluate(async path=>{
  const source=await(await fetch(`/src/views/${path==='/spc'?'SpcChartView':'TrendChartView'}.vue`)).text(),module=source.match(/from ["']([^"']*echarts[^"']*)["']/)?.[1],ec=await import(module!);
  const instance=ec.getInstanceByDom(document.querySelector(path==='/spc'?'[data-testid="primary-spc-chart"]':'[data-testid="chemical-trend-chart"]')),option=instance.getOption();return {id:instance.id,series:option.series,zoom:option.dataZoom};
 },path);
}
for(const width of [1280,390])for(const path of ['/spc','/trend-chart'])for(const kind of ['known','invalid'])test(`${width} ${path} ${kind}: notes follow locale while chart data and zoom stay unchanged`,async({page})=>{
 await page.setViewportSize({width,height:900});
 const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));
 const fixture=await prepare(page,path,true);fixture.state.notes=kind;await page.goto(fixture.url);
 const canvas=page.getByTestId(path==='/spc'?'primary-spc-chart':'chemical-trend-chart').locator('canvas').first();await expect(canvas).toBeVisible();
 await expect(page.getByTestId('normality-unavailable')).toBeVisible();const handle=await canvas.elementHandle(),before=await inspect(page,path),count=fixture.reads.length;
 for(const locale of ['en-US','zh-CN','zh-TW']){
  await page.getByTestId('locale-select').selectOption(locale);const strings=catalog(locale);
  await expect(page.getByTestId('normality-unavailable')).toHaveText(kind==='known'?strings.normalityNoVariation:'原零變異 <b>@:key {field}</b>');
  await expect(page.locator('main')).toContainText(kind==='known'?strings.normalityAndersonDarlingNote:'原方法 <b>@:key {field}</b>');
  if(path==='/spc')await expect(page.locator('main')).toContainText(kind==='known'?strings.chartAllSubgroupsExcluded:'原子組備註');
  expect(fixture.reads.length).toBe(count);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);
  const after=await inspect(page,path);if(before&&after){expect(after.id).toBe(before.id);expect(after.series).toEqual(before.series);expect(after.zoom).toEqual(before.zoom)}
  await page.getByTestId('normality-histogram').locator('canvas').first().hover();await expect(page.getByTestId('normality-method-tooltip')).toHaveText(kind==='known'?strings.normalityAndersonDarlingNote:'原方法 <b>@:key {field}</b>');
  await expect(page.getByTestId('normality-method-tooltip').locator('b')).toHaveCount(0);
  const box=await page.getByTestId('normality-method-tooltip').boundingBox();expect(box!.x).toBeGreaterThanOrEqual(0);expect(box!.x+box!.width).toBeLessThanOrEqual(width);
  if(width===390&&kind==='known'&&locale==='en-US')await page.screenshot({path:`../../.codex-tmp/i18n-task08c6/${path.slice(1)}-mobile-tooltip-en.png`});
 }
 expect(fixture.writes).toEqual([]);expect(errors).toEqual([]);
});
