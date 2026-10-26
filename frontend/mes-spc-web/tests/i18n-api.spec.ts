import {test,expect} from '@playwright/test';
import fs from 'node:fs';
for(const locale of ['zh-TW','zh-CN','en-US'])for(const kind of ['known','field','invalid','unknown','security','ppc','legacy','badHeader','chart','dynamic','badDynamic','import','service'])test(`${locale} ${kind}: safe API display and explicit request locale`,async({page})=>{
 const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));
 await page.addInitScript(()=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'fixture'}));});
 let reject=false;const headers:string[]=[];const raw='原名 <b>@:spc.api.unexpected {field}</b>';
 const catalog=JSON.parse(fs.readFileSync(new URL(`../src/i18n/api/${locale}.json`,import.meta.url),'utf8')).spc.api;
 await page.route('**/*',route=>{
  const request=route.request(),u=new URL(request.url());
  if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();
  expect(request.method()).toBe('GET');
  if(u.pathname==='/api/parts'){
   headers.push(request.headers()['x-spc-ui-locale']);
   if(reject){if(kind==='dynamic'||kind==='badDynamic')return route.fulfill({status:404,body:raw,contentType:'text/plain',headers:{'X-SPC-Message-Key':'spc.api.formulaCodeMissing','X-SPC-Message-Args':kind==='dynamic'?Buffer.from(JSON.stringify({code:raw})).toString('base64url'):'!!!'}});if(kind==='legacy'||kind==='badHeader')return route.fulfill({status:400,body:raw,contentType:'text/plain',headers:{'X-SPC-Message-Key':kind==='legacy'?'spc.api.masterProcessNameRequired':'common.actions.refresh'}});const descriptor=kind==='ppc'?{messageKey:'spc.api.ppcPartRequired',args:{groupName:raw}}:kind==='security'?{messageKey:'spc.api.viewerWriteDenied',args:{}}:kind==='known'?{messageKey:'spc.api.unexpected',args:{}}:kind==='field'?{messageKey:'spc.api.invalidField',args:{field:raw}}:kind==='invalid'?{messageKey:'spc.api.invalidField',args:{extra:raw}}:{messageKey:'common.actions.refresh',args:{}};
    return route.fulfill({status:kind==='service'?409:400,json:kind==='service'?{code:'VERSION_CONFLICT',message:raw,messageKey:'spc.api.calibrationVersionConflict',args:{}}:kind==='import'?{error:raw,messageKey:'spc.api.uploadXlsxRequired',args:{}}:{message:raw,...(kind==='chart'?{messageKey:'spc.api.segmentOverlap',args:{}}:descriptor)}});
   }
  }
  return route.fulfill({json:u.pathname.endsWith('/version')?{version:'0.1.73',environment:'test'}:[]});
 });
 await page.goto('/parts');await expect.poll(()=>headers.length).toBeGreaterThan(0);
 await page.getByTestId('locale-select').selectOption(locale);reject=true;
 await page.getByRole('button',{name:/重新整理|刷新|Refresh|重新載入/}).click();
 const expected=kind==='service'?catalog.calibrationVersionConflict:kind==='import'?catalog.uploadXlsxRequired:kind==='dynamic'?catalog.formulaCodeMissing.replace('{code}',raw):kind==='chart'?catalog.segmentOverlap:kind==='legacy'?catalog.masterProcessNameRequired:kind==='ppc'?catalog.ppcPartRequired.replace('{groupName}',raw):kind==='security'?catalog.viewerWriteDenied:kind==='known'?catalog.unexpected:kind==='field'?catalog.invalidField.replace('{field}',raw):raw;
 await expect(page.locator('main')).toContainText(expected);expect(headers.at(-1)).toBe(locale);
 await expect(page.locator('main b')).toHaveCount(0);expect(errors).toEqual([]);
});
