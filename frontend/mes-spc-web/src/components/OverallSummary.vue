<script setup>
import { computed, onBeforeUnmount, ref } from "vue";
import { useI18n } from "vue-i18n";
import { api, getApiErrorMessage } from "../api/client";

const { t, locale } = useI18n({ useScope: "global" });
const props = defineProps({ startDate: { type: String, required: true }, endDate: { type: String, required: true } });
const loading = ref(false);
const error = ref("");
const result = ref(null);
let request;
const count = value => Number(value).toLocaleString(locale.value);
const cards = computed(() => {
  if (!result.value) return [];
  const s = result.value.summary;
  return [
    { key: "items", title: t("spc.overall.items"), value: t("spc.overall.itemValue", { count: count(s.controlItemCount) }), detail: t("spc.overall.registered", { total: count(s.registeredItemCount), disabled: count(s.disabledItemCount) }) },
    { key: "ooc", title: "OOC", value: t("spc.overall.caseValue", { count: count(s.oocCount) }), detail: t("spc.overall.affected", { count: count(s.oocItemCount) }) },
    { key: "oos", title: "OOS", value: t("spc.overall.caseValue", { count: count(s.oosCount) }), detail: t("spc.overall.affected", { count: count(s.oosItemCount) }) },
    { key: "handled", title: t("spc.overall.handled"), value: t("spc.overall.caseValue", { count: count(s.handledCount) }), detail: t("spc.overall.handledHint") },
    { key: "unhandled", title: t("spc.overall.unhandled"), value: t("spc.overall.caseValue", { count: count(s.unhandledCount) }), detail: t("spc.overall.unhandledHint") }
  ];
});

async function load() {
  error.value = "";
  const difference = (Date.parse(props.endDate) - Date.parse(props.startDate)) / 86400000;
  if (!props.startDate || !props.endDate || !Number.isFinite(difference) || difference < 0 || difference > 93) {
    result.value = null;
    error.value = t("spc.overall.invalidDates");
    return;
  }
  request?.abort();
  request = new AbortController();
  loading.value = true;
  result.value = null;
  try {
    const { data } = await api.get("/v1/spc/overall", { params: { startDate: props.startDate, endDate: props.endDate }, signal: request.signal });
    if (!data?.success || !data.data?.summary) throw new Error(t("spc.overall.loadFailed"));
    result.value = data.data;
  } catch (e) {
    if (e.code !== "ERR_CANCELED") error.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}
defineExpose({ load });
onBeforeUnmount(() => request?.abort());
</script>

<template>
  <section data-testid="overall" class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-800 dark:bg-slate-900" :aria-busy="loading">
    <div>
      <div>
        <h2 class="text-lg font-bold text-slate-800 dark:text-white">{{ t('spc.overall.title') }}</h2>
        <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ t('spc.overall.scope') }}</p>
      </div>
    </div>
    <p v-if="loading" role="status" class="mt-4 text-sm text-slate-500 dark:text-slate-400">{{ t('spc.overall.loading') }}</p>
    <p v-if="error" role="alert" class="mt-4 rounded-lg bg-red-50 p-3 text-sm text-red-700 dark:bg-red-950 dark:text-red-300">{{ error }}</p>
    <div v-if="result && !loading" aria-live="polite">
      <p data-testid="overall-period" class="mt-4 text-xs text-slate-500 dark:text-slate-400">{{ result.startDate }} ～ {{ result.endDate }}</p>
      <p v-if="result.summary.unanalyzedItemCount > 0" role="status" class="mt-3 rounded-lg bg-amber-50 p-3 text-sm text-amber-800 dark:bg-amber-950 dark:text-amber-200">{{ t('spc.overall.partial', { count: count(result.summary.unanalyzedItemCount) }) }}</p>
      <div class="mt-3 grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-5">
        <article v-for="card in cards" :key="card.key" :data-testid="`overall-${card.key}`" class="rounded-xl bg-slate-50 p-4 dark:bg-slate-800">
          <h3 class="text-sm font-semibold text-slate-600 dark:text-slate-300">{{ card.title }}</h3>
          <p class="mt-2 text-2xl font-black text-slate-900 dark:text-white">{{ card.value }}</p>
          <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ card.detail }}</p>
        </article>
      </div>
      <p data-testid="overall-total" class="mt-3 text-sm font-semibold text-slate-700 dark:text-slate-200">{{ t('spc.overall.total', { count: count(result.summary.abnormalCount) }) }}</p>
      <p class="mt-1 text-xs leading-relaxed text-slate-500 dark:text-slate-400">{{ t('spc.overall.rules') }}</p>
    </div>
  </section>
</template>
