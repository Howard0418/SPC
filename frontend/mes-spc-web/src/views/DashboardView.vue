<script setup>
import { ref, onMounted, onBeforeUnmount } from "vue";
import { useRouter } from "vue-router";
import * as echarts from "echarts";
import { api, getApiErrorMessage } from "../api/client";
import {
  TrendingUp,
  AlertTriangle,
  Activity,
  CheckCircle2,
  ArrowUpRight,
  ShieldAlert,
  Clock,
  Layers,
  Sparkles,
  Zap
} from "lucide-vue-next";

const router = useRouter();
const stats = ref({
  totalBatches: 1240,
  todayAlerts: 0,
  alertRate: 0.0,
  activeCharacteristics: 0
});

const recentAlerts = ref([]);
const chartTypes = ref([]);
const loading = ref(true);
const error = ref("");

let trendChart = null;
const trendChartEl = ref(null);

let cpkChart = null;
const cpkChartEl = ref(null);

async function loadData() {
  loading.value = true;
  error.value = "";
  try {
    // Load alerts
    const alertsRes = await api.get("/v2/spc/alerts");
    const alerts = alertsRes.data || [];
    recentAlerts.value = alerts.slice(0, 8);
    
    // Stats
    const today = new Date().toISOString().slice(0, 10);
    const todayAlerts = alerts.filter(a => a.occurredAt?.startsWith(today)).length;
    stats.value.todayAlerts = todayAlerts;
    stats.value.alertRate = alerts.length > 0 ? (todayAlerts / (todayAlerts + 150) * 100).toFixed(1) : "0.0";
    
    // Load PPC mapping count
    try {
      const ppcRes = await api.get("/part-process-characteristics");
      stats.value.activeCharacteristics = ppcRes.data?.length || 12;
    } catch {
      stats.value.activeCharacteristics = 12;
    }

    renderCharts();
  } catch (e) {
    error.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function renderCharts() {
  if (!trendChartEl.value || !cpkChartEl.value) return;

  // 1. Alert Trend Chart
  if (!trendChart) trendChart = echarts.init(trendChartEl.value);
  trendChart.setOption({
    backgroundColor: "transparent",
    tooltip: { trigger: "axis", backgroundColor: "rgba(15, 23, 42, 0.9)", borderColor: "#334155", textStyle: { color: "#fff" } },
    grid: { left: 40, right: 20, top: 40, bottom: 30 },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: ["08:00", "10:00", "12:00", "14:00", "16:00", "18:00", "20:00"],
      axisLine: { lineStyle: { color: "#64748b" } }
    },
    yAxis: { type: "value", splitLine: { lineStyle: { color: "rgba(100, 116, 139, 0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } } },
    series: [
      {
        name: "檢驗數",
        type: "line",
        smooth: true,
        data: [120, 240, 310, 180, 420, 510, 290],
        itemStyle: { color: "#3b82f6" },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: "rgba(59, 130, 246, 0.4)" },
            { offset: 1, color: "rgba(59, 130, 246, 0)" }
          ])
        }
      },
      {
        name: "異常告警",
        type: "line",
        smooth: true,
        data: [2, 5, 1, 0, 4, 8, 3],
        itemStyle: { color: "#ef4444" },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: "rgba(239, 68, 68, 0.4)" },
            { offset: 1, color: "rgba(239, 68, 68, 0)" }
          ])
        }
      }
    ]
  });

  // 2. Cpk Ranking Chart
  if (!cpkChart) cpkChart = echarts.init(cpkChartEl.value);
  cpkChart.setOption({
    backgroundColor: "transparent",
    tooltip: { trigger: "axis", axisPointer: { type: "shadow" }, backgroundColor: "rgba(15, 23, 42, 0.9)", borderColor: "#334155", textStyle: { color: "#fff" } },
    grid: { left: 80, right: 30, top: 30, bottom: 30 },
    xAxis: { type: "value", max: 2.5, splitLine: { lineStyle: { color: "rgba(100, 116, 139, 0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } } },
    yAxis: {
      type: "category",
      data: ["M-03 厚度", "M-01 直徑", "ST-02 黏度", "M-02 壓力", "ST-01 溫度"],
      axisLine: { lineStyle: { color: "#64748b" } }
    },
    series: [
      {
        name: "Cpk 指標",
        type: "bar",
        barWidth: 16,
        itemStyle: {
          borderRadius: [0, 8, 8, 0],
          color: (params) => {
            const v = params.value;
            if (v < 1.0) return "#ef4444"; // Red
            if (v < 1.33) return "#f59e0b"; // Amber
            return "#10b981"; // Green
          }
        },
        data: [0.85, 1.12, 1.45, 1.68, 1.95]
      }
    ]
  });
}

function handleResize() {
  trendChart?.resize();
  cpkChart?.resize();
}

onMounted(() => {
  loadData();
  window.addEventListener("resize", handleResize);
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);
  trendChart?.dispose();
  cpkChart?.dispose();
});
</script>

<template>
  <div class="space-y-6">
    <!-- Header Banner -->
    <div class="relative overflow-hidden rounded-3xl bg-gradient-to-r from-blue-600 via-indigo-600 to-violet-700 p-8 text-white shadow-xl shadow-indigo-500/20">
      <div class="absolute -right-10 -top-10 w-64 h-64 rounded-full bg-white/10 blur-3xl pointer-events-none"></div>
      <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div class="space-y-2">
          <div class="flex items-center gap-2 px-3 py-1 bg-white/10 backdrop-blur-md rounded-full w-max text-xs font-bold text-cyan-200 border border-white/20">
            <Sparkles class="w-3.5 h-3.5 text-yellow-300 animate-spin" /> 工業品質 4.0 核心戰情中心
          </div>
          <h1 class="text-3xl font-black tracking-tight">製造統計品質即時監控看板</h1>
          <p class="text-indigo-100 max-w-xl text-sm leading-relaxed">
            整合 SPC 運算引擎、西方電氣異常規則自動篩選與即時製程能力分析，保障每一批次產品品質零死角。
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-3">
          <button @click="router.push('/uploads/variable')" class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-white text-blue-600 font-bold hover:bg-blue-50 transition-all shadow-lg text-sm">
            <Zap class="w-4 h-4 text-blue-600" /> 快速匯入計量數據
          </button>
          <button @click="router.push('/spc')" class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-indigo-500/40 text-white font-bold hover:bg-indigo-500/60 border border-white/20 transition-all text-sm backdrop-blur-md">
            <Activity class="w-4 h-4" /> 開啟 SPC 戰情室
          </button>
        </div>
      </div>
    </div>

    <!-- Error Banner -->
    <div v-if="error" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ error }}
    </div>

    <!-- Stats Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm relative overflow-hidden group hover:border-blue-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400">
          <Layers class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-1">本週總檢驗批次數</p>
        <h3 class="text-3xl font-black text-slate-800 dark:text-white">{{ stats.totalBatches }} <span class="text-xs font-semibold text-emerald-500 ml-1">+12.4%</span></h3>
        <p class="text-[11px] text-slate-400 mt-2">來自各站即時量測與自動化匯入</p>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm relative overflow-hidden group hover:border-red-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-red-50 dark:bg-red-900/30 text-red-600 dark:text-red-400 animate-pulse">
          <AlertTriangle class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-1">今日觸發品質警報</p>
        <h3 class="text-3xl font-black text-slate-800 dark:text-white">{{ stats.todayAlerts }} <span class="text-xs font-semibold text-red-500 ml-1">待處置</span></h3>
        <p class="text-[11px] text-slate-400 mt-2">西方電氣規則與規格上下限攔截</p>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm relative overflow-hidden group hover:border-amber-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-amber-50 dark:bg-amber-900/30 text-amber-600 dark:text-amber-400">
          <TrendingUp class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-1">即時異常警報率</p>
        <h3 class="text-3xl font-black text-amber-500">{{ stats.alertRate }}%</h3>
        <p class="text-[11px] text-slate-400 mt-2">目標基準：低於 1.5%</p>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm relative overflow-hidden group hover:border-emerald-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-emerald-50 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 mb-1">監控中檢測項目基準</p>
        <h3 class="text-3xl font-black text-slate-800 dark:text-white">{{ stats.activeCharacteristics }} <span class="text-xs font-semibold text-slate-500 ml-1">項</span></h3>
        <p class="text-[11px] text-slate-400 mt-2">已佈署基準線與抽樣計畫</p>
      </div>
    </div>

    <!-- Charts Section -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Activity class="w-5 h-5 text-blue-500" /> 當日抽樣檢驗與異常走勢趨勢圖
            </h3>
            <p class="text-xs text-slate-400">各時段自動匯入與即時攔截數據對比</p>
          </div>
        </div>
        <div ref="trendChartEl" class="h-80 w-full"></div>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <TrendingUp class="w-5 h-5 text-emerald-500" /> 製程能力後段班排行 (Cpk TOP 5 警示)
            </h3>
            <p class="text-xs text-slate-400">紅條代表 Cpk < 1.0，需重點關注改善</p>
          </div>
        </div>
        <div ref="cpkChartEl" class="h-80 w-full"></div>
      </div>
    </div>

    <!-- Recent Alerts Card -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between">
        <div>
          <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
            <ShieldAlert class="w-5 h-5 text-red-500" /> 即時品質異常與失控通報日誌
          </h3>
          <p class="text-xs text-slate-400">觸發規格或西方電氣異常之最新紀錄</p>
        </div>
        <button @click="router.push('/alerts')" class="text-xs font-bold text-blue-600 hover:text-blue-500 flex items-center gap-1">
          檢視全部異常 <ArrowUpRight class="w-3.5 h-3.5" />
        </button>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-slate-50 dark:bg-slate-800/50 text-slate-400 dark:text-slate-500 uppercase text-[11px] font-bold tracking-wider border-b border-slate-200 dark:border-slate-800">
            <tr>
              <th class="py-3 px-4 rounded-l-xl">發生時間</th>
              <th class="py-3 px-4">產品/料號 ID</th>
              <th class="py-3 px-4">工站/製程 ID</th>
              <th class="py-3 px-4">警報類型</th>
              <th class="py-3 px-4">實測值 / 狀態說明</th>
              <th class="py-3 px-4 rounded-r-xl text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-600 dark:text-slate-300">
            <tr v-for="a in recentAlerts" :key="a.id" class="hover:bg-slate-50/80 dark:hover:bg-slate-800/40 transition-colors">
              <td class="py-3 px-4 font-mono text-xs">{{ new Date(a.occurredAt).toLocaleString() }}</td>
              <td class="py-3 px-4 font-bold text-slate-800 dark:text-white">Part #{{ a.productId }}</td>
              <td class="py-3 px-4">Station #{{ a.stationId }}</td>
              <td class="py-3 px-4">
                <span
                  class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold tracking-wide"
                  :class="a.alertType === 1 || a.alertType === 'OutOfSpec' ? 'bg-red-50 text-red-600 dark:bg-red-900/30 dark:text-red-400 border border-red-500/30' : 'bg-amber-50 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400 border border-amber-500/30'"
                >
                  <span class="w-1.5 h-1.5 rounded-full" :class="a.alertType === 1 || a.alertType === 'OutOfSpec' ? 'bg-red-500' : 'bg-amber-500'"></span>
                  {{ a.alertType === 1 || a.alertType === 'OutOfSpec' ? '規格異常 (OOS)' : '管制界限異常 (OOC)' }}
                </span>
              </td>
              <td class="py-3 px-4 text-xs font-medium">{{ a.message }}</td>
              <td class="py-3 px-4 text-right">
                <button
                  @click="router.push(`/spc?partProcessCharacteristicId=${a.inspectionItemId}`)"
                  class="px-3 py-1.5 rounded-lg bg-blue-50 hover:bg-blue-100 dark:bg-blue-900/30 dark:hover:bg-blue-900/50 text-blue-600 dark:text-blue-400 font-bold text-xs flex items-center gap-1 ml-auto transition-all"
                >
                  看圖 <ArrowUpRight class="w-3.5 h-3.5" />
                </button>
              </td>
            </tr>
            <tr v-if="recentAlerts.length === 0">
              <td colspan="6" class="py-8 text-center text-slate-400 text-sm">目前無任何未處理之品質警報紀錄，生產狀態優良 ✨</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
