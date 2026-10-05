import { defineConfig } from '@playwright/test';
import base from './playwright.calibration.config';
const port = process.env.CALIBRATION_TEST_PORT || '5191';
export default defineConfig({ ...base, testMatch: ['calibration-import.spec.ts','calibration-date-edit.spec.ts'],
  use: { ...base.use, baseURL: `http://127.0.0.1:${port}` },
  webServer: { ...base.webServer, command: `node node_modules/vite/bin/vite.js --mode testhost --host 127.0.0.1 --port ${port} --strictPort`, url: `http://127.0.0.1:${port}` }
});
