import test from 'node:test';
import assert from 'node:assert/strict';
import { etchTrendPresentation } from '../src/utils/etchTrendPresentation.js';
const mapping=(line='QE1',side='A')=>({machine:{machineCode:line},characteristic:{characteristicCode:`ETCH_${side}_AVG`,characteristicName:`${side} side 平均咬蝕量`},sampleSize:['QE1','PT1'].includes(line)?25:50});
const chart=n=>({rawDataPoints:Array.from({length:n},(_,i)=>({value:i,variableMeasurementId:i+1})),chartData:{points:[{n,xbar:0,measuredAt:'2026-09-01',lotNo:'first',variableMeasurementId:1}]}});
test('25 and 50 raw samples become one server-calculated average, including zero',()=>{
 for(const line of ['PT1','PT2','QE1','QE2']){const m=mapping(line);const c=chart(m.sampleSize);const p=etchTrendPresentation(c,m);assert.equal(p.points.length,1);assert.equal(p.points[0].value,0);assert.equal(p.sampleSize,m.sampleSize);assert.equal(p.rawSampleCount,m.sampleSize);assert.equal(p.points[0].variableMeasurementId,null);assert.equal(c.rawDataPoints.length,m.sampleSize);}
});
test('same-day distinct server subgroups and separate B side remain separate',()=>{
 const c=chart(25);c.chartData.points.push({...c.chartData.points[0],lotNo:'second',xbar:2});const p=etchTrendPresentation(c,mapping('QE1','B'));assert.equal(p.points.length,2);assert.deepEqual(p.points.map(x=>x.value),[0,2]);
});
test('incomplete or invalid subgroup never falls back to raw points',()=>{
 const c=chart(25);c.chartData.points[0].n=24;assert.equal(etchTrendPresentation(c,mapping()).points.length,0);assert.equal(etchTrendPresentation(c,mapping()).incompleteCount,1);
 c.chartData.points[0].n=25;c.chartData.points[0].xbar=null;assert.equal(etchTrendPresentation(c,mapping()).points.length,0);
 delete c.chartData;assert.equal(etchTrendPresentation(c,mapping()).points.length,0);
});
test('ordinary chemistry, other Xbar items and rate keep raw points unchanged',()=>{
 const c=chart(25);
 for(const m of [{characteristic:{characteristicName:'硫酸'},sampleSize:25},{characteristic:{characteristicCode:'DIMENSION'},sampleSize:25},{...mapping(),characteristic:{characteristicCode:'ETCH_RATE',characteristicName:'咬蝕速率'}}]){const p=etchTrendPresentation(c,m);assert.equal(p.isSubgroup,false);assert.equal(p.points,c.rawDataPoints);}
});
test('mismatched configured sample size rejects display instead of guessing',()=>{
 const m={...mapping('QE1'),sampleSize:50};assert.equal(etchTrendPresentation(chart(50),m).points.length,0);
});
test('legacy characteristic code uses the explicit side name and process line',()=>{
 const m={process:{processCode:'QE1'},characteristic:{characteristicCode:'LEGACY_QE_A',characteristicName:'A side 平均咬蝕量'},sampleSize:25};
 assert.equal(etchTrendPresentation(chart(25),m).points.length,1);
});
