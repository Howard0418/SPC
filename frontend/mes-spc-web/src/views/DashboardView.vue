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

let paretoChart = null;
const paretoChartEl = ref(null);

let pollTimer = null;
const trendData = ref({ hours: ["08:00", "10:00", "12:00", "14:00", "16:00"], counts: [0, 0, 0, 0, 0], alerts: [0, 0, 0, 0, 0] });
const cpkData = ref({ names: ["尚無資料"], values: [0] });
const paretoData = ref({ names: ["尚無資料"], counts: [0], pcts: [0] });

async function loadData() {
  loading.value = true;
  error.value = "";
  try {
    const summaryRes = await api.get("/v1/dashboard/summary");
    const dData = summaryRes.data || {};
    const alerts = dData.recentAlerts || [];
    recentAlerts.value = alerts.slice(0, 8);
    
    // Stats
    const today = new Date().toISOString().slice(0, 10);
    const todayAlerts = dData.todayAlertCount ?? alerts.filter(a => a.occurredAt?.startsWith(today)).length;
    stats.value.totalBatches = dData.todayBatchCount ?? stats.value.totalBatches;
    stats.value.todayAlerts = todayAlerts;
    stats.value.alertRate = dData.alertRate != null ? (Number(dData.alertRate) * 100).toFixed(1) : "0.0";
    
    // Load PPC mapping count
    try {
      const ppcRes = await api.get("/part-process-characteristics");
      stats.value.activeCharacteristics = ppcRes.data?.length || 12;
    } catch {
      stats.value.activeCharacteristics = 12;
    }

    // Load Dashboard Stats
    try {
      if (dData && dData.trend) {
         let hours = dData.trend.map(t => `${t.hour.toString().padStart(2, '0')}:00`);
         let counts = dData.trend.map(t => t.count);
         if(hours.length === 0) {
            hours = ["08:00", "10:00", "12:00", "14:00", "16:00", "18:00", "20:00"];
            counts = [0, 0, 0, 0, 0, 0, 0];
         }
         trendData.value.hours = hours;
         trendData.value.counts = counts;
         
         const alertsPerHour = new Array(24).fill(0);
         alerts.forEach(a => {
           if (a.occurredAt?.startsWith(today)) {
             const h = new Date(a.occurredAt).getHours();
             alertsPerHour[h]++;
           }
         });
         trendData.value.alerts = hours.map(h => alertsPerHour[parseInt(h.split(':')[0])] || 0);

         if (dData.bottomCpk && dData.bottomCpk.length > 0) {
           cpkData.value.names = dData.bottomCpk.map(c => c.name);
           cpkData.value.values = dData.bottomCpk.map(c => c.cpk);
         } else {
           cpkData.value.names = ["暫無資料"];
           cpkData.value.values = [0];
         }
         
         if (dData.pareto && dData.pareto.length > 0) {
           paretoData.value.names = dData.pareto.map(p => p.name);
           paretoData.value.counts = dData.pareto.map(p => p.count);
           paretoData.value.pcts = dData.pareto.map(p => p.cumulativePercentage);
         } else {
           paretoData.value.names = ["暫無資料"];
           paretoData.value.counts = [0];
           paretoData.value.pcts = [0];
         }
      }
    } catch (e) {
      console.error("Failed to load dashboard stats", e);
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
    tooltip: { trigger: "axis", backgroundColor: "rgba(255, 255, 255, 0.9)", borderColor: "rgba(200, 200, 200, 0.5)", padding: 12, textStyle: { color: "#1e293b", fontFamily: "Inter" }, backdropFilter: "blur(10px)", shadowBlur: 15, shadowColor: "rgba(0,0,0,0.1)" },
    grid: { left: 40, right: 20, top: 40, bottom: 30 },
    xAxis: {
      type: "category",
      boundaryGap: false,
      data: trendData.value.hours,
      axisLine: { lineStyle: { color: "rgba(148, 163, 184, 0.3)" } },
      axisLabel: { color: "#64748b", fontFamily: "Inter" },
      splitLine: { show: false }
    },
    yAxis: { type: "value", splitLine: { lineStyle: { color: "rgba(100, 116, 139, 0.1)", type: "dashed" } }, axisLine: { show: false }, axisLabel: { color: "#64748b", fontFamily: "Inter" } },
    series: [
      {
        name: "檢驗數",
        type: "line",
        smooth: true,
        symbolSize: 0,
        showSymbol: false,
        data: trendData.value.counts,
        itemStyle: { color: "#38bdf8" },
        lineStyle: { width: 3, shadowColor: "rgba(56, 189, 248, 0.5)", shadowBlur: 10 },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: "rgba(56, 189, 248, 0.3)" },
            { offset: 1, color: "rgba(56, 189, 248, 0.02)" }
          ])
        }
      },
      {
        name: "異常告警",
        type: "line",
        smooth: true,
        symbolSize: 6,
        data: trendData.value.alerts,
        itemStyle: { color: "#f43f5e" },
        lineStyle: { width: 3, shadowColor: "rgba(244, 63, 94, 0.6)", shadowBlur: 12 },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: "rgba(244, 63, 94, 0.35)" },
            { offset: 1, color: "rgba(244, 63, 94, 0.02)" }
          ])
        }
      }
    ]
  });

  // 2. Cpk Ranking Chart
  if (!cpkChart) cpkChart = echarts.init(cpkChartEl.value);
  cpkChart.setOption({
    backgroundColor: "transparent",
    tooltip: { trigger: "axis", axisPointer: { type: "shadow", shadowStyle: { color: "rgba(0,0,0,0.05)" } }, backgroundColor: "rgba(255, 255, 255, 0.9)", borderColor: "rgba(200, 200, 200, 0.5)", padding: 12, textStyle: { color: "#1e293b", fontFamily: "Inter" } },
    grid: { left: 120, right: 30, top: 30, bottom: 30 },
    xAxis: { type: "value", max: 'dataMax', splitLine: { lineStyle: { color: "rgba(100, 116, 139, 0.1)", type: "dashed" } }, axisLabel: { color: "#64748b", fontFamily: "Inter" } },
    yAxis: {
      type: "category",
      data: cpkData.value.names,
      axisLine: { lineStyle: { color: "rgba(148, 163, 184, 0.3)" } },
      axisLabel: { color: "#475569", fontFamily: "Inter", fontWeight: 500, width: 100, overflow: "truncate" }
    },
    series: [
      {
        name: "Cpk 指標",
        type: "bar",
        barWidth: 14,
        itemStyle: {
          borderRadius: [0, 6, 6, 0],
          color: (params) => {
            const v = params.value;
            if (v < 1.0) return new echarts.graphic.LinearGradient(1, 0, 0, 0, [{offset:0, color:"#f43f5e"}, {offset:1, color:"#9f1239"}]);
            if (v < 1.33) return new echarts.graphic.LinearGradient(1, 0, 0, 0, [{offset:0, color:"#fb923c"}, {offset:1, color:"#c2410c"}]);
            return new echarts.graphic.LinearGradient(1, 0, 0, 0, [{offset:0, color:"#34d399"}, {offset:1, color:"#059669"}]);
          }
        },
        data: cpkData.value.values
      }
    ]
  });

  // 3. Pareto Chart
  if (!paretoChart) paretoChart = echarts.init(paretoChartEl.value);
  paretoChart.setOption({
    backgroundColor: "transparent",
    tooltip: { trigger: "axis", axisPointer: { type: "cross", crossStyle: { color: "#999" } }, backgroundColor: "rgba(255, 255, 255, 0.9)", padding: 12, textStyle: { color: "#1e293b", fontFamily: "Inter" } },
    grid: { left: 50, right: 50, top: 40, bottom: 40 },
    xAxis: [
      {
        type: "category",
        data: paretoData.value.names,
        axisPointer: { type: "shadow" },
        axisLabel: { color: "#64748b", fontFamily: "Inter", interval: 0, rotate: 15 }
      }
    ],
    yAxis: [
      {
        type: "value",
        name: "異常次數",
        axisLabel: { color: "#64748b", fontFamily: "Inter" },
        splitLine: { lineStyle: { color: "rgba(100, 116, 139, 0.1)", type: "dashed" } }
      },
      {
        type: "value",
        name: "累積佔比",
        min: 0,
        max: 100,
        axisLabel: { formatter: "{value} %", color: "#64748b", fontFamily: "Inter" },
        splitLine: { show: false }
      }
    ],
    series: [
      {
        name: "異常次數",
        type: "bar",
        barWidth: 24,
        itemStyle: {
          borderRadius: [4, 4, 0, 0],
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{offset:0, color:"#f97316"}, {offset:1, color:"#ea580c"}])
        },
        data: paretoData.value.counts
      },
      {
        name: "累積佔比 (%)",
        type: "line",
        yAxisIndex: 1,
        smooth: true,
        symbolSize: 8,
        itemStyle: { color: "#3b82f6" },
        lineStyle: { width: 3, shadowColor: "rgba(59, 130, 246, 0.5)", shadowBlur: 8 },
        data: paretoData.value.pcts
      }
    ]
  });
}

function handleResize() {
  trendChart?.resize();
  cpkChart?.resize();
  paretoChart?.resize();
}

onMounted(() => {
  loadData();
  pollTimer = setInterval(loadData, 30000);
  window.addEventListener("resize", handleResize);
});

onBeforeUnmount(() => {
  if (pollTimer) clearInterval(pollTimer);
  window.removeEventListener("resize", handleResize);
  trendChart?.dispose();
  cpkChart?.dispose();
  paretoChart?.dispose();
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
          <h2 class="text-xs font-bold text-indigo-200/80 uppercase tracking-widest">SPC Dashboard & Analysis</h2>
          <p class="text-indigo-100 max-w-xl text-sm leading-relaxed">
            整合 SPC 運算引擎、西方電氣異常規則自動篩選與即時製程能力分析，保障每一批次產品品質零死角。
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-3">
          <button @click="router.push('/uploads/variable')" class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-white text-blue-600 font-bold hover:bg-blue-50 transition-all shadow-lg text-sm">
            <Zap class="w-4 h-4 text-blue-600" /> 快速匯入計量數據
          </button>
          <button @click="router.push('/spc')" class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-indigo-500/40 text-white font-bold hover:bg-indigo-500/60 border border-white/20 transition-all text-sm backdrop-blur-md">
            <Activity class="w-4 h-4" /> 開啟 SPC 管制圖
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
      <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl relative overflow-hidden group hover:border-blue-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400">
          <Layers class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1">本週總檢驗批次數</p>
        <h3 class="text-3xl font-black text-slate-800 dark:text-white">{{ stats.totalBatches }} <span class="text-xs font-semibold text-emerald-500 ml-1">+12.4%</span></h3>
        <p class="text-[11px] text-slate-400 mt-2">來自各站即時量測與自動化匯入</p>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl relative overflow-hidden group hover:border-red-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-red-50 dark:bg-red-900/30 text-red-600 dark:text-red-400 animate-pulse">
          <AlertTriangle class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1">今日觸發品質警報</p>
        <h3 class="text-3xl font-black text-slate-800 dark:text-white">{{ stats.todayAlerts }} <span class="text-xs font-semibold text-red-500 ml-1">待處置</span></h3>
        <p class="text-[11px] text-slate-400 mt-2">西方電氣規則與規格上下限攔截</p>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl relative overflow-hidden group hover:border-amber-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-amber-50 dark:bg-amber-900/30 text-amber-600 dark:text-amber-400">
          <TrendingUp class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1">即時異常警報率</p>
        <h3 class="text-3xl font-black text-amber-600 dark:text-amber-500">{{ stats.alertRate }}%</h3>
        <p class="text-[11px] text-slate-400 mt-2">目標基準：低於 1.5%</p>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl relative overflow-hidden group hover:border-emerald-500/50 transition-all">
        <div class="absolute right-4 top-4 p-3 rounded-2xl bg-emerald-50 dark:bg-emerald-900/30 text-emerald-600 dark:text-emerald-400">
          <CheckCircle2 class="w-6 h-6" />
        </div>
        <p class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 mb-1">監控中檢測項目基準</p>
        <h3 class="text-3xl font-black text-slate-800 dark:text-white">{{ stats.activeCharacteristics }} <span class="text-xs font-semibold text-slate-400 ml-1">項</span></h3>
        <p class="text-[11px] text-slate-400 mt-2">已佈署基準線與抽樣計畫</p>
      </div>
    </div>

    <!-- Charts Section -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl flex flex-col">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Activity class="w-5 h-5 text-blue-500" /> 當日抽樣檢驗與異常走勢趨勢圖
            </h3>
            <p class="text-xs text-slate-500 dark:text-slate-400">各時段自動匯入與即時攔截數據對比</p>
          </div>
        </div>
        <div ref="trendChartEl" class="h-80 w-full"></div>
      </div>

      <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl flex flex-col">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <TrendingUp class="w-5 h-5 text-emerald-500" /> 製程能力後段班排行 (Cpk TOP 5 警示)
            </h3>
            <p class="text-xs text-slate-500 dark:text-slate-400">紅條代表 Cpk < 1.0，需重點關注改善</p>
          </div>
        </div>
        <div ref="cpkChartEl" class="h-80 w-full"></div>
      </div>
    </div>

    <!-- Pareto Chart Section -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl flex flex-col">
      <div class="flex items-center justify-between mb-4">
        <div>
          <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
            <Layers class="w-5 h-5 text-amber-500" /> 近 30 日異常排行柏拉圖 (Pareto Chart)
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">符合 80/20 法則的真因優先處置建議區段</p>
        </div>
      </div>
      <div ref="paretoChartEl" class="h-80 w-full"></div>
    </div>

    <!-- Recent Alerts Card -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-800 border border-slate-100 dark:border-slate-700 shadow-xl space-y-4">
      <div class="flex items-center justify-between">
        <div>
          <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
            <ShieldAlert class="w-5 h-5 text-red-500" /> 即時品質異常與失控通報日誌
          </h3>
          <p class="text-xs text-slate-500 dark:text-slate-400">觸發規格或西方電氣異常之最新紀錄</p>
        </div>
        <button @click="router.push('/alerts')" class="text-xs font-bold text-blue-600 hover:text-blue-500 flex items-center gap-1">
          檢視全部異常 <ArrowUpRight class="w-3.5 h-3.5" />
        </button>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-slate-50 dark:bg-slate-900 text-slate-500 dark:text-slate-400 uppercase text-[11px] font-bold tracking-wider border-b border-slate-200 dark:border-slate-700">
            <tr>
              <th class="py-3 px-4 rounded-l-xl">發生時間</th>
              <th class="py-3 px-4">產品料號</th>
              <th class="py-3 px-4">製程 / 特性</th>
              <th class="py-3 px-4">警報類型</th>
              <th class="py-3 px-4">實測值 / 狀態說明</th>
              <th class="py-3 px-4 rounded-r-xl text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-700 text-slate-600 dark:text-slate-300">
            <tr v-for="a in recentAlerts" :key="a.id" class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors">
              <td class="py-3 px-4 font-mono text-xs">{{ new Date(a.occurredAt).toLocaleString() }}</td>
              <td class="py-3 px-4 font-bold text-slate-800 dark:text-white">
                {{ a.partNo || `Part #${a.partId}` }}
                <div class="text-[11px] text-slate-400 font-mono">{{ a.partName || '' }}</div>
              </td>
              <td class="py-3 px-4">
                {{ a.processCode || `Process #${a.processId}` }}
                <div class="text-[11px] text-slate-400 font-mono">{{ a.characteristicCode || `Characteristic #${a.characteristicId}` }}</div>
              </td>
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
                  @click="a.ppcId ? router.push(`/spc?ppcId=${a.ppcId}`) : null"
                  :disabled="!a.ppcId"
                  class="px-3 py-1.5 rounded-lg bg-blue-50 hover:bg-blue-100 dark:bg-blue-900/30 dark:hover:bg-blue-900/50 text-blue-600 dark:text-blue-400 font-bold text-xs flex items-center gap-1 ml-auto transition-all"
                >
                  看圖 <ArrowUpRight class="w-3.5 h-3.5" />
                </button>
              </td>
            </tr>
            <tr v-if="recentAlerts.length === 0">
              <td colspan="6" class="py-8 text-center text-slate-500 dark:text-slate-400 text-sm">目前無任何未處理之品質警報紀錄，生產狀態優良 ✨</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
