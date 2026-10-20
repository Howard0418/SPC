<script setup>
import { ref, watch } from 'vue';
import { api, getApiErrorMessage } from '../api/client';
import VersionHistoryRange from './VersionHistoryRange.vue';
const props = defineProps({ ppcId: { type: Number, required: true } });
const rows = ref([]); const loading = ref(false); const error = ref(''); const more = ref(false);
let query = { skip: 0, take: 21 }; let request = 0;
async function load(next) {
  if (next) query = next;
  const current = ++request; loading.value = true; error.value = ''; rows.value = []; more.value = false;
  try { const { data } = await api.get(`/part-process-characteristics/${props.ppcId}/specification-limit-versions`, { params: query });
    if (current === request) { const result = data?.data || []; more.value = result.length > 20; rows.value = result.slice(0, 20); }
  } catch (e) { if (current === request) error.value = getApiErrorMessage(e); }
  finally { if (current === request) loading.value = false; }
}
watch(() => props.ppcId, () => { query = { skip: 0, take: 21 }; load(); }, { immediate: true });
function snapshot(value) { if (!value) return '無（新增前／刪除後）'; try { return JSON.stringify(JSON.parse(value), null, 2); } catch { return value; } }
</script>
<template>
  <section aria-label="規格界線版本歷史" class="mt-4 rounded-xl border p-3 dark:border-slate-700">
    <h3 class="font-bold">規格界線版本歷史</h3>
    <p class="text-xs text-slate-500">僅記錄啟用版本追蹤後的異動；之前的修改未留存。包含基本規格與日期分段，與公式歷史分開。</p>
    <VersionHistoryRange :reset-key="ppcId" :loading="loading" :has-more="more" @change="load" />
    <p v-if="loading">載入歷史中…</p><p v-else-if="error" role="alert" class="text-red-600">{{ error }} <button type="button" @click="load()" class="underline">重試</button></p>
    <p v-else-if="!rows.length" class="text-sm text-slate-500">此修改日期區間沒有規格版本紀錄。</p>
    <article v-for="row in rows" :key="row.id" class="mt-2 rounded border p-3 dark:border-slate-700">
      <p class="font-bold">v{{ row.versionNo }} · {{ row.sourceKind === 'Segment' ? '日期分段' : '基本規格' }} · {{ row.changeType }}</p>
      <p class="text-xs text-slate-500">{{ row.changedBy }} · {{ new Date(row.changedAt).toLocaleString('zh-TW', { timeZone: 'Asia/Taipei' }) }}</p>
      <details><summary class="cursor-pointer text-sm">修改前後規格</summary><div class="grid gap-2 md:grid-cols-2"><pre class="whitespace-pre-wrap break-all text-xs">修改前：{{ snapshot(row.previousSnapshotJson) }}</pre><pre class="whitespace-pre-wrap break-all text-xs">修改後：{{ snapshot(row.snapshotJson) }}</pre></div></details>
    </article>
  </section>
</template>
