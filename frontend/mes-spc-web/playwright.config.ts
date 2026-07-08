import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    storageState: '.playwright/auth.json'
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npm run dev -- --mode test',
    url: 'http://localhost:5173',
    reuseExistingServer: false, // Force restart to pick up env vars
    env: {
      VITE_AUTH_ENABLED: 'false',
      VITE_API_BASE: 'http://localhost:8082/api'
    }
  },
});
