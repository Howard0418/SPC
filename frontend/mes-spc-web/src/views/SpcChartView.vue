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

// SPC master data source: PartProcessCharacteristics
const mappings = ref([]);
const batchId = ref("");
const ppcId = ref("");
const uploadBatchId = ref("");
const loading = ref(false);
const error = ref("");
const startDate = ref("");
const endDate = ref("");

// Dimension selection state: 'PROC', 'CHEM', 'PROD'
const selectedDimension = ref("PROC");
const chartTypes = ref([]);
const categories = ref([]);
const groups = ref([]);
const trendChartEl = ref(null);
let trendChartInstance = null;

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

// Filtered mappings based on selected dimension
const filteredMappingsByDimension = computed(() => {
  return mappings.value.filter(m => {
    if (!m.isEnabled) return false;
    const dim = getDimensionForMapping(m);
    return dim === selectedDimension.value;
  });
});

// Unique Parts for selection
const uniqueParts = computed(() => {
  const byId = new Map();
  filteredMappingsByDimension.value
    .filter(m => (m.controlScope || "PRODUCT") === "PRODUCT" && m.part?.isEnabled !== false)
    .forEach(m => {
      if (!byId.has(m.partId)) byId.set(m.partId, m.part);
    });
  return [...byId.entries()]
    .map(([id, part]) => ({ ...part, id }))
    .sort((a, b) => (a.partNo || "").localeCompare(b.partNo || ""));
});

// Processes available for selected Part (Station)
const availableProcesses = computed(() => {
  if (selectedDimension.value === "PROD" && !selectedPartId.value) return [];
  const byId = new Map();
  filteredMappingsByDimension.value
    .filter(m =>
      (selectedDimension.value !== "PROD" || m.partId === Number(selectedPartId.value)) &&
      m.process?.isEnabled !== false
    )
    .forEach(m => {
      if (!byId.has(m.processId)) byId.set(m.processId, m.process);
    });
  return [...byId.entries()]
    .map(([id, process]) => ({ ...process, id }))
    .sort((a, b) => (a.processCode || "").localeCompare(b.processCode || ""));
});

// Characteristics available for selected Part + Process (Inspection Items)
const availableCharacteristics = computed(() => {
  if ((selectedDimension.value === "PROD" && !selectedPartId.value) || !selectedProcessId.value) return [];
  return filteredMappingsByDimension.value
    .filter(m =>
      (selectedDimension.value !== "PROD" || m.partId === Number(selectedPartId.value)) &&
      m.processId === Number(selectedProcessId.value) &&
      m.isEnabled &&
      m.characteristic?.isEnabled !== false &&
      m.characteristic?.isSpcEnabled !== false
    )
    .map(m => ({ ...m.characteristic, id: m.characteristicId, mappingId: m.id }))
    .sort((a, b) => (a.characteristicCode || "").localeCompare(b.characteristicCode || ""));
});

const selectedMapping = computed(() => {
  if ((selectedDimension.value === "PROD" && !selectedPartId.value) || !selectedProcessId.value || !selectedCharacteristicId.value) return null;
  return filteredMappingsByDimension.value.find(m =>
    (selectedDimension.value !== "PROD" || m.partId === Number(selectedPartId.value)) &&
    m.processId === Number(selectedProcessId.value) &&
    m.characteristicId === Number(selectedCharacteristicId.value)
  ) || null;
});

const getDimensionForMapping = (m) => {
  if (m.controlScope === "PROCESS") return "PROC";
  if (m.controlScope === "CHEMICAL") return "CHEM";
  if (m.controlScope === "PRODUCT") return "PROD";
  if (!m.chartTypeId) {
    return m.partId ? "PROD" : "PROC";
  }
  const type = chartTypes.value.find(t => t.id === m.chartTypeId);
  if (!type) return m.partId ? "PROD" : "PROC";
  const cat = categories.value.find(c => c.id === type.chartCategoryId);
  if (!cat) return m.partId ? "PROD" : "PROC";
  const group = groups.value.find(g => g.id === cat.chartGroupId);
  return group?.groupCode || (m.partId ? "PROD" : "PROC");
};

// Autocomplete search filtering
const filteredMappings = computed(() => {
  const query = searchQuery.value.trim().toLowerCase();
  if (!query) return [];
  return filteredMappingsByDimension.value.filter(m => {
    return (
      (m.part?.partNo || "").toLowerCase().includes(query) ||
      (m.part?.partName || "").toLowerCase().includes(query) ||
      (m.process?.processName || "").toLowerCase().includes(query) ||
      (m.characteristic?.characteristicName || "").toLowerCase().includes(query)
    );
  }).slice(0, 15);
});

// Sync cascading dropdown selectors with selected mapping ID
function syncCascadingDropdowns(productId, stationId, inspectionItemId) {
  updatingCascades.value = true;
  selectedPartId.value = productId;
  selectedProcessId.value = stationId;
  selectedCharacteristicId.value = inspectionItemId;
  updatingCascades.value = false;
}

// Watchers for cascading select workflow
watch(selectedDimension, (newDim) => {
  if (newDim === "PROD") {
    selectedPartId.value = "";
  } else {
    selectedPartId.value = "";
  }
  selectedProcessId.value = "";
  selectedCharacteristicId.value = "";
  ppcId.value = "";
  uploadBatchId.value = "";
});

watch(selectedPartId, () => {
  if (updatingCascades.value) return;
  ppcId.value = "";
  uploadBatchId.value = "";
  selectedProcessId.value = "";
  selectedCharacteristicId.value = "";
});

watch(selectedProcessId, () => {
  if (updatingCascades.value) return;
  ppcId.value = "";
  uploadBatchId.value = "";
  selectedCharacteristicId.value = "";
});

watch(selectedCharacteristicId, (newVal) => {
  if (updatingCascades.value) return;
  ppcId.value = "";
  uploadBatchId.value = "";
});

function selectMappingFromSearch(m) {
  syncCascadingDropdowns(m.partId, m.processId, m.characteristicId);
  showSearchResults.value = false;
  loadActiveChart();
}

function hideSearchResults() {
  setTimeout(() => {
    showSearchResults.value = false;
  }, 200);
}

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
    
    // Auto-select from query
    const qPpc = Number(route.query.ppcId || route.query.partProcessCharacteristicId);
    
    if (qPpc) {
      const match = mappings.value.find(m => m.id === qPpc);
      if (match) {
        const dim = getDimensionForMapping(match);
        selectedDimension.value = dim;
        
        await nextTick();
        syncCascadingDropdowns(match.partId, match.processId, match.characteristicId);
      }
      ppcId.value = String(qPpc);
      loadActiveChart();
    } else {
      selectedPartId.value = "";
    }
  } catch (e) {
    error.value = "無法載入檢驗項目基準及管制圖配置：" + getApiErrorMessage(e);
  }
}

onMounted(() => {
  if (route.query.ppcId || route.query.partProcessCharacteristicId) {
    ppcId.value = route.query.ppcId || route.query.partProcessCharacteristicId;
  }
  if (route.query.uploadBatchId) {
    uploadBatchId.value = route.query.uploadBatchId;
  }
  if (route.query.batchId) {
    batchId.value = route.query.batchId;
  }
  if (ppcId.value && uploadBatchId.value) {
    loadUploadBatchChart();
  } else {
    loadMappings();
  }
  window.addEventListener("resize", handleResize);
});

watch(batchId, () => {
  loadActiveChart();
});

const getChartQueryParams = (activePpcId) => {
  const params = { ppcId: activePpcId };
  if (uploadBatchId.value) params.uploadBatchId = uploadBatchId.value;
  if (startDate.value) params.startDate = startDate.value;
  if (endDate.value) params.endDate = endDate.value;
  return params;
};

async function loadActiveChart() {
  if (startDate.value && endDate.value && startDate.value > endDate.value) {
    error.value = "量測起日不可晚於量測迄日。";
    return;
  }

  if (ppcId.value && uploadBatchId.value) {
    await loadUploadBatchChart();
  } else {
    await loadInteractiveChart();
  }
}

async function loadUploadBatchChart() {
  if (!ppcId.value || !uploadBatchId.value) return;
  loading.value = true;
  error.value = "";
  chartResult.value = null;
  selectedPoint.value = null;
  selectedPointIndex.value = -1;

  try {
    const res = await api.get("/v1/spc/chart", {
      params: getChartQueryParams(ppcId.value)
    });
    chartResult.value = res.data;
    loading.value = false;
    await nextTick();
    renderECharts();
  } catch (e) {
    if (e?.response?.status === 404) {
      error.value = "找不到該 Excel/匯入批次的管制圖資料，可能尚未確認匯入或檢驗基準不存在。";
    } else {
      error.value = getApiErrorMessage(e);
    }
  } finally {
    loading.value = false;
  }
}

async function loadInteractiveChart() {
  const mapping = selectedMapping.value;
  if (!mapping && !ppcId.value) return;
  loading.value = true;
  error.value = "";
  chartResult.value = null;
  selectedPoint.value = null;
  selectedPointIndex.value = -1;

  try {
    const activePpcId = ppcId.value || mapping.id;
    ppcId.value = String(activePpcId);
    const res = await api.get("/v1/spc/chart", {
      params: getChartQueryParams(activePpcId)
    });
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

async function toggleExcludeBatch() {
  if (!selectedPoint.value && !uploadBatchId.value) return;
  try {
    const batchIdToToggle = selectedPoint.value?.measurementBatchId || uploadBatchId.value;
    await api.post(`/v1/spc/exclude-batch/${batchIdToToggle}`);
    await loadActiveChart();
  } catch (e) {
    alert("剔除狀態更新失敗：" + getApiErrorMessage(e));
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
  const isDual = type === "XBAR_R" || type === "XBAR_S" || type === "I-MR" || type === "I_MR";

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

  // Control limits (Static, for Variables)
  const hasDynamicLimits = (data.chartData?.points || []).some(p => p.uclStat != null || p.lclStat != null);
  if (!hasDynamicLimits) {
    addLine(limits.ucl, "UCL", "#f59e0b", "solid", 1.5);
    addLine(limits.cl, "CL", "#3b82f6", "solid", 1.5);
    addLine(limits.lcl, "LCL", "#f59e0b", "solid", 1.5);
  } else {
    // If it has dynamic limits, we still might want a static CL if pBar/cBar is constant
    addLine(limits.cl, "CL", "#3b82f6", "solid", 1.5);
  }

  const topMarkLineObj = markLinesTop.length > 0 ? { symbol: "none", data: markLinesTop, animation: false } : undefined;

  // Bottom marklines
  const markLinesBottom = [];
  const stat = data.statControlLimits || {};
  let bottomUcl = null, bottomCl = null, bottomLcl = null;
  if (type === "XBAR_R") {
    bottomUcl = stat.rControl?.ucl;
    bottomCl = stat.rControl?.cl;
    bottomLcl = stat.rControl?.lcl;
  } else if (type === "XBAR_S") {
    bottomUcl = stat.sControl?.ucl;
    bottomCl = stat.sControl?.cl;
    bottomLcl = stat.sControl?.lcl;
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

  const seriesTopData = pointsTop.map((p, i) => {
    const isErr = p.outOfSpec || p.outOfControl || (p.violatedRules && p.violatedRules.length > 0);
    const isExcluded = p.isExcluded;
    
    let color = isErr ? "#ef4444" : "#3b82f6";
    let borderColor = isErr ? "#991b1b" : "#2563eb";
    let symbol = "circle";
    let symbolSize = isErr ? 12 : 8;
    
    if (isExcluded) {
      color = "#94a3b8";
      borderColor = "#64748b";
      symbol = "path://M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z";
      symbolSize = 14;
    }

    return {
      value: p.value !== undefined ? p.value : p.xbar !== undefined ? p.xbar : null,
      itemStyle: { color, borderColor, borderWidth: 2 },
      symbol,
      symbolSize,
      violatedRules: p.violatedRules,
      meta: p
    };
  });

  const seriesBottomData = pointsBottom.map(p => {
    const isErr = p.outOfControl;
    const isExcluded = p.isExcluded;
    
    let color = isErr ? "#ef4444" : "#64748b";
    let symbol = "circle";
    let symbolSize = isErr ? 10 : 6;
    
    if (isExcluded) {
      color = "#cbd5e1";
      symbol = "path://M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z";
      symbolSize = 12;
    }

    return {
      value: p.value !== undefined ? p.value : null,
      itemStyle: { color },
      symbol,
      symbolSize,
      meta: p
    };
  });

  const seriesTopList = [
    { name: type === "XBAR_R" || type === "XBAR_S" ? "Xbar" : (isDual ? "Individual" : type), type: "line", xAxisIndex: 0, yAxisIndex: 0, data: seriesTopData, showSymbol: true, markLine: topMarkLineObj, markArea: hasDynamicLimits ? undefined : markAreaTop, smooth: false }
  ];

  if (hasDynamicLimits) {
    seriesTopList.push({
      name: "UCL",
      type: "line",
      step: "middle",
      symbol: "none",
      xAxisIndex: 0, yAxisIndex: 0,
      lineStyle: { color: "#f59e0b", width: 1.5, type: "solid" },
      data: pointsTop.map(p => p.uclStat != null ? Number(p.uclStat.toFixed(4)) : null)
    });
    seriesTopList.push({
      name: "LCL",
      type: "line",
      step: "middle",
      symbol: "none",
      xAxisIndex: 0, yAxisIndex: 0,
      lineStyle: { color: "#f59e0b", width: 1.5, type: "solid" },
      data: pointsTop.map(p => p.lclStat != null ? Math.max(0, Number(p.lclStat.toFixed(4))) : null)
    });
  }

  const option = {
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
          res += `<div class="text-[10px] text-slate-400 mb-2">`;
          if (meta.lotNo) res += `<div>批號: <span class="text-slate-200">${meta.lotNo}</span></div>`;
          if (meta.sideCode && meta.sideCode !== "0") res += `<div>板面: <span class="text-blue-300 font-bold">${meta.sideCode === "1" ? "S1 (Top)" : "S2 (Bottom)"}</span></div>`;
          if (meta.lineId) res += `<div>產線: <span class="text-emerald-400">ID ${meta.lineId}</span></div>`;
          if (meta.tankId) res += `<div>槽體: <span class="text-emerald-400">ID ${meta.tankId}</span></div>`;
          if (meta.slotId) res += `<div>槽位: <span class="text-emerald-400">ID ${meta.slotId}</span></div>`;
          res += `</div>`;
        }
        params.forEach(p => {
          res += `<div><span class="inline-block w-2 h-2 rounded-full mr-1" style="background-color:${p.color}"></span> ${p.seriesName}: <strong>${p.data?.value !== undefined ? Number(p.data?.value).toFixed(4) : Number(p.value).toFixed(4)}</strong></div>`;
          if (p.data?.violatedRules?.length > 0) {
            res += `<div class="mt-1.5 px-2 py-0.5 rounded bg-red-900/50 border border-red-500/50 text-red-300 text-[11px] font-bold">⚠️ 西方電氣規則違規：<br>${p.data.violatedRules.join("<br>")}</div>`;
          }
        });
        if (hasDynamicLimits && meta) {
          if (meta.n != null) res += `<div class="text-[10px] text-slate-400 mt-2">樣本數 (n): <strong class="text-slate-200">${meta.n}</strong></div>`;
        }
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
          { type: "value", gridIndex: 0, name: type === "XBAR_R" || type === "XBAR_S" ? "Xbar 平均值" : "單值 (I)", splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, scale: true },
          { type: "value", gridIndex: 1, name: type === "XBAR_R" ? "全距 (R)" : type === "XBAR_S" ? "標準差 (S)" : "移動全距 (MR)", splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, scale: true }
        ]
      : [{ type: "value", name: `${type} 數值`, splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, scale: true }],
    series: isDual
      ? [
          ...seriesTopList,
          { name: type === "XBAR_R" ? "Range" : type === "XBAR_S" ? "Std Dev" : "Moving Range", type: "line", xAxisIndex: 1, yAxisIndex: 1, data: seriesBottomData, showSymbol: true, markLine: bottomMarkLineObj, smooth: false }
        ]
      : seriesTopList
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

  // Render the raw data points trend chart
  renderTrendChart();
}

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

  const labels = rawPoints.map((p, i) => {
    if (p.measuredAt) {
      return new Date(p.measuredAt).toLocaleString('zh-TW', { 
        month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false 
      });
    }
    return `P#${i + 1}`;
  });

  const seriesData = rawPoints.map((p, i) => {
    const isExcluded = p.isExcluded;
    const isOos = (limits.usl != null && p.value > limits.usl) || (limits.lsl != null && p.value < limits.lsl);
    
    let color = isOos ? "#ef4444" : "#10b981";
    let borderColor = isOos ? "#991b1b" : "#047857";
    let symbol = "circle";
    let symbolSize = isOos ? 10 : 6;

    if (isExcluded) {
      color = "#94a3b8";
      borderColor = "#64748b";
      symbol = "path://M12 2C6.47 2 2 6.47 2 12s4.47 10 10 10 10-4.47 10-10S17.53 2 12 2zm5 13.59L15.59 17 12 13.41 8.41 17 7 15.59 10.59 12 7 8.41 8.41 7 12 10.59 15.59 7 17 8.41 13.41 12 17 15.59z";
      symbolSize = 12;
    }

    return {
      value: p.value !== undefined ? p.value : null,
      itemStyle: { color, borderColor, borderWidth: 1.5 },
      symbol,
      symbolSize,
      meta: p
    };
  });

  const markLines = [];
  const addLine = (y, name, color, style = "dashed", width = 1.5) => {
    if (y == null || Number.isNaN(y)) return;
    markLines.push({
      name,
      yAxis: y,
      lineStyle: { color, width, type: style },
      label: { formatter: `${name}: ${Number(y).toFixed(3)}`, color, position: "end" }
    });
  };

  addLine(limits.usl, "USL", "#ef4444", "dashed", 2);
  addLine(limits.lsl, "LSL", "#ef4444", "dashed", 2);
  addLine(limits.target, "Target", "#10b981", "solid", 2);

  const option = {
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
          res += `<div class="text-[10px] text-slate-400 mb-2">`;
          if (meta.lotNo) res += `<div>批號: <span class="text-slate-200">${meta.lotNo}</span></div>`;
          if (meta.serialNo) res += `<div>序號: <span class="text-slate-200">${meta.serialNo}</span></div>`;
          if (meta.operator) res += `<div>人員: <span class="text-slate-200">${meta.operator}</span></div>`;
          if (meta.lineId) res += `<div>產線: <span class="text-emerald-400">ID ${meta.lineId}</span></div>`;
          if (meta.tankId) res += `<div>槽體: <span class="text-emerald-400">ID ${meta.tankId}</span></div>`;
          if (meta.slotId) res += `<div>槽位: <span class="text-emerald-400">ID ${meta.slotId}</span></div>`;
          res += `</div>`;
        }
        params.forEach(p => {
          res += `<div><span class="inline-block w-2 h-2 rounded-full mr-1" style="background-color:${p.color}"></span> 量測值: <strong>${Number(p.value).toFixed(4)}</strong></div>`;
        });
        return res;
      }
    },
    toolbox: {
      feature: {
        dataZoom: { yAxisIndex: "none" },
        restore: {},
        saveAsImage: { name: `Trend_Chart` }
      },
      iconStyle: { borderColor: "#64748b" }
    },
    dataZoom: [
      { type: "slider", show: true, xAxisIndex: [0], bottom: 10, borderColor: "#334155", textStyle: { color: "#64748b" } },
      { type: "inside", xAxisIndex: [0] }
    ],
    grid: { left: 60, right: 80, top: 40, bottom: 60 },
    xAxis: { type: "category", data: labels, boundaryGap: false, axisLine: { lineStyle: { color: "#64748b" } } },
    yAxis: { type: "value", name: "原始量測值", splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, scale: true },
    series: [
      {
        name: "量測值",
        type: "line",
        data: seriesData,
        showSymbol: true,
        markLine: markLines.length > 0 ? { symbol: "none", data: markLines, animation: false } : undefined,
        smooth: false,
        lineStyle: { color: "#6366f1", width: 1.5 }
      }
    ]
  };

  trendChartInstance.setOption(option);
}

function handleResize() {
  chartInstance?.resize();
  trendChartInstance?.resize();
}

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);
  chartInstance?.dispose();
  chartInstance = null;
  trendChartInstance?.dispose();
  trendChartInstance = null;
});
</script>

<template>
  <div class="space-y-6">
    <!-- 🌟 三大類管制項目維度選擇器 -->
    <div class="grid grid-cols-3 gap-4 bg-white dark:bg-slate-900 p-2 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <button 
        @click="selectedDimension = 'PROC'" 
        :class="selectedDimension === 'PROC' ? 'bg-gradient-to-r from-blue-600 to-indigo-600 text-white font-bold shadow-lg shadow-blue-500/25' : 'text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'"
        class="py-3 px-4 rounded-xl text-center text-sm font-semibold transition-all duration-200"
      >
        製程管制項目 (Process Control)
      </button>
      <button 
        @click="selectedDimension = 'CHEM'" 
        :class="selectedDimension === 'CHEM' ? 'bg-gradient-to-r from-teal-600 to-emerald-600 text-white font-bold shadow-lg shadow-teal-500/25' : 'text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'"
        class="py-3 px-4 rounded-xl text-center text-sm font-semibold transition-all duration-200"
      >
        藥液管制項目 (Chemical Control)
      </button>
      <button 
        @click="selectedDimension = 'PROD'" 
        :class="selectedDimension === 'PROD' ? 'bg-gradient-to-r from-purple-600 to-fuchsia-600 text-white font-bold shadow-lg shadow-purple-500/25' : 'text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800'"
        class="py-3 px-4 rounded-xl text-center text-sm font-semibold transition-all duration-200"
      >
        產品管制項目 (Product Control)
      </button>
    </div>

    <!-- Header Controls -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div>
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-blue-600 dark:text-blue-400 mb-1">
          <Activity class="w-4 h-4 animate-spin" /> 工業品質分析空間
        </div>
        <div class="flex items-center gap-3">
          <h1 class="text-2xl font-black text-slate-800 dark:text-white">SPC 即時互動管制圖</h1>
          <button
            v-if="selectedMapping"
            @click="router.push({ path: '/part-process-characteristics', query: { editId: selectedMapping.id } })"
            type="button"
            class="flex items-center gap-1.5 px-3 py-1 rounded-xl bg-orange-50 hover:bg-orange-100 dark:bg-slate-800 dark:hover:bg-slate-700 text-orange-600 dark:text-orange-400 text-xs font-bold transition-all border border-orange-200 dark:border-slate-700"
            title="編輯此項檢驗基準與規格"
          >
            <Sliders class="w-3.5 h-3.5" /> 編輯此基準
          </button>
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-4 w-full xl:w-auto">
        <!-- Fuzzy Autocomplete Search Box -->
        <div class="relative w-full md:w-72">
          <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">🔍 快速搜尋檢驗基準</label>
          <div class="relative">
            <input v-model="searchQuery" @focus="showSearchResults = true" @blur="hideSearchResults" placeholder="輸入關鍵字快速搜尋..." class="w-full pl-9 pr-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
            <Search class="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
          </div>
        </div>

        <!-- 3-Level Cascading selectors -->
        <div class="flex flex-wrap gap-2 items-center w-full md:w-auto">
          <div class="w-40" v-if="selectedDimension === 'PROD'">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">料號 (Product)</label>
            <select v-model="selectedPartId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
              <option value="">選擇產品...</option>
              <option v-for="p in uniqueParts" :key="p.id" :value="p.id">[{{ p.partNo }}] {{ p.partName }}</option>
            </select>
          </div>

          <div class="w-40">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">工站 (Station)</label>
            <select v-model="selectedProcessId" :disabled="selectedDimension === 'PROD' ? !selectedPartId : false" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
              <option value="">選擇工站...</option>
              <option v-for="pr in availableProcesses" :key="pr.id" :value="pr.id">[{{ pr.processCode }}] {{ pr.processName }}</option>
            </select>
          </div>

          <div class="w-44">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">檢驗項目 (Inspection Item)</label>
            <select v-model="selectedCharacteristicId" :disabled="!selectedProcessId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
              <option value="">選擇檢驗項目...</option>
              <option v-for="c in availableCharacteristics" :key="c.id" :value="c.id">[{{ c.characteristicCode }}] {{ c.characteristicName }}</option>
            </select>
          </div>
        </div>

        <div class="w-48">
          <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">工單 / 批號過濾</label>
          <input v-model="batchId" placeholder="輸入 Lot No..." class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
        </div>

        <div class="w-40">
          <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">量測起日</label>
          <input v-model="startDate" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
        </div>

        <div class="w-40">
          <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">量測迄日</label>
          <input v-model="endDate" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
        </div>

        <button @click="loadActiveChart" :disabled="loading" class="mt-4 flex items-center gap-2 px-5 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold shadow-lg shadow-blue-500/20 disabled:opacity-50 transition-all text-sm h-10">
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
            <router-link
              v-if="selectedMapping && selectedMapping.chartTypeId"
              :to="`/control-chart-groups?tab=types&id=${selectedMapping.chartTypeId}`"
              class="px-2.5 py-1 rounded-full bg-slate-100 hover:bg-slate-200 dark:bg-slate-800 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 text-xs font-semibold border border-slate-300 dark:border-slate-600 transition-all flex items-center gap-1"
            >
              ⚙️ 微調公式配置
            </router-link>
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

        <!-- 🌟 量測點位趨勢圖 (Trend Chart) -->
        <div class="mt-8 pt-8 border-t border-slate-200 dark:border-slate-800">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-base font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
              <Sliders class="w-5 h-5 text-indigo-500" />
              量測點位趨勢圖 (Raw Measurements Trend)
            </h3>
            <span class="text-xs font-semibold text-slate-400">
              僅顯示工程規格界限 (USL / LSL / Target)
            </span>
          </div>

          <!-- Trend Chart Info panel (No UCL/LCL/CL) -->
          <div v-if="selectedMapping" class="grid grid-cols-1 sm:grid-cols-5 gap-4 mb-5 p-4 bg-slate-50 dark:bg-slate-800/40 rounded-2xl border border-slate-200 dark:border-slate-800 text-xs font-semibold">
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">工站製程 (Process)</span>
              <span class="text-slate-800 dark:text-slate-200">
                [{{ selectedMapping.process?.processCode }}] {{ selectedMapping.process?.processName }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">檢驗特性 (Characteristic)</span>
              <span class="text-slate-800 dark:text-slate-200">
                [{{ selectedMapping.characteristic?.characteristicCode }}] {{ selectedMapping.characteristic?.characteristicName }}
                <span v-if="selectedMapping.characteristic?.unit" class="text-slate-400"> ({{ selectedMapping.characteristic?.unit }})</span>
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">工程規格界限 (Specs Limit)</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                LSL: {{ selectedMapping.lsl !== null ? selectedMapping.lsl : '-∞' }} | 
                Target: {{ selectedMapping.targetValue !== null ? selectedMapping.targetValue : 'N/A' }} | 
                USL: {{ selectedMapping.usl !== null ? selectedMapping.usl : '+∞' }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">抽樣組數配置 (SampleSize N)</span>
              <span class="text-slate-800 dark:text-slate-200">
                N = {{ selectedMapping.sampleSize || 1 }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">綁定管制圖 (Chart Type)</span>
              <span class="text-indigo-600 dark:text-indigo-400 font-bold">
                {{ chartTypes.find(t => t.id === selectedMapping.chartTypeId)?.chartTypeName || chartResult?.chartType || 'N/A' }}
              </span>
            </div>
          </div>

          <div ref="trendChartEl" class="h-[350px] w-full min-h-[250px]"></div>
        </div>

        <!-- Point Detail Drilldown Card -->
        <div v-if="selectedPoint" class="mt-6 p-5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 shadow-inner transition-all duration-300">
          <div class="flex items-center justify-between border-b border-slate-200 dark:border-slate-700 pb-3 mb-4">
            <h4 class="text-sm font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <span class="inline-block w-2.5 h-2.5 rounded-full bg-blue-500 animate-pulse"></span>
              點位品質追溯詳細資料 (點位 #{{ selectedPointIndex + 1 }})
            </h4>
            <div class="flex gap-2">
              <span v-if="selectedPoint.isExcluded" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-300 border border-slate-300 dark:border-slate-600">
                ✖ 已剔除不計 (Excluded)
              </span>
              <span v-else-if="selectedPoint.outOfSpec" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-red-100 text-red-700 dark:bg-red-950/50 dark:text-red-400 border border-red-200 dark:border-red-900/50">
                OOS 超出規格
              </span>
              <span v-else-if="selectedPoint.outOfControl" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400 border border-amber-200 dark:border-amber-900/50">
                OOC 管制失控
              </span>
              <span v-else class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-900/50">
                正常 (In Control)
              </span>
              
              <!-- Toggle Exclude Button -->
              <button @click="toggleExcludeBatch" v-if="selectedPoint.measurementBatchId || uploadBatchId" class="ml-2 px-3 py-0.5 rounded text-[10px] font-black uppercase tracking-wider border transition-colors focus:outline-none" :class="selectedPoint.isExcluded ? 'bg-blue-100 text-blue-700 border-blue-200 hover:bg-blue-200 dark:bg-blue-900/50 dark:text-blue-300 dark:border-blue-800' : 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-400 dark:border-slate-700'">
                {{ selectedPoint.isExcluded ? '↺ 恢復此數據' : '✖ 剔除此數據' }}
              </button>
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
              <h3 class="text-xl font-black" :class="selectedPoint.isExcluded ? 'text-slate-400 line-through' : 'text-slate-800 dark:text-white'">
                {{ selectedPoint.value !== undefined ? Number(selectedPoint.value).toFixed(4) : (selectedPoint.xbar !== undefined ? Number(selectedPoint.xbar).toFixed(4) : 'N/A') }}
                <span v-if="selectedPoint.range !== undefined" class="text-sm text-slate-400 font-semibold ml-3 no-underline">
                  (子組全距 R = {{ Number(selectedPoint.range).toFixed(4) }}, 子組大小 n = {{ selectedPoint.n }})
                </span>
              </h3>
            </div>

            <!-- Action trace panel (Root Cause & Corrective Action) -->
            <div v-if="selectedPoint.rootCause || selectedPoint.correctiveAction" class="w-full md:flex-1 md:ml-6 p-3 rounded-xl bg-blue-50 dark:bg-slate-900 border border-blue-100 dark:border-slate-700">
              <p class="text-[10px] font-bold text-blue-600 dark:text-blue-400 uppercase mb-2 flex items-center gap-1">
                <Info class="w-3.5 h-3.5" /> 處置追溯紀錄
              </p>
              <div class="space-y-1.5 text-xs text-slate-700 dark:text-slate-300">
                <div v-if="selectedPoint.rootCause" class="flex gap-2">
                  <span class="font-bold shrink-0">發生原因：</span>
                  <span>{{ selectedPoint.rootCause }}</span>
                </div>
                <div v-if="selectedPoint.correctiveAction" class="flex gap-2">
                  <span class="font-bold shrink-0">初步對策：</span>
                  <span>{{ selectedPoint.correctiveAction }}</span>
                </div>
              </div>
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
