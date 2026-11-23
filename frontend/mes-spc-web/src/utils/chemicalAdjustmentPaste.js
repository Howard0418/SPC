export const editableFields = ["chemicalName", "tankCapacityLiters", "additionControlValue", "dilutionControlValue", "adjustmentRemark"];

const normalize = value => String(value ?? "").trim().toUpperCase().replace(/[\s/／()（）_\-+.]/g, "");
const aliases = new Map([
  ["藥液", "chemicalName"], ["藥液名稱", "chemicalName"], ["槽容", "tankCapacityLiters"], ["槽容L", "tankCapacityLiters"],
  ["添加管制值", "additionControlValue"], ["稀釋管制值", "dilutionControlValue"], ["備註", "adjustmentRemark"]
].map(([name, field]) => [normalize(name), field]));

export function cleanCell(value) {
  return String(value ?? "").replace(/\u3000/g, " ").trim().replace(/\s+/g, " ");
}

export function parseAdjustmentPaste(text) {
  const rows = String(text ?? "").split(/\r?\n/).map(line => line.split("\t").map(cleanCell)).filter(cells => cells.some(Boolean));
  if (!rows.length) return { fields: editableFields, rows: [] };
  const detected = rows[0].map(header => aliases.get(normalize(header)) || null);
  const hasHeader = detected.filter(Boolean).length >= 2;
  return { fields: hasHeader ? detected : editableFields, rows: rows.slice(hasHeader ? 1 : 0) };
}

export function positiveOptional(value) {
  if (value === undefined || value === "") return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : Number.NaN;
}
