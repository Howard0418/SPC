import { defineConfig } from '@playwright/test';
const target = process.env.SPC_I18N_TEST_URL;
if (target && new URL(target).origin.toLowerCase() !== 'https://spctest.pmr.com.tw') throw Error('Only SPC test site is allowed');
export default defineConfig({
  testDir: './tests', testMatch: 'i18n-master.spec.ts', workers: 1, reporter: 'list', timeout: 60000,
  outputDir: '../../.codex-tmp/i18n-task05a/playwright',
  webServer: target ? undefined : {
    command: 'node node_modules/vite/bin/vite.js --mode testhost --host 127.0.0.1 --port 5199 --strictPort',
    url: 'http://127.0.0.1:5199', reuseExistingServer: false,
  },
  use: { baseURL: target || 'http://127.0.0.1:5199', headless: true, ignoreHTTPSErrors: true },
});
