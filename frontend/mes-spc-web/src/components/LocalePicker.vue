<script setup>
import { inject } from 'vue';
import { useI18n } from 'vue-i18n';
import { localeControlKey } from '../i18n/context';

const { t, locale } = useI18n({ useScope: 'global' });
const changeLocale = inject(localeControlKey);
</script>

<template>
  <label class="flex flex-wrap items-center gap-2 text-xs">
    <span>{{ t('common.language.label') }}</span>
    <select data-testid="locale-select" :aria-label="t('common.language.label')" :value="locale" @change="changeLocale" class="rounded-lg border border-slate-600 bg-slate-800 text-white px-2 py-2 focus:outline focus:outline-2 focus:outline-blue-400">
      <option value="zh-TW">繁體中文</option>
      <option value="zh-CN">简体中文</option>
      <option value="en-US">English</option>
    </select>
  </label>
</template>
