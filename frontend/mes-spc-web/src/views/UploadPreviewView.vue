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

const isImported = computed(() => {
  return batch.value?.importStatus === "Imported" || batch.value?.isConfirmed === true;
});

async function load() {
  err.value = "";
  try {
    const { data } = await api.get(`/uploads/${batchId.value}/preview`);
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
        characteristicCode: p.CharacteristicCode || p["檢驗項目"] || "",
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

async function confirmImport() {
  if (errors.value.length > 0 && details.value.filter(d => d.isValid).length === 0) {
    err.value = "無有效資料可匯入。請先修正錯誤或重新上傳。";
    return;
  }
  loading.value = true;
  err.value = "";
  try {
    await api.post(`/uploads/${batchId.value}/confirm`);
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
  <div class="max-w-6xl mx-auto space-y-6">
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
            :disabled="loading || details.filter(d => d.isValid).length === 0"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-white text-indigo-650 hover:bg-indigo-50 font-black shadow-xl shadow-black/20 disabled:opacity-50 disabled:cursor-not-allowed transition-all text-sm"
          >
            <RefreshCw v-if="loading" class="w-4 h-4 animate-spin text-indigo-600" />
            <ArrowRight v-else class="w-4 h-4 text-indigo-600" />
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
          <Table class="w-5 h-5 text-blue-500" /> 上傳資料明細與檢核結果預覽 (Live Preview Workspace)
        </h3>
        <span class="text-xs text-slate-400 font-bold">⚠️ 系統已自動排除不合規點位，點位以紅色高亮示警。</span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs border-collapse">
          <thead class="bg-slate-50 dark:bg-slate-800/50 text-slate-400 uppercase text-[11px] font-bold tracking-wider border-b border-slate-250 dark:border-slate-800">
            <tr>
              <th class="p-3 w-16">列號</th>
              <th class="p-3">產品料號 (PartNo)</th>
              <th class="p-3">製程代碼 (Process)</th>
              <th class="p-3">檢測項目 (Char)</th>
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
            <tr v-for="d in details" :key="d.id" class="transition-colors" :class="d.isValid ? 'hover:bg-slate-50/50 dark:hover:bg-slate-800/40' : 'bg-red-500/5 dark:bg-red-950/20 text-red-700 dark:text-red-300 border-l-4 border-red-500'">
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
                  {{ d.processCode }}
                </span>
                <div v-if="hasCellError(d, 'ProcessCode')" class="absolute z-50 left-10 bottom-6 hidden group-hover:block bg-red-900 text-red-100 text-[10px] p-2 rounded shadow-lg whitespace-nowrap border border-red-500">
                  {{ getCellErrorMessage(d, 'ProcessCode') }}
                </div>
              </td>

              <!-- CharacteristicCode -->
              <td class="p-3" :class="{ 'relative group': hasCellError(d, 'CharacteristicCode') || hasCellError(d, 'PartProcessCharacteristic') }">
                <span :class="hasCellError(d, 'CharacteristicCode') || hasCellError(d, 'PartProcessCharacteristic') ? 'text-red-500 border-b border-dashed border-red-500 font-black' : 'text-blue-600 dark:text-blue-400 font-semibold'">
                  {{ d.characteristicCode }}
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
                <div v-if="d.isValid" class="text-emerald-600 dark:text-emerald-400 flex items-center gap-1">
                  <CheckCircle2 class="w-3.5 h-3.5" /> 檢驗通過
                </div>
                <div v-else class="text-red-500 space-y-1">
                  <div v-for="errItem in d.errorsList" :key="errItem.id" class="flex items-center gap-1 text-[11px]">
                    <AlertTriangle class="w-3 h-3 flex-shrink-0" /> {{ errItem.errorMessage }}
                  </div>
                </div>
              </td>
            </tr>

            <!-- Empty Rows fallback -->
            <tr v-if="!details || details.length === 0">
              <td colspan="11" class="p-8 text-center text-slate-400 text-sm">
                無任何量測上傳數據明細。
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
