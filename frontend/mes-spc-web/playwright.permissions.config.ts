import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  testMatch: 'navigation-permissions.spec.ts',
  workers: 1,
  use: { baseURL: 'http://127.0.0.1:5174', screenshot: 'only-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'npm run dev -- --mode permissions --host 127.0.0.1 --port 5174',
    url: 'http://127.0.0.1:5174',
    reuseExistingServer: false,
    env: { VITE_AUTH_ENABLED: 'true', VITE_API_BASE: '/api', VITE_APP_ENV: 'test' }
  }
});
