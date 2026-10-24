import { defineConfig } from '@playwright/test';
// Test-only build for a dormant branch. Never serves or deploys application routes.
export default defineConfig({
 testDir:'./tests',testMatch:'i18n-chart-types.spec.ts',workers:1,reporter:'list',timeout:45000,
 outputDir:'../../.codex-tmp/i18n-task05c1/playwright',
 webServer:{command:'node node_modules/vite/bin/vite.js preview --config vite.i18n-chart-types.config.ts --host 127.0.0.1 --port 5201 --strictPort',url:'http://127.0.0.1:5201/tests/fixtures/chart-types.html',reuseExistingServer:false},
 use:{baseURL:'http://127.0.0.1:5201',headless:true}
});
