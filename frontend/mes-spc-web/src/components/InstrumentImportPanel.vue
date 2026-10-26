<script setup>
import {useI18n} from "vue-i18n";
import {displayUiMessage} from "../i18n/uiMessage";
import LocalePicker from "../components/LocalePicker.vue";
const {t}=useI18n({useScope:"global"});
import { computed, onMounted, ref } from 'vue';
import { api, getApiErrorMessage } from '../api/client';

defineProps({ operators: { type: Array, default: () => [] } });
const emit = defineEmits(['imported']);
const canManage = ref(false);
const open = ref(false);
const busy = ref(false);
const error = ref('');
const accessError = ref('');
const file = ref(null);
const preview = ref(null);
const selected = ref([]);
const result = ref(null);
const confirmed = ref(false);
const department = ref('');
const custodian = ref('');
const status = ref('');
const includeCustodian = ref(true);
const ready = computed(() => preview.value?.rows.filter(row => row.status === 'Ready') || []);
const canSubmit = computed(() => !busy.value && !result.value && selected.value.length > 0 && department.value.trim() && custodian.value && status.value && confirmed.value);
const inputClass = 'w-full rounded border border-slate-300 bg-white px-3 py-2 text-slate-900 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

onMounted(async () => {
  try { canManage.value = !!(await api.get('/v1/instruments/import/access')).data?.data?.canManage; }
  catch (e) { accessError.value = {key:"spc.calibration.accessFailed",args:{error:getApiErrorMessage(e)}}; }
});
function changeFile(event) {
  file.value = event.target.files?.[0] || null;
  preview.value = null; selected.value = []; result.value = null; confirmed.value = false; error.value = '';
}
async function readPreview() {
  if (!file.value || !/\.xlsx$/i.test(file.value.name) || file.value.size > 10 * 1024 * 1024) {
    error.value = {key:'spc.calibration.importFileInvalid'}; return;
  }
  busy.value = true; error.value = ''; result.value = null; preview.value = null; selected.value = []; confirmed.value = false;
  try {
    const data = new FormData(); data.append('file', file.value);
    preview.value = (await api.post('/v1/instruments/import/preview', data)).data.data;
  } catch (e) { error.value = getApiErrorMessage(e); }
  finally { busy.value = false; }
}
function toggleAll(event) { selected.value = event.target.checked ? ready.value.map(row => row.rowNumber) : []; }
async function commit() {
  if (!canSubmit.value) return;
  busy.value = true; error.value = '';
  try {
    const data = new FormData(); data.append('file', file.value);
    data.append('options', JSON.stringify({ hash: preview.value.hash, selectedRows: selected.value,
      department: department.value.trim(), custodianOperatorId: Number(custodian.value), usageStatus: status.value,
      includeCustodian: includeCustodian.value }));
    result.value = (await api.post('/v1/instruments/import/commit', data)).data.data;
    emit('imported');
  } catch (e) {
    result.value = e.response?.data?.data || null;
    error.value = getApiErrorMessage(e);
  } finally { busy.value = false; }
}
const statusLabel = value => ({ Ready: t('spc.calibration.ready'), Invalid: t('spc.calibration.invalid'), Existing: t('spc.calibration.existing'), Added: t('spc.calibration.added'), Skipped: t('spc.calibration.skipped'), Failed: t('spc.calibration.importFailed') })[value] || value;
</script>

<template>
  <span v-if="accessError" role="alert" class="text-sm text-rose-500">{{ displayUiMessage(t,accessError) }}</span>
  <button v-if="canManage" type="button" @click="open = true" class="rounded-lg bg-cyan-500 px-4 py-2 text-sm font-medium text-white hover:bg-cyan-400">{{ t('spc.calibration.import') }}</button>
  <Teleport to="body">
    <div v-if="open && canManage" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-3 md:p-6">
      <section role="dialog" aria-modal="true" aria-labelledby="instrument-import-title" class="flex max-h-[94vh] w-full max-w-6xl flex-col rounded-xl border border-slate-300 bg-white text-slate-900 shadow-xl dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100">
        <header class="flex items-center justify-between border-b border-slate-300 p-4 dark:border-slate-700">
          <h2 id="instrument-import-title" class="text-lg font-bold">{{ t('spc.calibration.importTitle') }}</h2>
          <button :disabled="busy" @click="open = false" :aria-label="t('spc.calibration.closeImport')" class="rounded px-3 py-1 disabled:opacity-40">{{ t('spc.calibration.close') }}</button>
        </header>
        <div class="min-w-0 space-y-4 overflow-x-hidden overflow-y-auto p-4">
<LocalePicker />
          <p class="text-sm text-slate-600 dark:text-slate-300">{{ t('spc.calibration.importHelp') }}</p>
          <div class="flex flex-col gap-3 md:flex-row md:items-center">
            <label class="flex-1 text-sm">{{ t('spc.calibration.importFileLabel') }}
              <input :aria-label="t('spc.calibration.excelFile')" type="file" accept=".xlsx" :disabled="busy" @change="changeFile" class="mt-1 block w-full text-sm" />
            </label>
            <button :disabled="busy || !file" data-testid="calibration-readPreview" @click="readPreview" class="rounded bg-indigo-600 px-4 py-2 text-white disabled:opacity-40">{{ busy ? t('spc.calibration.processing') : t('spc.calibration.preview') }}</button>
          </div>
          <p v-if="error" role="alert" class="rounded bg-rose-100 p-3 text-sm text-rose-800 dark:bg-rose-950 dark:text-rose-200">{{ displayUiMessage(t,error) }}</p>
          <template v-if="preview">
            <p class="text-sm">{{ t('spc.calibration.sheetPrefix') }}{{ preview.sheetName }} {{ t('spc.calibration.totalRowsPrefix') }} {{ preview.rows.length }} {{ t('spc.calibration.readyRowsPrefix') }} {{ ready.length }} {{ t('spc.calibration.selectedRowsPrefix') }} {{ selected.length }} {{ t('spc.calibration.rowUnit') }}</p>
            <p class="text-sm text-amber-700 dark:text-amber-300">{{ t('spc.calibration.formulaHelp') }}</p>
            <fieldset :disabled="busy || !!result" class="min-w-0 space-y-3 disabled:opacity-70">
              <legend class="font-semibold">{{ t('spc.calibration.applySelected') }}</legend>
              <div class="grid gap-3 md:grid-cols-3">
                <label class="text-sm">{{ t('spc.calibration.importDepartmentRequired') }}<input :aria-label="t('spc.calibration.importDepartment')" data-testid="calibration-department" v-model="department" maxlength="100" :class="inputClass" :placeholder="t('spc.calibration.departmentPlaceholder')" /></label>
                <label class="text-sm">{{ t('spc.calibration.custodianRequired') }}<select :aria-label="t('spc.calibration.importCustodian')" data-testid="calibration-custodian" v-model="custodian" :class="inputClass"><option value="">{{ t('spc.calibration.custodianPlaceholder') }}</option><option v-for="op in operators" :key="op.id" :value="op.id">{{ op.operatorName }}（{{ op.operatorCode }}）</option></select></label>
                <label class="text-sm">{{ t('spc.calibration.usageStatusRequired') }}<select :aria-label="t('spc.calibration.importUsageStatus')" data-testid="calibration-status" v-model="status" :class="inputClass"><option value="">{{ t('spc.calibration.statusPlaceholder') }}</option><option value="Active">{{ t('spc.calibration.active') }}</option><option value="InCalibration">{{ t('spc.calibration.inCalibration') }}</option><option value="Inactive">{{ t('spc.calibration.inactive') }}</option><option value="Retired">{{ t('spc.calibration.retired') }}</option></select></label>
              </div>
              <p v-if="!operators.length" role="alert" class="text-sm text-rose-500">{{ t('spc.calibration.noOperators') }}</p>
              <label class="flex items-center gap-2 text-sm"><input type="checkbox" data-testid="calibration-includeCustodian" v-model="includeCustodian" />{{ t('spc.calibration.includeCustodian') }}</label>
              <div class="max-h-[38vh] overflow-auto rounded border border-slate-300 dark:border-slate-600">
                <table class="w-full text-left text-sm">
                  <thead class="sticky top-0 bg-slate-100 dark:bg-slate-900"><tr>
                    <th class="p-2"><input :aria-label="t('spc.calibration.selectAll')" type="checkbox" :checked="ready.length > 0 && selected.length === ready.length" :disabled="!ready.length" @change="toggleAll" /></th>
                    <th class="p-2">{{ t('spc.calibration.rowUnit') }}</th><th class="p-2">{{ t('spc.calibration.codeName') }}</th><th class="p-2">{{ t('spc.calibration.location') }}</th><th class="p-2">{{ t('spc.calibration.cycle') }}</th><th class="p-2">{{ t('spc.calibration.method') }}</th><th class="p-2">{{ t('spc.calibration.specification') }}</th><th class="p-2">{{ t('spc.calibration.precision') }}</th><th class="p-2">{{ t('spc.calibration.remarks') }}</th><th class="p-2">{{ t('spc.calibration.standard') }}</th><th class="p-2">{{ t('spc.calibration.acceptance') }}</th><th class="p-2">{{ t('spc.calibration.importLastDate') }}</th><th class="p-2">{{ t('spc.calibration.importNextDate') }}</th><th class="p-2">{{ t('spc.calibration.statusReason') }}</th>
                  </tr></thead>
                  <tbody><tr v-for="row in preview.rows" :key="row.rowNumber" class="border-t border-slate-200 align-top dark:border-slate-700">
                    <td class="p-2"><input type="checkbox" data-testid="calibration-selected" v-model="selected" :value="row.rowNumber" :disabled="row.status !== 'Ready'" :aria-label="t('spc.calibration.selectRow',{row:row.rowNumber})" /></td>
                    <td class="p-2">{{ row.rowNumber }}</td><td class="min-w-40 p-2"><div>{{ row.code || t('spc.calibration.missingCode') }}</div><div>{{ row.name || t('spc.calibration.missingName') }}</div></td>
                    <td class="min-w-28 p-2">{{ row.location || '—' }}</td>
                    <td class="whitespace-nowrap p-2">{{ row.cycleMonths ? t('spc.calibration.cycleMonths',{months:row.cycleMonths}) : '—' }}<small class="block text-slate-500 dark:text-slate-400">{{ row.rawCycle }}</small></td>
                    <td class="min-w-20 p-2">{{ row.calibrationMethod || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.measurementSpecification || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.precision || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.remarks || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.calibrationStandard || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.acceptanceCriteria || '—' }}</td>

                    <td class="whitespace-nowrap p-2">{{ row.lastCalibrationDate || '—' }}<small class="block text-slate-500 dark:text-slate-400">{{ t('spc.calibration.rawValue') }}{{ row.rawLastDate || t('spc.calibration.blank') }}</small></td>
                    <td class="whitespace-nowrap p-2">{{ row.nextCalibrationDate || '—' }}<small class="block text-slate-500 dark:text-slate-400">{{ t('spc.calibration.rawValue') }}{{ row.rawNextDate || t('spc.calibration.blank') }}</small></td>
                    <td class="min-w-60 p-2"><strong>{{ statusLabel(row.status) }}</strong><p v-for="message in row.errors" :key="message" class="text-rose-700 dark:text-rose-300">{{ message }}</p><p v-for="message in row.warnings" :key="message" class="text-amber-700 dark:text-amber-300">{{ message }}</p></td>
                  </tr></tbody>
                </table>
              </div>
              <label class="flex items-start gap-2 text-sm"><input data-testid="calibration-confirmed" v-model="confirmed" type="checkbox" :aria-label="t('spc.calibration.confirmData')" class="mt-1" />{{ t('spc.calibration.confirmDataHelp') }}</label>
            </fieldset>
          </template>
          <div v-if="result" role="status" class="space-y-2 rounded border border-slate-300 p-3 dark:border-slate-600">
            <p class="font-bold">{{ t('spc.calibration.addedPrefix') }} {{ result.added }} {{ t('spc.calibration.skippedPrefix') }} {{ result.skipped }} {{ t('spc.calibration.failedPrefix') }} {{ result.failed }} {{ t('spc.calibration.recordUnit') }}</p>
            <ul class="max-h-40 overflow-y-auto text-sm"><li v-for="row in result.rows" :key="row.rowNumber">{{ t('spc.calibration.rowPrefix') }} {{ row.rowNumber }} {{ t('spc.calibration.rowSuffix') }} {{ row.code }} · {{ statusLabel(row.status) }}：{{ row.message }}</li></ul>
            <p class="text-sm">{{ t('spc.calibration.importAgainHelp') }}</p>
          </div>
        </div>
        <footer class="flex justify-end gap-3 border-t border-slate-300 p-4 dark:border-slate-700">
          <button :disabled="busy" @click="open = false" class="rounded border border-slate-400 px-4 py-2 disabled:opacity-40">{{ t('spc.calibration.close') }}</button>
          <button :disabled="!canSubmit" data-testid="calibration-commit" @click="commit" class="rounded bg-teal-700 px-4 py-2 text-white disabled:opacity-40">{{ busy ? t('spc.calibration.processing') : t('spc.calibration.confirmImport',{count:selected.length}) }}</button>
        </footer>
      </section>
    </div>
  </Teleport>
</template>
