import {test} from 'node:test';
import assert from 'node:assert/strict';
import {characteristicDisplayName, summaryCharacteristicDisplayName} from '../src/i18n/characteristicNames.js';

test('characteristic display follows canonical locale, preserves literal names and original records',()=>{
 const raw='比重 <b>@:key {name}</b>',english='Proportion =1+1';
 const row=Object.freeze({characteristicName:raw,characteristicNameEn:english,characteristicCode:'DENSITY'});
 for(const [locale,name] of [['zh-TW',raw],['zh-CN',raw],['en-US',english],['en',raw],['invalid',raw]])assert.equal(characteristicDisplayName(row,locale,'Unnamed'),name);
 assert.equal(row.characteristicName,raw);assert.equal(row.characteristicNameEn,english);
});
test('characteristic missing, whitespace and invalid name values fall back without mutation',()=>{
 for(const absent of [null,undefined,'','  ',{},0]){
  assert.equal(characteristicDisplayName({characteristicName:'比重',characteristicNameEn:absent},'en-US','Unnamed'),'比重');
  assert.equal(characteristicDisplayName({characteristicName:absent,characteristicNameEn:'Proportion'},'zh-CN','Unnamed'),'Proportion');
 }
 assert.equal(characteristicDisplayName({characteristicCode:'C-RAW'},'en-US','Unnamed'),'C-RAW');
 assert.equal(characteristicDisplayName(null,'en-US','Unnamed'),'Unnamed');
 assert.equal(characteristicDisplayName({id:123,characteristicCode:' '},'zh-TW','未命名特性'),'未命名特性');
 assert.equal(characteristicDisplayName({characteristicName:'  比重  '},'en-US','Unnamed'),'  比重  ');
});

test('unnamed characteristic fallback translations retain draft provenance',async()=>{
 const {readFileSync}=await import('node:fs');const {createHash}=await import('node:crypto');
 const ledger=JSON.parse(readFileSync(new URL('../../../specs/20261027-i18n-characteristic-names/translation-review-a.json',import.meta.url),'utf8'));
 const sha=value=>createHash('sha256').update(value).digest('hex');assert.equal(ledger.ledger.length,3);
 for(const entry of ledger.ledger){const text=JSON.parse(readFileSync(new URL(`../src/i18n/locales/${entry.locale}.json`,import.meta.url),'utf8')).spc.characteristics.unnamed;assert.equal(entry.sourceHash,sha('未命名特性'));assert.equal(entry.translationHash,sha(text));assert.equal(entry.status,'draft');assert.equal(entry.reviewer,null)}
});

test('summary display only uses explicit characteristic fields; legacy independent chart title stays literal',()=>{
 const row=Object.freeze({chartName:'Original private title',characteristicName:'比重',characteristicNameEn:'Proportion',characteristicCode:'D0'});
 assert.equal(summaryCharacteristicDisplayName(row,'en-US','Unnamed'),'Proportion');assert.equal(row.chartName,'Original private title');
 assert.equal(summaryCharacteristicDisplayName({chartName:'獨立原圖名 <b>@:key {name}</b>'},'en-US','Unnamed'),'獨立原圖名 <b>@:key {name}</b>');
 assert.equal(summaryCharacteristicDisplayName({characteristicName:'比重',characteristicNameEn:' '},'en-US','Unnamed'),'比重');
});
