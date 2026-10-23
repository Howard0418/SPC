import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { createSpcI18n } from '../src/i18n/create.js';
import { parseCatalog, checkCatalogs } from '../scripts/check-i18n.mjs';
import { flattenCatalog } from '../scripts/check-i18n.mjs';
const catalogs = Object.fromEntries(['zh-TW','zh-CN','en-US'].map(locale => [locale,
  parseCatalog(readFileSync(new URL(`../src/i18n/locales/${locale}.json`, import.meta.url), 'utf8'))]));
test('runtime catalogs have matching portable keys',()=>assert.ok(checkCatalogs(catalogs) > 40));
test('pilot review ledger accurately records all draft translation hashes',()=>{
 const review=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task04/translation-review.json',import.meta.url),'utf8'));
 const strings=Object.fromEntries(Object.entries(catalogs).map(([locale,value])=>[locale,flattenCatalog(value)]));
 const sha=value=>createHash('sha256').update(value,'utf8').digest('hex');
 assert.equal(review.metadata.length,47);assert.equal(review.ledger.length,141);
 assert.equal(new Set(review.ledger.map(row=>`${row.key}:${row.locale}`)).size,141);
 for(const row of review.ledger){
  assert.equal(row.sourceHash,sha(strings['zh-TW'][row.key]));assert.equal(row.translationHash,sha(strings[row.locale][row.key]));
  assert.equal(row.status,'draft');assert.equal(row.reviewer,null);assert.equal(row.reviewedAt,null);
 }
 for(const row of review.metadata)assert.equal(row.sourceHash,sha(strings['zh-TW'][row.key]));
});
test('reject duplicate including escaped key',()=>assert.throws(()=>parseCatalog('{"a":1,"\\u0061":2}'),/Duplicate/));
test('allows repeated key in distinct objects',()=>assert.deepEqual(parseCatalog('{"a":{"x":1},"b":{"x":2}}'),{a:{x:1},b:{x:2}}));
for (const value of ['<b>text</b>','@:key','one | many','{bad','']) test(`reject unsupported message ${value}`,()=>{
 assert.throws(()=>checkCatalogs({'zh-TW':{common:{label:value}}}));
});
test('reject missing key and mismatched parameters',()=>{
 assert.throws(()=>checkCatalogs({'zh-TW':{common:{label:'Hello {name}'}},'en-US':{common:{label:'Hello'}}}),/Placeholder/);
 assert.throws(()=>checkCatalogs({'zh-TW':{common:{label:'Hello'}},'en-US':{common:{other:'Other'}}}),/Key/);
});
test('real composer falls back per key and treats parameters as data',()=>{
 const instance=createSpcI18n({'zh-TW':{common:{missing:'繁中預設',hello:'您好 {name}'}},'en-US':{common:{hello:'Hello {name}'}}});
 instance.global.locale.value='en-US';
 assert.equal(instance.global.t('common.missing'),'繁中預設');
 assert.equal(instance.global.t('common.hello',{name:'<script>@:common.missing {name}</script>'}),'Hello <script>@:common.missing {name}</script>');
 instance.global.locale.value='zh-CN';
 assert.equal(instance.global.t('common.missing'),'繁中預設');
});
test('missing source key is visible and diagnosed',()=>{
 const instance=createSpcI18n({'zh-TW':{common:{label:'文字'}}});
 const original=console.warn;const diagnostics=[];console.warn=(...args)=>diagnostics.push(args);
 try {assert.equal(instance.global.t('common.unknown'),'[common.unknown]');assert.equal(diagnostics.length,1);}
 finally {console.warn=original;}
});
