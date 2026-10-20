import { test, expect, Page } from '@playwright/test';

const segments = [
  { id: 11, startDate: '2026-10-01T00:00:00', endDate: '2026-10-10T23:59:59', usl: 15, lsl: 2 },
  { id: 12, startDate: '2026-10-11T00:00:00', endDate: null, usl: 20, lsl: 3 }
];
const config = JSON.stringify({ concentrationFormula: 'Primary * 2', adjustmentFormula: 'Primary > 2', adjustmentAmountFormula: 'Primary * 3' });
function detail(id: number | null, chemical = true, skip = 0) {
  const segment = segments.find(s => s.id === id);
  return { partProcessCharacteristicId: 1, segmentId: id, startDate: segment?.startDate, endDate: segment?.endDate,
    limits: { USL: { value: segment?.usl ?? 99, source: id ? 'Segment' : 'Base' }, LSL: { value: 1, source: 'Base' },
      Target: { value: 8, source: 'Base' }, UCL: { value: null, source: 'Base' }, CL: { value: 8, source: 'Segment' }, LCL: { value: null, source: 'Base' } },
    currentFormulaConfigJson: chemical ? config : null, formulaVersionStatus: chemical ? 'Unknown' : 'NotApplicable',
    formulaHistory: chemical ? [{ id: skip + 1, versionNo: skip ? 1 : 21, configJson: config, reason: 'WI-CA-001｜Rev.03', changedBy: '驗收人員', changedAt: '2026-10-19T16:00:00Z', changeType: 'Update' }] : [],
    specificationHistory: [{ id: 1, versionNo: 3, snapshotJson: '{"USL":15}', changedAt: '2026-10-19T00:00:00Z', changedBy: '工程師', changeType: 'Update' }],
    hasMoreFormula: !skip, hasMoreSpecification: false };
}
async function setup(page: Page, chemical = true, empty = false, chartType = 'I_MR', pagePath = '/spc', pointN = 0) {
  const writes: string[] = []; const calls: URL[] = []; const errors: string[] = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.addInitScript(() => localStorage.setItem('mes_spc_user', JSON.stringify({ role: 'Viewer' })));
  await page.route('**/*', async route => {
    const u = new URL(route.request().url());
    if (u.hostname !== '127.0.0.1') return route.abort();
    if (!u.pathname.startsWith('/api/')) return route.continue();
    if (route.request().method() !== 'GET') writes.push(route.request().method() + u.pathname);
    let json: any = [];
    if (u.pathname.endsWith('/version')) json = { environment: 'test', version: '0.1.63' };
    else if (u.pathname.endsWith('/chemical-calculation-traces')) json = {data:{applicable:chemical,totalMeasurements:0,unknownMeasurements:0,multipleFormulaVersions:false,rows:[],hasMore:false,hasMoreTrace:false}};
    else if (u.pathname.endsWith('/chart-segment-details')) {
      calls.push(u); json = { success: true, data: detail(u.searchParams.has('segmentId') ? Number(u.searchParams.get('segmentId')) : null, chemical, Number(u.searchParams.get('formulaSkip'))) };
      if (empty) { json.data.formulaHistory = []; json.data.specificationHistory = []; json.data.hasMoreFormula = false; }
    } else if (u.pathname.endsWith('/control-limit-segments')) json = empty ? [] : segments;
    else if (u.pathname.endsWith('/part-process-characteristics')) json = [{ id: 1, controlScope: chemical ? 'CHEM' : 'PRODUCT',
      processId: 1, partId: chemical ? null : 1, characteristicId: 1, chartTypeId: 1, isEnabled: true,
      displayMode: pagePath === '/trend-chart' ? 'TREND_CHART' : 'CONTROL_CHART', chemicalAnalysisConfigJson: config, machine: { machineCode: 'N1' },
      process: { processName: '測試製程' }, characteristic: { characteristicName: '濃度', dataCategory: 'Variable' } }];
    else if (u.pathname.endsWith('/control-chart-types')) json = [{ id: 1, chartTypeCode: 'I_MR', dataCategory: 'Variable' }];
    else if (u.pathname.endsWith('/control-chart-groups')) json = [{ id: 1, groupCode: chemical ? 'CHEM' : 'PRODUCT', groupType: 'CONTROL_CHART', businessScopeCode: chemical ? 'CHEM' : 'PRODUCT', isEnabled: true }];
    else if (u.pathname.endsWith('/spc/chart')) {
      const points = [1, 5, 10, 11, 15, 20].map((day, i) => ({ measuredAt: `2026-10-${String(day).padStart(2, '0')}T12:00:00`, value: 8 + i * .2, variableMeasurementId: pointN?500+i:undefined, n:pointN||undefined, violatedRules: [] }));
      json = { chartType, limits: { usl: 99, lsl: 1, ucl: 18, cl: 10, lcl: 2 }, chartData: { points },
        secondaryChartData: { points: [] }, rawDataPoints: points, specLimitSegments: empty ? [] : segments, subgroupSize: 1 };
    }
    return route.fulfill({ json });
  });
  await page.goto(`${pagePath}?ppcId=1&uploadBatchId=00000000-0000-0000-0000-000000000001&startDate=2026-10-01&endDate=2026-10-20`);
  const region = page.getByRole('region', { name: '日期分段與版本明細', exact: true });
  await expect(region).toBeVisible();
  return { region, writes, calls, errors };
}

test('date selection shows sources and audit evidence without claiming actual formula', async ({ page }) => {
  const { region, writes, errors } = await setup(page);
  await region.getByRole('button', { name: /2026-10-01/ }).click();
  await expect(region.getByTestId('segment-range')).toContainText('分段 #11');
  await expect(region.getByText('USL：15', { exact: true })).toBeVisible();
  await expect(region.getByText('來源：目前主檔回退值').first()).toBeVisible();
  await region.locator('summary').filter({ hasText: '公式修改 v21' }).click();
  await expect(region.getByText('參照文件／修改依據：WI-CA-001｜Rev.03')).toBeVisible();
  await expect(region.getByText(/當時實際使用的公式版本：無法確認當時版本/)).toBeVisible();
  await expect(region.getByText(/F 表使用版本：無法確認/)).toBeVisible();
  await region.getByRole('button', { name: /2026-10-11/ }).click();
  await expect(region.getByTestId('segment-range')).toContainText('分段 #12');
  await expect(region.getByText('USL：20', { exact: true })).toBeVisible();
  expect(writes).toEqual([]); expect(errors).toEqual([]);
});

test('chemical empty history and list failure retry remain readable', async ({ page }) => {
  const { region, writes } = await setup(page, true, true);
  await region.getByRole('button', { name: '主檔（未命中分段時）' }).click();
  await expect(region.getByText('沒有公式版本紀錄；不補造歷史。')).toBeVisible();
  await region.getByRole('button', { name: '關閉明細' }).click();
  await expect(region.getByTestId('segment-range')).toHaveCount(0);
  let fail = true;
  await page.route('**/api/control-limit-segments**', route => fail
    ? route.fulfill({ status: 503, json: { message: '分段清單暫停服務' } }) : route.fulfill({ json: segments }));
  await page.goto('/spc?ppcId=1&uploadBatchId=00000000-0000-0000-0000-000000000001&startDate=2026-10-01&endDate=2026-10-20');
  await expect(region.getByRole('alert')).toContainText('分段清單暫停服務');
  fail = false; await region.getByRole('button', { name: '重試分段清單' }).click();
  await expect(region.getByRole('button', { name: /2026-10-01/ })).toBeVisible();
  expect(writes).toEqual([]);
});

test('chemical trend chart reuses the same readonly segment and formula details', async ({ page }) => {
  const { region, writes, errors } = await setup(page, true, false, 'I_MR', '/trend-chart');
  await region.getByRole('button', { name: /2026-10-01/ }).click();
  await expect(region.getByTestId('segment-range')).toContainText('分段 #11');
  await expect(region.getByText(/當時實際使用的公式版本：無法確認/)).toBeVisible();
  await expect(region.getByText(/圖上的分段規格線/)).toHaveCount(0);
  expect(writes).toEqual([]); expect(errors).toEqual([]);
});

for (const chartType of ['XBAR_R', 'XBAR_S', 'P', 'U']) {
  test(`segment detail remains available on ${chartType}`, async ({ page }) => {
    const { region, errors, writes } = await setup(page, false, false, chartType);
    await region.getByRole('button', { name: /2026-10-01/ }).click();
    await expect(region.getByTestId('segment-range')).toContainText('分段 #11');
    await expect(region.getByText('藥液分析公式與 F 表：不適用。')).toBeVisible();
    expect(errors).toEqual([]); expect(writes).toEqual([]);
  });
}

test('formula paging keeps segment and clears when selecting another range', async ({ page }) => {
  const { region, calls } = await setup(page);
  await region.getByRole('button', { name: /2026-10-01/ }).click();
  await region.getByRole('button', { name: '公式下一頁' }).click();
  await expect(region.locator('summary').filter({ hasText: '公式修改 v1｜' })).toBeVisible();
  expect(calls.at(-1)?.searchParams.get('segmentId')).toBe('11');
  expect(calls.at(-1)?.searchParams.get('formulaSkip')).toBe('20');
  await expect(region.getByRole('button', { name: '公式下一頁' })).toBeDisabled();
  await region.getByRole('button', { name: /2026-10-11/ }).click();
  await expect(region.locator('summary').filter({ hasText: '公式修改 v21' })).toBeVisible();
  expect(calls.at(-1)?.searchParams.get('formulaSkip')).toBe('0');
});

test('non-chemical and missing histories are explicit on mobile', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const { region, writes, errors } = await setup(page, false, true);
  await expect(region.getByText(/目前沒有日期分段/)).toBeVisible();
  await region.getByRole('button', { name: '主檔（未命中分段時）' }).click();
  await expect(region.getByText('藥液分析公式與 F 表：不適用。')).toBeVisible();
  await expect(region.getByText('沒有規格版本紀錄；不補造歷史。')).toBeVisible();
  expect(await region.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
  expect(writes).toEqual([]); expect(errors).toEqual([]);
});

test('failure retries without old detail; slower request cannot overwrite new segment', async ({ page }) => {
  const { region, writes } = await setup(page);
  let fail = true;
  await page.route('**/api/v1/part-process-characteristics/1/chart-segment-details**', async route => {
    const u = new URL(route.request().url());
    if (fail) return route.fulfill({ status: 500, json: { message: '明細暫時無法讀取' } });
    if (u.searchParams.get('segmentId') === '11') await new Promise(resolve => setTimeout(resolve, 350));
    return route.fulfill({ json: { data: detail(Number(u.searchParams.get('segmentId'))) } });
  });
  await region.getByRole('button', { name: /2026-10-01/ }).click();
  await expect(region.getByRole('alert')).toContainText('明細暫時無法讀取'); fail = false;
  await region.getByRole('button', { name: '重試明細' }).click();
  await region.getByRole('button', { name: /2026-10-11/ }).click();
  await expect(region.getByTestId('segment-range')).toContainText('分段 #12');
  await page.waitForTimeout(500);
  await expect(region.getByTestId('segment-range')).toContainText('分段 #12');
  expect(writes).toEqual([]);
});

test('chart segment line is clickable and original point drilldown still works', async ({ page }) => {
  const { region, errors } = await setup(page);
  await page.getByLabel('規格界限（USL / Target / LSL）').check();
  await expect(page.getByTestId('primary-spc-chart').locator('canvas')).toBeVisible();
  const coordinates = await page.evaluate(async () => {
    const url = performance.getEntriesByType('resource').map(x => x.name).find(x => /\/echarts\.js\?/.test(x));
    const echarts = await import(url!);
    const instance = echarts.getInstanceByDom(document.querySelector('[data-testid="primary-spc-chart"]'));
    const mark = instance.getOption().series[0].markLine.data[0];
    return { segmentId: mark[0].segmentId, pixel: instance.convertToPixel({ xAxisIndex: 0, yAxisIndex: 0 }, [1, 15]) };
  });
  expect(coordinates.segmentId).toBe(11);
  await page.getByTestId('primary-spc-chart').click({ position: { x: coordinates.pixel[0], y: coordinates.pixel[1] } });
  await expect(region.getByTestId('segment-range')).toContainText('分段 #11');
  // Wait for ECharts' default series animation before clicking a final-position symbol.
  await page.waitForTimeout(1100);
  const pointPixel = await page.evaluate(async () => {
    const url = performance.getEntriesByType('resource').map(x => x.name).find(x => /\/echarts\.js\?/.test(x));
    const echarts = await import(url!);
    return echarts.getInstanceByDom(document.querySelector('[data-testid="primary-spc-chart"]'))
      .convertToPixel({ xAxisIndex: 0, yAxisIndex: 0 }, [1, 8.2]);
  });
  await page.getByTestId('primary-spc-chart').click({ position: { x: pointPixel[0], y: pointPixel[1] } });
  await expect(page.getByText('點位品質追溯詳細資料 (點位 #2)')).toBeVisible();
  expect(errors).toEqual([]);
});

function savedCalc(phase='Initial',version=2){return {phase,phaseVersion:1,evidenceStatus:'Recorded',formulaVersionId:version*10,formulaVersionNo:version,formulaConfigJson:JSON.stringify({concentrationFormula:'Primary*F!B3'}),reason:'WI-SAVED-004',fTableHistoryId:77,effectiveFromUtc:'2026-10-18T16:00:00Z',revisionId:9,primary:'1.23456789',rawResult:'2.46913578',storedResult:'2.47',judgment:'OK',actor:'editor',engineVersion:'portal-excel8-v1',precisionPolicy:'decimal-excel8-away-from-zero-v1',specification:{lsl:'0',usl:'10'},fCells:[{address:'B3',numericValue:'2'}]};}
async function traceFixture(page:Page,pagePath='/spc',pointN=0){
 const state=await setup(page,true,false,pointN>1?'XBAR_R':'I_MR',pagePath,pointN),traceCalls:URL[]=[];
 await page.route('**/api/v1/part-process-characteristics/1/chemical-calculation-traces**',async route=>{
  const u=new URL(route.request().url());traceCalls.push(u);const point=u.searchParams.has('measurementId');
  const rows:any[]=[{measurementId:501,measuredAt:'2026-10-19T04:00:00Z',initialStored:2.47,recheckStored:4.69,samplingPhase:'OPEN',samplingStage:'GENERAL',status:'Recorded',statisticsStatus:'Completed',calculations:u.searchParams.get('traceSkip')==='20'?[savedCalc('Initial',1)]:[savedCalc(),savedCalc('Recheck')]},
   {measurementId:502,measuredAt:'2026-10-18T04:00:00Z',initialStored:99,samplingPhase:'OPEN',samplingStage:'GENERAL',status:'Unknown',statisticsStatus:'NotRecorded',calculations:[]}];
  return route.fulfill({json:{data:{applicable:true,totalMeasurements:point?1:2,unknownMeasurements:point?0:1,multipleFormulaVersions:true,rows:point?[rows[0]]:rows,hasMore:false,hasMoreTrace:point&&u.searchParams.get('traceSkip')!=='20'}}});
 });return {...state,traceCalls};
}
test('saved initial recheck and F snapshots coexist with unknown legacy',async({page})=>{
 const {region,writes}=await traceFixture(page);await region.getByRole('button',{name:/2026-10-01/}).click();const e=region.getByRole('region',{name:'實際公式計算證據'});
 await expect(e).toContainText('保存多個公式版本');await expect(e).toContainText('無法確認當時版本（舊量測未保存追溯）');
 await e.locator('summary').filter({hasText:'初驗追溯'}).click();for(const text of ['WI-SAVED-004','Primary*F!B3','引用 B3 = 2','1.23456789','2.46913578'])await expect(e).toContainText(text);
 await e.locator('summary').filter({hasText:'複驗追溯'}).click();await expect(e).toContainText('F歷史快照 77');expect(writes).toEqual([]);
});
test('exact measurement history pagination and return retain scope',async({page})=>{
 const {region,traceCalls}=await traceFixture(page);await region.getByRole('button',{name:/2026-10-01/}).click();const e=region.getByRole('region',{name:'實際公式計算證據'});
 await e.getByRole('button',{name:'量測 #501 所有計算歷程'}).click();await expect(e).toContainText('此量測已保存');expect(traceCalls.at(-1)?.searchParams.get('measurementId')).toBe('501');
 await e.getByRole('button',{name:'歷程下一頁'}).click();await expect(e.locator('summary')).toContainText('公式 v1');expect(traceCalls.at(-1)?.searchParams.get('traceSkip')).toBe('20');
 await e.getByRole('button',{name:'返回區間量測'}).click();await expect(e).toContainText('未保存版本關聯 1 筆');expect(traceCalls.at(-1)?.searchParams.has('measurementId')).toBe(false);
});
test('trace errors retry and stale response does not replace selected range',async({page})=>{
 const {region}=await setup(page);let fail=true;
 await page.route('**/chemical-calculation-traces**',async route=>{
  const u=new URL(route.request().url());if(fail)return route.fulfill({status:503,json:{message:'計算證據暫時不可用'}});
  if(u.searchParams.get('segmentId')==='11')await new Promise(r=>setTimeout(r,350));
  return route.fulfill({json:{data:{applicable:true,totalMeasurements:Number(u.searchParams.get('segmentId')),unknownMeasurements:0,rows:[],hasMore:false,hasMoreTrace:false}}});
 });
 await region.getByRole('button',{name:/2026-10-01/}).click();const e=region.getByRole('region',{name:'實際公式計算證據'});await expect(e.getByRole('alert')).toContainText('計算證據暫時不可用');fail=false;
 await e.getByRole('button',{name:'重試計算證據'}).click();await region.getByRole('button',{name:/2026-10-11/}).click();await expect(e).toContainText('全部量測 12 筆');await page.waitForTimeout(450);await expect(e).toContainText('全部量測 12 筆');
});

async function clickSavedPoint(page:Page,testId:string){
 await expect(page.getByTestId(testId).locator('canvas')).toBeVisible();await page.waitForTimeout(1100);
 const pixel=await page.evaluate(async testId=>{
  const url=performance.getEntriesByType('resource').map(x=>x.name).find(x=>/\/echarts\.js\?/.test(x));const echarts=await import(url!);
  return echarts.getInstanceByDom(document.querySelector(`[data-testid="${testId}"]`)).convertToPixel({xAxisIndex:0,yAxisIndex:0},[1,8.2]);
 },testId);
 await page.getByTestId(testId).click({position:{x:pixel[0],y:pixel[1]}});
}
for(const path of ['/spc','/trend-chart'])test(`exact measurement evidence opens from chart point ${path}`,async({page})=>{
 const {traceCalls,errors}=await traceFixture(page,path,1);await clickSavedPoint(page,path==='/spc'?'primary-spc-chart':'chemical-trend-chart');
 const e=page.getByRole('region',{name:'實際公式計算證據'});await expect(e).toContainText('此量測已保存');await expect(e).toContainText('量測 #501');
 expect(traceCalls.at(-1)?.searchParams.get('measurementId')).toBe('501');expect(errors).toEqual([]);
});
test('multi-measurement subgroup never claims the first member formula as whole subgroup',async({page})=>{
 const {traceCalls}=await traceFixture(page,'/spc',3);await clickSavedPoint(page,'primary-spc-chart');
 await expect(page.getByRole('region',{name:'實際公式計算證據'})).toContainText('此點為多筆子組');expect(traceCalls).toEqual([]);
});
