import { test, expect, Page } from '@playwright/test';

let normalityFixture: any;
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
  const writes: string[] = []; const calls: URL[] = []; const chartMethods: string[] = []; const errors: string[] = [];
  page.on('pageerror', e => errors.push(e.message));
  await page.addInitScript(() => {
    localStorage.setItem('mes_spc_token', 'mock-ui-only-token'); // All API calls are intercepted below.
    localStorage.setItem('mes_spc_user', JSON.stringify({ role: 'Viewer' }));
  });
  await page.route('**/*', async route => {
    const u = new URL(route.request().url());
    if (u.origin !== new URL(test.info().project.use.baseURL as string).origin) return route.abort();
    if (!u.pathname.startsWith('/api/')) return route.continue();
    if (route.request().method() !== 'GET') writes.push(route.request().method() + u.pathname);
    let json: any = [];
    if (u.pathname.endsWith('/version')) json = { environment: 'test', version: '0.1.72' };
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
      chartMethods.push(u.searchParams.get('normalityMethod') || '');
      const points = [1, 5, 10, 11, 15, 20].map((day, i) => ({ measuredAt: `2026-10-${String(day).padStart(2, '0')}T12:00:00`, value: 8 + i * .2, variableMeasurementId: pointN?500+i:undefined, n:pointN||undefined, violatedRules: [] }));
      json = { chartType, normality: normalityFixture, normalCurve:[{x:8,scaledPdf:1},{x:9,scaledPdf:2}], limits: { usl: 99, lsl: 1, ucl: 18, cl: 10, lcl: 2 }, chartData: { points },
        secondaryChartData: { points: [] }, rawDataPoints: points, specLimitSegments: empty ? [] : segments, subgroupSize: 1 };
    }
    return route.fulfill({ json });
  });
  await page.goto(`${pagePath}?ppcId=1&uploadBatchId=00000000-0000-0000-0000-000000000001&startDate=2026-10-01&endDate=2026-10-20`);
  const region = page.getByRole('region', { name: '日期分段與版本明細', exact: true });
  await expect(region).toBeVisible();
  return { region, writes, calls, errors, chartMethods };
}


for(const path of ['/spc','/trend-chart'])for(const scenario of ['normal','tiny','constant','insufficient']) {
 test(`${path} ${scenario} fixed AD`,async({page})=>{
  await page.setViewportSize(scenario==='tiny'?{width:390,height:844}:{width:1366,height:900});
  if(scenario==='tiny')await page.emulateMedia({colorScheme:'dark'});
  const p=scenario==='tiny'?0.000001:scenario==='constant'?null:0.4;
  normalityFixture=scenario==='insufficient'?null:{testName:'Anderson-Darling',pValue:p,isNormal:p===null?true:p>=.05,skewness:123,kurtosis:456,methodNote:'本系統採 Anderson-Darling 常態檢定。',note:scenario==='constant'?'數據無變異，無法進行常態性檢定。':null};
  const {writes,errors,chartMethods}=await setup(page,false,true,'I_MR',path);
  await expect(page.getByTestId('normality-method')).toHaveText('Anderson-Darling');
  expect(chartMethods.length).toBeGreaterThan(0);expect(chartMethods.every(x=>x==='AD')).toBeTruthy();
  await expect(page.locator('option[value="AD"]')).toHaveCount(0);
  await expect(page.getByText(/Skewness|Kurtosis|Adjusted Jarque-Bera/)).toHaveCount(0);
  if(scenario==='normal')await expect(page.getByText('未拒絕常態假設',{exact:true})).toBeVisible();
  if(scenario==='tiny')await expect(page.getByText('<0.0001',{exact:true})).toBeVisible();
  if(scenario==='constant')await expect(page.getByTestId('normality-unavailable')).toContainText('無變異');
  if(scenario==='insufficient')await expect(page.getByTestId('normality-unavailable')).toContainText('至少3筆');
  if(scenario==='normal') {
    // I18N-TASK-04: shell language changes must not recreate chart instances or query state.
    const chartNodes=page.locator('[_echarts_instance_]');
    await expect(chartNodes.first()).toBeVisible();
    const before=await chartNodes.evaluateAll(nodes=>nodes.map(node=>node.getAttribute('_echarts_instance_')));
    const dates=await page.locator('input[type="date"]').evaluateAll(nodes=>nodes.map(node=>(node as HTMLInputElement).value));
    const currentUrl=page.url();const requestCount=chartMethods.length;
    for(const locale of ['en-US','zh-CN','zh-TW']) {
      await page.getByTestId('locale-select').selectOption(locale);
      expect(await chartNodes.evaluateAll(nodes=>nodes.map(node=>node.getAttribute('_echarts_instance_')))).toEqual(before);
      expect(await page.locator('input[type="date"]').evaluateAll(nodes=>nodes.map(node=>(node as HTMLInputElement).value))).toEqual(dates);
      expect(page.url()).toBe(currentUrl);expect(chartMethods.length).toBe(requestCount);
      const conclusion=({'en-US':'Normality hypothesis not rejected','zh-CN':'未拒绝常态假设','zh-TW':'未拒絕常態假設'}[locale]);
      await expect(page.getByText(conclusion,{exact:true})).toBeVisible();
    }
  }
  expect(writes).toEqual([]);expect(errors).toEqual([]);
  if(scenario==='normal')await page.screenshot({path:`test-results/normality-${path.slice(1)}.png`,fullPage:true});
 });
}
