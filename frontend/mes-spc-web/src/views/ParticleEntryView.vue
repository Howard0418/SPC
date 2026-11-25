<script setup>
import { computed, ref } from 'vue';
import { AlertTriangle, CheckCircle2, ClipboardPaste, Eraser, Eye, Save } from 'lucide-vue-next';
import { useI18n } from 'vue-i18n';
import { api, getApiErrorDisplay } from '../api/client';
import { displayUiMessage } from '../i18n/uiMessage';
import { buildParticlePreviewRows, parseParticlePaste, particleLocations, particleSizes, sha256Hex } from '../particle/particlePaste';

const { t } = useI18n({ useScope: 'global' });
const clipboardText = ref('');
const parsed = ref({ dataRows: [], missingCount: 0, errors: [] });
const preview = ref(null);
const previewErrors = ref([]);
const result = ref(null);
const error = ref('');
const busy = ref(false);

const expandedRows = computed(() => buildParticlePreviewRows(parsed.value.dataRows));
const hasDraft = computed(() => clipboardText.value.length > 0 || parsed.value.dataRows.length > 0 || preview.value);
const canConfirm = computed(() => preview.value && preview.value.errorRows === 0 && !result.value);

function applyPaste() {
  parsed.value = parseParticlePaste(clipboardText.value);
  preview.value = null;
  previewErrors.value = [];
  result.value = null;
  error.value = '';
}

function onPaste(event) {
  const value = event.clipboardData?.getData('text/plain');
  if (typeof value !== 'string') return;
  event.preventDefault();
  clipboardText.value = value;
  applyPaste();
}

function clearDraft() {
  clipboardText.value = '';
  parsed.value = { dataRows: [], missingCount: 0, errors: [] };
  preview.value = null;
  previewErrors.value = [];
  result.value = null;
  error.value = '';
}

async function createPreview() {
  applyPaste();
  if (parsed.value.errors.length || !expandedRows.value.length) return;
  busy.value = true;
  try {
    const hash = await sha256Hex(JSON.stringify(expandedRows.value));
    const response = await api.post('/v1/uploads/particle/preview', {
      clientBatchId: crypto.randomUUID(),
      originalFileName: `web-paste-${new Date().toISOString().replace(/[:.]/g, '-')}.txt`,
      fileHashSha256: hash,
      sourceType: 'WebPaste',
      rows: expandedRows.value
    });
    preview.value = response.data;
    if (response.data?.errorRows > 0) {
      const details = await api.get(`/v1/uploads/${response.data.uploadBatchId}/preview`);
      previewErrors.value = details.data?.errors || [];
    }
  } catch (reason) { error.value = getApiErrorDisplay(reason); }
  finally { busy.value = false; }
}

async function confirm(duplicateMode = 'reject') {
  if (!canConfirm.value) return;
  busy.value = true; error.value = '';
  try {
    const response = await api.post(`/v1/uploads/${preview.value.uploadBatchId}/confirm`, null, { params: { duplicateMode } });
    result.value = response.data;
  } catch (reason) {
    if (reason?.response?.status === 409 && reason?.response?.data?.conflictCode === 'DUPLICATE_MEASUREMENT') {
      preview.value = { ...preview.value, duplicateRows: reason.response.data.skippedRows || preview.value.duplicateRows };
      error.value = { key: 'spc.particleEntry.duplicateBlocked' };
    } else error.value = getApiErrorDisplay(reason);
  } finally { busy.value = false; }
}

const cellValue = (row, locationIndex, sizeIndex) => row.counts[locationIndex * 4 + sizeIndex] ?? '';
function measurementTimeParts(value) {
  const match = String(value ?? '').match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/);
  return match
    ? { date: `${match[1]}/${match[2]}/${match[3]}`, time: `${match[4]}:${match[5]}` }
    : { date: String(value ?? ''), time: '' };
}
const errorKey = { EMPTY: 'empty', NON_RECTANGULAR: 'nonRectangular', HEADER_MISMATCH: 'headerMismatch', COLUMN_COUNT: 'columnCount', TIME_INVALID: 'timeInvalid', COUNT_INVALID: 'countInvalid' };
const errorText = item => t(`spc.particleEntry.errors.${errorKey[item.code] || 'countInvalid'}`, { row: item.row, column: item.column, expected: item.expected, actual: item.actual });
</script>

<template>
  <section class="mx-auto w-full max-w-[1800px] space-y-4">
    <header class="border-b border-slate-200 pb-4 dark:border-slate-800">
      <h2 class="text-2xl font-black">{{ t('spc.particleEntry.title') }}</h2>
      <p class="mt-1 text-sm text-slate-500">{{ t('spc.particleEntry.description') }}</p>
    </header>

    <section data-testid="particle-floor-plan-section" class="overflow-hidden rounded-xl border border-slate-300 bg-white p-3 dark:border-slate-700 dark:bg-slate-900">
      <img
        data-testid="particle-floor-plan"
        src="/assets/particle-floor-plan.png"
        :alt="t('spc.particleEntry.title')"
        class="mx-auto block h-auto max-h-[720px] w-full object-contain"
      >
    </section>

    <section data-testid="particle-paste-section" class="rounded-xl border border-slate-300 bg-white p-4 dark:border-slate-700 dark:bg-slate-900">
      <div class="flex flex-wrap items-end justify-between gap-2">
        <label id="particle-paste-label" class="text-sm font-bold">{{ t('spc.particleEntry.pasteLabel') }}</label>
        <p class="text-xs text-slate-500">{{ t('spc.particleEntry.pastePlaceholder') }}</p>
      </div>
      <div class="mt-2 overflow-x-auto rounded-lg border-2 border-slate-900 dark:border-slate-300">
        <table
          data-testid="particle-paste-grid"
          tabindex="0"
          role="grid"
          aria-labelledby="particle-paste-label"
          class="min-w-[2450px] border-collapse text-center text-xs outline-none focus:ring-4 focus:ring-blue-300 dark:focus:ring-blue-700"
          @paste="onPaste"
        >
          <thead class="bg-yellow-300 text-slate-950">
            <tr>
              <th rowspan="2" class="sticky left-0 z-20 w-40 min-w-40 border border-black bg-yellow-300 px-2 py-2 font-black">
                {{ t('spc.particleEntry.measurementTime') }}\{{ t('spc.particles.location') }}
              </th>
              <th v-for="location in particleLocations" :key="location" colspan="4" class="border border-black bg-yellow-300 px-2 py-1 font-black">{{ location }}</th>
            </tr>
            <tr>
              <template v-for="location in particleLocations" :key="`${location}-paste-sizes`">
                <th v-for="size in particleSizes" :key="`${location}-${size}-paste`" class="min-w-12 border border-black bg-yellow-300 px-2 py-1 font-semibold">
                  {{ size }}<span v-if="size === '0.5'"> µm</span>
                </th>
              </template>
            </tr>
          </thead>
          <tbody class="bg-white text-slate-950">
            <tr v-if="!parsed.dataRows.length" data-testid="particle-grid-empty">
              <td class="sticky left-0 border border-slate-500 bg-white px-2 py-4 font-semibold">—</td>
              <td colspan="48" class="border border-slate-500 px-4 py-4 text-left text-slate-500">{{ t('spc.particleEntry.pastePlaceholder') }}</td>
            </tr>
            <tr v-for="(row, rowIndex) in parsed.dataRows" :key="row.sourceRow">
              <td :data-testid="`particle-time-${rowIndex}`" class="sticky left-0 z-10 border border-slate-500 bg-white px-2 py-2 font-mono font-semibold">
                <div>{{ measurementTimeParts(row.measurementTime).date }}</div>
                <div>{{ measurementTimeParts(row.measurementTime).time }}</div>
              </td>
              <template v-for="(_, locationIndex) in particleLocations" :key="`${row.sourceRow}-${locationIndex}`">
                <td
                  v-for="(_, sizeIndex) in particleSizes"
                  :key="`${row.sourceRow}-${locationIndex}-${sizeIndex}`"
                  :data-testid="`particle-cell-${rowIndex}-${locationIndex * 4 + sizeIndex}`"
                  class="border border-slate-500 px-2 py-2 font-mono"
                >{{ cellValue(row, locationIndex, sizeIndex) }}</td>
              </template>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="mt-3 flex flex-wrap gap-2">
        <button type="button" data-testid="particle-parse" class="inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-bold" :disabled="busy" @click="applyPaste"><ClipboardPaste class="h-4 w-4" />{{ t('spc.particleEntry.parse') }}</button>
        <button type="button" data-testid="particle-preview" class="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-bold text-white disabled:opacity-50" :disabled="busy || !clipboardText" @click="createPreview"><Eye class="h-4 w-4" />{{ t('spc.particleEntry.preview') }}</button>
        <button type="button" class="inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-bold" :disabled="busy || !hasDraft" @click="clearDraft"><Eraser class="h-4 w-4" />{{ t('spc.particleEntry.clear') }}</button>
      </div>
    </section>

    <div v-if="parsed.errors.length" role="alert" class="rounded-lg border border-red-300 bg-red-50 p-4 text-sm text-red-800 dark:bg-red-950/30 dark:text-red-200">
      <div class="flex items-center gap-2 font-bold"><AlertTriangle class="h-4 w-4" />{{ t('spc.particleEntry.parseFailed') }}</div>
      <ul class="mt-2 list-disc pl-6"><li v-for="(item,index) in parsed.errors" :key="index">{{ errorText(item) }}</li></ul>
    </div>
    <div v-if="error" role="alert" class="rounded-lg border border-red-300 bg-red-50 p-4 text-sm text-red-800 dark:bg-red-950/30 dark:text-red-200">{{ displayUiMessage(t, error) }}</div>
    <div v-if="result" role="status" class="flex items-center gap-2 rounded-lg border border-emerald-300 bg-emerald-50 p-4 text-sm font-bold text-emerald-800 dark:bg-emerald-950/30 dark:text-emerald-200"><CheckCircle2 class="h-5 w-5" />{{ t('spc.particleEntry.confirmed', { inserted: result.insertedRows, skipped: result.skippedRows }) }}</div>

    <section v-if="parsed.dataRows.length" class="space-y-3">
      <div class="flex flex-wrap gap-4 text-sm font-semibold text-slate-600 dark:text-slate-300">
        <span>{{ t('spc.particleEntry.sourceRows', { count: parsed.dataRows.length }) }}</span>
        <span>{{ t('spc.particleEntry.validValues', { count: expandedRows.length }) }}</span>
        <span>{{ t('spc.particleEntry.missingValues', { count: parsed.missingCount }) }}</span>
        <span v-if="preview">{{ t('spc.particleEntry.duplicates', { count: preview.duplicateRows }) }}</span>
      </div>
    </section>

    <section v-if="preview" class="rounded-xl border border-slate-200 p-4 dark:border-slate-800">
      <h3 class="font-black">{{ t('spc.particleEntry.previewResult') }}</h3>
      <p class="mt-2 text-sm">{{ t('spc.particleEntry.previewSummary', { valid: preview.validRows, errors: preview.errorRows, duplicates: preview.duplicateRows }) }}</p>
      <ul v-if="previewErrors.length" class="mt-2 list-disc pl-6 text-sm text-red-600"><li v-for="item in previewErrors" :key="`${item.rowNo}-${item.errorCode}`">{{ item.errorCode }}（{{ item.fieldName }}／{{ item.rowNo }}）</li></ul>
      <div class="mt-4 flex flex-wrap gap-2">
        <button type="button" data-testid="particle-confirm" class="inline-flex items-center gap-2 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-bold text-white disabled:opacity-50" :disabled="busy || !canConfirm" @click="confirm('reject')"><Save class="h-4 w-4" />{{ t('spc.particleEntry.confirm') }}</button>
        <button v-if="preview.duplicateRows > 0" type="button" data-testid="particle-confirm-skip" class="rounded-lg border border-amber-500 px-4 py-2 text-sm font-bold text-amber-700" :disabled="busy || !canConfirm" @click="confirm('skip')">{{ t('spc.particleEntry.confirmSkip') }}</button>
      </div>
    </section>
  </section>
</template>
