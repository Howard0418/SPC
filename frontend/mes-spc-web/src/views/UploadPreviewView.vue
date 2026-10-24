<script setup>
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
import { useRoute, useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import {
  CheckCircle2,
  ShieldAlert,
  Sparkles,
  RefreshCw,
  AlertTriangle,
  ArrowRight,
  Table,
  Server,
  ArrowLeft,
  XCircle
} from "lucide-vue-next";

const route = useRoute();
const router = useRouter();
const { t } = useI18n({ useScope: "global" });
const batch = ref(null);
const details = ref([]);
const errors = ref([]);
const err = ref("");
const loading = ref(false);
const batchId = computed(() => route.params.batchId);
const chartPpcId = ref(null);

function formatMeasuredAt(value) {
  if (!value) return t('spc.uploads.now');
  const text = String(value).trim();
  const normalized = /^(\d{4})(\d{2})(\d{2})(.*)$/.test(text)
    ? text.replace(/^(\d{4})(\d{2})(\d{2})(.*)$/, "$1-$2-$3$4")
    : text;
  const parsed = new Date(normalized);
  return Number.isNaN(parsed.getTime()) ? text : parsed.toLocaleString();
}
const revalidateProgress = ref(null);
const missingMappingCount = computed(() =>
  new Set(errors.value
    .filter(e => ["MAPPING_NOT_FOUND", "UNIT_MISMATCH", "CHAR_NOT_FOUND"].includes(e.errorCode))
    .map(e => e.uploadDetailId)).size
);
const blockedSetupCount = computed(() => {
  const setupIds = new Set(errors.value
    .filter(e => ["MAPPING_NOT_FOUND", "UNIT_MISMATCH", "CHAR_NOT_FOUND"].includes(e.errorCode))
    .map(e => e.uploadDetailId));
  const blockedIds = new Set(errors.value
    .filter(e => ["PROCESS_NOT_FOUND", "MACHINE_NOT_FOUND", "TANK_NOT_FOUND"].includes(e.errorCode))
    .map(e => e.uploadDetailId));
  return [...setupIds].filter(id => blockedIds.has(id)).length;
});
const actionableSetupCount = computed(() => Math.max(0, missingMappingCount.value - blockedSetupCount.value));
const errorDetails = computed(() => details.value.filter(d => !d.isValid));

const isImported = computed(() => {
  return batch.value?.importStatus === "Imported" || batch.value?.isConfirmed === true;
});

async function load() {
  err.value = "";
  try {
    const { data } = await api.get(`/uploads/${batchId.value}/preview`, {
      params: { _: Date.now() },
      headers: { "Cache-Control": "no-cache" }
    });
    batch.value = data.batch;
    errors.value = data.errors || [];
    details.value = (data.details || []).map(d => {
      let p = {};
      try { p = JSON.parse(d.payloadJson || "{}"); } catch(e){}
      
      // Match errors for this specific detail record
      const rowErrors = (data.errors || []).filter(errItem => errItem.uploadDetailId === d.id);
      
      return {
        id: d.id,
        rowNo: d.rowNo,
        isValid: d.isValid,
        partNo: p.PartNo || p["料號"] || "",
        processCode: p.ProcessCode || p["製程"] || "",
        machineCode: p.MachineCode || p["機台"] || p["線別"] || "",
        tankCode: p.TankCode || p["槽位"] || "",
        characteristicCode: p.CharacteristicCode || p["檢驗項目"] || p["管制項目"] || "",
        unit: p.Unit || p["單位"] || "",
        resolvedProcessCode: p.ResolvedProcessCode || "",
        resolvedMachineCode: p.ResolvedMachineCode || "",
        resolvedMachineName: p.ResolvedMachineName || "",
        resolvedTankCode: p.ResolvedTankCode || "",
        resolvedTankName: p.ResolvedTankName || "",
        resolvedCharacteristicCode: p.ResolvedCharacteristicCode || "",
        resolvedCharacteristicName: p.ResolvedCharacteristicName || "",
        resolvedUnit: p.ResolvedUnit || "",
        resolvedMappingId: p.ResolvedMappingId || "",
        duplicateStatus: p.DuplicateStatus || "",
        sourceFile: rowErrors[0]?.sourceFile || batch.value?.originalFileName || "",
        sourceSheet: rowErrors[0]?.sourceSheet || p.__SourceSheet || "",
        sourceRow: rowErrors[0]?.sourceRow || p.__SourceRow || d.rowNo,
        measuredValue: p.MeasuredValue !== undefined ? p.MeasuredValue : (p["測量值"] !== undefined ? p["測量值"] : (p["量測值"] !== undefined ? p["量測值"] : null)),
        recheckValue: p.RecheckValue !== undefined ? p.RecheckValue : (p["複驗"] !== undefined ? p["複驗"] : (p["複驗值"] !== undefined ? p["複驗值"] : "")),
        adjustAction: p.AdjustAction || p["調整"] || p["調整方式"] || "",
        adjustAmount: p.AdjustAmount !== undefined ? p.AdjustAmount : (p["調整量"] !== undefined ? p["調整量"] : ""),
        defectQty: p.DefectQty !== undefined ? p.DefectQty : (p["不良數"] !== undefined ? p["不良數"] : null),
        inspectedQty: p.InspectedQty !== undefined ? p.InspectedQty : (p["總數"] !== undefined ? p["總數"] : null),
        measuredAt: p.MeasuredAt || p["日期"] || p["量測日期"] || "",
        operator: p.Operator || p["作業員"] || p["量測員"] || "",
        errorsList: rowErrors
      };
    });
    chartPpcId.value = await resolveChartPpcId();
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}

async function resolveChartPpcId() {
  try {
    const { data: targetData } = await api.get(`/uploads/${batchId.value}/chart-targets`);
    const targetId = Number(targetData?.targets?.[0]?.partProcessCharacteristicId || 0);
    if (targetId) return targetId;

    // Legacy product batches may not have measurements linked by upload batch.
    const firstValid = details.value.find(d => d.isValid && d.partNo && d.processCode && d.characteristicCode);
    if (!firstValid) return null;
    const { data } = await api.get("/v1/part-process-characteristics");
    const match = (data || []).find(x => {
      const partNo = x.part?.partNo || x.Part?.partNo || x.Part?.PartNo;
      const processCode = x.process?.processCode || x.Process?.processCode || x.Process?.ProcessCode;
      const characteristicCode = x.characteristic?.characteristicCode || x.Characteristic?.characteristicCode || x.Characteristic?.CharacteristicCode;
      return partNo === firstValid.partNo &&
        processCode === firstValid.processCode &&
        characteristicCode === firstValid.characteristicCode;
    });
    return match?.id || null;
  } catch {
    return null;
  }
}

function goToChart() {
  if (!chartPpcId.value) {
    err.value = { key: 'spc.uploads.noChart' };
    return;
  }
  router.push({
    path: "/spc",
    query: {
      ppcId: chartPpcId.value,
      uploadBatchId: batchId.value
    }
  });
}

function goToMissingMappings() {
  router.push({
    path: "/part-process-characteristics",
    query: { scope: "CHEM", uploadBatchId: batchId.value }
  });
}

async function createMissingMappings() {
  if (!confirm(t('spc.uploads.confirmCreate', { count: missingMappingCount.value }))) return;
  loading.value = true;
  err.value = "";
  revalidateProgress.value = {
    processed: 0,
    total: Number(batch.value?.totalRows || 0),
    percent: 0
  };
  const progressTimer = window.setInterval(async () => {
    try {
      const { data } = await api.get(`/uploads/${batchId.value}/progress`);
      const total = Number(data.total || batch.value?.totalRows || 1);
      const processed = data.importStatus === "Revalidating" ? Number(data.processed || 0) : 0;
      revalidateProgress.value = {
        processed,
        total,
        percent: Math.min(100, Math.round(processed * 100 / total))
      };
    } catch {}
  }, 1000);
  try {
    const { data } = await api.post(`/uploads/${batchId.value}/create-missing-mappings`);
    if (batch.value) {
      batch.value.validRows = Number(data.validRows || 0);
      batch.value.errorRows = Number(data.errorRows || 0);
    }
    revalidateProgress.value = {
      processed: Number(batch.value?.totalRows || 0),
      total: Number(batch.value?.totalRows || 0),
      percent: 100
    };
    await load();
    alert([
      t('spc.uploads.createdCharacteristics', { count: data.createdCharacteristics || 0 }),
      t('spc.uploads.createdTanks', { count: data.createdTanks || 0 }),
      t('spc.uploads.createdMappings', { count: data.createdMappings || 0 }),
      t('spc.uploads.reusedMappings', { count: data.reusedMappings || 0 }),
      t('spc.uploads.skippedTank', { count: data.skippedMissingTank || 0 }),
      t('spc.uploads.skippedMaster', { count: data.skippedMissingMasterData || 0 }),
      t('spc.uploads.revalidated')
    ].join("\n"));
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    window.clearInterval(progressTimer);
    revalidateProgress.value = null;
    loading.value = false;
  }
}

// Check if a specific cell key is invalid
function hasCellError(detailItem, fieldName) {
  return detailItem.errorsList.some(e => e.fieldName?.toLowerCase() === fieldName.toLowerCase());
}

function getCellErrorMessage(detailItem, fieldName) {
  const matched = detailItem.errorsList.find(e => e.fieldName?.toLowerCase() === fieldName.toLowerCase());
  return matched ? matched.errorMessage : "";
}

function hasAnyCellError(detailItem, fieldNames) {
  return fieldNames.some(field => hasCellError(detailItem, field));
}

function matchState(detailItem, section) {
  const configs = {
    line: { fields: ["MachineCode", "ProcessCode"], resolved: detailItem.resolvedMachineCode },
    tank: { fields: ["TankCode"], resolved: detailItem.resolvedTankName || detailItem.resolvedTankCode, blockedBy: ["MachineCode", "ProcessCode"] },
    characteristic: { fields: ["CharacteristicCode"], resolved: detailItem.resolvedCharacteristicName || detailItem.resolvedCharacteristicCode },
    unit: { fields: ["Unit"], resolved: detailItem.resolvedMappingId ? (detailItem.resolvedUnit || t('spc.uploads.blank')) : detailItem.resolvedUnit, blockedBy: ["TankCode", "CharacteristicCode"] },
    mapping: { fields: ["PartProcessCharacteristic"], resolved: detailItem.resolvedMappingId, blockedBy: ["MachineCode", "ProcessCode", "TankCode", "CharacteristicCode", "Unit"] }
  };
  const config = configs[section];
  if (hasAnyCellError(detailItem, config.fields)) return "error";
  if (config.blockedBy && hasAnyCellError(detailItem, config.blockedBy) && !config.resolved) return "skipped";
  return config.resolved ? "success" : "skipped";
}

function matchStateClass(state) {
  return state === "success"
    ? "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/30 dark:text-emerald-300 dark:border-emerald-800"
    : state === "error"
      ? "bg-red-50 text-red-700 border-red-200 dark:bg-red-950/30 dark:text-red-300 dark:border-red-800"
      : "bg-slate-100 text-slate-500 border-slate-200 dark:bg-slate-800 dark:text-slate-400 dark:border-slate-700";
}

function matchStateLabel(state) {
  return state === "success" ? t('spc.uploads.matched') : state === "error" ? t('spc.uploads.failed') : t('spc.uploads.pending');
}

onMounted(load);
</script>

<template>
  <div class="w-full max-w-[1920px] mx-auto px-1 sm:px-2 space-y-6">
    <div v-if="revalidateProgress" class="fixed inset-0 z-[100] bg-slate-950/60 flex items-center justify-center p-4">
      <div class="w-full max-w-lg rounded-3xl bg-white dark:bg-slate-900 p-7 shadow-2xl">
        <div class="flex justify-between text-sm font-black text-slate-800 dark:text-white mb-3">
          <span>{{ t('spc.uploads.revalidating') }}</span>
          <span>{{ revalidateProgress.processed }} / {{ revalidateProgress.total }}（{{ revalidateProgress.percent }}%）</span>
        </div>
        <div class="h-4 rounded-full bg-slate-200 dark:bg-slate-700 overflow-hidden">
          <div class="h-full bg-amber-500 transition-all duration-300" :style="{ width: `${revalidateProgress.percent}%` }"></div>
        </div>
        <p class="mt-3 text-xs text-slate-500">{{ t('spc.uploads.stayOnPage') }}</p>
      </div>
    </div>
    <!-- Header -->
    <div class="p-4 sm:p-8 rounded-3xl bg-gradient-to-r from-violet-600 via-purple-600 to-indigo-700 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div class="space-y-2">
          <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-fit max-w-full text-xs font-bold text-cyan-200 border border-white/20">
            <Server class="w-3.5 h-3.5" /> {{ t('spc.uploads.previewBadge') }}
          </div>
          <h1 class="text-3xl font-black tracking-tight">{{ t('spc.uploads.previewTitle') }}</h1>
          <p class="text-indigo-100 text-sm max-w-xl">
            {{ t('spc.uploads.batchId') }}<code class="px-2 py-0.5 rounded bg-black/30 font-mono text-xs">{{ batchId }}</code>
          </p>
        </div>

        <div v-if="batch" class="flex flex-wrap items-center gap-3">
          <span
            class="px-4 py-2 rounded-2xl text-xs font-bold uppercase tracking-widest flex items-center gap-2 shadow-lg"
            :class="isImported ? 'bg-emerald-500 text-white' : errors.length > 0 ? 'bg-amber-500 text-white border border-amber-400/30' : 'bg-blue-600 text-white'"
          >
            <CheckCircle2 v-if="isImported" class="w-4 h-4" />
            <AlertTriangle v-else-if="errors.length > 0" class="w-4 h-4 animate-bounce" />
            <Sparkles v-else class="w-4 h-4" />
            {{ isImported ? t('spc.uploads.importedTable') : errors.length > 0 ? t('spc.uploads.formatIssues') : t('spc.uploads.pendingImport') }}
          </span>

          <button
            v-if="isImported"
            @click="goToChart"
            data-testid="uploads-chart"
            :disabled="!chartPpcId"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-emerald-500 hover:bg-emerald-400 text-white font-black shadow-xl shadow-black/20 disabled:opacity-50 disabled:cursor-not-allowed transition-all text-sm"
          >
            <ArrowRight class="w-4 h-4" />
            {{ t('spc.uploads.chart') }}
          </button>
        </div>
      </div>
    </div>

    <!-- API Errors -->
    <div v-if="err" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ displayUiMessage(t, err) }}
    </div>

    <div v-if="!isImported" class="p-5 rounded-2xl bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 text-blue-800 dark:text-blue-300">
      <p class="font-black">{{ t('spc.uploads.viewOnly') }}</p>
      <p class="mt-1 text-sm">
        {{ t('spc.uploads.viewHelp') }}
      </p>
    </div>

    <div v-if="!isImported && missingMappingCount > 0" class="p-5 rounded-2xl bg-amber-50 dark:bg-amber-950/20 border border-amber-300 dark:border-amber-800 flex flex-col md:flex-row md:items-center justify-between gap-3">
      <div>
        <p class="font-black text-amber-800 dark:text-amber-300">{{ t('spc.uploads.missingMappings', { count: missingMappingCount }) }}</p>
        <p class="text-xs text-amber-700 dark:text-amber-400 mt-1">
          {{ t('spc.uploads.setupCounts', { actionable: actionableSetupCount, blocked: blockedSetupCount }) }}
        </p>
        <details class="mt-2 text-xs text-amber-800 dark:text-amber-300">
          <summary class="cursor-pointer font-bold">{{ t('spc.uploads.mappingRules') }}</summary>
          <ol class="list-decimal ml-5 mt-2 space-y-1">
            <li>{{ t('spc.uploads.lineRule') }}</li>
            <li>{{ t('spc.uploads.tankRule') }}</li>
            <li>{{ t('spc.uploads.characteristicRule') }}</li>
            <li>{{ t('spc.uploads.unitRule') }}</li>
            <li>{{ t('spc.uploads.createRule') }}</li>
          </ol>
        </details>
      </div>
      <div class="flex flex-wrap gap-2">
        <button @click="createMissingMappings" data-testid="uploads-create" :disabled="loading" class="px-5 py-2.5 rounded-xl bg-amber-600 hover:bg-amber-500 disabled:opacity-50 text-white text-sm font-black">
          {{ t('spc.uploads.create') }}
        </button>
        <button @click="goToMissingMappings" data-testid="uploads-manual" class="px-4 py-2.5 rounded-xl border border-amber-500 text-amber-700 dark:text-amber-300 text-sm font-bold">
          {{ t('spc.uploads.manual') }}
        </button>
      </div>
    </div>

    <!-- Validation Summary Card -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3.5 rounded-xl bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400">
          <Table class="w-6 h-6" />
        </div>
        <div>
          <p class="text-xs text-slate-400 font-bold uppercase">{{ t('spc.uploads.totalUploaded') }}</p>
          <h3 class="text-2xl font-black text-slate-800 dark:text-white">{{ t('spc.uploads.rowCount', { count: batch?.totalRows || 0 }) }}</h3>
        </div>
      </div>

      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3.5 rounded-xl bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 class="w-6 h-6" />
        </div>
        <div>
          <p class="text-xs text-slate-400 font-bold uppercase">{{ t('spc.uploads.validRows') }}</p>
          <h3 class="text-2xl font-black text-slate-800 dark:text-white text-emerald-600 dark:text-emerald-400">
            {{ t('spc.uploads.rowCount', { count: batch?.validRows || 0 }) }}
          </h3>
        </div>
      </div>

      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3.5 rounded-xl bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400">
          <XCircle class="w-6 h-6" />
        </div>
        <div>
          <p class="text-xs text-slate-400 font-bold uppercase">{{ t('spc.uploads.errorRows') }}</p>
          <h3 class="text-2xl font-black" :class="errors.length > 0 ? 'text-red-500 animate-pulse' : 'text-slate-550 dark:text-slate-400'">
            {{ t('spc.uploads.rowCount', { count: batch?.errorRows || 0 }) }}
          </h3>
        </div>
      </div>
    </div>

    <!-- Unified Master Table with Highlighted Errors -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between border-b border-slate-100 dark:border-slate-850 pb-4">
        <h3 class="text-lg font-black text-slate-800 dark:text-white flex items-center gap-2">
          <Table class="w-5 h-5 text-blue-500" /> {{ t('spc.uploads.rejected') }}
        </h3>
        <span class="text-xs text-slate-400 font-bold">{{ t('spc.uploads.allValidated') }}</span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs border-collapse">
          <thead class="bg-slate-50 dark:bg-slate-800/50 text-slate-400 uppercase text-[11px] font-bold tracking-wider border-b border-slate-250 dark:border-slate-800">
            <tr>
              <th class="p-3 w-16">{{ t('spc.uploads.row') }}</th>
              <th class="p-3">{{ t('spc.uploads.source') }}</th>
              <th class="p-3">{{ t('spc.uploads.partNo') }}</th>
              <th class="p-3">{{ t('spc.uploads.processMapping') }}</th>
              <th class="p-3">{{ t('spc.uploads.lineMapping') }}</th>
              <th class="p-3">{{ t('spc.uploads.tankMapping') }}</th>
              <th class="p-3">{{ t('spc.uploads.characteristicMapping') }}</th>
              <th class="p-3">{{ t('spc.uploads.unitMapping') }}</th>
              <th class="p-3">{{ t('spc.uploads.controlItem') }}</th>
              <th class="p-3">{{ t('spc.uploads.value') }}</th>
              <th class="p-3">{{ t('spc.uploads.recheck') }}</th>
              <th class="p-3">{{ t('spc.uploads.adjust') }}</th>
              <th class="p-3">{{ t('spc.uploads.adjustAmount') }}</th>
              <th class="p-3">{{ t('spc.uploads.timeColumn') }}</th>
              <th class="p-3">{{ t('spc.uploads.operator') }}</th>
              <th class="p-3 w-72">{{ t('spc.uploads.validationDetails') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-650 dark:text-slate-300 font-medium">
            <tr v-for="d in errorDetails" :key="d.id" class="transition-colors bg-white dark:bg-slate-900 border-l-4 border-amber-400">
              <!-- Row No -->
              <td class="p-3 font-mono font-bold" :class="!d.isValid ? 'text-red-650 dark:text-red-400' : 'text-slate-450'">
                #{{ d.rowNo }}
              </td>

              <td class="p-3 align-top">
                <div class="font-bold text-slate-700 dark:text-slate-200">{{ d.sourceFile || batch?.originalFileName || '-' }}</div>
                <div class="mt-1 text-[11px] text-slate-500 dark:text-slate-400">
                  {{ d.sourceSheet || t('spc.uploads.sheet') }} · {{ t('spc.uploads.excelRow', { row: d.sourceRow || d.rowNo }) }}
                </div>
              </td>

              <!-- PartNo -->
              <td class="p-3" :class="{ 'relative group': hasCellError(d, 'PartNo') }">
                <span :class="hasCellError(d, 'PartNo') ? 'text-red-500 border-b border-dashed border-red-500 font-black' : 'font-bold text-slate-850 dark:text-white'">
                  {{ d.partNo }}
                </span>
                <div v-if="hasCellError(d, 'PartNo')" class="absolute z-50 left-10 bottom-6 hidden group-hover:block bg-red-900 text-red-100 text-[10px] p-2 rounded shadow-lg whitespace-nowrap border border-red-500">
                  {{ getCellErrorMessage(d, 'PartNo') }}
                </div>
              </td>

              <!-- ProcessCode -->
              <td class="p-3" :class="{ 'relative group': hasCellError(d, 'ProcessCode') }">
                <span :class="hasCellError(d, 'ProcessCode') ? 'text-red-500 border-b border-dashed border-red-500 font-black' : ''">
                  {{ d.processCode || '-' }} → {{ d.resolvedProcessCode || '-' }}
                </span>
                <div v-if="hasCellError(d, 'ProcessCode')" class="absolute z-50 left-10 bottom-6 hidden group-hover:block bg-red-900 text-red-100 text-[10px] p-2 rounded shadow-lg whitespace-nowrap border border-red-500">
                  {{ getCellErrorMessage(d, 'ProcessCode') }}
                </div>
              </td>

              <td class="p-3 align-top">
                <div class="font-bold">{{ d.machineCode || '-' }} → {{ d.resolvedMachineName || d.resolvedMachineCode || '-' }}</div>
                <span class="inline-flex mt-1 px-2 py-0.5 rounded border text-[10px] font-black" :class="matchStateClass(matchState(d, 'line'))">
                  {{ matchStateLabel(matchState(d, 'line')) }}
                </span>
              </td>
              <td class="p-3 align-top relative group">
                <div class="font-bold">{{ d.tankCode || '-' }} → {{ d.resolvedTankName || d.resolvedTankCode || '-' }}</div>
                <span class="inline-flex mt-1 px-2 py-0.5 rounded border text-[10px] font-black" :class="matchStateClass(matchState(d, 'tank'))">
                  {{ matchStateLabel(matchState(d, 'tank')) }}
                </span>
                <div v-if="hasCellError(d, 'TankCode')" class="mt-1 text-[10px] text-red-600 dark:text-red-300">
                  {{ getCellErrorMessage(d, 'TankCode') }}
                </div>
              </td>

              <!-- CharacteristicCode -->
              <td class="p-3 align-top">
                <div class="font-bold">{{ d.characteristicCode || '-' }} → {{ d.resolvedCharacteristicName || d.resolvedCharacteristicCode || '-' }}</div>
                <span class="inline-flex mt-1 px-2 py-0.5 rounded border text-[10px] font-black" :class="matchStateClass(matchState(d, 'characteristic'))">
                  {{ matchStateLabel(matchState(d, 'characteristic')) }}
                </span>
                <div v-if="hasCellError(d, 'CharacteristicCode')" class="mt-1 text-[10px] text-red-600 dark:text-red-300">
                  {{ getCellErrorMessage(d, 'CharacteristicCode') }}
                </div>
              </td>

              <td class="p-3 align-top">
                <div class="font-bold">{{ d.unit || t('spc.uploads.blank') }} → {{ d.resolvedMappingId ? (d.resolvedUnit || t('spc.uploads.blank')) : '-' }}</div>
                <span class="inline-flex mt-1 px-2 py-0.5 rounded border text-[10px] font-black" :class="matchStateClass(matchState(d, 'unit'))">
                  {{ matchStateLabel(matchState(d, 'unit')) }}
                </span>
                <div v-if="hasCellError(d, 'Unit')" class="mt-1 text-[10px] text-red-600 dark:text-red-300">
                  {{ getCellErrorMessage(d, 'Unit') }}
                </div>
              </td>

              <td class="p-3 align-top">
                <div class="font-bold">{{ d.resolvedMappingId ? `#${d.resolvedMappingId}` : '-' }}</div>
                <span class="inline-flex mt-1 px-2 py-0.5 rounded border text-[10px] font-black" :class="matchStateClass(matchState(d, 'mapping'))">
                  {{ matchStateLabel(matchState(d, 'mapping')) }}
                </span>
                <div v-if="hasCellError(d, 'PartProcessCharacteristic')" class="mt-1 text-[10px] text-red-600 dark:text-red-300">
                  {{ getCellErrorMessage(d, 'PartProcessCharacteristic') }}
                </div>
              </td>

              <!-- MeasuredValue / Counts -->
              <td class="p-3 font-mono font-bold" :class="{ 'relative group': hasCellError(d, 'MeasuredValue') }">
                <span :class="hasCellError(d, 'MeasuredValue') ? 'text-red-500 border-b border-dashed border-red-500' : ''">
                  {{ d.measuredValue !== null ? `${d.measuredValue}` : t('spc.uploads.defectTotal', { defect: d.defectQty || 0, total: d.inspectedQty || 0 }) }}
                </span>
                <div v-if="hasCellError(d, 'MeasuredValue')" class="absolute z-50 left-10 bottom-6 hidden group-hover:block bg-red-900 text-red-100 text-[10px] p-2 rounded shadow-lg whitespace-nowrap border border-red-500">
                  {{ getCellErrorMessage(d, 'MeasuredValue') }}
                </div>
              </td>

              <td class="p-3 font-mono text-slate-500 dark:text-slate-400">
                {{ d.recheckValue !== '' && d.recheckValue !== null && d.recheckValue !== undefined ? d.recheckValue : '-' }}
              </td>

              <td class="p-3">
                {{ d.adjustAction || '-' }}
              </td>

              <td class="p-3 font-mono text-slate-500 dark:text-slate-400">
                {{ d.adjustAmount !== '' && d.adjustAmount !== null && d.adjustAmount !== undefined ? d.adjustAmount : '-' }}
              </td>

              <!-- MeasuredAt -->
              <td class="p-3 font-mono text-slate-400">
                {{ formatMeasuredAt(d.measuredAt) }}
              </td>

              <!-- Operator -->
              <td class="p-3">
                {{ d.operator || t('spc.uploads.system') }}
              </td>

              <!-- Verification Messages -->
              <td class="p-3 font-semibold">
                <div v-if="d.isValid && d.duplicateStatus" class="text-amber-600 dark:text-amber-400 flex items-center gap-1">
                  {{ t('spc.uploads.duplicate', { status: d.duplicateStatus }) }}
                </div>
                <div v-else class="text-red-500 space-y-1">
                  <div v-for="errItem in d.errorsList" :key="errItem.id" class="space-y-0.5 text-[11px]">
                    <div class="flex items-center gap-1">
                      <AlertTriangle class="w-3 h-3 flex-shrink-0" />
                      <span class="font-black">{{ errItem.errorCode }}</span>
                      <span>{{ errItem.errorMessage }}</span>
                    </div>
                    <div v-if="errItem.suggestion" class="ml-4 text-amber-700 dark:text-amber-300">
                      {{ t('spc.uploads.suggestion', { text: errItem.suggestion }) }}
                    </div>
                  </div>
                </div>
              </td>
            </tr>

            <!-- Empty Rows fallback -->
            <tr v-if="errorDetails.length === 0">
              <td colspan="16" class="p-8 text-center text-slate-400 text-sm">
                {{ Number(batch?.errorRows || 0) === 0 ? t('spc.uploads.noRejected') : t('spc.uploads.refreshRejected') }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Back redirect button -->
      <div v-if="isImported" class="flex justify-end pt-4 border-t border-slate-100 dark:border-slate-800">
        <router-link to="/spc" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-cyan-300 font-bold transition-all text-xs border border-cyan-500/20">
          <ArrowLeft class="w-4 h-4" /> {{ t('spc.uploads.backCharts') }}
        </router-link>
      </div>
    </div>
  </div>
</template>
