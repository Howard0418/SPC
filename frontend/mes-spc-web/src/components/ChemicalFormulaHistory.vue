<script setup>
import { watch, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import VersionHistoryRange from "./VersionHistoryRange.vue";
const hasMore = ref(false);
let query = { skip: 0, take: 21 };
const props = defineProps({ ppcId: { type: Number, required: true }, title: { type: String, default: "" } });
const versions = ref([]);
const loading = ref(false);
const error = ref("");
let request = 0;
async function load(next) {
  if (next) query = next;
  const current = ++request;
  const id = props.ppcId;
  versions.value = [];
  hasMore.value = false;
  error.value = "";
  loading.value = true;
  try {
    const { data } = await api.get(`/part-process-characteristics/${id}/chemical-analysis-formula-versions`, { params: query });
    if (current === request) { const result = data?.data || []; hasMore.value = result.length > 20; versions.value = result.slice(0, 20); }
  } catch (e) {
    if (current === request) error.value = getApiErrorMessage(e);
  } finally {
    if (current === request) loading.value = false;
  }
}
watch(() => props.ppcId, () => { query = { skip: 0, take: 21 }; load(); }, { immediate: true });
</script>
<template>
  <section aria-label="公式版本歷史" class="rounded-xl border border-emerald-200 bg-white p-4 dark:border-emerald-900 dark:bg-slate-900">
    <div class="flex flex-wrap items-center justify-between gap-2">
      <h2 class="font-bold">公式版本歷史 · {{ title }}</h2>
      <button type="button" @click="load()" :disabled="loading" class="rounded border px-3 py-1 text-sm">重新載入</button>
    </div>
    <p class="mt-2 text-xs text-slate-500">與管制項目設定共用同一份歷史；依版號由新到舊排列。回復請至管制項目設定操作。</p>
    <VersionHistoryRange :reset-key="ppcId" :loading="loading" :has-more="hasMore" @change="load" />
    <p v-if="loading" class="py-3">載入版本歷史中…</p>
    <div v-else-if="error" role="alert" class="py-3 text-red-600">{{ error }} <button type="button" @click="load()" class="underline">重試</button></div>
    <p v-else-if="!versions.length" class="py-3 text-slate-500">此修改日期區間沒有公式版本紀錄；不會補造過去歷史。</p>
    <article v-for="version in versions" :key="version.id" class="mt-3 rounded-lg border p-3 dark:border-slate-700">
      <h3 class="font-bold">v{{ version.versionNo }} · {{ version.changeType }}</h3>
      <p class="text-xs text-slate-500">{{ version.changedBy || 'System' }} · {{ new Date(version.changedAt).toLocaleString('zh-TW', { timeZone: 'Asia/Taipei' }) }}</p>
      <p class="mt-2 whitespace-pre-wrap break-all text-sm">參照文件／修改依據：{{ version.reason || '未填寫' }}</p>
      <details class="mt-2 text-sm"><summary class="cursor-pointer">公式快照</summary><pre class="mt-2 whitespace-pre-wrap break-all font-mono text-xs">{{ version.configJson || '未設定' }}</pre></details>
    </article>
  </section>
</template>
