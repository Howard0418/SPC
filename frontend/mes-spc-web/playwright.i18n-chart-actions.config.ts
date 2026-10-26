import {defineConfig} from '@playwright/test';
const target=process.env.SPC_I18N_TEST_URL;
if(target&&new URL(target).origin.toLowerCase()!=='https://spctest.pmr.com.tw')throw Error('Only SPC test site allowed');
export default defineConfig({testDir:'./tests',testMatch:'i18n-chart-actions.spec.ts',workers:1,reporter:'list',timeout:45000,outputDir:'../../.codex-tmp/i18n-task06b3/playwright',webServer:target?undefined:{command:'node node_modules/vite/bin/vite.js --mode testhost --host 127.0.0.1 --port 5210 --strictPort',url:'http://127.0.0.1:5210',reuseExistingServer:false},use:{baseURL:target||'http://127.0.0.1:5210',headless:true,ignoreHTTPSErrors:true,timezoneId:'Asia/Taipei'}});
