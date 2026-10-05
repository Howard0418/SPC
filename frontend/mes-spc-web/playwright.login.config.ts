import {defineConfig} from '@playwright/test';
export default defineConfig({testDir:'./tests',testMatch:'direct-login.spec.ts',workers:1,use:{baseURL:'http://127.0.0.1:5193',headless:true},webServer:{command:'node node_modules/vite/bin/vite.js --mode testhost --host 127.0.0.1 --port 5193 --strictPort',url:'http://127.0.0.1:5193',reuseExistingServer:false}});
