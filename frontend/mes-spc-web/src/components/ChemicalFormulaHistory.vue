<script setup>
import { watch, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";
const props = defineProps({ ppcId: { type: Number, required: true }, title: { type: String, default: "" } });
const versions = ref([]);
const loading = ref(false);
const error = ref("");
let request = 0;
async function load() {
  const current = ++request;
  const id = props.ppcId;
  versions.value = [];
  error.value = "";
  loading.value = true;
  try {
    const { data } = await api.get(`/part-process-characteristics/${id}/chemical-analysis-formula-versions`);
    if (current === request) versions.value = data?.data || [];
  } catch (e) {
    if (current === request) error.value = getApiErrorMessage(e);
  } finally {
    if (current === request) loading.value = false;
  }
}
watch(() => props.ppcId, load, { immediate: true });
</script>
<template>
  <section aria-label="公式版本歷史" class="rounded-xl border border-emerald-200 bg-white p-4 dark:border-emerald-900 dark:bg-slate-900">
    <div class="flex flex-wrap items-center justify-between gap-2">
      <h2 class="font-bold">公式版本歷史 · {{ title }}</h2>
      <button type="button" @click="load" :disabled="loading" class="rounded border px-3 py-1 text-sm">重新載入</button>
    </div>
    <p class="mt-2 text-xs text-slate-500">與管制項目設定共用同一份歷史；最新版本列於最上方。回復請至管制項目設定操作。</p>
    <p v-if="loading" class="py-3">載入版本歷史中…</p>
    <div v-else-if="error" role="alert" class="py-3 text-red-600">{{ error }} <button type="button" @click="load" class="underline">重試</button></div>
    <p v-else-if="!versions.length" class="py-3 text-slate-500">尚無公式版本紀錄；不會補造過去歷史。</p>
    <article v-for="(version, index) in versions" :key="version.id" class="mt-3 rounded-lg border p-3 dark:border-slate-700">
      <h3 class="font-bold">v{{ version.versionNo }}{{ index === 0 ? ' · 最新版本' : '' }} · {{ version.changeType }}</h3>
      <p class="text-xs text-slate-500">{{ version.changedBy || 'System' }} · {{ new Date(version.changedAt).toLocaleString('zh-TW', { timeZone: 'Asia/Taipei' }) }}</p>
      <p class="mt-2 whitespace-pre-wrap break-all text-sm">參照文件／修改依據：{{ version.reason || '未填寫' }}</p>
      <details class="mt-2 text-sm"><summary class="cursor-pointer">公式快照</summary><pre class="mt-2 whitespace-pre-wrap break-all font-mono text-xs">{{ version.configJson || '未設定' }}</pre></details>
    </article>
  </section>
</template>
