import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {parse,compileTemplate} from '@vue/compiler-sfc';
import {parse as parseDom} from '@vue/compiler-dom';
test('SPC query and monitor template region has translated fixed text and valid full template',()=>{
 const r=parse(readFileSync(new URL('../src/views/SpcChartView.vue',import.meta.url),'utf8'));assert.deepEqual(r.errors,[]);const template=r.descriptor.template.content;
 assert.deepEqual(compileTemplate({source:template,filename:'SpcChartView.vue',id:'spc'}).errors,[]);const boundary=template.indexOf('<!-- Chart Display Box -->');assert.ok(boundary>0);
 function visit(n){if(n.loc?.start.offset>=boundary)return;if(n.type===2)assert.ok(!/[\u4e00-\u9fff]/.test(n.content),n.content);for(const p of n.props||[])if(p.type===6&&p.value)assert.ok(!/[\u4e00-\u9fff]/.test(p.value.content),p.value.content);for(const c of n.children||[])visit(c)}visit(parseDom(template));
});

import {createHash} from 'node:crypto';
import {flattenCatalog} from '../scripts/check-i18n.mjs';
import {createSpcI18n} from '../src/i18n/create.js';
const catalogs=Object.fromEntries(['zh-TW','zh-CN','en-US'].map(l=>[l,JSON.parse(readFileSync(new URL(`../src/i18n/locales/${l}.json`,import.meta.url),'utf8'))]));
const flat=Object.fromEntries(Object.entries(catalogs).map(([l,c])=>[l,flattenCatalog(c)]));
const review=JSON.parse(readFileSync(new URL('../../../specs/20261026-i18n-task06b1/translation-review.json',import.meta.url),'utf8'));
test('Chart query draft provenance, complete parameters and protected terminology',()=>{
 const sha=v=>createHash('sha256').update(v,'utf8').digest('hex');assert.equal(review.metadata.length,70);assert.equal(review.ledger.length,210);assert.equal(new Set(review.ledger.map(r=>r.key+r.locale)).size,210);
 for(const r of review.ledger){assert.equal(r.sourceHash,sha(flat['zh-TW'][r.key]));assert.equal(r.translationHash,sha(flat[r.locale][r.key]));assert.equal(r.status,'draft');assert.equal(r.uncertain,true);assert.equal(r.reviewer,null);assert.equal(r.reviewedAt,null)}
 for(const r of review.metadata){assert.equal(r.sourceHash,sha(flat['zh-TW'][r.key]));assert.deepEqual(r.placeholders.flatMap(p=>Array(p.count).fill(`{${p.name}}`)).sort(),(flat['zh-TW'][r.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort());for(const l of Object.keys(catalogs))for(const token of r.protectedTokens)assert.ok(flat[l][r.key].includes(token),l+' '+r.key+' '+token)}
});
test('Chart read/download errors and zero counts preserve parameters as literal data',()=>{
 const i=createSpcI18n(catalogs),raw='<b>@:key {name}|原文</b>';
 for(const l of Object.keys(catalogs)){i.global.locale.value=l;for(const key of ['loadMappingsFailed','loadSummaryFailed','loadExcludedFailed','loadRemarksFailed','downloadFailed'])assert.ok(i.global.t('spc.chartQuery.'+key,{error:raw}).includes(raw));for(const key of ['pointCount','measurementCount','excludedCount'])assert.ok(i.global.t('spc.chartQuery.'+key,{count:0}).includes('0'))}
});
