import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {parse as parseSfc,compileTemplate} from '@vue/compiler-sfc';
import {createSpcI18n} from '../src/i18n/create.js';
import {flattenCatalog} from '../scripts/check-i18n.mjs';
const review=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05c1/translation-review.json',import.meta.url),'utf8'));
const flat=Object.fromEntries(['zh-TW','zh-CN','en-US'].map(locale=>[locale,flattenCatalog(JSON.parse(readFileSync(new URL(`../src/i18n/locales/${locale}.json`,import.meta.url),'utf8')))]));
test('chart type review metadata and draft hashes',()=>{
 const sha=value=>createHash('sha256').update(value,'utf8').digest('hex');
 assert.equal(review.metadata.length,45);assert.equal(review.ledger.length,135);
 assert.equal(new Set(review.ledger.map(row=>row.key+row.locale)).size,135);
 for(const row of review.ledger){
  assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));assert.equal(row.translationHash,sha(flat[row.locale][row.key]));
  assert.equal(row.status,'draft');assert.equal(row.reviewer,null);assert.equal(row.reviewedAt,null);
 }
 for(const row of review.metadata){
  assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));
  assert.deepEqual(row.placeholders.flatMap(p=>Array(p.count).fill(`{${p.name}}`)).sort(),(flat['zh-TW'][row.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort());
 }
});

test('chart type template compiles including dormant branches',()=>{
 const source=readFileSync(new URL('../src/views/ControlChartGroupsView.vue',import.meta.url),'utf8');
 const parsed=parseSfc(source);assert.deepEqual(parsed.errors,[]);
 assert.deepEqual(compileTemplate({source:parsed.descriptor.template.content,filename:'ControlChartGroupsView.vue',id:'chart-types'}).errors,[]);
});
test('chart type translated parameters remain literal data in all languages',()=>{
 const catalogs=Object.fromEntries(Object.keys(flat).map(locale=>[locale,JSON.parse(readFileSync(new URL(`../src/i18n/locales/${locale}.json`,import.meta.url),'utf8'))]));
 const instance=createSpcI18n(catalogs);const value='<b>@:key {name}|原文</b>';
 for(const locale of Object.keys(flat)){
  instance.global.locale.value=locale;
  for(const key of ['created','updated','deleted','confirmDelete','loadRulesFailed'])assert.ok(instance.global.t('spc.chartTypes.'+key,{name:value,error:value}).includes(value));
 }
});
