import { test, expect } from '@playwright/test';

const API_URL = '/api/v1';

test.describe.skip('SPC Rule Engine API Validation', () => {

  test('should expose violatedRules on SPC chart points', async ({ request }) => {
    const mappingsRes = await request.get('/api/part-process-characteristics');
    expect(mappingsRes.ok()).toBeTruthy();

    const mappings = await mappingsRes.json();
    expect(mappings.length).toBeGreaterThan(0);

    let selectedPpc = mappings[0];
    for (const mapping of mappings) {
      const chartProbe = await request.get(`${API_URL}/spc/chart?ppcId=${mapping.id}`);
      if (chartProbe.ok()) {
        selectedPpc = mapping;
        break;
      }
    }
    let seedBatchId: string | undefined;

    const seedRes = await request.post(`${API_URL}/migration/seed-sample-measurements?ppcId=${selectedPpc.id}&count=25`);
    if (seedRes.ok()) {
      const seedJson = await seedRes.json();
      seedBatchId = seedJson.batchId;
    }

    const chartRes = await request.get(`${API_URL}/spc/chart?ppcId=${selectedPpc.id}`);
    expect(chartRes.ok()).toBeTruthy();
    
    const chartData = await chartRes.json();
    
    expect(chartData.chartData).toBeDefined();
    expect(chartData.chartData.points.length).toBeGreaterThan(0);
    
    const points = chartData.chartData.points as any[];

    const samplePoint = points[0];
    expect(samplePoint).toHaveProperty('violatedRules');
    expect(Array.isArray(samplePoint.violatedRules)).toBe(true);

    if (seedBatchId) {
      await request.delete(`${API_URL}/migration/seed-sample-measurements?batchId=${seedBatchId}`);
    }
  });

});
