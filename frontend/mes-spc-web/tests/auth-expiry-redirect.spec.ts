import {test,expect} from '@playwright/test';

test('401 during SSO grace still returns expired user to Portal login',async({page})=>{
  await page.route('https://portaltest.pmr.com.tw/Login?ReturnUrl=%2FSpc%2FLaunch',route=>
    route.fulfill({contentType:'text/html',body:'<h1>Portal login</h1>'}));
  await page.addInitScript(()=>{
    if(sessionStorage.getItem('auth-expiry-test-seeded'))return;
    sessionStorage.setItem('auth-expiry-test-seeded','yes');
    sessionStorage.removeItem('auth-expiry-error-rendered');
    localStorage.setItem('mes_spc_token','expired-token');
    localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'expired'}));
    sessionStorage.setItem('mes_spc_sso_grace_until',String(Date.now()+8000));
    window.addEventListener('DOMContentLoaded',()=>{
      const detect=()=>{
        const text=document.body?.innerText||'';
        if(text.includes('未授權 (401)')||text.includes('無法載入檢驗項目基準及管制圖配置'))
          sessionStorage.setItem('auth-expiry-error-rendered','yes');
      };
      new MutationObserver(detect).observe(document.body,{childList:true,subtree:true,characterData:true});
      detect();
    });
  });
  await page.route('**/*',route=>{
    const url=new URL(route.request().url());
    if(url.pathname.startsWith('/api/'))return route.fulfill({status:401,json:{message:'未授權'}});
    return route.continue();
  });

  await page.goto('/spc?dimension=CHEM#overall');

  await expect(page).toHaveURL(/\/login\?redirect=%2Fspc%3Fdimension%3DCHEM%23overall$/,{timeout:5000});
  expect(await page.evaluate(()=>localStorage.getItem('mes_spc_token'))).toBeNull();
  expect(await page.evaluate(()=>sessionStorage.getItem('auth-expiry-error-rendered'))).toBeNull();
});

test('a successful parallel API response cancels the short SSO redirect',async({page})=>{
  await page.addInitScript(()=>{
    localStorage.setItem('mes_spc_token','valid-token');
    localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'valid'}));
    sessionStorage.setItem('mes_spc_sso_grace_until',String(Date.now()+8000));
  });
  let first=true;
  await page.route('**/*',route=>{
    const url=new URL(route.request().url());
    if(!url.pathname.startsWith('/api/'))return route.continue();
    if(first){first=false;return route.fulfill({status:401,json:{message:'暫時未授權'}});}
    return route.fulfill({status:200,json:[]});
  });

  await page.goto('/spc');
  await page.waitForTimeout(600);

  await expect(page).toHaveURL(/\/spc$/);
  expect(await page.evaluate(()=>localStorage.getItem('mes_spc_token'))).toBe('valid-token');
});
