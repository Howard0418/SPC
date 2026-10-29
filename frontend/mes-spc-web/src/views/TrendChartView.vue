<script setup>
import {characteristicDisplayName, summaryCharacteristicDisplayName} from "../i18n/characteristicNames";
const characteristicName = item => characteristicDisplayName(item, locale.value, t("spc.characteristics.unnamed"));
const summaryName = row => summaryCharacteristicDisplayName(row, locale.value, t("spc.characteristics.unnamed"));
const dtoText = (data, fallback) => displayUiMessage(t, getApiMessageDisplay(data, fallback));
import { formatNormalityPValue, normalityConclusion, normalityUnavailableReason } from "../utils/normalityPresentation";
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from "vue";
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
import { createChartLabelTranslator } from "../i18n/chartLabels";
import { captureChartPresentation, translateChartPresentation, escapeChartText } from "../i18n/chartPresentation";
import { useRoute, useRouter } from "vue-router";
import * as echarts from "echarts";
import { api, getApiMessageDisplay, getApiErrorMessage, getApiErrorDisplay } from "../api/client";
import { buildHistogramBins, findHistogramBinIndex } from "../utils/histogram";
import {
  TrendingUp,
  RefreshCw,
  AlertTriangle,
  Activity,
  ShieldAlert,
  Info,
  Sliders,
  List,
  StickyNote
} from "lucide-vue-next";
import SpcSummaryTable from "../components/SpcSummaryTable.vue";
import ChemicalCalculationDetails from "../components/ChemicalCalculationDetails.vue";
import ChartSegmentDetails from "../components/ChartSegmentDetails.vue";
import LocalePicker from "../components/LocalePicker.vue";

const route = useRoute();
const router = useRouter();
const { t, locale } = useI18n({ useScope: "global" });

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
const selectedPpcId = ref("");
const updatingCascades = ref(false);
const toDateInputValue = date => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
};
const today = new Date();
const threeMonthsAgo = new Date(today);
threeMonthsAgo.setMonth(threeMonthsAgo.getMonth() - 3);
const startDate = ref(toDateInputValue(threeMonthsAgo));
const endDate = ref(toDateInputValue(today));
const normalityMethod = "AD";

// Chart state
const loading = ref(false);
const error = ref("");
const chartResult = ref(null);
const trendChartEl = ref(null);
const histogramChartEl = ref(null);
let trendChartInstance = null;
let histogramChartInstance = null;
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

const tableSummaryData = ref(null);
const cachedSummaryData = ref(null);
const selectedSummaryRow = ref(null);
// ─── Dimension helpers ───────────────────────────────────────
const getDimensionForMapping = (m) => {
  return m.controlScope || "";
};

const dimensionOptions = computed(() => {
  const byCode = new Map();
  groups.value
    .filter(group => group.isEnabled !== false
      && (group.groupType === "TREND_CHART" || group.groupType === "CONTROL_CHART"))
    .forEach(group => {
      if (!byCode.has(group.groupCode)) {
        byCode.set(group.groupCode, { id: group.groupCode, label: group.groupName });
      }
    });
  return [...byCode.values()];
});

const filteredMappingsByDimension = computed(() =>
  mappings.value.filter(m =>
    m.isEnabled &&
    m.characteristic?.dataCategory === "Variable" &&
    ((m.controlScope || "").toUpperCase() !== "CHEM" || m.displayMode === "TREND_CHART") &&
    getDimensionForMapping(m) === selectedDimension.value
  )
);

const uniqueParts = computed(() => {
  const byId = new Map();
  filteredMappingsByDimension.value
    .filter(m => (m.controlScope || "PRODUCT") === "PRODUCT" && m.part?.isEnabled !== false)
    .forEach(m => { if (!byId.has(m.partId)) byId.set(m.partId, m.part); });
  return [...byId.entries()]
    .map(([id, part]) => ({ ...part, id }))
    .sort((a, b) => (a.partNo || "").localeCompare(b.partNo || ""));
});

const availableProcesses = computed(() => {
  if (selectedDimension.value === "PROD" && !selectedPartId.value) return [];
  const byId = new Map();
  filteredMappingsByDimension.value
    .filter(m =>
      (selectedDimension.value !== "PROD" || m.partId === Number(selectedPartId.value)) &&
      m.process?.isEnabled !== false
    )
    .forEach(m => { if (!byId.has(m.processId)) byId.set(m.processId, m.process); });
  return [...byId.entries()]
    .map(([id, process]) => ({ ...process, id }))
    .sort((a, b) => (Number(a.sequenceNo) || 0) - (Number(b.sequenceNo) || 0) || a.id - b.id);
});

const availableCharacteristics = computed(() => {
  if ((selectedDimension.value === "PROD" && !selectedPartId.value) || !selectedProcessId.value) return [];
  return filteredMappingsByDimension.value
    .filter(m =>
      (selectedDimension.value !== "PROD" || m.partId === Number(selectedPartId.value)) &&
      m.processId === Number(selectedProcessId.value) &&
      m.isEnabled && m.characteristic?.isEnabled !== false && m.characteristic?.isSpcEnabled !== false
    )
    .map(m => ({ ...m.characteristic, id: m.characteristicId, mappingId: m.id }))
    .sort((a, b) => (a.characteristicCode || "").localeCompare(b.characteristicCode || ""));
});

const selectedMapping = computed(() => {
  if (selectedPpcId.value) {
    const exactMatch = filteredMappingsByDimension.value.find(m => m.id === Number(selectedPpcId.value));
    if (exactMatch) return exactMatch;
  }
  if ((selectedDimension.value === "PROD" && !selectedPartId.value) || !selectedProcessId.value || !selectedCharacteristicId.value) return null;
  return filteredMappingsByDimension.value.find(m =>
    (selectedDimension.value !== "PROD" || m.partId === Number(selectedPartId.value)) &&
    m.processId === Number(selectedProcessId.value) &&
    m.characteristicId === Number(selectedCharacteristicId.value)
  ) || null;
});
const activePpcIdValue = computed(() => Number(selectedPpcId.value || selectedMapping.value?.id || selectedSummaryRow.value?.partProcessCharacteristicId || 0));
const activeExclusionCount = computed(() => excludedPoints.value.length);

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
    row ? summaryName(row) : ""
  ]);
  if (rowTitleParts.length > 0) return rowTitleParts.join(" - ");

  const mapping = selectedMapping.value;
  const mappingTitleParts = uniqueNonEmptyParts([
    mapping?.process?.processName || mapping?.machine?.machineName,
    formatTankLabel(mapping?.tank),
    mapping?.characteristic ? characteristicName(mapping.characteristic) : ""
  ]);
  return mappingTitleParts.join(" - ") || t('spc.trend.title');
});

async function drawSingleChart(row) {
  const match = mappings.value.find(m => m.id === row.partProcessCharacteristicId);
  if (!match) {
    error.value = { key: 'spc.trend.mappingNotFound' };
    return;
  }
  selectedDimension.value = getDimensionForMapping(match);
  await nextTick();
  updatingCascades.value = true;
  selectedPartId.value = match.partId || "";
  selectedProcessId.value = match.processId;
  selectedCharacteristicId.value = match.characteristicId;
  await nextTick();
  updatingCascades.value = false;
  selectedPpcId.value = String(match.id);
  selectedSummaryRow.value = row;
  tableSummaryData.value = null;
  loadChart();
}

function returnToSummary() {
  chartResult.value = null;
  selectedPointIndex.value = -1;
  hidePointContextMenu();
  excludedPoints.value = [];
  showExcludedPointsPanel.value = false;
  selectedSummaryRow.value = null;
  tableSummaryData.value = cachedSummaryData.value || [];
  if (trendChartInstance) {
    trendChartInstance.dispose();
    trendChartInstance = null;
  }
}

// ─── Watchers ────────────────────────────────────────────────
watch(selectedDimension, (newDim) => {
  selectedPartId.value = "";
  selectedProcessId.value = "";
  selectedCharacteristicId.value = "";
  selectedPpcId.value = "";
  chartResult.value = null;
  tableSummaryData.value = null;
  selectedSummaryRow.value = null;
  hidePointContextMenu();
  excludedPoints.value = [];
  showExcludedPointsPanel.value = false;
});

watch(selectedPartId, () => {
  if (updatingCascades.value) return;
  selectedPpcId.value = "";
  selectedProcessId.value = "";
  selectedCharacteristicId.value = "";
  chartResult.value = null;
  tableSummaryData.value = null;
  selectedSummaryRow.value = null;
  excludedPoints.value = [];
  showExcludedPointsPanel.value = false;
});

watch(selectedProcessId, (newVal) => {
  if (updatingCascades.value) return;
  selectedPpcId.value = "";
  selectedCharacteristicId.value = newVal === "ALL" ? "ALL" : "";
  chartResult.value = null;
  tableSummaryData.value = null;
  selectedSummaryRow.value = null;
  excludedPoints.value = [];
  showExcludedPointsPanel.value = false;
});

watch(selectedCharacteristicId, () => {
  if (updatingCascades.value) return;
  selectedPpcId.value = "";
});

// ─── Data loading ─────────────────────────────────────────────
async function loadMappings() {
  try {
    const [mapRes, typesRes, groupsRes] = await Promise.all([
      api.get("/part-process-characteristics"),
      api.get("/control-chart-types"),
      api.get("/control-chart-groups")
    ]);
    chartTypes.value = typesRes.data || [];
    groups.value = groupsRes.data || [];
    mappings.value = mapRes.data || [];
    if (!dimensionOptions.value.some(group => group.id === selectedDimension.value)) {
      selectedDimension.value = dimensionOptions.value[0]?.id || "PROC";
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
        selectedPartId.value = match.partId || "";
        selectedProcessId.value = match.processId;
        selectedCharacteristicId.value = match.characteristicId;
        await nextTick();
        updatingCascades.value = false;
        selectedPpcId.value = String(match.id);
        loadChart();
      }
    }
  } catch (e) {
    error.value = { key: 'spc.trend.loadMappingsFailed', args: { error: getApiErrorDisplay(e) } };
  }
}

async function loadChart() {
  const start = startDate.value ? new Date(`${startDate.value}T00:00:00`) : null;
  const end = endDate.value ? new Date(`${endDate.value}T00:00:00`) : null;
  if (start && end && start > end) {
    error.value = { key: 'spc.chartQuery.dateOrder' };
    return;
  }
  if (start && end && (end - start) / 86400000 > 93) {
    error.value = { key: 'spc.chartQuery.queryRange' };
    return;
  }

  if (selectedProcessId.value && (!selectedCharacteristicId.value || selectedProcessId.value === "ALL" || selectedCharacteristicId.value === "ALL")) {
    loading.value = true;
    error.value = "";
    chartResult.value = null;
    try {
      const res = await api.get("/v1/spc/summary", {
        params: {
          dimension: selectedDimension.value,
          groupType: selectedDimension.value === "CHEM" ? "TREND_CHART" : undefined,
          partId: selectedPartId.value || undefined,
          processId: selectedProcessId.value !== "ALL" ? Number(selectedProcessId.value) : undefined,
          startDate: startDate.value || undefined,
          endDate: endDate.value || undefined
        }
      });
      tableSummaryData.value = res.data;
      cachedSummaryData.value = res.data;
    } catch (e) {
      error.value = { key: 'spc.trend.loadSummaryFailed', args: { error: getApiErrorDisplay(e) } };
      tableSummaryData.value = null;
    } finally {
      loading.value = false;
    }
    return;
  }

  tableSummaryData.value = null;
  selectedPointIndex.value = -1;
  hidePointContextMenu();

  const mapping = selectedMapping.value;
  if (!mapping) return;

  router.replace({
    path: route.path,
    query: {
      ppcId: mapping.id,
      ...(startDate.value ? { startDate: startDate.value } : {}),
      ...(endDate.value ? { endDate: endDate.value } : {}),
    },
  });

  if (selectedSummaryRow.value && selectedSummaryRow.value.partProcessCharacteristicId !== mapping.id) {
    selectedSummaryRow.value = null;
  }

  loading.value = true;
  error.value = "";
  chartResult.value = null;

  try {
    const res = await api.get("/v1/spc/chart", {
      params: {
        ppcId: mapping.id,
        startDate: startDate.value || undefined,
        endDate: endDate.value || undefined,
        normalityMethod: normalityMethod
      }
    });
    chartResult.value = res.data;
    await loadExcludedPoints(mapping.id);
    await loadPointRemarks(mapping.id);
    loading.value = false;
    await nextTick();
    renderTrendChart();
  } catch (e) {
    if (e?.response?.status === 404) {
      error.value = { key: 'spc.trend.chartNotFound' };
    } else {
      error.value = getApiErrorDisplay(e);
    }
  } finally {
    loading.value = false;
  }
}

function loadSummary() {
  selectedCharacteristicId.value = "";
  selectedSummaryRow.value = null;
  selectedPointIndex.value = -1;
  excludedPoints.value = [];
  showExcludedPointsPanel.value = false;
  loadChart();
}

function selectTrendPoint(index) {
  const points = chartResult.value?.rawDataPoints || [];
  if (index < 0 || index >= points.length) return;
  selectedPointIndex.value = index;
  trendChartInstance?.dispatchAction({ type: "downplay", seriesIndex: 0 });
  trendChartInstance?.dispatchAction({ type: "highlight", seriesIndex: 0, dataIndex: index });
  trendChartInstance?.dispatchAction({ type: "showTip", seriesIndex: 0, dataIndex: index });
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
    reason: "趨勢圖右鍵設定"
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
  return pointKey || t('spc.trend.point');
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

function getHiddenExclusionKeySet() {
  return new Set(excludedPoints.value
    .filter(row => readField(row, "state", "State") === "ExcludedHidden")
    .map(row => {
      const variableId = readField(row, "variableMeasurementId", "VariableMeasurementId");
      const attributeId = readField(row, "attributeMeasurementId", "AttributeMeasurementId");
      if (variableId) return `v:${variableId}`;
      if (attributeId) return `a:${attributeId}`;
      return "";
    })
    .filter(Boolean));
}

function isHiddenExcludedPoint(point, hiddenKeys = getHiddenExclusionKeySet()) {
  if (!point) return false;
  if (point.variableMeasurementId && hiddenKeys.has(`v:${point.variableMeasurementId}`)) return true;
  if (point.attributeMeasurementId && hiddenKeys.has(`a:${point.attributeMeasurementId}`)) return true;
  return false;
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

function openPointRemarkModal() {
  const point = pointContextMenu.value.point;
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
    await loadChart();
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
    await loadChart();
    closePointRemarkModal();
  } catch (e) {
    alert(t('spc.chartActions.clearFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointRemarkSaving.value = false;
  }
}

async function setPointExclusion(state) {
  const payload = getPointExclusionPayload(pointContextMenu.value.point, state);
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
    await loadChart();
  } catch (e) {
    alert(t('spc.chartActions.excludeFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointExclusionSaving.value = false;
  }
}

async function restorePointExclusion() {
  const payload = getPointExclusionPayload(pointContextMenu.value.point, "ExcludedVisible");
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
    await loadExcludedPoints(payload.partProcessCharacteristicId);
    await loadChart();
  } catch (e) {
    alert(t('spc.chartActions.restoreFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointExclusionSaving.value = false;
  }
}

async function restoreExcludedPoint(row) {
  const exclusionId = readField(row, "id", "Id");
  if (!exclusionId) return;
  pointExclusionSaving.value = true;
  try {
    await api.delete(`/v1/spc/point-exclusions/${exclusionId}`);
    await loadChart();
  } catch (e) {
    alert(t('spc.chartActions.restoreRowFailed', { error: getApiErrorMessage(e) }));
  } finally {
    pointExclusionSaving.value = false;
  }
}

function formatPointTime(value) {
  if (!value) return t('spc.chartQuery.notProvided');
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString("zh-TW", { hour12: false });
}

function formatPointValue(value) {
  return value == null || Number.isNaN(Number(value)) ? t('spc.chartQuery.notProvided') : Number(value).toFixed(4);
}

function formatNumber(value, digits = 4) {
  return value === null || value === undefined || Number.isNaN(Number(value)) ? "N/A" : Number(value).toFixed(digits);
}

function formatDifference(value, limit) {
  if (value == null || limit == null || Number.isNaN(Number(value)) || Number.isNaN(Number(limit))) return "未設定";
  const difference = Number(value) - Number(limit);
  return `${difference >= 0 ? "+" : ""}${difference.toFixed(4)}`;
}

const selectedPointDetails = computed(() => {
  const point = chartResult.value?.rawDataPoints?.[selectedPointIndex.value];
  if (!point) return null;
  const sourceLimits = chartResult.value?.limits || {};
  const limits = {
    ...sourceLimits,
    ucl: sourceLimits.ucl ?? (trendStats.value?.ucl !== "N/A" ? trendStats.value?.ucl : null),
    cl: sourceLimits.cl ?? (trendStats.value?.cl !== "N/A" ? trendStats.value?.cl : null),
    lcl: sourceLimits.lcl ?? (trendStats.value?.lcl !== "N/A" ? trendStats.value?.lcl : null)
  };
  const value = Number(point.value);
  const isOos = point.isOutOfSpec === true
    || (limits.usl != null && value > Number(limits.usl))
    || (limits.lsl != null && value < Number(limits.lsl));
  const isOoc = point.isOutOfControl === true
    || (limits.ucl != null && value > Number(limits.ucl))
    || (limits.lcl != null && value < Number(limits.lcl));
  return { point, limits, value, isOos, isOoc };
});

// ─── Chart rendering ──────────────────────────────────────────

const chartLabel = createChartLabelTranslator(t, {"量測值趨勢圖": "spc.trend.title", "顯示各量測點原始數值時序趨勢，含工程規格界限與目標值": "spc.trend.description", "料號 (Product)": "spc.trend.product", "線別 (Line)": "spc.trend.line", "正在載入量測趨勢資料...": "spc.trend.loading", "檢驗特性": "spc.trend.characteristic", "規格界限 (Spec)": "spc.trend.spec", "管制界線 (Control)": "spc.trend.control", "量測點數": "spc.trend.measurementCount", "樣本數 (N)": "spc.trend.sampleN", "平均值 (Mean)": "spc.trend.mean", "標準差 (σ)": "spc.trend.std", "能力指標 (Cpk)": "spc.trend.capability", "全距 (Range)": "spc.trend.range", "原始量測值趨勢": "spc.trend.rawTrend", "顯示工程規格線 (USL / LSL)": "spc.trend.showSpec", "顯示統計管制線 (UCL / LCL)": "spc.trend.showControl", "OOS 超規點": "spc.trend.oos", "正常量測點": "spc.trend.normal", "線條含義對照：": "spc.trend.lineGuide", "USL/LSL (工程規格界線)": "spc.trend.engineeringLimits", ": 客戶規格上下限，超越即為不良品": "spc.trend.specHelp", "目標值": "spc.trend.target", ": 理想生產基準": "spc.trend.targetHelp", "UCL/LCL (統計管制界線)": "spc.trend.statisticalLimits", ": 3σ 預警邊界，越界代表製程異常漂移": "spc.trend.controlHelp", "CL (平均線)": "spc.trend.meanLine", ": 目前數據的平均中心點": "spc.trend.meanHelp", "量測值分布直方圖": "spc.trend.histogram", "依目前趨勢圖 raw data 計算，不套用管制界線與規則判定": "spc.trend.histHelp", "規格界限": "spc.trend.specLimits", "常態性檢定 P-value": "spc.trend.pValue", "量測點明細": "spc.trend.pointDetails", "← 上一筆": "spc.trend.previous", "下一筆 →": "spc.trend.next", "規格判定": "spc.trend.specJudgment", "規格內": "spc.trend.withinSpec", "管制判定": "spc.trend.controlJudgment", "OOC 超出管制": "spc.trend.ooc", "管制內": "spc.trend.withinControl", "統計狀態": "spc.trend.statisticsStatus", "納入統計": "spc.trend.included", "資料來源": "spc.trend.source", "量測人員": "spc.trend.operator", "與界線差距（量測值－界線）": "spc.trend.distance", "請選擇日期與線別後查詢總表，再由總表選擇製圖項目": "spc.trend.emptyHelp", "量測點位": "spc.trend.point", "此期間無原始量測資料": "spc.trend.empty", "找不到該管制項目的設定，可能已被刪除。": "spc.trend.mappingNotFound", "找不到該檢驗項目的量測資料，可能尚無數據或尚未配置管制圖。": "spc.trend.chartNotFound", "常態分布曲線": "spc.chartLabels.normalCurve"});
const htmlChartLabel = value => escapeChartText(chartLabel(value));
const chartPresentations = new WeakMap();
function localizeChart(instance) {
 if(!instance || instance.isDisposed())return;
 if(!chartPresentations.has(instance))chartPresentations.set(instance,captureChartPresentation(instance.getOption()));
 instance.setOption(translateChartPresentation(chartPresentations.get(instance),chartLabel));
 if(instance === trendChartInstance)instance.setOption({title:{text:activeChartDisplayName.value}});
}
watch(locale,async()=>{await nextTick();for(const i of [trendChartInstance,histogramChartInstance])localizeChart(i)});

function renderTrendChart() {
  if (!trendChartEl.value || !chartResult.value) return;

  if (trendChartInstance) {
    trendChartInstance.dispose();
    trendChartInstance = null;
  }
  hidePointContextMenu();

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
    localizeChart(trendChartInstance);
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

  const hiddenExclusionKeys = getHiddenExclusionKeySet();
  const seriesData = rawPoints.map(p => {
    const isHidden = isHiddenExcludedPoint(p, hiddenExclusionKeys);
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
      value: isHidden ? null : (p.value !== undefined ? p.value : null),
      itemStyle: { color, borderColor, borderWidth: 2 },
      symbol, symbolSize, meta: p
    };
  });

  const markLines = [];
  const addLine = (y, name, color, style = "dashed", width = 2) => {
    if (y == null || Number.isNaN(Number(y))) return;
    markLines.push({
      name, yAxis: Number(y),
      lineStyle: { color, width, type: style },
      label: { formatter: `${name}: ${Number(y).toFixed(3)}`, color, position: "end", fontSize: 11, fontWeight: "bold" }
    });
  };

  if (showSpecLimits.value) {
    addLine(limits.usl, "USL", "#ef4444", "dashed", 2);
    addLine(limits.lsl, "LSL", "#ef4444", "dashed", 2);
    addLine(limits.target, "目標值", "#10b981", "solid", 2);
  }

  if (showControlLimits.value) {
    const stats = trendStats.value;
    const uclVal = limits.ucl ?? (stats && stats.ucl !== "N/A" ? Number(stats.ucl) : null);
    const clVal = limits.cl ?? (stats && stats.cl !== "N/A" ? Number(stats.cl) : null);
    const lclVal = limits.lcl ?? (stats && stats.lcl !== "N/A" ? Number(stats.lcl) : null);

    addLine(uclVal, "UCL", "#f59e0b", "dashed", 2);
    addLine(clVal, "CL", "#3b82f6", "solid", 1.5);
    addLine(lclVal, "LCL", "#f59e0b", "dashed", 2);
  }

  const yAxisValues = [
    ...seriesData.map(point => Number(point.value)),
    ...markLines.map(line => Number(line.yAxis))
  ].filter(Number.isFinite);
  let yAxisMin;
  let yAxisMax;
  if (yAxisValues.length > 0) {
    const dataMin = Math.min(...yAxisValues);
    const dataMax = Math.max(...yAxisValues);
    const range = dataMax - dataMin;
    const padding = range > 0
      ? range * 0.08
      : Math.max(Math.abs(dataMin) * 0.08, 1);
    yAxisMin = dataMin - padding;
    yAxisMax = dataMax + padding;
  }

  trendChartInstance.setOption({
    backgroundColor: "transparent",
    title: {
      text: activeChartDisplayName.value,
      left: "center",
      top: 5,
      textStyle: {
        fontSize: 14,
        fontWeight: "bold",
        color: document.documentElement.classList.contains("dark") ? "#e2e8f0" : "#1e293b"
      }
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
          res += `<div style="font-size:10px;color:#94a3b8;margin-bottom:6px">`;
          if (meta.lotNo) res += `<div>${htmlChartLabel("批號")}: <span style="color:#e2e8f0">${escapeChartText(meta.lotNo)}</span></div>`;
          if (meta.serialNo) res += `<div>${htmlChartLabel("序號")}: <span style="color:#e2e8f0">${escapeChartText(meta.serialNo)}</span></div>`;
          if (meta.operator) res += `<div>${htmlChartLabel("人員")}: <span style="color:#e2e8f0">${escapeChartText(meta.operator)}</span></div>`;
          if (meta.pointRemark) res += `<div>${htmlChartLabel("備註")}: <span style="color:#fde68a">${escapeChartText(meta.pointRemark)}</span></div>`;
          res += `</div>`;
        }
        params.forEach(p => {
          if (p.data?.value != null) {
            const v = Number(p.data.value);
            const isOos = (limits.usl != null && v > limits.usl) || (limits.lsl != null && v < limits.lsl);
            res += `<div style="margin-top:4px"><span style="display:inline-block;width:8px;height:8px;border-radius:50%;background:${p.color};margin-right:4px"></span>${htmlChartLabel("量測值")}: <strong style="color:${isOos ? '#f87171' : '#a5b4fc'}">${v.toFixed(4)}</strong>${isOos ? ' ⚠ OOS' : ''}</div>`;
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
    grid: { left: 65, right: 90, top: 55, bottom: 60 },
    xAxis: {
      type: "category", data: labels, boundaryGap: false,
      axisLine: { lineStyle: { color: "#64748b" } },
      axisTick: { lineStyle: { color: "#334155" } },
      axisLabel: { color: "#94a3b8", fontSize: 10, rotate: labels.length > 50 ? 30 : 0 }
    },
    yAxis: {
      type: "value", name: "量測值",
      min: yAxisMin,
      max: yAxisMax,
      nameTextStyle: { color: "#94a3b8", fontSize: 11 },
      splitLine: { lineStyle: { color: "rgba(100,116,139,0.15)" } },
      axisLine: { lineStyle: { color: "#64748b" } },
      axisLabel: { color: "#94a3b8", fontSize: 10, formatter: value => Number(value).toFixed(3) },
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

  localizeChart(trendChartInstance);
  trendChartInstance.on("click", params => {
    hidePointContextMenu();
    if (params.componentType === "series" && params.seriesType === "line" && Number.isInteger(params.dataIndex)) {
      selectTrendPoint(params.dataIndex);
    }
  });

  trendChartInstance.on("contextmenu", params => {
    params.event?.event?.preventDefault?.();
    if (params.componentType !== "series" || params.seriesType !== "line" || !Number.isInteger(params.dataIndex)) return;
    const point = rawPoints[params.dataIndex];
    if (!point) return;
    selectedPointIndex.value = params.dataIndex;
    const nativeEvent = params.event?.event;
    pointContextMenu.value = {
      visible: true,
      x: nativeEvent?.offsetX ?? params.event?.offsetX ?? 0,
      y: nativeEvent?.offsetY ?? params.event?.offsetY ?? 0,
      point
    };
  });

  renderHistogramChart();
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
  const values = (data.rawDataPoints || [])
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
  const min = bins[0]?.min ?? 0;
  const max = bins[bins.length - 1]?.max ?? 0;
  const labels = bins.map(bin => `${formatNumber(bin.min, 3)} - ${formatNumber(bin.max, 3)}`);
  const mean = values.reduce((sum, value) => sum + value, 0) / values.length;
  const variance = values.length > 1
    ? values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / (values.length - 1)
    : 0;
  const std = Math.sqrt(variance);

  const markLines = [];
  const addXAxisLine = (value, name, color, style = "dashed") => {
    const index = findHistogramBinIndex(bins, value);
    if (index === null) return;
    markLines.push({
      name,
      xAxis: index,
      lineStyle: { color, width: 2, type: style },
      label: {
        formatter: `${name}: ${formatNumber(value, 3)}`,
        color,
        position: "end",
        fontSize: 10,
        fontWeight: "bold",
        backgroundColor: "rgba(255,255,255,0.88)",
        borderRadius: 4,
        padding: [2, 4]
      }
    });
  };

  addXAxisLine(limits.lsl, "LSL", "#ef4444", "dashed");
  addXAxisLine(limits.usl, "USL", "#ef4444", "dashed");
  addXAxisLine(limits.target, "Target", "#10b981", "solid");
  addXAxisLine(mean, "Mean", "#0ea5e9", "dotted");

  const hasNormality = data.normality && data.normalCurve && data.normalCurve.length > 0;
  const xAxisList = [{
    type: "category",
    data: labels,
    axisLine: { lineStyle: { color: "#64748b" } },
    axisTick: { alignWithLabel: true, lineStyle: { color: "#334155" } },
    axisLabel: { color: "#94a3b8", fontSize: 10, rotate: labels.length > 8 ? 28 : 0 }
  }];
  if (hasNormality) {
    xAxisList.push({ type: "value", min, max, show: false, axisLine: { show: false } });
  }

  const seriesList = [{
    name: "量測值分布",
    type: "bar",
    xAxisIndex: 0,
    data: bins.map(bin => ({
      value: bin.count,
      itemStyle: { color: "rgba(125, 211, 252, 0.82)", borderRadius: [4, 4, 0, 0] }
    })),
    barMaxWidth: 42,
    markLine: markLines.length > 0 ? { symbol: "none", data: markLines, animation: false } : undefined
  }];

  if (hasNormality) {
    seriesList.push({
      name: "常態分布曲線",
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
      confine: true,
      extraCssText: "max-width:min(320px,calc(100vw - 100px));white-space:normal;overflow-wrap:anywhere;",
      axisPointer: { type: "shadow" },
      backgroundColor: "rgba(15, 23, 42, 0.95)",
      borderColor: "#334155",
      textStyle: { color: "#fff", fontSize: 12 },
      formatter: params => {
        const point = params.find(p => p.seriesName === "量測值分布");
        if (!point) return "";
        const bin = bins[point.dataIndex];
        if (!bin) return "";
        const pValue = data.normality?.pValue !== null && data.normality?.pValue !== undefined
          ? formatNormalityPValue(data.normality.pValue)
          : "N/A";
        return [
          `<div style="font-weight:700;border-bottom:1px solid #334155;padding-bottom:4px;margin-bottom:6px">${htmlChartLabel("量測值區間")}</div>`,
          `<div>${formatNumber(bin.min, 4)} &lt;= X ${point.dataIndex === bins.length - 1 ? "&lt;=" : "&lt;"} ${formatNumber(bin.max, 4)}</div>`,
          `<div style="margin-top:4px">${htmlChartLabel("區間筆數")}: <strong style="color:#a5b4fc">${bin.count}</strong></div>`,
          `<div style="margin-top:4px;color:#94a3b8;font-size:11px">${htmlChartLabel("總樣本數 N")}=${values.length}, ${htmlChartLabel("平均值")}=${formatNumber(mean, 4)}, ${htmlChartLabel("標準差")}=${formatNumber(std, 4)}</div>`,
          data.normality ? `<div style="margin-top:6px;border-top:1px dashed #334155;padding-top:6px;font-size:11px;max-width:260px;white-space:normal;overflow-wrap:anywhere">${htmlChartLabel("常態性檢定")} ${escapeChartText(data.normality.testName || "")} P-value: <strong style="color:#fbbf24">${pValue}</strong>${data.normality.methodNote ? `<div data-testid="normality-method-tooltip" style="margin-top:4px;color:#cbd5e1">${escapeChartText(dtoText(data.normality.methodNoteMessage, data.normality.methodNote))}</div>` : ""}</div>` : ""
        ].join("");
      }
    },
    toolbox: {
      feature: { restore: {}, saveAsImage: { name: "Trend_Histogram" } },
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
  trendChartInstance?.resize();
  histogramChartInstance?.resize();
}

onMounted(() => {
  if (route.query.startDate) startDate.value = String(route.query.startDate);
  if (route.query.endDate) endDate.value = String(route.query.endDate);
  loadMappings();
  window.addEventListener("resize", handleResize);
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", handleResize);
  hidePointContextMenu();
  trendChartInstance?.dispose();
  trendChartInstance = null;
  histogramChartInstance?.dispose();
  histogramChartInstance = null;
});

const showSpecLimits = ref(true);
const showControlLimits = ref(false);

watch([showSpecLimits, showControlLimits], () => {
  renderTrendChart();
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
  const variance = vals.reduce((a, b) => a + (b - mean) ** 2, 0) / Math.max(1, n - 1);
  const std = Math.sqrt(variance);
  const min = Math.min(...vals);
  const max = Math.max(...vals);
  const range = max - min;

  const limits = chartResult.value.limits || {};
  const ucl = limits.ucl ?? (mean + 3 * std);
  const cl = limits.cl ?? mean;
  const lcl = limits.lcl ?? (mean - 3 * std);

  let cpk = "N/A";
  let cp = "N/A";
  if (limits.usl != null && limits.lsl != null && std > 0) {
    cp = ((limits.usl - limits.lsl) / (6 * std)).toFixed(3);
    const cpu = (limits.usl - mean) / (3 * std);
    const cpl = (mean - limits.lsl) / (3 * std);
    cpk = Math.min(cpu, cpl).toFixed(3);
  } else if (limits.usl != null && std > 0) {
    cpk = ((limits.usl - mean) / (3 * std)).toFixed(3);
  } else if (limits.lsl != null && std > 0) {
    cpk = ((mean - limits.lsl) / (3 * std)).toFixed(3);
  }

  return {
    n,
    mean: mean.toFixed(4),
    std: std.toFixed(4),
    min: min.toFixed(4),
    max: max.toFixed(4),
    range: range.toFixed(4),
    ucl: ucl != null && !Number.isNaN(ucl) ? ucl.toFixed(4) : "N/A",
    cl: cl != null && !Number.isNaN(cl) ? cl.toFixed(4) : "N/A",
    lcl: lcl != null && !Number.isNaN(lcl) ? lcl.toFixed(4) : "N/A",
    cp,
    cpk
  };
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
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">{{ t('spc.trend.title') }}</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ t('spc.trend.description') }}</p>
        </div>
      </div>
    </div>

    <!-- Dimension Selector -->
    <div class="flex flex-wrap gap-3 bg-white dark:bg-slate-900 p-2 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <button
        v-for="dim in dimensionOptions"
        :key="dim.id"
        @click="selectedDimension = dim.id"
        :class="[
          'flex-1 min-w-40 py-3 px-4 rounded-xl text-center text-sm font-semibold transition-all duration-200',
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
      <!-- Cascading Selectors -->
      <div class="w-40">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartQuery.startDate') }}</label>
        <input data-testid="trend-startDate" v-model="startDate" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500" />
      </div>

      <div class="w-40">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartQuery.endDate') }}</label>
        <input data-testid="trend-endDate" v-model="endDate" type="date" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500" />
      </div>

      <div v-if="selectedDimension === 'PROD'" class="w-40">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.trend.product') }}</label>
        <select data-testid="trend-selectedPartId" v-model="selectedPartId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500">
          <option value="">{{ t('spc.chartQuery.choosePart') }}</option>
          <option v-for="p in uniqueParts" :key="p.id" :value="p.id">[{{ p.partNo }}] {{ p.partName }}</option>
        </select>
      </div>

      <div class="w-40">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.trend.line') }}</label>
        <select data-testid="trend-selectedProcessId" v-model="selectedProcessId" :disabled="selectedDimension === 'PROD' ? !selectedPartId : false"
          class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-indigo-500 disabled:opacity-50">
          <option value="">{{ t('spc.chartQuery.chooseLine') }}</option>
          <option value="ALL">{{ t('spc.chartQuery.all') }}</option>
          <option v-for="pr in availableProcesses" :key="pr.id" :value="pr.id">{{ pr.processName || pr.processCode }}</option>
        </select>
      </div>

      <div class="w-48">
        <label class="block text-[11px] font-bold text-slate-400 mb-1">{{ t('spc.chartQuery.normalityMethod') }}</label>
        <p data-testid="normality-method" class="px-3 py-2 text-xs font-semibold text-slate-800 dark:text-white">Anderson-Darling</p>
      </div>

      <button
        data-testid="trend-loadSummary" @click="loadSummary"
        :disabled="loading || !selectedProcessId"
        class="flex items-center gap-2 px-5 py-2 rounded-xl bg-gradient-to-r from-indigo-600 to-violet-600 hover:from-indigo-500 hover:to-violet-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 disabled:opacity-50 transition-all"
      >
        <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" />
        {{ t('spc.chartQuery.query') }}
      </button>
    </div>

    <!-- Error -->
    <div v-if="error" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ displayUiMessage(t, error) }}
    </div>

    <!-- Loading -->
    <div v-if="loading" class="h-64 flex flex-col items-center justify-center space-y-3 text-slate-400">
      <Activity class="w-10 h-10 animate-bounce text-indigo-500" />
      <p class="text-sm font-bold">{{ t('spc.trend.loading') }}</p>
    </div>

    <!-- Summary Table -->
    <SpcSummaryTable
      v-if="tableSummaryData && !loading"
      :data="tableSummaryData"
      :loading="loading"
      :isSpc="false"
      @draw-chart="drawSingleChart"
    />

    <!-- Chart Area -->
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
          data-testid="trend-excluded-points-toggle"
          @click="showExcludedPointsPanel = !showExcludedPointsPanel"
          class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-white dark:bg-slate-900 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800 font-bold text-xs shadow-sm transition-colors"
        >
          <List class="w-4 h-4" /> {{ t('spc.chartQuery.excludedCount', { count: activeExclusionCount }) }}
        </button>
      </div>

      <div
        v-if="showExcludedPointsPanel"
        data-testid="trend-excluded-points-panel"
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

      <!-- Active Chart Display Name -->
      <div
        v-if="activeChartDisplayName"
        data-testid="active-chart-display-name"
        class="px-4 py-3 rounded-xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm"
      >
        <p class="text-[11px] font-black uppercase tracking-wider text-slate-400 dark:text-slate-500">{{ t('spc.chartQuery.chartName') }}</p>
        <h2 class="mt-1 text-xl font-black text-slate-900 dark:text-white leading-snug">
          <span data-testid="characteristic-chart-title">{{ activeChartDisplayName }}</span>
        </h2>
      </div>

      <!-- Info Strip -->
      <div v-if="selectedMapping" class="grid grid-cols-1 sm:grid-cols-5 gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm text-xs font-semibold">
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">{{ t('spc.chartQuery.line') }}</span>
          <span class="text-slate-800 dark:text-slate-200">{{ selectedMapping.process?.processName || selectedMapping.process?.processCode }}</span>
        </div>
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">{{ t('spc.trend.characteristic') }}</span>
          <span class="text-slate-800 dark:text-slate-200">
            {{ characteristicName(selectedMapping.characteristic) }}
            <span v-if="selectedMapping.unit" class="text-slate-400">({{ selectedMapping.unit }})</span>
          </span>
        </div>
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">{{ t('spc.trend.spec') }}</span>
          <span class="font-mono text-slate-800 dark:text-slate-200">
            {{ t('spc.trend.specSnapshot', { lsl: selectedMapping.lsl ?? '-∞', target: selectedMapping.targetValue ?? 'N/A', usl: selectedMapping.usl ?? '+∞' }) }}
          </span>
        </div>
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">{{ t('spc.trend.control') }}</span>
          <span class="font-mono text-amber-600 dark:text-amber-400">
            LCL: {{ trendStats?.lcl ?? 'N/A' }} ｜ CL: {{ trendStats?.cl ?? 'N/A' }} ｜ UCL: {{ trendStats?.ucl ?? 'N/A' }}
          </span>
        </div>
        <div class="space-y-1">
          <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">{{ t('spc.trend.measurementCount') }}</span>
          <span class="text-indigo-600 dark:text-indigo-400 font-bold">{{ t('spc.chartQuery.measurementCount', { count: chartResult.rawDataPoints?.length ?? 0 }) }}</span>
        </div>
      </div>

      <!-- Stats Summary Cards -->
      <div v-if="trendStats" class="grid grid-cols-2 sm:grid-cols-4 md:grid-cols-8 gap-3">
        <div v-for="(card) in [
          { key: 'n', label: t('spc.trend.sampleN'), value: trendStats.n, cls: 'text-slate-200' },
          { key: 'mean', label: t('spc.trend.mean'), value: trendStats.mean, cls: 'text-blue-400' },
          { key: 'std', label: t('spc.trend.std'), value: trendStats.std, cls: 'text-indigo-400' },
          { key: 'ucl', label: 'UCL', value: trendStats.ucl, cls: 'text-amber-400' },
          { key: 'cl', label: 'CL', value: trendStats.cl, cls: 'text-sky-400' },
          { key: 'lcl', label: 'LCL', value: trendStats.lcl, cls: 'text-amber-400' },
          { key: 'cpk', label: t('spc.trend.capability'), value: trendStats.cpk, cls: 'text-emerald-400 font-bold' },
          { key: 'range', label: t('spc.trend.range'), value: trendStats.range, cls: 'text-violet-400' }
        ]" :key="card.key"
          class="p-3.5 rounded-2xl bg-gradient-to-tr from-slate-900 to-slate-800 border border-slate-700 shadow-md"
        >
          <p class="text-[10px] font-bold uppercase tracking-wider text-slate-400 truncate">{{ card.label }}</p>
          <h3 class="text-lg font-black mt-1 font-mono truncate" :class="card.cls">{{ card.value }}</h3>
        </div>
      </div>

      <!-- Main Chart -->
      <div v-if="chartResult && !loading" class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-md p-4 relative space-y-4">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-3 pb-3 border-b border-slate-100 dark:border-slate-800">
          <h2 class="text-base font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
            <Sliders class="w-5 h-5 text-indigo-500" />
            {{ t('spc.trend.rawTrend') }}
          </h2>
          <div class="flex flex-wrap items-center gap-4 text-xs font-semibold">
            <!-- Line Display Controls -->
            <label class="flex items-center gap-1.5 cursor-pointer text-slate-700 dark:text-slate-300">
              <input type="checkbox" data-testid="trend-showSpecLimits" v-model="showSpecLimits" class="rounded border-slate-300 text-red-600 focus:ring-red-500" />
              <span>{{ t('spc.trend.showSpec') }}</span>
            </label>
            <label class="flex items-center gap-1.5 cursor-pointer text-slate-700 dark:text-slate-300">
              <input type="checkbox" data-testid="trend-showControlLimits" v-model="showControlLimits" class="rounded border-slate-300 text-amber-600 focus:ring-amber-500" />
              <span>{{ t('spc.trend.showControl') }}</span>
            </label>

            <span class="h-4 w-[1px] bg-slate-200 dark:bg-slate-700"></span>

            <span class="flex items-center gap-1.5 text-slate-500"><span class="w-2.5 h-2.5 rounded-full bg-red-500 inline-block"></span>{{ t('spc.trend.oos') }}</span>
            <span class="flex items-center gap-1.5 text-slate-500"><span class="w-2.5 h-2.5 rounded-full bg-indigo-500 inline-block"></span>{{ t('spc.trend.normal') }}</span>
            <span class="flex items-center gap-1.5 text-slate-500"><span class="w-2.5 h-2.5 rounded-full bg-slate-400 inline-block border border-slate-500"></span>{{ t('spc.chartQuery.excluded') }}</span>
          </div>
        </div>

        <ChartSegmentDetails v-if="activePpcIdValue" :ppc-id="activePpcIdValue" />

        <!-- Line Meaning Guide Bar -->
        <div class="p-3 bg-slate-50 dark:bg-slate-800/60 rounded-xl border border-slate-200/80 dark:border-slate-700/60 text-xs flex flex-wrap gap-x-6 gap-y-2 text-slate-600 dark:text-slate-300">
          <div class="flex items-center gap-1.5 font-medium">
            <Info class="w-4 h-4 text-indigo-500 flex-shrink-0" />
            <span class="font-bold text-slate-800 dark:text-slate-200">{{ t('spc.trend.lineGuide') }}</span>
          </div>
          <div v-if="showSpecLimits" class="flex items-center gap-1.5">
            <span class="w-3 h-0.5 border-t-2 border-dashed border-red-500 inline-block"></span>
            <span><strong class="text-red-500">{{ t('spc.trend.engineeringLimits') }}</strong>{{ t('spc.trend.specHelp') }}</span>
          </div>
          <div v-if="showSpecLimits" class="flex items-center gap-1.5">
            <span class="w-3 h-0.5 bg-emerald-500 inline-block"></span>
            <span><strong class="text-emerald-600 dark:text-emerald-400">{{ t('spc.trend.target') }}</strong>{{ t('spc.trend.targetHelp') }}</span>
          </div>
          <div v-if="showControlLimits" class="flex items-center gap-1.5">
            <span class="w-3 h-0.5 border-t-2 border-dashed border-amber-500 inline-block"></span>
            <span><strong class="text-amber-500">{{ t('spc.trend.statisticalLimits') }}</strong>{{ t('spc.trend.controlHelp') }}</span>
          </div>
          <div v-if="showControlLimits" class="flex items-center gap-1.5">
            <span class="w-3 h-0.5 bg-blue-500 inline-block"></span>
            <span><strong class="text-blue-500">{{ t('spc.trend.meanLine') }}</strong>{{ t('spc.trend.meanHelp') }}</span>
          </div>
        </div>

        <div class="relative">
          <div ref="trendChartEl" data-testid="chemical-trend-chart" class="h-[480px] w-full min-h-[320px]"></div>
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
              data-testid="trend-openPointRemarkModal" @click="openPointRemarkModal"
            >
              {{ t('spc.chartActions.editRemark') }}
            </button>
            <button
              type="button"
              class="w-full px-3 py-2 text-left text-xs font-bold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-50"
              :disabled="pointExclusionSaving"
              @click="setPointExclusion('ExcludedVisible')"
            >
              {{ t('spc.chartQuery.visible') }}
            </button>
            <button
              type="button"
              class="w-full px-3 py-2 text-left text-xs font-bold text-slate-700 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800 disabled:opacity-50"
              :disabled="pointExclusionSaving"
              @click="setPointExclusion('ExcludedHidden')"
            >
              {{ t('spc.chartQuery.hidden') }}
            </button>
            <button
              type="button"
              class="w-full px-3 py-2 text-left text-xs font-bold text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-950/40 disabled:opacity-50"
              :disabled="pointExclusionSaving"
              @click="restorePointExclusion"
            >
              {{ t('spc.chartQuery.restore') }}
            </button>
          </div>
        </div>

        <div
          v-if="pointRemarkModal.visible" data-testid="trend-remark-overlay"
          class="fixed inset-0 z-40 flex items-center justify-center bg-slate-950/40 p-4"
          @click.self="closePointRemarkModal"
        >
          <div data-testid="trend-remark-dialog" role="dialog" :aria-label="t('spc.chartActions.remark')" class="w-full max-w-md max-h-[calc(100dvh-2rem)] overflow-y-auto rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 shadow-2xl p-4">
            <div class="mb-3 flex flex-wrap items-center justify-between gap-2"><h3 class="text-sm font-black text-slate-900 dark:text-white">{{ t('spc.chartActions.remark') }}</h3><LocalePicker /></div>
            <textarea
              data-testid="trend-pointRemarkModal.text" v-model="pointRemarkModal.text"
              maxlength="500"
              rows="5"
              class="w-full rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 px-3 py-2 text-sm text-slate-800 dark:text-slate-100 focus:outline-none focus:ring-2 focus:ring-indigo-500"
              :placeholder="t('spc.chartActions.remarkPlaceholder')"
            ></textarea>
            <div class="mt-2 flex items-center justify-between gap-3">
              <span class="text-xs text-slate-500">{{ pointRemarkModal.text.length }}/500</span>
              <div class="flex items-center gap-2">
                <button
                  type="button"
                  class="px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 text-xs font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 disabled:opacity-50"
                  :disabled="pointRemarkSaving"
                  data-testid="trend-clearPointRemark" @click="clearPointRemark"
                >
                  {{ t('spc.chartActions.clear') }}
                </button>
                <button
                  type="button"
                  class="px-3 py-2 rounded-lg border border-slate-200 dark:border-slate-700 text-xs font-bold text-slate-600 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800"
                  data-testid="trend-closePointRemarkModal" @click="closePointRemarkModal"
                >
                  {{ t('spc.chartActions.cancel') }}
                </button>
                <button
                  type="button"
                  class="px-3 py-2 rounded-lg bg-indigo-600 text-white text-xs font-bold hover:bg-indigo-700 disabled:opacity-50"
                  :disabled="pointRemarkSaving"
                  data-testid="trend-savePointRemark" @click="savePointRemark"
                >
                  {{ t('spc.chartActions.save') }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Distribution Histogram -->
        <div class="pt-5 border-t border-slate-200 dark:border-slate-800 space-y-4">
          <div class="flex flex-col md:flex-row md:items-start justify-between gap-3">
            <div>
              <h2 class="text-base font-bold text-slate-800 dark:text-slate-100 flex items-center gap-2">
                <Activity class="w-5 h-5 text-indigo-500" />
                {{ t('spc.trend.histogram') }}
              </h2>
              <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">{{ t('spc.trend.histHelp') }}</p>
            </div>
            <div class="flex flex-wrap items-center gap-3 text-xs font-semibold text-slate-500">
              <span class="flex items-center gap-1.5"><span class="w-3 h-0.5 border-t-2 border-dashed border-red-500 inline-block"></span>{{ t('spc.trend.specLimits') }}</span>
              <span class="flex items-center gap-1.5"><span class="w-3 h-0.5 bg-emerald-500 inline-block"></span>Target</span>
              <span class="flex items-center gap-1.5"><span class="w-3 h-0.5 border-t-2 border-dotted border-sky-500 inline-block"></span>Mean</span>
            </div>
          </div>

          <div v-if="chartResult.normality" class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div class="rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950/40 p-3">
              <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">{{ t('spc.trend.pValue') }}</span>
              <p class="mt-1 text-sm font-black font-mono text-amber-600 dark:text-amber-400">{{ chartResult.normality.pValue !== null ? formatNormalityPValue(chartResult.normality.pValue) : 'N/A' }}</p>
            </div>
            <div class="rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950/40 p-3">
              <span class="block text-[10px] text-slate-400 uppercase font-bold tracking-wider">{{ t('spc.chartLabels.normality') }}</span>
              <p class="mt-1">
                <span v-if="normalityConclusion(chartResult.normality) === '無法檢定'" class="px-2 py-0.5 rounded text-[10px] font-bold bg-slate-100 text-slate-500 dark:bg-slate-800 dark:text-slate-400">{{ t('spc.chartLabels.unavailable') }}</span>
                <span v-else-if="normalityConclusion(chartResult.normality) === '未拒絕常態假設'" class="px-2 py-0.5 rounded text-[10px] font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">{{ t('spc.chartLabels.notRejected') }}</span>
                <span v-else class="px-2 py-0.5 rounded text-[10px] font-bold bg-rose-500/10 text-rose-600 dark:text-rose-400 border border-rose-500/20">{{ t('spc.chartLabels.rejected') }}</span>
              </p>
            </div>
          </div>

          <p v-if="normalityUnavailableReason(chartResult.normality)" data-testid="normality-unavailable" class="text-xs text-amber-700 dark:text-amber-300">
            {{ dtoText(chartResult.normality?.noteMessage, chartResult.normality?.note) || chartLabel(normalityUnavailableReason(chartResult.normality)) }}
          </p>
          <p v-if="chartResult.normality?.methodNote" class="text-xs text-slate-500 dark:text-slate-400">
            {{ dtoText(chartResult.normality.methodNoteMessage, chartResult.normality.methodNote) }}
          </p>

          <div ref="histogramChartEl" data-testid="normality-histogram" class="h-[300px] w-full min-h-[240px]"></div>
        </div>

        <div
          v-if="selectedPointDetails"
          data-testid="trend-point-details"
          class="rounded-2xl border border-indigo-200 dark:border-indigo-800 bg-indigo-50/70 dark:bg-indigo-950/20 p-4 space-y-4"
        >
          <ChemicalCalculationDetails v-if="activePpcIdValue" :ppc-id="activePpcIdValue" :measurement-id="selectedPointDetails.point.variableMeasurementId||null" point-mode />
          <div class="flex flex-wrap items-center justify-between gap-3">
            <div>
              <p class="text-xs font-bold text-indigo-500">{{ t('spc.trend.pointDetails') }}</p>
              <h3 class="mt-1 text-lg font-black text-slate-900 dark:text-white">
                {{ t('spc.trend.pointHeader', { point: selectedPointIndex + 1, date: formatPointTime(selectedPointDetails.point.measuredAt) }) }}
              </h3>
            </div>
            <div class="flex items-center gap-2">
              <button
                type="button"
                :disabled="selectedPointIndex <= 0"
                @click="selectTrendPoint(selectedPointIndex - 1)"
                class="px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-bold disabled:opacity-40"
              >
                {{ t('spc.trend.previous') }}
              </button>
              <button
                type="button"
                :disabled="selectedPointIndex >= (chartResult.rawDataPoints?.length || 0) - 1"
                @click="selectTrendPoint(selectedPointIndex + 1)"
                class="px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-bold disabled:opacity-40"
              >
                {{ t('spc.trend.next') }}
              </button>
            </div>
          </div>

          <div class="grid grid-cols-2 md:grid-cols-4 gap-3 text-sm">
            <div class="rounded-xl bg-white dark:bg-slate-900 p-3 border border-slate-200 dark:border-slate-800">
              <p class="text-xs text-slate-400">{{ t('spc.chartActions.value') }}</p>
              <p class="mt-1 text-xl font-black font-mono">{{ formatPointValue(selectedPointDetails.point.value) }}</p>
            </div>
            <div class="rounded-xl bg-white dark:bg-slate-900 p-3 border border-slate-200 dark:border-slate-800">
              <p class="text-xs text-slate-400">{{ t('spc.trend.specJudgment') }}</p>
              <p class="mt-1 font-black" :class="selectedPointDetails.isOos ? 'text-red-600' : 'text-emerald-600'">
                {{ selectedPointDetails.isOos ? t('spc.chartActions.oos') : t('spc.trend.withinSpec') }}
              </p>
            </div>
            <div class="rounded-xl bg-white dark:bg-slate-900 p-3 border border-slate-200 dark:border-slate-800">
              <p class="text-xs text-slate-400">{{ t('spc.trend.controlJudgment') }}</p>
              <p class="mt-1 font-black" :class="selectedPointDetails.isOoc ? 'text-amber-600' : 'text-emerald-600'">
                {{ selectedPointDetails.isOoc ? t('spc.trend.ooc') : t('spc.trend.withinControl') }}
              </p>
            </div>
            <div class="rounded-xl bg-white dark:bg-slate-900 p-3 border border-slate-200 dark:border-slate-800">
              <p class="text-xs text-slate-400">{{ t('spc.trend.statisticsStatus') }}</p>
              <p class="mt-1 font-black" :class="selectedPointDetails.point.isExcluded ? 'text-slate-500' : 'text-blue-600'">
                {{ selectedPointDetails.point.isExcluded ? t('spc.chartQuery.excludedFallback') : t('spc.trend.included') }}
              </p>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
            <div v-if="selectedPointDetails.point.pointRemark" class="md:col-span-2 rounded-xl bg-amber-50 dark:bg-amber-950/30 p-4 border border-amber-200 dark:border-amber-900/50 flex items-start gap-2.5">
              <StickyNote class="w-4 h-4 text-amber-500 mt-1" />
              <div>
                <p class="font-black text-amber-700 dark:text-amber-300 mb-1">{{ t('spc.chartActions.remark') }}</p>
                <p class="text-sm font-semibold text-slate-700 dark:text-slate-200 whitespace-pre-wrap">{{ selectedPointDetails.point.pointRemark }}</p>
              </div>
            </div>
            <div class="rounded-xl bg-white dark:bg-slate-900 p-4 border border-slate-200 dark:border-slate-800">
              <p class="font-black text-slate-700 dark:text-slate-200 mb-2">{{ t('spc.trend.source') }}</p>
              <div class="grid grid-cols-2 gap-x-4 gap-y-2">
                <span class="text-slate-400">{{ t('spc.chartLabels.lot') }}</span><span>{{ selectedPointDetails.point.lotNo || t('spc.chartQuery.notProvided') }}</span>
                <span class="text-slate-400">{{ t('spc.chartLabels.serial') }}</span><span>{{ selectedPointDetails.point.serialNo || t('spc.chartQuery.notProvided') }}</span>
                <span class="text-slate-400">{{ t('spc.trend.operator') }}</span><span>{{ selectedPointDetails.point.operator || t('spc.chartQuery.notProvided') }}</span>
              </div>
            </div>
            <div class="rounded-xl bg-white dark:bg-slate-900 p-4 border border-slate-200 dark:border-slate-800">
              <p class="font-black text-slate-700 dark:text-slate-200 mb-2">{{ t('spc.trend.distance') }}</p>
              <div class="grid grid-cols-2 gap-x-4 gap-y-2 font-mono">
                <span class="text-slate-400">USL</span><span>{{ formatDifference(selectedPointDetails.value, selectedPointDetails.limits.usl) }}</span>
                <span class="text-slate-400">LSL</span><span>{{ formatDifference(selectedPointDetails.value, selectedPointDetails.limits.lsl) }}</span>
                <span class="text-slate-400">UCL</span><span>{{ formatDifference(selectedPointDetails.value, selectedPointDetails.limits.ucl) }}</span>
                <span class="text-slate-400">LCL</span><span>{{ formatDifference(selectedPointDetails.value, selectedPointDetails.limits.lcl) }}</span>
                <span class="text-slate-400">{{ t('spc.trend.target') }}</span><span>{{ formatDifference(selectedPointDetails.value, selectedPointDetails.limits.target) }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

    </div>

    <!-- Empty state -->
    <div v-if="!chartResult && !tableSummaryData && !loading && !error" class="h-64 flex flex-col items-center justify-center space-y-3 text-slate-400 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
      <TrendingUp class="w-12 h-12 text-indigo-300" />
      <p class="text-sm font-bold">{{ t('spc.trend.emptyHelp') }}</p>
    </div>
  </section>
</template>
