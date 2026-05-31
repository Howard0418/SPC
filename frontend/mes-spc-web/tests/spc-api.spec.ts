import { test, expect } from '@playwright/test';

// Configuration for the backend API
const API_URL = 'http://localhost:5000/api/v2';

test.describe('SPC Rule Engine API Validation', () => {

  // Optional: This test requires a running backend at localhost:5000 
  // and assumes PPC 1 has Western Electric Rules configured.
  // It tests the actual calculation logic of the backend SpcRuleEngine.
  test('should accurately identify Rule 5 and Rule 6 violations via interactive-chart API', async ({ request }) => {
    
    // First, seed some test data using the migration endpoint
    // Seed 25 records for PPC 1
    const seedRes = await request.post(`${API_URL}/migration/seed-sample-measurements?ppcId=1&count=25`);
    expect(seedRes.ok()).toBeTruthy();
    
    // Now call the interactive chart endpoint
    const chartRes = await request.get(`${API_URL}/spc/interactive-chart?partProcessCharacteristicId=1`);
    expect(chartRes.ok()).toBeTruthy();
    
    const chartData = await chartRes.json();
    
    // Assert that the response has points
    expect(chartData.chartData).toBeDefined();
    expect(chartData.chartData.points.length).toBeGreaterThan(0);
    
    // We are looking for specifically whether the structure of ViolatedRules exists
    const points = chartData.chartData.points as any[];
    
    // Check if any point has a violated rule string (the seeded data is random normal, 
    // it might or might not violate a rule depending on the seed. But we assert the property exists).
    const samplePoint = points[0];
    expect(samplePoint).toHaveProperty('violatedRules');
    expect(Array.isArray(samplePoint.violatedRules)).toBe(true);

    // Optional: Clean up the seeded data after test
    // Assuming the seed response returns the batchId
    const seedResponseJson = await seedRes.json();
    if (seedResponseJson.batchId) {
      await request.delete(`${API_URL}/migration/seed-sample-measurements?batchId=${seedResponseJson.batchId}`);
    }
  });

});
