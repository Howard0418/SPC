import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {createHash} from 'node:crypto';
import {baseParse} from '@vue/compiler-dom';
import {parse as parseSfc} from '@vue/compiler-sfc';
import {flattenCatalog} from '../scripts/check-i18n.mjs';
const review=JSON.parse(readFileSync(new URL('../../../specs/20261024-i18n-task05b/translation-review.json',import.meta.url),'utf8'));
const flat=Object.fromEntries(['zh-TW','zh-CN','en-US'].map(locale=>[locale,flattenCatalog(JSON.parse(readFileSync(new URL(`../src/i18n/locales/${locale}.json`,import.meta.url),'utf8')))]));
test('foundation review metadata matches all 188 keys and 564 draft hashes',()=>{
 const sha=value=>createHash('sha256').update(value,'utf8').digest('hex');
 assert.equal(review.metadata.length,188);assert.equal(review.ledger.length,564);
 assert.equal(new Set(review.ledger.map(row=>row.key+row.locale)).size,564);
 for(const row of review.ledger){
  assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));assert.equal(row.translationHash,sha(flat[row.locale][row.key]));
  assert.equal(row.status,'draft');assert.equal(row.reviewer,null);assert.equal(row.reviewedAt,null);
 }
 for(const row of review.metadata){
  assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));
  assert.deepEqual(row.placeholders.flatMap(p=>Array(p.count).fill(`{${p.name}}`)).sort(),(flat['zh-TW'][row.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort());
 }
});
for(const view of ['ProcessesView','TraceabilityMasterView','ControlChartGroupsView','SpcRuleGroupsView'])test(`${view}: translated public template and stable keys`,()=>{
 const source=readFileSync(new URL(`../src/views/${view}.vue`,import.meta.url),'utf8');
 const ast=baseParse(parseSfc(source).descriptor.template.content);
 function visit(node){
  if(node.type===2)assert.ok(!/[\u4e00-\u9fff]/.test(node.content),node.content);
  for(const prop of node.props||[])if(prop.type===6&&prop.value)assert.ok(!/[\u4e00-\u9fff]/.test(prop.value.content),prop.value.content);
  for(const child of node.children||[])visit(child);
 }
 visit(ast);
 for(const match of source.matchAll(/['"]((?:common|spc)\.[a-zA-Z.]+)['"]/g))assert.ok(Object.hasOwn(flat['zh-TW'],match[1]),match[1]);
});
