import { test } from 'node:test';
import assert from 'node:assert/strict';
import { displayUiMessage } from '../src/i18n/uiMessage.js';
test('preserve original API text and empty messages',()=>{
 const t=()=>{throw Error('raw API text must not be compiled')};
 assert.equal(displayUiMessage(t,'原 API <b>@:key {value}</b>'),'原 API <b>@:key {value}</b>');
 assert.equal(displayUiMessage(t,null),'');assert.equal(displayUiMessage(t,''),'');
});
test('resolve message at display time without changing parameters',()=>{
 let language='A';const args={name:'原文 <b>@:key {name}</b>'};const message={key:'spc.test.saved',args};
 const t=(key,values)=>`${language}:${key}:${values.name}`;
 assert.equal(displayUiMessage(t,message),'A:spc.test.saved:原文 <b>@:key {name}</b>');
 language='B';assert.equal(displayUiMessage(t,message),'B:spc.test.saved:原文 <b>@:key {name}</b>');
 assert.deepEqual(message,{key:'spc.test.saved',args});
});
