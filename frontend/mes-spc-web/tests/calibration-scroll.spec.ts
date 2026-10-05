import {test,expect} from '@playwright/test';
for(const width of [1280,390]) test(`100 instruments scroll independently at ${width}px`,async({page})=>{
 await page.setViewportSize({width,height:844});
 await page.addInitScript(()=>{
  localStorage.setItem('mes_spc_token','mock');
  localStorage.setItem('mes_spc_user',JSON.stringify({username:'qa',role:'Editor',permissions:['calibration.manage']}));
 });
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url());if(u.hostname!=='127.0.0.1')return route.abort();
  if(!u.pathname.startsWith('/api/'))return route.continue();
  let json:any={success:true,data:[]};
  if(u.pathname.endsWith('/version'))json={environment:'test',version:'test'};
  if(u.pathname.endsWith('/summary'))json={success:true,data:{windowDays:30}};
  if(u.pathname.endsWith('/import/access'))json={success:true,data:{canManage:true}};
  if(u.pathname.endsWith('/instruments'))json={success:true,total:100,data:Array.from({length:100},(_,i)=>({id:i+1,code:`QA-${i+1}`,name:'量規',department:'品保',usageStatus:'Active',cycleMonths:12,measurementSpecification:'0~150 mm',precision:'0.01 mm',remarks:'備註',calibrationStandard:'SOP',acceptanceCriteria:'±0.02 mm'}))};
  await route.fulfill({json});
 });
 await page.goto('/calibration-instruments');
 const region=page.getByRole('region',{name:'儀器資料表格'});
 await expect(region.getByRole('row')).toHaveCount(101);
 const size=await region.evaluate(e=>({height:e.clientHeight,scroll:e.scrollHeight,width:e.clientWidth,full:e.scrollWidth}));
 if(width===390) expect(size.width).toBeGreaterThan(250);
 expect(size.height).toBeLessThanOrEqual(507);expect(size.scroll).toBeGreaterThan(size.height*3);expect(size.full).toBeGreaterThan(size.width);
 await page.getByRole('button',{name:'儀器表格向右',exact:true}).click();
 await expect.poll(()=>region.evaluate(e=>e.scrollLeft)).toBeGreaterThan(50);
 await region.evaluate(e=>{e.scrollTop=1000;e.scrollLeft=e.scrollWidth});
 await expect.poll(()=>region.evaluate(e=>e.scrollTop)).toBeGreaterThan(900);
 const header=region.locator('thead');
 expect(Math.abs((await header.boundingBox())!.y-(await region.boundingBox())!.y)).toBeLessThan(3);
 expect(await region.evaluate(e=>e.scrollLeft+e.clientWidth>=e.scrollWidth-1)).toBeTruthy();
 await page.getByRole('button',{name:'儀器表格向左',exact:true}).click();
 await expect.poll(()=>region.evaluate(e=>e.scrollLeft)).toBeLessThan(size.full-size.width-20);
 await region.evaluate(e=>{e.scrollLeft=0});
 await region.scrollIntoViewIfNeeded();
 await page.screenshot({path:`test-results/calibration-scroll/${width}.png`});
});
