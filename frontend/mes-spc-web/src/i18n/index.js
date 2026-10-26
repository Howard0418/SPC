import { createSpcI18n } from './create';
import zhTW from './locales/zh-TW.json';
import zhCN from './locales/zh-CN.json';
import enUS from './locales/en-US.json';
import apiZhTW from './api/zh-TW.json';
import apiZhCN from './api/zh-CN.json';
import apiEnUS from './api/en-US.json';
import { composeCatalogs } from './catalogModules';

export const apiMessageSource = apiZhTW;
export const i18n = createSpcI18n({
 'zh-TW': composeCatalogs(zhTW,apiZhTW),
 'zh-CN': composeCatalogs(zhCN,apiZhCN),
 'en-US': composeCatalogs(enUS,apiEnUS)
}, import.meta.env.DEV);
