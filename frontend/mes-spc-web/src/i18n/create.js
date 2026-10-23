import { createI18n } from 'vue-i18n';

export function createSpcI18n(messages, development = false) {
  return createI18n({
    legacy: false, locale: 'zh-TW', fallbackLocale: 'zh-TW', messages,
    missingWarn: development, fallbackWarn: development,
    missing(_locale, key) {
      const source = key.split('.').reduce((node, segment) =>
        node && Object.hasOwn(node, segment) ? node[segment] : undefined, messages['zh-TW']);
      if (typeof source === 'string') return undefined; // Let Vue I18n resolve the source fallback.
      console.warn('SPC i18n missing source key:', key);
      return `[${key}]`;
    },
  });
}
