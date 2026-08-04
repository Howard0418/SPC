<script setup>
import { computed, onMounted, ref } from "vue";
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
const batch = ref(null);
const details = ref([]);
const errors = ref([]);
const err = ref("");
const loading = ref(false);
const batchId = computed(() => route.params.batchId);
const chartPpcId = ref(null);
const importMode = ref("insertOnly");
const autoImporting = ref(false);
const revalidateProgress = ref(null);
const missingMappingCount = computed(() =>
  new Set(errors.value
    .filter(e => e.errorCode === "MAPPING_NOT_FOUND" || e.errorCode === "CHAR_NOT_FOUND")
    .map(e => e.uploadDetailId)).size
);
const blockedSetupCount = computed(() => {
  const setupIds = new Set(errors.value
    .filter(e => e.errorCode === "MAPPING_NOT_FOUND" || e.errorCode === "CHAR_NOT_FOUND")
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
        characteristicCode: p.CharacteristicCode || p["檢驗項目"] || "",
        resolvedProcessCode: p.ResolvedProcessCode || "",
        resolvedMachineCode: p.ResolvedMachineCode || "",
        resolvedTankCode: p.ResolvedTankCode || "",
        resolvedCharacteristicCode: p.ResolvedCharacteristicCode || "",
        duplicateStatus: p.DuplicateStatus || "",
        measuredValue: p.MeasuredValue !== undefined ? p.MeasuredValue : (p["測量值"] !== undefined ? p["測量值"] : null),
        recheckValue: p.RecheckValue !== undefined ? p.RecheckValue : (p["複驗"] !== undefined ? p["複驗"] : (p["複驗值"] !== undefined ? p["複驗值"] : "")),
        adjustAction: p.AdjustAction || p["調整"] || p["調整方式"] || "",
        adjustAmount: p.AdjustAmount !== undefined ? p.AdjustAmount : (p["調整量"] !== undefined ? p["調整量"] : ""),
        defectQty: p.DefectQty !== undefined ? p.DefectQty : (p["不良數"] !== undefined ? p["不良數"] : null),
        inspectedQty: p.InspectedQty !== undefined ? p.InspectedQty : (p["總數"] !== undefined ? p["總數"] : null),
        measuredAt: p.MeasuredAt || p["日期"] || "",
        operator: p.Operator || p["作業員"] || "",
        errorsList: rowErrors
      };
    });
    chartPpcId.value = await resolveChartPpcId();
    if (!isImported.value &&
        !autoImporting.value &&
        Number(batch.value?.validRows || 0) > 0 &&
        Number(batch.value?.errorRows || 0) === 0) {
      autoImporting.value = true;
      await confirmImport();
    }
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}

async function resolveChartPpcId() {
  const firstValid = details.value.find(d => d.isValid && d.partNo && d.processCode && d.characteristicCode);
  if (!firstValid) return null;

  try {
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
    err.value = "找不到此匯入批次對應的 SPC 管制項目，無法開啟管制圖。";
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
  if (!confirm(`確定批次建立缺少的品質特性設定？共影響 ${missingMappingCount.value} 列。`)) return;
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
      `建立品質特性：${data.createdCharacteristics || 0}`,
      `建立槽位主檔：${data.createdTanks || 0}`,
      `建立線別／槽位設定：${data.createdMappings || 0}`,
      `重用既有設定：${data.reusedMappings || 0}`,
      `因槽位不存在略過：${data.skippedMissingTank || 0}`,
      `因其他主檔不存在略過：${data.skippedMissingMasterData || 0}`,
      `原批次已重新驗證。`
    ].join("\n"));
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    window.clearInterval(progressTimer);
    revalidateProgress.value = null;
    loading.value = false;
  }
}

async function confirmImport() {
  if (errors.value.length > 0 && Number(batch.value?.validRows || 0) === 0) {
    err.value = "無有效資料可匯入。請先修正錯誤或重新上傳。";
    return;
  }
  loading.value = true;
  err.value = "";
  try {
    await api.post(`/uploads/${batchId.value}/confirm`, null, { params: { mode: importMode.value } });
    await load();
    if (isImported.value) {
      goToChart();
    }
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function discardImport() {
  if (!confirm("確定要放棄本次匯入並刪除暫存資料嗎？")) return;
  loading.value = true;
  err.value = "";
  try {
    await api.delete(`/uploads/${batchId.value}`);
    const isVariable = batch.value?.uploadType === "Variable";
    router.push(isVariable ? "/uploads/variable" : "/uploads/attribute");
  } catch (e) {
    err.value = "放棄匯入失敗：" + getApiErrorMessage(e);
  } finally {
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

onMounted(load);
</script>

<template>
  <div class="w-full max-w-[1920px] mx-auto px-1 sm:px-2 space-y-6">
    <div v-if="revalidateProgress" class="fixed inset-0 z-[100] bg-slate-950/60 flex items-center justify-center p-4">
      <div class="w-full max-w-lg rounded-3xl bg-white dark:bg-slate-900 p-7 shadow-2xl">
        <div class="flex justify-between text-sm font-black text-slate-800 dark:text-white mb-3">
          <span>正在批次建立並重新驗證</span>
          <span>{{ revalidateProgress.processed }} / {{ revalidateProgress.total }}（{{ revalidateProgress.percent }}%）</span>
        </div>
        <div class="h-4 rounded-full bg-slate-200 dark:bg-slate-700 overflow-hidden">
          <div class="h-full bg-amber-500 transition-all duration-300" :style="{ width: `${revalidateProgress.percent}%` }"></div>
        </div>
        <p class="mt-3 text-xs text-slate-500">請勿關閉頁面，完成後會自動刷新驗證結果。</p>
      </div>
    </div>
    <!-- Header -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-violet-600 via-purple-600 to-indigo-700 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div class="space-y-2">
          <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-cyan-200 border border-white/20">
            <Server class="w-3.5 h-3.5" /> Stage 2: 暫存批次校驗與確認匯入
          </div>
          <h1 class="text-3xl font-black tracking-tight">匯入資料檢核與異常對照預覽</h1>
          <p class="text-indigo-100 text-sm max-w-xl">
            批次識別碼：<code class="px-2 py-0.5 rounded bg-black/30 font-mono text-xs">{{ batchId }}</code>
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
            {{ isImported ? '已成功匯入正式資料表' : errors.length > 0 ? '含有格式/主檔異常' : ' Stage 2 檢驗通過' }}
          </span>

          <button
            v-if="!isImported"
            @click="confirmImport"
            :disabled="loading || Number(batch?.validRows || 0) === 0"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-emerald-500 text-white hover:bg-emerald-400 font-black shadow-xl shadow-emerald-900/30 disabled:opacity-50 disabled:cursor-not-allowed transition-all text-sm"
          >
            <RefreshCw v-if="loading" class="w-4 h-4 animate-spin text-white" />
            <ArrowRight v-else class="w-4 h-4 text-white" />
            確認轉入正式 SPC 運算
          </button>

          <button
            v-if="!isImported"
            @click="discardImport"
            :disabled="loading"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-rose-600 hover:bg-rose-500 text-white font-black shadow-xl shadow-indigo-900/30 disabled:opacity-50 transition-all text-sm"
          >
            <XCircle class="w-4 h-4 text-white" />
            放棄並刪除此暫存
          </button>

          <button
            v-if="isImported"
            @click="goToChart"
            :disabled="!chartPpcId"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-emerald-500 hover:bg-emerald-400 text-white font-black shadow-xl shadow-black/20 disabled:opacity-50 disabled:cursor-not-allowed transition-all text-sm"
          >
            <ArrowRight class="w-4 h-4" />
            查看管制圖
          </button>
        </div>
      </div>
    </div>

    <!-- API Errors -->
    <div v-if="err" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ err }}
    </div>

    <div v-if="!isImported && missingMappingCount > 0" class="p-5 rounded-2xl bg-amber-50 dark:bg-amber-950/20 border border-amber-300 dark:border-amber-800 flex flex-col md:flex-row md:items-center justify-between gap-3">
      <div>
        <p class="font-black text-amber-800 dark:text-amber-300">有 {{ missingMappingCount }} 列缺少品質特性或「線別＋槽位＋管制項目」設定</p>
        <p class="text-xs text-amber-700 dark:text-amber-400 mt-1">
          可直接建立：{{ actionableSetupCount }} 列；因線別／槽位主檔不存在而阻擋：{{ blockedSetupCount }} 列。
        </p>
        <details class="mt-2 text-xs text-amber-800 dark:text-amber-300">
          <summary class="cursor-pointer font-bold">查看比對與建立規則</summary>
          <ol class="list-decimal ml-5 mt-2 space-y-1">
            <li>線別先比對原代碼；找不到再比對「原代碼＋1」。</li>
            <li>槽位限定在線別底下，以槽位代碼或名稱比對。</li>
            <li>管制項目限定 CHEM，以代碼／名稱，再以項目＋單位比對。</li>
            <li>三者都存在才建立「線別＋槽位＋管制項目」設定；缺少槽位時會略過並保留錯誤。</li>
          </ol>
        </details>
      </div>
      <div class="flex gap-2">
        <button @click="createMissingMappings" :disabled="loading" class="px-5 py-2.5 rounded-xl bg-amber-600 hover:bg-amber-500 disabled:opacity-50 text-white text-sm font-black">
          批次建立並重新驗證
        </button>
        <button @click="goToMissingMappings" class="px-4 py-2.5 rounded-xl border border-amber-500 text-amber-700 dark:text-amber-300 text-sm font-bold">
          手動設定
        </button>
      </div>
    </div>

    <div v-if="!isImported" class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
      <label class="block text-sm font-black text-slate-800 dark:text-white mb-2">重複資料處理方式</label>
      <select v-model="importMode" class="w-full md:w-96 px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-800 text-sm">
        <option value="insertOnly">僅新增，重複資料略過（預設）</option>
        <option value="upsert">重複資料覆蓋舊資料</option>
      </select>
      <p class="mt-2 text-xs text-slate-500">重複鍵值：管制項目＋量測時間＋批號＋樣本號。</p>
    </div>

    <!-- Validation Summary Card -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3.5 rounded-xl bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400">
          <Table class="w-6 h-6" />
        </div>
        <div>
          <p class="text-xs text-slate-400 font-bold uppercase">總上傳列數 (Total)</p>
          <h3 class="text-2xl font-black text-slate-800 dark:text-white">{{ batch?.totalRows || 0 }} 筆</h3>
        </div>
      </div>

      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3.5 rounded-xl bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 class="w-6 h-6" />
        </div>
        <div>
          <p class="text-xs text-slate-400 font-bold uppercase">通過校驗列數 (Valid)</p>
          <h3 class="text-2xl font-black text-slate-800 dark:text-white text-emerald-600 dark:text-emerald-400">
            {{ batch?.validRows || 0 }} 筆
          </h3>
        </div>
      </div>

      <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3.5 rounded-xl bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400">
          <XCircle class="w-6 h-6" />
        </div>
        <div>
          <p class="text-xs text-slate-400 font-bold uppercase">攔截錯誤列數 (Errors)</p>
          <h3 class="text-2xl font-black" :class="errors.length > 0 ? 'text-red-500 animate-pulse' : 'text-slate-550 dark:text-slate-400'">
            {{ batch?.errorRows || 0 }} 筆
          </h3>
        </div>
      </div>
    </div>

    <!-- Unified Master Table with Highlighted Errors -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between border-b border-slate-100 dark:border-slate-850 pb-4">
        <h3 class="text-lg font-black text-slate-800 dark:text-white flex items-center gap-2">
          <Table class="w-5 h-5 text-blue-500" /> 未通過資料與錯誤原因
        </h3>
        <span class="text-xs text-slate-400 font-bold">全部資料皆已驗證；下方顯示所有未通過資料。</span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs border-collapse">
          <thead class="bg-slate-50 dark:bg-slate-800/50 text-slate-400 uppercase text-[11px] font-bold tracking-wider border-b border-slate-250 dark:border-slate-800">
            <tr>
              <th class="p-3 w-16">列號</th>
              <th class="p-3">產品料號 (PartNo)</th>
              <th class="p-3">製程對應</th>
              <th class="p-3">線別對應</th>
              <th class="p-3">槽位對應</th>
              <th class="p-3">管制項目對應</th>
              <th class="p-3">量測數值 (Value)</th>
              <th class="p-3">複驗 (Recheck)</th>
              <th class="p-3">調整 (Adjust)</th>
              <th class="p-3">調整量</th>
              <th class="p-3">時間 (Time)</th>
              <th class="p-3">人員 (OP)</th>
              <th class="p-3 w-72">校驗狀態 / 錯誤詳細原因</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-650 dark:text-slate-300 font-medium">
            <tr v-for="d in errorDetails" :key="d.id" class="transition-colors bg-red-500/5 dark:bg-red-950/20 text-red-700 dark:text-red-300 border-l-4 border-red-500">
              <!-- Row No -->
              <td class="p-3 font-mono font-bold" :class="!d.isValid ? 'text-red-650 dark:text-red-400' : 'text-slate-450'">
                #{{ d.rowNo }}
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

              <td class="p-3">{{ d.machineCode || '-' }} → {{ d.resolvedMachineCode || '-' }}</td>
              <td class="p-3">{{ d.tankCode || '-' }} → {{ d.resolvedTankCode || '-' }}</td>

              <!-- CharacteristicCode -->
              <td class="p-3" :class="{ 'relative group': hasCellError(d, 'CharacteristicCode') || hasCellError(d, 'PartProcessCharacteristic') }">
                <span :class="hasCellError(d, 'CharacteristicCode') || hasCellError(d, 'PartProcessCharacteristic') ? 'text-red-500 border-b border-dashed border-red-500 font-black' : 'text-blue-600 dark:text-blue-400 font-semibold'">
                  {{ d.characteristicCode }} → {{ d.resolvedCharacteristicCode || '-' }}
                </span>
                <div v-if="hasCellError(d, 'CharacteristicCode') || hasCellError(d, 'PartProcessCharacteristic')" class="absolute z-50 left-10 bottom-6 hidden group-hover:block bg-red-900 text-red-100 text-[10px] p-2 rounded shadow-lg border border-red-500">
                  {{ getCellErrorMessage(d, 'CharacteristicCode') || getCellErrorMessage(d, 'PartProcessCharacteristic') }}
                </div>
              </td>

              <!-- MeasuredValue / Counts -->
              <td class="p-3 font-mono font-bold" :class="{ 'relative group': hasCellError(d, 'MeasuredValue') }">
                <span :class="hasCellError(d, 'MeasuredValue') ? 'text-red-500 border-b border-dashed border-red-500' : ''">
                  {{ d.measuredValue !== null ? `${d.measuredValue}` : `不良/總數: ${d.defectQty || 0} / ${d.inspectedQty || 0}` }}
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
                {{ d.measuredAt ? new Date(d.measuredAt).toLocaleString() : '即時' }}
              </td>

              <!-- Operator -->
              <td class="p-3">
                {{ d.operator || '系統自動' }}
              </td>

              <!-- Verification Messages -->
              <td class="p-3 font-semibold">
                <div v-if="d.isValid && d.duplicateStatus" class="text-amber-600 dark:text-amber-400 flex items-center gap-1">
                  {{ `重複：${d.duplicateStatus}` }}
                </div>
                <div v-else class="text-red-500 space-y-1">
                  <div v-for="errItem in d.errorsList" :key="errItem.id" class="flex items-center gap-1 text-[11px]">
                    <AlertTriangle class="w-3 h-3 flex-shrink-0" /> {{ errItem.errorMessage }}
                  </div>
                </div>
              </td>
            </tr>

            <!-- Empty Rows fallback -->
            <tr v-if="errorDetails.length === 0">
              <td colspan="13" class="p-8 text-center text-slate-400 text-sm">
                {{ Number(batch?.errorRows || 0) === 0 ? '無未通過資料，系統將直接匯入。' : '仍有未通過資料，請重新整理後查看。' }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Back redirect button -->
      <div v-if="isImported" class="flex justify-end pt-4 border-t border-slate-100 dark:border-slate-800">
        <router-link to="/spc" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-slate-800 hover:bg-slate-700 text-cyan-300 font-bold transition-all text-xs border border-cyan-500/20">
          <ArrowLeft class="w-4 h-4" /> 返回 SPC 管制圖首頁
        </router-link>
      </div>
    </div>
  </div>
</template>
