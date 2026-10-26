import {defineConfig} from '@playwright/test';
const target=process.env.SPC_I18N_TEST_URL;
if(target&&new URL(target).origin.toLowerCase()!=='https://spctest.pmr.com.tw')throw Error('Only SPC test site allowed');
export default defineConfig({testDir:'./tests',testMatch:'i18n-chart-query.spec.ts',workers:1,reporter:'list',timeout:45000,outputDir:'../../.codex-tmp/i18n-task06b1/playwright',webServer:target?undefined:{command:'node node_modules/vite/bin/vite.js preview --host 127.0.0.1 --port 5208 --strictPort',url:'http://127.0.0.1:5208',reuseExistingServer:false},use:{baseURL:target||'http://127.0.0.1:5208',headless:true,ignoreHTTPSErrors:true,timezoneId:'Asia/Taipei'}});
