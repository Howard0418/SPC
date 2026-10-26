<script setup>
import { displayUiMessage } from "../i18n/uiMessage";
import { useI18n } from "vue-i18n";
const { t } = useI18n({ useScope: "global" });
import { computed, ref, watch } from "vue";
import ChemicalCalculationDetails from "./ChemicalCalculationDetails.vue";
import { api, getApiErrorDisplay } from "../api/client";

const props = defineProps({ ppcId: { type: [Number, String], required: true }, chartLineEntry: Boolean });
const segments = ref([]);
const detail = ref(null);
const opened = ref(false);
const selectedId = ref(null);
const loading = ref(false);
const listError = ref("");
const error = ref("");
const formulaSkip = ref(0);
const specificationSkip = ref(0);
let listRequest = 0;
let detailRequest = 0;
const pageSize = 20;
const isChemical = computed(() => detail.value?.formulaVersionStatus !== "NotApplicable");
const formatRangeDate = value => value ? String(value).replace("T", " ") : t('spc.segmentDetails.notSet');
const formatChangeDate = value => value ? new Date(value).toLocaleString("zh-TW", { timeZone: "Asia/Taipei", hour12: false }) : t('spc.segmentDetails.notRecorded');
function formulaText(json) {
  if (!json) return t('spc.segmentDetails.notSet');
  try {
    const value = JSON.parse(json);
    if (!value || typeof value !== "object") return t('spc.segmentDetails.invalidFormula');
    const fields = [[t('spc.segmentDetails.concentration'), "concentrationFormula"], [t('spc.segmentDetails.adjustment'), "adjustmentFormula"], [t('spc.segmentDetails.amount'), "adjustmentAmountFormula"]];
    return fields.filter(([, key]) => value[key]).map(([label, key]) => `${label}：${value[key]}`).join("\n") || t('spc.segmentDetails.noFormulaSet');
  } catch { return t('spc.segmentDetails.invalidFormula'); }
}
function snapshotText(json) {
  if (!json) return t('spc.segmentDetails.noSnapshot');
  try { return JSON.stringify(JSON.parse(json), null, 2); }
  catch { return t('spc.segmentDetails.invalidSnapshot'); }
}
async function loadSegments() {
  const request = ++listRequest;
  ++detailRequest;
  opened.value = false; detail.value = null; segments.value = [];
  selectedId.value = null; error.value = ""; listError.value = ""; loading.value = false;
  if (!props.ppcId) return;
  try {
    const response = await api.get("/control-limit-segments", { params: { ppcId: props.ppcId } });
    if (request !== listRequest) return;
    segments.value = Array.isArray(response.data) ? response.data : [];
  } catch (e) {
    if (request === listRequest) listError.value = getApiErrorDisplay(e);
  }
}
async function loadDetail() {
  const request = ++detailRequest;
  loading.value = true; detail.value = null; error.value = "";
  try {
    const response = await api.get(`/v1/part-process-characteristics/${props.ppcId}/chart-segment-details`, {
      params: { segmentId: selectedId.value ?? undefined, formulaSkip: formulaSkip.value, specificationSkip: specificationSkip.value }
    });
    if (request === detailRequest) detail.value = response.data.data;
  } catch (e) {
    if (request === detailRequest) error.value = getApiErrorDisplay(e);
  } finally {
    if (request === detailRequest) loading.value = false;
  }
}
function openSegment(id = null) {
  selectedId.value = id;
  formulaSkip.value = 0; specificationSkip.value = 0; opened.value = true;
  loadDetail();
}
function close() { ++detailRequest; opened.value = false; detail.value = null; loading.value = false; error.value = ""; }
function changePage(kind, offset) {
  if (kind === "formula") formulaSkip.value = Math.max(0, formulaSkip.value + offset);
  else specificationSkip.value = Math.max(0, specificationSkip.value + offset);
  loadDetail();
}
watch(() => props.ppcId, loadSegments, { immediate: true });
defineExpose({ openSegment });
</script>

<template>
  <section data-testid="segment-details" :aria-label="t('spc.segmentDetails.title')" class="my-3 min-w-0 break-words rounded-xl border border-slate-200 dark:border-slate-700 p-3 text-sm text-slate-700 dark:text-slate-200">
    <h3 class="font-bold">{{ t('spc.segmentDetails.title') }}</h3>
    <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ chartLineEntry ? t('spc.segmentDetails.lineEntry') : t('spc.segmentDetails.dateEntry') }}{{ t('spc.segmentDetails.intro') }}</p>
    <div v-if="listError" role="alert" class="mt-2 text-red-600 dark:text-red-400">
      {{ displayUiMessage(t, listError) }} <button type="button" class="underline" @click="loadSegments">{{ t('spc.segmentDetails.retryList') }}</button>
    </div>
    <div v-else class="mt-2 flex flex-wrap gap-2">
      <button type="button" class="rounded border border-slate-300 dark:border-slate-600 px-3 py-2 hover:bg-blue-50 dark:hover:bg-slate-800" :aria-pressed="opened && selectedId === null" @click="openSegment()">{{ t('spc.segmentDetails.base') }}</button>
      <button v-for="segment in segments" :key="segment.id" type="button" :aria-pressed="opened && selectedId === segment.id"
        class="rounded border border-blue-300 dark:border-blue-700 px-3 py-2 hover:bg-blue-50 dark:hover:bg-slate-800"
        @click="openSegment(segment.id)">
        {{ formatRangeDate(segment.startDate) }} ～ {{ segment.endDate ? formatRangeDate(segment.endDate) : t('spc.segmentDetails.openEnded') }}
      </button>
      <p v-if="!segments.length" class="self-center text-xs text-slate-500 dark:text-slate-400">{{ t('spc.segmentDetails.noSegments') }}</p>
    </div>
    <div v-if="opened" class="mt-3 border-t border-slate-200 dark:border-slate-700 pt-3" aria-live="polite">
      <div class="flex justify-between gap-2"><h4 class="font-bold">{{ t('spc.segmentDetails.details') }}</h4><button type="button" class="underline" @click="close">{{ t('spc.segmentDetails.close') }}</button></div>
      <p v-if="loading" role="status" class="mt-2">{{ t('spc.segmentDetails.loading') }}</p>
      <div v-else-if="error" role="alert" class="mt-2 text-red-600 dark:text-red-400">{{ displayUiMessage(t, error) }} <button type="button" class="underline" @click="loadDetail">{{ t('spc.segmentDetails.retryDetail') }}</button></div>
      <div v-else-if="detail" class="mt-2 space-y-4">
        <p data-testid="segment-range">{{ detail.segmentId ? t('spc.segmentDetails.range', { id: detail.segmentId, start: formatRangeDate(detail.startDate), end: detail.endDate ? formatRangeDate(detail.endDate) : t('spc.segmentDetails.noEnd') }) : t('spc.segmentDetails.baseRange') }}</p>
        <p v-if="detail.note" class="whitespace-pre-wrap break-words">{{ t('spc.segmentDetails.note', { note: detail.note }) }}</p>
        <div class="grid grid-cols-2 md:grid-cols-3 gap-2">
          <div v-for="(limit, name) in detail.limits" :key="name" class="rounded bg-slate-50 dark:bg-slate-800 p-2">
            <strong>{{ name }}：{{ limit.value ?? t('spc.segmentDetails.notSet') }}</strong>
            <p class="text-xs text-slate-500 dark:text-slate-400">{{ limit.source === 'Segment' ? t('spc.segmentDetails.segmentSource') : detail.segmentId ? t('spc.segmentDetails.fallbackSource') : t('spc.segmentDetails.masterSource') }}</p>
          </div>
        </div>
        <p class="text-xs text-amber-700 dark:text-amber-400">{{ t('spc.segmentDetails.limitsNotice') }}</p>
        <ChemicalCalculationDetails v-if="isChemical" :ppc-id="ppcId" :segment-id="selectedId" />
        <section :aria-label="t('spc.segmentDetails.specSection')" class="space-y-2">
          <h4 class="font-bold">{{ detail.segmentId ? t('spc.segmentDetails.segmentSpecTitle') : t('spc.segmentDetails.masterSpecTitle') }}</h4>
          <p v-if="!detail.specificationHistory.length">{{ t('spc.segmentDetails.noSpec') }}</p>
          <details v-for="row in detail.specificationHistory" :key="row.id" class="rounded border border-slate-200 dark:border-slate-700 p-2">
            <summary class="cursor-pointer">{{ t('spc.segmentDetails.specVersion', { version: row.versionNo, date: formatChangeDate(row.changedAt), type: row.changeType }) }}</summary>
            <p>{{ t('spc.segmentDetails.changedBy', { name: row.changedBy || t('spc.segmentDetails.notRecorded') }) }}</p>
            <pre class="whitespace-pre-wrap break-words text-xs mt-2">{{ snapshotText(row.snapshotJson) }}</pre>
          </details>
          <div class="flex flex-wrap gap-3"><button type="button" class="underline disabled:opacity-40" :disabled="specificationSkip === 0" @click="changePage('specification', -pageSize)">{{ t('spc.segmentDetails.specPrevious') }}</button><span>{{ t('spc.segmentDetails.page', { page: specificationSkip / pageSize + 1 }) }}</span><button type="button" class="underline disabled:opacity-40" :disabled="!detail.hasMoreSpecification" @click="changePage('specification', pageSize)">{{ t('spc.segmentDetails.specNext') }}</button></div>
        </section>
        <section :aria-label="t('spc.segmentDetails.formulaTitle')" class="space-y-2">
          <h4 class="font-bold">{{ t('spc.segmentDetails.formulaTitle') }}</h4>
          <template v-if="isChemical">
            <p class="text-amber-700 dark:text-amber-400">{{ t('spc.segmentDetails.formulaNotice') }}</p>
            <h5 class="font-bold">{{ t('spc.segmentDetails.currentFormula') }}</h5>
            <pre class="whitespace-pre-wrap break-words rounded bg-slate-50 dark:bg-slate-800 p-2 text-xs">{{ formulaText(detail.currentFormulaConfigJson) }}</pre>
            <p v-if="!detail.formulaHistory.length">{{ t('spc.segmentDetails.noFormula') }}</p>
            <details v-for="row in detail.formulaHistory" :key="row.id" class="rounded border border-slate-200 dark:border-slate-700 p-2">
              <summary class="cursor-pointer">{{ t('spc.segmentDetails.formulaVersion', { version: row.versionNo, date: formatChangeDate(row.changedAt), type: row.changeType }) }}</summary>
              <p class="whitespace-pre-wrap break-words">{{ t('spc.segmentDetails.reason', { reason: row.reason || t('spc.segmentDetails.notRecorded') }) }}</p>
              <p>{{ t('spc.segmentDetails.changedBy', { name: row.changedBy || t('spc.segmentDetails.notRecorded') }) }}</p>
              <pre class="whitespace-pre-wrap break-words text-xs mt-2">{{ formulaText(row.configJson) }}</pre>
            </details>
            <div class="flex flex-wrap gap-3"><button type="button" class="underline disabled:opacity-40" :disabled="formulaSkip === 0" @click="changePage('formula', -pageSize)">{{ t('spc.segmentDetails.formulaPrevious') }}</button><span>{{ t('spc.segmentDetails.page', { page: formulaSkip / pageSize + 1 }) }}</span><button type="button" class="underline disabled:opacity-40" :disabled="!detail.hasMoreFormula" @click="changePage('formula', pageSize)">{{ t('spc.segmentDetails.formulaNext') }}</button></div>
            <p>{{ t('spc.segmentDetails.fNotice') }}</p>
          </template>
          <p v-else>{{ t('spc.segmentDetails.notApplicable') }}</p>
          <details v-if="detail.currentStatisticalConfigJson"><summary class="cursor-pointer">{{ t('spc.segmentDetails.statisticalConfig') }}</summary><pre class="whitespace-pre-wrap break-words text-xs mt-2">{{ snapshotText(detail.currentStatisticalConfigJson) }}</pre></details>
        </section>
      </div>
    </div>
  </section>
</template>
