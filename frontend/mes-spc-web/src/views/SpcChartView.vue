<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
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
  Download,
  User,
  Calendar,
  Hash,
  Clock
} from "lucide-vue-next";

const route = useRoute();
const router = useRouter();

const mappings = ref([]);
const selectedMappingId = ref("");
const batchId = ref("");
const loading = ref(false);
const error = ref("");

// Cascading Selectors State
const selectedPartId = ref("");
const selectedProcessId = ref("");
const selectedCharacteristicId = ref("");
const updatingCascades = ref(false);

// Autocomplete Search State
const searchQuery = ref("");
const showSearchResults = ref(false);

const chartResult = ref(null);
let chartInstance = null;
const chartEl = ref(null);

// Click Drill-down State
const selectedPoint = ref(null);
const selectedPointIndex = ref(-1);

// Unique Parts for selection
const uniqueParts = computed(() => {
  const seen = new Set();
  const list = [];
  mappings.value.forEach(m => {
    if (m.part && !seen.has(m.part.id)) {
      seen.add(m.part.id);
      list.push({ id: m.partId, partNo: m.part.partNo, partName: m.part.partName });
    }
  });
  return list;
});

// Processes available for selected Part
const availableProcesses = computed(() => {
  if (!selectedPartId.value) return [];
  const seen = new Set();
  const list = [];
  mappings.value.forEach(m => {
    if (m.partId === Number(selectedPartId.value) && m.process && !seen.has(m.process.id)) {
      seen.add(m.process.id);
      list.push({ id: m.processId, processCode: m.process.processCode, processName: m.process.processName });
    }
  });
  return list;
});

// Characteristics available for selected Part + Process
const availableCharacteristics = computed(() => {
  if (!selectedPartId.value || !selectedProcessId.value) return [];
  const seen = new Set();
  const list = [];
  mappings.value.forEach(m => {
    if (m.partId === Number(selectedPartId.value) && m.processId === Number(selectedProcessId.value) && m.characteristic && !seen.has(m.characteristic.id)) {
      seen.add(m.characteristic.id);
      list.push({ id: m.characteristicId, characteristicCode: m.characteristic.characteristicCode, characteristicName: m.characteristic.characteristicName });
    }
  });
  return list;
});

// Autocomplete search filtering
const filteredMappings = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return [];
  return mappings.value.filter(m => {
    return (
      (m.part?.partNo || "").toLowerCase().includes(query) ||
      (m.part?.partName || "").toLowerCase().includes(query) ||
      (m.process?.processName || "").toLowerCase().includes(query) ||
      (m.characteristic?.characteristicName || "").toLowerCase().includes(query)
    );
  }).slice(0, 15);
});

// Sync cascading dropdown selectors with selected mapping ID
function syncCascadingDropdowns(mappingId) {
  const m = mappings.value.find(x => x.id === mappingId);
  if (m) {
    updatingCascades.value = true;
    selectedPartId.value = m.partId;
    selectedProcessId.value = m.processId;
    selectedCharacteristicId.value = m.characteristicId;
    selectedMappingId.value = m.id;
    searchQuery.value = `[${m.part?.partNo}] ${m.process?.processName} - ${m.characteristic?.characteristicName}`;
    updatingCascades.value = false;
  }
}

// Watchers for cascading select workflow
watch(selectedPartId, () => {
  if (updatingCascades.value) return;
  selectedProcessId.value = "";
  selectedCharacteristicId.value = "";
  selectedMappingId.value = "";
});

watch(selectedProcessId, () => {
  if (updatingCascades.value) return;
  selectedCharacteristicId.value = "";
  selectedMappingId.value = "";
});

watch(selectedCharacteristicId, (newVal) => {
  if (updatingCascades.value) return;
  if (newVal) {
    const match = mappings.value.find(m => 
      m.partId === Number(selectedPartId.value) &&
      m.processId === Number(selectedProcessId.value) &&
      m.characteristicId === Number(selectedCharacteristicId.value)
    );
    if (match) {
      selectedMappingId.value = match.id;
      searchQuery.value = `[${match.part?.partNo}] ${match.process?.processName} - ${match.characteristic?.characteristicName}`;
    }
  } else {
    selectedMappingId.value = "";
  }
});

function selectMappingFromSearch(m) {
  syncCascadingDropdowns(m.id);
  showSearchResults.value = false;
}

function hideSearchResults() {
  setTimeout(() => {
    showSearchResults.value = false;
  }, 200);
}

async function loadMappings() {
  try {
    const res = await api.get("/part-process-characteristics");
    mappings.value = res.data || [];
    
    // Auto-select from query or first item
    const qId = Number(route.query.partProcessCharacteristicId);
    if (qId && mappings.value.some(m => m.id === qId)) {
      syncCascadingDropdowns(qId);
    } else if (mappings.value.length > 0) {
      syncCascadingDropdowns(mappings.value[0].id);
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
  selectedPoint.value = null;
  selectedPointIndex.value = -1;

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

  // Calculate standard deviation (sigma) based on control limits
  const cl = limits.cl;
  const ucl = limits.ucl;
  const lcl = limits.lcl;
  const sigma = (cl != null && ucl != null && ucl > cl) ? (ucl - cl) / 3 : null;

  // Build Zone A/B/C markArea bands
  let markAreaTop = undefined;
  if (cl != null && sigma != null && sigma > 0) {
    const areaStyle = (colorName, labelName) => {
      return {
        itemStyle: { color: colorName },
        label: {
          show: true,
          position: 'insideLeft',
          formatter: labelName,
          color: 'rgba(100, 116, 139, 0.45)',
          fontSize: 9,
          fontWeight: 'bold'
        }
      };
    };
    markAreaTop = {
      silent: true,
      data: [
        [
          Object.assign({ yAxis: cl + 2 * sigma }, areaStyle('rgba(239, 68, 68, 0.035)', 'Zone A (3σ)')),
          { yAxis: ucl || (cl + 3 * sigma) }
        ],
        [
          Object.assign({ yAxis: cl + 1 * sigma }, areaStyle('rgba(245, 158, 11, 0.035)', 'Zone B (2σ)')),
          { yAxis: cl + 2 * sigma }
        ],
        [
          Object.assign({ yAxis: cl }, areaStyle('rgba(16, 185, 129, 0.025)', 'Zone C (1σ)')),
          { yAxis: cl + 1 * sigma }
        ],
        [
          Object.assign({ yAxis: cl - 1 * sigma }, areaStyle('rgba(16, 185, 129, 0.025)', 'Zone C (1σ)')),
          { yAxis: cl }
        ],
        [
          Object.assign({ yAxis: cl - 2 * sigma }, areaStyle('rgba(245, 158, 11, 0.035)', 'Zone B (2σ)')),
          { yAxis: cl - 1 * sigma }
        ],
        [
          Object.assign({ yAxis: lcl || (cl - 3 * sigma) }, areaStyle('rgba(239, 68, 68, 0.035)', 'Zone A (3σ)')),
          { yAxis: cl - 2 * sigma }
        ]
      ]
    };
  }

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
          { name: type === "XBAR_R" ? "Xbar" : "Individual", type: "line", xAxisIndex: 0, yAxisIndex: 0, data: seriesTopData, showSymbol: true, markLine: topMarkLineObj, markArea: markAreaTop, smooth: true },
          { name: type === "XBAR_R" ? "Range" : "Moving Range", type: "line", xAxisIndex: 1, yAxisIndex: 1, data: seriesBottomData, showSymbol: true, markLine: bottomMarkLineObj, smooth: true }
        ]
      : [{ name: type, type: "line", data: seriesTopData, showSymbol: true, markLine: topMarkLineObj, markArea: markAreaTop, smooth: true }]
  };

  chartInstance.setOption(option);

  // Attach Point Click listener for Drill-down cards
  chartInstance.on("click", (params) => {
    if (params.componentType === "series") {
      const idx = params.dataIndex;
      selectedPointIndex.value = idx;
      if (params.seriesIndex === 0) {
        selectedPoint.value = pointsTop[idx];
      } else {
        selectedPoint.value = pointsBottom[idx];
      }
    }
  });

  // Auto highlight point matching batchId / lotNo
  if (route.query.batchId || batchId.value) {
    const targetLot = route.query.batchId || batchId.value;
    const matchIdx = pointsTop.findIndex(p => p.lotNo === targetLot);
    if (matchIdx !== -1) {
      setTimeout(() => {
        chartInstance.dispatchAction({
          type: "showTip",
          seriesIndex: 0,
          dataIndex: matchIdx
        });
        selectedPoint.value = pointsTop[matchIdx];
        selectedPointIndex.value = matchIdx;
      }, 300);
    }
  }
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

      <div class="flex flex-wrap items-center gap-4 w-full xl:w-auto">
        <!-- Fuzzy Autocomplete Search Box -->
        <div class="relative w-full md:w-72">
          <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">🔍 快速搜尋檢驗基準</label>
          <div class="relative">
            <input v-model="searchQuery" @focus="showSearchResults = true" @blur="hideSearchResults" placeholder="輸入料號、工站、關鍵字..." class="w-full pl-9 pr-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
            <Search class="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
          </div>
          <!-- Search Dropdown list -->
          <div v-if="showSearchResults && filteredMappings.length > 0" class="absolute left-0 right-0 mt-1 max-h-60 overflow-y-auto bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-lg z-50 py-1">
            <button v-for="m in filteredMappings" :key="m.id" @mousedown="selectMappingFromSearch(m)" class="w-full text-left px-4 py-2 hover:bg-blue-50 dark:hover:bg-slate-700 text-xs text-slate-700 dark:text-slate-200 font-semibold border-b border-slate-100 dark:border-slate-700 last:border-0">
              <span class="text-blue-600 dark:text-blue-400 font-bold">[{{ m.part?.partNo }}]</span> {{ m.process?.processName }} - <span class="text-indigo-600 dark:text-indigo-400">{{ m.characteristic?.characteristicName }}</span>
            </button>
          </div>
        </div>

        <!-- 3-Level Cascading selectors -->
        <div class="flex flex-wrap gap-2 items-center w-full md:w-auto">
          <div class="w-40">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">料號 (Part)</label>
            <select v-model="selectedPartId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
              <option value="">選擇產品...</option>
              <option v-for="p in uniqueParts" :key="p.id" :value="p.id">[{{ p.partNo }}] {{ p.partName }}</option>
            </select>
          </div>

          <div class="w-40">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">工站 (Process)</label>
            <select v-model="selectedProcessId" :disabled="!selectedPartId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
              <option value="">選擇工站...</option>
              <option v-for="pr in availableProcesses" :key="pr.id" :value="pr.id">{{ pr.processName }}</option>
            </select>
          </div>

          <div class="w-44">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">特性項目 (Characteristic)</label>
            <select v-model="selectedCharacteristicId" :disabled="!selectedProcessId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
              <option value="">選擇檢驗項目...</option>
              <option v-for="c in availableCharacteristics" :key="c.id" :value="c.id">{{ c.characteristicName }}</option>
            </select>
          </div>
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

        <!-- Point Detail Drilldown Card -->
        <div v-if="selectedPoint" class="mt-6 p-5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 shadow-inner transition-all duration-300">
          <div class="flex items-center justify-between border-b border-slate-200 dark:border-slate-700 pb-3 mb-4">
            <h4 class="text-sm font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <span class="inline-block w-2.5 h-2.5 rounded-full bg-blue-500 animate-pulse"></span>
              點位品質追溯詳細資料 (點位 #{{ selectedPointIndex + 1 }})
            </h4>
            <div class="flex gap-2">
              <span v-if="selectedPoint.outOfSpec" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-red-100 text-red-700 dark:bg-red-950/50 dark:text-red-400 border border-red-200 dark:border-red-900/50">
                OOS 超出規格
              </span>
              <span v-if="selectedPoint.outOfControl" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400 border border-amber-200 dark:border-amber-900/50">
                OOC 管制失控
              </span>
              <span v-if="!selectedPoint.outOfSpec && !selectedPoint.outOfControl" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-900/50">
                正常 (In Control)
              </span>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div class="flex items-start gap-2.5">
              <Hash class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">生產批號 (Lot No)</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200 select-all">{{ selectedPoint.lotNo || '無批號資料' }}</p>
              </div>
            </div>

            <div v-if="selectedPoint.serialNo" class="flex items-start gap-2.5">
              <Sparkles class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">序號 (Serial No)</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200 select-all">{{ selectedPoint.serialNo || '無序號資料' }}</p>
              </div>
            </div>

            <div class="flex items-start gap-2.5">
              <User class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">作業人員 (Operator)</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200">{{ selectedPoint.operator || selectedPoint.Operator || '系統自動匯入' }}</p>
              </div>
            </div>

            <div class="flex items-start gap-2.5">
              <Clock class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">量測時間 (Measured At)</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200">
                  {{ selectedPoint.measuredAt ? new Date(selectedPoint.measuredAt).toLocaleString('zh-TW', { hour12: false }) : '無時間資料' }}
                </p>
              </div>
            </div>
          </div>

          <!-- Bottom detailed measurements & violation rules -->
          <div class="mt-4 pt-4 border-t border-slate-200 dark:border-slate-700 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
            <div>
              <p class="text-[10px] font-bold text-slate-400 uppercase mb-1">量測數據 (Measured Value)</p>
              <h3 class="text-xl font-black text-slate-800 dark:text-white">
                {{ selectedPoint.value !== undefined ? Number(selectedPoint.value).toFixed(4) : (selectedPoint.xbar !== undefined ? Number(selectedPoint.xbar).toFixed(4) : 'N/A') }}
                <span v-if="selectedPoint.range !== undefined" class="text-sm text-slate-400 font-semibold ml-3">
                  (子組全距 R = {{ Number(selectedPoint.range).toFixed(4) }}, 子組大小 n = {{ selectedPoint.n }})
                </span>
              </h3>
            </div>

            <div v-if="selectedPoint.violatedRules?.length > 0" class="w-full md:w-auto">
              <p class="text-[10px] font-bold text-red-500 uppercase mb-1 flex items-center gap-1">
                <AlertTriangle class="w-3.5 h-3.5" /> 觸發西方電氣判讀規則
              </p>
              <div class="flex flex-wrap gap-1.5">
                <span v-for="(rule, rIdx) in selectedPoint.violatedRules" :key="rIdx" class="px-2.5 py-1 rounded bg-red-500/10 text-red-600 dark:text-red-400 border border-red-200 dark:border-red-900/50 text-[11px] font-bold">
                  {{ rule }}
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
