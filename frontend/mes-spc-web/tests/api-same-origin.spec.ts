import {test,expect} from '@playwright/test';
test('legacy environment API URL cannot redirect browser calls off origin',async({page})=>{
 const requested:string[]=[];
 await page.addInitScript(()=>localStorage.setItem('mes_spc_user',JSON.stringify({role:'Editor'})));
 await page.route('**/*',route=>{
  const u=new URL(route.request().url());
  if(u.pathname.startsWith('/api/')){
   requested.push(u.href);
   return route.fulfill({json:u.pathname.endsWith('/version')?{version:'test',environment:'test'}:[]});
  }
  if(u.hostname!=='127.0.0.1')return route.abort();
  return route.continue();
 });
 await page.goto('/part-process-characteristics');
 await expect.poll(()=>requested.some(x=>new URL(x).pathname==='/api/part-process-characteristics')).toBe(true);
 expect(requested.filter(x=>new URL(x).origin!=='http://127.0.0.1:5194')).toEqual([]);
});
