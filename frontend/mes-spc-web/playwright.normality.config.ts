import { defineConfig } from '@playwright/test';
const target=process.env.SPC_NORMALITY_TEST_URL;
if(target && new URL(target).hostname.toLowerCase()!=='spctest.pmr.com.tw')throw new Error('Only SPC test site is allowed');
export default defineConfig({
 testDir:'./tests',testMatch:'normality-fixed-ad.spec.ts',workers:1,reporter:'list',
 webServer:target?undefined:{command:'node node_modules/vite/bin/vite.js --mode test --host 127.0.0.1 --port 5197',url:'http://127.0.0.1:5197',reuseExistingServer:false,env:{VITE_AUTH_ENABLED:'false',VITE_API_BASE:'/api',VITE_APP_ENV:'test'}},
 use:{baseURL:target||'http://127.0.0.1:5197',headless:true,ignoreHTTPSErrors:true}
});
