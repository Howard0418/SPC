import { defineConfig } from '@playwright/test';
export default defineConfig({
  testDir: './tests', testMatch: 'chart-segment-details.spec.ts', workers: 1, reporter: 'list',
  webServer: { command: 'node node_modules/vite/bin/vite.js --mode test --host 127.0.0.1 --port 5191',
    url: 'http://127.0.0.1:5191', reuseExistingServer: true,
    env: { VITE_AUTH_ENABLED: 'false', VITE_API_BASE: '/api', VITE_APP_ENV: 'test' } },
  use: { baseURL: 'http://127.0.0.1:5191', headless: true }
});
