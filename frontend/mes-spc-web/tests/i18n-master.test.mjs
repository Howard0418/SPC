import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { createSpcI18n } from '../src/i18n/create.js';
import { flattenCatalog } from '../scripts/check-i18n.mjs';
import { parse as parseSfc } from '@vue/compiler-sfc';
import { baseParse } from '@vue/compiler-dom';
const locales=['zh-TW','zh-CN','en-US'];
const catalogs=Object.fromEntries(locales.map(locale=>[locale,JSON.parse(readFileSync(new URL(`../src/i18n/locales/${locale}.json`,import.meta.url),'utf8'))]));
const flat=Object.fromEntries(locales.map(locale=>[locale,flattenCatalog(catalogs[locale])]));
const review=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05a/translation-review.json',import.meta.url),'utf8'));

test('master draft review metadata, parameters and complete runtime coverage',()=>{
 const pilot=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task04/translation-review.json',import.meta.url),'utf8'));
 const keys=review.metadata.map(row=>row.key);assert.equal(keys.length,110);
 assert.equal(review.ledger.length,330);assert.equal(new Set(review.ledger.map(row=>row.key+row.locale)).size,330);
 const next=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05b/translation-review.json',import.meta.url),'utf8'));
 const types=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05c1/translation-review.json',import.meta.url),'utf8'));
 const chemical=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05c2/translation-review.json',import.meta.url),'utf8'));
 const ppc=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05c3/translation-review.json',import.meta.url),'utf8'));
 const entry=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05d/translation-review.json',import.meta.url),'utf8'));
 const uploads=JSON.parse(readFileSync(new URL('../../../specs/20261025-i18n-task05e/translation-review.json',import.meta.url),'utf8'));
 const segments=JSON.parse(readFileSync(new URL('../../../specs/20261026-i18n-task06a/translation-review.json',import.meta.url),'utf8'));
 const query=JSON.parse(readFileSync(new URL('../../../specs/20261026-i18n-task06b1/translation-review.json',import.meta.url),'utf8'));
 assert.deepEqual([...query.metadata.map(row=>row.key),...segments.metadata.map(row=>row.key),...uploads.metadata.map(row=>row.key),...entry.metadata.map(row=>row.key),...ppc.metadata.map(row=>row.key),...chemical.metadata.map(row=>row.key),...types.metadata.map(row=>row.key),...pilot.metadata.map(row=>row.key),...keys,...next.metadata.map(row=>row.key)].sort(),Object.keys(flat['zh-TW']).sort());
 const sha=value=>createHash('sha256').update(value,'utf8').digest('hex');
 for(const row of review.ledger){
  assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));assert.equal(row.translationHash,sha(flat[row.locale][row.key]));
  assert.equal(row.status,'draft');assert.equal(row.reviewer,null);assert.equal(row.reviewedAt,null);
 }
 for(const row of review.metadata){
  assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));
  const expected=(flat['zh-TW'][row.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort();
  assert.deepEqual(row.placeholders.flatMap(param=>Array(param.count).fill(`{${param.name}}`)).sort(),expected);
 }
});
test('master messages render named values as literal text in all three languages',()=>{
 const instance=createSpcI18n(catalogs);const value='<b>@:key {name}|原文</b>';
 for(const locale of locales){
  instance.global.locale.value=locale;
  for(const key of ['spc.parts.created','spc.parts.updated','spc.parts.deleted','spc.characteristics.created','spc.characteristics.updated','spc.characteristics.deleted']){
   const result=instance.global.t(key,key.startsWith('spc.parts')?{partNo:value}:{name:value});
   assert.ok(result.includes(value),`${locale} ${key}`);
  }
  assert.ok(instance.global.t('spc.parts.confirmDelete',{partNo:value,partName:value}).includes(value));
  assert.ok(instance.global.t('spc.characteristics.confirmDelete',{name:value}).includes(value));
 }
});
for(const view of ['PartsView','CharacteristicsView'])test(`${view}: no remaining fixed Chinese template labels or unknown translation keys`,()=>{
 const source=readFileSync(new URL(`../src/views/${view}.vue`,import.meta.url),'utf8');
 const template=parseSfc(source).descriptor.template.content;const ast=baseParse(template);
 function visit(node){
  if(node.type===2)assert.ok(!/[\u4e00-\u9fff]/.test(node.content),node.content);
  for(const prop of node.props||[])if(prop.type===6&&prop.value)assert.ok(!/[\u4e00-\u9fff]/.test(prop.value.content),prop.value.content);
  for(const child of node.children||[])visit(child);
 }
 visit(ast);
 for(const match of source.matchAll(/['"]((?:common|spc)\.[a-zA-Z.]+)['"]/g))assert.ok(Object.hasOwn(flat['zh-TW'],match[1]),match[1]);
});
