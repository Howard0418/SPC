<script setup>
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
const { t } = useI18n({ useScope: "global" });
const messageText = value => displayUiMessage(t, value);
import { computed, onMounted, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import { History, RefreshCw, RotateCcw, Save, Search, TestTube2 } from "lucide-vue-next";

import VersionHistoryRange from "../components/VersionHistoryRange.vue";
const historyMore = ref(false);
let historyQuery = { skip: 0, take: 21 };
const defaultCells = [
  { cellAddress: "F!B3", standardSolution: "1N NaOH", numericValue: 1.02 },
  { cellAddress: "F!B4", standardSolution: "0.2N HCl", numericValue: 0.983 },
  { cellAddress: "F!B5", standardSolution: "0.4N HCl", numericValue: 0.983 },
  { cellAddress: "F!B6", standardSolution: "0.05N EDTA", numericValue: 1 },
  { cellAddress: "F!B7", standardSolution: "0.1N KMnO4", numericValue: 1 },
  { cellAddress: "F!B8", standardSolution: "0.1N H2SO4", numericValue: 1 },
  { cellAddress: "F!B9", standardSolution: "0.1N I2", numericValue: 1 },
  { cellAddress: "F!B10", standardSolution: "0.1ml Na2S2O3", numericValue: 1 }
];

const loading = ref(false);
const saving = ref(false);
const error = ref("");
const message = ref("");
const active = ref(null);
const form = ref({ versionCode: "", displayName: "", cells: defaultCells.map(x => ({ ...x })) });
const selectedCell = ref("F!B3");
const impactRows = ref([]);
const impactLoading = ref(false);
const formula = ref("Primary * F!B3");
const primaryValue = ref(10);
const formulaResult = ref(null);
const historyRows = ref([]);
const historyLoading = ref(false);
const restoringHistoryId = ref(null);

const hasActive = computed(() => !!active.value?.versionCode);

function loadIntoForm(detail) {
  form.value.versionCode = `F-${new Date().toISOString().slice(0, 10).replaceAll("-", "")}`;
  form.value.displayName = "F 表新版";
  form.value.cells = (detail?.cells?.length ? detail.cells : defaultCells).map(x => ({
    cellAddress: x.cellAddress,
    standardSolution: x.standardSolution || "",
    numericValue: Number(x.numericValue ?? 0)
  }));
  selectedCell.value = form.value.cells[0]?.cellAddress || "F!B3";
}

async function load() {
  loading.value = true;
  error.value = "";
  message.value = "";
  try {
    const { data } = await api.get("/v1/chemical-f-table/active");
    active.value = data;
    loadIntoForm(data);
  } catch (err) {
    if (err?.response?.status === 404) {
      active.value = null;
      loadIntoForm(null);
      message.value = { key: "spc.fTable.firstVersion" };
    } else {
      error.value = getApiErrorMessage(err);
    }
  } finally {
    loading.value = false;
  }
}

async function preview() {
  return apply(false);
}

async function save() {
  return apply(true);
}

async function loadHistory(next) {
  if (next && !(next instanceof Event)) historyQuery = next;
  historyLoading.value = true;
  error.value = "";
  historyRows.value = [];
  historyMore.value = false;
  try {
    const { data } = await api.get("/v1/chemical-f-table/versions", { params: historyQuery });
    const result = Array.isArray(data) ? data : [];
    historyMore.value = result.length > 20;
    historyRows.value = result.slice(0, 20);
  } catch (err) {
    error.value = getApiErrorMessage(err);
  } finally {
    historyLoading.value = false;
  }
}

async function restoreHistory(row) {
  if (!window.confirm(t("spc.fTable.confirmRestore", { version: row.versionCode }))) return;
  restoringHistoryId.value = row.id;
  error.value = "";
  message.value = "";
  try {
    await api.post(`/v1/chemical-f-table/versions/${row.id}/restore`, {
      reason: `由畫面回復版本 ${row.versionNo}`
    });
    message.value = { key: "spc.fTable.restored", args: { version: row.versionCode } };
    await load();
    await loadHistory();
    await loadImpact();
  } catch (err) {
    error.value = getApiErrorMessage(err);
  } finally {
    restoringHistoryId.value = null;
  }
}

async function apply(applyChanges) {
  saving.value = true;
  error.value = "";
  message.value = "";
  try {
    const payload = {
      apply: applyChanges,
      versionCode: form.value.versionCode,
      displayName: form.value.displayName,
      cells: form.value.cells.map(x => ({
        cellAddress: x.cellAddress,
        standardSolution: x.standardSolution,
        numericValue: Number(x.numericValue)
      }))
    };
    const { data } = await api.post("/v1/chemical-f-table/apply", payload);
    message.value = applyChanges
      ? { key: "spc.fTable.applied", args: { version: data.versionCode } }
      : { key: "spc.fTable.previewComplete", args: { count: data.cells?.filter(x => x.action !== "Unchanged").length || 0 } };
    if (applyChanges) {
      await load();
      await loadHistory();
    }
  } catch (err) {
    error.value = getApiErrorMessage(err);
  } finally {
    saving.value = false;
  }
}

async function loadImpact() {
  impactLoading.value = true;
  error.value = "";
  try {
    const { data } = await api.get("/v1/chemical-f-table/impact", { params: { cellAddress: selectedCell.value } });
    impactRows.value = Array.isArray(data) ? data : [];
  } catch (err) {
    error.value = getApiErrorMessage(err);
  } finally {
    impactLoading.value = false;
  }
}

async function evaluateFormula() {
  formulaResult.value = null;
  error.value = "";
  try {
    const { data } = await api.post("/v1/chemical-f-table/evaluate", {
      expression: formula.value,
      variables: { Primary: Number(primaryValue.value) }
    });
    formulaResult.value = data;
  } catch (err) {
    error.value = getApiErrorMessage(err);
  }
}

onMounted(async () => {
  await load();
  await loadHistory();
  await loadImpact();
});
</script>

<template>
  <div class="p-6 space-y-5">
    <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="text-2xl font-black text-slate-900 dark:text-white">{{ t('spc.fTable.title') }}</h1>
        <p class="text-sm text-slate-500 dark:text-slate-400">{{ t('spc.fTable.subtitle') }}</p>
      </div>
      <button @click="load" class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-slate-800 text-white text-sm font-bold hover:bg-slate-700">
        <RefreshCw class="w-4 h-4" /> {{ t('common.actions.refresh') }}
      </button>
    </div>

    <div v-if="error" class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">{{ messageText(error) }}</div>
    <div v-if="message" class="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-bold text-emerald-700">{{ messageText(message) }}</div>

    <section class="grid grid-cols-1 gap-4 lg:grid-cols-[1fr_360px]">
      <div class="min-w-0 rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
        <div class="mb-4 grid gap-3 md:grid-cols-2">
          <label class="text-xs font-bold text-slate-600 dark:text-slate-300">{{ t('spc.fTable.versionCode') }}
            <input data-testid="f-code" v-model="form.versionCode" class="mt-1 w-full rounded-lg border px-3 py-2 font-mono dark:bg-slate-950 dark:border-slate-700" />
          </label>
          <label class="text-xs font-bold text-slate-600 dark:text-slate-300">{{ t('spc.fTable.versionName') }}
            <input data-testid="f-name" v-model="form.displayName" class="mt-1 w-full rounded-lg border px-3 py-2 dark:bg-slate-950 dark:border-slate-700" />
          </label>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-[440px] w-full text-sm">
            <thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-950 dark:text-slate-400">
              <tr><th class="px-3 py-2">{{ t('spc.fTable.cell') }}</th><th class="px-3 py-2">{{ t('spc.fTable.solution') }}</th><th class="px-3 py-2">{{ t('spc.fTable.factor') }}</th></tr>
            </thead>
            <tbody>
              <tr v-for="cell in form.cells" :key="cell.cellAddress" class="border-t dark:border-slate-800">
                <td class="px-3 py-2 font-mono font-bold">{{ cell.cellAddress }}</td>
                <td class="px-3 py-2"><input data-testid="f-solution" v-model="cell.standardSolution" class="w-full min-w-32 rounded border px-2 py-1 dark:bg-slate-950 dark:border-slate-700" /></td>
                <td class="px-3 py-2"><input data-testid="f-value" v-model.number="cell.numericValue" type="number" step="0.000001" class="w-32 rounded border px-2 py-1 font-mono dark:bg-slate-950 dark:border-slate-700" /></td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="mt-4 flex flex-wrap gap-2">
          <button data-testid="f-preview" @click="preview" :disabled="saving" class="inline-flex items-center gap-2 px-4 py-2 rounded-lg border text-sm font-bold hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800">
            <Search class="w-4 h-4" /> {{ t('spc.fTable.dryRun') }}
          </button>
          <button data-testid="f-apply" @click="save" :disabled="saving" class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-bold hover:bg-blue-500">
            <Save class="w-4 h-4" /> {{ t('spc.fTable.apply') }}
          </button>
        </div>
      </div>

      <aside class="min-w-0 space-y-4">
        <div class="min-w-0 rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
          <h2 class="mb-2 text-sm font-black text-slate-800 dark:text-white">{{ t('spc.fTable.active') }}</h2>
          <div v-if="hasActive" class="text-sm text-slate-600 dark:text-slate-300">
            <div class="font-mono font-bold">{{ active.versionCode }}</div>
            <div>{{ active.displayName }}</div>
            <div class="text-xs text-slate-400">{{ new Date(active.effectiveAt).toLocaleString() }}</div>
          </div>
          <div v-else class="text-sm text-slate-500">{{ t('spc.fTable.notCreated') }}</div>
        </div>

        <div class="min-w-0 rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
          <h2 class="mb-3 text-sm font-black text-slate-800 dark:text-white">{{ t('spc.fTable.formulaTest') }}</h2>
          <input :aria-label="t('spc.fTable.expressionLabel')" data-testid="f-formula" v-model="formula" class="mb-2 w-full rounded border px-3 py-2 font-mono text-sm dark:bg-slate-950 dark:border-slate-700" />
          <input :aria-label="t('spc.fTable.primaryLabel')" data-testid="f-primary" v-model.number="primaryValue" type="number" class="mb-2 w-full rounded border px-3 py-2 font-mono text-sm dark:bg-slate-950 dark:border-slate-700" />
          <button data-testid="f-evaluate" @click="evaluateFormula" class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-500">
            <TestTube2 class="w-4 h-4" /> {{ t('spc.fTable.evaluate') }}
          </button>
          <div v-if="formulaResult" class="mt-3 rounded bg-slate-50 p-3 text-xs dark:bg-slate-950">
            <div>{{ t('spc.fTable.resultLabel') }}<span class="font-mono font-bold">{{ formulaResult.value }}</span></div>
            <div>{{ t('spc.fTable.versionLabel') }}<span class="font-mono">{{ formulaResult.versionCode }}</span></div>
          </div>
        </div>

        <div class="min-w-0 rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
          <div class="mb-3 flex items-center gap-2">
            <History class="w-4 h-4 text-slate-500" />
            <h2 class="mr-auto text-sm font-black text-slate-800 dark:text-white">{{ t('spc.fTable.history') }}</h2>
            <button @click="loadHistory" :disabled="historyLoading" class="rounded border px-2 py-1 text-xs font-bold hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800">{{ t('spc.fTable.update') }}</button>
          </div>
          <VersionHistoryRange :loading="historyLoading" :has-more="historyMore" @change="loadHistory" />
          <div v-if="historyRows.length" class="space-y-2">
            <div v-for="row in historyRows" :key="row.id" class="rounded border p-2 text-xs dark:border-slate-800">
              <div class="flex items-start gap-2">
                <div class="mr-auto">
                  <div class="font-mono font-bold">{{ row.versionCode }}</div>
                  <div class="text-slate-500">{{ row.displayName }}</div>
                  <div class="text-slate-400">#{{ row.versionNo }} {{ row.changeType }} · {{ new Date(row.changedAt).toLocaleString('zh-TW', { timeZone: 'Asia/Taipei' }) }}</div>
                </div>
                <button
                  data-testid="f-restore" @click="restoreHistory(row)"
                  :disabled="restoringHistoryId === row.id"
                  class="inline-flex items-center gap-1 rounded bg-amber-600 px-2 py-1 font-bold text-white hover:bg-amber-500"
                  :title="t('spc.fTable.restoreTitle')"
                >
                  <RotateCcw class="w-3 h-3" /> {{ t('spc.fTable.restore') }}
                </button>
              </div>
              <div v-if="row.reason" class="mt-1 text-slate-500">{{ row.reason }}</div>
            </div>
          </div>
          <div v-else class="text-sm text-slate-500">{{ t('spc.fTable.emptyHistory') }}</div>
        </div>
      </aside>
    </section>

    <section class="rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
      <div class="mb-3 flex flex-wrap items-center gap-2">
        <h2 class="mr-auto text-sm font-black text-slate-800 dark:text-white">{{ t('spc.fTable.impact') }}</h2>
        <select data-testid="f-cell" v-model="selectedCell" class="rounded-lg border px-3 py-2 text-sm dark:bg-slate-950 dark:border-slate-700">
          <option v-for="cell in form.cells" :key="cell.cellAddress" :value="cell.cellAddress">{{ cell.cellAddress }}</option>
        </select>
        <button data-testid="f-impact" @click="loadImpact" :disabled="impactLoading" class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-slate-800 text-white text-xs font-bold hover:bg-slate-700">
          <Search class="w-4 h-4" /> {{ t('spc.fTable.search') }}
        </button>
      </div>
      <div class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-950 dark:text-slate-400">
            <tr><th class="px-3 py-2">{{ t('spc.fTable.lineProcess') }}</th><th class="px-3 py-2">{{ t('spc.fTable.tank') }}</th><th class="px-3 py-2">{{ t('spc.fTable.item') }}</th><th class="px-3 py-2">{{ t('spc.fTable.formula') }}</th></tr>
          </thead>
          <tbody>
            <tr v-for="row in impactRows" :key="`${row.partProcessCharacteristicId}-${row.referenceContext}`" class="border-t dark:border-slate-800">
              <td class="px-3 py-2"><div class="font-bold">{{ row.machineName || row.processName }}</div><small class="text-slate-400">{{ row.machineCode || row.processCode }}</small></td>
              <td class="px-3 py-2"><div>{{ row.tankName || "-" }}</div><small class="text-slate-400">{{ row.tankCode }}</small></td>
              <td class="px-3 py-2"><div class="font-bold">{{ row.characteristicName }}</div><small class="text-slate-400">{{ row.characteristicCode }}</small></td>
              <td class="px-3 py-2 font-mono text-xs">{{ row.sourceFormula }}</td>
            </tr>
            <tr v-if="!impactRows.length"><td colspan="4" class="px-3 py-8 text-center text-slate-500">{{ t('spc.fTable.noReferences', { cell: selectedCell }) }}</td></tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
