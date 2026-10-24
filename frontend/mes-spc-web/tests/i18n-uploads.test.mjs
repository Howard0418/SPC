import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {parse,compileTemplate} from '@vue/compiler-sfc';
import {parse as parseDom} from '@vue/compiler-dom';
for(const path of ['DataUploadView.vue','UploadPreviewView.vue'])test(`${path}: translated fixed labels and valid template`,()=>{
 const source=readFileSync(new URL('../src/views/'+path,import.meta.url),'utf8');const result=parse(source);assert.deepEqual(result.errors,[]);
 assert.deepEqual(compileTemplate({source:result.descriptor.template.content,filename:path,id:path}).errors,[]);
 function visit(node){
  if(node.type===2)assert.ok(!/[\u4e00-\u9fff]/.test(node.content),node.content);
  for(const prop of node.props||[])if(prop.type===6&&prop.value)assert.ok(!/[\u4e00-\u9fff]/.test(prop.value.content),prop.value.content);
  for(const child of node.children||[])visit(child);
 }
 visit(parseDom(result.descriptor.template.content));
});

import {createHash} from 'node:crypto';
import {flattenCatalog} from '../scripts/check-i18n.mjs';
import {createSpcI18n} from '../src/i18n/create.js';
const catalogs=Object.fromEntries(['zh-TW','zh-CN','en-US'].map(locale=>[locale,JSON.parse(readFileSync(new URL(`../src/i18n/locales/${locale}.json`,import.meta.url),'utf8'))]));
const flat=Object.fromEntries(Object.entries(catalogs).map(([locale,cat])=>[locale,flattenCatalog(cat)]));
const review=JSON.parse(readFileSync(new URL('../../../specs/20261025-i18n-task05e/translation-review.json',import.meta.url),'utf8'));
test('Import UI review hashes, placeholders and draft provenance',()=>{
 const sha=value=>createHash('sha256').update(value,'utf8').digest('hex');
 assert.equal(review.metadata.length,89);assert.equal(review.ledger.length,267);assert.equal(new Set(review.ledger.map(row=>row.key+row.locale)).size,267);
 for(const row of review.ledger){assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));assert.equal(row.translationHash,sha(flat[row.locale][row.key]));assert.equal(row.status,'draft');assert.equal(row.reviewer,null);assert.equal(row.reviewedAt,null);assert.equal(row.uncertain,true)}
 for(const row of review.metadata){assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));assert.deepEqual(row.placeholders.flatMap(p=>Array(p.count).fill(`{${p.name}}`)).sort(),(flat['zh-TW'][row.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort());for(const locale of Object.keys(catalogs))for(const token of row.protectedTokens)assert.ok(flat[locale][row.key].includes(token),locale+' '+row.key+' '+token)}
});
test('Import messages preserve literal source parameters including zero',()=>{
 const instance=createSpcI18n(catalogs);const raw='<b>@:key {name}|原文</b>';
 for(const locale of Object.keys(catalogs)){
  instance.global.locale.value=locale;
  assert.ok(instance.global.t('spc.uploads.suggestion',{text:raw}).includes(raw));
  assert.ok(instance.global.t('spc.uploads.duplicate',{status:raw}).includes(raw));
  assert.ok(instance.global.t('spc.uploads.defectTotal',{defect:0,total:0}).includes('0 / 0'));
 }
});
