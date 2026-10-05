import { defineConfig } from '@playwright/test';
import base from './playwright.calibration.config';
export default defineConfig({
  ...base,
  use: { ...base.use, baseURL: 'http://127.0.0.1:5189' },
  webServer: {
    ...base.webServer,
    command: 'node node_modules/vite/bin/vite.js --mode testhost --host 127.0.0.1 --port 5189 --strictPort',
    url: 'http://127.0.0.1:5189'
  }
});
