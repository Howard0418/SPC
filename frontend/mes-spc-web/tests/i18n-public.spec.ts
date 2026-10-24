import {test,expect} from '@playwright/test';
for(const path of ['/control-chart-groups?tab=types','/control-chart-types'])test(`public route remains unchanged: ${path}`,async({page})=>{
 const errors:string[]=[];page.on('pageerror',e=>errors.push(e.message));
 await page.addInitScript(()=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'route-fixture'}))});
 await page.route('**/*',route=>{const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();if(!u.pathname.startsWith('/api/'))return route.continue();expect(route.request().method()).toBe('GET');return route.fulfill({json:u.pathname.endsWith('/version')?{version:'0.1.72',environment:'test'}:[]})});
 await page.goto(path);
 if(path.includes('groups')){await expect(page.getByTestId('type-add')).toHaveCount(0);await expect(page).toHaveURL(/control-chart-groups\?tab=types/)}
 else await expect(page).toHaveURL(/part-process-characteristics/);
 await expect(page.locator('main')).toBeVisible();expect(errors).toEqual([]);
});
