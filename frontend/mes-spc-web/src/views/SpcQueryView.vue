<script setup>
import {useI18n} from "vue-i18n";
import {displayUiMessage} from "../i18n/uiMessage";
import {normalizeLocale} from "../i18n/preferences";
const {t,locale}=useI18n({useScope:"global"});
import ModuleGuide from "../components/ModuleGuide.vue";
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { api, getApiErrorDisplay } from "../api/client";
import { 
  Search, 
  Filter, 
  Activity, 
  Layers, 
  Box, 
  Hash, 
  Calendar,
  AlertTriangle,
  CheckCircle2,
  BarChart2,
  RefreshCw,
  Download
} from "lucide-vue-next";

const router = useRouter();

const queryParams = ref({
  workOrderNo: "",
  lotNo: "",
  serialNo: "",
  partId: ""
});

const loading = ref(false);
const err = ref("");
const batches = ref([]);
const alertsMap = ref({});
const exportMonth = ref(new Date().toISOString().slice(0, 7));

function exportCpkMaster() {
  const baseUrl = (api.defaults?.baseURL || "/api").replace(/\/+$/, "");
  const month = encodeURIComponent(exportMonth.value);
  const url = `${baseUrl}/v1/reports/cpk-summary?month=${month}&uiLocale=${encodeURIComponent(normalizeLocale(locale.value))}`;
  window.open(url, "_blank");
}

async function executeQuery() {
  err.value = "";
  loading.value = true;
  batches.value = [];
  alertsMap.value = {};

  try {
    const { data } = await api.get("/v1/traceability", { 
      params: {
        workOrderNo: queryParams.value.workOrderNo || undefined,
        lotNo: queryParams.value.lotNo || undefined,
        serialNo: queryParams.value.serialNo || undefined
      } 
    });
    
    batches.value = data.batches || [];
    
    if (data.alerts) {
      data.alerts.forEach(a => {
        if (!alertsMap.value[a.batchId]) {
          alertsMap.value[a.batchId] = [];
        }
        alertsMap.value[a.batchId].push(a);
      });
    }

  } catch (e) {
    err.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

function goToSpcChart(batch) {
  const traceKey = batch.lotNo || batch.batchNo;

  if (!batch.ppcId) {
    err.value = {key:'spc.trace.missingPpc'};
    return;
  }

  router.push({
    path: "/spc",
    query: {
      ppcId: batch.ppcId,
      ...(traceKey ? { batchId: traceKey } : {})
    }
  });
}

onMounted(() => {
  executeQuery();
});
</script>

<template>
  <div class="space-y-6 pb-12">
    <!-- Header Banner -->
    <div class="p-6 rounded-2xl bg-gradient-to-r from-slate-900 via-indigo-950 to-slate-900 text-white shadow-xl border border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-6 relative overflow-hidden">
      <div class="absolute -right-12 -top-12 w-64 h-64 bg-indigo-500/10 rounded-full blur-3xl pointer-events-none"></div>
      
      <div class="flex items-start gap-4 z-10">
        <div class="p-3 bg-indigo-500/20 text-indigo-400 border border-indigo-500/30 rounded-2xl shadow-lg">
          <Search class="w-8 h-8" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-indigo-500/20 text-indigo-300 border border-indigo-500/30">Quality Traceability</span>
          </div>
          <h1 class="text-2xl font-black mt-1 bg-gradient-to-r from-white via-slate-100 to-slate-300 bg-clip-text text-transparent">
            {{ t('spc.trace.queryTitle') }}
          </h1>
          <p class="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
            {{ t('spc.trace.queryDescription') }}
          </p>
        </div>
      </div>

      <!-- Export Action Block -->
      <div class="flex items-center gap-3 z-10 bg-slate-800/80 p-3.5 rounded-2xl border border-slate-700/80 backdrop-blur shadow-xl">
        <div class="flex flex-col">
          <label class="text-[10px] font-bold text-slate-400 uppercase mb-1">{{ t('spc.trace.month') }}</label>
          <input data-testid="trace-exportMonth" v-model="exportMonth" type="month" class="bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1 text-xs text-white font-bold focus:outline-none focus:border-indigo-500" />
        </div>
        <button data-testid="trace-exportCpkMaster" @click="exportCpkMaster" class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white font-bold text-xs shadow-lg shadow-emerald-500/20 transition-all self-end">
          <Download class="w-4 h-4" /> {{ t('spc.trace.exportCpk') }}
        </button>
      </div>
    </div>

    <!-- Guide / Operation Tip -->
    <ModuleGuide :title="t('spc.trace.queryGuide')">
      <p class="text-xs text-indigo-700 dark:text-indigo-400/80 mt-1.5 leading-relaxed">
          {{ t('spc.trace.queryGuideDescription') }}
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-indigo-700 dark:text-indigo-400/80 leading-relaxed">
          <div class="font-black text-indigo-900 dark:text-indigo-300">{{ t('spc.trace.queryInstructions') }}</div>
          <p><strong>{{ t('spc.trace.enterCriteria') }}</strong>{{ t('spc.trace.criteriaHelp') }}</p>
          <p><strong>{{ t('spc.trace.execute') }}</strong>{{ t('spc.trace.executeHelp') }}</p>
          <p><strong>{{ t('spc.trace.viewChart') }}</strong>{{ t('spc.trace.viewChartHelp') }}</p>
          <p><strong>{{ t('spc.trace.exportReport') }}</strong>{{ t('spc.trace.exportHelp') }}</p>
        </div>
    </ModuleGuide>

    <!-- Query Panel -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex items-center gap-2 mb-4 pb-3 border-b border-slate-100 dark:border-slate-800/80">
        <Filter class="w-5 h-5 text-indigo-500" />
        <h3 class="font-bold text-slate-800 dark:text-slate-100">{{ t('spc.trace.filter') }}</h3>
      </div>
      
      <form @submit.prevent="executeQuery" class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <!-- Work Order -->
        <div>
          <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.trace.workOrder') }}</label>
          <div class="relative">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"><Layers class="w-4 h-4" /></span>
            <input data-testid="trace-queryParams.workOrderNo" v-model="queryParams.workOrderNo" type="text" :placeholder="t('spc.trace.workOrderExample')" class="w-full pl-9 pr-3 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-white" />
          </div>
        </div>

        <!-- Lot No -->
        <div>
          <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.trace.lot') }}</label>
          <div class="relative">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"><Box class="w-4 h-4" /></span>
            <input data-testid="trace-queryParams.lotNo" v-model="queryParams.lotNo" type="text" :placeholder="t('spc.trace.lotExample')" class="w-full pl-9 pr-3 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-white" />
          </div>
        </div>

        <!-- Serial No -->
        <div>
          <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.trace.serial') }}</label>
          <div class="relative">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"><Hash class="w-4 h-4" /></span>
            <input data-testid="trace-queryParams.serialNo" v-model="queryParams.serialNo" type="text" :placeholder="t('spc.trace.serialExample')" class="w-full pl-9 pr-3 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-white" />
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-end gap-2">
          <button data-testid="trace-executeQuery" type="submit" :disabled="loading" class="w-full flex justify-center items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 hover:to-blue-500 text-white font-bold text-sm shadow-lg shadow-indigo-500/25 transition-all disabled:opacity-50 h-[42px]">
            <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
            <Search v-else class="w-4 h-4" />
            {{ t('spc.trace.query') }}
          </button>
        </div>
      </form>
    </div>

    <!-- Error State -->
    <div v-if="err" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-400 text-sm flex items-center gap-3">
      <AlertTriangle class="w-5 h-5 flex-shrink-0" /> {{ displayUiMessage(t,err) }}
    </div>

    <!-- Results Table -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-md overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              <th class="py-4 px-5">{{ t('spc.trace.timeOperator') }}</th>
              <th class="py-4 px-5">{{ t('spc.trace.partProcess') }}</th>
              <th class="py-4 px-5">{{ t('spc.trace.tracking') }}</th>
              <th class="py-4 px-5">{{ t('spc.trace.statistics') }}</th>
              <th class="py-4 px-5 text-center">{{ t('spc.trace.alertStatus') }}</th>
              <th class="py-4 px-5 text-right">{{ t('spc.trace.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/60 text-sm">
            <tr v-if="loading">
              <td colspan="6" class="py-12 text-center text-slate-400">
                <Activity class="w-8 h-8 animate-spin mx-auto mb-2 text-indigo-400" />
                {{ t('spc.trace.queryLoading') }}
              </td>
            </tr>
            <tr v-else-if="batches.length === 0">
              <td colspan="6" class="py-12 text-center text-slate-400 font-medium">{{ t('spc.trace.queryEmpty') }}</td>
            </tr>
            <tr
              v-else
              v-for="b in batches"
              :key="b.id"
              class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors"
            >
              <!-- Time & Operator -->
              <td class="py-4 px-5">
                <div class="font-bold text-slate-800 dark:text-slate-200">
                  {{ new Date(b.measuredAt).toLocaleString() }}
                </div>
                <div class="text-xs text-slate-400 mt-0.5 font-mono">{{ t('spc.trace.operator') }} {{ b.operatorName || t('spc.trace.system') }}</div>
              </td>

              <!-- Part / Station -->
              <td class="py-4 px-5">
                <div class="font-bold text-indigo-600 dark:text-indigo-400 text-xs bg-indigo-50 dark:bg-indigo-950/40 inline-block px-2 py-0.5 rounded border border-indigo-100 dark:border-indigo-900/50">
                  {{ b.partNo || t('spc.trace.partFallback',{id:String(b.partId)}) }}
                </div>
                <div class="text-xs text-slate-500 mt-1 font-mono">
                  {{ b.processCode || t('spc.trace.processFallback',{id:String(b.processId)}) }} / {{ b.characteristicCode || t('spc.trace.itemFallback',{id:String(b.characteristicId)}) }}
                </div>
              </td>

              <!-- Lot / SN -->
              <td class="py-4 px-5">
                <div class="font-mono text-xs space-y-1">
                  <div v-if="b.lotNo" class="text-slate-600 dark:text-slate-300"><span class="text-slate-400">Lot:</span> {{ b.lotNo }}</div>
                  <div v-if="b.serialNo" class="text-slate-600 dark:text-slate-300"><span class="text-slate-400">SN:</span> {{ b.serialNo }}</div>
                  <div v-if="!b.lotNo && !b.serialNo" class="text-slate-400 italic">{{ t('spc.trace.noTracking') }}</div>
                </div>
              </td>

              <!-- Stats -->
              <td class="py-4 px-5">
                <div class="text-xs text-slate-600 dark:text-slate-300 font-bold bg-slate-100 dark:bg-slate-800 inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg border border-slate-200 dark:border-slate-700">
                  <Hash class="w-3.5 h-3.5" /> {{ t('spc.trace.includes') }} {{ b.values ? b.values.length : 0 }} {{ t('spc.trace.pointData') }}
                </div>
              </td>

              <!-- Alert Status -->
              <td class="py-4 px-5 text-center">
                <div v-if="alertsMap[b.id] && alertsMap[b.id].length > 0" class="flex flex-col items-center gap-1">
                  <span class="px-2.5 py-1 rounded-md text-[10px] font-bold bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/30 inline-flex items-center gap-1 animate-pulse">
                    <AlertTriangle class="w-3.5 h-3.5" /> {{ t('spc.trace.triggered') }} {{ alertsMap[b.id].length }} {{ t('spc.trace.alerts') }}
                  </span>
                </div>
                <div v-else class="flex flex-col items-center">
                  <span class="px-2.5 py-1 rounded-md text-[10px] font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 inline-flex items-center gap-1">
                    <CheckCircle2 class="w-3.5 h-3.5" /> {{ t('spc.trace.qualityNormal') }}
                  </span>
                </div>
              </td>

              <!-- Action -->
              <td class="py-4 px-5 text-right">
                <button
                  data-testid="trace-goToSpcChart" @click="goToSpcChart(b)"
                  class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold transition-all bg-white dark:bg-slate-800 text-indigo-600 dark:text-indigo-400 border border-indigo-200 dark:border-indigo-800 hover:bg-indigo-50 dark:hover:bg-indigo-900/50 shadow-sm"
                >
                  <BarChart2 class="w-4 h-4" /> {{ t('spc.trace.openChart') }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
