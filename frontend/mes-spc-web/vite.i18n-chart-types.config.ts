import {defineConfig} from 'vite';
import vue from '@vitejs/plugin-vue';
export default defineConfig({plugins:[vue()],build:{outDir:'../../.codex-tmp/i18n-task05c1/harness-dist',emptyOutDir:true,rollupOptions:{input:'tests/fixtures/chart-types.html'}}});
