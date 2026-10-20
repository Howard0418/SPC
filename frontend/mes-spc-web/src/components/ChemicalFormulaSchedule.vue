<script setup>
import { ref, watch, onBeforeUnmount, computed } from 'vue';
import { api, getApiErrorMessage } from '../api/client';
import { getCurrentUser } from '../utils/auth';
const props = defineProps({ ppcId: { type: Number, required: true } });
const revision = ref(null), periods = ref([]), versions = ref([]), fVersions = ref([]), reason = ref('');
const busy = ref(false), loaded = ref(false), error = ref(''), message = ref(''), result = ref(null);
const gaps = ref([]);
const measuredAt = ref(''), primary = ref(''), secondary = ref('');
const canPublish = computed(() => ['Admin', 'Editor'].includes(getCurrentUser()?.role));
let generation = 0;
onBeforeUnmount(() => generation++);
const url = () => `/part-process-characteristics/${props.ppcId}/chemical-formula-schedule`;
function taipeiDay(value, end = false) {
  if (!value) return '';
  return new Date(new Date(value).getTime() + 8 * 3600000 - (end ? 86400000 : 0)).toISOString().slice(0, 10);
}
async function load() {
  const request = ++generation; busy.value = true; loaded.value = false; error.value = ''; result.value = null;
  try {
    const responses = await Promise.allSettled([api.get(url()), api.get(`/part-process-characteristics/${props.ppcId}/chemical-analysis-formula-versions`, { params: { take: 100 } }), api.get('/v1/chemical-f-table/versions', { params: { take: 100 } })]);
    if (request !== generation) return;
    const failed = responses.find(r => r.status === 'rejected'); if (failed) throw failed.reason;
    const [schedule, formulas, f] = responses.map(r => r.value.data);
    const data = schedule.data;
    revision.value = data.revisionId; message.value = data.message; gaps.value = data.gaps || [];
    periods.value = (data.periods || []).map(p => ({ formulaVersionId: p.formulaVersionId, fTableHistoryId: p.fTableHistoryId, fromDate: taipeiDay(p.effectiveFromUtc), toDate: taipeiDay(p.effectiveUntilUtcExclusive, true) }));
    versions.value = formulas.data || []; fVersions.value = f || []; reason.value = ''; loaded.value = true;
  } catch (e) { if (request === generation) error.value = getApiErrorMessage(e); }
  finally { if (request === generation) busy.value = false; }
}
watch(() => props.ppcId, load, { immediate: true });
watch([measuredAt, primary, secondary], () => result.value = null);
function add() { periods.value.push({ formulaVersionId: versions.value[0]?.id || null, fTableHistoryId: null, fromDate: '', toDate: '' }); }
async function publish() {
  if (!periods.value.length || !reason.value.trim()) { error.value = '請新增期間並填寫發布依據。'; return; }
  if (periods.value.some(p => !p.fromDate || !p.formulaVersionId || (p.toDate && p.fromDate > p.toDate))) { error.value = '請選版本與有效起迄日，起日不可晚於迄日。'; return; }
  busy.value = true; error.value = ''; result.value = null; const request = generation;
  try {
    await api.post(url(), { baseRevisionId: revision.value, reason: reason.value, periods: periods.value.map(p => ({ ...p, toDate: p.toDate || null })) });
    if (request === generation) { await load(); message.value = '排程發布成功；尚未切換既有量測寫入。'; }
  } catch (e) { if (request === generation) error.value = getApiErrorMessage(e); }
  finally { if (request === generation) busy.value = false; }
}
async function preview() {
  busy.value = true; error.value = ''; result.value = null; const request = generation;
  try {
    const { data } = await api.post(`${url()}/preview`, { measuredAt: measuredAt.value, primary: primary.value, secondary: secondary.value || null, sourceKey: `settings-preview-${props.ppcId}` });
    if (request === generation) result.value = data.data;
  } catch (e) { if (request === generation) error.value = getApiErrorMessage(e); }
  finally { if (request === generation) busy.value = false; }
}
</script>
<template>
  <section aria-label="公式生效排程" class="rounded-xl border border-emerald-300 p-3 space-y-3 dark:border-emerald-800 dark:text-slate-100" @keydown.enter.prevent>
    <h3 class="font-bold">公式生效排程（台北日期）</h3>
    <p class="text-xs">沿用已儲存的公式版本；若剛修改公式，請先儲存再重新開啟。發布會取代整份排程，已發行憑證時點的公式／F快照須保留，可設定未使用日期的未來版本。</p>
    <p class="text-xs text-amber-700 dark:text-amber-300">{{ message }} 日期空缺不會套用最新版本；迄日包含當天，空白代表無期限。</p>
    <p v-if="error" role="alert" class="text-red-700 dark:text-red-300">{{ error }}</p>
    <button type="button" :disabled="busy" @click="load" class="underline disabled:opacity-50">重新載入排程</button>
    <p v-if="busy" role="status">處理中…</p>
    <template v-if="loaded">
      <p>目前排程修訂：{{ revision ?? '尚未發布' }}</p>
      <p v-for="gap in gaps" :key="gap.fromUtc" class="text-amber-700 dark:text-amber-300">空缺日期：{{ taipeiDay(gap.fromUtc) }} 至 {{ taipeiDay(gap.untilUtc, true) }}，此期間無適用公式。</p>
      <fieldset v-for="(p, index) in periods" :key="index" :disabled="busy || !canPublish" class="grid gap-2 md:grid-cols-2 rounded border p-2 dark:border-slate-700 disabled:opacity-60">
        <label>公式版本
          <select v-model="p.formulaVersionId" :aria-label="`期間${index + 1}公式版本`" class="w-full rounded border bg-white p-1 dark:bg-slate-900 dark:border-slate-600">
            <option v-if="p.formulaVersionId && !versions.some(v => v.id === p.formulaVersionId)" :value="p.formulaVersionId">既有版本 ID {{ p.formulaVersionId }}</option>
            <option v-for="v in versions" :key="v.id" :value="v.id">v{{ v.versionNo }} · {{ v.reason || '未填參照文件' }}</option>
          </select>
        </label>
        <label>F表歷史快照
          <select v-model="p.fTableHistoryId" :aria-label="`期間${index + 1}F快照`" class="w-full rounded border bg-white p-1 dark:bg-slate-900 dark:border-slate-600">
            <option :value="null">不使用F表</option>
            <option v-if="p.fTableHistoryId && !fVersions.some(v => v.id === p.fTableHistoryId)" :value="p.fTableHistoryId">既有快照 ID {{ p.fTableHistoryId }}</option>
            <option v-for="f in fVersions" :key="f.id" :value="f.id">{{ f.versionCode }} · 歷史v{{ f.versionNo }}</option>
          </select>
        </label>
        <label>生效起日<input v-model="p.fromDate" type="date" :aria-label="`期間${index + 1}起日`" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <label>生效迄日（含）<input v-model="p.toDate" type="date" :aria-label="`期間${index + 1}迄日`" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <button v-if="canPublish" type="button" @click="periods.splice(index, 1)" class="text-red-600 dark:text-red-300">移除期間 {{ index + 1 }}</button>
      </fieldset>
      <p v-if="!periods.length">尚無適用期間；所有日期皆無可選版本。</p>
      <p class="text-xs">版本選項顯示最近100筆；已排程的較舊版本仍保留。F歷史為空時，須先在F表維護留下版本歷史。</p>
      <template v-if="canPublish">
        <button type="button" :disabled="busy" @click="add" class="underline disabled:opacity-50">新增生效期間</button>
        <label class="block">發布依據<input v-model="reason" maxlength="500" :disabled="busy" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <button type="button" :disabled="busy" @click="publish" class="rounded bg-emerald-700 px-3 py-2 text-white disabled:opacity-50">發布公式排程</button>
      </template>
      <h4 class="font-semibold">依日期試算（不儲存量測）</h4>
      <div class="grid gap-2 md:grid-cols-2">
        <label>量測時間（含時區）<input v-model="measuredAt" :disabled="busy" placeholder="2026-11-01T00:00:00+08:00" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <label>Primary原始值<input v-model="primary" :disabled="busy" inputmode="decimal" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <label>Secondary原始值<input v-model="secondary" :disabled="busy" inputmode="decimal" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
      </div>
      <button type="button" :disabled="busy" @click="preview" class="underline disabled:opacity-50">依量測日期試算</button>
      <div v-if="result" role="status" class="rounded bg-emerald-50 p-2 dark:bg-emerald-950">
        <p>套用公式 v{{ result.formulaVersionNo }}（ID {{ result.formulaVersionId }}），排程 {{ result.revisionId }}；F快照 {{ result.fTableHistoryId ?? '無' }}</p>
        <p>原始計算結果 {{ result.result.raw }}；儲存結果 {{ result.result.stored }}；判定 {{ result.result.judgment }}</p>
        <p class="text-xs">{{ result.engineVersion }}；此結果未匯入。</p>
      </div>
    </template>
  </section>
</template>
