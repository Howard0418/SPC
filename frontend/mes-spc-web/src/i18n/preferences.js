export const supportedLocales = Object.freeze(['zh-TW', 'zh-CN', 'en-US']);
export const normalizeLocale = value => supportedLocales.includes(value) ? value : 'zh-TW';
const validSubject = subject => subject == null || (Number.isSafeInteger(subject) && subject > 0);

// Display preference only. This key never supplies identity or authorization.
export function localeKey(environment, subject) {
  const identity = Number.isSafeInteger(subject) && subject > 0 ? `operator-${subject}` : 'anonymous';
  return `spc:locale:v1:${encodeURIComponent(environment)}:${identity}`;
}

export function readLocale(storage, environment, subject) {
  if (!validSubject(subject)) return { locale: 'zh-TW', available: false };
  try {
    return { locale: normalizeLocale(storage.getItem(localeKey(environment, subject))), available: true };
  } catch {
    return { locale: 'zh-TW', available: false };
  }
}

export function saveLocale(storage, environment, subject, locale) {
  if (!validSubject(subject) || !supportedLocales.includes(locale)) return false;
  try {
    storage.setItem(localeKey(environment, subject), locale);
    return true;
  } catch {
    return false;
  }
}
