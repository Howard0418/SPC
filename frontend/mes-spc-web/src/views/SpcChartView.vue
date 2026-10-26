<script setup>
import { formatNormalityPValue, normalityConclusion, normalityUnavailableReason } from "../utils/normalityPresentation";
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
import { captureChartPresentation, translateChartPresentation, escapeChartText } from "../i18n/chartPresentation";
import { createChartLabelTranslator } from "../i18n/chartLabels";
import { useRoute, useRouter } from "vue-router";
import * as echarts from "echarts";
import { api, getApiErrorMessage, getApiErrorDisplay } from "../api/client";
import { normalizeControlScope } from "../utils/controlScope";
import { resolveActiveRawDataPpcId } from "../utils/spcRawDataDownload";
import { buildHistogramBins, findHistogramBinIndex } from "../utils/histogram";
import {
  useSingleChemicalLine,
  stageLabel,
  normalizeChemicalShift,
  isRecordedChemicalShift,
  shiftLabel,
  chartShiftLabel,
  chartStageShiftLabel,
  chemicalDateKey,
  buildPairedShiftLookup
} from "../utils/chemicalChart";
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
  RotateCcw,
  StickyNote,
  CheckCircle2
} from "lucide-vue-next";
import SpcSummaryTable from "../components/SpcSummaryTable.vue";
import ChemicalCalculationDetails from "../components/ChemicalCalculationDetails.vue";
import ChartSegmentDetails from "../components/ChartSegmentDetails.vue";
import LocalePicker from "../components/LocalePicker.vue";

const route = useRoute();
const router = useRouter();
const { t, locale } = useI18n({ useScope: "global" });
const analysisSectionByPath = {
  "/process-analysis/capability": "process-capability-analysis",
  "/process-analysis/violations": "violation-analysis",
};

function focusRequestedAnalysis() {
  const sectionId = analysisSectionByPath[route.path];
  if (!sectionId) return;
  window.requestAnimationFrame(() => document.getElementById(sectionId)?.scrollIntoView({ behavior: "smooth", block: "start" }));
}
const isMonthlyChartPage = computed(() => route.path.startsWith("/monthly-control-chart"));
const pageHeading = computed(() => ({
  "/process-analysis/capability": t('spc.chartQuery.capabilityHeading'),
  "/process-analysis/violations": t('spc.chartQuery.violationsHeading'),
}[route.path] || (isMonthlyChartPage.value ? t('spc.chartQuery.reportHeading') : t('spc.chartQuery.heading'))));
const pageDescription = computed(() => ({
  "/process-analysis/capability": t('spc.chartQuery.capabilityDescription'),
  "/process-analysis/violations": t('spc.chartQuery.violationsDescription'),
}[route.path] || (isMonthlyChartPage.value ? t('spc.chartQuery.reportDescription') : t('spc.chartQuery.description'))));

// SPC master data source: PartProcessCharacteristics
const mappings = ref([]);
const batchId = ref("");
const ppcId = ref("");
const uploadBatchId = ref("");
const loading = ref(false);
const rawDataDownloading = ref(false);
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
const normalityMethod = "AD";
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
const segmentDetails = ref(null);

// Click Drill-down State
const selectedPoint = ref(null);
const selectedPointIndex = ref(-1);
const pointContextMenu = ref({
  visible: false,
  x: 0,
  y: 0,
  point: null
});
const pointExclusionSaving = ref(false);
const excludedPoints = ref([]);
const excludedPointsLoading = ref(false);
const pointRemarks = ref([]);
const pointRemarkSaving = ref(false);
const pointRemarkModal = ref({
  visible: false,
  text: "",
  point: null,
  existing: null
});
const showExcludedPointsPanel = ref(false);
const showSpecLimits = ref(false);
const showControlLimits = ref(false);
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
      m.processId &&
      m.process &&
      (m.process.processName || m.process.processCode) &&
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
  return displayName || code || t('spc.chartQuery.notProvided');
}

const monitorProcessLabel = computed(() =>
  chartResult.value?.monitorContext?.lineName
  || chartResult.value?.monitorContext?.processName
  || monitorMapping.value?.process?.processName
  || selectedSummaryRow.value?.lineOrProcessName
  || t('spc.chartQuery.notProvided')
);
const monitorCharacteristicLabel = computed(() =>
  chartResult.value?.monitorContext?.characteristicName
  || monitorMapping.value?.characteristic?.characteristicName
  || selectedSummaryRow.value?.chartName
  || t('spc.chartQuery.notProvided')
);
const monitorMachineLabel = computed(() =>
  chartResult.value?.monitorContext?.lineName
  || monitorMapping.value?.machine?.machineName
  || monitorMapping.value?.process?.processName
  || selectedSummaryRow.value?.lineOrProcessName
  || chartResult.value?.monitorContext?.lineCode
  || monitorMapping.value?.machine?.machineCode
  || monitorMapping.value?.process?.processCode
  || t('spc.chartQuery.notProvided')
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
  || t('spc.chartQuery.notProvided')
);

const monitorSpecLimits = computed(() => {
  const configuredLimits = chartResult.value?.limits || {};
  const mapping = monitorMapping.value || {};
  return {
    usl: configuredLimits.usl ?? mapping.usl ?? null,
    target: configuredLimits.target ?? mapping.targetValue ?? mapping.target ?? null,
    lsl: configuredLimits.lsl ?? mapping.lsl ?? null
  };
});

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
const isEtchAverageChart = computed(() => {
  const mapping = selectedMapping.value || {};
  const characteristic = mapping.characteristic || {};
  const code = String(characteristic.characteristicCode || characteristic.code || characteristic.itemCode || mapping.characteristicCode || mapping.code || '').toUpperCase();
  const name = String(characteristic.characteristicName || characteristic.name || mapping.characteristicName || '').toLowerCase();
  return ['ETCH_A_AVG', 'ETCH_B_AVG'].includes(code) || (name.includes('咬蝕量') && name.includes('平均')) || monitorCharacteristicLabel.value.includes('咬蝕量');
});
const displayedMeasurementCount = computed(() => isEtchAverageChart.value ? chartPoints.value.length : rawPointCount.value);
const activePpcIdValue = computed(() => resolveActiveRawDataPpcId({
  ppcId: ppcId.value,
  selectedSummaryRow: selectedSummaryRow.value,
  selectedMapping: selectedMapping.value
}));
const activeExclusionCount = computed(() => excludedPoints.value.length);

const capabilityRows = computed(() => {
  const c = chartResult.value?.capability;
  if (!c) return [];
  return [
    { label: "Ca", value: c.ca },
    { label: "Cp", value: c.pp },
    { label: "Cpk", value: c.ppk }
  ];
});

const formatNumber = (value, digits = 4) => {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return "N/A";
  return Number(value).toFixed(digits);
};

const formatLimit = (value) => value === null || value === undefined ? "N/A" : Number(value).toFixed(4);
const formatSpecLimit = (value) => value === null || value === undefined ? "N/A" : Number(value).toFixed(2);
const formatPercentage = (value) => value === null || value === undefined ? "N/A" : `${Number(value).toFixed(2)}%`;
const readField = (row, camelName, pascalName) => row?.[camelName] ?? row?.[pascalName];
const formatExclusionState = (row) => {
  const state = readField(row, "state", "State");
  if (state === "ExcludedHidden") return t('spc.chartQuery.hidden');
  if (state === "ExcludedVisible") return t('spc.chartQuery.visible');
  return state || t('spc.chartQuery.excludedFallback');
};
const formatExclusionPointLabel = (row) => {
  const variableId = readField(row, "variableMeasurementId", "VariableMeasurementId");
  const attributeId = readField(row, "attributeMeasurementId", "AttributeMeasurementId");
  const pointKey = readField(row, "pointKey", "PointKey");
  if (variableId) return `Variable #${variableId}`;
  if (attributeId) return `Attribute #${attributeId}`;
  return pointKey || t('spc.chartQuery.aggregatePoint');
};
const formatExclusionUpdatedAt = (row) => {
  const value = readField(row, "updatedAt", "UpdatedAt") || readField(row, "createdAt", "CreatedAt");
  if (!value) return "";
  return new Date(value).toLocaleString("zh-TW", { month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", hour12: false });
};
const getPointIdentityKey = (point) => {
  if (!point) return "";
  if (point.variableMeasurementId) return `v:${point.variableMeasurementId}`;
  if (point.attributeMeasurementId) return `a:${point.attributeMeasurementId}`;
  if (point.pointKey) return `k:${point.pointKey}`;
  return "";
};
const getRemarkIdentityKey = (row) => {
  const variableId = readField(row, "variableMeasurementId", "VariableMeasurementId");
  const attributeId = readField(row, "attributeMeasurementId", "AttributeMeasurementId");
  const pointKey = readField(row, "pointKey", "PointKey");
  if (variableId) return `v:${variableId}`;
  if (attributeId) return `a:${attributeId}`;
  if (pointKey) return `k:${pointKey}`;
  return "";
};
const findPointRemark = (point) => {
  const key = getPointIdentityKey(point);
  if (!key) return null;
  return pointRemarks.value.find(row => getRemarkIdentityKey(row) === key) || null;
};
function applyPointRemarksToChart() {
  const points = chartResult.value?.rawDataPoints || [];
  points.forEach(point => {
    const row = findPointRemark(point);
    point.pointRemarkId = row ? readField(row, "id", "Id") : null;
    point.pointRemark = row ? readField(row, "remark", "Remark") : "";
  });
}
const formatRuleCode = (value) => {
  const match = String(value || "").match(/^(?:Rule|Nelson)(\d+)/i);
  return match ? `Rule ${match[1]}` : String(value || "");
};

const summaryDimensionMap = {
  PROCESS: "PROCESS",
  CHEM: "CHEM",
  PRODUCT: "PRODUCT",
  DUST: "DUST"
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
  if (scope === "DUST") return "DUST";
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
  if (dimensionId === "DUST") return "bg-gradient-to-r from-amber-500 to-orange-600 text-white font-bold shadow-lg shadow-amber-500/25";
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
    error.value = { key: 'spc.chartQuery.loadMappingsFailed', args: { error: getApiErrorDisplay(e) } };
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
  const params = { ppcId: activePpcId, normalityMethod: normalityMethod };
  if (uploadBatchId.value) params.uploadBatchId = uploadBatchId.value;
  if (batchId.value) params.batchNo = batchId.value;
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
      error.value = { key: 'spc.chartQuery.choosePartError' };
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
      error.value = { key: 'spc.chartQuery.dateOrder' };
      return;
    }

    // Enforce the date filter constraint for the active page.
    const startD = new Date(startDate.value);
    const endD = new Date(endDate.value);
    const diffDays = Math.ceil(Math.abs(endD - startD) / (1000 * 60 * 60 * 24));
    const maxQueryDays = isMonthlyChartPage.value ? 31 : 93;
    if (diffDays > maxQueryDays) {
      error.value = isMonthlyChartPage.value
        ? { key: 'spc.chartQuery.monthRange' }
        : { key: 'spc.chartQuery.queryRange' };
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
      error.value = { key: 'spc.chartQuery.loadSummaryFailed', args: { error: getApiErrorDisplay(e) } };
      tableSummaryData.value = null;
    } finally {
      loading.value = false;
    }
    return;
  }

  tableSummaryData.value = null;

  if (!ppcId.value) {
    error.value = { key: 'spc.chartQuery.chooseItemError' };
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
    await loadExcludedPoints(ppcId.value);
    await loadPointRemarks(ppcId.value);
    loading.value = false;
    await nextTick();
    renderECharts();
    focusRequestedAnalysis();
  } catch (e) {
    if (e?.response?.status === 404) {
      error.value = { key: 'spc.chartQuery.batchNotFound' };
    } else {
      error.value = getApiErrorDisplay(e);
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
    await loadExcludedPoints(activePpcId);
    await loadPointRemarks(activePpcId);
    loading.value = false;
    await nextTick();
    renderECharts();
    focusRequestedAnalysis();
  } catch (e) {
    if (e?.response?.status === 404) {
      error.value = { key: 'spc.chartQuery.chartNotFound' };
    } else {
      error.value = getApiErrorDisplay(e);
    }
  } finally {
    loading.value = false;
  }
}

async function loadExcludedPoints(activePpcId = activePpcIdValue.value) {
  if (!activePpcId) {
    excludedPoints.value = [];
    return;
  }
  excludedPointsLoading.value = true;
  try {
    const res = await api.get("/v1/spc/point-exclusions", {
      params: { ppcId: Number(activePpcId) }
    });
    excludedPoints.value = res.data?.data || [];
  } catch (e) {
    error.value = { key: 'spc.chartQuery.loadExcludedFailed', args: { error: getApiErrorDisplay(e) } };
    excludedPoints.value = [];
  } finally {
    excludedPointsLoading.value = false;
  }
}

async function loadPointRemarks(activePpcId = activePpcIdValue.value) {
  if (!activePpcId) {
    pointRemarks.value = [];
    return;
  }
  try {
    const res = await api.get("/v1/spc/point-remarks", {
      params: { ppcId: Number(activePpcId) }
    });
    pointRemarks.value = res.data?.data || [];
    applyPointRemarksToChart();
  } catch (e) {
    error.value = { key: 'spc.chartQuery.loadRemarksFailed', args: { error: getApiErrorDisplay(e) } };
    pointRemarks.value = [];
  }
}

async function drawSingleChart(row) {
  if (row?.groupType === "TREND_CHART" || row?.chartKind === "趨勢圖") {
    router.push({ path: "/trend-chart", query: { ppcId: row.partProcessCharacteristicId } });
    return;
  }

  const match = mappings.value.find(m => m.id === row.partProcessCharacteristicId);
  if (!match) {
    error.value = { key: 'spc.chartQuery.mappingNotFound' };
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

async function downloadRawData() {
  const activePpcId = activePpcIdValue.value;
  if (!activePpcId) {
    error.value = { key: 'spc.chartQuery.queryBeforeDownload' };
    return;
  }

  rawDataDownloading.value = true;
  try {
    const res = await api.get("/v1/spc/chart/raw-data", {
      params: getChartQueryParams(activePpcId),
      responseType: "blob"
    });
    const disposition = res.headers?.["content-disposition"] || "";
    const match = disposition.match(/filename\*=UTF-8''([^;]+)|filename="?([^"]+)"?/i);
    const fileName = decodeURIComponent(match?.[1] || match?.[2] || `SPC_RawData_${activePpcId}_${new Date().toISOString().slice(0, 10)}.csv`);
    const url = URL.createObjectURL(new Blob([res.data], { type: "text/csv;charset=utf-8" }));
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  } catch (e) {
    error.value = { key: 'spc.chartQuery.downloadFailed', args: { error: getApiErrorDisplay(e) } };
  } finally {
    rawDataDownloading.value = false;
  }
}

async function returnToSummary() {
  chartResult.value = null;
  selectedPoint.value = null;
  selectedPointIndex.value = -1;
  excludedPoints.value = [];
  pointRemarks.value = [];
  showExcludedPointsPanel.value = false;
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
    alert(t('spc.chartActions.batchFailed', { error: getApiErrorMessage(e) }));
  }
}

function getPointExclusionPayload(point, state) {
  const ppc = activePpcIdValue.value;
  if (!ppc || !point) return null;
  const variableMeasurementId = point.variableMeasurementId ?? null;
  const attributeMeasurementId = point.attributeMeasurementId ?? null;
  if (!variableMeasurementId && !attributeMeasurementId) return null;
  return {
    partProcessCharacteristicId: ppc,
    pointScope: attributeMeasurementId ? "AttributeMeasurement" : "VariableMeasurement",
    variableMeasurementId,
    attributeMeasurementId,
    measurementBatchId: point.measurementBatchId ?? null,
    pointKey: null,
    state,
    reason: "管制圖右鍵設定"
  };
}

function getPointRemarkPayload(point, remark) {
  const ppc = activePpcIdValue.value;
  if (!ppc || !point) return null;
  const variableMeasurementId = point.variableMeasurementId ?? null;
  const attributeMeasurementId = point.attributeMeasurementId ?? null;
  if (!variableMeasurementId && !attributeMeasurementId) return null;
  return {
    partProcessCharacteristicId: ppc,
    pointScope: attributeMeasurementId ? "AttributeMeasurement" : "VariableMeasurement",
    variableMeasurementId,
    attributeMeasurementId,
    measurementBatchId: point.measurementBatchId ?? null,
    pointKey: null,
    remark
  };
}

function hidePointContextMenu() {
  pointContextMenu.value.visible = false;
  pointContextMenu.value.point = null;
}

function openPointRemarkModal() {
  const point = pointContextMenu.value.point || selectedPoint.value;
  const payload = getPointRemarkPayload(point, "");
  if (!payload) {
    alert(t('spc.chartActions.noRemarkId'));
    hidePointContextMenu();
    return;
  }
  const existing = findPointRemark(point);
  pointRemarkModal.value = {
    visible: true,
    text: existing ? readField(existing, "remark", "Remark") || "" : "",
    point,
    existing
  };
  hidePointContextMenu();
}

function closePointRemarkModal() {
  pointRemarkModal.value = { visible: false, text: "", point: null, existing: null };
}

async function savePointRemark() {
  const payload = getPointRemarkPayload(pointRemarkModal.value.point, pointRemarkModal.value.text);
  if (!payload) return;
  pointRemarkSaving.value = true;
  try {
    await api.put("/v1/spc/point-remarks", payload);
    await loadPointRemarks(payload.partProcessCharacteristicId);
    await loadActiveChart();
    closePointRemarkModal();
  } catch (e) {
    alert(t('spc.chartActions.remarkFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointRemarkSaving.value = false;
  }
}

async function clearPointRemark() {
  const row = pointRemarkModal.value.existing || findPointRemark(pointRemarkModal.value.point);
  const remarkId = row ? readField(row, "id", "Id") : null;
  const payload = getPointRemarkPayload(pointRemarkModal.value.point, "");
  if (!payload) return;
  pointRemarkSaving.value = true;
  try {
    if (remarkId) {
      await api.delete(`/v1/spc/point-remarks/${remarkId}`);
    } else {
      await api.put("/v1/spc/point-remarks", payload);
    }
    await loadPointRemarks(payload.partProcessCharacteristicId);
    await loadActiveChart();
    closePointRemarkModal();
  } catch (e) {
    alert(t('spc.chartActions.clearFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointRemarkSaving.value = false;
  }
}

async function setPointExclusion(state) {
  const payload = getPointExclusionPayload(pointContextMenu.value.point || selectedPoint.value, state);
  if (!payload) {
    alert(t('spc.chartActions.noExcludeId'));
    hidePointContextMenu();
    return;
  }
  pointExclusionSaving.value = true;
  try {
    await api.put("/v1/spc/point-exclusions", payload);
    hidePointContextMenu();
    await loadExcludedPoints(payload.partProcessCharacteristicId);
    await loadActiveChart();
  } catch (e) {
    alert(t('spc.chartActions.excludeFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointExclusionSaving.value = false;
  }
}

async function restorePointExclusion() {
  const point = pointContextMenu.value.point || selectedPoint.value;
  const payload = getPointExclusionPayload(point, "ExcludedVisible");
  if (!payload) {
    alert(t('spc.chartActions.noRestoreId'));
    hidePointContextMenu();
    return;
  }
  pointExclusionSaving.value = true;
  try {
    const params = {
      ppcId: payload.partProcessCharacteristicId,
      pointScope: payload.pointScope
    };
    if (payload.variableMeasurementId) params.variableMeasurementId = payload.variableMeasurementId;
    if (payload.attributeMeasurementId) params.attributeMeasurementId = payload.attributeMeasurementId;
    const res = await api.get("/v1/spc/point-exclusions", { params });
    const row = (res.data?.data || [])[0];
    const exclusionId = row?.id ?? row?.Id;
    if (exclusionId) {
      await api.delete(`/v1/spc/point-exclusions/${exclusionId}`);
    }
    hidePointContextMenu();
    await loadActiveChart();
  } catch (e) {
    alert(t('spc.chartActions.restoreFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointExclusionSaving.value = false;
  }
}

async function restoreExcludedPoint(row) {
  const exclusionId = row?.id ?? row?.Id;
  if (!exclusionId) return;
  pointExclusionSaving.value = true;
  try {
    await api.delete(`/v1/spc/point-exclusions/${exclusionId}`);
    await loadActiveChart();
  } catch (e) {
    alert(t('spc.chartActions.restoreRowFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointExclusionSaving.value = false;
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
    alert(t('spc.chartActions.limitsFailed', { error: getApiErrorMessage(e) }));
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
    alert(t('spc.chartActions.ocapFailed', { error: getApiErrorMessage(e) }));
  } finally {
    savingOcap.value = false;
  }
}


const chartPresentations = new WeakMap();
const chartLabel = createChartLabelTranslator(t);
const htmlChartLabel = value => escapeChartText(chartLabel(value));
function localizeChart(instance) {
 if (!instance || instance.isDisposed()) return;
 if(!chartPresentations.has(instance))chartPresentations.set(instance,captureChartPresentation(instance.getOption()));
 instance.setOption(translateChartPresentation(chartPresentations.get(instance),chartLabel));
}
watch(locale,async()=>{await nextTick();for(const instance of [chartInstance,trendChartInstance,histogramChartInstance])localizeChart(instance)});

function renderECharts() {
  if (!chartEl.value || !chartResult.value) return;

  if (chartInstance) {
    chartInstance.dispose();
    chartInstance = null;
  }
  hidePointContextMenu();

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
  const rawTopPoints = data.chartData?.points || [];
  const topLimitValues = [];
  const addTopLimitValue = value => {
    if (value == null || Number.isNaN(Number(value))) return;
    topLimitValues.push(Number(value));
  };
  const addLine = (y, name, color, style = "solid", width = 1.5) => {
    if (y == null || Number.isNaN(y)) return;
    addTopLimitValue(y);
    markLinesTop.push({
      name,
      yAxis: y,
      lineStyle: { color, width, type: style },
      label: { formatter: `${name}: ${Number(y).toFixed(3)}`, color, position: "end" }
    });
  };
  const addSegmentedSpecLines = () => {
    const segments = Array.isArray(data.specLimitSegments) ? data.specLimitSegments : [];
    if (!segments.length || !rawTopPoints.length) return false;
    const findStartIndex = (startDate) => {
      const start = startDate ? new Date(startDate).getTime() : -Infinity;
      const idx = rawTopPoints.findIndex(p => p.measuredAt && new Date(p.measuredAt).getTime() >= start);
      return idx;
    };
    const findEndIndex = (endDate) => {
      if (!endDate) return rawTopPoints.length - 1;
      const end = new Date(endDate).getTime();
      for (let i = rawTopPoints.length - 1; i >= 0; i--) {
        if (rawTopPoints[i].measuredAt && new Date(rawTopPoints[i].measuredAt).getTime() <= end) return i;
      }
      return -1;
    };
    const addSegmentLine = (segment, field, name, color, style, width) => {
      const value = segment[field];
      if (value == null || Number.isNaN(Number(value))) return;
      const startIndex = findStartIndex(segment.startDate);
      const endIndex = findEndIndex(segment.endDate);
      if (startIndex < 0 || endIndex < 0 || startIndex > endIndex) return;
      addTopLimitValue(value);
      markLinesTop.push([
        {
          name,
          segmentId: segment.id,
          xAxis: startIndex,
          yAxis: Number(value),
          lineStyle: { color, width, type: style },
          label: { formatter: `${name}: ${Number(value).toFixed(3)}`, color, position: "insideEndTop" }
        },
        { xAxis: endIndex, yAxis: Number(value) }
      ]);
    };
    segments.forEach(segment => {
      addSegmentLine(segment, "usl", "USL", "#ef4444", "dashed", 2);
      addSegmentLine(segment, "lsl", "LSL", "#ef4444", "dashed", 2);
      addSegmentLine(segment, "target", "Target", "#10b981", "solid", 2);
    });
    return true;
  };

  if (showSpecLimits.value) {
    const hasSegmentedSpecLines = addSegmentedSpecLines();
    if (!hasSegmentedSpecLines) {
      addLine(limits.usl, "USL", "#ef4444", "dashed", 2);
      addLine(limits.lsl, "LSL", "#ef4444", "dashed", 2);
      addLine(limits.target, "Target", "#10b981", "solid", 2);
    }
  }

  // Control limits (Static, for Variables)
  const singleChemicalLine = useSingleChemicalLine(mapping);
  const pairedPhaseMode = rawTopPoints.some(p => isRecordedChemicalShift(p.samplingPhase));
  const hasDynamicLimits = !pairedPhaseMode && rawTopPoints.some(p => p.uclStat != null || p.lclStat != null);
  const dynamicLimitPointCount = rawTopPoints.filter(p => p.uclStat != null || p.clStat != null || p.lclStat != null).length;
  const useDynamicControlLimitSeries = hasDynamicLimits && dynamicLimitPointCount >= 2;
  if (!useDynamicControlLimitSeries && showControlLimits.value) {
    addLine(effectiveTopLimits.ucl, "UCL", "#f59e0b", "solid", 1.5);
    addLine(effectiveTopLimits.cl, "CL", "#3b82f6", "solid", 1.5);
    addLine(effectiveTopLimits.lcl, "LCL", "#f59e0b", "solid", 1.5);
  }
  if (useDynamicControlLimitSeries && showControlLimits.value) {
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

  const pointsTop = rawTopPoints;
  const pointsBottom = data.secondaryChartData?.points || [];

  const labels = pointsTop.map((p, i) => {
    if (pairedPhaseMode && p.samplingPhase) {
      const date = chemicalDateKey(p).slice(5).replace("-", "/");
      const phase = normalizeChemicalShift(p.samplingPhase);
      const chartPhaseLabel = singleChemicalLine
        ? chartStageShiftLabel(phase, p.samplingStage, true)
        : shiftLabel(phase);
      return `${date} ${chartPhaseLabel}`;
    }
    if (singleChemicalLine) return `${chemicalDateKey(p).slice(5).replace("-", "/")} ${stageLabel(p.samplingStage)}`;
    if (p.measuredAt) {
      return new Date(p.measuredAt).toLocaleString('zh-TW', { 
        month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hour12: false 
      });
    }
    return `P#${i + 1}`;
  });

  const buildPointDatum = (p, phase = null) => {
    if (!p) return null;
    const isErr = p.outOfSpec || p.outOfControl || (p.violatedRules && p.violatedRules.length > 0);
    const isExcluded = p.isExcluded;
    const phaseStyles = {
      OPEN: { color: "#3b82f6", borderColor: "#2563eb", symbol: "circle" },
      MIDDLE: { color: "#10b981", borderColor: "#047857", symbol: "diamond" },
      CLOSE: { color: "#f97316", borderColor: "#c2410c", symbol: "rect" }
    };
    const phaseStyle = phase ? phaseStyles[phase] : null;
    let color = phaseStyle?.color || (isErr ? "#ef4444" : "#3b82f6");
    let borderColor = phaseStyle?.borderColor || (isErr ? "#991b1b" : "#2563eb");
    let symbol = phaseStyle?.symbol || "circle";
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
  };

  const seriesTopData = pointsTop.map((p, i) => {
    const isErr = p.outOfSpec || p.outOfControl || (p.violatedRules && p.violatedRules.length > 0);
    const isExcluded = p.isExcluded;
    
    const phase = pairedPhaseMode ? normalizeChemicalShift(p.samplingPhase) : null;
    const phaseStyles = { OPEN: ["#3b82f6", "#2563eb", "circle"], MIDDLE: ["#10b981", "#047857", "diamond"], CLOSE: ["#f97316", "#c2410c", "rect"] };
    let color = phaseStyles[phase]?.[0] || (isErr ? "#ef4444" : "#3b82f6");
    let borderColor = phaseStyles[phase]?.[1] || (isErr ? "#991b1b" : "#2563eb");
    let symbol = phaseStyles[phase]?.[2] || "circle";
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
      name: pairedPhaseMode ? "早班" : primarySeriesName,
      type: "line",
      xAxisIndex: 0,
      yAxisIndex: 0,
      data: seriesTopData,
      showSymbol: true,
      label: { show: showPointValues.value, formatter: p => formatNumber(p.value, 3), fontSize: 10 },
      markLine: topMarkLineObj,
      markArea: !useDynamicControlLimitSeries && showControlLimits.value ? markAreaTop : undefined,
      smooth: false
    }
  ];


  router.replace({
    path: route.path,
    query: {
      ppcId: ppcId.value,
      ...(startDate.value ? { startDate: startDate.value } : {}),
      ...(endDate.value ? { endDate: endDate.value } : {}),
      ...(batchId.value ? { batchId: batchId.value } : {}),
    },
  });

  if (useDynamicControlLimitSeries && showControlLimits.value) {
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

  const calculateAxisRange = values => {
    const finiteValues = values.map(Number).filter(Number.isFinite);
    if (finiteValues.length === 0) return {};
    const minimum = Math.min(...finiteValues);
    const maximum = Math.max(...finiteValues);
    const padding = maximum > minimum
      ? (maximum - minimum) * 0.08
      : Math.max(Math.abs(minimum) * 0.08, 1);
    return { min: minimum - padding, max: maximum + padding };
  };
  const topAxisRange = calculateAxisRange([
    ...seriesTopData.map(point => point?.value),
    ...topLimitValues,
    ...(useDynamicControlLimitSeries && showControlLimits.value
      ? pointsTop.flatMap(point => [point.uclStat, point.clStat, point.lclStat])
      : [])
  ]);
  const bottomAxisRange = calculateAxisRange([
    ...seriesBottomData.map(point => point?.value),
    ...markLinesBottom.map(line => line.yAxis)
  ]);

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
            data: pairedPhaseMode ? ["UCL", "CL", "LCL"] : [primarySeriesName, "UCL", "CL", "LCL"],
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
        let res = `<div class="font-bold border-b border-slate-700 pb-1 mb-1">${escapeChartText(params[0].axisValue)}</div>`;
        const meta = params[0].data?.meta;
        if (meta) {
          res += `<div class="text-[10px] text-slate-400 mb-2">`;
          if (meta.lotNo) res += `<div>${htmlChartLabel("批號")}: <span class="text-slate-200">${escapeChartText(meta.lotNo)}</span></div>`;
          if (meta.sideCode && meta.sideCode !== "0") res += `<div>${htmlChartLabel("板面")}: <span class="text-blue-300 font-bold">${meta.sideCode === "1" ? "S1 (Top)" : "S2 (Bottom)"}</span></div>`;
          if (meta.lineId) res += `<div>${htmlChartLabel("產線")}: <span class="text-emerald-400">ID ${meta.lineId}</span></div>`;
          if (meta.tankId) res += `<div>${htmlChartLabel("槽體")}: <span class="text-emerald-400">ID ${meta.tankId}</span></div>`;
          if (meta.pointRemark) res += `<div>${htmlChartLabel("備註")}: <span class="text-amber-200">${escapeChartText(meta.pointRemark)}</span></div>`;
          if (meta.slotId) res += `<div>${htmlChartLabel("槽位")}: <span class="text-emerald-400">ID ${meta.slotId}</span></div>`;
          const phase = normalizeChemicalShift(meta.samplingPhase);
          const phaseColors = { OPEN: "text-blue-300", MIDDLE: "text-emerald-300", CLOSE: "text-orange-300" };
          res += `<div>${htmlChartLabel("班別")}: <span class="font-bold ${phaseColors[phase]}">${htmlChartLabel(chartStageShiftLabel(meta.samplingPhase, meta.samplingStage, singleChemicalLine))}</span></div>`;
          if (singleChemicalLine) res += `<div>${htmlChartLabel("取樣階段")}: ${htmlChartLabel(stageLabel(meta.samplingStage))}</div>`;
          if (meta.measuredAt) res += `<div>${htmlChartLabel("實際量測")}: <span class="text-slate-200">${new Date(meta.measuredAt).toLocaleString("zh-TW", { hour12: false })}</span></div>`;
          res += `</div>`;
        }
        params.forEach(p => {
          const rawValue = p.data?.value !== undefined ? p.data?.value : p.value;
          if (rawValue === null || rawValue === undefined || Number.isNaN(Number(rawValue))) return;
          res += `<div><span class="inline-block w-2 h-2 rounded-full mr-1" style="background-color:${p.color}"></span> ${htmlChartLabel(p.seriesName)}: <strong>${Number(rawValue).toFixed(4)}</strong></div>`;
          if (p.data?.violatedRules?.length > 0) {
            res += `<div class="mt-1.5 px-2 py-0.5 rounded bg-red-900/50 border border-red-500/50 text-red-300 text-[11px] font-bold">${htmlChartLabel("⚠️ 西方電氣規則違規：")}<br>${p.data.violatedRules.map(formatRuleCode).map(escapeChartText).join("<br>")}</div>`;
          }
        });
        if (hasDynamicLimits && meta) {
          if (meta.n != null) res += `<div class="text-[10px] text-slate-400 mt-2">${htmlChartLabel("樣本數 (n)")}: <strong class="text-slate-200">${meta.n}</strong></div>`;
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
            data: (type === "I-MR" || type === "I_MR") ? labels.slice(1) : labels,
            boundaryGap: false,
            gridIndex: 1,
            axisLine: { lineStyle: { color: "#64748b" } },
            axisLabel: { hideOverlap: true, margin: 12 }
          }
        ]
      : [{ type: "category", data: labels, boundaryGap: false, axisLine: { lineStyle: { color: "#64748b" } } }],
    yAxis: isDual
      ? [
          { type: "value", gridIndex: 0, name: type === "XBAR_R" || type === "XBAR_S" ? "Xbar 平均值" : "單值 (I)", splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, axisLabel: { formatter: value => formatNumber(value, 3) }, scale: true, ...topAxisRange },
          { type: "value", gridIndex: 1, name: type === "XBAR_R" ? "全距 (R)" : type === "XBAR_S" ? "標準差 (S)" : "移動全距 (MR)", splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, axisLabel: { formatter: value => formatNumber(value, 3) }, scale: true, ...bottomAxisRange }
        ]
      : [{ type: "value", name: `${type} 數值`, splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } }, axisLine: { lineStyle: { color: "#64748b" } }, axisLabel: { formatter: value => formatNumber(value, 3) }, scale: true, ...topAxisRange }],
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
  localizeChart(chartInstance);

  // Attach Point Click listener for Drill-down cards
  chartInstance.on("click", (params) => {
    hidePointContextMenu();
    if (params.componentType === "markLine" && params.data?.segmentId) {
      segmentDetails.value?.openSegment(params.data.segmentId);
      return;
    }
    if (params.componentType === "series") {
      const idx = params.dataIndex;
      selectedPointIndex.value = idx;
      selectedPoint.value = params.data?.meta || (params.seriesIndex < seriesTopList.length ? pointsTop[idx] : pointsBottom[idx]);
      prepareOcapForm(selectedPoint.value);
    }
  });

  chartInstance.on("contextmenu", (params) => {
    params.event?.event?.preventDefault?.();
    if (params.componentType !== "series") return;
    const idx = params.dataIndex;
    const point = params.data?.meta || (params.seriesIndex < seriesTopList.length ? pointsTop[idx] : pointsBottom[idx]);
    if (!point) return;
    selectedPointIndex.value = idx;
    selectedPoint.value = point;
    prepareOcapForm(point);
    const nativeEvent = params.event?.event;
    pointContextMenu.value = {
      visible: true,
      x: nativeEvent?.offsetX ?? params.event?.offsetX ?? 0,
      y: nativeEvent?.offsetY ?? params.event?.offsetY ?? 0,
      point
    };
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
        let res = `<div class="font-bold border-b border-slate-700 pb-1 mb-1">${escapeChartText(params[0].axisValue)}</div>`;
        const meta = params[0].data?.meta;
        if (meta) {
          res += `<div class="text-[10px] text-slate-400 mb-2">`;
          if (meta.lotNo) res += `<div>${htmlChartLabel("批號")}: <span class="text-slate-200">${escapeChartText(meta.lotNo)}</span></div>`;
          if (meta.serialNo) res += `<div>${htmlChartLabel("序號")}: <span class="text-slate-200">${escapeChartText(meta.serialNo)}</span></div>`;
          if (meta.operator) res += `<div>${htmlChartLabel("人員")}: <span class="text-slate-200">${escapeChartText(meta.operator)}</span></div>`;
          if (meta.pointRemark) res += `<div>${htmlChartLabel("備註")}: <span class="text-amber-200">${escapeChartText(meta.pointRemark)}</span></div>`;
          if (meta.lineId) res += `<div>${htmlChartLabel("產線")}: <span class="text-emerald-400">ID ${meta.lineId}</span></div>`;
          if (meta.tankId) res += `<div>${htmlChartLabel("槽體")}: <span class="text-emerald-400">ID ${meta.tankId}</span></div>`;
          if (meta.slotId) res += `<div>${htmlChartLabel("槽位")}: <span class="text-emerald-400">ID ${meta.slotId}</span></div>`;
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
  localizeChart(trendChartInstance);
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
    localizeChart(histogramChartInstance);
    return;
  }

  const bins = buildHistogramBins(values, 12);
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
  const labelSlotsByBin = new Map();
  const addXAxisLine = (value, name, color, style = "dashed") => {
    const idx = findHistogramBinIndex(bins, value);
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
          const pValStr = data.normality.pValue !== null ? formatNormalityPValue(data.normality.pValue) : "N/A";
          const normalStatus = chartLabel(normalityConclusion(data.normality));
          const methodNote = data.normality.methodNote
            ? `<div style="margin-top:4px;color:#cbd5e1">${escapeChartText(data.normality.methodNote)}</div>`
            : "";
          normalityInfo = `<div style="margin-top:6px;border-top:1px dashed #334155;padding-top:6px;font-size:11px">${htmlChartLabel("常態性檢定")} ${escapeChartText(data.normality.testName || "")} (P-val): <strong style="color:#fbbf24">${pValStr}</strong> (${normalStatus})${methodNote}</div>`;
        }

        return [
          `<div style="font-weight:700;border-bottom:1px solid #334155;padding-bottom:4px;margin-bottom:6px">${htmlChartLabel("量測值區間")}</div>`,
          `<div>${formatNumber(bin.min, 4)} &lt;= X ${point.dataIndex === bins.length - 1 ? "&lt;=" : "&lt;"} ${formatNumber(bin.max, 4)}</div>`,
          `<div style="margin-top:4px">${htmlChartLabel("區間筆數")}: <strong style="color:#fbbf24">${bin.count}</strong></div>`,
          `<div style="margin-top:4px;color:#94a3b8;font-size:11px">${htmlChartLabel("總樣本數 N")}=${values.length}, ${htmlChartLabel("平均值")}=${formatNumber(mean, 4)}, ${htmlChartLabel("標準差")}=${formatNumber(std, 4)}</div>`,
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
  localizeChart(histogramChartInstance);
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
  hidePointContextMenu();
  chartInstance?.dispose();
  chartInstance = null;
  trendChartInstance?.dispose();
  trendChartInstance = null;
  histogramChartInstance?.dispose();
  histogramChartInstance = null;
});
const ocapOptionKeys = {"人為": "people", "機器": "machine", "工法": "method", "測量": "measurementCause", "材料": "material", "環境": "environment", "其他": "other", "輸入錯誤": "inputError", "操作不當": "operationError", "違反SOP": "sopViolation", "機台異常": "machineFault", "溫度異常": "temperatureFault", "測量準確度不佳": "measurementAccuracy", "材料品質不佳": "materialQuality", "環境因素": "environmentCause", "確認輸入是否正確": "verifyInput", "確認機台是否正常": "verifyMachine", "通知工程師處理": "notifyEngineer", "調整參數": "adjustParameters", "清理機台": "cleanMachine", "重新量測": "remeasure", "通知主管": "notifySupervisor"};
const ocapOptionLabel = value => ocapOptionKeys[value] ? t("spc.chartActions." + ocapOptionKeys[value]) : value;

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
            {{ pageHeading }}
          </h1>
          <p class="mt-1 text-xs font-semibold text-slate-400 dark:text-slate-500">
            {{ pageDescription }}
          </p>
        </div>
      </div>
      <button
        v-if="selectedMapping"
        @click="router.push({ path: '/part-process-characteristics', query: { editId: selectedMapping.id } })"
        type="button"
        class="flex items-center justify-center gap-1.5 px-3 py-2 rounded-xl bg-orange-50 hover:bg-orange-100 dark:bg-slate-800 dark:hover:bg-slate-700 text-orange-600 dark:text-orange-400 text-xs font-bold transition-all border border-orange-200 dark:border-slate-700"
        :title="t('spc.chartQuery.editTitle')"
      >
        <Sliders class="w-4 h-4" /> {{ t('spc.chartQuery.edit') }}
      </button>
    </div>

    <!-- 🌟 三大類管制項目維度選擇器 -->
    <div class="flex flex-wrap gap-3 bg-white dark:bg-slate-900 p-2 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <button
        v-for="dimension in dimensionOptions"
        :key="dimension.id" :data-testid="'chart-dimension-' + dimension.id"
        @click="selectedDimension = dimension.id"
        :class="dimensionButtonClass(dimension.id)"
        class="flex-1 min-w-40 py-3 px-4 rounded-xl text-center text-xs md:text-sm font-semibold transition-all duration-200"
      >
        {{ dimension.label }}
      </button>
    </div>

    <!-- Selection Controls -->
    <div data-testid="chart-query" class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex flex-col sm:flex-row sm:flex-wrap sm:items-end gap-2 w-full">
        <!-- Query Conditions -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-5 gap-2 items-end flex-1 min-w-0 w-full">
          <div v-if="selectedDimension === 'PRODUCT' && !isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.queryConditions') }}</label>
            <select
              v-model="productQueryMode"
              data-testid="product-query-mode"
              class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500"
            >
              <option value="DATE">{{ t('spc.chartQuery.dateRange') }}</option>
              <option value="PART">{{ t('spc.chartQuery.part') }}</option>
            </select>
          </div>

          <div v-if="isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.period') }}</label>
            <select data-testid="chart-query-reportPeriodType" v-model="reportPeriodType" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
              <option value="MONTH">{{ t('spc.chartQuery.monthReport') }}</option>
              <option value="WEEK">{{ t('spc.chartQuery.weekReport') }}</option>
            </select>
          </div>

          <div v-if="isMonthlyChartPage && reportPeriodType === 'MONTH'" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.reportMonth') }}</label>
            <input data-testid="chart-query-reportMonth" v-model="reportMonth" type="month" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
          </div>

          <div v-if="isMonthlyChartPage && reportPeriodType === 'WEEK'" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.reportWeek') }}</label>
            <input data-testid="chart-query-reportWeek" v-model="reportWeek" type="week" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
          </div>

          <div v-if="!isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.startDate') }}</label>
            <input data-testid="chart-query-startDate" v-model="startDate" :disabled="selectedDimension === 'PRODUCT' && productQueryMode === 'PART'" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50" />
          </div>

          <div v-if="!isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.endDate') }}</label>
            <input data-testid="chart-query-endDate" v-model="endDate" :disabled="selectedDimension === 'PRODUCT' && productQueryMode === 'PART'" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50" />
          </div>

          <div v-if="selectedDimension === 'PRODUCT' && !isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.part') }}</label>
            <select
              v-model="productPartId"
              :disabled="productQueryMode !== 'PART'"
              data-testid="product-part-id"
              class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
            >
              <option value="">{{ t('spc.chartQuery.choosePart') }}</option>
              <option value="ALL">{{ t('spc.chartQuery.all') }}</option>
              <option v-for="part in productPartOptions" :key="part.id" :value="part.id">[{{ part.partNo }}] {{ part.partName }}</option>
            </select>
          </div>

          <div v-if="selectedDimension !== 'PROD'" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.line') }}</label>
            <select data-testid="chart-query-selectedProcessId" v-model="selectedProcessId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
              <option value="">{{ t('spc.chartQuery.chooseLine') }}</option>
              <option value="ALL">{{ t('spc.chartQuery.all') }}</option>
              <option v-for="pr in availableProcesses" :key="pr.id" :value="pr.id">{{ pr.processName || pr.processCode }}</option>
            </select>
          </div>

          <div v-if="!isMonthlyChartPage" class="w-full">
            <label class="block text-[11px] font-bold text-slate-400 dark:text-slate-500 mb-1">{{ t('spc.chartQuery.normalityMethod') }}</label>
            <p data-testid="normality-method" class="px-3 py-2 text-xs font-semibold text-slate-800 dark:text-white">Anderson-Darling</p>
          </div>

        </div>

        <button data-testid="chart-query-submit" @click="loadActiveChart" :disabled="loading" class="w-full sm:w-auto flex items-center justify-center gap-2 px-5 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-500 hover:to-violet-500 text-white font-bold shadow-md shadow-indigo-500/20 disabled:opacity-50 transition-all text-sm h-[38px] min-w-32">
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" /> {{ t('spc.chartQuery.query') }}
        </button>
      </div>
    </div>

    <!-- Error Display -->
    <div v-if="error" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ displayUiMessage(t, error) }}
    </div>

    <div v-if="loading && !chartResult" class="h-64 flex flex-col items-center justify-center space-y-2 text-slate-400">
      <Activity class="w-8 h-8 animate-bounce text-blue-500" />
      <p class="text-sm font-bold">{{ t('spc.chartQuery.querying') }}</p>
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
      <div class="flex flex-wrap items-center gap-2">
        <button
          v-if="cachedSummaryData"
          type="button"
          data-testid="return-to-summary"
          @click="returnToSummary"
          class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-white dark:bg-slate-900 text-blue-600 dark:text-blue-300 border border-blue-200 dark:border-blue-800 hover:bg-blue-50 dark:hover:bg-blue-950/40 font-bold text-xs shadow-sm transition-colors"
        >
          {{ t('spc.chartQuery.returnSummary') }}
        </button>
        <button
          type="button"
          data-testid="download-raw-data"
          @click="downloadRawData"
          :disabled="rawDataDownloading"
          class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-white dark:bg-slate-900 text-emerald-600 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 font-bold text-xs shadow-sm transition-colors disabled:opacity-50"
        >
          <Download class="w-4 h-4" /> {{ rawDataDownloading ? t('spc.chartQuery.downloading') : t('spc.chartQuery.download') }}
        </button>
        <button
          type="button"
          data-testid="excluded-points-toggle"
          @click="showExcludedPointsPanel = !showExcludedPointsPanel"
          class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-white dark:bg-slate-900 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800 font-bold text-xs shadow-sm transition-colors"
        >
          <List class="w-4 h-4" /> {{ t('spc.chartQuery.excludedCount', { count: activeExclusionCount }) }}
        </button>
      </div>

      <div
        v-if="showExcludedPointsPanel"
        data-testid="excluded-points-panel"
        class="p-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
      >
        <div class="flex items-center justify-between gap-2 mb-2">
          <h3 class="text-sm font-black text-slate-800 dark:text-white">{{ t('spc.chartQuery.excluded') }}</h3>
          <button
            type="button"
            class="px-2 py-1 text-xs font-bold rounded border border-slate-200 dark:border-slate-700 text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800"
            :disabled="excludedPointsLoading"
            @click="loadExcludedPoints()"
          >
            {{ t('spc.chartQuery.refresh') }}
          </button>
        </div>
        <div v-if="excludedPointsLoading" class="text-xs text-slate-500">{{ t('spc.chartQuery.loading') }}</div>
        <div v-else-if="excludedPoints.length === 0" class="text-xs text-slate-500">{{ t('spc.chartQuery.noExcluded') }}</div>
        <div v-else class="divide-y divide-slate-200 dark:divide-slate-800">
          <div
            v-for="row in excludedPoints"
            :key="readField(row, 'id', 'Id')"
            class="py-2 flex flex-col gap-2 md:flex-row md:items-center md:justify-between"
          >
            <div class="min-w-0">
              <p class="text-xs font-black text-slate-800 dark:text-slate-100">
                {{ formatExclusionPointLabel(row) }}
              </p>
              <p class="text-[11px] font-semibold text-slate-500 dark:text-slate-400">
                {{ formatExclusionState(row) }}
                <span v-if="formatExclusionUpdatedAt(row)">・{{ formatExclusionUpdatedAt(row) }}</span>
              </p>
            </div>
            <button
              type="button"
              class="self-start md:self-auto px-3 py-1.5 rounded-lg bg-blue-50 dark:bg-blue-950/40 text-blue-700 dark:text-blue-300 border border-blue-200 dark:border-blue-800 hover:bg-blue-100 dark:hover:bg-blue-900/50 text-xs font-black disabled:opacity-50"
              :disabled="pointExclusionSaving"
              @click="restoreExcludedPoint(row)"
            >
              {{ t('spc.chartQuery.restore') }}
            </button>
          </div>
        </div>
      </div>

      <div
        v-if="activeChartDisplayName"
        data-testid="active-chart-display-name"
        class="px-4 py-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
      >
        <p class="text-[11px] font-black uppercase tracking-wider text-slate-400 dark:text-slate-500">{{ t('spc.chartQuery.chartName') }}</p>
        <h2 class="mt-1 text-xl font-black text-slate-900 dark:text-white leading-snug">
          {{ activeChartDisplayName }}
        </h2>
      </div>

      <!-- Capability Summary Cards -->
      <div v-if="chartResult.capability" class="grid grid-cols-1 gap-2">
        <div class="p-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
          <p class="text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500">{{ t('spc.chartQuery.overallSigma') }}</p>
          <h3 class="text-lg font-bold mt-0.5 text-slate-800 dark:text-white">
            {{ chartResult.capability.sigmaOverall !== null ? Number(chartResult.capability.sigmaOverall).toFixed(4) : 'N/A' }}
          </h3>
        </div>
      </div>

      <!-- Monitor Detail Summary -->
      <div id="process-capability-analysis" data-testid="chart-monitor" class="scroll-mt-4 grid grid-cols-1 xl:grid-cols-4 gap-5 p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
        <div class="p-1">
          <h3 class="text-sm font-black text-slate-800 dark:text-white mb-2 flex items-center gap-2">
            <Info class="w-4 h-4 text-blue-500" /> {{ t('spc.chartQuery.monitor') }}
          </h3>
          <div class="grid grid-cols-2 gap-2 text-xs">
            <div>
              <p class="text-slate-400 font-bold">{{ t('spc.chartQuery.processLine') }}</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorProcessLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">{{ t('spc.chartQuery.characteristic') }}</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorCharacteristicLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">{{ t('spc.chartQuery.machine') }}</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorMachineLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">{{ t('spc.chartQuery.slot') }}</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ monitorSlotLabel }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">{{ isEtchAverageChart ? t('spc.chartQuery.pointCountLabel') : t('spc.chartQuery.measurementCountLabel') }}</p>
              <p class="font-semibold text-slate-800 dark:text-slate-200">{{ isEtchAverageChart ? t('spc.chartQuery.pointCount', { count: displayedMeasurementCount }) : t('spc.chartQuery.measurementCount', { count: displayedMeasurementCount }) }}</p>
            </div>
            <div>
              <p class="text-slate-400 font-bold">{{ t('spc.chartQuery.abnormal') }}</p>
              <p class="font-semibold" :class="violationRows.length > 0 ? 'text-red-500' : 'text-emerald-500'">{{ t('spc.chartQuery.pointCount', { count: violationRows.length }) }}</p>
            </div>
          </div>
        </div>

        <div class="p-1 xl:border-l xl:border-slate-200 xl:dark:border-slate-800 xl:pl-5">
          <div class="flex items-center justify-between mb-2">
            <h3 class="text-sm font-black text-slate-800 dark:text-white flex items-center gap-2">
              <Sliders class="w-4 h-4 text-amber-500" /> {{ t('spc.chartQuery.controlLimits') }}
            </h3>
          </div>
          <div class="grid grid-cols-2 gap-2 text-xs">
            <div class="space-y-1">
              <p class="text-slate-400 font-bold">I / Xbar</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">UCL {{ formatSpecLimit(topControlStat?.ucl ?? chartResult.limits?.ucl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">CL {{ formatSpecLimit(topControlStat?.cl ?? chartResult.limits?.cl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">LCL {{ formatSpecLimit(topControlStat?.lcl ?? chartResult.limits?.lcl) }}</p>
            </div>
            <div class="space-y-1">
              <p class="text-slate-400 font-bold">MR / R / S</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">UCL {{ formatSpecLimit(bottomControlStat?.ucl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">CL {{ formatSpecLimit(bottomControlStat?.cl) }}</p>
              <p class="font-mono text-slate-700 dark:text-slate-200">LCL {{ formatSpecLimit(bottomControlStat?.lcl) }}</p>
            </div>
          </div>
        </div>

        <div class="p-1 xl:border-l xl:border-slate-200 xl:dark:border-slate-800 xl:pl-5">
          <h3 class="text-sm font-black text-slate-800 dark:text-white mb-2 flex items-center gap-2">
            <ShieldAlert class="w-4 h-4 text-red-500" /> {{ t('spc.chartQuery.specLimits') }}
          </h3>
          <div data-testid="monitor-spec-limits" class="space-y-1 text-xs">
            <p class="font-mono text-slate-700 dark:text-slate-200">USL {{ formatSpecLimit(monitorSpecLimits.usl) }}</p>
            <p class="font-mono text-slate-700 dark:text-slate-200">Target {{ formatSpecLimit(monitorSpecLimits.target) }}</p>
            <p class="font-mono text-slate-700 dark:text-slate-200">LSL {{ formatSpecLimit(monitorSpecLimits.lsl) }}</p>
          </div>
        </div>

        <div class="p-1 xl:border-l xl:border-slate-200 xl:dark:border-slate-800 xl:pl-5">
          <h3 class="text-sm font-black text-slate-800 dark:text-white mb-2 flex items-center gap-2">
            <Activity class="w-4 h-4 text-emerald-500" /> {{ t('spc.chartQuery.capability') }}
          </h3>
          <div class="grid grid-cols-3 gap-2 text-xs">
            <div v-for="row in capabilityRows" :key="row.label" class="p-1.5 rounded-lg bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700">
              <p class="text-slate-400 font-bold">{{ row.label }}</p>
              <p class="text-base font-black text-slate-800 dark:text-white">{{ formatNumber(row.value, row.label === 'Ppm' ? 1 : 4) }}</p>
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
              {{ t('spc.chartLabels.specToggle') }}
            </label>
            <label class="flex items-center gap-1.5 cursor-pointer">
              <input v-model="showControlLimits" type="checkbox" class="rounded border-slate-300 text-amber-600" />
              {{ t('spc.chartLabels.controlToggle') }}
            </label>
            <label class="flex items-center gap-1.5 cursor-pointer">
              <input v-model="showPointValues" type="checkbox" class="rounded border-slate-300 text-emerald-600" />
              {{ t('spc.chartLabels.pointValues') }}
            </label>
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-red-500 inline-block"></span> {{ t('spc.chartLabels.outOfControl') }}</span>
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-blue-500 inline-block"></span> {{ t('spc.chartLabels.normalPoint') }}</span>
            <span class="flex items-center gap-1.5"><span class="w-3 h-3 rounded-full bg-slate-400 inline-block border border-slate-500"></span> {{ t('spc.chartLabels.excludedPoint') }}</span>
          </div>
        </div>

        <div v-if="chartResult.subgroupSizeNote" class="mb-2 p-2.5 rounded-lg bg-amber-500/10 border border-amber-500/30 text-amber-600 dark:text-amber-400 text-xs flex items-center gap-2">
          <AlertTriangle class="w-4 h-4 flex-shrink-0" /> {{ chartResult.subgroupSizeNote }}
        </div>

        <ChartSegmentDetails v-if="activePpcIdValue" ref="segmentDetails" :ppc-id="activePpcIdValue" chart-line-entry />

        <div class="relative">
          <div class="absolute top-1 right-12 z-20 flex items-center gap-1">
            <button type="button" @click="zoomChartIn" :title="t('spc.chartLabels.zoom')" :aria-label="t('spc.chartLabels.zoom')" class="p-1 text-slate-500 hover:text-blue-600 dark:text-slate-400 dark:hover:text-blue-400">
              <ZoomIn class="w-5 h-5" />
            </button>
            <button type="button" @click="resetChartZoom" :title="t('spc.chartLabels.resetZoom')" :aria-label="t('spc.chartLabels.resetZoom')" class="p-1 text-slate-500 hover:text-blue-600 dark:text-slate-400 dark:hover:text-blue-400">
              <RotateCcw class="w-5 h-5" />
            </button>
          </div>
          <div ref="chartEl" data-testid="primary-spc-chart" class="h-[620px] w-full min-h-[500px]"></div>
          <div
            v-if="pointContextMenu.visible"
            class="absolute z-30 w-52 rounded-lg border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 shadow-xl overflow-hidden"
            :style="{ left: `${pointContextMenu.x}px`, top: `${pointContextMenu.y}px` }"
            @click.stop
          >
            <button
              type="button"
              class="w-full px-3 py-2 text-left text-xs font-bold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-50"
              :disabled="pointRemarkSaving"
              data-testid="chart-action-openPointRemarkModal" @click="openPointRemarkModal"
            >
              {{ t('spc.chartActions.editRemark') }}
            </button>
            <button
              type="button"
              class="w-full px-3 py-2 text-left text-xs font-bold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-50"
              :disabled="pointExclusionSaving"
              data-testid="chart-action-exclude-visible" @click="setPointExclusion('ExcludedVisible')"
            >
              {{ t('spc.chartActions.visible') }}
            </button>
            <button
              type="button"
              class="w-full px-3 py-2 text-left text-xs font-bold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-50"
              :disabled="pointExclusionSaving"
              data-testid="chart-action-exclude-hidden" @click="setPointExclusion('ExcludedHidden')"
            >
              {{ t('spc.chartActions.hidden') }}
            </button>
            <button
              type="button"
              class="w-full px-3 py-2 text-left text-xs font-bold text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950/40 disabled:opacity-50"
              :disabled="pointExclusionSaving"
              data-testid="chart-action-restore-point" @click="restorePointExclusion"
            >
              {{ t('spc.chartActions.restore') }}
            </button>
          </div>
        </div>

        <div
          v-if="pointRemarkModal.visible" data-testid="chart-remark-overlay"
          class="fixed inset-0 z-40 flex items-center justify-center bg-slate-950/40 p-4"
          @click.self="closePointRemarkModal"
        >
          <div data-testid="chart-remark-dialog" role="dialog" :aria-label="t('spc.chartActions.remark')" class="w-full max-w-md max-h-[calc(100dvh-2rem)] overflow-y-auto rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 shadow-2xl p-4">
            <div class="mb-3 flex flex-wrap items-center justify-between gap-2"><h3 class="text-sm font-black text-slate-900 dark:text-white">{{ t('spc.chartActions.remark') }}</h3><LocalePicker /></div>
            <textarea
              data-testid="chart-action-pointRemarkModal.text" v-model="pointRemarkModal.text"
              maxlength="500"
              rows="5"
              class="w-full rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 px-3 py-2 text-sm text-slate-800 dark:text-slate-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              :placeholder="t('spc.chartActions.remarkPlaceholder')"
            ></textarea>
            <div class="mt-2 flex items-center justify-between gap-3">
              <span class="text-xs text-slate-500">{{ pointRemarkModal.text.length }}/500</span>
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 text-xs font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 disabled:opacity-50"
                  :disabled="pointRemarkSaving"
                  data-testid="chart-action-clearPointRemark" @click="clearPointRemark"
                >
                  {{ t('spc.chartActions.clear') }}
                </button>
                <button
                  type="button"
                  class="px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 text-xs font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800"
                  data-testid="chart-action-closePointRemarkModal" @click="closePointRemarkModal"
                >
                  {{ t('spc.chartActions.cancel') }}
                </button>
                <button
                  type="button"
                  class="px-3 py-2 rounded-lg bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 disabled:opacity-50"
                  :disabled="pointRemarkSaving"
                  data-testid="chart-action-savePointRemark" @click="savePointRemark"
                >
                  {{ t('spc.chartActions.save') }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <div id="violation-analysis" class="scroll-mt-4 mt-3 p-3 rounded-xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-800">
          <div class="flex items-center justify-between mb-2">
            <h3 class="text-sm font-black text-slate-800 dark:text-white flex items-center gap-2">
              <ShieldAlert class="w-4 h-4 text-red-500" /> {{ t('spc.chartActions.violations') }}
            </h3>
            <span class="text-xs font-bold" :class="violationRows.length > 0 ? 'text-red-500' : 'text-emerald-500'">
              {{ violationRows.length > 0 ? t('spc.chartActions.needsReview', { count: violationRows.length }) : t('spc.chartActions.noAbnormal') }}
            </span>
          </div>
          <div v-if="violationRows.length === 0" class="text-xs text-slate-500">
            {{ t('spc.chartActions.noViolation') }}
          </div>
          <div v-else class="overflow-x-auto">
            <table class="min-w-full text-xs">
              <thead class="text-slate-400 border-b border-slate-200 dark:border-slate-700">
                <tr>
                  <th class="text-left py-2 pr-3">{{ t('spc.chartActions.point') }}</th>
                  <th class="text-left py-2 pr-3">{{ t('spc.chartActions.time') }}</th>
                  <th class="text-left py-2 pr-3">{{ t('spc.chartActions.value') }}</th>
                  <th class="text-left py-2 pr-3">{{ t('spc.chartActions.type') }}</th>
                  <th class="text-left py-2 pr-3">{{ t('spc.chartActions.rules') }}</th>
                  <th class="text-left py-2 pr-3">{{ t('spc.chartActions.handlingStatus') }}</th>
                  <th class="text-left py-2 pr-3">{{ t('spc.chartActions.actions') }}</th>
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
                    <span v-else class="text-slate-400">{{ t('spc.chartActions.limitJudgment') }}</span>
                  </td>
                  <td class="py-2 pr-3">{{ row.alertStatus || (row.rootCause || row.correctiveAction ? 'Handled' : t('spc.chartActions.unhandled')) }}</td>
                  <td class="py-2 pr-3">
                    <button
                      data-testid="chart-action-select-point" @click="selectedPoint = row; selectedPointIndex = row.pointNo - 1; prepareOcapForm(row)"
                      class="px-2.5 py-1 rounded-lg bg-blue-600 hover:bg-blue-500 text-white font-bold"
                    >
                      {{ t('spc.chartActions.selectAction') }}
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- 量測值分布直方圖 (Histogram) -->
        <div id="distribution-analysis" class="scroll-mt-4 mt-5 pt-5 border-t border-slate-200 dark:border-slate-800">
          <div class="flex flex-col md:flex-row md:items-center justify-between gap-2 mb-3">
            <h3 class="text-base font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
              <BarChart3 class="w-5 h-5 text-amber-500" />
              {{ t('spc.chartLabels.histogram') }}
            </h3>
            <div class="flex flex-wrap items-center gap-3 text-xs font-semibold text-slate-500">
              <span class="flex items-center gap-1.5"><span class="w-5 h-0.5 bg-red-500 border-t border-dashed border-red-500 inline-block"></span>{{ t('spc.chartLabels.specRange') }}</span>
              <span class="flex items-center gap-1.5"><span class="w-5 h-0.5 bg-amber-500 inline-block"></span>{{ t('spc.chartLabels.controlRange') }}</span>
              <span class="text-slate-400">{{ t('spc.chartLabels.excludedNotice') }}</span>
            </div>
          </div>

          <div class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-2 mb-3 p-3 bg-slate-50 dark:bg-slate-800/40 rounded-xl border border-slate-200 dark:border-slate-800 text-xs font-semibold">
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">{{ t('spc.chartLabels.included') }}</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ isEtchAverageChart ? t('spc.chartQuery.pointCount', { count: chartPoints.length }) : t('spc.chartQuery.measurementCount', { count: (chartResult.rawDataPoints || []).filter(p => !p.isExcluded && p.value !== null && p.value !== undefined).length }) }}
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
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">{{ t('spc.chartLabels.pValue') }}</span>
              <span class="text-slate-800 dark:text-slate-200 font-mono">
                {{ chartResult.normality && chartResult.normality.pValue !== null ? formatNormalityPValue(chartResult.normality.pValue) : 'N/A' }}
              </span>
            </div>
            <div class="space-y-1">
              <span class="block text-[10px] text-slate-400 dark:text-slate-500 uppercase font-bold tracking-wider">{{ t('spc.chartLabels.normality') }}</span>
              <div>
                <span v-if="normalityConclusion(chartResult.normality) === '無法檢定'" class="px-2 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400">
                  {{ t('spc.chartLabels.unavailable') }}
                </span>
                <span v-else-if="normalityConclusion(chartResult.normality) === '未拒絕常態假設'" class="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
                  {{ t('spc.chartLabels.notRejected') }}
                </span>
                <span v-else-if="chartResult.normality" class="px-2 py-0.5 rounded text-[10px] font-bold bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20 animate-pulse">
                  {{ t('spc.chartLabels.rejected') }}
                </span>
                <span v-else class="text-slate-400 font-mono">N/A</span>
              </div>
            </div>
          </div>
          <p v-if="normalityUnavailableReason(chartResult.normality)" data-testid="normality-unavailable" class="text-xs text-amber-700 dark:text-amber-300">
            {{ chartResult.normality?.note || chartLabel(normalityUnavailableReason(chartResult.normality)) }}
          </p>
          <p v-if="chartResult.normality?.methodNote" class="text-xs text-slate-500 dark:text-slate-400">
            {{ chartResult.normality.methodNote }}
          </p>
          <div ref="histogramChartEl" class="h-[300px] w-full min-h-[240px]"></div>
        </div>

        <!-- Point Detail Drilldown Card -->
        <div v-if="selectedPoint" data-testid="chart-point-details" class="min-w-0 break-words mt-4 p-4 rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 shadow-inner transition-all duration-300">
          <ChemicalCalculationDetails v-if="activePpcIdValue" :ppc-id="activePpcIdValue" :measurement-id="selectedPoint.variableMeasurementId||null" :aggregate="Number(selectedPoint.n||1)>1" point-mode />
          <div class="flex items-center justify-between border-b border-slate-200 dark:border-slate-700 pb-2 mb-3">
            <h4 class="text-sm font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <span class="inline-block w-2.5 h-2.5 rounded-full bg-blue-500 animate-pulse"></span>
              {{ t('spc.chartActions.pointDetails', { point: selectedPointIndex + 1 }) }}
            </h4>
            <div class="flex gap-2">
              <span v-if="selectedPoint.isExcluded" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-300 border border-slate-300 dark:border-slate-600">
                {{ t('spc.chartActions.excluded') }}
              </span>
              <span v-else-if="selectedPoint.outOfSpec" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-red-100 text-red-700 dark:bg-red-950/50 dark:text-red-400 border border-red-200 dark:border-red-900/50">
                {{ t('spc.chartActions.oos') }}
              </span>
              <span v-else-if="selectedPoint.outOfControl" class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-400 border border-amber-200 dark:border-amber-900/50">
                {{ t('spc.chartActions.ooc') }}
              </span>
              <span v-else class="px-2 py-0.5 rounded text-[10px] font-black uppercase tracking-wider bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-900/50">
                {{ t('spc.chartActions.inControl') }}
              </span>
              
              <!-- Toggle Exclude Button -->
              <button @click="toggleExcludeBatch" v-if="selectedPoint.measurementBatchId || uploadBatchId" class="ml-2 px-3 py-0.5 rounded text-[10px] font-black uppercase tracking-wider border transition-colors focus:outline-none" :class="selectedPoint.isExcluded ? 'bg-blue-100 text-blue-700 border-blue-200 hover:bg-blue-200 dark:bg-blue-900/50 dark:text-blue-300 dark:border-blue-800' : 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-200 dark:bg-slate-800 dark:text-slate-400 dark:border-slate-700'">
                {{ selectedPoint.isExcluded ? t('spc.chartActions.restoreBatch') : t('spc.chartActions.excludeBatch') }}
              </button>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div v-if="selectedPoint.pointRemark" class="md:col-span-4 flex items-start gap-2.5 rounded-lg bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-900/50 p-3">
              <StickyNote class="w-4 h-4 text-amber-500 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-amber-600 dark:text-amber-400 uppercase mb-1">{{ t('spc.chartActions.remark') }}</p>
                <p class="text-sm font-semibold text-slate-700 dark:text-slate-200 whitespace-pre-wrap">{{ selectedPoint.pointRemark }}</p>
              </div>
            </div>
            <div class="flex items-start gap-2.5">
              <Hash class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">{{ t('spc.chartActions.lot') }}</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200 select-all">{{ selectedPoint.lotNo || t('spc.chartActions.noLot') }}</p>
              </div>
            </div>

            <div v-if="selectedPoint.serialNo" class="flex items-start gap-2.5">
              <Sparkles class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">{{ t('spc.chartActions.serial') }}</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200 select-all">{{ selectedPoint.serialNo || t('spc.chartActions.noSerial') }}</p>
              </div>
            </div>

            <div class="flex items-start gap-2.5">
              <User class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">{{ t('spc.chartActions.operator') }}</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200">{{ selectedPoint.operator || selectedPoint.Operator || t('spc.chartActions.autoImport') }}</p>
              </div>
            </div>

            <div class="flex items-start gap-2.5">
              <Clock class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">{{ t('spc.chartActions.measuredAt') }}</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200">
                  {{ selectedPoint.measuredAt ? new Date(selectedPoint.measuredAt).toLocaleString('zh-TW', { hour12: false }) : t('spc.chartActions.noTime') }}
                </p>
              </div>
            </div>

            <div v-if="selectedPoint.samplingPhase || selectedPoint.samplingStage" class="flex items-start gap-2.5">
              <Calendar class="w-4 h-4 text-slate-400 mt-1" />
              <div>
                <p class="text-[10px] font-bold text-slate-400 uppercase">{{ t('spc.chartActions.shift') }}</p>
                <p class="text-sm font-bold text-slate-700 dark:text-slate-200">
                  {{ chartLabel(chartStageShiftLabel(selectedPoint.samplingPhase, selectedPoint.samplingStage, useSingleChemicalLine(selectedMapping))) }}
                  <span v-if="selectedPoint.samplingStage && selectedPoint.samplingStage !== 'GENERAL' && !useSingleChemicalLine(selectedMapping)" class="text-xs text-slate-400">
                    / {{ chartLabel(stageLabel(selectedPoint.samplingStage)) }}
                  </span>
                </p>
              </div>
            </div>
          </div>

          <!-- Bottom detailed measurements & violation rules -->
          <div class="mt-4 pt-4 border-t border-slate-200 dark:border-slate-700 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
            <div>
              <p class="text-[10px] font-bold text-slate-400 uppercase mb-1">{{ t('spc.chartActions.measurement') }}</p>
              <h3 class="text-xl font-black" :class="selectedPoint.isExcluded ? 'text-slate-400 line-through' : 'text-slate-800 dark:text-white'">
                {{ selectedPoint.value !== undefined ? Number(selectedPoint.value).toFixed(4) : (selectedPoint.xbar !== undefined ? Number(selectedPoint.xbar).toFixed(4) : 'N/A') }}
                <span v-if="selectedPoint.range !== undefined" class="text-sm text-slate-400 font-semibold ml-3 no-underline">
                  {{ t('spc.chartActions.subgroup', { range: Number(selectedPoint.range).toFixed(4), size: selectedPoint.n }) }}
                </span>
              </h3>
            </div>

            <!-- Action trace panel (Root Cause & Corrective Action) -->
            <div v-if="selectedPoint.rootCause || selectedPoint.correctiveAction" class="w-full md:flex-1 md:ml-6 p-3 rounded-xl bg-blue-50 dark:bg-slate-900 border border-blue-100 dark:border-slate-700">
              <p class="text-[10px] font-bold text-blue-600 dark:text-blue-400 uppercase mb-2 flex items-center gap-1">
                <Info class="w-3.5 h-3.5" /> {{ t('spc.chartActions.trace') }}
              </p>
              <div class="space-y-1.5 text-xs text-slate-700 dark:text-slate-300">
                <div v-if="selectedPoint.rootCause" class="flex gap-2">
                  <span class="font-bold shrink-0">{{ t('spc.chartActions.rootCauseLabel') }}</span>
                  <span>{{ selectedPoint.rootCause }}</span>
                </div>
                <div v-if="selectedPoint.correctiveAction" class="flex gap-2">
                  <span class="font-bold shrink-0">{{ t('spc.chartActions.actionLabel') }}</span>
                  <span>{{ selectedPoint.correctiveAction }}</span>
                </div>
              </div>
            </div>

            <div v-if="selectedPoint.violatedRules?.length > 0" class="w-full md:w-auto">
              <p class="text-[10px] font-bold text-red-500 uppercase mb-1 flex items-center gap-1">
                <AlertTriangle class="w-3.5 h-3.5" /> {{ t('spc.chartActions.triggeredRules') }}
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
                <CheckCircle2 class="w-4 h-4 text-emerald-500" /> {{ t('spc.chartActions.ocap') }}
              </h4>
              <span class="text-xs font-semibold text-slate-400">
                {{ selectedPoint.alertId ? `Alert #${selectedPoint.alertId}` : t('spc.chartActions.noAlert') }}
              </span>
            </div>

            <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartActions.causeCategory') }}</label>
                <select data-testid="chart-action-ocapForm.causeCategory" v-model="ocapForm.causeCategory" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="">{{ t('spc.chartActions.chooseCategory') }}</option>
                  <option v-for="item in ocapCauseCategories" :key="item" :value="item">{{ ocapOptionLabel(item) }}</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartActions.causeType') }}</label>
                <select data-testid="chart-action-ocapForm.causeType" v-model="ocapForm.causeType" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="">{{ t('spc.chartActions.chooseCause') }}</option>
                  <option v-for="item in ocapCauseTypes" :key="item" :value="item">{{ ocapOptionLabel(item) }}</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartActions.actionType') }}</label>
                <select data-testid="chart-action-ocapForm.actionType" v-model="ocapForm.actionType" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="">{{ t('spc.chartActions.chooseAction') }}</option>
                  <option v-for="item in ocapActionTypes" :key="item" :value="item">{{ ocapOptionLabel(item) }}</option>
                </select>
              </div>
              <div class="md:col-span-3">
                <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartActions.rootCause') }}</label>
                <textarea data-testid="chart-action-ocapForm.rootCause" v-model="ocapForm.rootCause" rows="2" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white" :placeholder="t('spc.chartActions.rootCausePlaceholder')"></textarea>
              </div>
              <div class="md:col-span-3">
                <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartActions.correctiveAction') }}</label>
                <textarea data-testid="chart-action-ocapForm.correctiveAction" v-model="ocapForm.correctiveAction" rows="2" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white" :placeholder="t('spc.chartActions.actionPlaceholder')"></textarea>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartActions.status') }}</label>
                <select data-testid="chart-action-ocapForm.status" v-model="ocapForm.status" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white">
                  <option value="Open">{{ t('spc.chartActions.open') }}</option>
                  <option value="InProgress">{{ t('spc.chartActions.inProgress') }}</option>
                  <option value="Closed">{{ t('spc.chartActions.closed') }}</option>
                </select>
              </div>
              <div>
                <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartActions.responsible') }}</label>
                <input data-testid="chart-action-ocapForm.responsibleUser" v-model="ocapForm.responsibleUser" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm text-slate-800 dark:text-white" :placeholder="t('spc.chartActions.responsiblePlaceholder')" />
              </div>
              <div class="flex items-end">
                <button
                  data-testid="chart-action-saveOcapForSelectedPoint" @click="saveOcapForSelectedPoint"
                  :disabled="savingOcap"
                  class="w-full px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-black disabled:opacity-50"
                >
                  {{ savingOcap ? t('spc.chartActions.saving') : t('spc.chartActions.saveOcap') }}
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>
