<script setup>
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
const { t } = useI18n({ useScope: "global" });
const messageText = value => displayUiMessage(t, value);
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import * as XLSX from "xlsx";
import { Download, FlaskConical, RefreshCw, Save, Search, Settings2 } from "lucide-vue-next";
import { api, getApiErrorDisplay } from "../api/client";

import ChemicalFormulaHistory from "../components/ChemicalFormulaHistory.vue";

const selectedHistory = ref(null);
const historyRevision = ref(0);
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
const saveResults = ref({});
const editingRows = ref({});
const formulaVersionReason = ref("");
const page = ref(1);
const pageSize = ref(10);

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
    saveResult: saveResults.value[item.id] || null,
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
const totalPages = computed(() => Math.max(1, Math.ceil(filtered.value.length / pageSize.value)));
const safePage = computed(() => Math.min(page.value, totalPages.value));
const pagedRows = computed(() => {
  const start = (safePage.value - 1) * pageSize.value;
  return filtered.value.slice(start, start + pageSize.value);
});
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
    const previousDrafts = formulaDrafts.value;
    const previousResults = saveResults.value;
    formulaDrafts.value = Object.fromEntries(items.value
      .filter(x => String(x.controlScope || "").toUpperCase() === "CHEM")
      .map(x => {
        const failed = previousResults[x.id]?.status === "failed";
        return [x.id, failed && previousDrafts[x.id] ? previousDrafts[x.id] : draftOfConfig(configOf(x))];
      }));
  }
  catch (e) { error.value = getApiErrorDisplay(e); }
  finally { loading.value = false; }
}
function resetPage() { page.value = 1; }
function showLine(value) { line.value = value; tank.value = ""; resetPage(); }
function changePage(nextPage) { page.value = Math.min(totalPages.value, Math.max(1, nextPage)); }
function isEditing(row) { return editingRows.value[row.id] === true; }
function edit(row) { editingRows.value = { ...editingRows.value, [row.id]: !isEditing(row) }; }
function resetRow(row) {
  formulaDrafts.value[row.id] = draftOfConfig(configOf(row.raw));
  const next = { ...saveResults.value };
  delete next[row.id];
  saveResults.value = next;
}
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
    chemicalAnalysisFormulaVersionReason: formulaVersionReason.value.trim() || null,
    isRequired: raw.isRequired,
    isEnabled: raw.isEnabled
  };
}
async function saveChanges() {
  if (saving.value) return;
  const rows = dirtyRows.value;
  error.value = ""; message.value = "";
  saveResults.value = {};
  if (formulaVersionReason.value.trim().length > 500) {
    error.value = { key: "spc.chemicalOverview.reasonTooLong" };
    return;
  }
  if (!rows.length) {
    message.value = { key: "spc.chemicalOverview.noChanges" };
    return;
  }
  const preview = rows.slice(0, 8).map(x => `- ${x.line} / ${x.tank} / ${x.chemical}`).join("\n");
  const more = rows.length > 8 ? "\n" + t("spc.chemicalOverview.moreRows", { count: rows.length - 8 }) : "";
  const reasonText = formulaVersionReason.value.trim() || t("spc.chemicalOverview.notProvided");
  if (!window.confirm(t("spc.chemicalOverview.confirmSave", { count: rows.length, preview, more, reason: reasonText }))) return;

  saving.value = true;
  const payloads = new Map(rows.map(row => [row.id, buildSavePayload(row)]));
  const results = {};
  for (const row of rows) {
    try {
      await api.put(`/part-process-characteristics/${row.id}`, payloads.get(row.id));
      results[row.id] = { status: "saved", message: "已儲存" };
    } catch (e) {
      results[row.id] = { status: "failed", message: getApiErrorDisplay(e) };
    }
  }
  saveResults.value = results;
  const successCount = Object.values(results).filter(x => x.status === "saved").length;
  const failedCount = Object.values(results).filter(x => x.status === "failed").length;
  message.value = { key: "spc.chemicalOverview.saveSummary", args: { saved: successCount, failed: failedCount } };
  if (failedCount > 0) error.value = { key: "spc.chemicalOverview.partialFailure" };
  if (successCount) historyRevision.value++;
  if (!failedCount) formulaVersionReason.value = "";
  await load(false);
  saving.value = false;
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
      <div><h1 class="text-2xl font-black text-slate-800 dark:text-white flex items-center gap-2"><FlaskConical class="w-7 h-7 text-emerald-500" /> {{ t('spc.chemicalOverview.title') }}</h1><p class="text-sm text-slate-500 mt-1">{{ t('spc.chemicalOverview.subtitle') }}</p></div>
      <div class="flex flex-wrap gap-2"><button @click="load" :disabled="loading || saving" class="btn-outline"><RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" /> {{ t('common.actions.refresh') }}</button><button data-testid="overview-save" @click="saveChanges" :disabled="saving || !dirtyRows.length" class="btn-primary" :class="{ 'opacity-60': saving || !dirtyRows.length }"><Save class="w-4 h-4" /> {{ t('spc.chemicalOverview.saveChanges') }} {{ dirtyRows.length ? `(${dirtyRows.length})` : '' }}</button><button data-testid="overview-export" @click="exportExcel" class="btn-primary"><Download class="w-4 h-4" /> {{ t('spc.chemicalOverview.export') }}</button></div>
    </header>
    <div v-if="error" class="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{{ messageText(error) }}</div>
    <div v-if="message" class="rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700">{{ messageText(message) }}</div>
    <div class="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4"><button v-for="item in summaries" :key="item.line" @click="showLine(item.line)" class="rounded-2xl border bg-white p-4 text-left shadow-sm hover:border-emerald-400 dark:bg-slate-800 dark:border-slate-700"><div class="flex justify-between"><strong>{{ item.line }}</strong><span class="text-xs text-slate-500">{{ t('spc.chemicalOverview.tankCount', { count: item.tanks }) }}</span></div><div class="mt-3 text-3xl font-black text-emerald-600">{{ item.enabled }}<span class="ml-1 text-sm text-slate-500">{{ t('spc.chemicalOverview.enabledItems') }}</span></div><div class="mt-2 text-xs text-slate-500">{{ t('spc.chemicalOverview.titrationLabel') }}{{ item.secondary }}　{{ t('spc.chemicalOverview.disabledLabel') }}{{ item.disabled }}　<span :class="item.issues ? 'text-amber-600 font-bold' : ''">{{ t('spc.chemicalOverview.issuesLabel') }}{{ item.issues }}</span></div></button></div>
    <div class="rounded-2xl border bg-white p-4 shadow-sm dark:bg-slate-800 dark:border-slate-700"><div class="grid gap-3 md:grid-cols-5"><select data-testid="overview-line" v-model="line" @change="tank=''; resetPage()" class="field"><option value="">{{ t('spc.chemicalOverview.allLines') }}</option><option v-for="value in lines" :key="value">{{ value }}</option></select><select data-testid="overview-tank" v-model="tank" @change="resetPage" class="field"><option value="">{{ t('spc.chemicalOverview.allTanks') }}</option><option v-for="value in tanks" :key="value">{{ value }}</option></select><select data-testid="overview-status" v-model="status" @change="resetPage" class="field"><option value="all">{{ t('spc.chemicalOverview.allStatuses') }}</option><option value="enabled">{{ t('spc.chemicalOverview.onlyEnabled') }}</option><option value="secondary">{{ t('spc.chemicalOverview.onlySecondary') }}</option><option value="issues">{{ t('spc.chemicalOverview.onlyIssues') }}</option></select><select data-testid="overview-pageSize" v-model.number="pageSize" @change="resetPage" class="field"><option :value="5">{{ t('spc.chemicalOverview.pageSize', { count: 5 }) }}</option><option :value="10">{{ t('spc.chemicalOverview.pageSize', { count: 10 }) }}</option><option :value="20">{{ t('spc.chemicalOverview.pageSize', { count: 20 }) }}</option></select><div class="flex items-center text-sm text-slate-500"><Search class="mr-2 w-4 h-4" />{{ t('spc.chemicalOverview.itemCount', { count: filtered.length }) }}</div></div><div class="mt-3"><label class="text-sm font-bold">{{ t('spc.chemicalOverview.reasonField') }}<input :aria-label="t('spc.chemicalOverview.reasonLabel')" :disabled="saving" data-testid="overview-formulaVersionReason" v-model="formulaVersionReason" class="field w-full" maxlength="500" :placeholder="t('spc.chemicalOverview.reasonExample')" /></label></div></div>
    <div class="overflow-x-auto rounded-2xl border bg-white shadow-sm dark:bg-slate-800 dark:border-slate-700"><table class="min-w-[1260px] text-sm"><thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-900"><tr><th>{{ t('spc.chemicalOverview.line') }}</th><th>{{ t('spc.chemicalOverview.tank') }}</th><th>{{ t('spc.chemicalOverview.item') }}</th><th>{{ t('common.columns.status') }}</th><th>{{ t('spc.chemicalOverview.concentration') }}</th><th>{{ t('spc.chemicalOverview.adjustment') }}</th><th>{{ t('spc.chemicalOverview.adjustmentAmount') }}</th><th>{{ t('spc.chemicalOverview.decimals') }}</th><th>{{ t('spc.chemicalOverview.configuration') }}</th><th>{{ t('spc.chemicalOverview.saveResult') }}</th><th></th></tr></thead><tbody><tr v-for="item in pagedRows" :key="item.id" :data-testid="`overview-row-${item.id}`" class="border-t dark:border-slate-700" :class="item.saveResult?.status === 'failed' ? 'bg-red-50/70 dark:bg-red-950/20' : item.dirty ? 'bg-amber-50/70 dark:bg-amber-950/20' : ''"><td>{{ item.line }}</td><td><div>{{ item.tank }}</div><small class="text-slate-400">{{ item.tankCode }}</small></td><td class="font-bold"><div>{{ item.chemical }}</div><small v-if="item.secondary" class="text-slate-400">{{ t('spc.chemicalOverview.titrationLabel') }}{{ item.secondary }}</small></td><td><span :class="item.enabled ? 'badge-green' : 'badge-gray'">{{ item.enabled ? t('common.status.on') : t('common.status.off') }}</span><span v-if="item.dirty" class="ml-2 badge-amber">{{ t('spc.chemicalOverview.changed') }}</span></td><td><textarea v-if="isEditing(item)" data-testid="overview-concentrationFormula" v-model="item.draft.concentrationFormula" rows="5" class="formula-field" :placeholder="t('spc.chemicalOverview.concentration')"></textarea><pre v-else class="formula-preview">{{ item.formula || t('spc.chemicalOverview.notSet') }}</pre></td><td><textarea v-if="isEditing(item)" data-testid="overview-adjustmentFormula" v-model="item.draft.adjustmentFormula" rows="5" class="formula-field" :placeholder="t('spc.chemicalOverview.adjustment')"></textarea><pre v-else class="formula-preview">{{ item.adjustmentFormula || t('spc.chemicalOverview.notSet') }}</pre></td><td><textarea v-if="isEditing(item)" data-testid="overview-adjustmentAmountFormula" v-model="item.draft.adjustmentAmountFormula" rows="5" class="formula-field" :placeholder="t('spc.chemicalOverview.adjustmentAmount')"></textarea><pre v-else class="formula-preview">{{ item.adjustmentAmountFormula || t('spc.chemicalOverview.notSet') }}</pre></td><td><input data-testid="overview-decimalPlaces" v-model.number="item.draft.decimalPlaces" type="number" min="0" max="8" class="field w-20" /></td><td><span :class="item.issue ? 'text-amber-600 font-bold' : 'text-emerald-600'">{{ item.formulaMissing ? t('spc.chemicalOverview.missingFormula') : item.limitsMissing ? t('spc.chemicalOverview.missingLimits') : t('spc.chemicalOverview.complete') }}</span></td><td class="max-w-56"><span v-if="item.saveResult?.status === 'saved'" class="badge-green">{{ t('spc.chemicalOverview.saved') }}</span><span v-else-if="item.saveResult?.status === 'failed'" class="text-xs font-bold text-red-600">{{ displayUiMessage(t, item.saveResult.message) }}</span><span v-else class="text-slate-400">—</span></td><td><div class="flex flex-col gap-2"><button :disabled="saving" data-testid="overview-history" @click="selectedHistory = { id: item.id, title: `${item.line} / ${item.tank} / ${item.chemical}` }" class="text-emerald-700 hover:underline dark:text-emerald-400">{{ t('spc.chemicalOverview.history') }}</button><button :disabled="saving" data-testid="overview-edit" @click="edit(item)" class="text-blue-600 hover:underline"><Settings2 class="inline w-4 h-4" /> {{ isEditing(item) ? t('spc.chemicalOverview.done') : t('spc.chemicalOverview.edit') }}</button><button v-if="item.dirty" data-testid="overview-reset" @click="resetRow(item)" class="text-slate-500 hover:underline">{{ t('spc.chemicalOverview.reset') }}</button></div></td></tr><tr v-if="!loading && !filtered.length"><td colspan="11" class="p-8 text-center text-slate-500">{{ t('spc.chemicalOverview.empty') }}</td></tr></tbody></table></div>
    <div v-if="filtered.length" class="flex flex-wrap items-center justify-between gap-3 rounded-2xl border bg-white p-3 text-sm text-slate-600 shadow-sm dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300"><div>{{ t('spc.chemicalOverview.pagination', { page: safePage, pages: totalPages, from: (safePage - 1) * pageSize + 1, to: Math.min(safePage * pageSize, filtered.length), total: filtered.length }) }}</div><div class="flex gap-2"><button class="btn-outline" :disabled="safePage <= 1" :class="{ 'opacity-50': safePage <= 1 }" data-testid="overview-previous" @click="changePage(safePage - 1)">{{ t('spc.chemicalOverview.previous') }}</button><button class="btn-outline" :disabled="safePage >= totalPages" :class="{ 'opacity-50': safePage >= totalPages }" data-testid="overview-next" @click="changePage(safePage + 1)">{{ t('spc.chemicalOverview.next') }}</button></div></div>
    <div v-if="selectedHistory" class="space-y-2"><button type="button" data-testid="overview-history" @click="selectedHistory = null" class="btn-outline">{{ t('spc.chemicalOverview.closeHistory') }}</button><ChemicalFormulaHistory :key="`${selectedHistory.id}-${historyRevision}`" :ppc-id="selectedHistory.id" :title="selectedHistory.title" /></div>
  </section>
</template>

<style scoped>
th,td{padding:.8rem 1rem;vertical-align:top}.field,.formula-field{width:100%;border:1px solid #cbd5e1;border-radius:.65rem;padding:.55rem .7rem;background:transparent}.formula-field,.formula-preview{min-width:16rem;line-height:1.45;font-family:ui-monospace,SFMono-Regular,Menlo,Monaco,Consolas,"Liberation Mono","Courier New",monospace;font-size:.82rem;white-space:pre-wrap;overflow-wrap:anywhere}.formula-field{min-height:8.5rem;resize:vertical}.formula-preview{margin:0;border:1px solid #e2e8f0;border-radius:.65rem;background:#f8fafc;padding:.65rem .75rem;color:#0f172a}.dark .formula-preview{border-color:#334155;background:#0f172a;color:#e2e8f0}.btn-primary,.btn-outline{display:inline-flex;align-items:center;gap:.4rem;border-radius:.7rem;padding:.55rem .8rem;font-size:.875rem;font-weight:700}.btn-primary{background:#059669;color:white}.btn-outline{border:1px solid #94a3b8}.badge-green,.badge-gray,.badge-amber{border-radius:9999px;padding:.2rem .55rem;font-size:.75rem;font-weight:700;white-space:nowrap}.badge-green{background:#d1fae5;color:#047857}.badge-gray{background:#e2e8f0;color:#475569}.badge-amber{background:#fef3c7;color:#b45309}
</style>
