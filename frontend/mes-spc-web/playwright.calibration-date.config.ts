import base from './playwright.calibration.config';
import { defineConfig } from '@playwright/test';

export default defineConfig({ ...base,
  testMatch: ['calibration-import.spec.ts', 'calibration-date-edit.spec.ts'],
  outputDir: 'test-results/calibration-date-edit'
});
