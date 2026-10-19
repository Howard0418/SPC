import { test, expect, Page } from '@playwright/test';
async function setup(page: Page, reject = false) {
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
test('shared history and reference survive overview save',async({page})=>{
 const puts=await setup(page);await page.goto('/chemical-analysis-overview');await page.getByRole('button',{name:'版本歷史',exact:true}).first().click();await expect(page.getByText('參照文件／修改依據：WI-OLD')).toBeVisible();
 await page.getByRole('button',{name:'編輯',exact:true}).first().click();await page.locator('textarea').first().fill('Primary * 2');await page.getByLabel('參照文件／修改依據',{exact:true}).fill('WI-CA-001｜Rev.03');await page.getByRole('button',{name:/儲存變更/}).click();
 await expect(page.getByText('儲存完成：成功 1 筆，失敗 0 筆。')).toBeVisible();expect(puts[0].chemicalAnalysisFormulaVersionReason).toBe('WI-CA-001｜Rev.03');await page.getByRole('button',{name:'重新載入',exact:true}).click();await expect(page.getByText('參照文件／修改依據：WI-CA-001｜Rev.03')).toBeVisible();
 await page.getByRole('button',{name:'版本歷史',exact:true}).nth(1).click();await expect(page.getByText('尚無公式版本紀錄；不會補造過去歷史。')).toBeVisible();
});
test('partial failure preserves draft and reference',async({page})=>{
 await setup(page,true);await page.goto('/chemical-analysis-overview');for(let i=0;i<2;i++)await page.getByRole('button',{name:'編輯',exact:true}).first().click();await page.locator('textarea').nth(0).fill('Primary * 2');await page.locator('textarea').nth(3).fill('Primary * 3');await page.getByLabel('參照文件／修改依據',{exact:true}).fill('依據文件');await page.getByRole('button',{name:/儲存變更/}).click();await expect(page.getByText('儲存完成：成功 1 筆，失敗 1 筆。')).toBeVisible();await expect(page.locator('textarea').nth(3)).toHaveValue('Primary * 3');await expect(page.getByLabel('參照文件／修改依據',{exact:true})).toHaveValue('依據文件');
});
test('mobile history and unchanged reference do not write',async({page})=>{
 const puts=await setup(page);await page.setViewportSize({width:390,height:844});await page.goto('/chemical-analysis-overview');await page.getByLabel('參照文件／修改依據',{exact:true}).fill('僅改備註');await expect(page.getByRole('button',{name:/儲存變更/})).toBeDisabled();await page.getByRole('button',{name:'版本歷史',exact:true}).first().click();await expect(page.getByRole('region',{name:'公式版本歷史'})).toBeVisible();expect(puts).toHaveLength(0);
});

test('settings saves reference into the same PPC history',async({page})=>{
 const puts=await setup(page);await page.goto('/part-process-characteristics?editId=1');
 await page.getByRole('button',{name:'藥液公式',exact:true}).click();
 await page.getByLabel('參照文件／修改依據',{exact:true}).fill('WI-SETTINGS');
 await page.getByLabel('濃度公式 *',{exact:true}).fill('Primary * 4');
 await page.getByRole('button',{name:'版本紀錄',exact:true}).click();
 await expect(page.getByText('參照文件／修改依據：WI-OLD')).toBeVisible();
 await page.getByRole('button',{name:'確認儲存',exact:true}).click();
 await expect.poll(()=>puts.length).toBe(1);expect(puts[0].chemicalAnalysisFormulaVersionReason).toBe('WI-SETTINGS');
 await page.goto('/chemical-analysis-overview');await page.getByRole('button',{name:'版本歷史',exact:true}).first().click();await expect(page.getByText('參照文件／修改依據：WI-SETTINGS')).toBeVisible();
});

test('history loading error supports retry without losing context',async({page})=>{
 await setup(page);let failed=true;
 await page.route('**/api/part-process-characteristics/1/chemical-analysis-formula-versions',route=>failed?route.fulfill({status:500,json:{message:'暫時無法讀取'}}):route.fulfill({json:{data:[]}}));
 await page.goto('/chemical-analysis-overview');await page.getByRole('button',{name:'版本歷史',exact:true}).first().click();await expect(page.getByRole('alert')).toContainText('暫時無法讀取');failed=false;await page.getByRole('button',{name:'重試',exact:true}).click();await expect(page.getByText('尚無公式版本紀錄；不會補造過去歷史。')).toBeVisible();
});

test('settings reference-only save preserves original formula JSON',async({page})=>{
 const puts=await setup(page);await page.goto('/part-process-characteristics?editId=1');await page.getByRole('button',{name:'藥液公式',exact:true}).click();await page.getByLabel('參照文件／修改依據',{exact:true}).fill('僅備註');await page.getByRole('button',{name:'確認儲存',exact:true}).click();await expect.poll(()=>puts.length).toBe(1);expect(puts[0].chemicalAnalysisConfigJson).toBe(JSON.stringify({enabled:true,concentrationFormula:'Primary',decimalPlaces:2}));
});
