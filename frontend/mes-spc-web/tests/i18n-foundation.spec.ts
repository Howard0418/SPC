import {test,expect,Page} from '@playwright/test';
const raw='原名 <b>@:key {name}</b>';
const process={id:11,processCode:'P-RAW',processName:raw,processNameEn:'Original',sequenceNo:0,description:'原說明',isEnabled:true};
const machine={id:21,processId:11,machineCode:'MC-RAW',machineName:raw,location:'原位置',status:'IDLE',isEnabled:true};
const tank={id:31,lineId:11,tankCode:'MC-RAW-T1',tankName:raw,tankNameEn:'Original tank',sequenceNo:0,isActive:true};
const group={id:41,groupCode:'CHEM',groupName:raw,description:'原說明',groupType:'CONTROL_CHART',businessScopeCode:'CHEM',requiresPart:false,requiresMachine:true,requiresTank:true,isEnabled:true};
const rule={id:51,ruleGroupId:61,ruleCode:'Rule1',ruleName:raw,ruleConfigJson:'{"sigma":3,"text":"原值"}',priority:100,isEnabled:true};
const errors=new WeakMap<Page,string[]>();
test.beforeEach(({page})=>{const list:string[]=[];errors.set(page,list);page.on('pageerror',e=>list.push(e.message))});
test.afterEach(({page})=>expect(errors.get(page)).toEqual([]));
async function prepare(page:Page,path:string,role='Editor'){
 await page.addInitScript(({role})=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role,username:'foundation-fixture'}))},{role});
 const writes:any[]=[];const reads:string[]=[];const state={reject:false,empty:false};
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();const method=route.request().method();
  if(method!=='GET'){
   expect(u.pathname).toMatch(/^\/api\/(processes|machines|control-chart-groups|spc-rules|v1\/traceability-master\/tanks)(\/\d+)?$/);
   const body=method==='DELETE'?null:route.request().postDataJSON();writes.push({method,path:u.pathname,body});
   if(state.reject)return route.fulfill({status:409,json:{message:'原 API 衝突訊息',relatedItems:['原關聯資料']}});
   return route.fulfill({status:method==='DELETE'?204:200,...(method==='DELETE'?{}:{json:{...body,id:method==='POST'?91:Number(u.pathname.split('/').at(-1)),tankCode:body.tankCode||'AUTO-T1'}})});
  }
  reads.push(u.pathname+u.search);let data:any=[];
  if(u.pathname.endsWith('/version'))data={version:'0.1.72',environment:'test'};
  else if(!state.empty){
   if(u.pathname==='/api/processes')data=[process];
   else if(u.pathname==='/api/machines')data=[machine];
   else if(u.pathname==='/api/machines/21/tanks')data=[tank];
   else if(u.pathname.endsWith('/measurements'))data={process,total:1,rows:[{id:71,measuredAt:'2026-10-24T12:00:00',characteristicId:3,value:0.2,inspectedQty:10,defectQty:2,defectCount:3}]};
   else if(u.pathname==='/api/control-chart-groups')data=[group];
   else if(u.pathname==='/api/spc-rule-groups')data=[{id:61,ruleGroupCode:'WE',ruleGroupName:raw}];
   else if(u.pathname==='/api/spc-rules')data=[rule];
   else if(u.pathname.endsWith('/traceability-master/lines'))data=[{id:11,lineCode:'P-RAW',lineName:raw,lineNameEn:'Original',isActive:true}];
   else if(u.pathname.endsWith('/traceability-master/tanks'))data=[tank];
  }
  return route.fulfill({json:data});
 });
 await page.goto(`/${path}?keep=42`);return {writes,reads,state};
}
const titles:any={processes:{'zh-TW':'工站製程配置中心','zh-CN':'工站制程配置中心','en-US':'Process configuration'},'traceability-master':{'zh-TW':'線別槽體設定','zh-CN':'线别槽体设置','en-US':'Line and tank settings'},'control-chart-groups':{'zh-TW':'管制圖大類別維護','zh-CN':'管制图大类别维护','en-US':'Control chart categories'},'spc-rule-groups':{'zh-TW':'SPC 八大管制規則維護','zh-CN':'SPC 八大管制规则维护','en-US':'SPC eight control rules'}};
for(const path of Object.keys(titles))for(const width of [1920,1366,1280,390])for(const locale of ['zh-TW','zh-CN','en-US']){
 test(`${path} ${width} ${locale}: labels and accessible editor`,async({page})=>{
  await page.setViewportSize({width,height:900});await prepare(page,path);
  await page.getByTestId('locale-select').selectOption(locale);
  await expect(page.locator('main').getByRole('heading',{name:titles[path][locale],exact:true})).toBeVisible();
  if(path==='traceability-master'){
   await page.getByTestId('line-select').click();await page.getByTestId('tank-edit').click();
   await expect(page.getByTestId('tank-tankName')).toHaveValue(raw);
   if(width===390){await page.getByTestId('tank-tankName').click();expect((await page.getByTestId('tank-tankName').boundingBox())!.width).toBeGreaterThan(100);await expect(page.getByTestId('tank-save')).toBeInViewport();}
  }else{
   await page.getByTestId(path==='processes'?'process-edit':path==='control-chart-groups'?'group-edit':'rule-edit').first().click();
   const dialog=page.getByRole('dialog');await dialog.getByTestId('locale-select').selectOption(locale);
   await dialog.locator('input:not([readonly]):not([disabled])').first().click();
   expect(await dialog.evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual(width);
   await dialog.locator('button[type="submit"]').scrollIntoViewIfNeeded();await expect(dialog.locator('button[type="submit"]')).toBeInViewport();
  }
 });
}
for(const locale of ['zh-TW','zh-CN','en-US']){
 test(`process and machine create ${locale}: validation and nested tanks`,async({page})=>{
  const {writes}=await prepare(page,'processes');await page.getByTestId('locale-select').selectOption(locale);
  await page.getByTestId('process-add').click();let dialog=page.getByRole('dialog');await dialog.getByTestId('form-processName').fill(' ');await dialog.locator('button[type="submit"]').click();expect(writes).toHaveLength(0);
  await dialog.getByTestId('locale-select').selectOption('en-US');await expect(dialog).toContainText('The Chinese process name is required.');
  await dialog.getByTestId('form-processName').fill('新製程');await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  expect(writes[0]).toEqual({method:'POST',path:'/api/processes',body:{processCode:'',processName:'新製程',processNameEn:'',sequenceNo:0,description:'',isEnabled:true}});
  await page.getByTestId('machine-add').click();dialog=page.getByRole('dialog');await dialog.getByTestId('machineForm-machineCode').fill('MC-NEW');await dialog.getByTestId('machineForm-machineName').fill('新機台');
  await page.getByTestId('machine-tank-add').click();await dialog.getByTestId('tank-tankName').fill(' ');await dialog.locator('button[type="submit"]').click();expect(writes).toHaveLength(1);await expect(dialog).toContainText('Tank name must not be blank.');
  await dialog.getByTestId('tank-tankName').fill(' 槽原名 ');await dialog.getByTestId('locale-select').selectOption(locale);await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  expect(writes[1]).toEqual({method:'POST',path:'/api/machines',body:{machineCode:'MC-NEW',machineName:'新機台',location:'',status:'IDLE',isEnabled:true,tanks:[{id:null,tankCode:'',tankName:'槽原名',tankNameEn:null,sequenceNo:0,isActive:true}],processId:91}});
 });
 test(`group create/delete ${locale}: no translated enum or flags`,async({page})=>{
  const {writes}=await prepare(page,'control-chart-groups');await page.getByTestId('locale-select').selectOption(locale);await page.getByTestId('group-add').click();const dialog=page.getByRole('dialog');
  await dialog.getByTestId('groupsForm-groupCode').fill('RAW');await dialog.getByTestId('groupsForm-groupName').fill('原類別');await dialog.getByTestId('groupsForm-businessScopeCode').fill('RAW');
  await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  expect(writes[0]).toEqual({method:'POST',path:'/api/control-chart-groups',body:{groupCode:'RAW',groupName:'原類別',description:'',groupType:'CONTROL_CHART',businessScopeCode:'RAW',requiresPart:false,requiresMachine:false,requiresTank:false,isEnabled:true}});
  page.once('dialog',d=>d.accept());await page.getByTestId('group-delete').last().click();await expect.poll(()=>writes.length).toBe(2);expect(writes[1]).toEqual({method:'DELETE',path:'/api/control-chart-groups/91',body:null});
 });
 test(`tank create/delete ${locale}: parent ID and inline state unchanged`,async({page})=>{
  const {writes}=await prepare(page,'traceability-master');await page.getByTestId('line-select').click();await page.getByTestId('locale-select').selectOption(locale);await page.getByTestId('tank-add').click();
  await page.getByTestId('tank-tankName').fill('原槽名');await page.getByTestId('tank-save').click();await expect(page.getByTestId('tank-save')).toHaveCount(0);
  expect(writes[0]).toEqual({method:'POST',path:'/api/v1/traceability-master/tanks',body:{tankCode:'',tankName:'原槽名',tankNameEn:'',sequenceNo:0,isActive:true,lineId:11,id:0,_isEditing:true}});
  page.once('dialog',d=>d.accept());await page.getByTestId('tank-delete').first().click();await expect.poll(()=>writes.length).toBe(2);expect(writes[1]).toEqual({method:'DELETE',path:'/api/v1/traceability-master/tanks/91',body:null});
 });
 test(`process/machine ${locale}: original DTO and draft`,async({page})=>{
  const {writes,reads}=await prepare(page,'processes');await page.getByTestId('locale-select').selectOption(locale);
  await page.getByTestId('process-edit').click();let dialog=page.getByRole('dialog');
  await dialog.getByTestId('form-processName').fill('未存原名');const handle=await dialog.getByTestId('form-processName').elementHandle();const before=reads.length;
  await dialog.getByTestId('locale-select').selectOption('en-US');expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(before);
  await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  expect(writes[0]).toEqual({method:'PUT',path:'/api/processes/11',body:{processCode:'P-RAW',processName:'未存原名',processNameEn:'Original',sequenceNo:0,description:'原說明',isEnabled:true}});
  await page.getByTestId('machine-edit').click();dialog=page.getByRole('dialog');await expect(dialog.getByTestId('machineForm-status')).toHaveValue('IDLE');
  await dialog.getByTestId('locale-select').selectOption(locale);await expect(dialog.getByTestId('tank-tankName')).toHaveValue(raw);await expect(dialog.locator('input[readonly]').last()).toHaveValue('T1');
  await dialog.getByTestId('machineForm-status').selectOption('MAINTENANCE');await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  expect(writes[1]).toEqual({method:'PUT',path:'/api/machines/21',body:{machineCode:'MC-RAW',machineName:raw,location:'原位置',status:'MAINTENANCE',isEnabled:true,tanks:[{id:31,tankCode:'T1',tankName:raw,tankNameEn:'Original tank',sequenceNo:0,isActive:true}],processId:11}});
 });
 test(`groups ${locale}: requirements flags and category code unchanged`,async({page})=>{
  const {writes}=await prepare(page,'control-chart-groups');await page.getByTestId('locale-select').selectOption(locale);await page.getByTestId('group-edit').click();const dialog=page.getByRole('dialog');
  await dialog.getByTestId('groupsForm-groupType').selectOption('TREND_CHART');await dialog.getByTestId('groupsForm-requiresPart').check();await dialog.getByTestId('locale-select').selectOption(locale);
  await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  const {id,...body}=group;expect(writes[0]).toEqual({method:'PUT',path:'/api/control-chart-groups/41',body:{...body,groupType:'TREND_CHART',requiresPart:true}});
  let prompt='';page.once('dialog',async d=>{prompt=d.message();await d.dismiss()});await page.getByTestId('group-delete').click();expect(prompt).toContain('CHEM');expect(writes).toHaveLength(1);
 });
 test(`rules ${locale}: fixed library, code, validation and JSON`,async({page})=>{
  const {writes,state}=await prepare(page,'spc-rule-groups');await page.getByTestId('locale-select').selectOption(locale);await page.getByTestId('rule-edit').click();const dialog=page.getByRole('dialog');
  await expect(dialog.getByTestId('form-ruleCode')).toHaveAttribute('readonly','');await expect(dialog.getByTestId('form-ruleGroupId')).toBeDisabled();
  await dialog.getByTestId('form-ruleConfigJson').fill('{bad');await dialog.locator('button[type="submit"]').click();expect(writes).toHaveLength(0);
  await dialog.getByTestId('locale-select').selectOption('en-US');await expect(dialog).toContainText('Custom parameter JSON is invalid');
  await dialog.getByTestId('form-ruleConfigJson').fill(rule.ruleConfigJson);state.reject=true;await dialog.locator('button[type="submit"]').click();await expect(dialog).toContainText('原 API 衝突訊息');
  state.reject=false;await dialog.locator('button[type="submit"]').click();await expect(dialog).toHaveCount(0);
  const {id,...body}=rule;expect(writes[1]).toEqual({method:'PUT',path:'/api/spc-rules/51',body});
 });
 test(`traceability ${locale}: selection, inline draft and original payload`,async({page})=>{
  const {writes,reads}=await prepare(page,'traceability-master');await page.getByTestId('line-select').click();await page.getByTestId('tank-edit').click();
  await page.getByTestId('tank-tankName').fill('槽體原名');const before=reads.length;await page.getByTestId('locale-select').selectOption(locale);expect(reads.length).toBe(before);await expect(page.getByTestId('tank-tankName')).toHaveValue('槽體原名');
  await page.getByTestId('tank-save').click();await expect(page.getByTestId('tank-save')).toHaveCount(0);
  expect(writes[0]).toEqual({method:'PUT',path:'/api/v1/traceability-master/tanks/31',body:{...tank,tankName:'槽體原名',_isEditing:true}});
  page.once('dialog',d=>d.dismiss());await page.getByTestId('tank-delete').click();expect(writes).toHaveLength(1);
 });
}
test('process measurement query and numeric data survive language switching',async({page})=>{
 const {reads,writes}=await prepare(page,'processes');await page.getByTestId('measurements-open').click();const dialog=page.getByRole('dialog');
 await expect(dialog.locator('tbody')).toContainText('0.2');const before=reads.length;
 await dialog.getByTestId('locale-select').selectOption('en-US');expect(reads.length).toBe(before);await expect(dialog.locator('tbody')).toContainText('0.2');
 expect(reads.some(x=>x.includes('/processes/11/measurements?type=variable&take=500'))).toBe(true);
 await dialog.getByRole('button',{name:'Attribute',exact:true}).click();await expect(dialog.locator('tbody')).toContainText('Inspected 10 / nonconforming 2 / defects 3');expect(writes).toEqual([]);
});
for(const path of Object.keys(titles))test(`${path}: Viewer keeps existing route guard`,async({page})=>{
 await prepare(page,path,'Viewer');await expect(page).not.toHaveURL(new RegExp('/'+path+'\\?keep=42$'));
});
for(const path of Object.keys(titles))test(`${path}: empty response and labels`,async({page})=>{
 const {state,writes}=await prepare(page,path);state.empty=true;await page.reload();await page.getByTestId('locale-select').selectOption('en-US');
 const expected:any={processes:'No matching stations found','traceability-master':'No data','control-chart-groups':'No matching categories found','spc-rule-groups':'No matching control rules found'};
 await expect(page.locator('main')).toContainText(expected[path]);expect(writes).toEqual([]);
});
test('process deletion cancel and linked-data conflict retain original diagnostics',async({page})=>{
 const {state,writes}=await prepare(page,'processes');await page.getByTestId('locale-select').selectOption('en-US');
 page.once('dialog',d=>d.dismiss());await page.getByTitle('Delete station',{exact:true}).click();expect(writes).toEqual([]);
 state.reject=true;page.once('dialog',d=>d.accept());await page.getByTitle('Delete station',{exact:true}).click();
 await expect(page.locator('main')).toContainText('原 API 衝突訊息');await expect(page.locator('main')).toContainText('原關聯資料');
 expect(writes).toEqual([{method:'DELETE',path:'/api/processes/11',body:null}]);
});
