import { test, expect, Page } from '@playwright/test';
async function baseSetup(page: Page, reject = false) {
 const config = JSON.stringify({enabled:true,concentrationFormula:'Primary',decimalPlaces:2});
 let rows:any[] = [1,2].map(id=>({id,controlScope:'CHEM',processId:1,machineId:1,tankId:id,characteristicId:id,displayMode:'TREND_CHART',sampleSize:1,isEnabled:true,machine:{machineCode:'N1'},tank:{tankName:`槽${id}`},characteristic:{characteristicName:`濃度${id}`},chemicalAnalysisConfigJson:config}));
 const histories:any = {1:[{id:1,versionNo:1,reason:'WI-OLD',configJson:config,changedAt:'2026-10-01T00:00:00Z',changedBy:'測試人員',changeType:'Update'}],2:[]};
 const puts:any[]=[];
 await page.addInitScript(()=>localStorage.setItem('mes_spc_user',JSON.stringify({role:'Editor'})));
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url()); if(u.hostname!=='127.0.0.1')return route.abort(); if(!u.pathname.startsWith('/api/'))return route.continue();
  const path=u.pathname; const history=path.match(/part-process-characteristics\/(\d+)\/chemical-analysis-formula-versions/);
  if(history)return route.fulfill({json:{data:histories[history[1]]}});
  if(route.request().method()==='PUT'){
   const id=Number(path.split('/').at(-1));const data=route.request().postDataJSON();puts.push(data);
   if(id===2&&reject)return route.fulfill({status:400,json:{message:'測試拒絕'}});
   rows=rows.map(r=>r.id===id?{...r,...data}:r);histories[id].unshift({id:20,versionNo:2,reason:data.chemicalAnalysisFormulaVersionReason,configJson:data.chemicalAnalysisConfigJson,changedAt:'2026-10-19T00:00:00Z',changedBy:'測試人員',changeType:'Update'});
   return route.fulfill({json:rows.find(r=>r.id===id)});
  }
  let json:any=[]; if(path.endsWith('/version'))json={environment:'test',version:'test'};else if(path.endsWith('/part-process-characteristics'))json=rows;
 else if(path.endsWith('/processes'))json=[{id:1,processCode:'CHEM',processName:'藥液',controlScope:'CHEM'}];
 else if(path.endsWith('/characteristics'))json=rows.map(r=>({id:r.characteristicId,characteristicName:`濃度${r.id}`,controlScope:'CHEM',dataCategory:'Variable'}));
 else if(path.endsWith('/machines'))json=[{id:1,machineCode:'N1'}];
 else if(path.endsWith('/tanks'))json=rows.map(r=>({id:r.tankId,machineId:1,tankName:r.tank.tankName}));
 else if(path.endsWith('/control-chart-groups'))json=[{id:1,groupCode:'CHEM',groupType:'TREND_CHART',businessScopeCode:'CHEM',isEnabled:true}];
  return route.fulfill({json});
 });page.on('dialog',d=>d.accept());return puts;
}
async function setupSchedule(page:Page, options:{viewer?:boolean,conflict?:boolean,error?:boolean}={}) {
 await baseSetup(page); const writes:any[]=[]; let fail=options.error;
 if(options.viewer)await page.addInitScript(()=>localStorage.setItem('mes_spc_user',JSON.stringify({role:'Viewer'})));
 await page.route('**/api/**/chemical-formula-schedule**',async route=>{
  const path=new URL(route.request().url()).pathname;
  if(route.request().method()==='POST'){
   writes.push({path,body:route.request().postDataJSON()});
   if(path.endsWith('/preview'))return route.fulfill({json:{success:true,data:{revisionId:10,formulaVersionId:1,formulaVersionNo:1,result:{raw:'2.469',stored:'2.47',judgment:'Unknown'},engineVersion:'portal-excel8-v1'}}});
   if(options.conflict)return route.fulfill({status:409,json:{message:'排程已被其他人發布，請重新載入後再操作。'}});
   return route.fulfill({json:{success:true,data:{revisionId:11}}});
  }
  if(fail){fail=false;return route.fulfill({status:500,json:{message:'排程暫時無法載入'}});}
  return route.fulfill({json:{success:true,data:{revisionId:10,periods:[{formulaVersionId:1,fTableHistoryId:null,effectiveFromUtc:'2026-10-01T16:00:00Z',effectiveUntilUtcExclusive:'2026-10-31T16:00:00Z'}],message:'目前僅排程與試算；既有寫入尚未切換日期選版。'}}});
 });
 await page.goto('/part-process-characteristics?editId=1');await page.getByRole('button',{name:'藥液公式',exact:true}).click(); return writes;
}
const section=(page:Page)=>page.getByRole('region',{name:'公式生效排程'});
test('Taipei inclusive days without writes',async({page})=>{
 const writes=await setupSchedule(page);const s=section(page);await expect(s.getByLabel('期間1起日')).toHaveValue('2026-10-02');await expect(s.getByLabel('期間1迄日')).toHaveValue('2026-10-31');expect(writes).toHaveLength(0);
});
test('publish revision and dates',async({page})=>{
 const writes=await setupSchedule(page);const s=section(page);await s.getByLabel('發布依據').fill('DOC-001');await s.getByRole('button',{name:'發布公式排程'}).click();await expect(s).toContainText('排程發布成功');expect(writes[0].body).toEqual({baseRevisionId:10,reason:'DOC-001',periods:[{formulaVersionId:1,fTableHistoryId:null,fromDate:'2026-10-02',toDate:'2026-10-31'}]});
});
test('conflict preserves draft',async({page})=>{
 await setupSchedule(page,{conflict:true});const s=section(page);await s.getByLabel('發布依據').fill('保留草稿');await s.getByRole('button',{name:'發布公式排程'}).click();await expect(s.getByRole('alert')).toContainText('其他人');await expect(s.getByLabel('發布依據')).toHaveValue('保留草稿');
});
test('invalid range is rejected before request',async({page})=>{
 const writes=await setupSchedule(page);const s=section(page);await s.getByLabel('發布依據').fill('test');await s.getByLabel('期間1起日').fill('2026-11-02');await s.getByRole('button',{name:'發布公式排程'}).click();await expect(s.getByRole('alert')).toContainText('起日不可晚於迄日');expect(writes).toHaveLength(0);
});
test('raw preview cleared after edit without prepare',async({page})=>{
 const writes=await setupSchedule(page);const s=section(page);await s.getByLabel('量測時間（含時區）').fill('2026-10-20T12:00:00+08:00');await s.getByLabel('Primary原始值',{exact:true}).fill('1.2345');await s.getByRole('button',{name:'依量測日期試算'}).click();await expect(s).toContainText('原始計算結果 2.469');expect(writes[0].body.primary).toBe('1.2345');expect(writes[0].path).toMatch(/preview$/);await s.getByLabel('Primary原始值',{exact:true}).fill('2');await expect(s.getByText(/原始計算結果/)).toHaveCount(0);
});
test('Viewer cannot publish',async({page})=>{
 await setupSchedule(page,{viewer:true});const s=section(page);await expect(s).toContainText('目前排程修訂：10');await expect(s.getByRole('button',{name:'發布公式排程'})).toHaveCount(0);await expect(s.getByLabel('期間1起日')).toBeDisabled();
});
test('load error supports retry',async({page})=>{
 await setupSchedule(page,{error:true});const s=section(page);await expect(s.getByRole('alert')).toContainText('暫時無法載入');await expect(s.getByRole('button',{name:'發布公式排程'})).toHaveCount(0);await s.getByRole('button',{name:'重新載入排程'}).click();await expect(s).toContainText('目前排程修訂：10');
});
