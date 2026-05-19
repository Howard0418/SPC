<script setup>
import { ref, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import * as echarts from "echarts";
import { api, getApiErrorMessage } from "../api/client";
import {
  LineChart,
  Sliders,
  Activity,
  Layers,
  Sparkles,
  ShieldAlert,
  Search,
  RefreshCw,
  Info,
  CheckCircle2,
  AlertTriangle,
  Download
} from "lucide-vue-next";

const route = useRoute();
const router = useRouter();

const mappings = ref([]);
const selectedMappingId = ref("");
const batchId = ref("");
const loading = ref(false);
const error = ref("");

const chartResult = ref(null);
let chartInstance = null;
const chartEl = ref(null);

async function loadMappings() {
  try {
    const res = await api.get("/part-process-characteristics");
    mappings.value = res.data || [];
    
    // Auto-select from query or first item
    const qId = Number(route.query.partProcessCharacteristicId);
    if (qId && mappings.value.some(m => m.id === qId)) {
      selectedMappingId.value = qId;
    } else if (mappings.value.length > 0) {
      selectedMappingId.value = mappings.value[0].id;
    }
  } catch (e) {
    error.value = "無法載入檢驗項目基準列表：" + getApiErrorMessage(e);
  }
}

onMounted(() => {
  if (route.query.batchId) {
    batchId.value = route.query.batchId;
  }
  loadMappings();
  window.addEventListener("resize", handleResize);
});

watch(selectedMappingId, (newVal) => {
  if (newVal) loadInteractiveChart();
});

watch(batchId, () => {
  if (selectedMappingId.value) loadInteractiveChart();
});

async function loadInteractiveChart() {
  if (!selectedMappingId.value) return;
  loading.value = true;
  error.value = "";
  chartResult.value = null;

  try {
    const params = { partProcessCharacteristicId: selectedMappingId.value };
    if (batchId.value) params.uploadBatchId = batchId.value;

    const res = await api.get("/v2/spc/interactive-chart", { params });
    chartResult.value = res.data;
    loading.value = false;
    await nextTick();
    renderECharts();
  } catch (e) {
    if (e?.response?.status === 404) {
      error.value = "找不到該檢驗項目的管制圖資料，可能尚無量測數據或尚未配置管制圖。";
    } else {
      error.value = getApiErrorMessage(e);
    }
  } finally {
    loading.value = false;
  }
}

function renderECharts() {
  if (!chartEl.value || !chartResult.value) return;

  if (chartInstance) {
    chartInstance.dispose();
    chartInstance = null;
  }

  chartInstance = echarts.init(chartEl.value);

  const data = chartResult.value;
  const type = data.chartType?.toUpperCase() || "";
  const limits = data.limits || {};
  const isDual = type === "XBAR_R" || type === "I-MR" || type === "I_MR";

  // Build spec & control marklines
  const markLinesTop = [];
  const addLine = (y, name, color, style = "solid", width = 1.5) => {
    if (y == null || Number.isNaN(y)) return;
    markLinesTop.push({
      name,
      yAxis: y,
      lineStyle: { color, width, type: style },
      label: { formatter: `${name}: ${Number(y).toFixed(3)}`, color, position: "end" }
    });
  };

  // Specs
  addLine(limits.usl, "USL", "#ef4444", "dashed", 2);
  addLine(limits.lsl, "LSL", "#ef4444", "dashed", 2);
  addLine(limits.target, "Target", "#10b981", "solid", 2);

  // Control limits
  addLine(limits.ucl, "UCL", "#f59e0b", "solid", 1.5);
  addLine(limits.cl, "CL", "#3b82f6", "solid", 1.5);
  addLine(limits.lcl, "LCL", "#f59e0b", "solid", 1.5);

  const topMarkLineObj = markLinesTop.length > 0 ? { symbol: "none", data: markLinesTop, animation: false } : undefined;

  // Bottom marklines
  const markLinesBottom = [];
  const stat = data.statControlLimits || {};
  let bottomUcl = null, bottomCl = null, bottomLcl = null;
  if (type === "XBAR_R") {
    bottomUcl = stat.rControl?.ucl;
    bottomCl = stat.rControl?.cl;
    bottomLcl = stat.rControl?.lcl;
  } else if (type === "I-MR" || type === "I_MR") {
    bottomUcl = stat.mrControlLimitsStat?.ucl;
    bottomCl = stat.mrControlLimitsStat?.cl;
    bottomLcl = stat.mrControlLimitsStat?.lcl;
  }
  const addLineB = (y, name, color) => {
    if (y == null || Number.isNaN(y)) return;
    markLinesBottom.push({
      name,
      yAxis: y,
      lineStyle: { color, width: 1.5, type: "dashed" },
      label: { formatter: `${name}: ${Number(y).toFixed(3)}`, color, position: "end" }
    });
  };
  addLineB(bottomUcl, "UCL(R/MR)", "#f59e0b");
  addLineB(bottomCl, "CL(R/MR)", "#3b82f6");
  addLineB(bottomLcl, "LCL(R/MR)", "#f59e0b");

  const bottomMarkLineObj = markLinesBottom.length > 0 ? { symbol: "none", data: markLinesBottom, animation: false } : undefined;

  const pointsTop = data.chartData?.points || [];
  const pointsBottom = data.secondaryChartData?.points || [];

  const labels = pointsTop.map((p, i) => {
    if (p.measuredAt) {
      return new Date(p.measuredAt).toLocaleString('zh-TW', { 
        month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false 
      });
    }
    return `P#${i + 1}`;
  });

  const seriesTopData = pointsTop.map(p => {
    const isErr = p.outOfSpec || p.outOfControl || (p.violatedRules && p.violatedRules.length > 0);
    return {
      value: p.value !== undefined ? p.value : p.xbar !== undefined ? p.xbar : null,
      itemStyle: { color: isErr ? "#ef4444" : "#3b82f6", borderColor: isErr ? "#991b1b" : "#2563eb", borderWidth: 2 },
      symbolSize: isErr ? 12 : 8,
      violatedRules: p.violatedRules
    };
  });

  const seriesBottomData = pointsBottom.map(p => {
    const isErr = p.outOfControl;
    return {
      value: p.value !== undefined ? p.value : null,
      itemStyle: { color: isErr ? "#ef4444" : "#64748b" },
      symbolSize: isErr ? 10 : 6
    };
  });

  const option = {
    backgroundColor: "transparent",
    tooltip: {
      trigger: "axis",
      backgroundColor: "rgba(15, 23, 42, 0.95)",
      borderColor: "#334155",
      textStyle: { color: "#fff", fontSize: 12 },
      formatter: (params) => {
        let res = `<div class="font-bold border-b border-slate-700 pb-1 mb-1">${params[0].axisValue}</div>`;
        params.forEach(p => {
          res += `<div><span class="inline-block w-2 h-2 rounded-full mr-1" style="background-color:${p.color}"></span> ${p.seriesName}: <strong>${Number(p.data?.value).toFixed(4)}</strong></div>`;
          if (p.data?.violatedRules?.length > 0) {
            res += `<div class="mt-1.5 px-2 py-0.5 rounded bg-red-900/50 border border-red-500/50 text-red-300 text-[11px] font-bold">⚠️ 西方電氣規則違規：<br>${p.data.violatedRules.join("<br>")}</div>`;
          }
        });
        return res;
      }
    },
    toolbox: {
      feature: {
        dataZoom: { yAxisIndex: "none" },
        restore: {},
        saveAsImage: { name: `SPC_${type}_Chart` }
      },
      iconStyle: { borderColor: "#64748b" }
    },
    dataZoom: [
      { type: "slider", show: true, xAxisIndex: isDual ? [0, 1] : [0], bottom: 10, borderColor: "#334155", textStyle: { color: "#64748b" } },
      { type: "inside", xAxisIndex: isDual ? [0, 1] : [0] }
    ],
    grid: isDual
      ? [
          { left: 60, right: 80, top: 40, height: "42%" },
          { left: 60, right: 80, top: "56%", height: "30%" }
        ]
      : [{ left: 60, right: 80, top: 40, bottom: 60 }],
    xAxis: isDual
      ? [
          { type: "category", data: labels, boundaryGap: false, gridIndex: 0, axisLine: { lineStyle: { color: "#64748b" } } },
          { type: "category", data: labels, boundaryGap: false, gridIndex: 1, axisLine: { lineStyle: { color: "#64748b" } } }
        ]
      : [{ type: "category", data: labels, boundaryGap: false, axisLine: { lineStyle: { color: "#64748b" } } }],
    yAxis: isDual
      ? [
          { type: "value", gridIndex: 0, name: type === "XBAR_R" ? "Xbar 平均值" : "單值 (I)", splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, scale: true },
          { type: "value", gridIndex: 1, name: type === "XBAR_R" ? "全距 (R)" : "移動全距 (MR)", splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, scale: true }
        ]
      : [{ type: "value", name: `${type} 數值`, splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, scale: true }],
    series: isDual
      ? [
          { name: type === "XBAR_R" ? "Xbar" : "Individual", type: "line", xAxisIndex: 0, yAxisIndex: 0, data: seriesTopData, showSymbol: true, markLine: topMarkLineObj, smooth: true },
          { name: type === "XBAR_R" ? "Range" : "Moving Range", type: "line", xAxisIndex: 1, yAxisIndex: 1, data: seriesBottomData, showSymbol: true, markLine: bottomMarkLineObj, smooth: true }
        ]
      : [{ name: type, type: "line", data: seriesTopData, showSymbol: true, markLine: topMarkLineObj, smooth: true }]
  };

  chartInstance.setOption(option);
}

function handleResize() {
  chartInstance?.resize();
}

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);
  chartInstance?.dispose();
  chartInstance = null;
});
</script>

<template>
  <div class="space-y-6">
    <!-- Header Controls -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-blue-600 dark:text-blue-400 mb-1">
          <Activity class="w-4 h-4 animate-spin" /> 工業品質分析空間
        </div>
        <h1 class="text-2xl font-black text-slate-800 dark:text-white">SPC 即時互動管制圖戰情室</h1>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <div class="w-72">
          <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">選擇料號製程檢驗項目</label>
          <select v-model="selectedMappingId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
            <option v-for="m in mappings" :key="m.id" :value="m.id">
              [{{ m.part?.partNo }}] {{ m.process?.processName }} - {{ m.characteristic?.characteristicName }}
            </option>
          </select>
        </div>

        <div class="w-48">
          <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">指定匯入批次過濾 (可選)</label>
          <input v-model="batchId" placeholder="輸入 Batch UUID..." class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
        </div>

        <button @click="loadInteractiveChart" :disabled="loading" class="mt-4 flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold shadow-lg shadow-blue-500/20 disabled:opacity-50 transition-all text-sm h-10">
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" /> 重新計算
        </button>
      </div>
    </div>

    <!-- Error Display -->
    <div v-if="error" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ error }}
    </div>

    <div v-if="loading && !chartResult" class="h-96 flex flex-col items-center justify-center space-y-3 text-slate-400">
      <Activity class="w-10 h-10 animate-bounce text-blue-500" />
      <p class="text-sm font-bold">正在執行西方電氣規則判定與管制圖引擎運算...</p>
    </div>

    <!-- Main Chart & Capability Workspace -->
    <div v-if="chartResult && !loading" class="space-y-6">
      <!-- Capability Summary Cards -->
      <div v-if="chartResult.capability" class="grid grid-cols-2 md:grid-cols-6 gap-4">
        <div class="p-4 rounded-2xl bg-gradient-to-tr from-slate-900 to-slate-800 text-white border border-slate-700 shadow-md">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400">Cp (規格寬度比)</p>
          <h3 class="text-2xl font-black mt-1" :class="chartResult.capability.cp >= 1.33 ? 'text-emerald-400' : 'text-amber-400'">
            {{ chartResult.capability.cp !== null ? chartResult.capability.cp : 'N/A' }}
          </h3>
        </div>

        <div class="p-4 rounded-2xl bg-gradient-to-tr from-slate-900 to-slate-800 text-white border border-slate-700 shadow-md">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400">Cpk (製程能力指標)</p>
          <h3 class="text-2xl font-black mt-1" :class="chartResult.capability.cpk >= 1.33 ? 'text-emerald-400' : chartResult.capability.cpk < 1.0 ? 'text-red-400 animate-pulse' : 'text-amber-400'">
            {{ chartResult.capability.cpk !== null ? chartResult.capability.cpk : 'N/A' }}
          </h3>
        </div>

        <div class="p-4 rounded-2xl bg-gradient-to-tr from-slate-900 to-slate-800 text-white border border-slate-700 shadow-md">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400">Pp (長期性能比)</p>
          <h3 class="text-2xl font-black mt-1 text-blue-400">
            {{ chartResult.capability.pp !== null ? chartResult.capability.pp : 'N/A' }}
          </h3>
        </div>

        <div class="p-4 rounded-2xl bg-gradient-to-tr from-slate-900 to-slate-800 text-white border border-slate-700 shadow-md">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400">Ppk (長期能力指標)</p>
          <h3 class="text-2xl font-black mt-1 text-cyan-400">
            {{ chartResult.capability.ppk !== null ? chartResult.capability.ppk : 'N/A' }}
          </h3>
        </div>

        <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">組內標準差 (σ_within)</p>
          <h3 class="text-xl font-bold mt-1 text-slate-800 dark:text-white">
            {{ chartResult.capability.sigmaWithin !== null ? Number(chartResult.capability.sigmaWithin).toFixed(4) : 'N/A' }}
          </h3>
        </div>

        <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">整體標準差 (σ_overall)</p>
          <h3 class="text-xl font-bold mt-1 text-slate-800 dark:text-white">
            {{ chartResult.capability.sigmaOverall !== null ? Number(chartResult.capability.sigmaOverall).toFixed(4) : 'N/A' }}
          </h3>
        </div>
      </div>

      <!-- Chart Display Box -->
      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl relative">
        <div class="flex flex-wrap items-center justify-between gap-4 border-b border-slate-100 dark:border-slate-800 pb-4 mb-4">
          <div class="flex items-center gap-3">
            <span class="px-3 py-1 rounded-full bg-blue-600 text-white text-xs font-bold tracking-widest uppercase shadow-md shadow-blue-500/20">
              {{ chartResult.chartType }} CHART
            </span>
            <span class="text-sm font-bold text-slate-600 dark:text-slate-300">
              子組大小 (Subgroup N) = {{ chartResult.subgroupSize }}
            </span>
            <span v-if="chartResult.statControlLimits?.xbarControl?.calculationMethod === 'MR_METHOD'" class="px-2.5 py-1 rounded-full bg-purple-100 text-purple-700 dark:bg-purple-950/80 dark:text-purple-300 border border-purple-300 dark:border-purple-800 text-xs font-bold tracking-wide flex items-center gap-1.5 animate-pulse shadow-sm">
              🧪 截圖公式：平均值移動全距法 (UCL = Xbar + 2.66 * MRbar)
            </span>
            <span v-else-if="chartResult.statControlLimits?.xbarControl?.calculationMethod === 'SIGMA_METHOD'" class="px-2.5 py-1 rounded-full bg-emerald-100 text-emerald-700 dark:bg-emerald-950/80 dark:text-emerald-300 border border-emerald-300 dark:border-emerald-800 text-xs font-bold tracking-wide flex items-center gap-1.5 animate-pulse shadow-sm">
              🧪 樣本標準差法 (UCL = Xbar + 3 * S_Xbar)
            </span>
            <span v-else class="px-2.5 py-1 rounded-full bg-blue-100 text-blue-700 dark:bg-blue-950/80 dark:text-blue-300 border border-blue-300 dark:border-blue-800 text-xs font-bold tracking-wide flex items-center gap-1.5 shadow-sm">
              ✨ 預設公式：標準全距法 (UCL = Xbar + A2 * Rbar)
            </span>
          </div>

          <div class="flex items-center gap-4 text-xs font-semibold text-slate-500">
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-red-500 inline-block"></span> 規格/管制界限失控點</span>
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-blue-500 inline-block"></span> 正常管制點位</span>
          </div>
        </div>

        <div v-if="chartResult.subgroupSizeNote" class="mb-4 p-3 rounded-xl bg-amber-500/10 border border-amber-500/30 text-amber-600 dark:text-amber-400 text-xs flex items-center gap-2">
          <AlertTriangle class="w-4 h-4 flex-shrink-0" /> {{ chartResult.subgroupSizeNote }}
        </div>

        <div ref="chartEl" class="h-[600px] w-full min-h-[450px]"></div>
      </div>
    </div>
  </div>
</template>
