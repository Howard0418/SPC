<script setup>
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
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
  const baseUrl = api.defaults?.baseURL || "http://localhost:5243";
  const url = `${baseUrl}/api/v2/reports/cpk-summary?month=${exportMonth.value}`;
  window.open(url, "_blank");
}

async function executeQuery() {
  err.value = "";
  loading.value = true;
  batches.value = [];
  alertsMap.value = {};

  try {
    const { data } = await api.get("/v2/traceability", { 
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
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function goToSpcChart(batchId) {
  router.push({ path: "/spc", query: { batchId } });
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
            多維度品質履歷與 SPC 查詢大廳
          </h1>
          <p class="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
            透過工單、批號 (Lot)、序號 (SN) 或時間區間進行複合式條件篩選。快速鎖定特定批次的量測紀錄並一鍵轉入 SPC 戰情室檢視 CPK 指標與管制圖。
          </p>
        </div>
      </div>

      <!-- Export Action Block -->
      <div class="flex items-center gap-3 z-10 bg-slate-800/80 p-3.5 rounded-2xl border border-slate-700/80 backdrop-blur shadow-xl">
        <div class="flex flex-col">
          <label class="text-[10px] font-bold text-slate-400 uppercase mb-1">選擇月份</label>
          <input v-model="exportMonth" type="month" class="bg-slate-900 border border-slate-700 rounded-lg px-2.5 py-1 text-xs text-white font-bold focus:outline-none focus:border-indigo-500" />
        </div>
        <button @click="exportCpkMaster" class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white font-bold text-xs shadow-lg shadow-emerald-500/20 transition-all self-end">
          <Download class="w-4 h-4" /> 📥 匯出廠級 CPK 總表 (Excel)
        </button>
      </div>
    </div>

    <!-- Query Panel -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex items-center gap-2 mb-4 pb-3 border-b border-slate-100 dark:border-slate-800/80">
        <Filter class="w-5 h-5 text-indigo-500" />
        <h3 class="font-bold text-slate-800 dark:text-slate-100">複合條件篩選 (Multi-dimensional Filter)</h3>
      </div>
      
      <form @submit.prevent="executeQuery" class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <!-- Work Order -->
        <div>
          <label class="block text-xs font-bold text-slate-500 uppercase mb-1">生產工單 (Work Order)</label>
          <div class="relative">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"><Layers class="w-4 h-4" /></span>
            <input v-model="queryParams.workOrderNo" type="text" placeholder="例如：WO-2026..." class="w-full pl-9 pr-3 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-white" />
          </div>
        </div>

        <!-- Lot No -->
        <div>
          <label class="block text-xs font-bold text-slate-500 uppercase mb-1">生產批號 (Lot No)</label>
          <div class="relative">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"><Box class="w-4 h-4" /></span>
            <input v-model="queryParams.lotNo" type="text" placeholder="例如：LOT-8899..." class="w-full pl-9 pr-3 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-white" />
          </div>
        </div>

        <!-- Serial No -->
        <div>
          <label class="block text-xs font-bold text-slate-500 uppercase mb-1">產品序號 (Serial No)</label>
          <div class="relative">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"><Hash class="w-4 h-4" /></span>
            <input v-model="queryParams.serialNo" type="text" placeholder="例如：SN-001..." class="w-full pl-9 pr-3 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-white" />
          </div>
        </div>

        <!-- Actions -->
        <div class="flex items-end gap-2">
          <button type="submit" :disabled="loading" class="w-full flex justify-center items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 hover:to-blue-500 text-white font-bold text-sm shadow-lg shadow-indigo-500/25 transition-all disabled:opacity-50 h-[42px]">
            <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
            <Search v-else class="w-4 h-4" />
            執行查詢
          </button>
        </div>
      </form>
    </div>

    <!-- Error State -->
    <div v-if="err" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-600 dark:text-red-400 text-sm flex items-center gap-3">
      <AlertTriangle class="w-5 h-5 flex-shrink-0" /> {{ err }}
    </div>

    <!-- Results Table -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-md overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              <th class="py-4 px-5">量測時間 / 操作員</th>
              <th class="py-4 px-5">料號 / 站別</th>
              <th class="py-4 px-5">追蹤屬性 (Lot / SN)</th>
              <th class="py-4 px-5">數據統計</th>
              <th class="py-4 px-5 text-center">異常狀態</th>
              <th class="py-4 px-5 text-right">操作 (SPC)</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/60 text-sm">
            <tr v-if="loading">
              <td colspan="6" class="py-12 text-center text-slate-400">
                <Activity class="w-8 h-8 animate-spin mx-auto mb-2 text-indigo-400" />
                正在大數據資料庫中檢索...
              </td>
            </tr>
            <tr v-else-if="batches.length === 0">
              <td colspan="6" class="py-12 text-center text-slate-400 font-medium">查無符合條件的量測紀錄</td>
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
                <div class="text-xs text-slate-400 mt-0.5 font-mono">OP: {{ b.operatorName || 'System' }}</div>
              </td>

              <!-- Part / Station -->
              <td class="py-4 px-5">
                <div class="font-bold text-indigo-600 dark:text-indigo-400 text-xs bg-indigo-50 dark:bg-indigo-950/40 inline-block px-2 py-0.5 rounded border border-indigo-100 dark:border-indigo-900/50">
                  Part: {{ b.productId }}
                </div>
                <div class="text-xs text-slate-500 mt-1 font-mono">Station: {{ b.stationId }}</div>
              </td>

              <!-- Lot / SN -->
              <td class="py-4 px-5">
                <div class="font-mono text-xs space-y-1">
                  <div v-if="b.lotNo" class="text-slate-600 dark:text-slate-300"><span class="text-slate-400">Lot:</span> {{ b.lotNo }}</div>
                  <div v-if="b.serialNo" class="text-slate-600 dark:text-slate-300"><span class="text-slate-400">SN:</span> {{ b.serialNo }}</div>
                  <div v-if="!b.lotNo && !b.serialNo" class="text-slate-400 italic">無追蹤碼</div>
                </div>
              </td>

              <!-- Stats -->
              <td class="py-4 px-5">
                <div class="text-xs text-slate-600 dark:text-slate-300 font-bold bg-slate-100 dark:bg-slate-800 inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg border border-slate-200 dark:border-slate-700">
                  <Hash class="w-3.5 h-3.5" /> 包含 {{ b.values ? b.values.length : 0 }} 筆點位資料
                </div>
              </td>

              <!-- Alert Status -->
              <td class="py-4 px-5 text-center">
                <div v-if="alertsMap[b.id] && alertsMap[b.id].length > 0" class="flex flex-col items-center gap-1">
                  <span class="px-2.5 py-1 rounded-md text-[10px] font-bold bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/30 inline-flex items-center gap-1 animate-pulse">
                    <AlertTriangle class="w-3.5 h-3.5" /> 觸發 {{ alertsMap[b.id].length }} 筆警報
                  </span>
                </div>
                <div v-else class="flex flex-col items-center">
                  <span class="px-2.5 py-1 rounded-md text-[10px] font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 inline-flex items-center gap-1">
                    <CheckCircle2 class="w-3.5 h-3.5" /> 品質正常
                  </span>
                </div>
              </td>

              <!-- Action -->
              <td class="py-4 px-5 text-right">
                <button
                  @click="goToSpcChart(b.id)"
                  class="inline-flex items-center gap-1.5 px-4 py-2 rounded-xl text-xs font-bold transition-all bg-white dark:bg-slate-800 text-indigo-600 dark:text-indigo-400 border border-indigo-200 dark:border-indigo-800 hover:bg-indigo-50 dark:hover:bg-indigo-900/50 shadow-sm"
                >
                  <BarChart2 class="w-4 h-4" /> 轉入 SPC 管制圖
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
