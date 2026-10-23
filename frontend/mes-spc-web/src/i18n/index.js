import { createSpcI18n } from './create';
import zhTW from './locales/zh-TW.json';
import zhCN from './locales/zh-CN.json';
import enUS from './locales/en-US.json';

export const i18n = createSpcI18n({ 'zh-TW': zhTW, 'zh-CN': zhCN, 'en-US': enUS }, import.meta.env.DEV);
