<script setup>
import ModuleGuide from "../components/ModuleGuide.vue";
import { onBeforeUnmount, onMounted, ref, computed, watch, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import {
  FolderTree,
  Plus,
  Search,
  Edit,
  Trash2,
  X,
  Save,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  RefreshCw,
  Sliders,
  Package,
  Layers,
  Activity,
  Info,
  Cpu,
  Code2
} from "lucide-vue-next";

const rows = ref([]);
const parts = ref([]);
const processes = ref([]);
const filterProcesses = ref([]);
const formProcesses = ref([]);
const currentStep = ref(1);
const characteristics = ref([]);
const chartTypes = ref([]);
const ruleGroups = ref([]);
const ruleLibrary = ref([]);
const categories = ref([]);
const groups = ref([]);
const machines = ref([]);
const lines = ref([]);
const tanks = ref([]);
const tableScroll = ref(null);
const tableScrollLeft = ref(0);
const tableScrollMax = ref(0);

const route = useRoute();
const router = useRouter();

const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all");
const processFilter = ref("all");
const scopeFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const advancedMode = ref(localStorage.getItem("ppcAdvancedMode") === "true");
const formMode = ref("quick"); // 'quick', 'advanced'

const form = ref({
  controlScope: "PRODUCT",
  partId: null,
  processId: null,
  machineId: null,
  tankId: null,
  characteristicId: null,
  unit: "",
  usl: null,
  lsl: null,
  ucl: null,
  cl: null,
  lcl: null,
  targetValue: null,
  sampleSize: 5,
  displayMode: "CONTROL_CHART",
  chartTypeId: null,
  formulaConfigJson: "",
  selectedRuleCodes: [],
  isRequired: true,
  isEnabled: true
});
const formErr = ref("");

function normalizeControlScope(scope) {
  const value = String(scope || "PRODUCT").trim().toUpperCase();
  if (["PROD", "PRODUCT"].includes(value)) return "PRODUCT";
  if (["PROC", "PROCESS"].includes(value)) return "PROCESS";
  if (["CHEM", "CHEMICAL"].includes(value)) return "CHEMICAL";
  return value;
}

const controlScopes = computed(() => {
  const labelDefaults = {
    PRODUCT: "產品管制",
    PROCESS: "製程管制",
    CHEMICAL: "藥液管制"
  };
  const scopeMap = new Map();
  const enabledGroups = groups.value.filter(g => g.isEnabled !== false);
  const scopeIds = new Set([
    ...enabledGroups.map(g => normalizeControlScope(g.groupCode)),
    ...rows.value.map(row => normalizeControlScope(row.controlScope))
  ]);
  scopeIds.forEach(id => {
      if (scopeMap.has(id)) return;
      const group = enabledGroups.find(g => normalizeControlScope(g.groupCode) === id);
      let tone = "blue";
      if (id === "PROCESS") tone = "purple";
      else if (id === "CHEMICAL") tone = "teal";
      else if (id === "PRODUCT") tone = "blue";
      else if (id.includes("DUST") || group?.groupName?.includes("落塵")) tone = "amber";
      else tone = "slate";

      scopeMap.set(id, {
        id,
        label: group?.groupName || labelDefaults[id] || id,
        tone: tone
      });
  });
  return [...scopeMap.values()];
});

const filterControlScopes = computed(() => {
  const scopesWithRows = new Set(rows.value.map(row => normalizeControlScope(row.controlScope)));
  return controlScopes.value.filter(scope => scopesWithRows.has(scope.id));
});

const formulaOptions = [
  { id: "", label: "系統標準公式" },
  {
    id: JSON.stringify({
      XbarCalculationMethod: "STANDARD_RANGE",
      UclFormula: "XDoubleBar + (A2 * Rbar)",
      LclFormula: "XDoubleBar - (A2 * Rbar)"
    }),
    label: "標準全距法"
  },
  {
    id: JSON.stringify({
      XbarCalculationMethod: "MOVING_RANGE_OF_XBAR",
      MrMultiplier: 2.66,
      UclFormula: "XDoubleBar + (2.66 * MRbar_Xbar)",
      LclFormula: "XDoubleBar - (2.66 * MRbar_Xbar)"
    }),
    label: "平均移動全距法"
  },
  {
    id: JSON.stringify({
      XbarCalculationMethod: "SIGMA_METHOD",
      Multiplier: 3,
      UclFormula: "XDoubleBar + (3.0 * S_Xbar)",
      LclFormula: "XDoubleBar - (3.0 * S_Xbar)"
    }),
    label: "樣本標準差法"
  }
];

function formulaLabel(config) {
  if (!config) return "系統標準公式";
  try {
    const parsed = JSON.parse(config);
    const method = parsed.XbarCalculationMethod;
    if (method === "MOVING_RANGE_OF_XBAR" || method === "MR_METHOD") return "平均移動全距法";
    if (method === "SIGMA_METHOD" || method === "SAMPLE_STD_DEV") return "樣本標準差法";
    return "標準全距法";
  } catch {
    return "自訂公式";
  }
}

const scopeLabel = (scope) => controlScopes.value.find(s => s.id === normalizeControlScope(scope))?.label || "產品管制";

function updateTableScrollMax() {
  const body = tableScroll.value;
  if (!body) return;
  tableScrollMax.value = Math.max(0, body.scrollWidth - body.clientWidth);
  tableScrollLeft.value = Math.min(body.scrollLeft, tableScrollMax.value);
}

function syncTableScroll() {
  updateTableScrollMax();
}

function setTableScrollFromSlider() {
  const body = tableScroll.value;
  if (!body) return;
  body.scrollLeft = Number(tableScrollLeft.value) || 0;
  updateTableScrollMax();
}

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [resMain, resParts, resProc, resChar, resTypes, resRules, resRuleLibrary, resCats, resGroups, resMachines, resLines, resTanks] = await Promise.all([
      api.get("/part-process-characteristics"),
      api.get("/parts"),
      api.get("/processes"),
      api.get("/characteristics"),
      api.get("/control-chart-types"),
      api.get("/spc-rule-groups"),
      api.get("/spc-rules", { params: { libraryOnly: true } }),
      api.get("/control-chart-categories"),
      api.get("/control-chart-groups"),
      api.get("/machines"),
      api.get("/v1/traceability-master/lines"),
      api.get("/v1/traceability-master/tanks")
    ]);
    rows.value = resMain.data || [];
    parts.value = resParts.data || [];
    processes.value = resProc.data || [];
    formProcesses.value = processes.value.filter(p => normalizeControlScope(p.controlScope) === normalizeControlScope(form.value.controlScope));
    characteristics.value = resChar.data || [];
    chartTypes.value = resTypes.data || [];
    ruleGroups.value = resRules.data || [];
    ruleLibrary.value = resRuleLibrary.data || [];
    categories.value = resCats.data || [];
    groups.value = resGroups.data || [];
    machines.value = resMachines.data || [];
    lines.value = resLines.data || [];
    tanks.value = resTanks.data || [];
    
    // Load filtered processes based on initial scopeFilter
    try {
      const resFilterProc = await api.get("/processes", { params: { controlScope: scopeFilter.value, configuredOnly: true } });
      filterProcesses.value = resFilterProc.data || [];
    } catch (e) {
      filterProcesses.value = resProc.data || [];
    }

    await nextTick();
    updateTableScrollMax();
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const chartTypeMap = computed(() => {
  const m = {};
  chartTypes.value.forEach(ct => { m[ct.id] = `${ct.chartTypeCode} (${ct.chartTypeName})`; });
  return m;
});

const ruleGroupMap = computed(() => {
  const m = {};
  ruleGroups.value.forEach(rg => { m[rg.id] = `${rg.ruleGroupCode} (${rg.ruleGroupName})`; });
  return m;
});

function getChartTypeRuleGroupId(chartTypeId) {
  if (!chartTypeId) return null;
  return chartTypes.value.find(ct => ct.id === chartTypeId)?.ruleGroupId || null;
}

function getEffectiveRuleGroupId(item) {
  return item.ruleGroupId || getChartTypeRuleGroupId(item.chartTypeId);
}

function defaultSelectedRuleCodes() {
  const firstRule = [...ruleLibrary.value]
    .sort((a, b) => (Number(a.priority) || 0) - (Number(b.priority) || 0) || (Number(a.id) || 0) - (Number(b.id) || 0))[0];
  return firstRule ? [firstRule.ruleCode] : [];
}

const selectedChartTypeDimension = computed(() => {
  if (!form.value.chartTypeId) return null;
  const type = chartTypes.value.find(t => t.id === Number(form.value.chartTypeId));
  if (!type) return null;
  const group = groups.value.find(g => g.id === type.chartGroupId);
  return group ? group.groupCode : null;
});

function getChartTypeGroupType(chartTypeId) {
  if (!chartTypeId) return "";
  const type = chartTypes.value.find(t => t.id === Number(chartTypeId));
  const group = type ? groups.value.find(g => g.id === type.chartGroupId) : null;
  return group?.groupType || "CONTROL_CHART";
}

const selectedCharacteristic = computed(() =>
  characteristics.value.find(x => x.id === Number(form.value.characteristicId)) || null
);
const availableCharacteristics = computed(() =>
  characteristics.value.filter(x =>
    x.isEnabled !== false &&
    x.isSpcEnabled !== false &&
    normalizeControlScope(x.controlScope) === normalizeControlScope(form.value.controlScope)
  )
);

const selectedChartType = computed(() =>
  chartTypes.value.find(x => x.id === Number(form.value.chartTypeId)) || null
);

function getControlScopeGroupType(scope) {
  const normalizedScope = normalizeControlScope(scope);
  return groups.value.find(group => normalizeControlScope(group.groupCode) === normalizedScope)?.groupType || "CONTROL_CHART";
}

const selectedDisplayModeLabel = computed(() =>
  form.value.displayMode === "TREND_CHART" ? "趨勢圖" : "管制圖"
);

const availableChartTypes = computed(() =>
  chartTypes.value.filter(type => {
    const groupType = getChartTypeGroupType(type.id);
    const dataCategory = selectedCharacteristic.value?.dataCategory;
    return groupType === form.value.displayMode
      && (!dataCategory || type.dataCategory === dataCategory);
  })
);

function findDefaultChartTypeId(mode = "CONTROL_CHART", dataCategory = null) {
  return chartTypes.value.find(type =>
    getChartTypeGroupType(type.id) === mode
    && (!dataCategory || type.dataCategory === dataCategory)
  )?.id || null;
}

const effectiveUnit = computed(() => form.value.unit || selectedCharacteristic.value?.unit || "-");
const isUnitOverridden = computed(() =>
  !!form.value.unit && !!selectedCharacteristic.value?.unit && form.value.unit !== selectedCharacteristic.value.unit
);
const isFormulaOverridden = computed(() => !!form.value.formulaConfigJson);
const hasManualControlLimits = computed(() =>
  form.value.ucl !== null && form.value.ucl !== "" ||
  form.value.cl !== null && form.value.cl !== "" ||
  form.value.lcl !== null && form.value.lcl !== ""
);
const hasItemRules = computed(() => form.value.selectedRuleCodes.length > 0);

const availableMachines = computed(() =>
  machines.value.filter(m => m.processId === Number(form.value.processId))
);

const availableTanks = ref([]);

watch(() => form.value.controlScope, async (newScope) => {
  if (!availableCharacteristics.value.some(x => x.id === Number(form.value.characteristicId))) {
    form.value.characteristicId = null;
    form.value.unit = "";
  }
  form.value.displayMode = getControlScopeGroupType(newScope);
  if (form.value.displayMode === "TREND_CHART") {
    form.value.chartTypeId = null;
    form.value.formulaConfigJson = "";
    form.value.selectedRuleCodes = [];
    form.value.ucl = null;
    form.value.cl = null;
    form.value.lcl = null;
  } else if (!availableChartTypes.value.some(type => type.id === Number(form.value.chartTypeId))) {
    form.value.chartTypeId = availableChartTypes.value[0]?.id || null;
  }
  if (newScope !== "PRODUCT") {
    form.value.partId = null;
  }
  if (newScope !== "CHEMICAL") {
    form.value.machineId = null;
    form.value.tankId = null;
  }
  try {
    const { data } = await api.get("/processes", { params: { controlScope: newScope, configuredOnly: false } });
    formProcesses.value = data || [];
    if (form.value.processId && !formProcesses.value.some(p => p.id === Number(form.value.processId))) {
      form.value.processId = null;
    }
  } catch (e) {
    console.error("無法查詢表單製程資料：", e);
  }
});

watch(scopeFilter, async (newScope) => {
  processFilter.value = "all";
  try {
    const { data } = await api.get("/processes", { params: { controlScope: newScope, configuredOnly: true } });
    filterProcesses.value = data || [];
  } catch (e) {
    console.error("無法查詢製程資料：", e);
  }
});

watch(() => form.value.characteristicId, () => {
  form.value.displayMode = getControlScopeGroupType(form.value.controlScope);
  if (form.value.displayMode === "TREND_CHART") {
    form.value.chartTypeId = null;
  } else if (!availableChartTypes.value.some(type => type.id === Number(form.value.chartTypeId))) {
    form.value.chartTypeId = availableChartTypes.value[0]?.id || null;
  }
});

watch(() => form.value.processId, () => {
  if (!availableMachines.value.some(m => m.id === Number(form.value.machineId))) {
    form.value.machineId = null;
  }
  form.value.tankId = null;
});

watch(() => form.value.machineId, async (newMachineId, oldMachineId) => {
  if (!newMachineId) {
    availableTanks.value = [];
    form.value.tankId = null;
    return;
  }
  try {
    const { data } = await api.get(`/machines/${newMachineId}/tanks`);
    availableTanks.value = data || [];
    if (oldMachineId !== undefined && !availableTanks.value.some(t => t.id === Number(form.value.tankId))) {
      form.value.tankId = null;
    }
  } catch (e) {
    console.error(e);
    availableTanks.value = [];
  }
});

const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const pNo = row.part?.partNo || "";
    const pName = row.part?.partName || "";
    const prCode = row.process?.processCode || "";
    const prName = row.process?.processName || "";
    const chCode = row.characteristic?.characteristicCode || "";
    const chName = row.characteristic?.characteristicName || "";
    const machineCode = row.machine?.machineCode || "";
    const machineName = row.machine?.machineName || "";
    const tankCode = row.tank?.tankCode || "";
    const tankName = row.tank?.tankName || "";

    const matchQuery = !q || 
      pNo.toLowerCase().includes(q) || pName.toLowerCase().includes(q) ||
      prCode.toLowerCase().includes(q) || prName.toLowerCase().includes(q) ||
      chCode.toLowerCase().includes(q) || chName.toLowerCase().includes(q) ||
      machineCode.toLowerCase().includes(q) || machineName.toLowerCase().includes(q) ||
      tankCode.toLowerCase().includes(q) || tankName.toLowerCase().includes(q);

    const matchStatus = statusFilter.value === "all" ||
      (statusFilter.value === "active" && row.isEnabled) ||
      (statusFilter.value === "inactive" && !row.isEnabled);

    const matchProc = processFilter.value === "all" || row.processId === parseInt(processFilter.value);
    const matchScope = scopeFilter.value === "all" || normalizeControlScope(row.controlScope) === normalizeControlScope(scopeFilter.value);

    return matchQuery && matchStatus && matchProc && matchScope;
  });
});

function validateStep(step) {
  formErr.value = "";
  if (step === 1) {
    if (!form.value.controlScope) {
      formErr.value = "請選擇管制類型";
      return false;
    }
    if (!form.value.processId) {
      formErr.value = "請選擇工站製程";
      return false;
    }
    if (form.value.controlScope === "PRODUCT" && !form.value.partId) {
      formErr.value = "請選擇產品料號";
      return false;
    }
    if (form.value.controlScope === "CHEMICAL") {
      if (!form.value.machineId) {
        formErr.value = "請選擇生產設備機台";
        return false;
      }
      if (!form.value.tankId) {
        formErr.value = "請選擇生產槽體";
        return false;
      }
    }
    if (!form.value.characteristicId) {
      formErr.value = "請選擇品質檢驗特性";
      return false;
    }
  } else if (step === 2) {
    if (form.value.sampleSize === null || form.value.sampleSize === undefined || form.value.sampleSize <= 0) {
      formErr.value = "子組大小必須大於 0";
      return false;
    }
    if (form.value.usl !== null && form.value.lsl !== null && Number(form.value.usl) <= Number(form.value.lsl)) {
      formErr.value = "規格上限 (USL) 必須大於規格下限 (LSL)";
      return false;
    }
  }
  return true;
}

function nextStep() {
  if (validateStep(currentStep.value)) {
    currentStep.value += 1;
    scrollToModalTop();
  }
}

function prevStep() {
  if (currentStep.value > 1) {
    currentStep.value -= 1;
    scrollToModalTop();
  }
}

function scrollToModalTop() {
  nextTick(() => {
    const modalBody = document.querySelector(".overflow-y-auto");
    if (modalBody) {
      modalBody.scrollTop = 0;
    }
  });
}

async function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  currentStep.value = 1;
  formMode.value = "quick";
  availableTanks.value = [];
  const characteristic = characteristics.value[0] || null;
  const displayMode = getControlScopeGroupType("PRODUCT");
  
  try {
    const { data } = await api.get("/processes", { params: { controlScope: "PRODUCT", configuredOnly: false } });
    formProcesses.value = data || [];
  } catch (e) {
    console.error(e);
  }

  form.value = {
    controlScope: "PRODUCT",
    partId: parts.value.length > 0 ? parts.value[0].id : null,
    processId: formProcesses.value.length > 0 ? formProcesses.value[0].id : null,
    machineId: null,
    tankId: null,
    characteristicId: characteristic?.id || null,
    unit: characteristic?.unit || "",
    usl: null, lsl: null, ucl: null, cl: null, lcl: null, targetValue: null,
    sampleSize: 5,
    displayMode,
    chartTypeId: displayMode === "CONTROL_CHART" ? findDefaultChartTypeId(displayMode, characteristic?.dataCategory) : null,
    formulaConfigJson: "",
    selectedRuleCodes: displayMode === "CONTROL_CHART" ? defaultSelectedRuleCodes() : [],
    isRequired: true,
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

async function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  currentStep.value = 1;
  formMode.value = "quick";
  formErr.value = "";

  const scope = item.controlScope || (item.partId ? "PRODUCT" : "PROCESS");
  try {
    const { data } = await api.get("/processes", { params: { controlScope: scope, configuredOnly: false } });
    formProcesses.value = data || [];
  } catch (e) {
    console.error(e);
  }

  if (scope === "CHEMICAL" && item.machineId) {
    try {
      const { data } = await api.get(`/machines/${item.machineId}/tanks`);
      availableTanks.value = data || [];
    } catch (e) {
      console.error(e);
      availableTanks.value = [];
    }
  } else {
    availableTanks.value = [];
  }

  const displayMode = getControlScopeGroupType(scope);
  const existingChartTypeId = getChartTypeGroupType(item.chartTypeId) === displayMode ? item.chartTypeId : null;
  const itemDataCategory = item.characteristic?.dataCategory || characteristics.value.find(x => x.id === Number(item.characteristicId))?.dataCategory;

  form.value = {
    controlScope: scope,
    partId: item.partId || null,
    processId: item.processId || null,
    machineId: item.machineId || null,
    tankId: item.tankId || null,
    characteristicId: item.characteristicId || null,
    unit: item.unit ?? item.characteristic?.unit ?? "",
    usl: item.usl ?? null,
    lsl: item.lsl ?? null,
    ucl: item.ucl ?? null,
    cl: item.cl ?? null,
    lcl: item.lcl ?? null,
    targetValue: item.targetValue ?? null,
    sampleSize: item.sampleSize ?? 5,
    displayMode,
    chartTypeId: displayMode === "CONTROL_CHART" ? (existingChartTypeId || findDefaultChartTypeId(displayMode, itemDataCategory)) : null,
    formulaConfigJson: displayMode === "CONTROL_CHART" ? (item.formulaConfigJson || "") : "",
    selectedRuleCodes: [],
    isRequired: item.isRequired ?? true,
    isEnabled: item.isEnabled ?? true
  };
  try {
    const { data } = await api.get(`/part-process-characteristics/${item.id}/rules`);
    form.value.selectedRuleCodes = displayMode === "CONTROL_CHART" ? (data?.rules || [])
      .filter(rule => rule.isSelected)
      .map(rule => rule.ruleCode) : [];
  } catch (e) {
    formErr.value = "載入管制規則失敗：" + getApiErrorMessage(e);
  }
  showModal.value = true;
}

function applyCharacteristicUnit() {
  const characteristic = characteristics.value.find(x => x.id === Number(form.value.characteristicId));
  form.value.unit = characteristic?.unit || "";
}

async function save() {
  if (formMode.value === "quick") {
    form.value.displayMode = form.value.displayMode || "CONTROL_CHART";
    if (form.value.displayMode === "CONTROL_CHART") {
      form.value.chartTypeId = form.value.chartTypeId || findDefaultChartTypeId(form.value.displayMode, selectedCharacteristic.value?.dataCategory);
    }
    const isAttribute = selectedCharacteristic.value?.dataCategory === "Attribute";
    if (form.value.sampleSize === null || form.value.sampleSize === undefined || form.value.sampleSize <= 0) {
      form.value.sampleSize = isAttribute ? 100 : 5;
    }
    if (form.value.displayMode === "CONTROL_CHART" && (!form.value.selectedRuleCodes || form.value.selectedRuleCodes.length === 0)) {
      form.value.selectedRuleCodes = defaultSelectedRuleCodes();
    }
  }

  if (form.value.controlScope === "PRODUCT" && !form.value.partId) {
    formErr.value = "產品管制項目必須選擇產品料號。";
    return;
  }
  if (!form.value.processId || !form.value.characteristicId) {
    formErr.value = "工站製程與檢驗特性皆為必填項目。";
    return;
  }
  if (form.value.controlScope === "CHEMICAL" && (!form.value.machineId || !form.value.tankId)) {
    formErr.value = "藥水管制項目必須選擇線別/機台與槽體。";
    return;
  }
  form.value.displayMode = getControlScopeGroupType(form.value.controlScope);
  if (form.value.displayMode === "CONTROL_CHART" && !form.value.chartTypeId) {
    formErr.value = `請選擇${selectedDisplayModeLabel.value}類型。`;
    return;
  }
  if (form.value.displayMode === "CONTROL_CHART" && getChartTypeGroupType(form.value.chartTypeId) !== form.value.displayMode) {
    formErr.value = `目前選擇的是${selectedDisplayModeLabel.value}，不能同時選擇另一種圖表類型。`;
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = {
      ...form.value,
      controlScope: form.value.controlScope,
      partId: form.value.controlScope === "PRODUCT" ? parseInt(form.value.partId) : null,
      processId: parseInt(form.value.processId),
      machineId: form.value.machineId ? parseInt(form.value.machineId) : null,
      tankId: form.value.controlScope === "CHEMICAL" && form.value.tankId ? parseInt(form.value.tankId) : null,
      characteristicId: parseInt(form.value.characteristicId),
      usl: form.value.usl !== "" && form.value.usl !== null ? parseFloat(form.value.usl) : null,
      lsl: form.value.lsl !== "" && form.value.lsl !== null ? parseFloat(form.value.lsl) : null,
      ucl: form.value.displayMode === "CONTROL_CHART" && form.value.ucl !== "" && form.value.ucl !== null ? parseFloat(form.value.ucl) : null,
      cl: form.value.displayMode === "CONTROL_CHART" && form.value.cl !== "" && form.value.cl !== null ? parseFloat(form.value.cl) : null,
      lcl: form.value.displayMode === "CONTROL_CHART" && form.value.lcl !== "" && form.value.lcl !== null ? parseFloat(form.value.lcl) : null,
      targetValue: form.value.targetValue !== "" && form.value.targetValue !== null ? parseFloat(form.value.targetValue) : null,
      sampleSize: parseInt(form.value.sampleSize) || 1,
      displayMode: form.value.displayMode,
      chartTypeId: form.value.displayMode === "CONTROL_CHART" && form.value.chartTypeId ? parseInt(form.value.chartTypeId) : null,
      formulaConfigJson: form.value.displayMode === "CONTROL_CHART" ? form.value.formulaConfigJson : null
    };

    let savedItem;
    if (modalMode.value === "create") {
      const { data } = await api.post("/part-process-characteristics", payload);
      savedItem = data;
      successAlert("成功建立新檢驗基準");
    } else {
      const { data } = await api.put(`/part-process-characteristics/${currentId.value}`, payload);
      savedItem = data;
      successAlert("成功更新檢驗基準");
    }
    await api.put(`/part-process-characteristics/${savedItem.id}/rules`, {
      selectedRuleCodes: form.value.displayMode === "CONTROL_CHART" ? form.value.selectedRuleCodes : []
    });
    showModal.value = false;
    await load();
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  const pNo = item.controlScope === "PRODUCT" ? (item.part?.partNo || item.partId) : scopeLabel(item.controlScope);
  const cName = item.characteristic?.characteristicName || item.characteristicId;
  if (!confirm(`確定要刪除「${pNo} - ${cName}」的檢驗規範基準嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/part-process-characteristics/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除檢驗基準");
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function successAlert(msg) {
  successMsg.value = msg;
  setTimeout(() => { successMsg.value = ""; }, 3000);
}

const showSegmentModal = ref(false);
const selectedPpc = ref(null);
const segments = ref([]);
const loadingSegments = ref(false);
const isEditingSegment = ref(false);
const segmentFormErr = ref("");

const getTodayStr = () => new Date().toISOString().split("T")[0];
const getThreeMonthsAgoStr = () => {
  const d = new Date();
  d.setMonth(d.getMonth() - 3);
  return d.toISOString().split("T")[0];
};

const segmentForm = ref({
  id: null,
  startDate: getTodayStr(),
  endDate: "",
  ucl: null,
  cl: null,
  lcl: null,
  note: ""
});

const trialForm = ref({
  startDate: getThreeMonthsAgoStr(),
  endDate: getTodayStr()
});
const trialResult = ref(null);
const trialLoading = ref(false);
const trialErr = ref("");

async function openSegmentModal(item) {
  selectedPpc.value = item;
  showSegmentModal.value = true;
  trialResult.value = null;
  trialErr.value = "";
  segmentFormErr.value = "";
  trialForm.value = {
    startDate: getThreeMonthsAgoStr(),
    endDate: getTodayStr()
  };
  resetSegmentForm();
  await loadSegments();
}

async function loadSegments() {
  if (!selectedPpc.value) return;
  loadingSegments.value = true;
  try {
    const res = await api.get("/control-limit-segments", {
      params: { ppcId: selectedPpc.value.id }
    });
    segments.value = res.data || [];
  } catch (e) {
    alert("無法載入分段資訊：" + getApiErrorMessage(e));
  } finally {
    loadingSegments.value = false;
  }
}

function resetSegmentForm() {
  segmentForm.value = {
    id: null,
    startDate: getTodayStr(),
    endDate: "",
    ucl: null,
    cl: null,
    lcl: null,
    note: ""
  };
  isEditingSegment.value = false;
  segmentFormErr.value = "";
}

async function saveSegment() {
  if (!selectedPpc.value) return;
  segmentFormErr.value = "";
  const payload = {
    ...segmentForm.value,
    partProcessCharacteristicId: selectedPpc.value.id,
    ucl: segmentForm.value.ucl !== "" && segmentForm.value.ucl !== null ? parseFloat(segmentForm.value.ucl) : null,
    cl: segmentForm.value.cl !== "" && segmentForm.value.cl !== null ? parseFloat(segmentForm.value.cl) : null,
    lcl: segmentForm.value.lcl !== "" && segmentForm.value.lcl !== null ? parseFloat(segmentForm.value.lcl) : null,
    endDate: segmentForm.value.endDate ? segmentForm.value.endDate : null
  };

  try {
    if (isEditingSegment.value) {
      await api.put(`/control-limit-segments/${segmentForm.value.id}`, payload);
    } else {
      await api.post("/control-limit-segments", payload);
    }
    resetSegmentForm();
    await loadSegments();
    await load();
  } catch (e) {
    segmentFormErr.value = getApiErrorMessage(e);
  }
}

function editSegment(seg) {
  segmentForm.value = {
    id: seg.id,
    startDate: seg.startDate ? seg.startDate.split("T")[0] : "",
    endDate: seg.endDate ? seg.endDate.split("T")[0] : "",
    ucl: seg.ucl,
    cl: seg.cl,
    lcl: seg.lcl,
    note: seg.note || ""
  };
  isEditingSegment.value = true;
  segmentFormErr.value = "";
}

async function deleteSegment(seg) {
  if (!confirm("確定要刪除此分段設定嗎？")) return;
  try {
    await api.delete(`/control-limit-segments/${seg.id}`);
    await loadSegments();
    await load();
  } catch (e) {
    alert("刪除失敗：" + getApiErrorMessage(e));
  }
}

async function runTrialCalculate() {
  if (!selectedPpc.value) return;
  trialLoading.value = true;
  trialErr.value = "";
  trialResult.value = null;
  try {
    const res = await api.post("/v1/spc/trial-calculate", {
      partProcessCharacteristicId: selectedPpc.value.id,
      startDate: trialForm.value.startDate,
      endDate: trialForm.value.endDate
    });
    trialResult.value = res.data;
  } catch (e) {
    trialErr.value = getApiErrorMessage(e);
  } finally {
    trialLoading.value = false;
  }
}

function applyTrialResultToSegment() {
  if (!trialResult.value) return;
  segmentForm.value.ucl = trialResult.value.ucl;
  segmentForm.value.cl = trialResult.value.cl;
  segmentForm.value.lcl = trialResult.value.lcl;
  segmentForm.value.startDate = trialForm.value.startDate;
  segmentForm.value.endDate = trialForm.value.endDate;
}

const formatNumber = (value, digits = 4) => {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return "N/A";
  return Number(value).toFixed(digits);
};

function viewChartOrTrend(item) {
  if (item.displayMode === 'TREND_CHART') {
    router.push({ path: '/trend-chart', query: { ppcId: item.id } });
    return;
  }
  if (!item.chartTypeId) {
    router.push({ path: '/spc', query: { ppcId: item.id } });
    return;
  }
  const ct = chartTypes.value.find(c => c.id === item.chartTypeId);
  if (!ct) {
    router.push({ path: '/spc', query: { ppcId: item.id } });
    return;
  }
  const group = groups.value.find(g => g.id === ct.chartGroupId);
  if (!group) {
    router.push({ path: '/spc', query: { ppcId: item.id } });
    return;
  }
  
  if (group.groupType === 'TREND_CHART') {
    router.push({ path: '/trend-chart', query: { ppcId: item.id } });
  } else {
    router.push({ path: '/spc', query: { ppcId: item.id } });
  }
}

onMounted(async () => {
  await load();
  window.addEventListener("resize", updateTableScrollMax);
  const editId = Number(route.query.editId);
  if (editId) {
    const item = rows.value.find(r => r.id === editId);
    if (item) {
      openEditModal(item);
    }
  }
});

watch(advancedMode, (value) => {
  localStorage.setItem("ppcAdvancedMode", value ? "true" : "false");
});

onBeforeUnmount(() => {
  window.removeEventListener("resize", updateTableScrollMax);
});
</script>

<template>
  <section class="space-y-6">
    <!-- Title & Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-amber-600 to-orange-500 rounded-xl shadow-lg shadow-amber-500/30 text-white">
          <FolderTree class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">SPC 管制項目設定與規格維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">依製程管制、藥水管制與產品管制設定檢驗項目、規格界限與子組樣本數</p>
        </div>
      </div>
      
      <div class="flex items-center gap-3">
        <button
          @click="load"
          type="button"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-sm font-semibold transition-all border border-slate-200 dark:border-slate-700"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> 重新整理
        </button>
        <button
          @click="openCreateModal"
          type="button"
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-500 hover:to-orange-500 text-white text-sm font-bold shadow-lg shadow-amber-500/25 hover:shadow-xl hover:shadow-amber-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> 新增管制項目
        </button>
      </div>
    </div>

    <div class="space-y-6">
    <!-- Guide / Wizard Tip -->
    <ModuleGuide title="模組指南：SPC 管制項目設定">
      <p class="text-xs text-amber-700 dark:text-amber-400/80 mt-1.5 leading-relaxed">
          這是整個 SPC 系統中最核心的設定。請先選擇管制類型：製程管制與藥水管制不需要產品料號；只有產品管制才需要綁定產品料號。<br/>
          💡 <strong>功能說明：</strong> 檢驗數據上傳時，系統會比對這裡設定的規格界限。若未在此處建立管制項目，資料將無法進行 SPC 運算與判圖。
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-amber-700 dark:text-amber-400/80 leading-relaxed">
          <div class="font-black text-amber-900 dark:text-amber-300">SPC 管制項目設定頁面操作說明</div>
          <p><strong>查詢項目：</strong>可用管制類型、料號、製程或檢驗特性搜尋既有管制項目。</p>
          <p><strong>新增項目：</strong>按「新增管制項目」，先選管制類型，再選必要主檔。</p>
          <p><strong>設定規格：</strong>輸入 USL、LSL、目標值、樣本數與是否必檢。</p>
          <p><strong>指定管制圖：</strong>依資料類型選擇 I-MR、XBAR-R、XBAR-S、P、NP、C 或 U 管制圖。</p>
          <p><strong>啟用後使用：</strong>儲存並啟用後，資料匯入、現場量測與管制圖查詢才會套用此基準。</p>
        </div>
    </ModuleGuide>

    <!-- Alert Messages -->
    <div v-if="err" class="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm animate-fade-in">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500" />
      <div class="text-sm font-semibold">{{ err }}</div>
    </div>

    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm animate-fade-in">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ successMsg }}</div>
    </div>

    <!-- Filters & Search Bar -->
    <div class="flex flex-col xl:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-start xl:items-center justify-between">
      <div class="relative w-full xl:w-80">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="搜尋料號、製程或檢驗特性..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-amber-500/50 focus:border-amber-500 transition-all"
        />
      </div>

      <div class="flex flex-wrap items-center gap-3 w-full xl:w-auto">
        <!-- Process Filter Dropdown -->
        <select
          v-model="scopeFilter"
          class="px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all cursor-pointer"
        >
          <option value="all">全部管制類型</option>
          <option v-for="s in filterControlScopes" :key="s.id" :value="s.id">{{ s.label }}</option>
        </select>

        <select
          v-model="processFilter"
          class="px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all cursor-pointer"
        >
          <option value="all">全廠所有製程</option>
          <option v-for="p in filterProcesses" :key="p.id" :value="p.id">
            {{ p.processCode }} - {{ p.processName }}
          </option>
        </select>

        <!-- Status Filter -->
        <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
          <button
            v-for="f in [{id:'all', label:'全部狀態'}, {id:'active', label:'已啟用'}, {id:'inactive', label:'已停用'}]"
            :key="f.id"
            @click="statusFilter = f.id"
            type="button"
            :class="[
              'px-3 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap',
              statusFilter === f.id
                ? 'bg-white dark:bg-slate-700 text-amber-600 dark:text-amber-400 shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
            ]"
          >
            {{ f.label }}
          </button>
        </div>
      </div>
    </div>

    <!-- Data Table Container -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden transition-colors">
      <div v-if="tableScrollMax > 0" class="px-4 py-3 bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-700">
        <div class="flex items-center gap-3">
          <span class="text-[11px] font-bold text-slate-500 dark:text-slate-400 whitespace-nowrap">水平捲動</span>
          <input
            v-model.number="tableScrollLeft"
            type="range"
            min="0"
            :max="tableScrollMax"
            step="1"
            class="w-full h-2 accent-amber-500 cursor-ew-resize"
            aria-label="資料表水平捲動"
            @input="setTableScrollFromSlider"
          />
        </div>
      </div>
      <div ref="tableScroll" class="overflow-x-auto pb-3" @scroll="syncTableScroll">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700 whitespace-nowrap">
              <th class="py-4 px-6 w-16 text-center">ID</th>
              <th class="py-4 px-6">工站製程與生產機台</th>
              <th class="py-4 px-6">管制類型與檢驗特性</th>
              <th class="py-4 px-6">規格限值與抽樣配置</th>
              <th class="py-4 px-6">管制圖與規則來源</th>
              <th class="py-4 px-6 text-center">匯入筆數</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="8" class="py-12 text-center text-slate-400">正在載入檢驗基準清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="8" class="py-12 text-center text-slate-400">找不到相符的檢驗基準資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-amber-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-5 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ item.id }}</td>
              <td class="py-5 px-6">
                <div class="flex items-center gap-2 text-sm font-bold text-slate-900 dark:text-white">
                  <Layers class="w-4 h-4 text-cyan-500" />
                  {{ item.process?.processCode || `Proc #${item.processId}` }}
                  <span class="text-slate-500 text-xs font-normal ml-1">{{ item.process?.processName }}</span>
                </div>
                <!-- Associated Machines List -->
                <div class="mt-2 flex flex-wrap gap-1 items-center">
                  <span class="text-[10px] text-slate-400 font-bold mr-1">配置機台:</span>
                  <span v-if="machines.filter(m => m.processId === item.processId).length === 0" class="text-[10px] text-slate-400 italic">無配置機台</span>
                  <span
                    v-for="mach in machines.filter(m => m.processId === item.processId)"
                    :key="mach.id"
                    class="px-1.5 py-0.5 rounded text-[10px] bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 font-mono font-bold border border-slate-200 dark:border-slate-700"
                  >
                    {{ mach.machineCode }}
                  </span>
                </div>
              </td>
              <td class="py-5 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2 text-sm">
                  <Package v-if="(item.controlScope || 'PRODUCT') === 'PRODUCT'" class="w-4 h-4 text-blue-500" />
                  <Layers v-else class="w-4 h-4" :class="(item.controlScope || 'PRODUCT') === 'CHEMICAL' ? 'text-teal-500' : 'text-purple-500'" />
                  {{ scopeLabel(item.controlScope) }}
                  <span v-if="(item.controlScope || 'PRODUCT') === 'PRODUCT'" class="text-slate-500 text-xs font-normal ml-1">
                    {{ item.part?.partNo || `Part #${item.partId}` }} {{ item.part?.partName }}
                  </span>
                </div>
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-1.5 text-xs mt-2">
                  <Sliders class="w-3.5 h-3.5 text-pink-500 flex-shrink-0" />
                  <span>{{ item.characteristic?.characteristicName || `Char #${item.characteristicId}` }}</span>
                  <span class="text-xs font-mono bg-slate-100 dark:bg-slate-800 px-1.5 py-0.5 rounded text-slate-500">[{{ item.characteristic?.characteristicCode }}]</span>
                  <span v-if="item.unit || item.characteristic?.unit" class="text-xs text-slate-400 font-normal">({{ item.unit || item.characteristic?.unit }})</span>
                  <span v-if="item.isRequired" class="text-[10px] bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 px-1.5 py-0.5 rounded border border-red-200 dark:border-red-800 ml-1 tracking-wider">必檢</span>
                </div>
                <div v-if="(item.controlScope || 'PRODUCT') === 'CHEMICAL'" class="mt-2 flex flex-wrap items-center gap-1.5 text-[10px] font-bold">
                  <span class="px-1.5 py-0.5 rounded bg-teal-50 dark:bg-teal-900/30 text-teal-700 dark:text-teal-300 border border-teal-200 dark:border-teal-800">
                    線別/機台: {{ item.machine?.machineCode || '-' }} {{ item.machine?.machineName || '' }}
                  </span>
                  <span class="px-1.5 py-0.5 rounded bg-cyan-50 dark:bg-cyan-900/30 text-cyan-700 dark:text-cyan-300 border border-cyan-200 dark:border-cyan-800">
                    槽體: {{ item.tank?.tankName || item.tank?.tankCode || '-' }}
                  </span>
                </div>
              </td>
              <td class="py-5 px-6">
                <div class="flex flex-wrap items-center gap-2 text-[11px] font-mono">
                  <div class="flex items-center bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-md overflow-hidden shadow-sm">
                    <span class="px-2 py-1 bg-slate-100 dark:bg-slate-800 text-slate-500 font-bold border-r border-slate-200 dark:border-slate-700">LSL</span>
                    <span class="px-2 py-1 text-slate-700 dark:text-slate-300 font-bold">{{ item.lsl !== null ? item.lsl : '-∞' }}</span>
                  </div>
                  <div v-if="item.targetValue !== null" class="flex items-center bg-white dark:bg-slate-900 border border-amber-200 dark:border-amber-800 rounded-md overflow-hidden shadow-sm">
                    <span class="px-2 py-1 bg-amber-50 dark:bg-amber-900/40 text-amber-600 dark:text-amber-500 font-bold border-r border-amber-200 dark:border-amber-800">TGT</span>
                    <span class="px-2 py-1 text-amber-700 dark:text-amber-400 font-bold">{{ item.targetValue }}</span>
                  </div>
                  <div class="flex items-center bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-md overflow-hidden shadow-sm">
                    <span class="px-2 py-1 bg-slate-100 dark:bg-slate-800 text-slate-500 font-bold border-r border-slate-200 dark:border-slate-700">USL</span>
                    <span class="px-2 py-1 text-slate-700 dark:text-slate-300 font-bold">{{ item.usl !== null ? item.usl : '+∞' }}</span>
                  </div>
                </div>
                <div class="mt-2.5 flex flex-wrap items-center gap-1.5 text-[10px] font-mono text-slate-500">
                  <span class="px-1.5 py-0.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md">UCL: {{ item.ucl !== null ? item.ucl : '自動' }}</span>
                  <span class="px-1.5 py-0.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md font-bold text-amber-600 dark:text-amber-500">CL: {{ item.cl !== null ? item.cl : '自動' }}</span>
                  <span class="px-1.5 py-0.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-md">LCL: {{ item.lcl !== null ? item.lcl : '自動' }}</span>
                  <span class="px-1.5 py-0.5 rounded bg-blue-50 dark:bg-blue-950/40 text-blue-600 dark:text-blue-400 font-bold border border-blue-200 dark:border-blue-800/80">
                    N={{ item.sampleSize || 1 }}
                  </span>
                </div>
              </td>
              <td class="py-5 px-6 text-xs space-y-2">
                <div class="flex items-center gap-1.5">
                  <Activity class="w-3.5 h-3.5 text-indigo-500 flex-shrink-0" />
                  <span v-if="item.displayMode === 'TREND_CHART'" class="font-bold text-indigo-600 dark:text-indigo-400">量測值趨勢圖</span>
                  <span v-else-if="item.chartTypeId" class="font-bold text-indigo-600 dark:text-indigo-400">{{ chartTypeMap[item.chartTypeId] || `圖表 #${item.chartTypeId}` }}</span>
                  <span v-else class="text-slate-400 italic">未設定管制圖</span>
                </div>
                <div v-if="item.displayMode !== 'TREND_CHART'" class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
                  <AlertTriangle class="w-3 h-3 flex-shrink-0 opacity-70" />
                  <span v-if="getEffectiveRuleGroupId(item)" class="font-semibold">
                    {{ item.ruleGroupId ? '項目專屬規則' : (ruleGroupMap[getEffectiveRuleGroupId(item)] || `規則 #${getEffectiveRuleGroupId(item)}`) }}
                  </span>
                  <span v-else class="italic opacity-80">此管制圖未套用規則</span>
                </div>
                <div v-if="item.displayMode !== 'TREND_CHART'" class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
                  <Sliders class="w-3 h-3 flex-shrink-0 opacity-70" />
                  <span class="font-semibold">公式：{{ formulaLabel(item.formulaConfigJson) }}</span>
                </div>
              </td>
              <td class="py-5 px-6 text-center">
                <div
                  class="inline-flex flex-col items-center justify-center min-w-20 px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700"
                  :title="`計量型 ${item.variableMeasurementCount || 0} 筆；計數型 ${item.attributeMeasurementCount || 0} 筆`"
                >
                  <span class="text-lg font-black font-mono text-slate-900 dark:text-white">{{ item.measurementCount || 0 }}</span>
                  <span class="text-[10px] font-bold text-slate-400">筆資料</span>
                </div>
              </td>
              <td class="py-5 px-6 text-center">
                <span
                  :class="[
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide',
                    item.isEnabled
                      ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800'
                      : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700'
                  ]"
                >
                  <span :class="['w-1.5 h-1.5 rounded-full', item.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                  {{ item.isEnabled ? '啟用中' : '已停用' }}
                </span>
              </td>
              <td class="py-5 px-6 text-right space-x-2 whitespace-nowrap">
                <button
                  @click="viewChartOrTrend(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-amber-50 dark:bg-slate-800 hover:bg-amber-100 dark:hover:bg-amber-950 text-amber-600 dark:text-amber-400 transition-all border border-amber-200 dark:border-slate-700"
                  :title="item.displayMode === 'TREND_CHART' ? '查看趨勢圖' : '查看 SPC 管制圖'"
                >
                  <Activity class="w-4 h-4" />
                </button>
                <button
                  v-if="item.displayMode !== 'TREND_CHART'"
                  @click="openSegmentModal(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-purple-50 dark:bg-slate-800 hover:bg-purple-100 dark:hover:bg-purple-950 text-purple-600 dark:text-purple-400 transition-all border border-purple-200 dark:border-slate-700"
                  title="分段管制線與界線試算"
                >
                  <Sliders class="w-4 h-4" />
                </button>
                <button
                  @click="openEditModal(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  title="編輯檢驗基準"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除檢驗基準"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>顯示第 1 至 {{ filteredRows.length }} 項結果（總計 {{ rows.length }} 筆檢驗基準）</span>
      </div>
    </div>
    </div>

    <!-- Slide-over Panel (Add / Edit) -->
    <transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 translate-x-full"
      enter-to-class="opacity-100 translate-x-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-x-0"
      leave-to-class="opacity-0 translate-x-full"
    >
      <div v-if="showModal" class="fixed inset-0 z-50 flex justify-end bg-slate-900/60 backdrop-blur-sm">
        <div class="absolute inset-0 cursor-pointer" @click="showModal = false"></div>
        <div class="relative w-full max-w-4xl h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-amber-600 text-white rounded-xl shadow-md shadow-amber-500/20">
              <FolderTree class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增 SPC 管制項目' : '編輯 SPC 管制項目' }}
            </h3>
          </div>
          <div class="flex items-center gap-2 ml-auto mr-4">
            <button
              @click="formMode = formMode === 'quick' ? 'advanced' : 'quick'"
              type="button"
              class="flex items-center gap-1.5 px-3 py-1.5 rounded-xl border text-xs font-bold transition-all hover:opacity-90"
              :class="formMode === 'quick'
                ? 'bg-amber-50 dark:bg-amber-950/40 text-amber-600 dark:text-amber-400 border-amber-200 dark:border-amber-900/60'
                : 'bg-indigo-50 dark:bg-indigo-950/40 text-indigo-600 dark:text-indigo-400 border-indigo-200 dark:border-indigo-900/60'"
            >
              <span v-if="formMode === 'quick'">⚡ 快速建檔模式</span>
              <span v-else>⚙️ 進階精靈模式</span>
            </button>
          </div>
          <button
            @click="showModal = false"
            type="button"
            class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-all"
          >
            <X class="w-5 h-5" />
          </button>
        </div>

        <form @submit.prevent="save" class="flex flex-col h-full overflow-hidden">
          <div class="flex-1 overflow-y-auto p-6 space-y-5">
            <div v-if="formErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
              <XCircle class="w-4 h-4 flex-shrink-0" /> {{ formErr }}
            </div>

            <!-- Step Indicator (進度指示器) -->
            <div v-show="formMode === 'advanced'" class="px-2 py-4 bg-slate-50 dark:bg-slate-800/40 rounded-2xl border border-slate-200/80 dark:border-slate-700/80 mb-2">
              <div class="flex items-center justify-between max-w-lg mx-auto relative">
                <!-- Background Connecting Line -->
                <div class="absolute left-0 right-0 top-1/2 -translate-y-1/2 h-0.5 bg-slate-200 dark:bg-slate-700 z-0"></div>
                <!-- Active Progress Line -->
                <div 
                  class="absolute left-0 top-1/2 -translate-y-1/2 h-0.5 bg-amber-500 transition-all duration-300 z-0"
                  :style="{ width: currentStep === 1 ? '0%' : currentStep === 2 ? '50%' : '100%' }"
                ></div>

                <!-- Step 1 -->
                <div class="flex flex-col items-center relative z-10">
                  <div 
                    class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-black transition-all border-2"
                    :class="currentStep >= 1 ? 'bg-amber-500 border-amber-500 text-white shadow-md shadow-amber-500/20' : 'bg-slate-100 border-slate-300 text-slate-400 dark:bg-slate-800 dark:border-slate-700'"
                  >
                    <span v-if="currentStep > 1">✓</span>
                    <span v-else>1</span>
                  </div>
                  <span class="text-[11px] font-black mt-1.5" :class="currentStep >= 1 ? 'text-amber-600 dark:text-amber-400' : 'text-slate-400'">基礎對照</span>
                </div>

                <!-- Step 2 -->
                <div class="flex flex-col items-center relative z-10">
                  <div 
                    class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-black transition-all border-2"
                    :class="currentStep >= 2 ? 'bg-amber-500 border-amber-500 text-white shadow-md shadow-amber-500/20' : 'bg-white border-slate-200 text-slate-400 dark:bg-slate-900 dark:border-slate-800'"
                  >
                    <span v-if="currentStep > 2">✓</span>
                    <span v-else>2</span>
                  </div>
                  <span class="text-[11px] font-black mt-1.5" :class="currentStep >= 2 ? 'text-amber-600 dark:text-amber-400' : 'text-slate-400'">規格設定</span>
                </div>

                <!-- Step 3 -->
                <div class="flex flex-col items-center relative z-10">
                  <div 
                    class="w-8 h-8 rounded-full flex items-center justify-center text-xs font-black transition-all border-2"
                    :class="currentStep >= 3 ? 'bg-amber-500 border-amber-500 text-white shadow-md shadow-amber-500/20' : 'bg-white border-slate-200 text-slate-400 dark:bg-slate-900 dark:border-slate-800'"
                  >
                    3
                  </div>
                  <span class="text-[11px] font-black mt-1.5" :class="currentStep >= 3 ? 'text-amber-600 dark:text-amber-400' : 'text-slate-400'">圖表規則</span>
                </div>
              </div>
            </div>

            <!-- STEP 1 CONTAINER -->
            <div v-show="formMode === 'quick' || currentStep === 1" class="space-y-5 animate-fade-in">
              <!-- Control Scope Selector -->
              <div class="space-y-2">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">管制類型 <span class="text-red-500">*</span></label>
                <div class="grid grid-cols-3 gap-2">
                  <button
                    v-for="s in controlScopes"
                    :key="s.id"
                    type="button"
                    @click="form.controlScope = s.id"
                    :class="[
                      'px-3 py-2.5 rounded-xl text-xs font-black border transition-all',
                      form.controlScope === s.id
                        ? 'bg-amber-600 text-white border-amber-600 shadow-md shadow-amber-500/20'
                        : 'bg-slate-50 dark:bg-slate-800 text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700 hover:border-amber-300'
                    ]"
                  >
                    {{ s.label }}
                  </button>
                </div>
              </div>

              <!-- Process Selector (置於最上方) -->
              <div class="space-y-1.5">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">工站製程 <span class="text-red-500">*</span></label>
                <select
                  v-model="form.processId"
                  :required="currentStep === 1"
                  class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
                >
                  <option disabled value="null">-- 選擇製程 --</option>
                  <option v-for="pr in formProcesses" :key="pr.id" :value="pr.id">{{ pr.processCode }} - {{ pr.processName }}</option>
                </select>
              </div>

              <!-- Machines associated with the selected Process -->
              <div v-if="form.processId" class="p-4 bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-2xl text-xs font-semibold text-slate-600 dark:text-slate-300">
                <div class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400 mb-2">
                  <Cpu class="w-4 h-4 text-purple-500" />
                  <span>此工站配置生產設備機台：</span>
                </div>
                <div v-if="machines.filter(m => m.processId === Number(form.processId)).length === 0" class="text-slate-400 dark:text-slate-500 italic">
                  目前無配置任何機台設備。
                </div>
                <div v-else class="flex flex-wrap gap-2">
                  <span
                    v-for="mach in machines.filter(m => m.processId === Number(form.processId))"
                    :key="mach.id"
                    class="px-2 py-1 rounded bg-white dark:bg-slate-700 text-slate-700 dark:text-slate-200 border border-slate-200 dark:border-slate-600 font-mono text-[11px]"
                  >
                    {{ mach.machineCode }} - {{ mach.machineName }}
                  </span>
                </div>
              </div>

              <div v-if="form.controlScope === 'CHEMICAL'" class="p-4 bg-teal-50/60 dark:bg-teal-950/10 border border-teal-200 dark:border-teal-900/70 rounded-2xl space-y-4">
                <div class="flex items-center gap-2 text-xs font-bold text-teal-700 dark:text-teal-300">
                  <Cpu class="w-4 h-4" />
                  <span>藥水管制定位</span>
                </div>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div class="space-y-1.5">
                    <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">線別 / 機台 <span class="text-red-500">*</span></label>
                    <select
                      v-model="form.machineId"
                      :required="form.controlScope === 'CHEMICAL' && currentStep === 1"
                      class="w-full px-3 py-2.5 bg-white dark:bg-slate-900 border border-teal-200 dark:border-teal-800 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all"
                    >
                      <option :value="null">-- 選擇線別/機台 --</option>
                      <option v-for="mach in availableMachines" :key="mach.id" :value="mach.id">{{ mach.machineCode }} - {{ mach.machineName }}</option>
                    </select>
                  </div>

                  <div class="space-y-1.5">
                    <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">槽體 <span class="text-red-500">*</span></label>
                    <select
                      v-model="form.tankId"
                      :required="form.controlScope === 'CHEMICAL' && currentStep === 1"
                      :disabled="!form.machineId"
                      class="w-full px-3 py-2.5 bg-white dark:bg-slate-900 border border-teal-200 dark:border-teal-800 rounded-xl font-bold text-sm text-slate-800 dark:text-white disabled:opacity-60 focus:outline-none focus:ring-2 focus:ring-teal-500 transition-all"
                    >
                      <option :value="null">-- 選擇槽體 --</option>
                      <option v-for="tank in availableTanks" :key="tank.id" :value="tank.id">{{ tank.tankName }} [{{ tank.tankCode }}]</option>
                    </select>
                  </div>
                </div>
                <p v-if="form.machineId && availableTanks.length === 0" class="text-xs text-amber-600 dark:text-amber-400 font-semibold">
                  此線別尚未建立槽體，請先到「線別槽體設定」新增槽體。
                </p>
              </div>

              <!-- Product (Part) & Characteristic (Characteristic) -->
              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div v-if="form.controlScope === 'PRODUCT'" class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider flex items-center justify-between">
                    <span>產品料號 <span class="text-red-500">*</span></span>
                  </label>
                  <select
                    v-model="form.partId"
                    :required="form.controlScope === 'PRODUCT' && currentStep === 1"
                    class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
                  >
                    <option disabled value="null">-- 選擇料號 --</option>
                    <option v-for="p in parts" :key="p.id" :value="p.id">{{ p.partNo }} - {{ p.partName }}</option>
                  </select>
                </div>

                <div v-else class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">產品料號</label>
                  <div class="px-3 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold text-slate-500 dark:text-slate-400">
                    {{ form.controlScope === 'CHEMICAL' ? '藥水管制不需產品料號' : '製程管制不需產品料號' }}
                  </div>
                </div>

                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">檢驗特性 <span class="text-red-500">*</span></label>
                  <select
                    v-model="form.characteristicId"
                    @change="applyCharacteristicUnit"
                    :required="currentStep === 1"
                    class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
                  >
                    <option disabled value="null">-- 選擇特性 --</option>
                    <option v-for="ch in availableCharacteristics" :key="ch.id" :value="ch.id">{{ ch.characteristicCode }} - {{ ch.characteristicName }}</option>
                  </select>
                </div>
              </div>

              <!-- ⚡ 快速建檔模式：新增預設管制圖與顯示類型設定 -->
              <div v-if="formMode === 'quick'" class="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
                <div v-if="form.displayMode === 'CONTROL_CHART'" class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ selectedDisplayModeLabel }}類型</label>
                  <select
                    v-model="form.chartTypeId"
                    class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
                  >
                    <option :value="null">-- 請選擇{{ selectedDisplayModeLabel }}類型 --</option>
                    <option v-for="ct in availableChartTypes" :key="ct.id" :value="ct.id">
                      {{ ct.chartTypeCode }} - {{ ct.chartTypeName }}
                    </option>
                  </select>
                </div>
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">可用圖表</label>
                  <div class="px-3 py-2.5 bg-emerald-50 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-800 rounded-xl text-sm font-bold text-emerald-700 dark:text-emerald-300">
                    {{ selectedDisplayModeLabel }}
                  </div>
                </div>
              </div>
            </div>

            <!-- STEP 2 CONTAINER -->
            <div v-show="formMode === 'quick' || currentStep === 2" class="space-y-5 animate-fade-in">
              <!-- Specs: USL, Target, LSL, Sample Size -->
              <div class="p-4 bg-slate-50 dark:bg-slate-800/60 rounded-2xl border border-slate-200 dark:border-slate-700 space-y-4">
                <h4 class="text-xs font-bold uppercase tracking-wider text-slate-500 dark:text-slate-400 flex items-center gap-2">
                  <Sliders class="w-4 h-4 text-amber-500" /> 規格界限與抽樣配置
                </h4>
                
                <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
                  <div class="space-y-1">
                    <label class="block text-[11px] font-bold text-slate-600 dark:text-slate-400">上限 (USL)</label>
                    <input
                      v-model="form.usl"
                      type="number"
                      step="any"
                      placeholder="如: 10.2"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-mono text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
                    />
                  </div>

                  <div class="space-y-1">
                    <label class="block text-[11px] font-bold text-slate-600 dark:text-slate-400">目標值</label>
                    <input
                      v-model="form.targetValue"
                      type="number"
                      step="any"
                      placeholder="如: 10.0"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-mono text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
                    />
                  </div>

                  <div class="space-y-1">
                    <label class="block text-[11px] font-bold text-slate-600 dark:text-slate-400">下限 (LSL)</label>
                    <input
                      v-model="form.lsl"
                      type="number"
                      step="any"
                      placeholder="如: 9.8"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-mono text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
                    />
                  </div>

                  <div class="space-y-1">
                    <label class="block text-[11px] font-bold text-amber-600 dark:text-amber-400">子組大小 <span class="text-red-500">*</span></label>
                    <input
                      v-model="form.sampleSize"
                      type="number"
                      min="1"
                      max="25"
                      :required="currentStep === 2"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-amber-300 dark:border-amber-700 rounded-xl text-sm font-mono font-bold text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
                    />
                  </div>
                </div>
              </div>

              <!-- Toggles -->
              <div class="grid grid-cols-2 gap-4 pt-2">
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">是否必檢項目</label>
                  <label class="relative inline-flex items-center cursor-pointer">
                    <input v-model="form.isRequired" type="checkbox" class="sr-only peer" />
                    <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-amber-300 dark:peer-focus:ring-amber-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-amber-600"></div>
                    <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isRequired ? '必檢項目' : '非必檢' }}</span>
                  </label>
                </div>

                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">啟用設定</label>
                  <label class="relative inline-flex items-center cursor-pointer">
                    <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                    <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-amber-300 dark:peer-focus:ring-amber-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-amber-600"></div>
                    <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? '啟用' : '停用' }}</span>
                  </label>
                </div>
              </div>
            </div>

            <!-- STEP 3 CONTAINER -->
            <div v-show="formMode === 'quick' || currentStep === 3" class="space-y-5 animate-fade-in">
              <!-- Mode Switcher -->
              <div v-if="formMode === 'advanced'" class="p-1 bg-slate-100 dark:bg-slate-800 rounded-2xl border border-slate-200 dark:border-slate-700 grid grid-cols-2 gap-1">
                <button
                  type="button"
                  @click="advancedMode = false"
                  :class="[
                    'px-4 py-2.5 rounded-xl text-sm font-black transition-all',
                    !advancedMode ? 'bg-white dark:bg-slate-900 text-amber-600 shadow-sm' : 'text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'
                  ]"
                >
                  簡易模式
                </button>
                <button
                  type="button"
                  @click="advancedMode = true"
                  :class="[
                    'px-4 py-2.5 rounded-xl text-sm font-black transition-all',
                    advancedMode ? 'bg-white dark:bg-slate-900 text-amber-600 shadow-sm' : 'text-slate-500 hover:text-slate-800 dark:hover:text-slate-200'
                  ]"
                >
                  進階模式
                </button>
              </div>

              <!-- currently applied info card (!advancedMode) -->
              <div v-if="formMode === 'advanced' && !advancedMode" class="p-4 bg-blue-50/60 dark:bg-blue-950/15 rounded-2xl border border-blue-200 dark:border-blue-900/70 space-y-3">
                <h4 class="text-xs font-bold uppercase tracking-wider text-blue-700 dark:text-blue-300 flex items-center gap-2">
                  <Info class="w-4 h-4" /> 目前套用資訊
                </h4>
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                  <div class="p-3 rounded-xl bg-white dark:bg-slate-900 border border-blue-100 dark:border-blue-900/70">
                    <div class="text-slate-400 font-bold">量測單位</div>
                    <div class="mt-1 font-black text-slate-800 dark:text-white">{{ effectiveUnit }}</div>
                    <div class="mt-1 text-[11px]" :class="isUnitOverridden ? 'text-amber-600 dark:text-amber-400 font-bold' : 'text-slate-400'">
                      {{ isUnitOverridden ? '此管制項目覆寫' : '繼承品質特性主檔' }}
                    </div>
                  </div>
                  <div v-if="form.displayMode === 'CONTROL_CHART'" class="p-3 rounded-xl bg-white dark:bg-slate-900 border border-blue-100 dark:border-blue-900/70">
                    <div class="text-slate-400 font-bold">管制圖類型</div>
                    <div class="mt-1 font-black text-slate-800 dark:text-white">{{ selectedChartType ? `${selectedChartType.chartTypeCode} (${selectedChartType.chartTypeName})` : '繼承特性設定' }}</div>
                    <div class="mt-1 text-[11px] text-slate-400">{{ selectedChartType ? '此管制項目指定' : '未指定項目專屬管制圖' }}</div>
                  </div>
                  <div v-if="form.displayMode === 'CONTROL_CHART'" class="p-3 rounded-xl bg-white dark:bg-slate-900 border border-blue-100 dark:border-blue-900/70">
                    <div class="text-slate-400 font-bold">公式來源</div>
                    <div class="mt-1 font-black text-slate-800 dark:text-white">{{ formulaLabel(form.formulaConfigJson) }}</div>
                    <div class="mt-1 text-[11px] text-amber-600 dark:text-amber-400 font-bold">
                      此管制項目專屬設定
                    </div>
                  </div>
                  <div v-if="form.displayMode === 'CONTROL_CHART'" class="p-3 rounded-xl bg-white dark:bg-slate-900 border border-blue-100 dark:border-blue-900/70">
                    <div class="text-slate-400 font-bold">管制規則</div>
                    <div class="mt-1 font-black text-slate-800 dark:text-white">{{ hasItemRules ? `項目專屬 ${form.selectedRuleCodes.length} 條` : '未啟用規則' }}</div>
                    <div class="mt-1 text-[11px]" :class="hasItemRules ? 'text-amber-600 dark:text-amber-400 font-bold' : 'text-slate-400'">
                      {{ hasItemRules ? '此管制項目專屬設定' : '此項目不執行異常規則' }}
                    </div>
                  </div>
                </div>
                <p class="text-[11px] text-blue-700/70 dark:text-blue-300/70 font-semibold">
                  {{ form.displayMode === 'TREND_CHART' ? '趨勢圖僅使用量測單位、目標值與規格界限。' : '若要調整單位、公式、管制規則或固定 UCL/CL/LCL，請切換到進階模式。' }}
                </p>
              </div>

              <!-- Advanced Override items -->
              <div v-if="formMode === 'quick' || advancedMode" class="space-y-5">
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">量測單位</label>
                  <input
                    v-model.trim="form.unit"
                    type="text"
                    maxlength="50"
                    placeholder="例如：mm、μm、%、mg/L"
                    class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
                  />
                  <p class="text-[11px] text-slate-400">
                    <span :class="isUnitOverridden ? 'text-amber-600 dark:text-amber-400 font-bold' : ''">
                      {{ isUnitOverridden ? '此管制項目已覆寫品質特性單位。' : '目前繼承或沿用品質特性主檔單位。' }}
                    </span>
                    切換檢驗特性時會帶入特性主檔的預設單位，之後可自行修改。
                  </p>
                </div>

                <!-- Custom Control Limits -->
                <div v-if="form.displayMode === 'CONTROL_CHART'" class="p-4 bg-slate-50 dark:bg-slate-800/60 rounded-2xl border border-slate-200 dark:border-slate-700 space-y-4">
                  <span class="text-[11px] font-semibold text-slate-400 dark:text-slate-500 mb-2 block">固定統計管制線 (可留空由系統自動算圖)</span>
                  <div class="grid grid-cols-3 gap-3">
                    <div>
                      <label class="block text-[11px] text-slate-500">UCL</label>
                      <input v-model="form.ucl" type="number" step="any" placeholder="自動計算" class="w-full px-3 py-1.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-mono text-slate-700 dark:text-slate-300" />
                    </div>
                    <div>
                      <label class="block text-[11px] text-slate-500">中心線 (CL)</label>
                      <input v-model="form.cl" type="number" step="any" placeholder="自動計算" class="w-full px-3 py-1.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-mono text-slate-700 dark:text-slate-300" />
                    </div>
                    <div>
                      <label class="block text-[11px] text-slate-500">LCL</label>
                      <input v-model="form.lcl" type="number" step="any" placeholder="自動計算" class="w-full px-3 py-1.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-mono text-slate-700 dark:text-slate-300" />
                    </div>
                  </div>
                  <p class="mt-2 text-[11px]" :class="hasManualControlLimits ? 'text-amber-600 dark:text-amber-400 font-bold' : 'text-slate-400'">
                    {{ hasManualControlLimits ? '目前使用手動固定管制線；分段管制線仍會依日期區間覆寫。' : '目前未手動固定管制線，管制圖會自動計算或套用分段管制線。' }}
                  </p>
                </div>

                <!-- Chart Type -->
                <div v-if="form.displayMode === 'CONTROL_CHART'" class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">圖表顯示方式</label>
                  <div class="px-4 py-3 rounded-xl bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800 text-sm font-bold text-blue-700 dark:text-blue-300">
                    此項目由所屬大類別決定顯示為{{ selectedDisplayModeLabel }}。
                  </div>
                  <p class="text-[11px] text-slate-400">
                    如需變更顯示方式，請調整管制圖大類別的群組類型。
                  </p>
                </div>

                <!-- Bind Chart Type -->
                <div v-if="form.displayMode === 'CONTROL_CHART'" class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">綁定 {{ selectedDisplayModeLabel }} 類型</label>
                  <select
                    v-model="form.chartTypeId"
                    :required="advancedMode && currentStep === 3"
                    class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
                  >
                    <option :value="null" disabled>-- 請選擇{{ selectedDisplayModeLabel }}類型 --</option>
                    <option v-for="ct in availableChartTypes" :key="ct.id" :value="ct.id">{{ ct.chartTypeCode }} - {{ ct.chartTypeName }}</option>
                  </select>
                  <p class="text-[11px] text-slate-400 mt-1">
                    圖表類型只套用於目前管制項目；修改此處不會影響其他資料。
                  </p>
                </div>

                <!-- Formula Config -->
                <div v-if="form.displayMode === 'CONTROL_CHART'" class="p-4 space-y-4 rounded-2xl bg-indigo-50/60 dark:bg-indigo-950/15 border border-indigo-200 dark:border-indigo-900/70">
                  <div>
                    <h4 class="text-xs font-bold uppercase tracking-wider text-indigo-700 dark:text-indigo-300 flex items-center gap-2">
                      <Code2 class="w-4 h-4" /> 公式配置
                    </h4>
                    <p class="mt-1 text-[11px] text-indigo-700/70 dark:text-indigo-300/70">
                      可套用預設公式後再編輯 JSON；設定只影響目前這一個管制項目。
                    </p>
                  </div>

                  <div class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    <button
                      v-for="option in formulaOptions"
                      :key="option.label"
                      type="button"
                      @click="form.formulaConfigJson = option.id"
                      class="p-3 rounded-xl border text-left transition-all"
                      :class="form.formulaConfigJson === option.id
                        ? 'bg-white dark:bg-slate-900 border-indigo-500 ring-2 ring-indigo-500/20 shadow-sm'
                        : 'bg-indigo-50/40 dark:bg-slate-900/40 border-indigo-100 dark:border-slate-800 hover:border-indigo-300'"
                    >
                      <span class="block text-xs font-black text-slate-800 dark:text-slate-100">{{ option.label }}</span>
                      <span class="block mt-1 text-[10px] text-slate-400">
                        {{ option.id ? '套用後可在下方調整內容' : '由所選管制圖類型使用標準常數計算' }}
                      </span>
                    </button>
                  </div>

                  <div class="space-y-1.5">
                    <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">公式設定 JSON</label>
                    <textarea
                      v-model="form.formulaConfigJson"
                      rows="7"
                      spellcheck="false"
                      placeholder="留空代表使用系統標準公式；也可貼入自訂公式 JSON"
                      class="w-full px-4 py-3 bg-slate-900 dark:bg-slate-950 border border-slate-700 rounded-xl font-mono text-xs leading-5 text-green-400 focus:outline-none focus:ring-2 focus:ring-indigo-500 resize-y"
                    ></textarea>
                    <p class="text-[11px] text-slate-400">
                      留空使用系統標準公式；既有自訂公式會完整帶入此欄位，可直接修改。
                    </p>
                  </div>
                </div>

                <!-- Control Rules -->
                <div v-if="form.displayMode === 'CONTROL_CHART'" class="p-4 bg-amber-50/60 dark:bg-amber-950/15 rounded-2xl border border-amber-200 dark:border-amber-900/70 space-y-3">
                  <div>
                    <h4 class="text-xs font-bold uppercase tracking-wider text-amber-700 dark:text-amber-300 flex items-center gap-2">
                      <AlertTriangle class="w-4 h-4" /> 管制規則
                    </h4>
                    <p class="text-[11px] text-amber-700/70 dark:text-amber-300/70 mt-1">
                      規則只套用於目前管制項目；全部不勾選時，此項目不執行異常規則。
                    </p>
                  </div>
                  <div v-if="ruleLibrary.length" class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    <label
                      v-for="rule in ruleLibrary"
                      :key="rule.ruleCode"
                      class="flex items-start gap-2.5 p-3 rounded-xl border cursor-pointer transition-all"
                      :class="form.selectedRuleCodes.includes(rule.ruleCode)
                        ? 'bg-white dark:bg-slate-900 border-amber-400 dark:border-amber-700 shadow-sm'
                        : 'bg-amber-50/40 dark:bg-slate-900/40 border-amber-100 dark:border-slate-800 hover:border-amber-300'"
                    >
                      <input
                        v-model="form.selectedRuleCodes"
                        type="checkbox"
                        :value="rule.ruleCode"
                        class="mt-0.5 w-4 h-4 rounded border-slate-300 text-amber-600 focus:ring-amber-500"
                      />
                      <span class="min-w-0">
                        <span class="block text-xs font-black text-slate-800 dark:text-slate-100">{{ rule.ruleCode }} · {{ rule.ruleName }}</span>
                        <span class="block text-[10px] text-slate-400 mt-0.5">優先序 {{ rule.priority }}</span>
                      </span>
                    </label>
                  </div>
                  <p v-else class="text-xs text-slate-400 italic">尚未建立可選擇的管制規則。</p>
                </div>
              </div>
            </div>
          </div>
          <div class="shrink-0 p-6 bg-slate-50 dark:bg-slate-800/80 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between">
            <div>
              <button
                v-if="formMode === 'advanced' && currentStep > 1"
                @click="prevStep"
                type="button"
                class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all"
              >
                上一步
              </button>
              <button
                v-else
                @click="showModal = false"
                type="button"
                class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all"
              >
                取消
              </button>
            </div>
            <div>
              <button
                v-if="formMode === 'advanced' && currentStep < 3"
                @click="nextStep"
                type="button"
                class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-amber-600 hover:bg-amber-500 text-white text-sm font-bold shadow-lg shadow-amber-500/25 transition-all"
              >
                下一步
              </button>
              <button
                v-else
                type="submit"
                :disabled="loading"
                class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-500 hover:to-orange-500 text-white text-sm font-bold shadow-lg shadow-amber-500/25 hover:shadow-xl hover:shadow-amber-500/40 transition-all transform hover:-translate-y-0.5"
              >
                <Save class="w-4 h-4" /> 確認儲存
              </button>
            </div>
          </div>
        </form>
      </div>
      </div>
    </transition>

    <!-- Slide-over Panel (Segments & Trial Calculate) -->
    <transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 translate-x-full"
      enter-to-class="opacity-100 translate-x-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-x-0"
      leave-to-class="opacity-0 translate-x-full"
    >
      <div v-if="showSegmentModal" class="fixed inset-0 z-50 flex justify-end bg-slate-900/60 backdrop-blur-sm">
        <div class="absolute inset-0 cursor-pointer" @click="showSegmentModal = false"></div>
        <div class="relative w-full max-w-5xl h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          
          <!-- Header -->
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
            <div class="flex items-center gap-3">
              <div class="p-2.5 bg-purple-600 text-white rounded-xl shadow-md shadow-purple-500/20">
                <Sliders class="w-5 h-5" />
              </div>
              <div>
                <h3 class="text-lg font-black text-slate-800 dark:text-white">
                  分段管制線與界線試算
                </h3>
                <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5" v-if="selectedPpc">
                  {{ selectedPpc.process?.processCode }} ({{ selectedPpc.process?.processName }}) - {{ selectedPpc.characteristic?.characteristicName }}
                </p>
              </div>
            </div>
            <button
              @click="showSegmentModal = false"
              type="button"
              class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-all"
            >
              <X class="w-5 h-5" />
            </button>
          </div>

          <!-- Body -->
          <div class="flex-1 overflow-y-auto md:overflow-hidden flex flex-col md:flex-row">
            
            <!-- Left Side: Trial Calculate -->
            <div class="w-full md:w-1/2 p-6 md:border-r border-slate-200 dark:border-slate-800 md:overflow-y-auto space-y-6 shrink-0 md:shrink">
              <div>
                <h4 class="text-sm font-bold text-slate-800 dark:text-slate-200 border-b border-slate-100 dark:border-slate-800 pb-2 flex items-center gap-2">
                  <Activity class="w-4 h-4 text-amber-500" /> 管制界線試算工具
                </h4>
                <p class="text-xs text-slate-500 dark:text-slate-400 mt-1.5">
                  選擇特定日期範圍內的歷史量測數據來試算統計學上的管制上限(UCL)、中心線(CL)及管制下限(LCL)。
                </p>
              </div>

              <!-- Trial Form -->
              <div class="p-4 bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-700/80 rounded-2xl space-y-4">
                <div class="grid grid-cols-2 gap-4">
                  <div class="space-y-1">
                    <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">試算起日</label>
                    <input
                      v-model="trialForm.startDate"
                      type="date"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white"
                    />
                  </div>
                  <div class="space-y-1">
                    <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">試算迄日</label>
                    <input
                      v-model="trialForm.endDate"
                      type="date"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white"
                    />
                  </div>
                </div>

                <button
                  type="button"
                  @click="runTrialCalculate"
                  :disabled="trialLoading"
                  class="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl bg-gradient-to-r from-amber-600 to-orange-600 text-white text-xs font-bold shadow-md shadow-amber-500/25 transition-all"
                >
                  <RefreshCw :class="['w-4 h-4', trialLoading ? 'animate-spin' : '']" />
                  {{ trialLoading ? '試算中...' : '開始歷史數據試算' }}
                </button>
              </div>

              <!-- Trial Result Display -->
              <div v-if="trialErr" class="p-4 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900/60 rounded-2xl text-xs text-red-600 dark:text-red-400">
                試算錯誤：{{ trialErr }}
              </div>

              <div v-if="trialResult" class="p-5 bg-white dark:bg-slate-800/80 border border-slate-200 dark:border-slate-700/80 rounded-2xl space-y-4 shadow-sm">
                <div class="flex items-center justify-between">
                  <span class="text-xs font-bold text-slate-700 dark:text-slate-300">試算統計結果</span>
                  <span class="text-[10px] bg-slate-100 dark:bg-slate-900 px-2 py-0.5 rounded text-slate-500 font-bold">量測點數: {{ trialResult.sampleCount }} 點</span>
                </div>

                <div class="grid grid-cols-3 gap-3 text-center">
                  <div class="p-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl">
                    <div class="text-[10px] text-slate-400">UCL</div>
                    <div class="text-sm font-mono font-bold text-slate-800 dark:text-slate-200 mt-0.5">{{ formatNumber(trialResult.ucl, 4) }}</div>
                  </div>
                  <div class="p-2 bg-amber-50/50 dark:bg-amber-950/20 border border-amber-200/50 dark:border-amber-900/50 rounded-xl">
                    <div class="text-[10px] text-amber-600 dark:text-amber-500">CL (中心線)</div>
                    <div class="text-sm font-mono font-bold text-amber-800 dark:text-amber-300 mt-0.5">{{ formatNumber(trialResult.cl, 4) }}</div>
                  </div>
                  <div class="p-2 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-xl">
                    <div class="text-[10px] text-slate-400">LCL</div>
                    <div class="text-sm font-mono font-bold text-slate-800 dark:text-slate-200 mt-0.5">{{ formatNumber(trialResult.lcl, 4) }}</div>
                  </div>
                </div>

                <p class="text-[11px] text-slate-400 leading-relaxed italic">
                  {{ trialResult.note }}
                </p>

                <div class="flex gap-2">
                  <button
                    type="button"
                    @click="applyTrialResultToSegment"
                    class="flex-1 px-3 py-2 bg-purple-50 dark:bg-purple-950/40 text-purple-600 dark:text-purple-400 border border-purple-200 dark:border-purple-800 rounded-xl text-xs font-bold transition-all hover:bg-purple-100"
                  >
                    帶入右側分段表單
                  </button>
                </div>
              </div>
            </div>

            <!-- Right Side: Segment CRUD & List -->
            <div class="w-full md:w-1/2 p-6 md:overflow-y-auto space-y-6 shrink-0 md:shrink">
              <div>
                <h4 class="text-sm font-bold text-slate-800 dark:text-slate-200 border-b border-slate-100 dark:border-slate-800 pb-2 flex items-center gap-2">
                  <Sliders class="w-4 h-4 text-purple-500" /> 分段管制界線設定
                </h4>
                <p class="text-xs text-slate-500 dark:text-slate-400 mt-1.5">
                  定義不同時間區間的管制界線，系統會根據量測時間自動套用對應區間的 UCL/CL/LCL 進行規則檢驗。
                </p>
              </div>

              <!-- Segment Add/Edit Form -->
              <form @submit.prevent="saveSegment" class="p-4 bg-purple-50/20 dark:bg-purple-950/10 border border-purple-100 dark:border-purple-900/50 rounded-2xl space-y-4">
                <div v-if="segmentFormErr" class="p-3 bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900/60 rounded-xl text-xs text-red-600 dark:text-red-400">
                  儲存失敗：{{ segmentFormErr }}
                </div>

                <!-- Date range -->
                <div class="grid grid-cols-2 gap-4">
                  <div class="space-y-1">
                    <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">生效起日 <span class="text-red-500">*</span></label>
                    <input
                      v-model="segmentForm.startDate"
                      type="date"
                      required
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white"
                    />
                  </div>
                  <div class="space-y-1">
                    <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">失效止日 (選填，留空表無限期)</label>
                    <input
                      v-model="segmentForm.endDate"
                      type="date"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white"
                    />
                  </div>
                </div>

                <!-- Limits values -->
                <div class="grid grid-cols-3 gap-3">
                  <div class="space-y-1">
                    <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">UCL <span class="text-red-500">*</span></label>
                    <input v-model="segmentForm.ucl" type="number" step="any" required class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-mono text-slate-700 dark:text-white" />
                  </div>
                  <div class="space-y-1">
                    <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">CL <span class="text-red-500">*</span></label>
                    <input v-model="segmentForm.cl" type="number" step="any" required class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-mono text-slate-700 dark:text-white" />
                  </div>
                  <div class="space-y-1">
                    <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">LCL <span class="text-red-500">*</span></label>
                    <input v-model="segmentForm.lcl" type="number" step="any" required class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-mono text-slate-700 dark:text-white" />
                  </div>
                </div>

                <!-- Note -->
                <div class="space-y-1">
                  <label class="text-[11px] font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">說明備註</label>
                  <input
                    v-model="segmentForm.note"
                    type="text"
                    placeholder="例如：例行調整、換液、設備大修等..."
                    class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white"
                  />
                </div>

                <div class="flex gap-2 justify-end">
                  <button
                    v-if="isEditingSegment"
                    type="button"
                    @click="resetSegmentForm"
                    class="px-4 py-2 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 text-slate-600 dark:text-slate-300 text-xs font-bold"
                  >
                    取消編輯
                  </button>
                  <button
                    type="submit"
                    class="px-5 py-2 rounded-xl bg-purple-600 hover:bg-purple-500 text-white text-xs font-bold"
                  >
                    {{ isEditingSegment ? '儲存更新' : '新增分段' }}
                  </button>
                </div>
              </form>

              <!-- Segments List -->
              <div class="space-y-3">
                <h5 class="text-xs font-bold text-slate-500 uppercase tracking-wider">已設定的分段管制界線</h5>

                <div v-if="loadingSegments" class="text-center text-xs text-slate-400 py-6">載入分段設定中...</div>
                <div v-else-if="segments.length === 0" class="text-center text-xs text-slate-400 py-6 italic bg-slate-50 dark:bg-slate-800/20 border border-slate-100 dark:border-slate-800 rounded-2xl">
                  目前無設定任何分段管制界線，系統將採用全局預設管制界線。
                </div>

                <div v-else class="border border-slate-200 dark:border-slate-800 rounded-2xl overflow-hidden shadow-sm bg-white dark:bg-slate-900">
                  <table class="w-full text-left border-collapse text-xs">
                    <thead>
                      <tr class="bg-slate-50 dark:bg-slate-800 text-slate-500 font-bold border-b border-slate-200 dark:border-slate-700">
                        <th class="py-2.5 px-4">生效日期區間</th>
                        <th class="py-2.5 px-4">UCL/CL/LCL</th>
                        <th class="py-2.5 px-4">備註說明</th>
                        <th class="py-2.5 px-4 text-right">操作</th>
                      </tr>
                    </thead>
                    <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-700 dark:text-slate-300 font-mono">
                      <tr v-for="seg in segments" :key="seg.id" class="hover:bg-purple-50/20 dark:hover:bg-slate-800/40">
                        <td class="py-3 px-4 font-sans font-semibold">
                          <div>{{ seg.startDate?.split('T')[0] }}</div>
                          <div class="text-[10px] text-slate-400 mt-0.5">至 {{ seg.endDate ? seg.endDate.split('T')[0] : '永久' }}</div>
                        </td>
                        <td class="py-3 px-4">
                          <div>U: {{ formatNumber(seg.ucl, 3) }}</div>
                          <div class="text-amber-600 dark:text-amber-500 font-bold">C: {{ formatNumber(seg.cl, 3) }}</div>
                          <div>L: {{ formatNumber(seg.lcl, 3) }}</div>
                        </td>
                        <td class="py-3 px-4 font-sans text-slate-500 text-[11px] leading-relaxed max-w-[120px] truncate" :title="seg.note">
                          {{ seg.note || '-' }}
                        </td>
                        <td class="py-3 px-4 text-right space-x-1.5 font-sans">
                          <button
                            type="button"
                            @click="editSegment(seg)"
                            class="text-blue-500 hover:text-blue-400 font-bold"
                          >
                            編輯
                          </button>
                          <button
                            type="button"
                            @click="deleteSegment(seg)"
                            class="text-red-500 hover:text-red-400 font-bold"
                          >
                            刪除
                          </button>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>

          </div>

          <!-- Footer -->
          <div class="shrink-0 p-6 bg-slate-50 dark:bg-slate-800/80 border-t border-slate-200 dark:border-slate-800 flex items-center justify-end">
            <button
              @click="showSegmentModal = false"
              type="button"
              class="px-5 py-2.5 rounded-xl bg-slate-200 dark:bg-slate-700 hover:bg-slate-300 dark:hover:bg-slate-600 text-slate-700 dark:text-slate-200 text-sm font-bold transition-all"
            >
              關閉視窗
            </button>
          </div>

        </div>
      </div>
    </transition>
  </section>
</template>

<style scoped>
.ppc-table-scroll {
  scrollbar-gutter: stable;
}

.ppc-table-scroll::-webkit-scrollbar {
  height: 14px;
}

.ppc-table-scroll::-webkit-scrollbar-track {
  background: rgb(241 245 249);
  border-top: 1px solid rgb(226 232 240);
}

.ppc-table-scroll::-webkit-scrollbar-thumb {
  background: rgb(245 158 11);
  border: 3px solid rgb(241 245 249);
  border-radius: 999px;
}

.ppc-table-scroll::-webkit-scrollbar-thumb:hover {
  background: rgb(217 119 6);
}

:global(.dark) .ppc-table-scroll::-webkit-scrollbar-track {
  background: rgb(15 23 42);
  border-top-color: rgb(51 65 85);
}

:global(.dark) .ppc-table-scroll::-webkit-scrollbar-thumb {
  background: rgb(245 158 11);
  border-color: rgb(15 23 42);
}

.ppc-scroll-range {
  height: 18px;
  cursor: grab;
  accent-color: rgb(245 158 11);
}

.ppc-scroll-range:active {
  cursor: grabbing;
}

.ppc-scroll-range:disabled {
  cursor: default;
  opacity: 0.45;
}

.ppc-scroll-range::-webkit-slider-runnable-track {
  height: 12px;
  border-radius: 999px;
  background: rgb(226 232 240);
  border: 1px solid rgb(203 213 225);
}

.ppc-scroll-range::-webkit-slider-thumb {
  appearance: none;
  width: 88px;
  height: 18px;
  margin-top: -4px;
  border-radius: 999px;
  background: rgb(245 158 11);
  border: 3px solid rgb(255 251 235);
  box-shadow: 0 6px 14px rgb(245 158 11 / 0.28);
}

.ppc-scroll-range::-moz-range-track {
  height: 12px;
  border-radius: 999px;
  background: rgb(226 232 240);
  border: 1px solid rgb(203 213 225);
}

.ppc-scroll-range::-moz-range-thumb {
  width: 88px;
  height: 18px;
  border-radius: 999px;
  background: rgb(245 158 11);
  border: 3px solid rgb(255 251 235);
  box-shadow: 0 6px 14px rgb(245 158 11 / 0.28);
}

:global(.dark) .ppc-scroll-range::-webkit-slider-runnable-track {
  background: rgb(30 41 59);
  border-color: rgb(71 85 105);
}

:global(.dark) .ppc-scroll-range::-webkit-slider-thumb {
  border-color: rgb(15 23 42);
}

:global(.dark) .ppc-scroll-range::-moz-range-track {
  background: rgb(30 41 59);
  border-color: rgb(71 85 105);
}

:global(.dark) .ppc-scroll-range::-moz-range-thumb {
  border-color: rgb(15 23 42);
}
</style>
