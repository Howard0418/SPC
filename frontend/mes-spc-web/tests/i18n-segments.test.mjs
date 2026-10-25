import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {parse,compileTemplate} from '@vue/compiler-sfc';
import {parse as parseDom} from '@vue/compiler-dom';
for(const file of ['ChartSegmentDetails.vue','ChemicalCalculationDetails.vue'])test(`${file}: translated fixed text, attributes and valid template`,()=>{
 const result=parse(readFileSync(new URL('../src/components/'+file,import.meta.url),'utf8'));assert.deepEqual(result.errors,[]);
 assert.deepEqual(compileTemplate({source:result.descriptor.template.content,filename:file,id:file}).errors,[]);
 function visit(n){if(n.type===2)assert.ok(!/[\u4e00-\u9fff]/.test(n.content),n.content);for(const p of n.props||[])if(p.type===6&&p.value)assert.ok(!/[\u4e00-\u9fff]/.test(p.value.content),p.value.content);for(const c of n.children||[])visit(c)}
 visit(parseDom(result.descriptor.template.content));
});

import {createHash} from 'node:crypto';
import {flattenCatalog} from '../scripts/check-i18n.mjs';
import {createSpcI18n} from '../src/i18n/create.js';
const catalogs=Object.fromEntries(['zh-TW','zh-CN','en-US'].map(locale=>[locale,JSON.parse(readFileSync(new URL(`../src/i18n/locales/${locale}.json`,import.meta.url),'utf8'))]));
const flat=Object.fromEntries(Object.entries(catalogs).map(([l,c])=>[l,flattenCatalog(c)]));
const review=JSON.parse(readFileSync(new URL('../../../specs/20261026-i18n-task06a/translation-review.json',import.meta.url),'utf8'));
test('Segment and evidence draft provenance, protected tokens and parameter contracts',()=>{
 const sha=v=>createHash('sha256').update(v,'utf8').digest('hex');assert.equal(review.metadata.length,104);assert.equal(review.ledger.length,312);assert.equal(new Set(review.ledger.map(r=>r.key+r.locale)).size,312);
 for(const r of review.ledger){assert.equal(r.sourceHash,sha(flat['zh-TW'][r.key]));assert.equal(r.translationHash,sha(flat[r.locale][r.key]));assert.equal(r.status,'draft');assert.equal(r.uncertain,true);assert.equal(r.reviewer,null);assert.equal(r.reviewedAt,null)}
 for(const r of review.metadata){assert.equal(r.sourceHash,sha(flat['zh-TW'][r.key]));assert.deepEqual(r.placeholders.flatMap(p=>Array(p.count).fill(`{${p.name}}`)).sort(),(flat['zh-TW'][r.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort());for(const l of Object.keys(catalogs))for(const token of r.protectedTokens)assert.ok(flat[l][r.key].includes(token),l+' '+r.key+' '+token)}
});
test('Evidence parameters remain literal data, including zero',()=>{
 const i=createSpcI18n(catalogs),raw='<b>@:key {name}|原式</b>';
 for(const l of Object.keys(catalogs)){i.global.locale.value=l;for(const [key,args] of [['spc.segmentDetails.note',{note:raw}],['spc.segmentDetails.reason',{reason:raw}],['spc.calculationEvidence.reason',{reason:raw}],['spc.calculationEvidence.inputs',{primary:raw,secondary:0}]])assert.ok(i.global.t(key,args).includes(raw));assert.ok(i.global.t('spc.calculationEvidence.cell',{address:'A1',value:0}).includes('0'))}
});
