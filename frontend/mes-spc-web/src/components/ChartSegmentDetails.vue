<script setup>
import { computed, ref, watch } from "vue";
import { api, getApiErrorMessage } from "../api/client";

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
const formatRangeDate = value => value ? String(value).replace("T", " ") : "未設定";
const formatChangeDate = value => value ? new Date(value).toLocaleString("zh-TW", { timeZone: "Asia/Taipei", hour12: false }) : "未記錄";
function formulaText(json) {
  if (!json) return "未設定";
  try {
    const value = JSON.parse(json);
    if (!value || typeof value !== "object") return "無法解析公式設定";
    const fields = [["濃度", "concentrationFormula"], ["調整判斷", "adjustmentFormula"], ["添加量", "adjustmentAmountFormula"]];
    return fields.filter(([, key]) => value[key]).map(([label, key]) => `${label}：${value[key]}`).join("\n") || "未設定公式";
  } catch { return "無法解析公式設定"; }
}
function snapshotText(json) {
  if (!json) return "無快照（可能為刪除紀錄）";
  try { return JSON.stringify(JSON.parse(json), null, 2); }
  catch { return "無法解析歷史快照"; }
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
    if (request === listRequest) listError.value = getApiErrorMessage(e);
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
    if (request === detailRequest) error.value = getApiErrorMessage(e);
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
  <section aria-label="日期分段與版本明細" class="my-3 min-w-0 break-words rounded-xl border border-slate-200 dark:border-slate-700 p-3 text-sm text-slate-700 dark:text-slate-200">
    <h3 class="font-bold">日期分段與版本明細</h3>
    <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ chartLineEntry ? '點選日期區間或圖上的分段規格線查看。' : '點選日期區間查看。' }}以下為目前設定與修改歷史，查閱不會重新計算資料。</p>
    <div v-if="listError" role="alert" class="mt-2 text-red-600 dark:text-red-400">
      {{ listError }} <button type="button" class="underline" @click="loadSegments">重試分段清單</button>
    </div>
    <div v-else class="mt-2 flex flex-wrap gap-2">
      <button type="button" class="rounded border border-slate-300 dark:border-slate-600 px-3 py-2 hover:bg-blue-50 dark:hover:bg-slate-800" :aria-pressed="opened && selectedId === null" @click="openSegment()">主檔（未命中分段時）</button>
      <button v-for="segment in segments" :key="segment.id" type="button" :aria-pressed="opened && selectedId === segment.id"
        class="rounded border border-blue-300 dark:border-blue-700 px-3 py-2 hover:bg-blue-50 dark:hover:bg-slate-800"
        @click="openSegment(segment.id)">
        {{ formatRangeDate(segment.startDate) }} ～ {{ segment.endDate ? formatRangeDate(segment.endDate) : '持續有效（無迄日）' }}
      </button>
      <p v-if="!segments.length" class="self-center text-xs text-slate-500 dark:text-slate-400">目前沒有日期分段，可查看主檔設定。</p>
    </div>
    <div v-if="opened" class="mt-3 border-t border-slate-200 dark:border-slate-700 pt-3" aria-live="polite">
      <div class="flex justify-between gap-2"><h4 class="font-bold">分段查閱明細</h4><button type="button" class="underline" @click="close">關閉明細</button></div>
      <p v-if="loading" role="status" class="mt-2">載入版本明細中…</p>
      <div v-else-if="error" role="alert" class="mt-2 text-red-600 dark:text-red-400">{{ error }} <button type="button" class="underline" @click="loadDetail">重試明細</button></div>
      <div v-else-if="detail" class="mt-2 space-y-4">
        <p data-testid="segment-range">{{ detail.segmentId ? `分段 #${detail.segmentId}：${formatRangeDate(detail.startDate)} ～ ${detail.endDate ? formatRangeDate(detail.endDate) : '無迄日'}` : '主檔設定（僅未命中分段或分段未填欄位時回退）' }}</p>
        <p v-if="detail.note" class="whitespace-pre-wrap break-words">分段備註：{{ detail.note }}</p>
        <div class="grid grid-cols-2 md:grid-cols-3 gap-2">
          <div v-for="(limit, name) in detail.limits" :key="name" class="rounded bg-slate-50 dark:bg-slate-800 p-2">
            <strong>{{ name }}：{{ limit.value ?? '未設定' }}</strong>
            <p class="text-xs text-slate-500 dark:text-slate-400">{{ limit.source === 'Segment' ? '來源：本分段' : detail.segmentId ? '來源：目前主檔回退值' : '來源：目前主檔' }}</p>
          </div>
        </div>
        <p class="text-xs text-amber-700 dark:text-amber-400">規格／界限顯示目前設定；修改快照不是量測使用版本。管制界限尚無獨立版本歷史，無法確認當時版本。未設定的管制界限可能由圖型統計推算，請對照圖表。</p>
        <section aria-label="分段規格修改歷史" class="space-y-2">
          <h4 class="font-bold">{{ detail.segmentId ? '本分段' : '主檔' }}規格修改歷史</h4>
          <p v-if="!detail.specificationHistory.length">沒有規格版本紀錄；不補造歷史。</p>
          <details v-for="row in detail.specificationHistory" :key="row.id" class="rounded border border-slate-200 dark:border-slate-700 p-2">
            <summary class="cursor-pointer">規格修改 v{{ row.versionNo }}｜{{ formatChangeDate(row.changedAt) }}（台北）｜{{ row.changeType }}</summary>
            <p>修改人：{{ row.changedBy || '未記錄' }}</p>
            <pre class="whitespace-pre-wrap break-words text-xs mt-2">{{ snapshotText(row.snapshotJson) }}</pre>
          </details>
          <div class="flex flex-wrap gap-3"><button type="button" class="underline disabled:opacity-40" :disabled="specificationSkip === 0" @click="changePage('specification', -pageSize)">規格上一頁</button><span>第 {{ specificationSkip / pageSize + 1 }} 頁</span><button type="button" class="underline disabled:opacity-40" :disabled="!detail.hasMoreSpecification" @click="changePage('specification', pageSize)">規格下一頁</button></div>
        </section>
        <section aria-label="分析公式與版本歷史" class="space-y-2">
          <h4 class="font-bold">分析公式與版本歷史</h4>
          <template v-if="isChemical">
            <p class="text-amber-700 dark:text-amber-400">當時實際使用的公式版本：無法確認當時版本。修改日期不是生效日期，以下歷史涵蓋此管制項目全部修改，不代表本分段適用公式。</p>
            <h5 class="font-bold">目前分析公式（僅供參考）</h5>
            <pre class="whitespace-pre-wrap break-words rounded bg-slate-50 dark:bg-slate-800 p-2 text-xs">{{ formulaText(detail.currentFormulaConfigJson) }}</pre>
            <p v-if="!detail.formulaHistory.length">沒有公式版本紀錄；不補造歷史。</p>
            <details v-for="row in detail.formulaHistory" :key="row.id" class="rounded border border-slate-200 dark:border-slate-700 p-2">
              <summary class="cursor-pointer">公式修改 v{{ row.versionNo }}｜{{ formatChangeDate(row.changedAt) }}（台北）｜{{ row.changeType }}</summary>
              <p class="whitespace-pre-wrap break-words">參照文件／修改依據：{{ row.reason || '未記錄' }}</p>
              <p>修改人：{{ row.changedBy || '未記錄' }}</p>
              <pre class="whitespace-pre-wrap break-words text-xs mt-2">{{ formulaText(row.configJson) }}</pre>
            </details>
            <div class="flex flex-wrap gap-3"><button type="button" class="underline disabled:opacity-40" :disabled="formulaSkip === 0" @click="changePage('formula', -pageSize)">公式上一頁</button><span>第 {{ formulaSkip / pageSize + 1 }} 頁</span><button type="button" class="underline disabled:opacity-40" :disabled="!detail.hasMoreFormula" @click="changePage('formula', pageSize)">公式下一頁</button></div>
            <p>F 表使用版本：無法確認當時版本（量測未保存版本關聯）。</p>
          </template>
          <p v-else>藥液分析公式與 F 表：不適用。</p>
          <details v-if="detail.currentStatisticalConfigJson"><summary class="cursor-pointer">目前管制線計算設定（非當時版本證據）</summary><pre class="whitespace-pre-wrap break-words text-xs mt-2">{{ snapshotText(detail.currentStatisticalConfigJson) }}</pre></details>
        </section>
      </div>
    </div>
  </section>
</template>
