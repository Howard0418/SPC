import {test,expect} from '@playwright/test';
test.beforeEach(async({page})=>{
 await page.route('http://172.16.110.27/Spc/Launch',r=>r.fulfill({contentType:'text/html',body:'<h1>Portal login</h1>'}));
 await page.route('**:8081/api/**',r=>r.fulfill({contentType:'application/json',body:JSON.stringify({configured:false,sources:[]})}));
});
test('login redirects without AD card',async({page})=>{
 let sawButton=false;
 await page.addInitScript(()=>{new MutationObserver(()=>{if(document.body?.textContent?.includes('使用 AD 帳號或工號登入'))sessionStorage.setItem('sawADCard','yes');}).observe(document,{subtree:true,childList:true});});
 await page.goto('/login');await expect(page).toHaveURL('http://172.16.110.27/Spc/Launch');
 // Revisit the SPC origin without executing app scripts to inspect its session.
 await page.route('**/session-check',r=>r.fulfill({contentType:'text/html',body:'check'}));
 await page.goto('/session-check');sawButton=await page.evaluate(()=>sessionStorage.getItem('sawADCard')==='yes');expect(sawButton).toBe(false);
});
test('SSO returns to original protected path',async({page})=>{
 await page.goto('/equipment-status?from=login');await expect(page).toHaveURL('http://172.16.110.27/Spc/Launch');
 const payload=Buffer.from(JSON.stringify({unique_name:'test',role:'Viewer',permission:['equipment.status'],exp:Math.floor(Date.now()/1000)+600})).toString('base64url');
 await page.goto(`/portal-sso#token=test.${payload}.test`);await expect(page).toHaveURL(/\/equipment-status\?from=login$/);
 expect(await page.evaluate(()=>sessionStorage.getItem('mes_spc_post_sso_redirect'))).toBeNull();
});
test('external return path is not saved',async({page})=>{
 await page.goto('/login?redirect=%2F%2Fevil.invalid');await expect(page).toHaveURL('http://172.16.110.27/Spc/Launch');
 await page.route('**/session-check',r=>r.fulfill({contentType:'text/html',body:'check'}));await page.goto('/session-check');
 expect(await page.evaluate(()=>sessionStorage.getItem('mes_spc_post_sso_redirect'))).toBeNull();
});
