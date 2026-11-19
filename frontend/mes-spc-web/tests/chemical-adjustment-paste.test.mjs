import test from 'node:test';
import assert from 'node:assert/strict';
import { parseAdjustmentPaste, positiveOptional } from '../src/utils/chemicalAdjustmentPaste.js';

test('依中文標題辨識貼上欄位並保留備註', () => {
  const parsed = parseAdjustmentPaste('藥液\t槽容\t添加管制值\t稀釋管制值\t備註\n硫酸\t664\t18\t23\t測試');
  assert.deepEqual(parsed.fields, ['chemicalName', 'tankCapacityLiters', 'additionControlValue', 'dilutionControlValue', 'adjustmentRemark']);
  assert.deepEqual(parsed.rows[0], ['硫酸', '664', '18', '23', '測試']);
});

test('無標題時依固定五欄順序解析', () => {
  const parsed = parseAdjustmentPaste('硫酸\t7000\t170\t190\t');
  assert.equal(parsed.rows.length, 1);
  assert.equal(parsed.fields[0], 'chemicalName');
});

test('管制數值必須為正數且空白可省略', () => {
  assert.equal(positiveOptional(''), undefined);
  assert.equal(positiveOptional('18.5'), 18.5);
  assert.equal(Number.isNaN(positiveOptional('0')), true);
  assert.equal(Number.isNaN(positiveOptional('錯誤')), true);
});
