import test from 'node:test';
import assert from 'node:assert/strict';
import { formatNormalityPValue, normalityConclusion, normalityUnavailableReason } from '../src/utils/normalityPresentation.js';
test('p-value formatting preserves tiny positive values and nulls',()=>{
 for(const [v,text] of [[null,'N/A'],[undefined,'N/A'],[NaN,'N/A'],[Infinity,'N/A'],[-1,'N/A'],[2,'N/A'],['0.2','N/A'],[0,'0.0000'],[0.000001,'<0.0001'],[0.0001,'0.0001'],[0.05,'0.0500'],[1,'1.0000']])assert.equal(formatNormalityPValue(v),text);
});
test('conclusion uses unrounded p-value, unavailable is never normal',()=>{
 assert.equal(normalityConclusion({pValue:0.0499999}), '偏離常態');
 assert.equal(normalityConclusion({pValue:0.05}), '未拒絕常態假設');
 assert.equal(normalityConclusion({pValue:1}), '未拒絕常態假設');
 for(const result of [null,{}, {pValue:null,isNormal:true}])assert.equal(normalityConclusion(result),'無法檢定');
});
test('unavailable reason distinguishes insufficient and constant data',()=>{
 assert.match(normalityUnavailableReason(null),/至少3筆/);
 assert.equal(normalityUnavailableReason({pValue:null,note:'數據無變異'}),'數據無變異');
 assert.equal(normalityUnavailableReason({pValue:0.4}), '');
});
