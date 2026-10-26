import {test} from 'node:test';
import assert from 'node:assert/strict';
import {computed,ref} from 'vue';
import {createSpcI18n} from '../src/i18n/create.js';
import {readRuntimeCatalogs} from '../scripts/check-i18n.mjs';
import {captureApiErrorDisplay} from '../src/i18n/apiMessages.js';
import {displayUiMessage} from '../src/i18n/uiMessage.js';
const catalogs=readRuntimeCatalogs(),source=catalogs['zh-TW'];
test('Stored API error updates with locale without a new response and snapshots external arguments',()=>{
 const data={messageKey:'spc.api.invalidField',args:{field:'<b>@:key {field}|raw</b>'}};
 const stored=ref(captureApiErrorDisplay(data,source)),i18n=createSpcI18n(catalogs),text=computed(()=>displayUiMessage(i18n.global.t,stored.value));
 const original=stored.value;data.args.field='mutated';
 for(const locale of ['zh-TW','zh-CN','en-US']){i18n.global.locale.value=locale;assert.equal(text.value,i18n.global.t('spc.api.invalidField',{field:'<b>@:key {field}|raw</b>'}));assert.equal(stored.value,original)}
});
test('Nested local error interpolation receives translated plain text, composite children preserve zero',()=>{
 const raw='<b>@:key {field}</b>',tree=captureApiErrorDisplay({messageKey:'spc.api.importRowInvalid',args:{row:0},details:[{messageKey:'spc.api.invalidField',args:{field:raw}}]},source),i18n=createSpcI18n(catalogs);
 for(const locale of ['zh-TW','zh-CN','en-US']){i18n.global.locale.value=locale;const expected=i18n.global.t('spc.api.importRowInvalid',{row:'0'})+'\n'+i18n.global.t('spc.api.invalidField',{field:raw});assert.equal(displayUiMessage(i18n.global.t,tree),expected);assert.equal(displayUiMessage(i18n.global.t,{key:'spc.groups.loadFailed',args:{error:tree}}),i18n.global.t('spc.groups.loadFailed',{error:expected}))}
});
test('External unknown metadata is never interpreted as a UI descriptor; invalid trees preserve legacy fallback',()=>{
 const t=()=>assert.fail('Unknown external input must not translate');
 for(const data of [{messageKey:'common.actions.refresh'},{messageKey:'spc.api.invalidField',args:{field:{key:'common.actions.refresh'}}}])assert.equal(captureApiErrorDisplay(data,source),null);
 const bad={messageKey:'spc.api.unexpected',args:{},message:'legacy <b>@:key</b>',details:[{messageKey:'common.actions.refresh'}]};
 assert.equal(displayUiMessage(t,captureApiErrorDisplay(bad,source)),bad.message);
 const cycle={messageKey:'spc.api.unexpected',args:{},details:[]};cycle.details.push(cycle);
 assert.equal(displayUiMessage(key=>key,captureApiErrorDisplay(cycle,source)),'spc.api.unexpected');
 assert.equal(displayUiMessage(t,'raw <b>@:key {field}</b>'),'raw <b>@:key {field}</b>');assert.equal(displayUiMessage(t,null),'');
});
