<script setup>
import { useI18n } from 'vue-i18n';
import { displayUiMessage } from '../i18n/uiMessage';
const { t } = useI18n({ useScope: 'global' });
const messageText = value => displayUiMessage(t, value);
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
function snapshot(value) { if (!value) return t("spc.specHistory.noneBeforeCreationAfterDeletion"); try { return JSON.stringify(JSON.parse(value), null, 2); } catch { return value; } }
</script>
<template>
  <section :aria-label="t('spc.specHistory.specificationLimitVersionHistory')" class="mt-4 rounded-xl border p-3 dark:border-slate-700">
    <h3 class="font-bold">{{ t('spc.specHistory.specificationLimitVersionHistory') }}</h3>
    <p class="text-xs text-slate-500">{{ t('spc.specHistory.recordsChangesMadeAfterVersionTrackingWasEnabledEarlierChangesWereNotRetainedIncludesBaseSpecificati') }}</p>
    <VersionHistoryRange :reset-key="ppcId" :loading="loading" :has-more="more" @change="load" />
    <p v-if="loading">{{ t('spc.specHistory.loadingHistory') }}</p><p v-else-if="error" role="alert" class="text-red-600">{{ error }} <button type="button" @click="load()" class="underline">{{ t('spc.specHistory.retry') }}</button></p>
    <p v-else-if="!rows.length" class="text-sm text-slate-500">{{ t('spc.specHistory.noSpecificationVersionsInThisModificationDateRange') }}</p>
    <article v-for="row in rows" :key="row.id" class="mt-2 rounded border p-3 dark:border-slate-700">
      <p class="font-bold">v{{ row.versionNo }} · {{ row.sourceKind === 'Segment' ? t('spc.specHistory.dateSegment') : t('spc.specHistory.baseSpecification') }} · {{ row.changeType }}</p>
      <p class="text-xs text-slate-500">{{ row.changedBy }} · {{ new Date(row.changedAt).toLocaleString('zh-TW', { timeZone: 'Asia/Taipei' }) }}</p>
      <details><summary class="cursor-pointer text-sm">{{ t('spc.specHistory.specificationsBeforeAndAfter') }}</summary><div class="grid gap-2 md:grid-cols-2"><pre class="whitespace-pre-wrap break-all text-xs">{{ t('spc.specHistory.before') }}{{ snapshot(row.previousSnapshotJson) }}</pre><pre class="whitespace-pre-wrap break-all text-xs">{{ t('spc.specHistory.after') }}{{ snapshot(row.snapshotJson) }}</pre></div></details>
    </article>
  </section>
</template>
