<script setup>
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import * as XLSX from "xlsx";
import { Download, FlaskConical, RefreshCw, Save, Search, Settings2 } from "lucide-vue-next";
import { api, getApiErrorMessage } from "../api/client";

const router = useRouter();
const loading = ref(false);
const saving = ref(false);
const error = ref("");
const message = ref("");
const items = ref([]);
const line = ref("");
const tank = ref("");
const status = ref("all");
const formulaDrafts = ref({});

const formulaKeys = [
  "concentrationFormula",
  "adjustmentFormula",
  "adjustmentAmountFormula",
  "decimalPlaces"
];

function configOf(item) {
  try { return JSON.parse(item.chemicalAnalysisConfigJson || "{}"); } catch { return {}; }
}
function draftOfConfig(config) {
  return {
    concentrationFormula: String(config.concentrationFormula || ""),
    adjustmentFormula: String(config.adjustmentFormula || ""),
    adjustmentAmountFormula: String(config.adjustmentAmountFormula || ""),
    decimalPlaces: Number.isFinite(Number(config.decimalPlaces)) ? Number(config.decimalPlaces) : 2
  };
}
function normalizeDraft(draft) {
  return {
    concentrationFormula: String(draft?.concentrationFormula || "").trim(),
    adjustmentFormula: String(draft?.adjustmentFormula || "").trim(),
    adjustmentAmountFormula: String(draft?.adjustmentAmountFormula || "").trim(),
    decimalPlaces: Math.min(8, Math.max(0, Number.isFinite(Number(draft?.decimalPlaces)) ? Number(draft.decimalPlaces) : 2))
  };
}
function hasDraftChange(item) {
  const draft = formulaDrafts.value[item.id];
  if (!draft) return false;
  const original = draftOfConfig(configOf(item));
  const current = normalizeDraft(draft);
  return formulaKeys.some(key => String(original[key] ?? "") !== String(current[key] ?? ""));
}
function rowOf(item) {
  const config = configOf(item);
  const draft = formulaDrafts.value[item.id] || draftOfConfig(config);
  const normalizedDraft = normalizeDraft(draft);
  const formulaMissing = !config.enabled || !normalizedDraft.concentrationFormula;
  const limitsMissing = item.lsl == null && item.usl == null && item.targetValue == null;
  return {
    id: item.id,
    raw: item,
    line: item.machine?.machineCode || item.process?.processCode || "未設定",
    tank: item.tank?.tankName || item.tank?.tankCode || "未設定",
    tankCode: item.tank?.tankCode || "",
    chemical: item.characteristic?.characteristicName || item.characteristic?.characteristicCode || "未命名",
    enabled: item.isEnabled === true,
    secondary: String(config.secondaryInputLabel || "").trim(),
    formulaMissing,
    limitsMissing,
    draft,
    formula: normalizedDraft.concentrationFormula,
    adjustmentFormula: normalizedDraft.adjustmentFormula,
    adjustmentAmountFormula: normalizedDraft.adjustmentAmountFormula,
    decimalPlaces: normalizedDraft.decimalPlaces,
    dirty: hasDraftChange(item),
    issue: formulaMissing || limitsMissing
  };
}
const chemicalRows = computed(() => items.value
  .filter(x => String(x.controlScope || "").toUpperCase() === "CHEM")
  .map(rowOf));
const lines = computed(() => [...new Set(chemicalRows.value.map(x => x.line))].sort());
const tanks = computed(() => [...new Set(chemicalRows.value.filter(x => !line.value || x.line === line.value).map(x => x.tank))].sort());
const filtered = computed(() => chemicalRows.value.filter(x =>
  (!line.value || x.line === line.value) && (!tank.value || x.tank === tank.value) &&
  (status.value === "all" || status.value === "enabled" && x.enabled || status.value === "issues" && x.issue || status.value === "secondary" && !!x.secondary)
));
const summaries = computed(() => lines.value.map(currentLine => {
  const rows = chemicalRows.value.filter(x => x.line === currentLine);
  return { line: currentLine, tanks: new Set(rows.map(x => x.tank)).size, enabled: rows.filter(x => x.enabled).length, disabled: rows.filter(x => !x.enabled).length, secondary: rows.filter(x => !!x.secondary).length, issues: rows.filter(x => x.issue).length };
}));
const dirtyRows = computed(() => chemicalRows.value.filter(x => x.dirty));

async function load(clearStatus = true) {
  loading.value = true;
  if (clearStatus) { error.value = ""; message.value = ""; }
  try {
    const { data } = await api.get("/part-process-characteristics");
    items.value = Array.isArray(data) ? data : [];
    formulaDrafts.value = Object.fromEntries(items.value
      .filter(x => String(x.controlScope || "").toUpperCase() === "CHEM")
      .map(x => [x.id, draftOfConfig(configOf(x))]));
  }
  catch (e) { error.value = getApiErrorMessage(e); }
  finally { loading.value = false; }
}
function showLine(value) { line.value = value; tank.value = ""; }
function edit(row) { router.push({ path: "/part-process-characteristics", query: { editId: row.id } }); }
function resetRow(row) { formulaDrafts.value[row.id] = draftOfConfig(configOf(row.raw)); }
function buildConfigJson(row) {
  return JSON.stringify({ ...configOf(row.raw), ...normalizeDraft(row.draft) });
}
function buildSavePayload(row) {
  const raw = row.raw;
  return {
    controlScope: raw.controlScope,
    partId: raw.partId,
    processId: raw.processId,
    machineId: raw.machineId,
    tankId: raw.tankId,
    slotId: raw.slotId,
    characteristicId: raw.characteristicId,
    sequenceNo: raw.sequenceNo,
    unit: raw.unit,
    usl: raw.usl,
    lsl: raw.lsl,
    ucl: raw.ucl,
    cl: raw.cl,
    lcl: raw.lcl,
    targetValue: raw.targetValue,
    sampleSize: raw.sampleSize,
    displayMode: raw.displayMode,
    chartTypeId: raw.chartTypeId,
    formulaConfigJson: raw.formulaConfigJson,
    chemicalAnalysisConfigJson: buildConfigJson(row),
    isRequired: raw.isRequired,
    isEnabled: raw.isEnabled
  };
}
async function saveChanges() {
  const rows = dirtyRows.value;
  error.value = ""; message.value = "";
  if (!rows.length) {
    message.value = "目前沒有需要儲存的公式變更。";
    return;
  }
  const preview = rows.slice(0, 8).map(x => `- ${x.line} / ${x.tank} / ${x.chemical}`).join("\n");
  const more = rows.length > 8 ? `\n...另有 ${rows.length - 8} 筆` : "";
  if (!window.confirm(`即將儲存 ${rows.length} 筆藥液公式變更：\n${preview}${more}\n\n是否繼續？`)) return;

  saving.value = true;
  try {
    for (const row of rows) {
      await api.put(`/part-process-characteristics/${row.id}`, buildSavePayload(row));
    }
    message.value = `已儲存 ${rows.length} 筆藥液公式變更。`;
    await load(false);
  } catch (e) {
    error.value = getApiErrorMessage(e);
  } finally {
    saving.value = false;
  }
}
function exportExcel() {
  const rows = filtered.value.map(x => ({ 線別: x.line, 槽體: x.tank, 槽體代碼: x.tankCode, 分析項目: x.chemical, 狀態: x.enabled ? "啟用" : "停用", 滴定值2: x.secondary || "無", 濃度公式: x.formula || "未設定", 調整公式: x.adjustmentFormula || "", 調整量公式: x.adjustmentAmountFormula || "", 小數位: x.decimalPlaces, 是否變更: x.dirty ? "已變更" : "未變更", 設定警示: x.formulaMissing ? "缺濃度公式" : x.limitsMissing ? "缺規格" : "完整" }));
  const book = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(book, XLSX.utils.json_to_sheet(rows), "藥液分析項目");
  XLSX.writeFile(book, `SPC_藥液分析項目總覽_${new Date().toISOString().slice(0, 10)}.xlsx`);
}
onMounted(load);
</script>

<template>
  <section class="space-y-6">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div><h1 class="text-2xl font-black text-slate-800 dark:text-white flex items-center gap-2"><FlaskConical class="w-7 h-7 text-emerald-500" /> 線別分析項目總覽</h1><p class="text-sm text-slate-500 mt-1">統計已設定的 CHEM 藥液管制項目；啟用項目即為量測頁可填寫的分析項目。</p></div>
      <div class="flex flex-wrap gap-2"><button @click="load" :disabled="loading || saving" class="btn-outline"><RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" /> 重新整理</button><button @click="saveChanges" :disabled="saving || !dirtyRows.length" class="btn-primary" :class="{ 'opacity-60': saving || !dirtyRows.length }"><Save class="w-4 h-4" /> 儲存變更 {{ dirtyRows.length ? `(${dirtyRows.length})` : '' }}</button><button @click="exportExcel" class="btn-primary"><Download class="w-4 h-4" /> 匯出 Excel</button></div>
    </header>
    <div v-if="error" class="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{{ error }}</div>
    <div v-if="message" class="rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700">{{ message }}</div>
    <div class="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4"><button v-for="item in summaries" :key="item.line" @click="showLine(item.line)" class="rounded-2xl border bg-white p-4 text-left shadow-sm hover:border-emerald-400 dark:bg-slate-800 dark:border-slate-700"><div class="flex justify-between"><strong>{{ item.line }}</strong><span class="text-xs text-slate-500">{{ item.tanks }} 槽</span></div><div class="mt-3 text-3xl font-black text-emerald-600">{{ item.enabled }}<span class="ml-1 text-sm text-slate-500">啟用項目</span></div><div class="mt-2 text-xs text-slate-500">滴定值 2：{{ item.secondary }}　停用：{{ item.disabled }}　<span :class="item.issues ? 'text-amber-600 font-bold' : ''">警示：{{ item.issues }}</span></div></button></div>
    <div class="rounded-2xl border bg-white p-4 shadow-sm dark:bg-slate-800 dark:border-slate-700"><div class="grid gap-3 md:grid-cols-4"><select v-model="line" @change="tank=''" class="field"><option value="">全部線別</option><option v-for="value in lines" :key="value">{{ value }}</option></select><select v-model="tank" class="field"><option value="">全部槽體</option><option v-for="value in tanks" :key="value">{{ value }}</option></select><select v-model="status" class="field"><option value="all">全部狀態</option><option value="enabled">僅啟用</option><option value="secondary">僅有滴定值 2</option><option value="issues">僅設定警示</option></select><div class="flex items-center text-sm text-slate-500"><Search class="mr-2 w-4 h-4" />顯示 {{ filtered.length }} 個項目</div></div></div>
    <div class="overflow-x-auto rounded-2xl border bg-white shadow-sm dark:bg-slate-800 dark:border-slate-700"><table class="min-w-[1180px] text-sm"><thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-900"><tr><th>線別</th><th>槽體</th><th>分析項目</th><th>狀態</th><th>濃度公式</th><th>調整公式</th><th>調整量公式</th><th>小數位</th><th>設定狀態</th><th></th></tr></thead><tbody><tr v-for="item in filtered" :key="item.id" class="border-t dark:border-slate-700" :class="item.dirty ? 'bg-amber-50/70 dark:bg-amber-950/20' : ''"><td>{{ item.line }}</td><td><div>{{ item.tank }}</div><small class="text-slate-400">{{ item.tankCode }}</small></td><td class="font-bold"><div>{{ item.chemical }}</div><small v-if="item.secondary" class="text-slate-400">滴定值 2：{{ item.secondary }}</small></td><td><span :class="item.enabled ? 'badge-green' : 'badge-gray'">{{ item.enabled ? '啟用' : '停用' }}</span><span v-if="item.dirty" class="ml-2 badge-amber">已變更</span></td><td><textarea v-model="item.draft.concentrationFormula" rows="2" class="formula-field" placeholder="濃度公式"></textarea></td><td><textarea v-model="item.draft.adjustmentFormula" rows="2" class="formula-field" placeholder="調整公式"></textarea></td><td><textarea v-model="item.draft.adjustmentAmountFormula" rows="2" class="formula-field" placeholder="調整量公式"></textarea></td><td><input v-model.number="item.draft.decimalPlaces" type="number" min="0" max="8" class="field w-20" /></td><td><span :class="item.issue ? 'text-amber-600 font-bold' : 'text-emerald-600'">{{ item.formulaMissing ? '缺濃度公式' : item.limitsMissing ? '缺規格' : '完整' }}</span></td><td><div class="flex flex-col gap-2"><button @click="edit(item)" class="text-blue-600 hover:underline"><Settings2 class="inline w-4 h-4" /> 編輯</button><button v-if="item.dirty" @click="resetRow(item)" class="text-slate-500 hover:underline">還原</button></div></td></tr><tr v-if="!loading && !filtered.length"><td colspan="10" class="p-8 text-center text-slate-500">沒有符合條件的藥液分析項目。</td></tr></tbody></table></div>
  </section>
</template>

<style scoped>
th,td{padding:.8rem 1rem;vertical-align:top}.field,.formula-field{width:100%;border:1px solid #cbd5e1;border-radius:.65rem;padding:.55rem .7rem;background:transparent}.formula-field{min-width:14rem;resize:vertical;line-height:1.35}.btn-primary,.btn-outline{display:inline-flex;align-items:center;gap:.4rem;border-radius:.7rem;padding:.55rem .8rem;font-size:.875rem;font-weight:700}.btn-primary{background:#059669;color:white}.btn-outline{border:1px solid #94a3b8}.badge-green,.badge-gray,.badge-amber{border-radius:9999px;padding:.2rem .55rem;font-size:.75rem;font-weight:700;white-space:nowrap}.badge-green{background:#d1fae5;color:#047857}.badge-gray{background:#e2e8f0;color:#475569}.badge-amber{background:#fef3c7;color:#b45309}
</style>
