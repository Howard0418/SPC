import {test,expect,Page} from '@playwright/test';
import fs from 'node:fs';
const raw='RAW <b>@:key {field}</b>';
const apiCatalog=(locale:string)=>JSON.parse(fs.readFileSync(new URL(`../src/i18n/api/${locale}.json`,import.meta.url),'utf8')).spc.api;
async function prepare(page:Page,respond:(path:string)=>any,allowedWrites:string[]=[]){
 const errors:string[]=[];let calls=0;page.on('pageerror',e=>errors.push(e.message));
 await page.addInitScript(()=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'fixture'}));});
 await page.route('**/*',route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();
  if(route.request().method()!=='GET')expect(allowedWrites).toContain(route.request().method()+' '+u.pathname);calls++;
  return route.fulfill(u.pathname.endsWith('/version')?{json:{version:'0.1.81',environment:'test'}}:respond(u.pathname));
 });
 return {errors,count:()=>calls};
}
for(const path of ['/measurements','/spc','/trend-chart','/alerts','/alerts-workflow','/part-process-characteristics','/spc-rule-groups','/uploads','/uploads/71/preview','/spc/query','/settings/smtp','/settings/spc-reports','/operators','/chemical-analysis-overview','/chemical-f-table','/equipment-status','/equipment-points','/equipment-monitor','/etch-analysis','/calibration-instruments','/particle-monitoring','/traceability-master','/genealogy'])test(`${path}: stored error updates with no reload`,async({page})=>{
 const fixture=await prepare(page,()=>({status:400,json:{message:'legacy',messageKey:'spc.api.invalidField',args:{field:raw}}}));
 await page.goto(path+'?keep=42');if(path==='/spc/query')await page.getByTestId('trace-executeQuery').click();
 if(path==='/genealogy'){await page.getByTestId('trace-searchQuery').fill(raw);await page.getByTestId('trace-performSearch').click()}
 await expect(page.locator('main')).toContainText(raw);const main=await page.locator('main').elementHandle(),count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){
  await (path==='/measurements'?page.getByTestId('binding-dialog'):page).getByTestId('locale-select').first().selectOption(locale);
  await expect(page.locator('main')).toContainText(apiCatalog(locale).invalidField.replace('{field}',raw));
  expect(fixture.count()).toBe(count);expect(await main!.evaluate(el=>el===document.querySelector('main'))).toBe(true);expect(new URL(page.url()).search).toBe('?keep=42');
 }
 expect(fixture.errors).toEqual([]);await expect(page.locator('main b')).toHaveCount(0);
});

for(const configured of [true,false])test(`equipment DTO ${configured}: source/device and root metadata switch without polling`,async({page})=>{
 const descriptor={message:'legacy raw',messageKey:'spc.api.invalidField',args:{field:raw}};
 const fixture=await prepare(page,path=>({json:path==='/api/equipment-status'?{isConfigured:configured,...descriptor,sources:[{sourceId:'S1',displayName:'Source raw',status:'unavailable',...descriptor,devices:[{sourceId:'S1',equipmentId:'D0',status:'unavailable',...descriptor}]}]}:[]}));
 await page.goto('/equipment-status');await expect(page.locator('main')).toContainText(raw);const count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){await page.getByTestId('locale-select').selectOption(locale);await expect(page.locator('main')).toContainText(apiCatalog(locale).invalidField.replace('{field}',raw));expect(fixture.count()).toBe(count)}
 expect(fixture.errors).toEqual([]);await expect(page.locator('main b')).toHaveCount(0);
});

for(const path of ['/uploads','/uploads/71/preview'])test(`${path}: nested error, suggestion, original code and zero remain correct`,async({page})=>{
 const descriptor={messageKey:'spc.api.invalidField',args:{field:raw}};
 const fixture=await prepare(page,endpoint=>({json:endpoint==='/api/uploads'?{batches:[{uploadBatchId:71,importStatus:'PreviewReady',totalRows:1,errorRows:1,firstErrorCode:'RAW_CODE',firstErrorMessage:'legacy',firstErrorDescriptor:descriptor}]}:endpoint.endsWith('/preview')?{batch:{uploadBatchId:71,importStatus:'PreviewReady',totalRows:1,errorRows:1},details:[{id:1,rowNo:6,isValid:false,payloadJson:JSON.stringify({MeasuredValue:0})}],errors:[{id:1,uploadDetailId:1,fieldName:'MeasuredValue',errorCode:'RAW_CODE',errorMessage:'legacy',...descriptor,suggestion:'legacy suggestion',suggestionMessage:{messageKey:'spc.api.importFixValueNumeric',args:{}}},{id:2,uploadDetailId:1,errorCode:'UNKNOWN',errorMessage:raw,messageKey:'common.actions.refresh'}]}:endpoint.endsWith('/chart-targets')?{targets:[]}:[]}));
 await page.goto(path);await expect(page.locator('main')).toContainText('RAW_CODE');await expect(page.locator('main')).toContainText(raw);const count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){
  await page.getByTestId('locale-select').selectOption(locale);const catalog=apiCatalog(locale);
  await expect(page.locator('main')).toContainText(catalog.invalidField.replace('{field}',raw));
  if(path.endsWith('/preview')){await expect(page.locator('main')).toContainText(catalog.importFixValueNumeric);await expect(page.locator('main')).toContainText('UNKNOWN');await expect(page.locator('main tbody tr').first().locator('td').nth(9).locator('span').first()).toHaveText('0')}
  expect(fixture.count()).toBe(count);
 }
 expect(fixture.errors).toEqual([]);await expect(page.locator('main b')).toHaveCount(0);
});

test('calibration indexed warnings and outcomes update while selection and draft remain',async({page})=>{
 const descriptor={messageKey:'spc.api.calibrationFieldRequired',args:{field:raw}};
 const fixture=await prepare(page,path=>({json:path==='/api/operators'?[{id:1,operatorName:raw,operatorCode:'001',isActive:true}]:path.endsWith('/import/access')?{success:true,data:{canManage:true}}:path.endsWith('/import/preview')?{success:true,data:{hash:'h0',sheetName:raw,rows:[{rowNumber:6,status:'Ready',code:'I0',name:raw,cycleMonths:12,errors:[],warnings:['legacy warning',raw],warningMessages:[{index:0,descriptor}]}]}}:path.endsWith('/import/commit')?{success:true,data:{added:1,skipped:0,failed:0,rows:[{rowNumber:6,code:'I0',status:'Added',message:'legacy outcome',messageKey:'spc.api.calibrationOutcomeAdded',args:{}}]}}:{success:true,data:[],total:0}}),['POST /api/v1/instruments/import/preview','POST /api/v1/instruments/import/commit']);
 await page.goto('/calibration-instruments');await page.getByRole('button',{name:'Excel 批次匯入',exact:true}).click();const dialog=page.getByRole('dialog');
 await dialog.getByLabel('儀器 Excel 檔案').setInputFiles({name:'fixture.xlsx',mimeType:'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',buffer:Buffer.from('mock-only')});await dialog.getByTestId('calibration-readPreview').click();
 await dialog.getByTestId('calibration-selected').check();await dialog.getByTestId('calibration-department').fill(raw);await dialog.getByTestId('calibration-custodian').selectOption('1');await dialog.getByTestId('calibration-status').selectOption('Active');await dialog.getByTestId('calibration-confirmed').check();
 let count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){await dialog.getByTestId('locale-select').selectOption(locale);await expect(dialog).toContainText(apiCatalog(locale).calibrationFieldRequired.replace('{field}',raw));await expect(dialog.getByTestId('calibration-selected')).toBeChecked();await expect(dialog.getByTestId('calibration-department')).toHaveValue(raw);expect(fixture.count()).toBe(count)}
 await dialog.getByTestId('calibration-commit').click();await expect(dialog.getByRole('status')).toContainText(apiCatalog('zh-TW').calibrationOutcomeAdded);count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){await dialog.getByTestId('locale-select').selectOption(locale);await expect(dialog.getByRole('status')).toContainText(apiCatalog(locale).calibrationOutcomeAdded);expect(fixture.count()).toBe(count)}
 expect(fixture.errors).toEqual([]);await expect(dialog.locator('b')).toHaveCount(0);
});

test('malformed external message objects stay outside the local UI key renderer',async({page})=>{
 const fixture=await prepare(page,()=>({status:400,json:{message:{key:'common.actions.refresh'},title:{key:'common.actions.refresh'}}}));
 await page.goto('/uploads');await expect(page.locator('main')).toContainText('Request failed with status code 400');expect(fixture.errors).toEqual([]);
});

test('native alert still receives a translated string',async({page})=>{
 const fixture=await prepare(page,path=>path.endsWith('/settings')?{status:400,json:{message:'legacy',messageKey:'spc.api.invalidField',args:{field:raw}}}:{json:path==='/api/operators'?[]:{success:true,data:[],total:0}});
 await page.goto('/calibration-instruments');await page.getByTestId('locale-select').selectOption('en-US');let message='';
 page.once('dialog',async dialog=>{message=dialog.message();await dialog.dismiss()});await page.getByTestId('calibration-openSettingsModal').click();
 await expect.poll(()=>message).toContain(apiCatalog('en-US').invalidField.replace('{field}',raw));expect(message).not.toContain('[object Object]');expect(fixture.errors).toEqual([]);
});

test('process related metadata switches while original unknown lines remain',async({page})=>{
 const fixture=await prepare(page,path=>path.endsWith('/processes/11')?{status:409,json:{message:'legacy',messageKey:'spc.api.invalidField',args:{field:raw},relatedItems:['legacy related',raw],relatedItemMessages:[{messageKey:'spc.api.masterRelatedVariable',args:{count:0}},{messageKey:'common.actions.refresh'}]}}:{json:path==='/api/processes'?[{id:11,processCode:'P0',processName:raw,isEnabled:true}]:[]},['DELETE /api/processes/11']);
 await page.goto('/processes');page.once('dialog',d=>d.accept());await page.getByTitle('刪除工站',{exact:true}).click();await expect(page.locator('main')).toContainText('1.');const count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){await page.getByTestId('locale-select').selectOption(locale);const catalog=apiCatalog(locale);await expect(page.locator('main')).toContainText(catalog.invalidField.replace('{field}',raw));await expect(page.locator('main')).toContainText(catalog.masterRelatedVariable.replace('{count}','0'));await expect(page.locator('main')).toContainText('2. '+raw);expect(fixture.count()).toBe(count)}
 expect(fixture.errors).toEqual([]);
});

test('SMTP recipient failure switches language without another send',async({page})=>{
 const fixture=await prepare(page,path=>({json:path==='/api/settings/smtp'?{smtp:{host:'smtp.invalid',port:2525,defaultRecipientEmail:'fixture@example.invalid',recipientOperatorIds:[]},operators:[],departments:[]}:path==='/api/settings/smtp/test'?{success:false,recipients:[{email:'fixture@example.invalid',name:raw,success:false,errorMessage:'寄信服務回傳失敗。',displayMessage:{messageKey:'spc.api.mailSmtpFailed',args:{}}}],host:'smtp.invalid',port:2525}:[]}),['POST /api/settings/smtp/test']);
 await page.goto('/settings/smtp');await page.getByTestId('settings-sendTestEmail').click();await expect(page.locator('main')).toContainText(apiCatalog('zh-TW').mailSmtpFailed);const count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){await page.getByTestId('locale-select').selectOption(locale);await expect(page.locator('main')).toContainText(apiCatalog(locale).mailSmtpFailed);expect(fixture.count()).toBe(count)}
 expect(fixture.errors).toEqual([]);await expect(page.locator('main b')).toHaveCount(0);
});


test('report recipient failure switches language without another send or losing original diagnostics',async({page})=>{
 const fixture=await prepare(page,path=>({json:path==='/api/v1/spc-report-settings'?{schedule:{sendTime:'08:00',weeklyDayOfWeek:1,monthlyDay:1},recipientOperatorIds:[1],operators:[{id:1,operatorCode:'001',operatorName:raw,email:'fixture@example.invalid',isActive:true}],departments:[]}:path==='/api/v1/spc-report-settings/send-now'?{reportType:'weekly',displayType:'週報',periodLabel:'2026-10-01 ~ 2026-10-25',fileName:'SPC_Item_Overview_週報_20261026.xlsx',total:2,success:0,failed:2,recipients:[{operatorName:raw,email:'fixture@example.invalid',success:false,errorMessage:'寄信服務回傳失敗。',displayMessage:{messageKey:'spc.api.mailSmtpFailed',args:{}}},{operatorName:raw,email:'unknown@example.invalid',success:false,errorMessage:raw,displayMessage:{messageKey:'common.actions.refresh'}}]}:[]}),['POST /api/v1/spc-report-settings/send-now']);
 await page.goto('/settings/spc-reports');await page.getByTestId('settings-sendWeekly').click();await expect(page.locator('main')).toContainText(apiCatalog('zh-TW').mailSmtpFailed);const count=fixture.count();
 for(const locale of ['en-US','zh-CN','zh-TW']){await page.getByTestId('locale-select').selectOption(locale);await expect(page.locator('main')).toContainText(apiCatalog(locale).mailSmtpFailed);await expect(page.locator('main')).toContainText(raw);expect(fixture.count()).toBe(count)}
 expect(fixture.errors).toEqual([]);await expect(page.locator('main b')).toHaveCount(0);
});
