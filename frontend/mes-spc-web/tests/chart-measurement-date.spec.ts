import {test,expect} from '@playwright/test';
import {prepare} from './fixtures/chart-query';

for(const path of ['/spc','/trend-chart'])test(`${path}: midnight axis and tooltip omit time while real time, points and samples remain`,async({page})=>{
  const {writes}=await prepare(page,path,true);
  const points=[{measuredAt:'2026-09-29T00:00:00',value:.94,xbar:.94,n:25,variableMeasurementId:701,lotNo:'ETCH:2026-09-29:QE1',violatedRules:[]},
    {measuredAt:'2026-10-13T12:34:56',value:1.3,xbar:1.3,n:25,variableMeasurementId:702,violatedRules:[]}];
  await page.route('**/api/**',async route=>{
    if(!new URL(route.request().url()).pathname.endsWith('/spc/chart'))return route.fallback();
    return route.fulfill({json:{chartType:'XBAR_S',subgroupSize:25,normality:{pValue:.5,testName:'Anderson-Darling'},limits:{usl:1.5,lsl:.5,target:1,ucl:1.4,cl:1,lcl:.6},
      chartData:{points},secondaryChartData:{points:points.map(p=>({...p,value:.1}))},rawDataPoints:points,capability:{sigmaOverall:.1,pp:1.2,ppk:1.1}}});
  });
  await page.reload();
  const chart=page.getByTestId(path==='/spc'?'primary-spc-chart':'chemical-trend-chart');
  await expect(chart.locator('canvas').first()).toBeVisible();
  const inspect=()=>page.evaluate(async({path})=>{
    const source=await(await fetch(`/src/views/${path==='/spc'?'SpcChartView':'TrendChartView'}.vue`)).text();
    const modulePath=source.match(/from ["']([^"']*echarts[^"']*)["']/)?.[1];const ec=await import(modulePath!);
    const instance=ec.getInstanceByDom(document.querySelector(path==='/spc'?'[data-testid="primary-spc-chart"]':'[data-testid="chemical-trend-chart"]'));
    const o=instance.getOption(),datum=o.series[0].data[0];
    return {labels:o.xAxis[0].data,values:o.series[0].data.map(d=>d.value),n:datum.meta?.n,
      tooltip:o.tooltip[0].formatter([{axisValue:o.xAxis[0].data[0],data:datum,seriesName:o.series[0].name,color:'#fff'}])};
  },{path});
  await expect.poll(async()=> (await inspect()).labels[0]).toBe('09/29');
  const result=await inspect();expect(result.labels[1]).toContain('12:34');expect(result.values).toEqual([.94,1.3]);
  expect(result.tooltip).not.toMatch(/00:00(?::00)?/);expect(result.tooltip).toContain('09/29');
  if(path==='/spc'){expect(result.tooltip).toContain('2026/9/29');expect(result.n).toBe(25)}
  expect(writes).toEqual([]);
});
