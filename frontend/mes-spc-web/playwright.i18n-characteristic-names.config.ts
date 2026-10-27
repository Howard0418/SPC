import {defineConfig} from '@playwright/test';
const target=process.env.SPC_I18N_TEST_URL;
if(target&&new URL(target).origin.toLowerCase()!=='https://spctest.pmr.com.tw')throw Error('Only SPC test site allowed');
export default defineConfig({testDir:'./tests',testMatch:['i18n-master.spec.ts','i18n-ppc.spec.ts','i18n-entry.spec.ts'],grep:/characteristic name switch/,workers:1,reporter:'list',timeout:30000,outputDir:'../../.codex-tmp/i18n-char-a/playwright',webServer:target?undefined:{command:'node node_modules/vite/bin/vite.js preview --host 127.0.0.1 --port 5241 --strictPort',url:'http://127.0.0.1:5241',reuseExistingServer:false},use:{baseURL:target||'http://127.0.0.1:5241',headless:true,ignoreHTTPSErrors:true,timezoneId:'Asia/Taipei'}});
