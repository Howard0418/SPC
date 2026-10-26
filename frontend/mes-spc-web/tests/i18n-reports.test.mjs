import {test} from 'node:test';import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';import {createHash} from 'node:crypto';
import {readRuntimeCatalogs,flattenCatalog} from '../scripts/check-i18n.mjs';
import {createSpcI18n} from '../src/i18n/create.js';
import {captureApiErrorDisplay} from '../src/i18n/apiMessages.js';
import {displayUiMessage} from '../src/i18n/uiMessage.js';
test('Report labels retain draft provenance, protected tokens and placeholders',()=>{
 const review=JSON.parse(readFileSync(new URL('../../../specs/20261026-i18n-task08e/translation-review.json',import.meta.url),'utf8'));
 const catalogs=readRuntimeCatalogs(),flat=Object.fromEntries(Object.entries(catalogs).map(([l,c])=>[l,flattenCatalog(c)])),sha=value=>createHash('sha256').update(value).digest('hex');
 assert.equal(review.metadata.length,62);assert.equal(review.ledger.length,186);
 for(const row of review.ledger){assert.equal(row.sourceHash,sha(flat['zh-TW'][row.key]));assert.equal(row.translationHash,sha(flat[row.locale][row.key]));assert.equal(row.status,'draft');assert.equal(row.reviewer,null);assert.equal(row.uncertain,true)}
 for(const row of review.metadata)for(const locale of Object.keys(flat)){assert.deepEqual((flat[locale][row.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort(),(flat['zh-TW'][row.key].match(/\{[a-z][a-zA-Z0-9]*\}/g)||[]).sort());for(const term of row.protectedTokens)assert.ok(flat[locale][row.key].includes(term))}

});
