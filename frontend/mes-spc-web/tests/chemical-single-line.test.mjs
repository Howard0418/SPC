import test from 'node:test';
import assert from 'node:assert/strict';
import {
  useSingleChemicalLine,
  stageLabel,
  normalizeChemicalShift,
  isRecordedChemicalShift,
  shiftLabel,
  chartShiftLabel,
  chartStageShiftLabel,
  buildPairedShiftLookup,
  chemicalDateKey
} from '../src/utils/chemicalChart.js';
import fs from 'node:fs';
import vm from 'node:vm';

function renderEChartsCode() {
 const source=fs.readFileSync(new URL('../src/views/SpcChartView.vue',import.meta.url),'utf8');
 return source.slice(source.indexOf('function renderECharts()'),source.indexOf('function renderHistogramChart()'));
}

test('Only N1/N2 CHEM use one sequence',()=>{
  for(const machineCode of ['N1','N2'])assert.equal(useSingleChemicalLine({controlScope:'CHEM',machine:{machineCode}}),true);
  assert.equal(useSingleChemicalLine({controlScope:'CHEM',machineCode:'N2'}),true);
  assert.equal(useSingleChemicalLine({controlScope:'CHEM',machineName:'N2'}),true);
  assert.equal(useSingleChemicalLine({controlScopeName:'藥液',machineCode:'N2'}),true);
  assert.equal(useSingleChemicalLine({scope:'CHEMICAL',machineName:'N1'}),true);
  assert.equal(useSingleChemicalLine({controlScope:'CHEM',machine:{machineCode:'N10'}}),false);
  assert.equal(useSingleChemicalLine({controlScope:'PROCESS',machine:{machineCode:'N2'}}),false);
});
test('Stages remain distinct from shifts',()=>{assert.equal(stageLabel('OPEN'),'開線');assert.equal(stageLabel('CLOSE'),'收線');assert.equal(stageLabel('GENERAL'),'未分階段');});
test('GENERAL and blank shifts count as morning',()=>{
  assert.equal(normalizeChemicalShift('GENERAL'),'OPEN');
  assert.equal(normalizeChemicalShift(''),'OPEN');
  assert.equal(normalizeChemicalShift('OPEN'),'OPEN');
  assert.equal(normalizeChemicalShift('MIDDLE'),'MIDDLE');
  assert.equal(isRecordedChemicalShift('GENERAL'),false);
  assert.equal(isRecordedChemicalShift('OPEN'),true);
  assert.equal(shiftLabel('GENERAL'),'早班');
  assert.equal(shiftLabel('OPEN'),'早班');
});
test('N1/N2 chart labels show open and close as morning stages without changing legacy shift labels',()=>{
  assert.equal(shiftLabel('CLOSE'),'晚班');
  assert.equal(chartShiftLabel('OPEN',true),'早班(開)');
  assert.equal(chartShiftLabel('CLOSE',true),'早(收)');
  assert.equal(chartStageShiftLabel('MIDDLE','OPEN',true),'中(開)');
  assert.equal(chartStageShiftLabel('MIDDLE','CLOSE',true),'中(收)');
  assert.equal(chartShiftLabel('CLOSE',false),'晚班');
});
test('Other-line pairing keeps GENERAL dates on the morning series',()=>{
  const points=[
    {measuredAt:'2026-08-03T09:10:00',samplingPhase:'GENERAL',value:0.22},
    {measuredAt:'2026-09-10T12:00:00',portalDailyDate:'2026-09-10',samplingPhase:'GENERAL',value:1.95},
    {measuredAt:'2026-09-18T12:00:00',portalDailyDate:'2026-09-18',samplingPhase:'OPEN',value:0.24},
    {measuredAt:'2026-09-21T12:00:00',portalDailyDate:'2026-09-21',samplingPhase:'OPEN',value:0.42},
    {measuredAt:'2026-09-22T12:00:00',portalDailyDate:'2026-09-22',samplingPhase:'OPEN',value:0.709}
  ];
  const {dates,lookup}=buildPairedShiftLookup(points);
  assert.equal(dates.length,5);
  assert.equal(lookup.get('2026-08-03:OPEN').value,0.22);
  assert.equal(lookup.get('2026-09-10:OPEN').value,1.95);
  assert.equal(lookup.get('2026-09-22:OPEN').value,0.709);
  assert.equal(useSingleChemicalLine({controlScope:'CHEM',machine:{machineCode:'DP'}}),false);
});
test('Actual chart renderer keeps same-day points in one line and aligns MR',()=>{
 const code=renderEChartsCode();
 const context=Object.fromEntries([...code.matchAll(/\b(\w+)\.value\b/g)].map(m=>[m[1],{value:false}]));
 let option;
 const points=[['OPEN','OPEN',1],['CLOSE','CLOSE',3],['MIDDLE','CLOSE',7]].map(([samplingPhase,samplingStage,value])=>({measuredAt:'2026-09-11T00:00:00',samplingPhase,samplingStage,value}));
 Object.assign(context,{useSingleChemicalLine,stageLabel,normalizeChemicalShift,isRecordedChemicalShift,shiftLabel,chartShiftLabel,chartStageShiftLabel,chemicalDateKey,buildPairedShiftLookup,hidePointContextMenu:()=>{},chartInstance:null,chartEl:{value:{}},selectedMapping:{value:{controlScope:'CHEM',machine:{machineCode:'N2'}}},chartResult:{value:{chartType:'I-MR',chartData:{points},secondaryChartData:{points:[{value:2},{value:4}]}}},echarts:{init:()=>({setOption:o=>option=o,on:()=>{}})},router:{replace:()=>{}},route:{path:'/spc',query:{}},renderHistogramChart:()=>{},formatNumber:String});
 vm.runInNewContext(code+';renderECharts();',context);
 const top=option.series.filter(s=>s.yAxisIndex===0);
 assert.equal(top.length,1);assert.deepEqual(Array.from(top[0].data,p=>p.value),[1,3,7]);
 assert.equal(option.xAxis[0].data.length,3);assert.equal(option.xAxis[1].data.length,2);
 const tooltip=option.tooltip.formatter([{axisValue:'09/11 收線',data:top[0].data[2],seriesName:'Individual',color:'blue'}]);
 assert.match(tooltip,/中\(收\)/);assert.match(tooltip,/收線/);
 const closeTooltip=option.tooltip.formatter([{axisValue:'09/11 收線',data:top[0].data[1],seriesName:'Individual',color:'blue'}]);
 assert.match(closeTooltip,/早\(收\)/);
 assert.doesNotMatch(closeTooltip,/晚班/);
});
test('DP mixed GENERAL and OPEN still draws every morning date',()=>{
 const code=renderEChartsCode();
 const context=Object.fromEntries([...code.matchAll(/\b(\w+)\.value\b/g)].map(m=>[m[1],{value:false}]));
 let option;
 const points=[
  {measuredAt:'2026-08-03T09:10:00',samplingPhase:'GENERAL',value:0.22},
  {measuredAt:'2026-09-10T12:00:00',portalDailyDate:'2026-09-10',samplingPhase:'GENERAL',value:1.95},
  {measuredAt:'2026-09-18T12:00:00',portalDailyDate:'2026-09-18',samplingPhase:'OPEN',value:0.24},
  {measuredAt:'2026-09-21T12:00:00',portalDailyDate:'2026-09-21',samplingPhase:'OPEN',value:0.42},
  {measuredAt:'2026-09-22T12:00:00',portalDailyDate:'2026-09-22',samplingPhase:'OPEN',value:0.709}
 ];
 Object.assign(context,{useSingleChemicalLine,stageLabel,normalizeChemicalShift,isRecordedChemicalShift,shiftLabel,chartShiftLabel,chartStageShiftLabel,chemicalDateKey,buildPairedShiftLookup,hidePointContextMenu:()=>{},chartInstance:null,chartEl:{value:{}},selectedMapping:{value:{controlScope:'CHEM',machine:{machineCode:'DP'}}},chartResult:{value:{chartType:'I-MR',chartData:{points},secondaryChartData:{points:[]}}},echarts:{init:()=>({setOption:o=>option=o,on:()=>{}})},router:{replace:()=>{}},route:{path:'/spc',query:{}},renderHistogramChart:()=>{},formatNumber:String});
 vm.runInNewContext(code+';renderECharts();',context);
 const morning=option.series.find(s=>s.name==='早班');
 assert.ok(morning);
 assert.deepEqual(Array.from(morning.data,p=>p&&p.value),[0.22,1.95,0.24,0.42,0.709]);
 assert.equal(option.xAxis[0].data.length,5);
});
