import test from 'node:test';
import assert from 'node:assert/strict';
import { resolveApiBase } from '../src/utils/apiBase.js';
const production='https://PMR-SPC-SRV.pmr.com.tw:8443/api';
test('Test always remains same origin despite configured legacy or production URLs',()=>{
 for(const env of ['test','unknown',undefined])for(const url of [production,'http://172.16.110.27:8081/api','https://evil.example/api','/api'])assert.equal(resolveApiBase(env,url),'/api');
});
test('Production permits only explicit same origin or existing formal API',()=>{
 assert.equal(resolveApiBase('production',production),production);
 assert.equal(resolveApiBase('production',production+'/'),production);
 assert.equal(resolveApiBase('production','/api'),'/api');
 assert.equal(resolveApiBase('production',''),'/api');
});
test('Production rejects off target or credential bearing endpoints',()=>{
 for(const url of ['https://evil.example/api','http://172.16.110.27:8081/api','http://172.16.119.140:8081/other','http://u:p@172.16.119.140:8081/api','http://PMR-SPC-SRV.pmr.com.tw:8443/api',production+'?x=1',production+'#x','//PMR-SPC-SRV.pmr.com.tw:8443/api','https://172.16.119.140:8443/api','not a URL'])assert.throws(()=>resolveApiBase('production',url),/正式 API/);
});
