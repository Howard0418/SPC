<script setup>
import { useI18n } from 'vue-i18n';
import { displayUiMessage } from '../i18n/uiMessage';
const { t } = useI18n({ useScope: 'global' });
const messageText = value => displayUiMessage(t, value);
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
  if (!periods.value.length || !reason.value.trim()) { error.value = { key: "spc.schedule.addAnEffectivePeriodAndEnterAPublicationReason" }; return; }
  if (periods.value.some(p => !p.fromDate || !p.formulaVersionId || (p.toDate && p.fromDate > p.toDate))) { error.value = { key: "spc.schedule.selectAVersionAndEffectiveDatesStartDateMustNotBeLaterThanEndDate" }; return; }
  busy.value = true; error.value = ''; result.value = null; const request = generation;
  try {
    await api.post(url(), { baseRevisionId: revision.value, reason: reason.value, periods: periods.value.map(p => ({ ...p, toDate: p.toDate || null })) });
    if (request === generation) { await load(); message.value = { key: "spc.schedule.schedulePublishedExistingMeasurementWritesHaveNotBeenSwitched" }; }
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
  <section :aria-label="t('spc.schedule.formulaEffectiveSchedule')" class="rounded-xl border border-emerald-300 p-3 space-y-3 dark:border-emerald-800 dark:text-slate-100" @keydown.enter.prevent>
    <h3 class="font-bold">{{ t('spc.schedule.formulaEffectiveScheduleTaipeiDates') }}</h3>
    <p class="text-xs">{{ t('spc.schedule.usesSavedFormulaVersionsSaveFormulaChangesAndReopenFirstPublishingReplacesTheEntireScheduleFormulaAn') }}</p>
    <p class="text-xs text-amber-700 dark:text-amber-300">{{ messageText(message) }} {{ t('spc.schedule.dateGapsDoNotUseTheLatestVersionTheEndDateIsInclusiveBlankMeansNoExpiry') }}</p>
    <p v-if="error" role="alert" class="text-red-700 dark:text-red-300">{{ messageText(error) }}</p>
    <button type="button" :disabled="busy" data-testid="schedule-reload" @click="load" class="underline disabled:opacity-50">{{ t('spc.schedule.reloadSchedule') }}</button>
    <p v-if="busy" role="status">{{ t('spc.schedule.processing') }}</p>
    <template v-if="loaded">
      <p>{{ t('spc.schedule.currentScheduleRevision') }}{{ revision ?? t('spc.schedule.notPublished') }}</p>
      <p v-for="gap in gaps" :key="gap.fromUtc" class="text-amber-700 dark:text-amber-300">{{ t('spc.schedule.gap', { from: taipeiDay(gap.fromUtc), to: taipeiDay(gap.untilUtc, true) }) }}</p>
      <fieldset v-for="(p, index) in periods" :key="index" :disabled="busy || !canPublish" class="grid gap-2 md:grid-cols-2 rounded border p-2 dark:border-slate-700 disabled:opacity-60">
        <label>{{ t('spc.schedule.formulaVersion') }}
          <select data-testid="schedule-p.formulaVersionId" v-model="p.formulaVersionId" :aria-label="t('spc.schedule.versionAria', { index: index + 1 })" class="w-full rounded border bg-white p-1 dark:bg-slate-900 dark:border-slate-600">
            <option v-if="p.formulaVersionId && !versions.some(v => v.id === p.formulaVersionId)" :value="p.formulaVersionId">{{ t('spc.schedule.existingVersion', { id: p.formulaVersionId }) }}</option>
            <option v-for="v in versions" :key="v.id" :value="v.id">v{{ v.versionNo }} · {{ v.reason || t('spc.schedule.noReferenceDocument') }}</option>
          </select>
        </label>
        <label>{{ t('spc.schedule.fTableHistorySnapshot') }}
          <select data-testid="schedule-p.fTableHistoryId" v-model="p.fTableHistoryId" :aria-label="t('spc.schedule.snapshotAria', { index: index + 1 })" class="w-full rounded border bg-white p-1 dark:bg-slate-900 dark:border-slate-600">
            <option :value="null">{{ t('spc.schedule.doNotUseAnFTable') }}</option>
            <option v-if="p.fTableHistoryId && !fVersions.some(v => v.id === p.fTableHistoryId)" :value="p.fTableHistoryId">{{ t('spc.schedule.existingSnapshot', { id: p.fTableHistoryId }) }}</option>
            <option v-for="f in fVersions" :key="f.id" :value="f.id">{{ f.versionCode }} · {{ t('spc.schedule.historyVersion', { version: f.versionNo }) }}</option>
          </select>
        </label>
        <label>{{ t('spc.schedule.effectiveStartDate') }}<input data-testid="schedule-p.fromDate" v-model="p.fromDate" type="date" :aria-label="t('spc.schedule.fromAria', { index: index + 1 })" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <label>{{ t('spc.schedule.effectiveEndDateInclusive') }}<input data-testid="schedule-p.toDate" v-model="p.toDate" type="date" :aria-label="t('spc.schedule.toAria', { index: index + 1 })" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <button v-if="canPublish" type="button" @click="periods.splice(index, 1)" class="text-red-600 dark:text-red-300">{{ t('spc.schedule.removePeriod', { index: index + 1 }) }}</button>
      </fieldset>
      <p v-if="!periods.length">{{ t('spc.schedule.noEffectivePeriodsNoVersionIsAvailableForAnyDate') }}</p>
      <p class="text-xs">{{ t('spc.schedule.optionsShowTheLatest100VersionsOlderScheduledVersionsAreRetainedIfFHistoryIsEmptyCreateVersionHistor') }}</p>
      <template v-if="canPublish">
        <button type="button" :disabled="busy" data-testid="schedule-add" @click="add" class="underline disabled:opacity-50">{{ t('spc.schedule.addEffectivePeriod') }}</button>
        <label class="block">{{ t('spc.schedule.publicationReason') }}<input data-testid="schedule-reason" v-model="reason" maxlength="500" :disabled="busy" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <button type="button" :disabled="busy" data-testid="schedule-publish" @click="publish" class="rounded bg-emerald-700 px-3 py-2 text-white disabled:opacity-50">{{ t('spc.schedule.publishFormulaSchedule') }}</button>
      </template>
      <h4 class="font-semibold">{{ t('spc.schedule.calculateByDateWithoutSavingMeasurements') }}</h4>
      <div class="grid gap-2 md:grid-cols-2">
        <label>{{ t('spc.schedule.measurementTimeWithTimeZone') }}<input data-testid="schedule-measuredAt" v-model="measuredAt" :disabled="busy" placeholder="2026-11-01T00:00:00+08:00" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <label>{{ t('spc.schedule.primaryRawValue') }}<input data-testid="schedule-primary" v-model="primary" :disabled="busy" inputmode="decimal" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
        <label>{{ t('spc.schedule.secondaryRawValue') }}<input data-testid="schedule-secondary" v-model="secondary" :disabled="busy" inputmode="decimal" class="block w-full rounded border p-1 dark:bg-slate-900 dark:border-slate-600"></label>
      </div>
      <button type="button" :disabled="busy" data-testid="schedule-preview" @click="preview" class="underline disabled:opacity-50">{{ t('spc.schedule.calculateByMeasurementDate') }}</button>
      <div v-if="result" role="status" class="rounded bg-emerald-50 p-2 dark:bg-emerald-950">
        <p>{{ t('spc.schedule.selectedResult', { version: result.formulaVersionNo, id: result.formulaVersionId, revision: result.revisionId, snapshot: result.fTableHistoryId ?? t('spc.schedule.none') }) }}</p>
        <p>{{ t('spc.schedule.calculatedResult', { raw: result.result.raw, stored: result.result.stored, judgment: result.result.judgment }) }}</p>
        <p class="text-xs">{{ t('spc.schedule.notImported', { engine: result.engineVersion }) }}</p>
      </div>
    </template>
  </section>
</template>
