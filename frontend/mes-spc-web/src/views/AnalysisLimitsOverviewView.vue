<script setup>
import { computed, onMounted, ref } from 'vue';
import { useI18n } from 'vue-i18n';
import * as XLSX from 'xlsx';
import { Download, RefreshCw, RotateCcw, Save, SlidersHorizontal } from 'lucide-vue-next';
import { api, getApiErrorDisplay } from '../api/client';
import { characteristicDisplayName } from '../i18n/characteristicNames';
import { buildLimitBatchItems, createLimitDraft, isLimitDraftDirty } from '../utils/analysisLimits';

const { t, locale } = useI18n({ useScope: 'global' });
const loading = ref(false);
const previewing = ref(false);
const applying = ref(false);
const error = ref('');
const message = ref('');
const items = ref([]);
const drafts = ref({});
const preview = ref(null);
const scopeFilter = ref('');
const processFilter = ref('');
const slotFilter = ref('');
const statusFilter = ref('enabled');
const configFilter = ref('');
const page = ref(1);
const pageSize = ref(10);

const displayName = item => characteristicDisplayName(item.characteristic, locale.value, t('spc.characteristics.unnamed'));
const textOf = value => value === null || value === undefined ? '' : String(value);
const scopeOf = item => String(item.controlScope || '').toUpperCase();
const processOf = item => item.machine?.machineCode || item.process?.processCode || t('spc.analysisLimits.notSet');
const slotOf = item => item.slot?.slotName || item.slot?.slotCode || item.tank?.tankName || item.tank?.tankCode || t('spc.analysisLimits.notSet');
const modeOf = item => [item.lcl, item.cl, item.ucl].every(value => value === null || value === undefined) ? 'auto' : 'manual';
const hasFormatIssue = item => (item.lsl != null && item.usl != null && item.lsl >= item.usl)
  || (item.targetValue != null && ((item.lsl != null && item.targetValue < item.lsl) || (item.usl != null && item.targetValue > item.usl)))
  || ([item.lcl, item.cl, item.ucl].filter(value => value != null).length > 0
    && !([item.lcl, item.cl, item.ucl].every(value => value != null) && item.lcl < item.cl && item.cl < item.ucl));

const dirtyIds = computed(() => new Set(items.value.filter(item => isLimitDraftDirty(item, drafts.value[item.id])).map(item => item.id)));
const scopes = computed(() => [...new Set(items.value.map(scopeOf))].filter(Boolean).sort());
const processes = computed(() => [...new Set(items.value.filter(item => !scopeFilter.value || scopeOf(item) === scopeFilter.value).map(processOf))].sort());
const slots = computed(() => [...new Set(items.value.filter(item => (!scopeFilter.value || scopeOf(item) === scopeFilter.value) && (!processFilter.value || processOf(item) === processFilter.value)).map(slotOf))].sort());
const filtered = computed(() => items.value.filter(item => {
  if (scopeFilter.value && scopeOf(item) !== scopeFilter.value) return false;
  if (processFilter.value && processOf(item) !== processFilter.value) return false;
  if (slotFilter.value && slotOf(item) !== slotFilter.value) return false;
  if (statusFilter.value === 'enabled' && !item.isEnabled) return false;
  if (statusFilter.value === 'disabled' && item.isEnabled) return false;
  if (configFilter.value === 'missing' && (item.lsl != null || item.targetValue != null || item.usl != null)) return false;
  if (configFilter.value === 'auto' && modeOf(item) !== 'auto') return false;
  if (configFilter.value === 'manual' && modeOf(item) !== 'manual') return false;
  if (configFilter.value === 'issue' && !hasFormatIssue(item)) return false;
  return true;
}));
const totalPages = computed(() => Math.max(1, Math.ceil(filtered.value.length / pageSize.value)));
const safePage = computed(() => Math.min(page.value, totalPages.value));
const paged = computed(() => filtered.value.slice((safePage.value - 1) * pageSize.value, safePage.value * pageSize.value));

async function load() {
  loading.value = true;
  error.value = '';
  preview.value = null;
  try {
    const response = await api.get('/part-process-characteristics');
    items.value = Array.isArray(response.data) ? response.data : [];
    drafts.value = Object.fromEntries(items.value.map(item => [item.id, createLimitDraft(item)]));
    page.value = 1;
  } catch (exception) {
    error.value = getApiErrorDisplay(exception);
  } finally {
    loading.value = false;
  }
}

function resetFilters() {
  scopeFilter.value = '';
  processFilter.value = '';
  slotFilter.value = '';
  statusFilter.value = 'enabled';
  configFilter.value = '';
  page.value = 1;
}
function resetRow(item) {
  drafts.value[item.id] = createLimitDraft(item);
  preview.value = null;
}
function changePage(value) { page.value = Math.min(totalPages.value, Math.max(1, value)); }

async function previewChanges() {
  const batchItems = buildLimitBatchItems(items.value, drafts.value);
  if (!batchItems.length) {
    message.value = t('spc.analysisLimits.noChanges');
    return;
  }
  previewing.value = true;
  error.value = '';
  message.value = '';
  try {
    const response = await api.post('/analysis-limits/preview', { items: batchItems });
    preview.value = response.data?.data || response.data;
  } catch (exception) {
    error.value = getApiErrorDisplay(exception);
  } finally {
    previewing.value = false;
  }
}

async function applyChanges() {
  if (!preview.value?.isValid || !preview.value?.applyToken) return;
  applying.value = true;
  error.value = '';
  try {
    const response = await api.post('/analysis-limits/apply', { applyToken: preview.value.applyToken });
    const result = response.data?.data || response.data;
    message.value = t('spc.analysisLimits.applied', { count: result.updatedCount });
    await load();
  } catch (exception) {
    error.value = getApiErrorDisplay(exception);
    preview.value = null;
  } finally {
    applying.value = false;
  }
}

function exportExcel() {
  const rows = filtered.value.map(item => ({
    Scope: scopeOf(item), ProcessLine: processOf(item), SlotTank: slotOf(item),
    AnalysisItem: displayName(item), Unit: item.unit || '', Status: item.isEnabled ? 'Enabled' : 'Disabled',
    LSL: item.lsl, Target: item.targetValue, USL: item.usl,
    LCL: item.lcl, CL: item.cl, UCL: item.ucl,
    ControlMode: modeOf(item), HasSegments: item.hasLimitSegments ? 'Y' : 'N'
  }));
  const book = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(book, XLSX.utils.json_to_sheet(rows), 'Analysis Limits');
  XLSX.writeFile(book, `SPC_Analysis_Limits_${new Date().toISOString().slice(0, 10)}.xlsx`);
}

onMounted(load);
</script>

<template>
  <section class="space-y-5" data-testid="analysis-limits-page">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="flex items-center gap-2 text-2xl font-black text-slate-800 dark:text-white"><SlidersHorizontal class="h-7 w-7 text-indigo-500" />{{ t('spc.analysisLimits.title') }}</h1>
        <p class="mt-1 text-sm text-slate-500">{{ t('spc.analysisLimits.subtitle') }}</p>
      </div>
      <div class="flex flex-wrap gap-2">
        <button class="btn-outline" :disabled="loading || applying" @click="load"><RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />{{ t('common.actions.refresh') }}</button>
        <button class="btn-outline" @click="exportExcel"><Download class="h-4 w-4" />{{ t('spc.analysisLimits.export') }}</button>
        <button data-testid="limits-preview" class="btn-primary" :disabled="!dirtyIds.size || previewing || applying" @click="previewChanges"><Save class="h-4 w-4" />{{ t('spc.analysisLimits.preview') }} ({{ dirtyIds.size }})</button>
      </div>
    </header>

    <div v-if="error" class="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950/30 dark:text-red-300">{{ error }}</div>
    <div v-if="message" class="rounded-xl border border-emerald-200 bg-emerald-50 p-3 text-sm text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/30 dark:text-emerald-300">{{ message }}</div>

    <div class="rounded-2xl border bg-white p-4 shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <div class="grid gap-3 md:grid-cols-3 xl:grid-cols-6">
        <select v-model="scopeFilter" class="field" @change="processFilter='';slotFilter='';page=1"><option value="">{{ t('spc.analysisLimits.allScopes') }}</option><option v-for="value in scopes" :key="value">{{ value }}</option></select>
        <select v-model="processFilter" class="field" @change="slotFilter='';page=1"><option value="">{{ t('spc.analysisLimits.allProcesses') }}</option><option v-for="value in processes" :key="value">{{ value }}</option></select>
        <select v-model="slotFilter" class="field" @change="page=1"><option value="">{{ t('spc.analysisLimits.allSlots') }}</option><option v-for="value in slots" :key="value">{{ value }}</option></select>
        <select v-model="statusFilter" class="field" @change="page=1"><option value="">{{ t('spc.analysisLimits.allStatuses') }}</option><option value="enabled">{{ t('spc.analysisLimits.enabled') }}</option><option value="disabled">{{ t('spc.analysisLimits.disabled') }}</option></select>
        <select v-model="configFilter" class="field" @change="page=1"><option value="">{{ t('spc.analysisLimits.allConfigurations') }}</option><option value="missing">{{ t('spc.analysisLimits.missingSpec') }}</option><option value="auto">{{ t('spc.analysisLimits.autoControl') }}</option><option value="manual">{{ t('spc.analysisLimits.manualControl') }}</option><option value="issue">{{ t('spc.analysisLimits.formatIssue') }}</option></select>
        <div class="flex gap-2"><select v-model.number="pageSize" class="field" @change="page=1"><option v-for="size in [5,10,20,50]" :key="size" :value="size">{{ size }}</option></select><button class="btn-outline" @click="resetFilters"><RotateCcw class="h-4 w-4" /></button></div>
      </div>
    </div>

    <div class="overflow-x-auto rounded-2xl border bg-white shadow-sm dark:border-slate-700 dark:bg-slate-800">
      <table class="min-w-[1380px] text-sm">
        <thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-900"><tr><th>{{ t('spc.analysisLimits.scope') }}</th><th>{{ t('spc.analysisLimits.processLine') }}</th><th>{{ t('spc.analysisLimits.slotTank') }}</th><th>{{ t('spc.analysisLimits.item') }}</th><th>{{ t('spc.analysisLimits.unit') }}</th><th>LSL</th><th>Target</th><th>USL</th><th>LCL</th><th>CL</th><th>UCL</th><th>{{ t('spc.analysisLimits.mode') }}</th><th></th></tr></thead>
        <tbody>
          <tr v-for="item in paged" :key="item.id" :data-testid="`limits-row-${item.id}`" class="border-t dark:border-slate-700" :class="dirtyIds.has(item.id) ? 'bg-amber-50/70 dark:bg-amber-950/20' : ''">
            <td>{{ scopeOf(item) }}</td><td>{{ processOf(item) }}</td><td>{{ slotOf(item) }}<div v-if="item.hasLimitSegments" class="text-xs font-bold text-amber-600">{{ t('spc.analysisLimits.segmented') }}</div></td><td class="font-bold">{{ displayName(item) }}</td><td>{{ item.unit || '—' }}</td>
            <td v-for="field in ['lsl','target','usl','lcl','cl','ucl']" :key="field"><input v-model="drafts[item.id][field]" inputmode="decimal" class="limit-field" :aria-label="`${field.toUpperCase()} ${displayName(item)}`" /></td>
            <td><span :class="modeOf(item) === 'auto' ? 'badge-auto' : 'badge-manual'">{{ t(`spc.analysisLimits.${modeOf(item)}Control`) }}</span></td>
            <td><button v-if="dirtyIds.has(item.id)" class="text-indigo-600 hover:underline" @click="resetRow(item)">{{ t('spc.analysisLimits.reset') }}</button></td>
          </tr>
          <tr v-if="!loading && !paged.length"><td colspan="13" class="p-10 text-center text-slate-500">{{ t('spc.analysisLimits.empty') }}</td></tr>
        </tbody>
      </table>
    </div>

    <div class="flex flex-wrap items-center justify-between gap-3 rounded-xl border bg-white p-3 text-sm dark:border-slate-700 dark:bg-slate-800">
      <span>{{ t('spc.analysisLimits.pagination', { page: safePage, pages: totalPages, total: filtered.length }) }}</span>
      <div class="flex gap-2"><button class="btn-outline" :disabled="safePage<=1" @click="changePage(safePage-1)">{{ t('spc.analysisLimits.previous') }}</button><button class="btn-outline" :disabled="safePage>=totalPages" @click="changePage(safePage+1)">{{ t('spc.analysisLimits.next') }}</button></div>
    </div>

    <div v-if="preview" class="rounded-2xl border bg-white p-5 shadow-lg dark:border-slate-700 dark:bg-slate-800" data-testid="limits-preview-result">
      <h2 class="text-lg font-black">{{ t('spc.analysisLimits.previewResult') }}</h2>
      <p class="mt-1 text-sm" :class="preview.isValid ? 'text-emerald-600' : 'text-red-600'">{{ preview.isValid ? t('spc.analysisLimits.previewPassed') : t('spc.analysisLimits.previewFailed') }}</p>
      <div class="mt-4 max-h-72 overflow-auto space-y-2"><div v-for="row in preview.items" :key="row.partProcessCharacteristicId" class="rounded-lg border p-3 dark:border-slate-700"><strong>#{{ row.partProcessCharacteristicId }}</strong><div v-for="validation in row.errors" :key="`${validation.field}-${validation.code}`" class="mt-1 text-sm text-red-600"><b>{{ validation.field }}</b>：{{ validation.message }} {{ validation.suggestion }}</div><div v-if="!row.errors.length" class="text-sm text-emerald-600">{{ t('spc.analysisLimits.rowPassed') }}</div></div></div>
      <div class="mt-4 flex justify-end gap-2"><button class="btn-outline" @click="preview=null">{{ t('spc.analysisLimits.close') }}</button><button data-testid="limits-apply" class="btn-primary" :disabled="!preview.isValid || applying" @click="applyChanges">{{ applying ? t('spc.analysisLimits.applying') : t('spc.analysisLimits.apply') }}</button></div>
    </div>
  </section>
</template>

<style scoped>
th,td{padding:.7rem .8rem;vertical-align:middle}.field,.limit-field{border:1px solid #cbd5e1;border-radius:.6rem;background:transparent;padding:.5rem .65rem}.field{width:100%}.limit-field{width:7rem}.btn-primary,.btn-outline{display:inline-flex;align-items:center;justify-content:center;gap:.4rem;border-radius:.65rem;padding:.5rem .75rem;font-size:.875rem;font-weight:700}.btn-primary{background:#4f46e5;color:#fff}.btn-primary:disabled,.btn-outline:disabled{opacity:.5}.btn-outline{border:1px solid #94a3b8}.badge-auto,.badge-manual{border-radius:9999px;padding:.2rem .55rem;font-size:.75rem;font-weight:700;white-space:nowrap}.badge-auto{background:#e2e8f0;color:#475569}.badge-manual{background:#dbeafe;color:#1d4ed8}.dark .field,.dark .limit-field{border-color:#475569}.dark .badge-auto{background:#334155;color:#e2e8f0}.dark .badge-manual{background:#1e3a8a;color:#bfdbfe}
</style>
