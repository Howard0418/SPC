import { test } from 'node:test';
import assert from 'node:assert/strict';
import { localeKey, readLocale, saveLocale, normalizeLocale } from '../src/i18n/preferences.js';
for (const locale of ['zh-TW','zh-CN','en-US']) test(`accept ${locale}`,()=>assert.equal(normalizeLocale(locale),locale));
for (const value of ['en','EN-us','zh_CN','en-US ',null,{},'__proto__']) test(`reject ${JSON.stringify(value)}`,()=>assert.equal(normalizeLocale(value),'zh-TW'));
test('application environment and user isolated',()=>{
 const keys=[localeKey('test',1),localeKey('production',1),localeKey('test',2),localeKey('test',null)];
 assert.equal(new Set(keys).size,4); assert.equal(localeKey('test',undefined),keys[3]);
 const data=new Map(); const storage={getItem:k=>data.get(k),setItem:(k,v)=>data.set(k,v)};
 assert.equal(saveLocale(storage,'test',1,'en-US'),true);
 assert.equal(readLocale(storage,'test',1).locale,'en-US');
 assert.equal(readLocale(storage,'test',2).locale,'zh-TW');
 assert.equal(readLocale(storage,'test',null).locale,'zh-TW');
 assert.equal(saveLocale(storage,'test',1,'en'),false);
 assert.equal(readLocale(storage,'test',1).locale,'en-US');
});
test('storage exceptions degrade without crash',()=>{
 const storage={getItem(){throw Error('blocked')},setItem(){throw Error('quota')}};
 assert.deepEqual(readLocale(storage,'test',1),{locale:'zh-TW',available:false});
 assert.equal(saveLocale(storage,'test',1,'en-US'),false);
});
test('malformed cached locale rejected',()=>assert.equal(readLocale({getItem:()=>'<script>'},'test',1).locale,'zh-TW'));
test('unbound authenticated subject never reads anonymous cache',()=>{
 const storage={getItem(){throw Error('must not read')},setItem(){throw Error('must not write')}};
 for(const subject of [0,-1,'101',{},NaN]) {
  assert.deepEqual(readLocale(storage,'test',subject),{locale:'zh-TW',available:false});
  assert.equal(saveLocale(storage,'test',subject,'en-US'),false);
 }
});
