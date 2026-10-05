import {defineConfig} from '@playwright/test';
import base from './playwright.calibration-details.config';
export default defineConfig({...base,testMatch:['calibration-scroll.spec.ts','calibration-date-edit.spec.ts']});
