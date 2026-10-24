import {test,expect,Page} from '@playwright/test';
const raw='原名 <b>@:key {name}</b>';
const formula='{\n "UclFormula": "Xbar + A2 * Rbar", "原值": "@:key {name}"\n}';
const group={id:41,groupCode:'CHEM',groupName:raw,isEnabled:true};
const item={id:11,chartGroupId:41,chartTypeCode:'XBAR_R',chartTypeName:raw,dataCategory:'Variable',requiredSampleSize:5,ruleGroupId:61,description:'原說明',formulaConfigJson:formula,isEnabled:true};
const rules=Array.from({length:8},(_,i)=>({id:i+1,ruleCode:`Rule${i+1}`,ruleName:`原規則${i+1}`,description:'原規則說明',priority:i+1,isSelected:true}));
const errors=new WeakMap<Page,string[]>();
test.beforeEach(({page})=>{const list:string[]=[];errors.set(page,list);page.on('pageerror',e=>list.push(e.message))});
test.afterEach(({page})=>expect(errors.get(page)).toEqual([]));
async function prepare(page:Page,role='Editor'){
 await page.addInitScript(role=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role,username:'chart-types-fixture'}))},role);
 const writes:any[]=[];const reads:string[]=[];const state={reject:false,empty:false,failRules:false};
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();const method=route.request().method();
  if(method!=='GET'){
   expect(u.pathname).toMatch(/^\/api\/control-chart-types(\/\d+)?$/);
   const body=method==='DELETE'?null:route.request().postDataJSON();writes.push({method,path:u.pathname,body});
   if(state.reject)return route.fulfill({status:409,json:{message:'原 API 衝突訊息 <b>@:key {name}</b>'}});
   return route.fulfill({status:method==='DELETE'?204:200,...(method==='DELETE'?{}:{json:{...body,id:method==='POST'?91:Number(u.pathname.split('/').at(-1))}})});
  }
  reads.push(u.pathname+u.search);let data:any=[];
  if(u.pathname.endsWith('/version'))data={version:'0.1.72',environment:'test'};
  else if(!state.empty){
   if(u.pathname==='/api/control-chart-groups')data=[group];
   if(u.pathname==='/api/spc-rule-groups')data=[{id:61,ruleGroupCode:'WE',ruleGroupName:raw}];
   if(u.pathname==='/api/spc-rules')data=rules;
   if(u.pathname==='/api/control-chart-types')data=[item,{...item,id:12,chartTypeCode:'I-MR',chartGroupId:null,ruleGroupId:null},{...item,id:13,chartTypeCode:'ORPHAN',chartGroupId:99,ruleGroupId:null}];
   if(/\/control-chart-types\/\d+\/rules$/.test(u.pathname)){
    if(state.failRules)return route.fulfill({status:500,json:{message:'原 API 規則錯誤'}});
    data={rules};
   }
  }
  return route.fulfill({json:data});
 });
 await page.goto('/tests/fixtures/chart-types.html?tab=types&keep=42');return {writes,reads,state};
}
for(const width of [1920,1366,1280,390])for(const locale of ['zh-TW','zh-CN','en-US'])test(`types ${width} ${locale}: labels, raw values and reachable editor`,async({page})=>{
 await page.setViewportSize({width,height:900});const {writes}=await prepare(page);await page.getByTestId('locale-select').selectOption(locale);
 await expect(page.getByTestId('type-add')).toHaveText(locale==='en-US'?'Add chart type':locale==='zh-CN'?'新增小分类':'新增小分類');
 await page.getByTestId('type-edit').first().click();const dialog=page.getByRole('dialog');await dialog.getByTestId('locale-select').selectOption(locale);
 await expect(dialog.getByTestId('type-chartTypeName')).toHaveValue(raw);await expect(dialog.getByTestId('type-formulaConfigJson')).toHaveValue(formula);
 await expect(dialog.locator('input[disabled]')).toHaveCount(8);expect(await dialog.locator('input[disabled]:checked').count()).toBe(8);
 await dialog.getByTestId('type-chartTypeCode').click();expect(await dialog.evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual(width);
 await dialog.locator('button[type="submit"]').scrollIntoViewIfNeeded();await expect(dialog.locator('button[type="submit"]')).toBeInViewport();expect(writes).toEqual([]);
 if(width===390&&locale==='en-US')await page.screenshot({path:'../../.codex-tmp/i18n-task05c1/mobile-en.png'});
});
for(const locale of ['zh-TW','zh-CN','en-US']){
 test(`edit ${locale}: switching preserves draft, DOM, filters and exact DTO`,async({page})=>{
  const {writes,reads}=await prepare(page);await page.getByTestId('type-search').fill('XBAR');await page.getByTestId('type-group-filter').selectOption('41');await page.getByTestId('type-edit').click();const dialog=page.getByRole('dialog');
  await dialog.getByTestId('type-chartTypeName').fill('未存 '+raw);await dialog.getByTestId('type-requiredSampleSize').fill('50');await dialog.getByTestId('type-dataCategory').selectOption('Attribute');
  const handle=await dialog.getByTestId('type-chartTypeName').elementHandle();await expect(dialog.locator('input[disabled]')).toHaveCount(8);const before=reads.length;const url=page.url();
  await dialog.getByTestId('locale-select').selectOption(locale);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(before);expect(page.url()).toBe(url);expect(writes).toHaveLength(0);
  await expect(page.getByTestId('type-search')).toHaveValue('XBAR');await expect(page.getByTestId('type-group-filter')).toHaveValue('41');
  await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  const {id,...body}=item;expect(writes).toEqual([{method:'PUT',path:'/api/control-chart-types/11',body:{...body,chartTypeName:'未存 '+raw,requiredSampleSize:50,dataCategory:'Attribute'}}]);
  await expect(page.locator('main b')).toHaveCount(0);
 });
 test(`create ${locale}: required, invalid JSON, API rejection and unchanged default formulas`,async({page})=>{
  const {writes,state}=await prepare(page);await page.getByTestId('type-add').click();const dialog=page.getByRole('dialog');
  await dialog.getByTestId('type-chartTypeCode').fill(' ');await dialog.getByTestId('type-chartTypeName').fill(' ');await dialog.locator('button[type="submit"]').click();expect(writes).toHaveLength(0);
  await dialog.getByTestId('locale-select').selectOption('en-US');await expect(dialog).toContainText('Parent category, chart code and name are required.');
  await dialog.getByTestId('type-chartTypeCode').fill('RAW');await dialog.getByTestId('type-chartTypeName').fill(raw);
  const defaults=await dialog.getByTestId('type-formulaConfigJson').inputValue();expect(defaults).toBe('{\n  "UclFormula": "Xbar + A2 * Rbar",\n  "LclFormula": "Xbar - A2 * Rbar"\n}');
  await dialog.getByTestId('type-formulaConfigJson').fill('{bad');await dialog.locator('button[type="submit"]').click();await expect(dialog).toContainText('Formula configuration JSON is invalid.');expect(writes).toHaveLength(0);
  await dialog.getByTestId('locale-select').selectOption(locale);await expect(dialog.getByTestId('type-formulaConfigJson')).toHaveValue('{bad');
  await dialog.getByTestId('type-formulaConfigJson').fill(defaults);await dialog.getByTestId('type-requiredSampleSize').fill('1');state.reject=true;await dialog.locator('button[type="submit"]').click();await expect(dialog).toContainText('原 API 衝突訊息 <b>@:key {name}</b>');
  await dialog.getByTestId('locale-select').selectOption(locale);await expect(dialog).toContainText('原 API 衝突訊息');state.reject=false;await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  expect(writes[1]).toEqual({method:'POST',path:'/api/control-chart-types',body:{chartGroupId:41,chartTypeCode:'RAW',chartTypeName:raw,dataCategory:'Variable',requiredSampleSize:1,ruleGroupId:null,description:'',formulaConfigJson:defaults,isEnabled:true}});
 });
 test(`delete ${locale}: original I_MR confirmation, cancel and delete`,async({page})=>{
  const {writes}=await prepare(page);await page.getByTestId('locale-select').selectOption(locale);await page.getByTestId('type-search').fill('I-MR');let prompt='';page.once('dialog',async d=>{prompt=d.message();await d.dismiss()});await page.getByTestId('type-delete').click();expect(prompt).toContain('I_MR');expect(prompt).not.toContain(raw);expect(writes).toHaveLength(0);
  page.once('dialog',d=>d.accept());await page.getByTestId('type-delete').click();await expect.poll(()=>writes.length).toBe(1);expect(writes[0]).toEqual({method:'DELETE',path:'/api/control-chart-types/12',body:null});
 });
}
test('missing parent display changes while original search semantics stay stable',async({page})=>{
 const {reads,writes}=await prepare(page);await page.getByTestId('type-search').fill('未指定大類別');await expect(page.getByTestId('type-edit')).toHaveCount(1);const before=reads.length;
 await page.getByTestId('locale-select').selectOption('en-US');await expect(page.getByTestId('type-edit')).toHaveCount(1);await expect(page.locator('main')).toContainText('No parent category');expect(reads.length).toBe(before);
 await page.getByTestId('type-search').fill('大類別 #99');await expect(page.getByTestId('type-edit')).toHaveCount(1);await expect(page.locator('main')).toContainText('Category #99');expect(writes).toEqual([]);
});
test('empty response and rule-load failure retain translated interface and API text',async({page})=>{
 const {state,writes}=await prepare(page);state.failRules=true;await page.getByTestId('type-edit').first().click();const dialog=page.getByRole('dialog');await expect(dialog).toContainText('原 API 規則錯誤');await dialog.getByTestId('locale-select').selectOption('en-US');await expect(dialog).toContainText('Failed to load chart type rules: 原 API 規則錯誤');
 await dialog.getByRole('button',{name:'Close',exact:true}).click();state.empty=true;await page.reload();await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText('No matching chart types');await page.getByTestId('type-add').click();await expect(page.getByRole('dialog')).toContainText('No control rules are available');expect(writes).toEqual([]);
});
