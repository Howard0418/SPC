import {test,expect} from '@playwright/test';
import fs from 'node:fs';
for(const locale of ['zh-TW','zh-CN','en-US'])for(const kind of ['known','field','invalid','unknown','security','ppc','legacy','badHeader','chart','dynamic','badDynamic','import','service','composite','badDetails'])test(`${locale} ${kind}: safe API display and explicit request locale`,async({page})=>{
 const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));
 await page.addInitScript(()=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'fixture'}));});
 let reject=false;const headers:string[]=[];const raw='原名 <b>@:spc.api.unexpected {field}</b>';
 let catalog=JSON.parse(fs.readFileSync(new URL(`../src/i18n/api/${locale}.json`,import.meta.url),'utf8')).spc.api;
 await page.route('**/*',route=>{
  const request=route.request(),u=new URL(request.url());
  if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();
  expect(request.method()).toBe('GET');
  if(u.pathname==='/api/parts'){
   headers.push(request.headers()['x-spc-ui-locale']);
   if(reject){if(kind==='dynamic'||kind==='badDynamic')return route.fulfill({status:404,body:raw,contentType:'text/plain',headers:{'X-SPC-Message-Key':'spc.api.formulaCodeMissing','X-SPC-Message-Args':kind==='dynamic'?Buffer.from(JSON.stringify({code:raw})).toString('base64url'):'!!!'}});if(kind==='legacy'||kind==='badHeader')return route.fulfill({status:400,body:raw,contentType:'text/plain',headers:{'X-SPC-Message-Key':kind==='legacy'?'spc.api.masterProcessNameRequired':'common.actions.refresh'}});const descriptor=kind==='ppc'?{messageKey:'spc.api.ppcPartRequired',args:{groupName:raw}}:kind==='security'?{messageKey:'spc.api.viewerWriteDenied',args:{}}:kind==='known'?{messageKey:'spc.api.unexpected',args:{}}:kind==='field'?{messageKey:'spc.api.invalidField',args:{field:raw}}:kind==='invalid'?{messageKey:'spc.api.invalidField',args:{extra:raw}}:{messageKey:'common.actions.refresh',args:{}};
    return route.fulfill({status:kind==='service'?409:400,json:kind==='composite'||kind==='badDetails'?{message:raw,messageKey:'spc.api.importRowInvalid',args:{row:'6'},details:[kind==='composite'?{messageKey:'spc.api.calibrationFieldRequired',args:{field:raw}}:{messageKey:'common.actions.refresh',args:{}}]}:kind==='service'?{code:'VERSION_CONFLICT',message:raw,messageKey:'spc.api.calibrationVersionConflict',args:{}}:kind==='import'?{error:raw,messageKey:'spc.api.uploadXlsxRequired',args:{}}:{message:raw,...(kind==='chart'?{messageKey:'spc.api.segmentOverlap',args:{}}:descriptor)}});
   }
  }
  return route.fulfill({json:u.pathname.endsWith('/version')?{version:'0.1.73',environment:'test'}:[]});
 });
 await page.goto('/parts');await expect.poll(()=>headers.length).toBeGreaterThan(0);
 await page.getByTestId('locale-select').selectOption(locale);reject=true;
 await page.getByRole('button',{name:/重新整理|刷新|Refresh|重新載入/}).click();
 const expectedText=()=>kind==='composite'?catalog.importRowInvalid.replace('{row}','6')+'\n'+catalog.calibrationFieldRequired.replace('{field}',raw):kind==='service'?catalog.calibrationVersionConflict:kind==='import'?catalog.uploadXlsxRequired:kind==='dynamic'?catalog.formulaCodeMissing.replace('{code}',raw):kind==='chart'?catalog.segmentOverlap:kind==='legacy'?catalog.masterProcessNameRequired:kind==='ppc'?catalog.ppcPartRequired.replace('{groupName}',raw):kind==='security'?catalog.viewerWriteDenied:kind==='known'?catalog.unexpected:kind==='field'?catalog.invalidField.replace('{field}',raw):raw;
 await expect(page.locator('main')).toContainText(expectedText());expect(headers.at(-1)).toBe(locale);
 const count=headers.length;const main=await page.locator('main').elementHandle();
 await page.getByTestId('master-search').fill('draft 原文');
 for(const next of ['en-US','zh-CN','zh-TW']){
  catalog=JSON.parse(fs.readFileSync(new URL(`../src/i18n/api/${next}.json`,import.meta.url),'utf8')).spc.api;
  await page.getByTestId('locale-select').selectOption(next);
  await expect(page.locator('main')).toContainText(expectedText());
  await expect(page.getByTestId('master-search')).toHaveValue('draft 原文');
  expect(await main!.evaluate(el=>el===document.querySelector('main'))).toBe(true);expect(headers).toHaveLength(count);
 }
 await expect(page.locator('main b')).toHaveCount(0);expect(errors).toEqual([]);
});

for(const path of ['parts','characteristics','processes','control-chart-groups'])test(`${path}: stored load error and query survive all locale changes`,async({page})=>{
 const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));let calls=0;
 await page.addInitScript(()=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'fixture'}));});
 await page.route('**/*',route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();expect(route.request().method()).toBe('GET');calls++;
  return route.fulfill(u.pathname===`/api/${path}`?{status:400,json:{message:'legacy',messageKey:'spc.api.invalidField',args:{field:'RAW <b>@:key {field}</b>'}}}:{json:u.pathname.endsWith('/version')?{version:'0.1.81',environment:'test'}:[]});
 });
 await page.goto(`/${path}?keep=42`);await expect(page.locator('main')).toContainText('RAW <b>@:key {field}</b>');
 const main=await page.locator('main').elementHandle(),count=calls;
 for(const locale of ['en-US','zh-CN','zh-TW']){
  const catalog=JSON.parse(fs.readFileSync(new URL(`../src/i18n/api/${locale}.json`,import.meta.url),'utf8')).spc.api;
  await page.getByTestId('locale-select').selectOption(locale);
  await expect(page.locator('main')).toContainText(catalog.invalidField.replace('{field}','RAW <b>@:key {field}</b>'));
  expect(calls).toBe(count);expect(new URL(page.url()).search).toBe('?keep=42');expect(await main!.evaluate(el=>el===document.querySelector('main'))).toBe(true);
 }
 expect(errors).toEqual([]);await expect(page.locator('main b')).toHaveCount(0);
});
