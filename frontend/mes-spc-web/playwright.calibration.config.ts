import { defineConfig, devices } from '@playwright/test';

// Isolated local frontend with every API request mocked in the test; never targets IIS.
export default defineConfig({
  testDir: './tests', testMatch: 'calibration-import.spec.ts', workers: 1,
  reporter: 'list', outputDir: 'test-results/calibration-import',
  use: { baseURL: 'http://127.0.0.1:5187', trace: 'retain-on-failure', ...devices['Desktop Chrome'] },
  webServer: {
    command: 'node node_modules/vite/bin/vite.js --mode testhost --host 127.0.0.1 --port 5187 --strictPort',
    url: 'http://127.0.0.1:5187', reuseExistingServer: false,
    env: { VITE_API_BASE: '/api', VITE_APP_ENV: 'test', VITE_AUTH_ENABLED: 'true' }
  }
});
