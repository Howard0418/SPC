<script setup>
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
const { t } = useI18n({ useScope: "global" });
const messageText = value => displayUiMessage(t, value);
import { watch, ref } from "vue";
import { api, getApiErrorDisplay } from "../api/client";
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
    if (current === request) error.value = getApiErrorDisplay(e);
  } finally {
    if (current === request) loading.value = false;
  }
}
watch(() => props.ppcId, () => { query = { skip: 0, take: 21 }; load(); }, { immediate: true });
</script>
<template>
  <section :aria-label="t('spc.formulaHistory.title')" class="rounded-xl border border-emerald-200 bg-white p-4 dark:border-emerald-900 dark:bg-slate-900">
    <div class="flex flex-wrap items-center justify-between gap-2">
      <h2 class="font-bold">{{ t('spc.formulaHistory.title') }} · {{ title }}</h2>
      <button type="button" @click="load()" :disabled="loading" class="rounded border px-3 py-1 text-sm">{{ t('spc.formulaHistory.reload') }}</button>
    </div>
    <p class="mt-2 text-xs text-slate-500">{{ t('spc.formulaHistory.help') }}</p>
    <VersionHistoryRange :reset-key="ppcId" :loading="loading" :has-more="hasMore" @change="load" />
    <p v-if="loading" class="py-3">{{ t('spc.formulaHistory.loading') }}</p>
    <div v-else-if="error" role="alert" class="py-3 text-red-600">{{ messageText(error) }} <button type="button" @click="load()" class="underline">{{ t('spc.formulaHistory.retry') }}</button></div>
    <p v-else-if="!versions.length" class="py-3 text-slate-500">{{ t('spc.formulaHistory.empty') }}</p>
    <article v-for="version in versions" :key="version.id" class="mt-3 rounded-lg border p-3 dark:border-slate-700">
      <h3 class="font-bold">v{{ version.versionNo }} · {{ version.changeType }}</h3>
      <p class="text-xs text-slate-500">{{ version.changedBy || 'System' }} · {{ new Date(version.changedAt).toLocaleString('zh-TW', { timeZone: 'Asia/Taipei' }) }}</p>
      <p class="mt-2 whitespace-pre-wrap break-all text-sm">{{ t('spc.formulaHistory.reasonLabel') }}{{ version.reason || t('spc.formulaHistory.notProvided') }}</p>
      <details class="mt-2 text-sm"><summary class="cursor-pointer">{{ t('spc.formulaHistory.snapshot') }}</summary><pre class="mt-2 whitespace-pre-wrap break-all font-mono text-xs">{{ version.configJson || t('spc.formulaHistory.notSet') }}</pre></details>
    </article>
  </section>
</template>
