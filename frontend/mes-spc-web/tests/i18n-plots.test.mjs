import {test} from 'node:test';import assert from 'node:assert/strict';import {readFileSync} from 'node:fs';import {createHash} from 'node:crypto';
import {parse,compileTemplate} from '@vue/compiler-sfc';import {parse as parseDom} from '@vue/compiler-dom';
import {flattenCatalog} from '../scripts/check-i18n.mjs';
test('Summary fixed template is translated, full chart and summary templates compile',()=>{
 for(const file of ['components/SpcSummaryTable.vue','views/SpcChartView.vue']){const r=parse(readFileSync(new URL('../src/'+file,import.meta.url),'utf8'));assert.deepEqual(r.errors,[]);assert.deepEqual(compileTemplate({source:r.descriptor.template.content,filename:file,id:file}).errors,[]);if(file.includes('Summary')){function visit(n){if(n.type===2)assert.ok(!/[\u4e00-\u9fff]/.test(n.content),n.content);for(const p of n.props||[])if(p.type===6&&p.value)assert.ok(!/[\u4e00-\u9fff]/.test(p.value.content),p.value.content);for(const c of n.children||[])visit(c)}visit(parseDom(r.descriptor.template.content))}}
});
test('Plot draft review hashes and named parameters match runtime',()=>{
 const flat=Object.fromEntries(['zh-TW','zh-CN','en-US'].map(l=>[l,flattenCatalog(JSON.parse(readFileSync(new URL(`../src/i18n/locales/${l}.json`,import.meta.url),'utf8')))]));const r=JSON.parse(readFileSync(new URL('../../../specs/20261026-i18n-task06b2/translation-review.json',import.meta.url),'utf8'));const sha=v=>createHash('sha256').update(v).digest('hex');assert.equal(r.metadata.length,99);assert.equal(r.ledger.length,297);assert.equal(new Set(r.ledger.map(v=>v.key+v.locale)).size,297);
 for(const row of r.ledger){assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));assert.equal(row.translationHash,sha(flat[row.locale][row.key]));assert.equal(row.status,'draft');assert.equal(row.reviewer,null);assert.equal(row.uncertain,true)}
 for(const row of r.metadata){assert.deepEqual(row.placeholders.flatMap(p=>Array(p.count).fill(`{${p.name}}`)).sort(),(flat['zh-TW'][row.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort());for(const l of Object.keys(flat))for(const token of row.protectedTokens)assert.ok(flat[l][row.key].includes(token),l+' '+row.key+' '+token)}
});
