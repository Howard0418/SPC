<script setup>
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import * as echarts from "echarts";
import { api, getApiErrorMessage } from "../api/client";
import { normalizeControlScope } from "../utils/controlScope";
import {
  LineChart,
  TrendingUp,
  Search,
  RefreshCw,
  AlertTriangle,
  Activity,
  ShieldAlert,
  Info,
  Sliders,
  List,
  BarChart3,
  Layers,
  Sparkles,
  Download,
  User,
  Calendar,
  Hash,
  Clock,
  Loader2,
  ZoomIn,
  RotateCcw
} from "lucide-vue-next";
import SpcSummaryTable from "../components/SpcSummaryTable.vue";

const route = useRoute();
const router = useRouter();
const isMonthlyChartPage = computed(() => route.path.startsWith("/monthly-control-chart"));

// SPC master data source: PartProcessCharacteristics
const mappings = ref([]);
const batchId = ref("");
const ppcId = ref("");
const uploadBatchId = ref("");
const loading = ref(false);
const error = ref("");

// Initialize dates: defaults to today and 3 months ago
const getTodayStr = () => new Date().toISOString().split("T")[0];
const getThreeMonthsAgoStr = () => {
  const d = new Date();
  d.setMonth(d.getMonth() - 3);
  return d.toISOString().split("T")[0];
};
const toLocalDateStr = (date) => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};
const getCurrentMonthStr = () => getTodayStr().slice(0, 7);
const getCurrentIsoWeekStr = () => {
  const date = new Date();
  const utc = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  utc.setUTCDate(utc.getUTCDate() + 4 - (utc.getUTCDay() || 7));
  const yearStart = new Date(Date.UTC(utc.getUTCFullYear(), 0, 1));
  const week = Math.ceil((((utc - yearStart) / 86400000) + 1) / 7);
  return `${utc.getUTCFullYear()}-W${String(week).padStart(2, "0")}`;
};
const getMonthStartStr = (monthValue = getCurrentMonthStr()) => `${monthValue}-01`;
const getMonthEndStr = (monthValue = getCurrentMonthStr()) => {
  const [year, month] = monthValue.split("-").map(Number);
  return toLocalDateStr(new Date(year, month, 0));
};
const getDefaultStartDateStr = () => isMonthlyChartPage.value ? getMonthStartStr() : getThreeMonthsAgoStr();
const getDefaultEndDateStr = () => isMonthlyChartPage.value ? getMonthEndStr() : getTodayStr();

const startDate = ref(getDefaultStartDateStr());
const endDate = ref(getDefaultEndDateStr());
const reportPeriodType = ref("MONTH");
const reportMonth = ref(getCurrentMonthStr());
const reportWeek = ref(getCurrentIsoWeekStr());
const productQueryMode = ref("DATE");
const productPartId = ref("");

// Dimension selection state: 'PROC', 'CHEM', 'PROD'
const selectedDimension = ref("PROC");
const chartTypes = ref([]);
const groups = ref([]);
const trendChartEl = ref(null);
const histogramChartEl = ref(null);
let trendChartInstance = null;
let histogramChartInstance = null;

// Cascading Selectors State
const selectedPartId = ref("");
const selectedProcessId = ref("");
const selectedCharacteristicId = ref("");
const updatingCascades = ref(false);

const tableSummaryData = ref(null);
const cachedSummaryData = ref(null);
const summaryDimension = ref("");
const summaryPartId = ref("");
const summaryQueryMode = ref("DATE");
const summaryProductPartId = ref("");
const selectedSummaryRow = ref(null);
const chartResult = ref(null);
let chartInstance = null;
const chartEl = ref(null);

// Click Drill-down State
const selectedPoint = ref(null);
const selectedPointIndex = ref(-1);
const showSpecLimits = ref(false);
const showControlLimits = ref(true);
const showPointValues = ref(false);
const savingControlLimits = ref(false);
const savingOcap = ref(false);
const ocapForm = ref({
  status: "Closed",
  causeCategory: "",
  causeType: "",
  actionType: "",
  rootCause: "",
  correctiveAction: "",
  responsibleUser: ""
});

const ocapCauseCategories = ["人為", "機器", "工法", "測量", "材料", "環境", "其他"];
const ocapCauseTypes = ["輸入錯誤", "操作不當", "違反SOP", "機台異常", "溫度異常", "測量準確度不佳", "材料品質不佳", "環境因素", "其他"];
const ocapActionTypes = ["確認輸入是否正確", "確認機台是否正常", "通知工程師處理", "調整參數", "清理機台", "重新量測", "通知主管", "其他"];

// Filtered mappings based on selected dimension
const filteredMappingsByDimension = computed(() => {
  return mappings.value.filter(m => {
    if (!m.isEnabled) return false;
    if (normalizeControlScope(m.controlScope, "") === "CHEM" && m.displayMode === "TREND_CHART") return false;
    const dim = getDimensionForMapping(m);
    return dim === selectedDimension.value;
  });
});

const productPartOptions = computed(() => {
  const byId = new Map();
  mappings.value
    .filter(m => m.controlScope === "PRODUCT" && m.isEnabled && m.part?.isEnabled !== false)
    .forEach(m => {
      if (m.partId && !byId.has(m.partId)) byId.set(m.partId, m.part);
    });
  return [...byId.entries()]
    .map(([id, part]) => ({ ...part, id }))
    .sort((a, b) => (a.partNo || "").localeCompare(b.partNo || ""));
});

// Processes available for selected Part (Station)
const availableProcesses = computed(() => {
  if (selectedDimension.value === "PRODUCT" && !selectedPartId.value) return [];
  const byId = new Map();
  filteredMappingsByDimension.value
    .filter(m =>
      (selectedDimension.value !== "PRODUCT" || m.partId === Number(selectedPartId.value)) &&
      m.process?.isEnabled !== false
    )
    .forEach(m => {
      if (!byId.has(m.processId)) byId.set(m.processId, m.process);
    });
  return [...byId.entries()]
    .map(([id, process]) => ({ ...process, id }))
    .sort((a, b) => (Number(a.sequenceNo) || 0) - (Number(b.sequenceNo) || 0) || a.id - b.id);
});

const selectedMapping = computed(() => {
  if (ppcId.value) {
    const exactMatch = filteredMappingsByDimension.value.find(m => m.id === Number(ppcId.value));
    if (exactMatch) return exactMatch;
  }
  if ((selectedDimension.value === "PRODUCT" && !selectedPartId.value) || !selectedProcessId.value || !selectedCharacteristicId.value) return null;
  return filteredMappingsByDimension.value.find(m =>
    (selectedDimension.value !== "PRODUCT" || m.partId === Number(selectedPartId.value)) &&
    m.processId === Number(selectedProcessId.value) &&
    m.characteristicId === Number(selectedCharacteristicId.value)
  ) || null;
});

const monitorMapping = computed(() =>
  selectedMapping.value
  || mappings.value.find(m => m.id === Number(ppcId.value))
  || null
);

function formatMonitorMaster(code, name, fallbackName) {
  const displayName = name || fallbackName || "";
  if (code && displayName) return `[${code}] ${displayName}`;
  return displayName || code || "未提供";
}

const monitorProcessLabel = computed(() =>
  chartResult.value?.monitorContext?.lineName
  || chartResult.value?.monitorContext?.processName
  || monitorMapping.value?.process?.processName
  || selectedSummaryRow.value?.lineOrProcessName
  || "未提供"
);
const monitorCharacteristicLabel = computed(() =>
  chartResult.value?.monitorContext?.characteristicName
  || monitorMapping.value?.characteristic?.characteristicName
  || selectedSummaryRow.value?.chartName
  || "未提供"
);
const monitorMachineLabel = computed(() =>
  chartResult.value?.monitorContext?.lineName
  || monitorMapping.value?.machine?.machineName
  || monitorMapping.value?.process?.processName
  || selectedSummaryRow.value?.lineOrProcessName
  || chartResult.value?.monitorContext?.lineCode
  || monitorMapping.value?.machine?.machineCode
  || monitorMapping.value?.process?.processCode
  || "未提供"
);
const monitorSlotLabel = computed(() =>
  chartResult.value?.monitorContext?.slotName
  || chartResult.value?.monitorContext?.tankName
  || monitorMapping.value?.slot?.slotName
  || monitorMapping.value?.tank?.tankName
  || selectedSummaryRow.value?.slotName
  || chartResult.value?.monitorContext?.slotCode
  || chartResult.value?.monitorContext?.tankCode
  || monitorMapping.value?.slot?.slotCode
  || monitorMapping.value?.tank?.tankCode
  || "未提供"
);

function uniqueNonEmptyParts(parts) {
  return [...new Set(parts
    .map(x => (x ?? "").toString().trim())
    .filter(Boolean))];
}

function formatTankLabel(tank) {
  if (!tank) return "";
  return tank.tankName || tank.tankCode || "";
}

const activeChartDisplayName = computed(() => {
  const row = selectedSummaryRow.value;
  const rowTitleParts = uniqueNonEmptyParts([
    row?.lineOrProcessName,
    row?.slotName,
    row?.chartName
  ]);
  if (rowTitleParts.length > 0) return rowTitleParts.join(" - ");

  const mapping = selectedMapping.value;
  const mappingTitleParts = uniqueNonEmptyParts([
    mapping?.process?.processName || mapping?.machine?.machineName,
    formatTankLabel(mapping?.tank),
    mapping?.characteristic?.characteristicName
  ]);
  return mappingTitleParts.join(" - ");
});

const topControlStat = computed(() => {
  const stat = chartResult.value?.statControlLimits || {};
  return stat.iControlLimitsStat || stat.xbarControl || stat.pControlLimitsStat || stat.npControlLimitsStat || null;
});

const bottomControlStat = computed(() => {
  const stat = chartResult.value?.statControlLimits || {};
  return stat.mrControlLimitsStat || stat.rControl || stat.sControl || null;
});

const chartPoints = computed(() => chartResult.value?.chartData?.points || []);

const violationRows = computed(() => {
  return chartPoints.value
    .map((p, idx) => ({ ...p, pointNo: idx + 1 }))
    .filter(p => p.outOfSpec || p.outOfControl || p.violatedRules?.length > 0);
});

const rawPointCount = computed(() => chartResult.value?.rawDataPoints?.length || 0);

const capabilityRows = computed(() => {
  const c = chartResult.value?.capability;
  if (!c) return [];
  return [
    { label: "Ca", value: c.ca, note: "準確度" },
    { label: "Cp", value: c.cp, note: "短期能力" },
    { label: "Cpk", value: c.cpk, note: "短期能力下限" }
  ];
});

const formatNumber = (value, digits = 4) => {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return "N/A";
  return Number(value).toFixed(digits);
};

const formatLimit = (value) => value === null || value === undefined ? "N/A" : Number(value).toFixed(4);
const formatPercentage = (value) => value === null || value === undefined ? "N/A" : `${Number(value).toFixed(2)}%`;
const formatRuleCode = (value) => {
  const match = String(value || "").match(/^(?:Rule|Nelson)(\d+)/i);
  return match ? `Rule ${match[1]}` : String(value || "");
};

const summaryDimensionMap = {
  PROCESS: "PROCESS",
  CHEM: "CHEM",
  PRODUCT: "PRODUCT"
};
const pageGroupType = "CONTROL_CHART";

const getDimensionForMapping = (m) => {
  if (m.chartTypeId) {
    const type = chartTypes.value.find(t => t.id === m.chartTypeId);
    const group = type ? groups.value.find(g => g.id === type.chartGroupId) : null;
    return group?.groupCode || "";
  }
  const scope = normalizeControlScope(m.controlScope, "");
  if (scope === "PROCESS") return "PROCESS";
  if (scope === "CHEM") return "CHEM";
  if (scope === "PRODUCT") return "PRODUCT";
  return m.partId ? "PRODUCT" : "PROCESS";
};

const dimensionOptions = computed(() =>
  groups.value
    .filter(group => group.isEnabled !== false && (group.groupType || pageGroupType) === pageGroupType)
    .map(group => ({
      id: group.groupCode,
      label: group.groupName,
      description: group.description
    }))
);

const dimensionButtonClass = (dimensionId) => {
  if (selectedDimension.value !== dimensionId) {
    return "text-slate-600 dark:text-slate-400 hover:bg-slate-50 dark:hover:bg-slate-800";
  }
  if (dimensionId === "CHEM") return "bg-gradient-to-r from-teal-600 to-emerald-600 text-white font-bold shadow-lg shadow-teal-500/25";
  if (dimensionId === "PRODUCT") return "bg-gradient-to-r from-purple-600 to-fuchsia-600 text-white font-bold shadow-lg shadow-purple-500/25";
  return "bg-gradient-to-r from-blue-600 to-indigo-600 text-white font-bold shadow-lg shadow-blue-500/25";
};

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
  if (newDim === "PRODUCT") {
    selectedPartId.value = "ALL";
    selectedProcessId.value = "ALL";
  } else {
    selectedPartId.value = "";
    selectedProcessId.value = "";
    productQueryMode.value = "DATE";
    productPartId.value = "";
  }
  selectedCharacteristicId.value = "";
  ppcId.value = "";
  uploadBatchId.value = "";
  tableSummaryData.value = null;
});

watch(selectedPartId, (newValue) => {
  if (updatingCascades.value) return;
  ppcId.value = "";
  uploadBatchId.value = "";
  selectedProcessId.value = newValue === "ALL" ? "ALL" : "";
  selectedCharacteristicId.value = "";
  tableSummaryData.value = null;
});

watch(selectedProcessId, () => {
  if (updatingCascades.value) return;
  ppcId.value = "";
  uploadBatchId.value = "";
  selectedCharacteristicId.value = "";
  tableSummaryData.value = null;
});

watch(selectedCharacteristicId, (newVal) => {
  if (updatingCascades.value) return;
  ppcId.value = "";
  uploadBatchId.value = "";
});

async function loadMappings() {
  try {
    const [mapRes, typesRes, groupsRes] = await Promise.all([
      api.get("/part-process-characteristics"),
      api.get("/control-chart-types"),
      api.get("/control-chart-groups", { params: { groupType: pageGroupType } })
    ]);
    
    chartTypes.value = typesRes.data || [];
    groups.value = groupsRes.data || [];
    mappings.value = mapRes.data || [];
    if (!dimensionOptions.value.some(group => group.id === selectedDimension.value)) {
      selectedDimension.value = dimensionOptions.value[0]?.id || "PROCESS";
    }
    
    // Auto-select from query
    const qPpc = Number(route.query.ppcId || route.query.partProcessCharacteristicId);
    
    if (qPpc) {
      const match = mappings.value.find(m => m.id === qPpc);
      if (match) {
        const dim = getDimensionForMapping(match);
        if (!groups.value.some(group => group.groupCode === dim)) {
          router.replace({ path: "/trend-chart", query: route.query });
          return;
        }
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
  if (route.query.startDate) {
    startDate.value = route.query.startDate;
  }
  if (route.query.endDate) {
    endDate.value = route.query.endDate;
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

watch(isMonthlyChartPage, () => {
  if (isMonthlyChartPage.value) productQueryMode.value = "DATE";
  startDate.value = getDefaultStartDateStr();
  endDate.value = getDefaultEndDateStr();
  ppcId.value = "";
  chartResult.value = null;
  tableSummaryData.value = null;
});

watch([reportPeriodType, reportMonth, reportWeek], () => {
  if (!isMonthlyChartPage.value) return;
  if (reportPeriodType.value === "MONTH") {
    if (!reportMonth.value) return;
    startDate.value = getMonthStartStr(reportMonth.value);
    endDate.value = getMonthEndStr(reportMonth.value);
    return;
  }
  const match = /^(\d{4})-W(\d{2})$/.exec(reportWeek.value || "");
  if (!match) return;
  const year = Number(match[1]);
  const week = Number(match[2]);
  const januaryFourth = new Date(year, 0, 4);
  const monday = new Date(januaryFourth);
  monday.setDate(januaryFourth.getDate() - ((januaryFourth.getDay() + 6) % 7) + ((week - 1) * 7));
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  startDate.value = toLocalDateStr(monday);
  endDate.value = toLocalDateStr(sunday);
}, { immediate: true });

watch([showSpecLimits, showControlLimits, showPointValues], () => {
  if (chartResult.value) renderECharts();
});

const getChartQueryParams = (activePpcId) => {
  const params = { ppcId: activePpcId };
  if (uploadBatchId.value) params.uploadBatchId = uploadBatchId.value;
  if (selectedDimension.value === "PRODUCT" && productQueryMode.value === "PART") {
    if (productPartId.value && productPartId.value !== "ALL") params.partId = Number(productPartId.value);
  } else {
    if (startDate.value) params.startDate = startDate.value;
    if (endDate.value) params.endDate = endDate.value;
  }
  return params;
};

async function loadActiveChart() {
  const useProductPart = selectedDimension.value === "PRODUCT" && productQueryMode.value === "PART";
  if (useProductPart) {
    if (!productPartId.value) {
      error.value = "請選擇料號。";
      return;
    }
  } else {
    // Ensure default dates if they are cleared/empty to protect DB from full table scans
    if (!startDate.value) {
      startDate.value = getDefaultStartDateStr();
    }
    if (!endDate.value) {
      endDate.value = getTodayStr();
    }

    if (startDate.value > endDate.value) {
      error.value = "量測起日不可晚於量測迄日。";
      return;
    }

    // Enforce the date filter constraint for the active page.
    const startD = new Date(startDate.value);
    const endD = new Date(endDate.value);
    const diffDays = Math.ceil(Math.abs(endD - startD) / (1000 * 60 * 60 * 24));
    const maxQueryDays = isMonthlyChartPage.value ? 31 : 93;
    if (diffDays > maxQueryDays) {
      error.value = isMonthlyChartPage.value
        ? "月管制圖查詢時間範圍最多不可超過 1 個月。"
        : "查詢時間範圍最多不可超過 3 個月。";
      return;
    }
  }

  if (!ppcId.value && (selectedDimension.value === "PRODUCT" || selectedProcessId.value)) {
    loading.value = true;
    error.value = "";
    chartResult.value = null;
    try {
      const params = { dimension: summaryDimensionMap[selectedDimension.value] || selectedDimension.value, groupType: pageGroupType };
      if (isMonthlyChartPage.value) params.comparisonPeriod = reportPeriodType.value;
      if (useProductPart) {
        if (productPartId.value !== "ALL") params.partId = Number(productPartId.value);
      } else {
        if (startDate.value) params.startDate = startDate.value;
        if (endDate.value) params.endDate = endDate.value;
      }
      if (uploadBatchId.value) params.uploadBatchId = uploadBatchId.value;
      if (selectedProcessId.value && selectedProcessId.value !== "ALL") {
        params.processId = Number(selectedProcessId.value);
      }
      const res = await api.get("/v1/spc/summary", { params });
      tableSummaryData.value = res.data;
      cachedSummaryData.value = res.data;
      summaryDimension.value = selectedDimension.value;
      summaryPartId.value = selectedPartId.value;
      summaryQueryMode.value = productQueryMode.value;
      summaryProductPartId.value = productPartId.value;
    } catch (e) {
      error.value = "無法載入總覽資料：" + getApiErrorMessage(e);
      tableSummaryData.value = null;
    } finally {
      loading.value = false;
    }
    return;
  }

  tableSummaryData.value = null;

  if (!ppcId.value) {
    error.value = "請選擇一個檢驗項目，或選擇全部(All)以檢視總表。";
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

async function drawSingleChart(row) {
  if (row?.groupType === "TREND_CHART" || row?.chartKind === "趨勢圖") {
    router.push({ path: "/trend-chart", query: { ppcId: row.partProcessCharacteristicId } });
    return;
  }

  const match = mappings.value.find(m => m.id === row.partProcessCharacteristicId);
  if (!match) {
    error.value = "找不到此管制項目的設定，可能已停用或刪除。";
    return;
  }

  selectedDimension.value = getDimensionForMapping(match);
  await nextTick();
  syncCascadingDropdowns(match.partId, match.processId, match.characteristicId);
  ppcId.value = String(match.id);
  uploadBatchId.value = "";
  selectedSummaryRow.value = row;
  tableSummaryData.value = null;
  await loadInteractiveChart();
}

async function returnToSummary() {
  chartResult.value = null;
  selectedPoint.value = null;
  selectedPointIndex.value = -1;
  selectedSummaryRow.value = null;
  ppcId.value = "";
  uploadBatchId.value = "";

  if (summaryDimension.value) {
    selectedDimension.value = summaryDimension.value;
    await nextTick();
  }

  productQueryMode.value = summaryQueryMode.value;
  productPartId.value = summaryProductPartId.value;
  selectedPartId.value = summaryDimension.value === "PRODUCT" ? (summaryPartId.value || "ALL") : "";
  await nextTick();
  selectedProcessId.value = "ALL";
  selectedCharacteristicId.value = "";
  await nextTick();
  tableSummaryData.value = cachedSummaryData.value || [];
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

async function updateControlLimitsFromChart() {
  if (!ppcId.value || !topControlStat.value) return;
  savingControlLimits.value = true;
  try {
    await api.put(`/v1/spc/control-limits/${ppcId.value}`, {
      ucl: topControlStat.value.ucl,
      cl: topControlStat.value.cl,
      lcl: topControlStat.value.lcl
    });
    await loadActiveChart();
  } catch (e) {
    alert("更新管制界線失敗：" + getApiErrorMessage(e));
  } finally {
    savingControlLimits.value = false;
  }
}

function prepareOcapForm(point = selectedPoint.value) {
  ocapForm.value = {
    status: point?.alertStatus || "Closed",
    causeCategory: "",
    causeType: "",
    actionType: "",
    rootCause: point?.rootCause || "",
    correctiveAction: point?.correctiveAction || "",
    responsibleUser: point?.responsibleUser || ""
  };
}

async function saveOcapForSelectedPoint() {
  if (!selectedPoint.value || !ppcId.value) return;
  savingOcap.value = true;
  try {
    const value = selectedPoint.value.value ?? selectedPoint.value.xbar;
    await api.post("/v1/spc/ocap", {
      ppcId: Number(ppcId.value),
      variableMeasurementId: selectedPoint.value.variableMeasurementId,
      alertId: selectedPoint.value.alertId,
      actualValue: value,
      ...ocapForm.value
    });
    await loadActiveChart();
  } catch (e) {
    alert("儲存 OCAP 處置失敗：" + getApiErrorMessage(e));
  } finally {
    savingOcap.value = false;
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
  const mapping = selectedMapping.value || {};
  const configuredLimits = data.limits || {};
  const limits = {
    usl: configuredLimits.usl ?? mapping.usl ?? null,
    lsl: configuredLimits.lsl ?? mapping.lsl ?? null,
    target: configuredLimits.target ?? mapping.targetValue ?? mapping.target ?? null,
    ucl: configuredLimits.ucl ?? mapping.ucl ?? null,
    cl: configuredLimits.cl ?? mapping.cl ?? null,
    lcl: configuredLimits.lcl ?? mapping.lcl ?? null
  };
  const stat = data.statControlLimits || {};
  const topStat = stat.iControlLimitsStat
    || stat.xbarControl
    || stat.pControlLimitsStat
    || stat.npControlLimitsStat
    || null;
  const effectiveTopLimits = {
    ucl: topStat?.ucl ?? limits.ucl,
    cl: topStat?.cl ?? limits.cl,
    lcl: topStat?.lcl ?? limits.lcl
  };
  const isDual = type === "XBAR_R" || type === "XBAR_S" || type === "I-MR" || type === "I_MR";
  const topChartTitle = type === "XBAR_R" || type === "XBAR_S"
    ? "Xbar 平均值管制圖"
    : "I 個別值管制圖";
  const bottomChartTitle = type === "XBAR_R"
    ? "R 全距管制圖"
    : type === "XBAR_S"
      ? "S 標準差管制圖"
      : "MR 移動全距圖";
  const primarySeriesName = type === "XBAR_R" || type === "XBAR_S" ? "Xbar" : (isDual ? "Individual" : type);
  const secondarySeriesName = type === "XBAR_R" ? "Range" : type === "XBAR_S" ? "Std Dev" : "Moving Range";

  // Calculate standard deviation (sigma) based on control limits
  const cl = effectiveTopLimits.cl;
  const ucl = effectiveTopLimits.ucl;
  const lcl = effectiveTopLimits.lcl;
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

  if (showSpecLimits.value) {
    addLine(limits.usl, "USL", "#ef4444", "dashed", 2);
    addLine(limits.lsl, "LSL", "#ef4444", "dashed", 2);
    addLine(limits.target, "Target", "#10b981", "solid", 2);
  }

  // Control limits (Static, for Variables)
  const hasDynamicLimits = (data.chartData?.points || []).some(p => p.uclStat != null || p.lclStat != null);
  if (!hasDynamicLimits && showControlLimits.value) {
    addLine(effectiveTopLimits.ucl, "UCL", "#f59e0b", "solid", 1.5);
    addLine(effectiveTopLimits.cl, "CL", "#3b82f6", "solid", 1.5);
    addLine(effectiveTopLimits.lcl, "LCL", "#f59e0b", "solid", 1.5);
  }
  if (hasDynamicLimits && showControlLimits.value) {
    // UCL/CL/LCL are rendered as point-wise stepped line series.
  }

  const topMarkLineObj = markLinesTop.length > 0 ? { symbol: "none", data: markLinesTop, animation: false } : undefined;

  // Bottom marklines
  const markLinesBottom = [];
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
      lineStyle: { color, width: 1.5, type: "solid" },
      label: { formatter: `${name}: ${Number(y).toFixed(3)}`, color, position: "end" }
    });
  };
  if (showControlLimits.value) {
    addLineB(bottomUcl, "UCL", "#f59e0b");
    addLineB(bottomCl, "CL", "#3b82f6");
    addLineB(bottomLcl, "LCL", "#f59e0b");
  }

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
    {
      name: primarySeriesName,
      type: "line",
      xAxisIndex: 0,
      yAxisIndex: 0,
      data: seriesTopData,
      showSymbol: true,
      label: { show: showPointValues.value, formatter: p => formatNumber(p.value, 3), fontSize: 10 },
      markLine: topMarkLineObj,
      markArea: !hasDynamicLimits && showControlLimits.value ? markAreaTop : undefined,
      smooth: false
    }
  ];

  if (hasDynamicLimits && showControlLimits.value) {
    seriesTopList.push({
      name: "UCL",
      type: "line",
      step: "middle",
      symbol: "none",
      xAxisIndex: 0, yAxisIndex: 0,
      lineStyle: { color: "#f59e0b", width: 1.5, type: "solid" },
      endLabel: { show: true, formatter: p => `UCL ${formatNumber(p.value, 3)}`, color: "#d97706", fontSize: 10, fontWeight: "bold" },
      labelLayout: { moveOverlap: "shiftY" },
      data: pointsTop.map(p => p.uclStat != null ? Number(p.uclStat.toFixed(4)) : null)
    });
    seriesTopList.push({
      name: "CL",
      type: "line",
      step: "middle",
      symbol: "none",
      xAxisIndex: 0, yAxisIndex: 0,
      lineStyle: { color: "#3b82f6", width: 1.5, type: "solid" },
      endLabel: { show: true, formatter: p => `CL ${formatNumber(p.value, 3)}`, color: "#2563eb", fontSize: 10, fontWeight: "bold" },
      labelLayout: { moveOverlap: "shiftY" },
      data: pointsTop.map(p => p.clStat != null ? Number(p.clStat.toFixed(4)) : null)
    });
    seriesTopList.push({
      name: "LCL",
      type: "line",
      step: "middle",
      symbol: "none",
      xAxisIndex: 0, yAxisIndex: 0,
      lineStyle: { color: "#f59e0b", width: 1.5, type: "solid" },
      endLabel: { show: true, formatter: p => `LCL ${formatNumber(p.value, 3)}`, color: "#d97706", fontSize: 10, fontWeight: "bold" },
      labelLayout: { moveOverlap: "shiftY" },
      data: pointsTop.map(p => p.lclStat != null ? Math.max(0, Number(p.lclStat.toFixed(4))) : null)
    });
  }

  const option = {
    backgroundColor: "transparent",
    title: isDual
      ? [
          { text: topChartTitle, left: "center", top: 5, textStyle: { fontSize: 13, fontWeight: "bold", color: "#475569" } },
          { text: bottomChartTitle, left: "center", top: "55%", textStyle: { fontSize: 13, fontWeight: "bold", color: "#475569" } }
        ]
      : undefined,
    legend: isDual
      ? [
          {
            top: 27,
            left: "center",
            data: [primarySeriesName, "UCL", "CL", "LCL"],
            itemWidth: 22,
            itemHeight: 9,
            textStyle: { color: "#64748b", fontSize: 11 },
            formatter: name => name === "Individual" ? "量測值 (I)" : name
          },
          {
            top: "58%",
            left: "center",
            data: [secondarySeriesName],
            itemWidth: 22,
            itemHeight: 9,
            textStyle: { color: "#64748b", fontSize: 11 },
            formatter: name => ({
              "Moving Range": "移動全距 (MR)",
              Range: "全距 (R)",
              "Std Dev": "標準差 (S)"
            }[name] || name)
          }
        ]
      : {
          top: 27,
          left: "center",
          itemWidth: 22,
          itemHeight: 9,
          textStyle: { color: "#64748b", fontSize: 11 }
        },
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
            res += `<div class="mt-1.5 px-2 py-0.5 rounded bg-red-900/50 border border-red-500/50 text-red-300 text-[11px] font-bold">⚠️ 西方電氣規則違規：<br>${p.data.violatedRules.map(formatRuleCode).join("<br>")}</div>`;
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
        saveAsImage: { name: `SPC_${type}_Chart` }
      },
      iconStyle: { borderColor: "#64748b" }
    },
    dataZoom: [
      { type: "slider", show: true, xAxisIndex: isDual ? [0, 1] : [0], bottom: 10, borderColor: "#334155", textStyle: { color: "#64748b" } }
    ],
    grid: isDual
      ? [
          { left: 60, right: 80, top: 62, height: "31%", containLabel: true },
          { left: 60, right: 80, top: "65%", height: "21%", containLabel: true }
        ]
      : [{ left: 60, right: 80, top: 40, bottom: 60, containLabel: true }],
    xAxis: isDual
      ? [
          {
            type: "category",
            data: labels,
            boundaryGap: false,
            gridIndex: 0,
            axisLine: { lineStyle: { color: "#64748b" } },
            axisLabel: { show: true, hideOverlap: true, margin: 10 },
            axisTick: { show: true }
          },
          {
            type: "category",
            data: labels,
            boundaryGap: false,
            gridIndex: 1,
            axisLine: { lineStyle: { color: "#64748b" } },
            axisLabel: { hideOverlap: true, margin: 12 }
          }
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
          {
            name: secondarySeriesName,
            type: "line",
            xAxisIndex: 1,
            yAxisIndex: 1,
            data: seriesBottomData,
            showSymbol: true,
            label: { show: showPointValues.value, formatter: p => formatNumber(p.value, 3), fontSize: 10 },
            markLine: bottomMarkLineObj,
            smooth: false
          }
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
      prepareOcapForm(selectedPoint.value);
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
        prepareOcapForm(selectedPoint.value);
      }, 300);
    }
  }

  // Render raw measurement distribution companion chart.
  renderHistogramChart();
}

function buildHistogramBins(values, preferredBins = 12, domainValues = values) {
  if (!values.length) return [];

  const numericDomainValues = domainValues
    .filter(value => value !== null && value !== undefined && !Number.isNaN(Number(value)))
    .map(value => Number(value));
  const min = Math.min(...numericDomainValues);
  const max = Math.max(...numericDomainValues);
  if (min === max) {
    const pad = Math.abs(min) > 0 ? Math.abs(min) * 0.05 : 0.5;
    return [{
      min: min - pad,
      max: max + pad,
      count: values.length,
      values
    }];
  }

  const binCount = Math.max(5, Math.min(preferredBins, Math.ceil(Math.sqrt(values.length)) + 3));
  const width = (max - min) / binCount;
  const bins = Array.from({ length: binCount }, (_, idx) => ({
    min: min + idx * width,
    max: idx === binCount - 1 ? max : min + (idx + 1) * width,
    count: 0,
    values: []
  }));

  values.forEach(value => {
    const rawIndex = Math.floor((value - min) / width);
    const index = Math.max(0, Math.min(binCount - 1, rawIndex));
    bins[index].count += 1;
    bins[index].values.push(value);
  });

  return bins;
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

function renderHistogramChart() {
  if (!histogramChartEl.value || !chartResult.value) return;

  if (histogramChartInstance) {
    histogramChartInstance.dispose();
    histogramChartInstance = null;
  }

  histogramChartInstance = echarts.init(histogramChartEl.value);

  const data = chartResult.value;
  const limits = data.limits || {};
  const controlLimits = topControlStat.value || {};
  const rawPoints = data.rawDataPoints || [];
  const values = rawPoints
    .filter(p => !p.isExcluded && p.value !== null && p.value !== undefined && !Number.isNaN(Number(p.value)))
    .map(p => Number(p.value));

  if (values.length === 0) {
    histogramChartInstance.setOption({
      backgroundColor: "transparent",
      graphic: [{
        type: "text",
        left: "center",
        top: "middle",
        style: { text: "此期間無可納入直方圖的量測值", fontSize: 14, fill: "#94a3b8" }
      }]
    });
    return;
  }

  const histogramBoundaries = [
    limits.lsl,
    limits.usl,
    limits.target,
    limits.lcl,
    limits.cl,
    limits.ucl,
    controlLimits.lcl,
    controlLimits.cl,
    controlLimits.ucl
  ];
  const bins = buildHistogramBins(values, 12, [...values, ...histogramBoundaries]);
  const min = bins.length > 0 ? bins[0].min : 0;
  const max = bins.length > 0 ? bins[bins.length - 1].max : 100;
  const labels = bins.map(bin => `${formatNumber(bin.min, 3)} - ${formatNumber(bin.max, 3)}`);
  const maxCount = Math.max(...bins.map(bin => bin.count));
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length;
  const variance = values.length > 1
    ? values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / (values.length - 1)
    : 0;
  const std = Math.sqrt(variance);

  const markLines = [];
  const findBinIndexForValue = (value) => {
    if (value == null || Number.isNaN(Number(value))) return null;
    const numericValue = Number(value);
    const idx = bins.findIndex(bin => numericValue >= bin.min && numericValue <= bin.max);
    return idx >= 0 ? idx : null;
  };
  const labelSlotsByBin = new Map();
  const addXAxisLine = (value, name, color, style = "dashed") => {
    const idx = findBinIndexForValue(value);
    if (idx === null) return;
    const slot = labelSlotsByBin.get(idx) || 0;
    labelSlotsByBin.set(idx, slot + 1);
    markLines.push({
      name,
      xAxis: idx,
      lineStyle: { color, width: 2, type: style },
      label: {
        formatter: `${name}: ${formatNumber(value, 3)}`,
        color,
        position: slot % 2 === 0 ? "end" : "start",
        offset: [Math.floor(slot / 2) * 18, 0],
        fontSize: 10,
        fontWeight: "bold",
        backgroundColor: "rgba(255, 255, 255, 0.88)",
        borderRadius: 4,
        padding: [2, 4]
      }
    });
  };

  addXAxisLine(limits.lsl, "LSL", "#ef4444", "dashed");
  addXAxisLine(limits.usl, "USL", "#ef4444", "dashed");
  addXAxisLine(controlLimits.lcl ?? limits.lcl, "LCL", "#f59e0b", "solid");
  addXAxisLine(controlLimits.ucl ?? limits.ucl, "UCL", "#f59e0b", "solid");
  addXAxisLine(controlLimits.cl ?? limits.cl, "CL", "#3b82f6", "dotted");
  addXAxisLine(limits.target, "Target", "#10b981", "solid");
  addXAxisLine(mean, "Mean", "#0ea5e9", "dotted");

  const hasNormality = data.normality && data.normalCurve && data.normalCurve.length > 0;

  const xAxisList = [
    {
      type: "category",
      data: labels,
      axisLine: { lineStyle: { color: "#64748b" } },
      axisTick: { alignWithLabel: true, lineStyle: { color: "#334155" } },
      axisLabel: { color: "#94a3b8", fontSize: 10, rotate: labels.length > 8 ? 28 : 0 }
    }
  ];

  if (hasNormality) {
    xAxisList.push({
      type: "value",
      min: min,
      max: max,
      show: false,
      axisLine: { show: false }
    });
  }

  const seriesList = [
    {
      name: "量測值分布",
      type: "bar",
      xAxisIndex: 0,
      data: bins.map(bin => ({
        value: bin.count,
        itemStyle: {
          color: "rgba(245, 158, 11, 0.75)",
          borderRadius: [4, 4, 0, 0]
        }
      })),
      barMaxWidth: 42,
      markLine: markLines.length > 0 ? { symbol: "none", data: markLines, animation: false } : undefined
    }
  ];

  if (hasNormality) {
    seriesList.push({
      name: "常態分佈曲線",
      type: "line",
      xAxisIndex: 1,
      yAxisIndex: 0,
      data: data.normalCurve.map(pt => [pt.x, pt.scaledPdf]),
      showSymbol: false,
      smooth: true,
      lineStyle: { color: "#10b981", width: 2.5 },
      areaStyle: { color: "rgba(16, 185, 129, 0.08)" }
    });
  }

  histogramChartInstance.setOption({
    backgroundColor: "transparent",
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      backgroundColor: "rgba(15, 23, 42, 0.95)",
      borderColor: "#334155",
      textStyle: { color: "#fff", fontSize: 12 },
      formatter: (params) => {
        const point = params.find(p => p.seriesName === "量測值分布");
        if (!point) return "";
        const bin = bins[point.dataIndex];
        if (!bin) return "";

        let normalityInfo = "";
        if (data.normality) {
          const pValStr = data.normality.pValue !== null ? formatNumber(data.normality.pValue, 4) : "N/A";
          const normalStatus = data.normality.isNormal
            ? "<span style='color:#34d399;font-weight:bold'>符合常態</span>"
            : "<span style='color:#f87171;font-weight:bold'>偏離常態</span>";
          normalityInfo = `<div style="margin-top:6px;border-top:1px dashed #334155;padding-top:6px;font-size:11px">常態性檢定 (JB p-val): <strong style="color:#fbbf24">${pValStr}</strong> (${normalStatus})</div>`;
        }

        return [
          `<div style="font-weight:700;border-bottom:1px solid #334155;padding-bottom:4px;margin-bottom:6px">量測值區間</div>`,
          `<div>${formatNumber(bin.min, 4)} &lt;= X ${point.dataIndex === bins.length - 1 ? "&lt;=" : "&lt;"} ${formatNumber(bin.max, 4)}</div>`,
          `<div style="margin-top:4px">區間筆數: <strong style="color:#fbbf24">${bin.count}</strong></div>`,
          `<div style="margin-top:4px;color:#94a3b8;font-size:11px">總樣本數 N=${values.length}, 平均值=${formatNumber(mean, 4)}, 標準差=${formatNumber(std, 4)}</div>`,
          normalityInfo
        ].join("");
      }
    },
    toolbox: {
      feature: {
        restore: {},
        saveAsImage: { name: "Measurement_Histogram" }
      },
      iconStyle: { borderColor: "#64748b" }
    },
    grid: { left: 58, right: 80, top: 48, bottom: 74 },
    xAxis: xAxisList,
    yAxis: {
      type: "value",
      name: "筆數",
      minInterval: 1,
      nameTextStyle: { color: "#94a3b8", fontSize: 11 },
      splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } },
      axisLine: { lineStyle: { color: "#64748b" } },
      axisLabel: { color: "#94a3b8", fontSize: 10 }
    },
    series: seriesList
  });
}

function handleResize() {
  chartInstance?.resize();
  trendChartInstance?.resize();
  histogramChartInstance?.resize();
}

function zoomChartIn() {
  if (!chartInstance) return;
  const zoom = chartInstance.getOption()?.dataZoom?.[0] || {};
  const start = Number.isFinite(Number(zoom.start)) ? Number(zoom.start) : 0;
  const end = Number.isFinite(Number(zoom.end)) ? Number(zoom.end) : 100;
  const span = end - start;
  if (span <= 10) return;
  const nextSpan = Math.max(10, span * 0.7);
  const center = (start + end) / 2;
  chartInstance.dispatchAction({
    type: "dataZoom",
    dataZoomIndex: 0,
    start: Math.max(0, center - nextSpan / 2),
    end: Math.min(100, center + nextSpan / 2)
  });
}

function resetChartZoom() {
  chartInstance?.dispatchAction({
    type: "dataZoom",
    dataZoomIndex: 0,
    start: 0,
    end: 100
  });
}

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);
  chartInstance?.dispose();
  chartInstance = null;
  trendChartInstance?.dispose();
  trendChartInstance = null;
  histogramChartInstance?.dispose();
  histogramChartInstance = null;
});
</script>

<template>
  <section class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex items-center gap-4">
        <div class="p-3 rounded-xl bg-gradient-to-br from-indigo-500 to-violet-600 text-white shadow-lg shadow-indigo-500/20">
          <Activity class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-white">
            {{ isMonthlyChartPage ? 'SPC 週月報表' : 'SPC 即時互動管制圖' }}
          </h1>
          <p class="mt-1 text-xs font-semibold text-slate-400 dark:text-slate-500">
            {{ isMonthlyChartPage ? '依週期檢視 SPC 管制結果與前期比較' : '使用統計管制界線與規則監控製程穩定性' }}
          </p>
        </div>
      </div>
      <button
        v-if="selectedMapping"
        @click="router.push({ path: '/part-process-characteristics', query: { editId: selectedMapping.id } })"
        type="button"
        class="flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl bg-orange-50 hover:bg-orange-100 dark:bg-slate-800 dark:hover:bg-slate-700 text-orange-600 dark:text-orange-400 text-xs font-bold transition-all border border-orange-200 dark:border-slate-700"
        title="編輯此項檢驗基準與規格"
      >
        <Sliders class="w-4 h-4" /> 編輯此基準
      </button>
    </div>

    <!-- 🌟 三大類管制項目維度選擇器 -->
    <div class="flex flex-wrap gap-3 bg-white dark:bg-slate-900 p-2 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <button
        v-for="dimension in dimensionOptions"
        :key="dimension.id"
        @click="selectedDimension = dimension.id"
        :class="dimensionButtonClass(dimension.id)"
        class="flex-1 min-w-40 py-3 px-4 rounded-xl text-center text-xs md:text-sm font-semibold transition-all duration-200"
      >
        {{ dimension.label }}
      </button>
    </div>

    <!-- Selection Controls -->
    <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex flex-wrap items-end gap-2 w-full">
        <!-- Query Conditions -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-5 gap-2 items-end flex-1">
          <div v-if="selectedDimension === 'PRODUCT' && !isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">查詢條件</label>
            <select
              v-model="productQueryMode"
              data-testid="product-query-mode"
              class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500"
            >
              <option value="DATE">量測起迄日</option>
              <option value="PART">料號</option>
            </select>
          </div>

          <div v-if="isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">報表週期</label>
            <select v-model="reportPeriodType" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
              <option value="MONTH">月報</option>
              <option value="WEEK">週報</option>
            </select>
          </div>

          <div v-if="isMonthlyChartPage && reportPeriodType === 'MONTH'" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">報表月份</label>
            <input v-model="reportMonth" type="month" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
          </div>

          <div v-if="isMonthlyChartPage && reportPeriodType === 'WEEK'" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">報表週次</label>
            <input v-model="reportWeek" type="week" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
          </div>

          <div v-if="!isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">量測起日</label>
            <input v-model="startDate" :disabled="selectedDimension === 'PRODUCT' && productQueryMode === 'PART'" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50" />
          </div>

          <div v-if="!isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">量測迄日</label>
            <input v-model="endDate" :disabled="selectedDimension === 'PRODUCT' && productQueryMode === 'PART'" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50" />
          </div>

          <div v-if="selectedDimension === 'PRODUCT' && !isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">料號</label>
            <select
              v-model="productPartId"
              :disabled="productQueryMode !== 'PART'"
              data-testid="product-part-id"
              class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
            >
              <option value="">選擇料號...</option>
              <option value="ALL">全部 (All)</option>
              <option v-for="part in productPartOptions" :key="part.id" :value="part.id">[{{ part.partNo }}] {{ part.partName }}</option>
            </select>
          </div>

          <div v-if="selectedDimension !== 'PROD'" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">線別</label>
            <select v-model="selectedProcessId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
              <option value="">選擇線別...</option>
              <option value="ALL">全部 (All)</option>
              <option v-for="pr in availableProcesses" :key="pr.id" :value="pr.id">{{ pr.processName || pr.processCode }}</option>
            </select>
          </div>

        </div>

        <button @click="loadActiveChart" :disabled="loading" class="flex items-center justify-center gap-2 px-5 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-500 hover:to-violet-500 text-white font-bold shadow-md shadow-indigo-500/20 disabled:opacity-50 transition-all text-sm h-[38px] min-w-32">
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" /> 查詢總表
        </button>
      </div>
    </div>

    <!-- Error Display -->
    <div v-if="error" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ error }}
    </div>

    <div v-if="loading && !chartResult" class="h-64 flex flex-col items-center justify-center space-y-2 text-slate-400">
      <Activity class="w-8 h-8 animate-bounce text-blue-500" />
      <p class="text-sm font-bold">查詢中...</p>
    </div>

    <!-- All-lines summary table -->
    <SpcSummaryTable
      v-if="tableSummaryData && !loading"
      :data="tableSummaryData"
      :loading="loading"
      :comparison-period="isMonthlyChartPage ? reportPeriodType : ''"
      @draw-chart="drawSingleChart"
    />

    <!-- Main Chart & Capability Workspace -->
    <div v-if="chartResult && !loading" class="space-y-5">
      <button
        v-if="cachedSummaryData"
        type="button"
        data-testid="return-to-summary"
        @click="returnToSummary"
        class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-300 border border-blue-200 dark:border-blue-800 hover:bg-blue-50 dark:hover:bg-blue-950/40 font-bold text-xs shadow-sm transition-colors"
      >
        ← 返回已查詢總表
      </button>

      <div
        v-if="activeChartDisplayName"
        data-testid="active-chart-display-name"
        class="px-4 py-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
      >
        <p class="text-[11px] font-black uppercase tracking-wider text-slate-400 dark:text-slate-500">圖表名稱</p>
        <h2 class="mt-1 text-xl font-black text-slate-900 dark:text-white leading-snug">
          {{ activeChartDisplayName }}
        </h2>
      </div>

      <!-- Capability Summary Cards -->
      <div v-if="chartResult.capability" class="grid grid-cols-1 gap-2">
        <div class="p-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">整體標準差 (σ_overall)</p>
          <h3 class="text-lg font-bold mt-0.5 text-slate-800 dark:text-white">
            {{ chartResult.capability.sigmaOverall !== null ? Number(chartResult.capability.sigmaOverall).toFixed(4) : 'N/A' }}
          </h3>
        </div>
      </div>

      <!-- Monitor Detail Summary -->
      <div class="grid grid-cols-1 xl:grid-cols-3 gap-5 p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
        <div class="p-1">
          <h3 class="text-sm font-black text-slate-800 dark:text-white mb-2 flex items-center gap-2">
            <Info class="w-4 h-4 text-blue-500" /> 管制圖監控明細
          </h3>
          <div class="grid grid-cols-2 gap-2 text-xs">
            <div>
              <p class="text-slate-400 font-bold">製程 / 線別</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorProcessLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">藥液 / 檢驗特性</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorCharacteristicLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">機台</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorMachineLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">槽體</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorSlotLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">量測筆數</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ rawPointCount }} 筆</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">異常點數</p>
              <p class="font-semibold" :class="violationRows.length > 0 ? 'text-red-500' : 'text-emerald-500'">{{ violationRows.length }} 點</p>
            </div>
          </div>
        </div>

        <div class="p-1 xl:border-l xl:border-slate-200 xl:dark:border-slate-800 xl:pl-5">
          <div class="flex items-center justify-between mb-2">
            <h3 class="text-sm font-black text-slate-800 dark:text-white flex items-center gap-2">
              <Sliders class="w-4 h-4 text-amber-500" /> 管制界線
            </h3>
          </div>
          <div class="grid grid-cols-2 gap-2 text-xs">
            <div class="space-y-1">
              <p class="text-slate-400 font-bold">I / Xbar</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">UCL {{ formatLimit(topControlStat?.ucl ?? chartResult.limits?.ucl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">CL {{ formatLimit(topControlStat?.cl ?? chartResult.limits?.cl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">LCL {{ formatLimit(topControlStat?.lcl ?? chartResult.limits?.lcl) }}</p>
            </div>
            <div class="space-y-1">
              <p class="text-slate-400 font-bold">MR / R / S</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">UCL {{ formatLimit(bottomControlStat?.ucl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">CL {{ formatLimit(bottomControlStat?.cl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">LCL {{ formatLimit(bottomControlStat?.lcl) }}</p>
            </div>
          </div>
        </div>

        <div class="p-1 xl:border-l xl:border-slate-200 xl:dark:border-slate-800 xl:pl-5">
          <h3 class="text-sm font-black text-slate-800 dark:text-white mb-2 flex items-center gap-2">
            <Activity class="w-4 h-4 text-emerald-500" /> 能力指標
          </h3>
          <div class="grid grid-cols-3 gap-2 text-xs">
            <div v-for="row in capabilityRows" :key="row.label" class="p-1.5 rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700">
              <p class="text-slate-400 font-bold">{{ row.label }}</p>
              <p class="text-base font-black text-slate-800 dark:text-white">{{ formatNumber(row.value, row.label === 'Ppm' ? 1 : 4) }}</p>
              <p class="text-[10px] text-slate-400">{{ row.note }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Chart Display Box -->
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-lg relative">
        <div class="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 dark:border-slate-800 pb-3 mb-2">
          <div class="flex items-center gap-3">
            <span class="px-3 py-1 rounded-full bg-blue-600 text-white text-xs font-bold tracking-widest uppercase shadow-md shadow-blue-500/20">
              {{ ['I-MR', 'I_MR'].includes(String(chartResult.chartType).toUpperCase()) ? 'I-MR' : `${chartResult.chartType} CHART` }}
            </span>
          </div>

          <div class="flex flex-wrap items-center gap-3 text-xs font-semibold text-slate-500">
            <label class="flex items-center gap-1.5 cursor-pointer">
              <input v-model="showSpecLimits" type="checkbox" class="rounded border-slate-300 text-red-600" />
              規格界限（USL / Target / LSL）
            </label>
            <label class="flex items-center gap-1.5 cursor-pointer">
              <input v-model="showControlLimits" type="checkbox" class="rounded border-slate-300 text-amber-600" />
              管制界限（UCL / CL / LCL）
            </label>
            <label class="flex items-center gap-1.5 cursor-pointer">
              <input v-model="showPointValues" type="checkbox" class="rounded border-slate-300 text-emerald-600" />
              各點數值
            </label>
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-red-500 inline-block"></span> 規格/管制界限失控點</span>
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-blue-500 inline-block"></span> 正常管制點位</span>
          </div>
        </div>

        <div v-if="chartResult.subgroupSizeNote" class="mb-2 p-2.5 rounded-lg bg-amber-500/10 border border-amber-500/30 text-amber-600 dark:text-amber-400 text-xs flex items-center gap-2">
          <AlertTriangle class="w-4 h-4 flex-shrink-0" /> {{ chartResult.subgroupSizeNote }}
        </div>

        <div class="relative">
          <div class="absolute top-1 right-12 z-20 flex items-center gap-1">
            <button type="button" @click="zoomChartIn" title="放大" aria-label="放大" class="p-1 text-slate-500 hover:text-blue-600 dark:text-slate-400 dark:hover:text-blue-400">
              <ZoomIn class="w-5 h-5" />
            </button>
            <button type="button" @click="resetChartZoom" title="重設縮放" aria-label="重設縮放" class="p-1 text-slate-500 hover:text-blue-600 dark:text-slate-400 dark:hover:text-blue-400">
              <RotateCcw class="w-5 h-5" />
            </button>
          </div>
          <div ref="chartEl" data-testid="primary-spc-chart" class="h-[620px] w-full min-h-[500px]"></div>
        </div>

        <div class="mt-3 p-3 rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-800">
          <div class="flex items-center justify-between mb-2">
            <h3 class="text-sm font-black text-slate-800 dark:text-white flex items-center gap-2">
              <ShieldAlert class="w-4 h-4 text-red-500" /> SPC 異常 / 違規點清單
            </h3>
            <span class="text-xs font-bold" :class="violationRows.length > 0 ? 'text-red-500' : 'text-emerald-500'">
              {{ violationRows.length > 0 ? `${violationRows.length} 點需確認` : '目前無異常點' }}
            </span>
          </div>
          <div v-if="violationRows.length === 0" class="text-xs text-slate-500">
            沒有超規、失控或觸發 8 大管制規則的點位。
          </div>
          <div v-else class="overflow-x-auto">
            <table class="min-w-full text-xs">
              <thead class="text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <tr>
                  <th class="text-left py-2 pr-3">點位</th>
                  <th class="text-left py-2 pr-3">時間</th>
                  <th class="text-left py-2 pr-3">量測值</th>
                  <th class="text-left py-2 pr-3">類型</th>
                  <th class="text-left py-2 pr-3">規則</th>
                  <th class="text-left py-2 pr-3">處置狀態</th>
                  <th class="text-left py-2 pr-3">操作</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-200 dark:divide-slate-800 text-slate-700 dark:text-slate-200">
                <tr v-for="row in violationRows" :key="`${row.pointNo}-${row.measuredAt}`">
                  <td class="py-2 pr-3 font-bold">#{{ row.pointNo }}</td>
                  <td class="py-2 pr-3 whitespace-nowrap">{{ row.measuredAt ? new Date(row.measuredAt).toLocaleString('zh-TW', { hour12: false }) : '-' }}</td>
                  <td class="py-2 pr-3 font-mono">{{ formatNumber(row.value ?? row.xbar, 4) }}</td>
                  <td class="py-2 pr-3">
                    <span v-if="row.outOfSpec" class="px-2 py-0.5 rounded bg-red-100 text-red-700 dark:bg-red-950/50 dark:text-red-300 font-bold">OOS</span>
                    <span v-else-if="row.outOfControl" class="px-2 py-0.5 rounded bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300 font-bold">OOC</span>
                  </td>
                  <td class="py-2 pr-3 max-w-md">
                    <span v-if="row.violatedRules?.length">{{ row.violatedRules.map(formatRuleCode).join('、') }}</span>
                    <span v-else class="text-slate-400">界限判定</span>
                  </td>
                  <td class="py-2 pr-3">{{ row.alertStatus || (row.rootCause || row.correctiveAction ? 'Handled' : '未處置') }}</td>
                  <td class="py-2 pr-3">
                    <button
                      @click="selectedPoint = row; selectedPointIndex = row.pointNo - 1; prepareOcapForm(row)"
                      class="px-2.5 py-1 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold"
                    >
                      選取處置
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- 量測值分布直方圖 (Histogram) -->
        <div class="mt-5 pt-5 border-t border-slate-200 dark:border-slate-800">
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-2 mb-3">
            <h3 class="text-base font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
              <BarChart3 class="w-5 h-5 text-amber-500" />
              量測值分布直方圖 (Raw Measurements Histogram)
            </h3>
            <div class="flex flex-wrap items-center gap-3 text-xs font-semibold text-slate-500">
              <span class="flex items-center gap-1.5"><span class="w-5 h-0.5 bg-red-500 border-t border-dashed border-red-500 inline-block"></span>規格上下線</span>
              <span class="flex items-center gap-1.5"><span class="w-5 h-0.5 bg-amber-500 inline-block"></span>管制上下線</span>
              <span class="text-slate-400">已剔除資料不納入統計</span>
            </div>
          </div>

          <div class="grid grid-cols-2 md:grid-cols-4 xl:grid-cols-8 gap-2 mb-3 p-3 bg-slate-50 dark:bg-slate-800/40 rounded-xl border border-slate-200 dark:border-slate-800 text-xs font-semibold">
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">納入樣本數</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ (chartResult.rawDataPoints || []).filter(p => !p.isExcluded && p.value !== null && p.value !== undefined).length }} 筆
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">USL / LSL</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ formatLimit(chartResult.limits?.usl) }} / {{ formatLimit(chartResult.limits?.lsl) }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">UCL / LCL</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ formatLimit(topControlStat?.ucl ?? chartResult.limits?.ucl) }} / {{ formatLimit(topControlStat?.lcl ?? chartResult.limits?.lcl) }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">Target</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ formatLimit(chartResult.limits?.target) }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">偏態 (Skewness)</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ chartResult.normality ? formatNumber(chartResult.normality.skewness, 4) : 'N/A' }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">峰態 (Kurtosis)</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ chartResult.normality ? formatNumber(chartResult.normality.kurtosis, 4) : 'N/A' }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">常態性檢定 (JB p-val)</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ chartResult.normality && chartResult.normality.pValue !== null ? formatNumber(chartResult.normality.pValue, 4) : 'N/A' }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">常態判定</span>
              <div>
                <span v-if="chartResult.normality?.pValue === null" class="px-2 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                  無法檢定
                </span>
                <span v-else-if="chartResult.normality?.isNormal" class="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                  符合常態
                </span>
                <span v-else-if="chartResult.normality" class="px-2 py-0.5 rounded text-[10px] font-bold bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 animate-pulse">
                  偏離常態
                </span>
                <span v-else class="text-slate-400 font-mono">N/A</span>
              </div>
            </div>
          </div>
          <div ref="histogramChartEl" class="h-[300px] w-full min-h-[240px]"></div>
        </div>

        <!-- Point Detail Drilldown Card -->
        <div v-if="selectedPoint" class="mt-4 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 shadow-inner transition-all duration-300">
          <div class="flex items-center justify-between border-b border-slate-200 dark:border-slate-700 pb-2 mb-3">
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
                  {{ formatRuleCode(rule) }}
                </span>
              </div>
            </div>
          </div>

          <div class="mt-5 p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700">
            <div class="flex items-center justify-between gap-3 mb-4">
              <h4 class="text-sm font-black text-slate-800 dark:text-white flex items-center gap-2">
                <CheckCircle2 class="w-4 h-4 text-emerald-500" /> OCAP 異常原因與處置
              </h4>
              <span class="text-xs font-semibold text-slate-400">
                {{ selectedPoint.alertId ? `Alert #${selectedPoint.alertId}` : '尚未建立 Alert，儲存後會自動建立' }}
              </span>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">原因分類</label>
                <select v-model="ocapForm.causeCategory" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="">選擇原因分類...</option>
                  <option v-for="item in ocapCauseCategories" :key="item" :value="item">{{ item }}</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">原因類型</label>
                <select v-model="ocapForm.causeType" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="">選擇原因類型...</option>
                  <option v-for="item in ocapCauseTypes" :key="item" :value="item">{{ item }}</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">處置類型</label>
                <select v-model="ocapForm.actionType" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="">選擇處置類型...</option>
                  <option v-for="item in ocapActionTypes" :key="item" :value="item">{{ item }}</option>
                </select>
              </div>
              <div class="md:col-span-3">
                <label class="block text-[11px] font-bold text-slate-400 mb-1">發生原因說明</label>
                <textarea v-model="ocapForm.rootCause" rows="2" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white" placeholder="例如：補藥後濃度尚未均勻、槽液溫度偏移、檢測輸入需確認"></textarea>
              </div>
              <div class="md:col-span-3">
                <label class="block text-[11px] font-bold text-slate-400 mb-1">矯正 / 處置內容</label>
                <textarea v-model="ocapForm.correctiveAction" rows="2" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white" placeholder="例如：重新取樣確認、調整藥液參數、通知工程師覆核"></textarea>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">狀態</label>
                <select v-model="ocapForm.status" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="Open">Open</option>
                  <option value="InProgress">InProgress</option>
                  <option value="Closed">Closed</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">責任人員</label>
                <input v-model="ocapForm.responsibleUser" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white" placeholder="輸入人員代號或姓名" />
              </div>
              <div class="flex items-end">
                <button
                  @click="saveOcapForSelectedPoint"
                  :disabled="savingOcap"
                  class="w-full px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-black disabled:opacity-50"
                >
                  {{ savingOcap ? '儲存中...' : '儲存 OCAP 處置' }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
