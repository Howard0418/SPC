<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { useRoute } from "vue-router";
import * as echarts from "echarts";
import { api, getApiErrorMessage } from "../api/client";
import {
  TrendingUp,
  Search,
  RefreshCw,
  AlertTriangle,
  Activity,
  ShieldAlert,
  Info,
  Sliders
} from "lucide-vue-next";

const route = useRoute();

// Master data
const mappings = ref([]);
const chartTypes = ref([]);
const categories = ref([]);
const groups = ref([]);

// Selection state
const selectedDimension = ref("PROC");
const selectedPartId = ref("");
const selectedProcessId = ref("");
const selectedCharacteristicId = ref("");
const updatingCascades = ref(false);

// Autocomplete search
const searchQuery = ref("");
const showSearchResults = ref(false);

// Chart state
const loading = ref(false);
const error = ref("");
const chartResult = ref(null);
const trendChartEl = ref(null);
let trendChartInstance = null;

// ─── Dimension helpers ───────────────────────────────────────
const getDimensionForMapping = (m) => {
  if (!m.chartTypeId) {
    return m.part?.partNo === "COMMON" ? "PROC" : "PROD";
  }
  const type = chartTypes.value.find(t => t.id === m.chartTypeId);
  if (!type) return m.part?.partNo === "COMMON" ? "PROC" : "PROD";
  const cat = categories.value.find(c => c.id === type.chartCategoryId);
  if (!cat) return m.part?.partNo === "COMMON" ? "PROC" : "PROD";
  const group = groups.value.find(g => g.id === cat.chartGroupId);
  return group?.groupCode || (m.part?.partNo === "COMMON" ? "PROC" : "PROD");
};

const filteredMappingsByDimension = computed(() =>
  mappings.value.filter(m => m.isEnabled && getDimensionForMapping(m) === selectedDimension.value)
);

const uniqueParts = computed(() => {
  const byId = new Map();
  filteredMappingsByDimension.value
    .filter(m => m.part?.isEnabled !== false)
    .forEach(m => { if (!byId.has(m.partId)) byId.set(m.partId, m.part); });
  return [...byId.entries()]
    .map(([id, part]) => ({ ...part, id }))
    .sort((a, b) => (a.partNo || "").localeCompare(b.partNo || ""));
});

const availableProcesses = computed(() => {
  if (!selectedPartId.value) return [];
  const byId = new Map();
  filteredMappingsByDimension.value
    .filter(m => m.partId === Number(selectedPartId.value) && m.process?.isEnabled !== false)
    .forEach(m => { if (!byId.has(m.processId)) byId.set(m.processId, m.process); });
  return [...byId.entries()]
    .map(([id, process]) => ({ ...process, id }))
    .sort((a, b) => (a.processCode || "").localeCompare(b.processCode || ""));
});

const availableCharacteristics = computed(() => {
  if (!selectedPartId.value || !selectedProcessId.value) return [];
  return filteredMappingsByDimension.value
    .filter(m =>
      m.partId === Number(selectedPartId.value) &&
      m.processId === Number(selectedProcessId.value) &&
      m.isEnabled && m.characteristic?.isEnabled !== false && m.characteristic?.isSpcEnabled !== false
    )
    .map(m => ({ ...m.characteristic, id: m.characteristicId, mappingId: m.id }))
    .sort((a, b) => (a.characteristicCode || "").localeCompare(b.characteristicCode || ""));
});

const selectedMapping = computed(() => {
  if (!selectedPartId.value || !selectedProcessId.value || !selectedCharacteristicId.value) return null;
  return filteredMappingsByDimension.value.find(m =>
    m.partId === Number(selectedPartId.value) &&
    m.processId === Number(selectedProcessId.value) &&
    m.characteristicId === Number(selectedCharacteristicId.value)
  ) || null;
});

// Autocomplete
const filteredMappings = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  if (!q) return [];
  return filteredMappingsByDimension.value.filter(m =>
    (m.part?.partNo || "").toLowerCase().includes(q) ||
    (m.part?.partName || "").toLowerCase().includes(q) ||
    (m.process?.processName || "").toLowerCase().includes(q) ||
    (m.characteristic?.characteristicName || "").toLowerCase().includes(q)
  ).slice(0, 15);
});

function selectMappingFromSearch(m) {
  updatingCascades.value = true;
  selectedPartId.value = m.partId;
  selectedProcessId.value = m.processId;
  selectedCharacteristicId.value = m.characteristicId;
  updatingCascades.value = false;
  showSearchResults.value = false;
  loadChart();
}

function hideSearchResults() {
  setTimeout(() => { showSearchResults.value = false; }, 200);
}

// ─── Watchers ────────────────────────────────────────────────
watch(selectedDimension, (newDim) => {
  if (newDim !== "PROD") {
    const commonP = mappings.value.find(m => m.part?.partNo === "COMMON")?.part;
    selectedPartId.value = commonP ? commonP.id : "";
  } else {
    selectedPartId.value = "";
  }
  selectedProcessId.value = "";
  selectedCharacteristicId.value = "";
  chartResult.value = null;
});

watch(selectedPartId, () => {
  if (updatingCascades.value) return;
  selectedProcessId.value = "";
  selectedCharacteristicId.value = "";
  chartResult.value = null;
});

watch(selectedProcessId, () => {
  if (updatingCascades.value) return;
  selectedCharacteristicId.value = "";
  chartResult.value = null;
});

// ─── Data loading ─────────────────────────────────────────────
async function loadMappings() {
  try {
    const [mapRes, typesRes, catsRes, groupsRes] = await Promise.all([
      api.get("/part-process-characteristics"),
      api.get("/control-chart-types"),
      api.get("/control-chart-categories"),
      api.get("/control-chart-groups")
    ]);
    chartTypes.value = typesRes.data || [];
    categories.value = catsRes.data || [];
    groups.value = groupsRes.data || [];
    mappings.value = mapRes.data || [];

    // Init PROC dimension default part
    const commonP = mappings.value.find(m => m.part?.partNo === "COMMON")?.part;
    if (selectedDimension.value !== "PROD") {
      selectedPartId.value = commonP ? commonP.id : "";
    }

    // Auto-load from route query
    const qPpc = Number(route.query.ppcId);
    if (qPpc) {
      const match = mappings.value.find(m => m.id === qPpc);
      if (match) {
        const dim = getDimensionForMapping(match);
        selectedDimension.value = dim;
        await nextTick();
        updatingCascades.value = true;
        selectedPartId.value = match.partId;
        selectedProcessId.value = match.processId;
        selectedCharacteristicId.value = match.characteristicId;
        updatingCascades.value = false;
        loadChart();
      }
    }
  } catch (e) {
    error.value = "無法載入檢驗項目基準：" + getApiErrorMessage(e);
  }
}

async function loadChart() {
  const mapping = selectedMapping.value;
  if (!mapping) return;

  loading.value = true;
  error.value = "";
  chartResult.value = null;

  try {
    const res = await api.get("/v1/spc/chart", { params: { ppcId: mapping.id } });
    chartResult.value = res.data;
    loading.value = false;
    await nextTick();
    renderTrendChart();
  } catch (e) {
    if (e?.response?.status === 404) {
      error.value = "找不到該檢驗項目的量測資料，可能尚無數據或尚未配置管制圖。";
    } else {
      error.value = getApiErrorMessage(e);
    }
  } finally {
    loading.value = false;
  }
}

// ─── Chart rendering ──────────────────────────────────────────
function renderTrendChart() {
  if (!trendChartEl.value || !chartResult.value) return;

  if (trendChartInstance) {
    trendChartInstance.dispose();
    trendChartInstance = null;
  }

  trendChartInstance = echarts.init(trendChartEl.value);

  const data = chartResult.value;
  const rawPoints = data.rawDataPoints || [];
  const limits = data.limits || {};

  if (rawPoints.length === 0) {
    trendChartInstance.setOption({
      backgroundColor: "transparent",
      graphic: [{
        type: "text",
        left: "center",
        top: "middle",
        style: { text: "此期間無原始量測資料", fontSize: 16, fill: "#94a3b8" }
      }]
    });
    return;
  }

  const labels = rawPoints.map((p, i) => {
    if (p.measuredAt) {
      return new Date(p.measuredAt).toLocaleString("zh-TW", {
        month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hour12: false
      });
    }
    return `P#${i + 1}`;
  });

  const seriesData = rawPoints.map(p => {
    const isExcluded = p.isExcluded;
    const isOos = (limits.usl != null && p.value > limits.usl) || (limits.lsl != null && p.value < limits.lsl);
    let color = isOos ? "#ef4444" : "#6366f1";
    let borderColor = isOos ? "#991b1b" : "#4338ca";
    let symbol = "circle";
    let symbolSize = isOos ? 11 : 7;

    if (isExcluded) {
      color = "#94a3b8"; borderColor = "#64748b";
      symbol = "path://M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z";
      symbolSize = 13;
    }

    return {
      value: p.value !== undefined ? p.value : null,
      itemStyle: { color, borderColor, borderWidth: 2 },
      symbol, symbolSize, meta: p
    };
  });

  const markLines = [];
  const addLine = (y, name, color, style = "dashed", width = 2) => {
    if (y == null || Number.isNaN(y)) return;
    markLines.push({
      name, yAxis: y,
      lineStyle: { color, width, type: style },
      label: { formatter: `${name}: ${Number(y).toFixed(3)}`, color, position: "end", fontSize: 11, fontWeight: "bold" }
    });
  };
  addLine(limits.usl, "USL", "#ef4444", "dashed", 2);
  addLine(limits.lsl, "LSL", "#ef4444", "dashed", 2);
  addLine(limits.target, "Target", "#10b981", "solid", 2);

  // Stats for reference
  const vals = rawPoints.filter(p => !p.isExcluded && p.value != null).map(p => p.value);
  const mean = vals.length ? vals.reduce((a, b) => a + b, 0) / vals.length : null;
  if (mean != null) {
    markLines.push({
      name: "Mean", yAxis: mean,
      lineStyle: { color: "#3b82f6", width: 1.5, type: "dotted" },
      label: { formatter: `Mean: ${mean.toFixed(3)}`, color: "#3b82f6", position: "end", fontSize: 10 }
    });
  }

  trendChartInstance.setOption({
    backgroundColor: "transparent",
    tooltip: {
      trigger: "axis",
      backgroundColor: "rgba(15, 23, 42, 0.95)",
      borderColor: "#334155",
      textStyle: { color: "#fff", fontSize: 12 },
      formatter: (params) => {
        let res = `<div class="font-bold border-b border-slate-700 pb-1 mb-1">${params[0].axisValue}</div>`;
        const meta = params[0].data?.meta;
        if (meta) {
          res += `<div style="font-size:10px;color:#94a3b8;margin-bottom:6px">`;
          if (meta.lotNo) res += `<div>批號: <span style="color:#e2e8f0">${meta.lotNo}</span></div>`;
          if (meta.serialNo) res += `<div>序號: <span style="color:#e2e8f0">${meta.serialNo}</span></div>`;
          if (meta.operator) res += `<div>人員: <span style="color:#e2e8f0">${meta.operator}</span></div>`;
          res += `</div>`;
        }
        params.forEach(p => {
          if (p.data?.value != null) {
            const v = Number(p.data.value);
            const isOos = (limits.usl != null && v > limits.usl) || (limits.lsl != null && v < limits.lsl);
            res += `<div style="margin-top:4px"><span style="display:inline-block;width:8px;height:8px;border-radius:50%;background:${p.color};margin-right:4px"></span>量測值: <strong style="color:${isOos ? '#f87171' : '#a5b4fc'}">${v.toFixed(4)}</strong>${isOos ? ' ⚠ OOS' : ''}</div>`;
          }
        });
        return res;
      }
    },
    toolbox: {
      feature: {
        dataZoom: { yAxisIndex: "none" },
        restore: {},
        saveAsImage: { name: "TrendChart" }
      },
      iconStyle: { borderColor: "#64748b" }
    },
    dataZoom: [
      { type: "slider", show: true, xAxisIndex: [0], bottom: 8, borderColor: "#334155", textStyle: { color: "#64748b" } },
      { type: "inside", xAxisIndex: [0] }
    ],
    grid: { left: 65, right: 90, top: 50, bottom: 60 },
    xAxis: {
      type: "category", data: labels, boundaryGap: false,
      axisLine: { lineStyle: { color: "#64748b" } },
      axisTick: { lineStyle: { color: "#334155" } },
      axisLabel: { color: "#94a3b8", fontSize: 10, rotate: labels.length > 50 ? 30 : 0 }
    },
    yAxis: {
      type: "value", name: "量測值",
      nameTextStyle: { color: "#94a3b8", fontSize: 11 },
      splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } },
      axisLine: { lineStyle: { color: "#64748b" } },
      axisLabel: { color: "#94a3b8", fontSize: 10 },
      scale: true
    },
    series: [{
      name: "量測值",
      type: "line",
      data: seriesData,
      showSymbol: true,
      markLine: markLines.length > 0 ? { symbol: "none", data: markLines, animation: false } : undefined,
      smooth: false,
      lineStyle: { color: "#6366f1", width: 2 },
      areaStyle: {
        color: {
          type: "linear", x: 0, y: 0, x2: 0, y2: 1,
          colorStops: [
            { offset: 0, color: "rgba(99,102,241,0.15)" },
            { offset: 1, color: "rgba(99,102,241,0)" }
          ]
        }
      }
    }]
  });
}

function handleResize() { trendChartInstance?.resize(); }

onMounted(() => {
  loadMappings();
  window.addEventListener("resize", handleResize);
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);
  trendChartInstance?.dispose();
  trendChartInstance = null;
});

// Stats computed
const trendStats = computed(() => {
  if (!chartResult.value) return null;
  const vals = (chartResult.value.rawDataPoints || [])
    .filter(p => !p.isExcluded && p.value != null)
    .map(p => p.value);
  if (!vals.length) return null;
  const n = vals.length;
  const mean = vals.reduce((a, b) => a + b, 0) / n;
  const variance = vals.reduce((a, b) => a + (b - mean) ** 2, 0) / (n - 1);
  const std = Math.sqrt(variance);
  const min = Math.min(...vals);
  const max = Math.max(...vals);
  const range = max - min;
  return { n, mean: mean.toFixed(4), std: std.toFixed(4), min: min.toFixed(4), max: max.toFixed(4), range: range.toFixed(4) };
});
</script>

<template>
  <section class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-indigo-600 to-violet-500 rounded-xl shadow-lg shadow-indigo-500/30 text-white">
          <TrendingUp class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">量測值趨勢圖</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">顯示各量測點原始數值時序趨勢，含工程規格界限 (USL / LSL / Target)</p>
        </div>
      </div>
    </div>

    <!-- Dimension Selector -->
    <div class="grid grid-cols-3 gap-3 bg-white dark:bg-slate-900 p-2 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <button
        v-for="dim in [{ id: 'PROC', label: '製程管制項目 (Process)' }, { id: 'CHEM', label: '藥液管制項目 (Chemical)' }, { id: 'PROD', label: '產品管制項目 (Product)' }]"
        :key="dim.id"
        @click="selectedDimension = dim.id"
        :class="[
          'py-3 px-4 rounded-xl text-center text-sm font-semibold transition-all duration-200',
          selectedDimension === dim.id
            ? 'bg-gradient-to-r from-indigo-600 to-violet-600 text-white font-bold shadow-lg shadow-indigo-500/25'
            : 'text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'
        ]"
      >
        {{ dim.label }}
      </button>
    </div>

    <!-- Selection Controls -->
    <div class="p-5 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm flex flex-wrap items-end gap-4">
      <!-- Fuzzy Search -->
      <div class="relative w-full md:w-72">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">🔍 快速搜尋</label>
        <div class="relative">
          <input
            v-model="searchQuery"
            @focus="showSearchResults = true"
            @blur="hideSearchResults"
            placeholder="輸入關鍵字快速搜尋..."
            class="w-full pl-9 pr-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500"
          />
          <Search class="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
        </div>
        <!-- Autocomplete Dropdown -->
        <div v-if="showSearchResults && filteredMappings.length > 0"
          class="absolute z-20 top-full mt-1 w-full bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl shadow-xl overflow-hidden max-h-64 overflow-y-auto">
          <button
            v-for="m in filteredMappings"
            :key="m.id"
            @mousedown="selectMappingFromSearch(m)"
            class="w-full text-left px-4 py-2.5 hover:bg-indigo-50 dark:hover:bg-slate-800 text-xs transition-all border-b border-slate-100 dark:border-slate-800 last:border-0"
          >
            <span class="font-bold text-indigo-600 dark:text-indigo-400">{{ m.part?.partNo }}</span>
            <span class="text-slate-400 mx-1">›</span>
            <span class="text-slate-700 dark:text-slate-200">{{ m.process?.processName }}</span>
            <span class="text-slate-400 mx-1">›</span>
            <span class="text-slate-600 dark:text-slate-300">{{ m.characteristic?.characteristicName }}</span>
          </button>
        </div>
      </div>

      <!-- Cascading Selectors -->
      <div v-if="selectedDimension === 'PROD'" class="w-40">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">料號 (Product)</label>
        <select v-model="selectedPartId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500">
          <option value="">選擇料號...</option>
          <option v-for="p in uniqueParts" :key="p.id" :value="p.id">[{{ p.partNo }}] {{ p.partName }}</option>
        </select>
      </div>

      <div class="w-40">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">工站 (Station)</label>
        <select v-model="selectedProcessId" :disabled="selectedDimension === 'PROD' ? !selectedPartId : false"
          class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500 disabled:opacity-50">
          <option value="">選擇工站...</option>
          <option v-for="pr in availableProcesses" :key="pr.id" :value="pr.id">[{{ pr.processCode }}] {{ pr.processName }}</option>
        </select>
      </div>

      <div class="w-48">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">檢驗項目 (Inspection Item)</label>
        <select v-model="selectedCharacteristicId" :disabled="!selectedProcessId"
          class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500 disabled:opacity-50">
          <option value="">選擇檢驗項目...</option>
          <option v-for="c in availableCharacteristics" :key="c.id" :value="c.id">[{{ c.characteristicCode }}] {{ c.characteristicName }}</option>
        </select>
      </div>

      <button
        @click="loadChart"
        :disabled="loading || !selectedMapping"
        class="flex items-center gap-2 px-5 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-500 hover:to-violet-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 disabled:opacity-50 transition-all"
      >
        <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" />
        載入趨勢圖
      </button>
    </div>

    <!-- Error -->
    <div v-if="error" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ error }}
    </div>

    <!-- Loading -->
    <div v-if="loading" class="h-64 flex flex-col items-center justify-center space-y-3 text-slate-400">
      <Activity class="w-10 h-10 animate-bounce text-indigo-500" />
      <p class="text-sm font-bold">正在載入量測趨勢資料...</p>
    </div>

    <!-- Chart Area -->
    <div v-if="chartResult && !loading" class="space-y-5">

      <!-- Info Strip -->
      <div v-if="selectedMapping" class="grid grid-cols-1 sm:grid-cols-4 gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm text-xs font-semibold">
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">工站製程</span>
          <span class="text-slate-800 dark:text-slate-200">[{{ selectedMapping.process?.processCode }}] {{ selectedMapping.process?.processName }}</span>
        </div>
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">檢驗特性</span>
          <span class="text-slate-800 dark:text-slate-200">
            [{{ selectedMapping.characteristic?.characteristicCode }}] {{ selectedMapping.characteristic?.characteristicName }}
            <span v-if="selectedMapping.characteristic?.unit" class="text-slate-400">({{ selectedMapping.characteristic.unit }})</span>
          </span>
        </div>
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">規格界限</span>
          <span class="font-mono text-slate-800 dark:text-slate-200">
            LSL: {{ selectedMapping.lsl ?? '-∞' }} ｜ Target: {{ selectedMapping.targetValue ?? 'N/A' }} ｜ USL: {{ selectedMapping.usl ?? '+∞' }}
          </span>
        </div>
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">量測點數</span>
          <span class="text-indigo-600 dark:text-indigo-400 font-bold">{{ chartResult.rawDataPoints?.length ?? 0 }} 筆</span>
        </div>
      </div>

      <!-- Stats Summary Cards -->
      <div v-if="trendStats" class="grid grid-cols-2 md:grid-cols-6 gap-4">
        <div v-for="(card) in [
          { label: '樣本數 (N)', value: trendStats.n, cls: 'text-slate-200' },
          { label: '平均值 (Mean)', value: trendStats.mean, cls: 'text-blue-400' },
          { label: '標準差 (σ)', value: trendStats.std, cls: 'text-indigo-400' },
          { label: '最小值 (Min)', value: trendStats.min, cls: 'text-emerald-400' },
          { label: '最大值 (Max)', value: trendStats.max, cls: 'text-amber-400' },
          { label: '全距 (Range)', value: trendStats.range, cls: 'text-violet-400' }
        ]" :key="card.label"
          class="p-4 rounded-2xl bg-gradient-to-tr from-slate-900 to-slate-800 border border-slate-700 shadow-md"
        >
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400">{{ card.label }}</p>
          <h3 class="text-xl font-black mt-1 font-mono" :class="card.cls">{{ card.value }}</h3>
        </div>
      </div>

      <!-- Trend Chart -->
      <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl">
        <div class="flex items-center justify-between mb-4 pb-4 border-b border-slate-100 dark:border-slate-800">
          <h2 class="text-base font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
            <Sliders class="w-5 h-5 text-indigo-500" />
            原始量測值趨勢 (Raw Measurement Trend)
          </h2>
          <div class="flex items-center gap-4 text-xs font-semibold text-slate-500">
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-red-500 inline-block"></span>超出規格 (OOS)</span>
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-indigo-500 inline-block"></span>正常量測值</span>
          </div>
        </div>
        <div ref="trendChartEl" class="h-[480px] w-full min-h-[320px]"></div>
      </div>

      <!-- Guide tip -->
      <div class="p-4 bg-indigo-50 dark:bg-indigo-950/30 border border-indigo-100 dark:border-indigo-800/50 rounded-2xl flex items-start gap-3 text-xs text-indigo-700 dark:text-indigo-300">
        <Info class="w-4 h-4 mt-0.5 flex-shrink-0 text-indigo-500" />
        <span>此趨勢圖顯示的是<strong>每筆原始量測值</strong>的時序折線，不含管制界限（UCL/LCL/CL）。如需查看管制界限與西方電氣判讀，請切換至「SPC 管制圖」頁面。</span>
      </div>
    </div>

    <!-- Empty state -->
    <div v-if="!chartResult && !loading && !error" class="h-64 flex flex-col items-center justify-center space-y-3 text-slate-400 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
      <TrendingUp class="w-12 h-12 text-indigo-300" />
      <p class="text-sm font-bold">請選擇工站與檢驗項目後按「載入趨勢圖」</p>
    </div>
  </section>
</template>
