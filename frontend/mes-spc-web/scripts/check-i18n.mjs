import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

export function parseCatalog(source) {
  const result = JSON.parse(source);
  const stack = [];
  for (const token of source.match(/"(?:\\.|[^"\\])*"|[{}\[\]:,]|[^\s{}\[\]:,]+/g) || []) {
    if (token === '{') stack.push({ keys: new Set(), expectingKey: true });
    else if (token === '[') stack.push({});
    else if (token === '}' || token === ']') stack.pop();
    else {
      const frame = stack.at(-1);
      if (token === ',' && frame?.keys) frame.expectingKey = true;
      else if (token.startsWith('"') && frame?.expectingKey) {
        const key = JSON.parse(token);
        if (frame.keys.has(key)) throw Error(`Duplicate dictionary key: ${key}`);
        frame.keys.add(key);
        frame.expectingKey = false;
      }
    }
  }
  return result;
}

export function flattenCatalog(value, prefix = '', output = {}) {
  if (!value || typeof value !== 'object' || Array.isArray(value) || !Object.keys(value).length) throw Error(`Invalid object: ${prefix}`);
  for (const [key, entry] of Object.entries(value)) {
    if (!/^[a-z][a-zA-Z0-9]*$/.test(key) || ['prototype','constructor','__proto__'].includes(key)) throw Error(`Invalid key: ${key}`);
    const path = prefix ? `${prefix}.${key}` : key;
    if (typeof entry === 'string' && entry.trim()) {
      if (/[<>@|]/.test(entry) || /[{}]/.test(entry.replace(/\{[a-z][a-zA-Z0-9]*\}/g, ''))) throw Error(`Nonportable message: ${path}`);
      output[path] = entry;
    } else flattenCatalog(entry, path, output);
  }
  return output;
}

export function checkCatalogs(catalogs) {
  const flattened = Object.fromEntries(Object.entries(catalogs).map(([locale, data]) => [locale, flattenCatalog(data)]));
  const source = flattened['zh-TW'];
  if (!source) throw Error('Missing zh-TW');
  const keys = Object.keys(source).sort();
  const params = value => (value.match(/\{[a-z][a-zA-Z0-9]*\}/g) || []).sort().join(',');
  for (const [locale, data] of Object.entries(flattened)) {
    if (JSON.stringify(Object.keys(data).sort()) !== JSON.stringify(keys)) throw Error(`Key mismatch: ${locale}`);
    if (keys.some(key => params(source[key]) !== params(data[key]))) throw Error(`Placeholder mismatch: ${locale}`);
  }
  return keys.length;
}

if (process.argv[1] && fileURLToPath(import.meta.url) === process.argv[1]) {
  const catalogs = Object.fromEntries(['zh-TW','zh-CN','en-US'].map(locale => [locale,
    parseCatalog(readFileSync(new URL(`../src/i18n/locales/${locale}.json`, import.meta.url), 'utf8'))]));
  console.log(`I18n catalogs: ${checkCatalogs(catalogs)} keys × 3 locales passed (draft translations).`);
}
