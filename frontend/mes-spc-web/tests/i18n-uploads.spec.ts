import {test,expect,Page} from '@playwright/test';
const raw='原名 <b>@:key {name}|data</b>';
const title={'zh-TW':'匯入批次與錯誤追蹤','zh-CN':'汇入批次与错误追踪','en-US':'Import batches and error tracking'};
const previewTitle={'zh-TW':'匯入資料檢核與異常對照預覽','zh-CN':'汇入资料检核与异常对照预览','en-US':'Import validation and error preview'};
const confirmText={'zh-TW':'確定批次建立缺少的品質特性設定？共影響 2 列。','zh-CN':'确定批次建立缺少的品质特性设置？共影响 2 列。','en-US':'Create the missing characteristic settings for this batch? This affects 2 rows.'};
const errors=new WeakMap<Page,string[]>();
test.beforeEach(({page})=>{const list:string[]=[];errors.set(page,list);page.on('pageerror',e=>list.push(e.message))});
test.afterEach(({page})=>expect(errors.get(page)).toEqual([]));
async function prepare(page:Page,path='/uploads?keep=42',role='Editor'){
 await page.addInitScript(role=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role,username:'uploads-fixture'}))},role);
 const reads:string[]=[],writes:any[]=[];
 const state={empty:false,failList:false,failPreview:false,imported:false,noTarget:false,failCreate:false,emptyDetails:false,errorRows:3,delay:false,release:()=>{}};
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();if(!u.pathname.startsWith('/api/'))return route.continue();
  const method=route.request().method(),path=u.pathname;let data:any=[];
  if(method!=='GET'){
   expect(path).toBe('/api/uploads/71/create-missing-mappings');expect(method).toBe('POST');writes.push({method,path,body:route.request().postData()});
   if(state.delay)await new Promise<void>(resolve=>{state.release=resolve});
   if(state.failCreate)return route.fulfill({status:409,json:{message:'原 API 補建失敗 '+raw}});
   return route.fulfill({json:{validRows:3,errorRows:1,createdCharacteristics:1,createdTanks:0,createdMappings:2,reusedMappings:3,skippedMissingTank:4,skippedMissingMasterData:5}});
  }
  reads.push(path+u.search);
  if(path.endsWith('/version'))data={version:'0.1.72',environment:'test'};
  else if(path==='/api/uploads'){
   if(state.failList)return route.fulfill({status:500,json:{message:'原 API 清單失敗 '+raw}});
   data={batches:state.empty?[]:[{uploadBatchId:71,sourceType:'TransFiles',originalFileName:raw,createdAt:'2026-10-25T04:34:00Z',importStatus:'PreviewReady',totalRows:4,validRows:1,errorRows:3,firstErrorCode:'MAPPING_NOT_FOUND',firstErrorMessage:raw},{uploadBatchId:72,importStatus:'Imported',totalRows:1,validRows:1,errorRows:0},{uploadBatchId:73,createdAt:'invalid-date',importStatus:'CustomRawStatus',totalRows:0,validRows:0,errorRows:0}]};
  }else if(path==='/api/uploads/71/preview'){
   if(state.failPreview)return route.fulfill({status:500,json:{message:'原 API 預覽失敗 '+raw}});
   const base={PartNo:raw,ProcessCode:'P-RAW',MachineCode:'L-RAW',TankCode:'T-RAW',CharacteristicCode:'C-RAW',ResolvedProcessCode:'P-RAW',ResolvedMachineCode:'L-RAW',ResolvedTankCode:'T-RAW',ResolvedCharacteristicName:raw,Unit:'',ResolvedUnit:'',ResolvedMappingId:51,Operator:raw,__SourceSheet:raw,__SourceRow:12};
   const details=[{id:1,rowNo:12,isValid:false,payloadJson:JSON.stringify({...base,MeasuredValue:0,RecheckValue:0,AdjustAmount:0,MeasuredAt:'20261025T04:34:00Z'})},{id:2,rowNo:13,isValid:false,payloadJson:JSON.stringify({'料號':raw,'製程':'P-RAW','線別':'L-RAW','槽位':'T-RAW','檢驗項目':raw,'單位':'','不良數':0,'總數':0,'日期':'invalid-date','作業員':''})},{id:3,rowNo:14,isValid:false,payloadJson:'not-json'}];
   const err=[{id:1,uploadDetailId:1,fieldName:'CharacteristicCode',errorCode:'MAPPING_NOT_FOUND',errorMessage:raw,suggestion:raw,sourceFile:raw,sourceSheet:raw,sourceRow:12},{id:2,uploadDetailId:2,fieldName:'Unit',errorCode:'UNIT_MISMATCH',errorMessage:raw},{id:3,uploadDetailId:2,fieldName:'TankCode',errorCode:'TANK_NOT_FOUND',errorMessage:raw},{id:4,uploadDetailId:3,fieldName:'MeasuredValue',errorCode:'INVALID_NUMBER',errorMessage:raw}];
   data={batch:{uploadBatchId:71,originalFileName:raw,totalRows:4,validRows:1,errorRows:state.errorRows,importStatus:state.imported?'Imported':'PreviewReady'},details:state.emptyDetails?[]:details,errors:state.emptyDetails?[]:err};
  }else if(path==='/api/uploads/71/chart-targets')data={targets:state.noTarget?[]:[{partProcessCharacteristicId:51}]};
  else if(path==='/api/uploads/71/progress')data={total:4,processed:2,importStatus:'Revalidating'};
  return route.fulfill({json:data});
 });
 await page.goto(path);return {reads,writes,state};
}
const field=(page:Page,name:string)=>page.getByTestId('uploads-'+name);
async function noImport(page:Page){
 await expect(page.locator('main input[type=file]')).toHaveCount(0);
 await expect(page.locator('main').getByRole('button',{name:/^(確認匯入|确认导入|Confirm import|放棄暫存|Discard staging|Overwrite)/i})).toHaveCount(0);
}
async function fits(page:Page){expect(await page.locator('main').evaluate(el=>el.scrollWidth)).toBeLessThanOrEqual(await page.locator('main').evaluate(el=>el.clientWidth))}
for(const width of [1920,1366,1280,390])for(const locale of ['zh-TW','zh-CN','en-US']){
 test(`list ${width} ${locale}: translations, source data and reachable preview`,async({page})=>{
  await page.setViewportSize({width,height:900});const {reads,writes}=await prepare(page);await expect(field(page,'preview')).toHaveCount(3);const before=reads.length;const handle=await field(page,'preview').first().elementHandle();await page.getByTestId('locale-select').selectOption(locale);
  await expect(page.locator('main h1')).toHaveText(title[locale]);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(before);await expect(page).toHaveURL(/uploads\?keep=42$/);await expect(page.locator('main')).toContainText(raw);await expect(page.locator('main')).toContainText('PreviewReady');await expect(page.locator('main')).toContainText('CustomRawStatus');await expect(page.locator('main b')).toHaveCount(0);await noImport(page);await fits(page);await field(page,'preview').first().scrollIntoViewIfNeeded();await expect(field(page,'preview').first()).toBeInViewport();expect(writes).toEqual([]);
  const query=new URL('https://fixture'+reads.find(r=>r.startsWith('/api/uploads?'))!);expect([...query.searchParams.keys()].sort()).toEqual(['_','take']);expect(query.searchParams.get('take')).toBe('80');expect(Number(query.searchParams.get('_'))).toBeGreaterThan(0);
  if(width===390&&locale==='en-US')await page.screenshot({path:'../../.codex-tmp/i18n-task05e/list-mobile-en.png'});
  await field(page,'preview').first().click();await expect(page).toHaveURL(/uploads\/71\/preview$/);
 });
 test(`preview ${width} ${locale}: source, zero and actions survive language switch`,async({page})=>{
  await page.setViewportSize({width,height:900});const {reads,writes}=await prepare(page,'/uploads/71/preview?keep=42');await expect(field(page,'create')).toBeVisible();const before=reads.length;const handle=await field(page,'create').elementHandle();await page.getByTestId('locale-select').selectOption(locale);await expect(page.locator('main h1')).toHaveText(previewTitle[locale]);expect(await handle!.evaluate(el=>el.isConnected)).toBe(true);expect(reads.length).toBe(before);await expect(page).toHaveURL(/preview\?keep=42$/);await expect(page.locator('main')).toContainText(raw);await expect(page.locator('main b')).toHaveCount(0);
  const cells=page.locator('main tbody tr').first().locator('td');await expect(cells.nth(9)).toHaveText('0');await expect(cells.nth(10)).toHaveText('0');await expect(cells.nth(12)).toHaveText('0');await expect(cells.nth(13)).toContainText('2026');await expect(page.locator('main tbody tr').nth(1)).toContainText('invalid-date');await expect(page.locator('main tbody tr').nth(1).locator('td').nth(9)).toContainText('0 / 0');await noImport(page);await fits(page);await field(page,'create').scrollIntoViewIfNeeded();await expect(field(page,'create')).toBeInViewport();await field(page,'manual').scrollIntoViewIfNeeded();await expect(field(page,'manual')).toBeInViewport();expect(writes).toEqual([]);
  if(width===390&&locale==='en-US')await page.screenshot({path:'../../.codex-tmp/i18n-task05e/preview-mobile-en.png'});
 });
}
for(const locale of ['zh-TW','zh-CN','en-US']){
 test(`create ${locale}: cancel, original POST, progress and translated result`,async({page})=>{
  const {writes,state}=await prepare(page,'/uploads/71/preview');await expect(field(page,'create')).toBeVisible();await page.getByTestId('locale-select').selectOption(locale);
  const cancel=page.waitForEvent('dialog').then(async d=>{expect(d.message()).toBe(confirmText[locale]);await d.dismiss()});await field(page,'create').click();await cancel;expect(writes).toEqual([]);
  state.delay=true;let alertText='';page.on('dialog',async d=>{if(d.type()==='confirm')expect(d.message()).toBe(confirmText[locale]);else alertText=d.message();await d.accept()});await field(page,'create').click();await expect.poll(()=>writes.length).toBe(1);await expect(page.locator('main .fixed')).toBeVisible();await expect(page.locator('main .fixed')).toContainText('50%');state.release();await expect(page.locator('main .fixed')).toHaveCount(0);await expect.poll(()=>alertText).not.toBe('');
  expect(alertText.split('\n')).toHaveLength(7);expect(alertText).toContain(': 1'.replace(': ',locale==='en-US'?': ':'：'));for(const n of [0,2,3,4,5])expect(alertText).toContain(String(n));expect(writes).toEqual([{method:'POST',path:'/api/uploads/71/create-missing-mappings',body:null}]);
 });
 test(`navigation ${locale}: chart and manual setup keep original query`,async({page})=>{
  const {state,writes}=await prepare(page,'/uploads/71/preview');state.imported=true;await page.reload();await page.getByTestId('locale-select').selectOption(locale);await expect(field(page,'chart')).toBeEnabled();await field(page,'chart').click();await expect(page).toHaveURL(/\/spc\?ppcId=51&uploadBatchId=71$/);expect(writes).toEqual([]);
  state.imported=false;await page.goto('/uploads/71/preview');await field(page,'manual').click();await expect(page).toHaveURL(/part-process-characteristics\?scope=CHEM&uploadBatchId=71$/);expect(writes).toEqual([]);
 });
}
test('empty list, refresh, raw API failures, empty preview and missing chart target',async({page})=>{
 const {state,reads,writes}=await prepare(page);await page.getByTestId('locale-select').selectOption('en-US');state.empty=true;await field(page,'refresh').click();await expect(page.locator('main')).toContainText('No import batches yet.');state.failList=true;await field(page,'refresh').click();await expect(page.locator('main')).toContainText('原 API 清單失敗 '+raw);await expect(page.locator('main b')).toHaveCount(0);expect(reads.filter(r=>r.startsWith('/api/uploads?'))).toHaveLength(3);
 state.failPreview=true;await page.goto('/uploads/71/preview');await expect(page.locator('main')).toContainText('原 API 預覽失敗 '+raw);state.failPreview=false;state.emptyDetails=true;state.errorRows=0;await page.reload();await expect(page.locator('main')).toContainText('No rejected data.');state.errorRows=1;await page.reload();await expect(page.locator('main')).toContainText('Rejected data remains.');state.imported=true;state.noTarget=true;await page.reload();await expect(field(page,'chart')).toBeDisabled();await noImport(page);expect(writes).toEqual([]);
});
test('failed master setup preserves raw API error through language changes',async({page})=>{
 const {state,writes}=await prepare(page,'/uploads/71/preview');state.failCreate=true;page.on('dialog',d=>d.accept());await field(page,'create').click();await expect(page.locator('main')).toContainText('原 API 補建失敗 '+raw);await page.getByTestId('locale-select').selectOption('en-US');await expect(page.locator('main')).toContainText('原 API 補建失敗 '+raw);await expect(field(page,'create')).toBeEnabled();expect(writes).toHaveLength(1);
});
for(const path of ['/uploads','/uploads/71/preview'])test(`Viewer guard ${path}`,async({page})=>{
 const {writes}=await prepare(page,path,'Viewer');await expect(page).not.toHaveURL(new RegExp(path+'$'));expect(writes).toEqual([]);
});
test('legacy upload aliases retain redirects without file input or writes',async({page})=>{
 const {writes}=await prepare(page,'/uploads/attribute');await expect(page).toHaveURL(/uploads\?tab=attribute$/);await noImport(page);await page.goto('/uploads/variable');await expect(page).toHaveURL(/\/uploads$/);await noImport(page);await page.goto('/csv-import');await expect(page).toHaveURL(/\/uploads$/);expect(writes).toEqual([]);
});
