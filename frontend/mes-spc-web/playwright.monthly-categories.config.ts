import { defineConfig } from '@playwright/test';
const target = process.env.SPC_MONTHLY_TEST_URL;
if (target && !['http://127.0.0.1:5198', 'https://spctest.pmr.com.tw'].includes(new URL(target).origin.toLowerCase())) throw Error('Only local or SPC test site allowed');
export default defineConfig({
  testDir: './tests', testMatch: ['monthly-categories.spec.ts', 'i18n-chart-query.spec.ts'], workers: 1,
  outputDir: '../../.codex-tmp/monthly-categories-20261028/playwright',
  reporter: 'list', use: { baseURL: target || 'http://127.0.0.1:5198', headless: true, timezoneId: 'Asia/Taipei', ignoreHTTPSErrors: true },
  webServer: target ? undefined : { command: 'node node_modules/vite/bin/vite.js --mode test --host 127.0.0.1 --port 5198 --strictPort', url: 'http://127.0.0.1:5198', reuseExistingServer: false,
    env: { VITE_AUTH_ENABLED: 'false', VITE_API_BASE: '/api' } }
});
