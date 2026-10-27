import { test, expect, Page } from '@playwright/test';

const fixtureName = '原始名稱 <b>@:key {name}</b>';
const part = {id:11,partNo:'P-原始',partName:fixtureName,specification:'0.2 μm',customer:'原客戶',isEnabled:true};
const characteristic = {id:21,characteristicCode:'C-RAW',characteristicName:fixtureName,characteristicNameEn:'Original English',dataCategory:'Variable',inputMode:'FORMULA',valueLabel:'濃度',decimalPlaces:3,isSpcEnabled:true,isEnabled:true};
const errors = new WeakMap<Page,string[]>();
test.beforeEach(({page})=>{const list:string[]=[];errors.set(page,list);page.on('pageerror',e=>list.push(e.message))});
test.afterEach(({page})=>expect(errors.get(page)).toEqual([]));

async function prepare(page: Page, endpoint: 'parts'|'characteristics', role='Editor', empty=false) {
 await page.addInitScript(({role})=>{
  localStorage.setItem('mes_spc_token','mock-ui-only-token');
  localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role,username:'i18n-master-fixture'}));
 },{role});
 const writes: {method:string,path:string,body:any}[]=[];
 const reads:string[]=[];const state={reject:false};
 await page.route('**/*',async route=>{
  const url=new URL(route.request().url());
  if(url.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!url.pathname.startsWith('/api/'))return route.continue();
  const method=route.request().method();
  if(method!=='GET'){
   expect(url.pathname).toMatch(new RegExp(`^/api/${endpoint}(/\\d+)?$`));
   const body=method==='DELETE'?null:route.request().postDataJSON();writes.push({method,path:url.pathname,body});
   if(state.reject)return route.fulfill({status:409,json:{message:'原始 API 衝突訊息'}});
   const id=method==='POST' ? (endpoint==='parts'?31:41) : (endpoint==='parts'?11:21);
   return route.fulfill({status:method==='DELETE'?204:200,...(method==='DELETE'?{}:{json:{id,...body}})});
  }
  reads.push(url.pathname);
  const rows=empty?[]:endpoint==='parts'?[part,{...part,id:12,partNo:'P-OFF',partName:'inactive',isEnabled:false}]:[characteristic,{...characteristic,id:22,characteristicName:'inactive',dataCategory:'Attribute',isEnabled:false}];
  return route.fulfill({json:url.pathname.endsWith('/version')?{version:'0.1.72',environment:'test'}:url.pathname===`/api/${endpoint}`?rows:[]});
 });
 await page.goto(`/${endpoint}?keep=42`);
 return {writes,state,reads};
}

test('parts page switches its own title',async({page})=>{
 await prepare(page,'parts');await page.getByTestId('locale-select').selectOption('en-US');
 await expect(page.getByRole('heading',{name:'Product part master',exact:true})).toBeVisible();
});

const titles={parts:{'zh-TW':'產品料號主檔維護','zh-CN':'产品料号主档维护','en-US':'Product part master'},characteristics:{'zh-TW':'品質檢驗特性主檔維護','zh-CN':'质量检验特性主档维护','en-US':'Quality characteristic master'}};
for(const endpoint of ['parts','characteristics'] as const) {
 for(const width of [1920,1366,1280,390])for(const language of ['zh-TW','zh-CN','en-US'] as const) {
  test(`${endpoint} ${width} ${language}: labels, raw data and drawer fit`,async({page})=>{
   await page.setViewportSize({width,height:900});await page.emulateMedia({colorScheme:width===390?'dark':'light'});
   await prepare(page,endpoint);await page.getByTestId('locale-select').selectOption(language);
   await expect(page.getByRole('heading',{name:titles[endpoint][language],exact:true})).toBeVisible();
   await expect(page.locator('tbody')).toContainText(endpoint==='characteristics'&&language==='en-US'?'Original English':fixtureName);await expect(page.locator('tbody b')).toHaveCount(0);
   await page.getByRole('button',{name:/展開或收合|展开或收起|Expand or collapse/}).click();
   await expect(page.getByRole('button',{name:/展開或收合|展开或收起|Expand or collapse/})).toHaveAttribute('aria-expanded','true');
   await page.getByTestId('master-add').click();const dialog=page.getByRole('dialog');
   await expect(dialog).toBeVisible();await expect(dialog.getByTestId('locale-select')).toBeInViewport();
   expect(await dialog.evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual(Math.min(width,448));
   const save=dialog.locator('button[type="submit"]');await save.scrollIntoViewIfNeeded();await expect(save).toBeInViewport();
   await dialog.getByRole('button',{name:/^關閉$|^关闭$|^Close$/}).click();await expect(dialog).toHaveCount(0);
  });
 }
 for(const language of ['zh-TW','zh-CN','en-US']) {
  test(`${endpoint} ${language}: CRUD keeps original data contract`,async({page})=>{
   const {writes}=await prepare(page,endpoint);await page.getByTestId('locale-select').selectOption(language);
   await page.getByTestId('master-edit').first().click();let dialog=page.getByRole('dialog');
   if(endpoint==='parts'){
    await expect(dialog.getByTestId('partName')).toHaveValue(fixtureName);await dialog.getByTestId('partName').fill('未存名稱 <b>{name}@</b>');
   }else{
    await expect(dialog.getByTestId('valueLabel')).toHaveValue('濃度');
    await dialog.getByTestId('dataCategory').selectOption('Attribute');await dialog.getByTestId('inputMode').selectOption('RECORD_ONLY');await dialog.getByTestId('decimalPlaces').fill('0');
   }
   await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
   expect(writes[0]).toEqual(endpoint==='parts'?{method:'PUT',path:'/api/parts/11',body:{partNo:part.partNo,partName:'未存名稱 <b>{name}@</b>',specification:part.specification,customer:part.customer,isEnabled:true}}:{method:'PUT',path:'/api/characteristics/21',body:{characteristicCode:'C-RAW',characteristicName:fixtureName,characteristicNameEn:'Original English',dataCategory:'Attribute',inputMode:'RECORD_ONLY',valueLabel:'濃度',decimalPlaces:0,isSpcEnabled:true,isEnabled:true,defaultChartTypeId:null}});
   // An existing success message also follows a later language switch.
   await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText(endpoint==='parts'?'Part updated: P-原始':'Characteristic updated: Original English');
   await page.getByTestId('master-add').click();dialog=page.getByRole('dialog');await dialog.getByTestId('locale-select').selectOption(language);
   if(endpoint==='parts'){
    await dialog.getByTestId('partNo').fill('新增 RAW');await dialog.getByTestId('partName').fill('保留產品原文');
   }else{
    await dialog.getByTestId('characteristicName').fill('保留特性原文');await dialog.getByTestId('decimalPlaces').fill('8');
   }
   await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
   expect(writes[1]).toEqual(endpoint==='parts'?{method:'POST',path:'/api/parts',body:{partNo:'新增 RAW',partName:'保留產品原文',specification:'',customer:'',isEnabled:true}}:{method:'POST',path:'/api/characteristics',body:{characteristicCode:'',characteristicName:'保留特性原文',characteristicNameEn:'',dataCategory:'Variable',inputMode:'DIRECT',valueLabel:'濃度',decimalPlaces:8,isSpcEnabled:true,isEnabled:true,defaultChartTypeId:null}});
   let confirmation='';page.once('dialog',async prompt=>{confirmation=prompt.message();await prompt.dismiss()});await page.getByTestId('master-delete').first().click();expect(writes).toHaveLength(2);
   expect(confirmation).toContain(endpoint==='parts'?part.partNo:language==='en-US'?'Original English':fixtureName);
   page.once('dialog',async prompt=>prompt.accept());await page.getByTestId('master-delete').first().click();await expect.poll(()=>writes.length).toBe(3);
   expect(writes[2]).toEqual({method:'DELETE',path:`/api/${endpoint}/${endpoint==='parts'?11:21}`,body:null});
  });
 }
 test(`${endpoint}: draft, validation and rejected write survive language change`,async({page})=>{
  const {writes,state}=await prepare(page,endpoint);await page.getByTestId('master-add').click();const dialog=page.getByRole('dialog');
  const field=dialog.getByTestId(endpoint==='parts'?'partName':'characteristicName');await field.fill(' ');
  if(endpoint==='parts')await dialog.getByTestId('partNo').fill('DRAFT');
  await dialog.locator('button[type="submit"]').click();expect(writes).toHaveLength(0);
  await dialog.getByTestId('locale-select').selectOption('en-US');await expect(dialog).toContainText(endpoint==='parts'?'Part number and part name are required.':'The Chinese characteristic name is required.');
  await expect(field).toHaveValue(' ');await field.fill('草稿 RAW');await field.evaluate(el=>{(window as any).__draft=el});
  const values=await dialog.locator('input,textarea,select').evaluateAll(nodes=>nodes.map(el=>(el as HTMLInputElement).value));
  await dialog.getByTestId('locale-select').selectOption('zh-CN');
  expect(await field.evaluate(el=>el===(window as any).__draft)).toBe(true);
  const after=await dialog.locator('input,textarea,select').evaluateAll(nodes=>nodes.map(el=>(el as HTMLInputElement).value));
  expect(after.slice(1)).toEqual(values.slice(1));await expect(page).toHaveURL(new RegExp(`/${endpoint}\\?keep=42$`));
  state.reject=true;await dialog.locator('button[type="submit"]').click();await expect(dialog).toContainText('原始 API 衝突訊息');await expect(field).toHaveValue('草稿 RAW');
  await dialog.getByTestId('locale-select').selectOption('en-US');await expect(dialog).toContainText('原始 API 衝突訊息');expect(writes).toHaveLength(1);
 });
 test(`${endpoint}: filters and guide state retained, zero count and empty list`,async({page})=>{
  await prepare(page,endpoint);await page.getByTestId('master-search').fill('inactive');
  await page.getByRole('button',{name:'已停用',exact:true}).click();
  await page.getByRole('button',{name:'展開或收合模組指南'}).click();
  await page.getByTestId('locale-select').selectOption('en-US');await expect(page.getByTestId('master-search')).toHaveValue('inactive');
  await expect(page.locator('tbody tr')).toHaveCount(1);await expect(page.getByRole('button',{name:'Expand or collapse module guide'})).toHaveAttribute('aria-expanded','true');
  await page.getByTestId('master-search').fill('no-match');await expect(page.locator('main')).toContainText('Showing 0 to 0;');
 });
 test(`${endpoint}: empty response is translated`,async({page})=>{
  await prepare(page,endpoint,'Editor',true);await page.getByTestId('locale-select').selectOption('en-US');
  await expect(page.locator('tbody')).toContainText(endpoint==='parts'?'No matching parts found':'No matching quality characteristics found');
 });
 test(`${endpoint}: Viewer direct navigation still rejected`,async({page})=>{
  await prepare(page,endpoint,'Viewer');await expect(page).not.toHaveURL(new RegExp(`/${endpoint}\\?keep=42$`));
  await expect(page.getByTestId('master-add')).toHaveCount(0);
 });
}

for(const width of [390,1280])test(`characteristic name switch ${width}: search and bilingual draft stay intact`,async({page})=>{
 await page.setViewportSize({width,height:900});const {writes,reads}=await prepare(page,'characteristics');const count=reads.length;
 for(const query of ['C-RAW','Original English',fixtureName]){await page.getByTestId('master-search').fill(query);await expect(page.getByTestId('characteristic-display-name')).toHaveCount(query===fixtureName?1:2)}
 const handle=await page.getByTestId('master-search').elementHandle();
 for(const [locale,name] of [['en-US','Original English'],['zh-CN',fixtureName],['zh-TW',fixtureName]]){await page.getByTestId('locale-select').selectOption(locale);await expect(page.getByTestId('characteristic-display-name')).toContainText(name);expect(await handle!.evaluate(n=>n.isConnected)).toBe(true);expect(reads.length).toBe(count)}
 await page.getByTestId('master-edit').first().click();const d=page.getByRole('dialog');await d.getByTestId('characteristicNameEn').fill('Proportion');
 await d.getByTestId('locale-select').selectOption('en-US');await expect(d.getByTestId('characteristicName')).toHaveValue(fixtureName);await expect(d.getByTestId('characteristicNameEn')).toHaveValue('Proportion');await expect(d.getByTestId('valueLabel')).toHaveValue('濃度');
 expect(writes).toEqual([]);await expect(d.locator('b')).toHaveCount(0);const box=await d.boundingBox();expect(box!.width).toBeLessThanOrEqual(width);await expect.poll(async()=>{const b=await d.boundingBox();return b!.x+b!.width}).toBeLessThanOrEqual(width+0.5);await page.screenshot({path:`../../.codex-tmp/i18n-char-a/master-${width}-en.png`});
});
