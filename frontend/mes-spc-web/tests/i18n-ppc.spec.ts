import {test,expect,Page} from '@playwright/test';
import fs from 'node:fs';
const raw='原名 <b>@:key {name}</b>';
const chemicalJson='{ "enabled": true, "version": "1", "primaryInputLabel": "滴定值", "secondaryInputLabel": "原第二值", "concentrationFormula": "Primary * F!B3", "adjustmentFormula": "", "adjustmentAmountFormula": "", "decimalPlaces": 2, "custom": "原值" }';
const formulaJson='{ "XbarCalculationMethod": "MR_METHOD", "UclFormula": "Xbar + A2 * Rbar", "原值": "@:key {name}" }';
const groups=[{id:10,groupCode:'PRODUCT',businessScopeCode:'PRODUCT',groupType:'CONTROL_CHART',groupName:raw,isEnabled:true},{id:11,groupCode:'PRODUCT_TREND',businessScopeCode:'PRODUCT',groupType:'TREND_CHART',groupName:raw,isEnabled:true},{id:20,groupCode:'PROCESS',businessScopeCode:'PROCESS',groupType:'CONTROL_CHART',groupName:raw,isEnabled:true},{id:30,groupCode:'CHEM',businessScopeCode:'CHEM',groupType:'TREND_CHART',groupName:raw,isEnabled:true}];
const chartTypes=[{id:101,chartGroupId:10,chartTypeCode:'XBAR_R',chartTypeName:raw,dataCategory:'Variable',isEnabled:true},{id:102,chartGroupId:20,chartTypeCode:'I_MR',chartTypeName:raw,dataCategory:'Variable',isEnabled:true}];
const base={processId:21,characteristicId:51,sequenceNo:0,unit:'μm',lsl:0.19999999999999998,usl:0.4,targetValue:0.3,ucl:null,cl:null,lcl:null,sampleSize:5,isRequired:true,isEnabled:true,process:{processName:raw,processCode:'P-RAW'},characteristic:{characteristicName:raw,characteristicCode:'C-RAW',dataCategory:'Variable',unit:'μm'}};
const records=[{...base,id:1,controlScope:'PRODUCT',partId:1,machineId:null,tankId:null,displayMode:'CONTROL_CHART',chartTypeId:101,formulaConfigJson:formulaJson,chemicalAnalysisConfigJson:'',part:{partNo:'RAW-001',partName:raw}}, {...base,id:2,controlScope:'PROCESS',partId:null,machineId:null,tankId:null,displayMode:'CONTROL_CHART',chartTypeId:102,formulaConfigJson:formulaJson,chemicalAnalysisConfigJson:''}, {...base,id:3,controlScope:'CHEM',partId:null,machineId:31,tankId:41,displayMode:'TREND_CHART',chartTypeId:null,formulaConfigJson:'',chemicalAnalysisConfigJson:chemicalJson,machine:{machineCode:'LN-RAW',machineName:raw},tank:{tankCode:'T-RAW',tankName:raw}}];
const rules=Array.from({length:8},(_,i)=>({id:i+1,ruleCode:`Rule${i+1}`,ruleName:`原規則${i+1}`,isSelected:true,priority:i+1}));
const version={id:71,versionNo:7,reason:raw,configJson:chemicalJson,changeType:'Update',changedBy:raw,changedAt:'2026-10-24T04:05:00Z'};
const segment={id:81,partProcessCharacteristicId:1,startDate:'2026-10-01T00:00:00',endDate:null,ucl:0.38,cl:0.3,lcl:0.22,usl:0.4,lsl:0.2,targetValue:0.3,note:raw};
const errors=new WeakMap<Page,string[]>();
const noteCatalog=(locale:string)=>JSON.parse(fs.readFileSync(new URL(`../src/i18n/api/${locale}.json`,import.meta.url),'utf8')).spc.api;
test.beforeEach(({page})=>{const list:string[]=[];errors.set(page,list);page.on('pageerror',e=>list.push(e.message))});
test.afterEach(({page})=>expect(errors.get(page)).toEqual([]));
async function prepare(page:Page,id?:number,role='Editor'){
 await page.addInitScript(role=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role,username:'ppc-fixture'}))},role);
 const reads:string[]=[];const writes:any[]=[];const state={reject:false,empty:false,closed:0,failSchedule:false,noteMetadata:false,englishName:null as string|null,records:structuredClone(records),segments:[structuredClone(segment)]};
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();if(!u.pathname.startsWith('/api/'))return route.continue();const path=u.pathname,method=route.request().method();
  if(method!=='GET'){
   expect(path).toMatch(/^\/api\/(part-process-characteristics(\/\d+)?(\/chemical-formula-schedule(\/preview)?|\/chemical-analysis-formula-versions\/\d+\/restore)?|control-limit-segments(\/\d+)?|v1\/spc\/trial-calculate)$/);
   const body=method==='DELETE'?null:route.request().postDataJSON();writes.push({method,path,search:u.search,body});if(state.reject)return route.fulfill({status:409,json:{message:'原 API 衝突訊息 <b>@:key {name}</b>'}});
   if(path.endsWith('/trial-calculate'))return route.fulfill({json:{ucl:0.38,cl:0.3,lcl:0.22,sampleCount:600,note:raw,...(state.noteMetadata?{noteMessage:{messageKey:'spc.api.trialCalculated',args:{type:raw,count:600}}}:{})}});
   if(path.endsWith('/chemical-formula-schedule/preview'))return route.fulfill({json:{data:{revisionId:9,formulaVersionId:71,formulaVersionNo:7,fTableHistoryId:null,result:{raw:'0.2469',stored:'0.25',judgment:'Pass'},engineVersion:'engine-raw'}}});
   if(path.endsWith('/chemical-formula-schedule'))return route.fulfill({json:{data:{revisionId:10}}});
   if(path.includes('/chemical-analysis-formula-versions/'))return route.fulfill({json:{data:{}}});
   const key=Number(path.split('/').at(-1));if(path.includes('part-process-characteristics')){if(method==='DELETE')state.records=state.records.filter(x=>x.id!==key);else if(method==='PUT'){const row=state.records.find(x=>x.id===key);if(row)Object.assign(row,body)}return route.fulfill({status:method==='DELETE'?204:200,...(method==='DELETE'?{}:{json:{...body,id:method==='POST'?91:key}})})}
   if(method==='DELETE')state.segments=state.segments.filter(x=>x.id!==key);return route.fulfill({status:method==='DELETE'?204:200,...(method==='DELETE'?{}:{json:{...body,id:method==='POST'?82:key}})});
  }
  reads.push(path+u.search);let data:any=[];
  if(path.endsWith('/version'))data={version:'0.1.72',environment:'test'};
  else if(path.endsWith('/delete-impact'))data={variableMeasurements:600,attributeMeasurements:0,calculationResults:3,alerts:2,controlLimitSegments:1,closedAlerts:state.closed};
  else if(path.endsWith('/chemical-formula-schedule')){if(state.failSchedule)return route.fulfill({status:500,json:{message:'原 API 排程錯誤'}});data={data:{revisionId:9,periods:[{formulaVersionId:71,fTableHistoryId:null,effectiveFromUtc:'2026-10-01T16:00:00Z',effectiveUntilUtcExclusive:'2026-10-31T16:00:00Z'}],gaps:[{fromUtc:'2026-09-01T16:00:00Z',untilUtc:'2026-10-01T16:00:00Z'}],message:'原 API 排程訊息',...(state.noteMetadata?{messageKey:'spc.api.chemicalSchedulePreviewOnly',args:{}}:{})}}}
  else if(path.endsWith('/chemical-analysis-formula-versions'))data={data:state.empty?[]:[version]};
  else if(path.endsWith('/specification-limit-versions'))data={data:state.empty?[]:[{id:91,versionNo:2,sourceKind:'Segment',changeType:'Updated',changedBy:raw,changedAt:'2026-10-24T04:05:00Z',previousSnapshotJson:null,snapshotJson:'{"lsl":0.2,"原值":"原文"}'}]};
  else if(path.endsWith('/chemical-f-table/versions'))data=[{id:72,versionNo:2,versionCode:'F-RAW',displayName:raw}];
  else if(/\/part-process-characteristics\/\d+\/rules$/.test(path))data={rules};
  else if(!state.empty){
   if(path==='/api/part-process-characteristics')data=state.records;
   else if(path==='/api/parts')data=[{id:1,partNo:'RAW-001',partName:raw}];
   else if(path==='/api/processes')data=[{id:21,processCode:'P-RAW',processName:raw,isEnabled:true}];
   else if(path==='/api/characteristics')data=[{id:51,characteristicName:raw,...(state.englishName?{characteristicNameEn:state.englishName}:{}),dataCategory:'Variable',unit:'μm',isEnabled:true,isSpcEnabled:true}];
   else if(path==='/api/control-chart-types')data=chartTypes;
   else if(path==='/api/control-chart-groups')data=groups;
   else if(path==='/api/spc-rules')data=rules;
   else if(path==='/api/spc-rule-groups')data=[{id:61,ruleGroupCode:'WE',ruleGroupName:raw}];
   else if(path==='/api/machines')data=[{id:31,processId:21,machineCode:'LN-RAW',machineName:raw}];
   else if(path.endsWith('/tanks'))data=[{id:41,machineId:31,lineId:31,tankCode:'T-RAW',tankName:raw}];
   else if(path.endsWith('/lines'))data=[{id:31,lineCode:'LN-RAW',lineName:raw}];
   else if(path==='/api/control-limit-segments')data=state.segments;
  }
  return route.fulfill({json:data});
 });
 await page.goto('/part-process-characteristics?keep=42'+(id?`&editId=${id}`:''));return {writes,reads,state};
}
const dialog=(page:Page)=>page.getByTestId('ppc-dialog');const field=(page:Page,name:string)=>dialog(page).getByTestId('ppc-form.'+name);const tab=(page:Page,id:number)=>dialog(page).getByTestId('ppc-tab-'+id);
for(const width of [1920,1366,1280,390])for(const locale of ['zh-TW','zh-CN','en-US'])test(`PPC ${width} ${locale}: clear labels, inputs and save reachable`,async({page})=>{
 await page.setViewportSize({width,height:900});const {writes}=await prepare(page);await page.getByTestId('locale-select').selectOption(locale);await expect(page.locator('main h1')).toHaveText(locale==='en-US'?'SPC control items and specifications':locale==='zh-CN'?'SPC 管制项目设置与规格维护':'SPC 管制項目設定與規格維護');
 expect((await page.getByTestId('ppc-searchQuery').boundingBox())!.width).toBeGreaterThan(120);await page.getByTestId('ppc-edit').first().click();await dialog(page).getByTestId('locale-select').selectOption(locale);await tab(page,2).click();await field(page,'lsl').click();expect((await field(page,'lsl').boundingBox())!.width).toBeGreaterThan(90);await expect(dialog(page).locator('button[type="submit"]')).toBeInViewport();
 expect(await dialog(page).evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual(width);expect(writes).toEqual([]);
 if(width===390&&locale==='en-US')await page.screenshot({path:'../../.codex-tmp/i18n-task05c3/ppc-mobile-en.png'});
});
for(const locale of ['zh-TW','zh-CN','en-US']){
 for(const id of [1,2,3])test(`edit ${id} ${locale}: LSL 0.2, stable draft and original DTO`,async({page})=>{
  const {writes,reads}=await prepare(page,id);await expect(dialog(page)).toBeVisible();await tab(page,2).click();await field(page,'lsl').fill('0.2');const handle=await field(page,'lsl').elementHandle();const before=reads.length,url=page.url();await dialog(page).getByTestId('locale-select').selectOption(locale);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(before);expect(page.url()).toBe(url);expect(writes).toEqual([]);await expect(field(page,'lsl')).toHaveValue('0.2');await expect(dialog(page)).toContainText('0.3 ± 0.1');
  if(id!==3){await tab(page,5).click();await expect(field(page,'formulaConfigJson')).toHaveValue(formulaJson);await expect(dialog(page)).toContainText('Anderson-Darling')}
  await dialog(page).locator('button[type="submit"]').click();await expect(dialog(page)).toHaveCount(0);expect(writes).toHaveLength(1);const body=writes[0].body;
  expect(writes[0].method).toBe('PUT');expect(writes[0].path).toBe('/api/part-process-characteristics/'+id);
  const expected={controlScope:records[id-1].controlScope,partId:id===1?1:null,processId:21,machineId:id===3?31:null,tankId:id===3?41:null,characteristicId:51,sequenceNo:0,unit:'μm',usl:0.4,lsl:0.2,ucl:null,cl:null,lcl:null,targetValue:0.3,sampleSize:5,displayMode:id===3?'TREND_CHART':'CONTROL_CHART',chartTypeId:id===1?101:id===2?102:null,formulaConfigJson:id===3?null:formulaJson,chemicalAnalysisConfigJson:id===3?chemicalJson:null,chemicalAnalysis:id===3?{enabled:true,version:'1',primaryInputLabel:'滴定值',secondaryInputLabel:'原第二值',concentrationFormula:'Primary * F!B3',adjustmentFormula:'',adjustmentAmountFormula:'',decimalPlaces:2,custom:'原值'}:{enabled:false,version:'1',primaryInputLabel:'滴定值',secondaryInputLabel:'',concentrationFormula:'',adjustmentFormula:'',adjustmentAmountFormula:'',decimalPlaces:2},selectedRuleCodes:id===3?[]:rules.map(x=>x.ruleCode),isRequired:true,isEnabled:true,chemicalAnalysisFormulaVersionReason:null};expect(body).toEqual(expected);
 });
 test(`create and invalid specs ${locale}: original values and translated validation`,async({page})=>{
  const {writes}=await prepare(page);await page.getByTestId('ppc-add').click();await dialog(page).getByTestId('locale-select').selectOption(locale);await tab(page,2).click();await field(page,'lsl').fill('0.4');await field(page,'usl').fill('0.2');await dialog(page).locator('button[type="submit"]').click();expect(writes).toEqual([]);await dialog(page).getByTestId('locale-select').selectOption('en-US');await expect(dialog(page)).toContainText('USL must be greater than LSL.');await field(page,'lsl').fill('0.2');await field(page,'usl').fill('0.4');await dialog(page).locator('button[type="submit"]').click();await expect(dialog(page)).toHaveCount(0);expect(writes[0].method).toBe('POST');expect(writes[0].body).toMatchObject({controlScope:'PRODUCT',partId:1,processId:21,characteristicId:51,lsl:0.2,usl:0.4,targetValue:0.3,sampleSize:5,chartTypeId:101,chemicalAnalysisConfigJson:null,formulaConfigJson:''});
 });
 test(`schedule ${locale}: draft, validation, exact publish/preview and formula restore`,async({page})=>{
  const {writes,reads,state}=await prepare(page,3);await tab(page,4).click();const s=dialog(page).locator('section').filter({has:page.getByTestId('schedule-add')});await expect(s.getByTestId('schedule-p.fromDate')).toHaveValue('2026-10-02');await expect(s.getByTestId('schedule-p.toDate')).toHaveValue('2026-10-31');await s.getByTestId('schedule-reason').fill('');await s.getByTestId('schedule-publish').click();expect(writes).toEqual([]);await dialog(page).getByTestId('locale-select').selectOption('en-US');await expect(s.getByRole('alert')).toHaveText('Add an effective period and enter a publication reason.');
  await s.getByTestId('schedule-reason').fill(' 原發布原因 ');await s.getByTestId('schedule-p.fromDate').fill('2026-11-02');await s.getByTestId('schedule-publish').click();await expect(s.getByRole('alert')).toContainText('Start date must not be later');await s.getByTestId('schedule-p.fromDate').fill('2026-10-02');await s.getByTestId('schedule-p.toDate').fill('');const before=reads.length;await dialog(page).getByTestId('locale-select').selectOption(locale);expect(reads.length).toBe(before);await expect(s.getByTestId('schedule-reason')).toHaveValue(' 原發布原因 ');
  state.reject=true;await s.getByTestId('schedule-publish').click();await expect(s.getByRole('alert')).toContainText('原 API 衝突訊息');state.reject=false;await s.getByTestId('schedule-publish').click();await expect(s.getByTestId('schedule-reason')).toHaveValue('');expect(writes[1]).toEqual({method:'POST',path:'/api/part-process-characteristics/3/chemical-formula-schedule',search:'',body:{baseRevisionId:9,reason:' 原發布原因 ',periods:[{formulaVersionId:71,fTableHistoryId:null,fromDate:'2026-10-02',toDate:null}]}});
  await s.getByTestId('schedule-measuredAt').fill('2026-10-20T12:00:00+08:00');await s.getByTestId('schedule-primary').fill('0.2000');await s.getByTestId('schedule-preview').click();await expect(s).toContainText('0.2469');expect(writes[2].body).toEqual({measuredAt:'2026-10-20T12:00:00+08:00',primary:'0.2000',secondary:null,sourceKey:'settings-preview-3'});await dialog(page).getByTestId('locale-select').selectOption('en-US');await expect(s).toContainText('stored result 0.25');await s.getByTestId('schedule-primary').fill('0.3');await expect(s).not.toContainText('0.2469');
  await dialog(page).getByTestId('ppc-formula-history').click();await expect(dialog(page)).toContainText(raw);page.once('dialog',d=>d.accept());await dialog(page).getByTestId('ppc-restore-formula').click();await expect.poll(()=>writes.length).toBe(4);expect(writes[3]).toEqual({method:'POST',path:'/api/part-process-characteristics/3/chemical-analysis-formula-versions/71/restore',search:'',body:{reason:'由前端回復至 v7'}});
 });
 test(`segments ${locale}: trial, original date/limit payload, history and delete`,async({page})=>{
  const {writes,reads}=await prepare(page);await page.getByTestId('ppc-segments').first().click();const d=page.getByTestId('segment-dialog');await d.getByTestId('ppc-trialForm.startDate').fill('2026-10-01');await d.getByTestId('ppc-trialForm.endDate').fill('2026-10-24');await d.getByTestId('ppc-trial').click();await expect(d).toContainText('0.3800');await d.getByTestId('ppc-copy-trial').click();await d.getByTestId('ppc-segmentForm.note').fill(raw);const before=reads.length;await d.getByTestId('locale-select').selectOption(locale);expect(reads.length).toBe(before);await expect(d.getByTestId('ppc-segmentForm.ucl')).toHaveValue('0.38');await d.locator('form button[type="submit"]').click();await expect.poll(()=>writes.length).toBe(2);expect(writes[0].body).toEqual({partProcessCharacteristicId:1,startDate:'2026-10-01',endDate:'2026-10-24'});expect(writes[1].body).toEqual({id:null,startDate:'2026-10-01',endDate:'2026-10-24',ucl:0.38,cl:0.3,lcl:0.22,usl:null,lsl:null,targetValue:null,note:raw,partProcessCharacteristicId:1});
  await d.getByTestId('ppc-edit-segment').click();await d.getByTestId('ppc-segmentForm.lsl').fill('0.2');await d.locator('form button[type="submit"]').click();await expect.poll(()=>writes.length).toBe(3);expect(writes[2].path).toBe('/api/control-limit-segments/81');expect(writes[2].body).toEqual({...segment,startDate:'2026-10-01',endDate:null});
  page.once('dialog',d=>d.accept());await d.getByTestId('ppc-delete-segment').click();await expect.poll(()=>writes.length).toBe(4);expect(writes[3].method).toBe('DELETE');expect(writes[3].path).toBe('/api/control-limit-segments/81');
 });
}
test('PPC purge retains closed-alert block, stable typed label and purge flag',async({page})=>{
 const {state,writes}=await prepare(page);await page.getByTestId('locale-select').selectOption('en-US');state.closed=1;let text='';page.once('dialog',async d=>{text=d.message();await d.accept()});await page.getByTestId('ppc-purge').first().click();await expect.poll(()=>text).toContain('closed alerts');expect(writes).toEqual([]);state.closed=0;
 let cancelled=false;page.once('dialog',async d=>{page.once('dialog',async x=>{page.once('dialog',async y=>{cancelled=true;await y.accept()});await x.accept('wrong')});await d.accept()});await page.getByTestId('ppc-purge').first().click();await expect.poll(()=>cancelled).toBe(true);await expect(page.getByTestId('ppc-edit')).toHaveCount(3);expect(writes).toEqual([]);
 // A fresh handler supplies the exact original identifier, independent of translated UI.
 page.removeAllListeners('dialog');page.on('dialog',async d=>{if(d.type()==='confirm')await d.accept();else await d.accept('RAW-001 - '+raw)});await page.getByTestId('ppc-purge').first().click();await expect.poll(()=>writes.length).toBe(1);expect(writes[0]).toEqual({method:'DELETE',path:'/api/part-process-characteristics/1',search:'?purge=true',body:null});
});
test('PPC API conflict and mode conversion preserve draft and clearing contract',async({page})=>{
 const {state,writes}=await prepare(page,1);state.reject=true;await dialog(page).locator('button[type="submit"]').click();await expect(dialog(page)).toContainText('原 API 衝突訊息');await dialog(page).getByTestId('locale-select').selectOption('en-US');await expect(dialog(page)).toContainText('原 API 衝突訊息');state.reject=false;await tab(page,3).click();await field(page,'displayMode').selectOption('TREND_CHART');let prompt='';page.once('dialog',async d=>{prompt=d.message();await d.dismiss()});await dialog(page).locator('button[type="submit"]').click();expect(writes).toHaveLength(1);expect(prompt).toContain('Raw measurements will not be deleted');page.once('dialog',d=>d.accept());await dialog(page).locator('button[type="submit"]').click();await expect(dialog(page)).toHaveCount(0);expect(writes[1].body).toMatchObject({displayMode:'TREND_CHART',chartTypeId:null,formulaConfigJson:null,ucl:null,cl:null,lcl:null,lsl:0.19999999999999998,usl:0.4,targetValue:0.3});
});
test('PPC empty state and existing Viewer guard',async({page})=>{const {state,writes}=await prepare(page);state.empty=true;await page.reload();await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText('No matching control items');expect(writes).toEqual([])});
test('PPC Viewer cannot access editor route',async({page})=>{const {writes}=await prepare(page,undefined,'Viewer');await expect(page).not.toHaveURL(/part-process-characteristics\?keep=42$/);expect(writes).toEqual([])});
for(const locale of ['zh-TW','zh-CN','en-US'])test(`mobile schedule and segments ${locale}: drafts and controls fit`,async({page})=>{
 await page.setViewportSize({width:390,height:900});const {writes}=await prepare(page,3);await tab(page,4).click();await dialog(page).getByTestId('locale-select').selectOption(locale);const s=dialog(page).locator('section').filter({has:page.getByTestId('schedule-add')});await s.getByTestId('schedule-primary').fill('0.2');await s.getByTestId('schedule-reason').fill('草稿');await s.getByTestId('schedule-publish').scrollIntoViewIfNeeded();await expect(s.getByTestId('schedule-publish')).toBeInViewport();expect(await s.evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual((await s.boundingBox())!.width);await expect(dialog(page).locator('button[type="submit"]')).toBeInViewport();
 await dialog(page).getByRole('button',{name:locale==='en-US'?'Close':locale==='zh-CN'?'关闭':'關閉',exact:true}).click();await page.getByTestId('ppc-segments').first().click();const d=page.getByTestId('segment-dialog');await d.getByTestId('locale-select').selectOption(locale);await d.getByTestId('ppc-segmentForm.note').fill('未存');await d.locator('form button[type="submit"]').scrollIntoViewIfNeeded();await expect(d.locator('form button[type="submit"]')).toBeInViewport();expect(await d.evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual(390);expect(writes).toEqual([]);
 if(locale==='en-US')await page.screenshot({path:'../../.codex-tmp/i18n-task05c3/segments-mobile-en.png'});
});
for(const locale of ['zh-TW','zh-CN','en-US'])test(`normal delete ${locale}: cancel and original ID`,async({page})=>{
 const {writes}=await prepare(page);await page.getByTestId('locale-select').selectOption(locale);let prompt='';page.once('dialog',async d=>{prompt=d.message();await d.dismiss()});await page.getByTestId('ppc-delete').first().click();expect(prompt).toContain('RAW-001 - '+raw);expect(writes).toEqual([]);page.once('dialog',d=>d.accept());await page.getByTestId('ppc-delete').first().click();await expect.poll(()=>writes.length).toBe(1);expect(writes[0]).toEqual({method:'DELETE',path:'/api/part-process-characteristics/1',search:'',body:null});
});

test('trial note metadata: language changes keep limits and original note draft without recalculation',async({page})=>{
 const {state,reads,writes}=await prepare(page);state.noteMetadata=true;
 await page.getByTestId('ppc-segments').first().click();const d=page.getByTestId('segment-dialog');
 await d.getByTestId('ppc-trialForm.startDate').fill('2026-10-01');await d.getByTestId('ppc-trialForm.endDate').fill('2026-10-24');await d.getByTestId('ppc-trial').click();
 await expect(d).toContainText('0.3800');await d.getByTestId('ppc-copy-trial').click();await d.getByTestId('ppc-segmentForm.note').fill('原草稿 '+raw);
 const handle=await d.getByTestId('ppc-segmentForm.ucl').elementHandle(),count=reads.length;
 for(const locale of ['en-US','zh-CN','zh-TW']){await d.getByTestId('locale-select').selectOption(locale);await expect(d).toContainText(noteCatalog(locale).trialCalculated.replace('{type}',raw).replace('{count}','600'));await expect(d.getByTestId('ppc-segmentForm.ucl')).toHaveValue('0.38');await expect(d.getByTestId('ppc-segmentForm.note')).toHaveValue('原草稿 '+raw);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(count);expect(writes).toHaveLength(1)}
 await expect(d.locator('b')).toHaveCount(0);
});

test('schedule note metadata: fixed explanation switches while publication draft remains',async({page})=>{
 const {state,reads,writes}=await prepare(page,3);state.noteMetadata=true;await tab(page,4).click();
 const s=dialog(page).locator('section').filter({has:page.getByTestId('schedule-add')});await expect(s.getByTestId('schedule-p.fromDate')).toHaveValue('2026-10-02');
 await s.getByTestId('schedule-reason').fill('原排程草稿');const count=reads.length,handle=await s.getByTestId('schedule-reason').elementHandle();
 for(const locale of ['en-US','zh-CN','zh-TW']){await dialog(page).getByTestId('locale-select').selectOption(locale);await expect(s).toContainText(noteCatalog(locale).chemicalSchedulePreviewOnly);await expect(s.getByTestId('schedule-reason')).toHaveValue('原排程草稿');expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(count);expect(writes).toEqual([])}
});

for(const width of [390,1280])test(`characteristic name switch ${width}: mapping names and query remain stable`,async({page})=>{
 await page.setViewportSize({width,height:900});const {reads,writes,state}=await prepare(page);
 state.englishName='Proportion <b>@:key {name}</b>';state.records[0].characteristic={...state.records[0].characteristic,characteristicNameEn:state.englishName};await page.reload();await expect(page.getByTestId('ppc-characteristic-name').first()).toContainText(raw);const count=reads.length;
 const handle=await page.getByTestId('ppc-characteristic-name').first().elementHandle();
 for(const [locale,name] of [['en-US','Proportion <b>@:key {name}</b>'],['zh-CN',raw],['zh-TW',raw]]){await page.getByTestId('locale-select').selectOption(locale);await expect(page.getByTestId('ppc-characteristic-name').first()).toHaveText(name);expect(await handle!.evaluate(n=>n.isConnected)).toBe(true);expect(reads.length).toBe(count)}
 await page.getByTestId('locale-select').selectOption('en-US');await expect(page.getByTestId('ppc-characteristic-name').nth(1)).toHaveText(raw);await expect(page.locator('main b')).toHaveCount(0);expect(writes).toEqual([]);
 await page.getByTestId('ppc-edit').first().click();await expect(field(page,'characteristicId')).toHaveValue('51');await expect(field(page,'characteristicId').locator('option:checked')).toContainText(state.englishName);await dialog(page).getByTestId('locale-select').selectOption('zh-TW');await expect(field(page,'characteristicId')).toHaveValue('51');await expect(field(page,'characteristicId').locator('option:checked')).toContainText(raw);expect(writes).toEqual([]);
});
