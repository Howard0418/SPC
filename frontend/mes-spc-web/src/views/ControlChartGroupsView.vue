<script setup>
import { onMounted, ref, computed, watch } from "vue";
import { useRoute } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import {
  Layers,
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
  FolderTree,
  Code2,
  Info,
  Activity,
  Sliders
} from "lucide-vue-next";

const { embedded } = defineProps({
  embedded: {
    type: Boolean,
    default: false
  }
});

const route = useRoute();

// --- Tab Navigation ---
const activeTab = ref("groups"); // 'groups', 'types'

// --- Global State ---
const loading = ref(false);
const err = ref("");
const successMsg = ref("");

function successAlert(msg) {
  successMsg.value = msg;
  setTimeout(() => { successMsg.value = ""; }, 3000);
}

// ==========================================
// 1. GROUPS STATE & METHODS
// ==========================================
const groupsRows = ref([]);
const groupsSearchQuery = ref("");
const groupsStatusFilter = ref("all");
const groupsShowModal = ref(false);
const groupsModalMode = ref("create");
const groupsCurrentId = ref(null);
const groupsForm = ref({
  groupCode: "",
  groupName: "",
  description: "",
  groupType: "CONTROL_CHART",
  isEnabled: true
});
const groupsFormErr = ref("");

async function loadGroups() {
  const { data } = await api.get("/control-chart-groups");
  groupsRows.value = data || [];
}

const filteredGroups = computed(() => {
  return groupsRows.value.filter(row => {
    const q = groupsSearchQuery.value.toLowerCase();
    const matchQuery = !q || 
      (row.groupCode && row.groupCode.toLowerCase().includes(q)) ||
      (row.groupName && row.groupName.toLowerCase().includes(q)) ||
      (row.description && row.description.toLowerCase().includes(q));
      
    if (groupsStatusFilter.value === "active") return matchQuery && row.isEnabled;
    if (groupsStatusFilter.value === "inactive") return matchQuery && !row.isEnabled;
    return matchQuery;
  });
});

function openGroupsCreate() {
  groupsModalMode.value = "create";
  groupsCurrentId.value = null;
  groupsForm.value = { groupCode: "", groupName: "", description: "", groupType: "CONTROL_CHART", isEnabled: true };
  groupsFormErr.value = "";
  groupsShowModal.value = true;
}

function openGroupsEdit(item) {
  groupsModalMode.value = "edit";
  groupsCurrentId.value = item.id;
  groupsForm.value = {
    groupCode: item.groupCode || "",
    groupName: item.groupName || "",
    description: item.description || "",
    groupType: item.groupType || "CONTROL_CHART",
    isEnabled: item.isEnabled ?? true
  };
  groupsFormErr.value = "";
  groupsShowModal.value = true;
}

async function saveGroup() {
  if (!groupsForm.value.groupCode?.trim() || !groupsForm.value.groupName?.trim()) {
    groupsFormErr.value = "群組代號與名稱皆為必填欄位。";
    return;
  }
  groupsFormErr.value = "";
  loading.value = true;
  try {
    if (groupsModalMode.value === "create") {
      const { data } = await api.post("/control-chart-groups", groupsForm.value);
      groupsRows.value.push(data);
      successAlert("成功建立大群組：" + data.groupName);
    } else {
      const { data } = await api.put(`/control-chart-groups/${groupsCurrentId.value}`, groupsForm.value);
      const idx = groupsRows.value.findIndex(x => x.id === groupsCurrentId.value);
      if (idx !== -1) groupsRows.value[idx] = data;
      successAlert("成功更新大群組：" + data.groupName);
    }
    groupsShowModal.value = false;
  } catch (e) {
    groupsFormErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function deleteGroup(item) {
  if (!confirm(`確定要刪除管制圖群組「${item.groupCode} (${item.groupName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/control-chart-groups/${item.id}`);
    groupsRows.value = groupsRows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除群組：" + item.groupName);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}


// ==========================================
// 2. CATEGORIES STATE & METHODS
// ==========================================
const categoriesRows = ref([]);
const categoriesSearchQuery = ref("");
const categoriesStatusFilter = ref("all");
const categoriesGroupFilter = ref("all");
const categoriesShowModal = ref(false);
const categoriesModalMode = ref("create");
const categoriesCurrentId = ref(null);
const categoriesForm = ref({
  chartGroupId: null,
  categoryCode: "",
  categoryName: "",
  description: "",
  isEnabled: true
});
const categoriesFormErr = ref("");

async function loadCategories() {
  const { data } = await api.get("/control-chart-categories");
  categoriesRows.value = data || [];
}

const groupMap = computed(() => {
  const map = {};
  groupsRows.value.forEach(g => {
    map[g.id] = `${g.groupCode} (${g.groupName})`;
  });
  return map;
});

const procGroupId = computed(() => groupsRows.value.find(g => g.groupCode === "PROC")?.id || null);
const chemGroupId = computed(() => groupsRows.value.find(g => g.groupCode === "CHEM")?.id || null);
const prodGroupId = computed(() => groupsRows.value.find(g => g.groupCode === "PROD")?.id || null);

const filteredCategories = computed(() => {
  return categoriesRows.value.filter(row => {
    const q = categoriesSearchQuery.value.toLowerCase();
    const gName = row.chartGroupId ? (groupMap.value[row.chartGroupId] || "") : "";
    const matchQuery = !q || 
      (row.categoryCode && row.categoryCode.toLowerCase().includes(q)) ||
      (row.categoryName && row.categoryName.toLowerCase().includes(q)) ||
      (row.description && row.description.toLowerCase().includes(q)) ||
      gName.toLowerCase().includes(q);
      
    const matchStatus = categoriesStatusFilter.value === "all" ||
      (categoriesStatusFilter.value === "active" && row.isEnabled) ||
      (categoriesStatusFilter.value === "inactive" && !row.isEnabled);

    const matchGroup = categoriesGroupFilter.value === "all" || row.chartGroupId === parseInt(categoriesGroupFilter.value);

    return matchQuery && matchStatus && matchGroup;
  });
});

function openCategoriesCreate() {
  categoriesModalMode.value = "create";
  categoriesCurrentId.value = null;
  categoriesForm.value = {
    chartGroupId: groupsRows.value.length > 0 ? groupsRows.value[0].id : null,
    categoryCode: "",
    categoryName: "",
    description: "",
    isEnabled: true
  };
  categoriesFormErr.value = "";
  categoriesShowModal.value = true;
}

function openCategoriesEdit(item) {
  categoriesModalMode.value = "edit";
  categoriesCurrentId.value = item.id;
  categoriesForm.value = {
    chartGroupId: item.chartGroupId || (groupsRows.value.length > 0 ? groupsRows.value[0].id : null),
    categoryCode: item.categoryCode || "",
    categoryName: item.categoryName || "",
    description: item.description || "",
    isEnabled: item.isEnabled ?? true
  };
  categoriesFormErr.value = "";
  categoriesShowModal.value = true;
}

async function saveCategory() {
  if (!categoriesForm.value.categoryCode?.trim() || !categoriesForm.value.categoryName?.trim() || !categoriesForm.value.chartGroupId) {
    categoriesFormErr.value = "所屬群組、類別代號與名稱皆為必填欄位。";
    return;
  }
  categoriesFormErr.value = "";
  loading.value = true;
  try {
    const payload = {
      ...categoriesForm.value,
      chartGroupId: parseInt(categoriesForm.value.chartGroupId)
    };
    if (categoriesModalMode.value === "create") {
      const { data } = await api.post("/control-chart-categories", payload);
      categoriesRows.value.push(data);
      successAlert("成功建立中分類：" + data.categoryName);
    } else {
      const { data } = await api.put(`/control-chart-categories/${categoriesCurrentId.value}`, payload);
      const idx = categoriesRows.value.findIndex(x => x.id === categoriesCurrentId.value);
      if (idx !== -1) categoriesRows.value[idx] = data;
      successAlert("成功更新中分類：" + data.categoryName);
    }
    categoriesShowModal.value = false;
  } catch (e) {
    categoriesFormErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function deleteCategory(item) {
  if (!confirm(`確定要刪除管制圖類別「${item.categoryCode} (${item.categoryName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/control-chart-categories/${item.id}`);
    categoriesRows.value = categoriesRows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除類別：" + item.categoryName);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}


// ==========================================
// 3. TYPES STATE & METHODS
// ==========================================
const typesRows = ref([]);
const ruleGroupsRows = ref([]);
const typesSearchQuery = ref("");
const typesStatusFilter = ref("all");
const typesGroupFilter = ref("all");
const typesShowModal = ref(false);
const typesModalMode = ref("create");
const typesCurrentId = ref(null);
const ruleLibraryRows = ref([]);
const typeRuleOptions = ref([]);
const selectedTypeRuleCodes = ref([]);
const typeRuleSummaryMap = ref({});
const typesForm = ref({
  chartGroupId: null,
  chartCategoryId: null,
  chartTypeCode: "",
  chartTypeName: "",
  dataCategory: "Variable",
  requiredSampleSize: 5,
  ruleGroupId: null,
  description: "",
  formulaConfigJson: "{}",
  isEnabled: true
});
const typesFormErr = ref("");

async function loadTypes() {
  const { data } = await api.get("/control-chart-types");
  typesRows.value = data || [];
  await loadTypeRuleSummaries();
}

async function loadRuleGroups() {
  const { data } = await api.get("/spc-rule-groups");
  ruleGroupsRows.value = data || [];
}

async function loadRuleLibrary() {
  const { data } = await api.get("/spc-rules?libraryOnly=true");
  ruleLibraryRows.value = data || [];
}

const ruleGroupMap = computed(() => {
  const map = {};
  ruleGroupsRows.value.forEach(rg => {
    map[rg.id] = `${rg.ruleGroupCode} (${rg.ruleGroupName})`;
  });
  return map;
});

const categoryMap = computed(() => {
  const map = {};
  categoriesRows.value.forEach(c => {
    map[c.id] = `${c.categoryCode} (${c.categoryName})`;
  });
  return map;
});

const categoryGroupIdMap = computed(() => {
  const map = {};
  categoriesRows.value.forEach(c => {
    map[c.id] = c.chartGroupId;
  });
  return map;
});

const groupLabelMap = computed(() => {
  const map = {};
  groupsRows.value.forEach(g => {
    map[g.id] = `${g.groupCode} (${g.groupName})`;
  });
  return map;
});

const defaultGroupId = computed(() => groupsRows.value[0]?.id || null);

function getTypeGroupId(type) {
  return categoryGroupIdMap.value[type.chartCategoryId] || null;
}

function getTypeGroupLabel(type) {
  const groupId = getTypeGroupId(type);
  return groupId ? (groupLabelMap.value[groupId] || `大類別 #${groupId}`) : "未指定大類別";
}

function resetTypeRuleOptions(options = null) {
  const source = options || ruleLibraryRows.value.map(x => ({ ...x, isSelected: false }));
  typeRuleOptions.value = source.map(x => ({ ...x }));
  selectedTypeRuleCodes.value = typeRuleOptions.value.filter(x => x.isSelected).map(x => x.ruleCode);
}

function syncTabFromRoute() {
  if (route.path.includes("types")) {
    activeTab.value = "types";
  }

  if (route.query.tab === "groups" || route.query.tab === "categories" || route.query.tab === "types") {
    activeTab.value = route.query.tab;
  }
}

function defaultTypeRuleCodes() {
  const firstRule = [...ruleLibraryRows.value]
    .sort((a, b) => (Number(a.priority) || 0) - (Number(b.priority) || 0) || (Number(a.id) || 0) - (Number(b.id) || 0))[0];
  return firstRule ? [firstRule.ruleCode] : [];
}

async function loadTypeRules(chartTypeId) {
  const { data } = await api.get(`/control-chart-types/${chartTypeId}/rules`);
  resetTypeRuleOptions(data?.rules || null);
  typeRuleSummaryMap.value = {
    ...typeRuleSummaryMap.value,
    [chartTypeId]: buildTypeRuleSummary(data?.rules || [])
  };
}

async function saveTypeRules(chartTypeId) {
  const { data } = await api.put(`/control-chart-types/${chartTypeId}/rules`, {
    selectedRuleCodes: selectedTypeRuleCodes.value
  });
  typeRuleSummaryMap.value = {
    ...typeRuleSummaryMap.value,
    [chartTypeId]: buildTypeRuleSummary(data?.rules || [])
  };
  return data;
}

async function loadTypeRuleSummaries() {
  if (!typesRows.value.length) {
    typeRuleSummaryMap.value = {};
    return;
  }

  const entries = await Promise.all(typesRows.value.map(async item => {
    if (!item.ruleGroupId) return [item.id, buildTypeRuleSummary([])];
    try {
      const { data } = await api.get(`/control-chart-types/${item.id}/rules`);
      return [item.id, buildTypeRuleSummary(data?.rules || [])];
    } catch {
      return [item.id, null];
    }
  }));

  typeRuleSummaryMap.value = Object.fromEntries(entries);
}

function buildTypeRuleSummary(rules) {
  const selectedRules = (rules || []).filter(x => x.isSelected);
  return {
    count: selectedRules.length,
    label: selectedRules.length ? selectedRules.map(x => x.ruleName).join("、") : "不套用規則"
  };
}

function getTypeRuleSummary(item) {
  const summary = typeRuleSummaryMap.value[item.id];
  if (summary) return summary;
  if (item.ruleGroupId) return { count: null, label: ruleGroupMap.value[item.ruleGroupId] || `Rule #${item.ruleGroupId}` };
  return { count: 0, label: "不套用規則" };
}

function selectAllTypeRules() {
  selectedTypeRuleCodes.value = typeRuleOptions.value.map(x => x.ruleCode);
}

function clearTypeRules() {
  selectedTypeRuleCodes.value = [];
}

async function ensureCategoryForGroup(groupId) {
  const numericGroupId = parseInt(groupId);
  if (!numericGroupId) return null;

  const existing = categoriesRows.value.find(c => c.chartGroupId === numericGroupId);
  if (existing) return existing;

  const group = groupsRows.value.find(g => g.id === numericGroupId);
  const code = `AUTO_${group?.groupCode || numericGroupId}`;
  const payload = {
    chartGroupId: numericGroupId,
    categoryCode: code,
    categoryName: `${group?.groupName || "管制圖"}預設分類`,
    description: "系統自動建立的隱藏分類，用於讓小分類直接掛在大類別底下。",
    isEnabled: true
  };
  const { data } = await api.post("/control-chart-categories", payload);
  categoriesRows.value.push(data);
  return data;
}

async function ensureDefaultCategories() {
  for (const group of groupsRows.value) {
    await ensureCategoryForGroup(group.id);
  }
}

const filteredTypes = computed(() => {
  return typesRows.value.filter(row => {
    const q = typesSearchQuery.value.toLowerCase();
    const groupName = getTypeGroupLabel(row);
    const matchQuery = !q || 
      (row.chartTypeCode && row.chartTypeCode.toLowerCase().includes(q)) ||
      (row.chartTypeName && row.chartTypeName.toLowerCase().includes(q)) ||
      (row.description && row.description.toLowerCase().includes(q)) ||
      groupName.toLowerCase().includes(q);
      
    const matchStatus = typesStatusFilter.value === "all" ||
      (typesStatusFilter.value === "active" && row.isEnabled) ||
      (typesStatusFilter.value === "inactive" && !row.isEnabled);

    const matchGroup = typesGroupFilter.value === "all" || getTypeGroupId(row) === parseInt(typesGroupFilter.value);

    return matchQuery && matchStatus && matchGroup;
  });
});

function openTypesCreate() {
  typesModalMode.value = "create";
  typesCurrentId.value = null;
  resetTypeRuleOptions();
  selectedTypeRuleCodes.value = defaultTypeRuleCodes();
  typesForm.value = {
    chartGroupId: defaultGroupId.value,
    chartCategoryId: null,
    chartTypeCode: "",
    chartTypeName: "",
    dataCategory: "Variable",
    requiredSampleSize: 5,
    ruleGroupId: null,
    description: "",
    formulaConfigJson: "{\n  \"UclFormula\": \"Xbar + A2 * Rbar\",\n  \"LclFormula\": \"Xbar - A2 * Rbar\"\n}",
    isEnabled: true
  };
  typesFormErr.value = "";
  typesShowModal.value = true;
}

async function openTypesEdit(item) {
  typesModalMode.value = "edit";
  typesCurrentId.value = item.id;
  resetTypeRuleOptions();
  typesForm.value = {
    chartGroupId: getTypeGroupId(item) || defaultGroupId.value,
    chartCategoryId: item.chartCategoryId || null,
    chartTypeCode: item.chartTypeCode || "",
    chartTypeName: item.chartTypeName || "",
    dataCategory: item.dataCategory || "Variable",
    requiredSampleSize: item.requiredSampleSize || 5,
    ruleGroupId: item.ruleGroupId || null,
    description: item.description || "",
    formulaConfigJson: item.formulaConfigJson || "{}",
    isEnabled: item.isEnabled ?? true
  };
  typesFormErr.value = "";
  typesShowModal.value = true;
  try {
    await loadTypeRules(item.id);
  } catch (e) {
    typesFormErr.value = "載入小分類管制規則失敗：" + getApiErrorMessage(e);
  }
}

function setStandardFormula() {
  typesForm.value.formulaConfigJson = JSON.stringify({
    XbarCalculationMethod: "STANDARD_RBAR",
    UclFormula: "XDoubleBar + (A2 * Rbar)",
    LclFormula: "XDoubleBar - (A2 * Rbar)"
  }, null, 2);
}

function setMrMethodFormula() {
  typesForm.value.formulaConfigJson = JSON.stringify({
    XbarCalculationMethod: "MOVING_RANGE_OF_XBAR",
    MrMultiplier: 2.66,
    UclFormula: "XDoubleBar + (2.66 * MRbar_Xbar)",
    LclFormula: "XDoubleBar - (2.66 * MRbar_Xbar)"
  }, null, 2);
}

function setSigmaMethodFormula() {
  typesForm.value.formulaConfigJson = JSON.stringify({
    XbarCalculationMethod: "SIGMA_METHOD",
    Multiplier: 3.0,
    UclFormula: "XDoubleBar + (3.0 * S_Xbar)",
    LclFormula: "XDoubleBar - (3.0 * S_Xbar)"
  }, null, 2);
}

async function saveType() {
  if (!typesForm.value.chartTypeCode?.trim() || !typesForm.value.chartTypeName?.trim() || !typesForm.value.chartGroupId) {
    typesFormErr.value = "所屬大類別、管制圖代號與名稱皆為必填欄位。";
    return;
  }
  try {
    JSON.parse(typesForm.value.formulaConfigJson || "{}");
  } catch(e) {
    typesFormErr.value = "公式設定 JSON 格式無效，請檢查語法。";
    return;
  }
  typesFormErr.value = "";
  loading.value = true;
  try {
    const category = await ensureCategoryForGroup(typesForm.value.chartGroupId);
    if (!category) {
      typesFormErr.value = "找不到可用的大類別，請先建立管制圖大類別。";
      return;
    }
    const payload = {
      ...typesForm.value,
      chartCategoryId: category.id,
      ruleGroupId: typesForm.value.ruleGroupId ? parseInt(typesForm.value.ruleGroupId) : null,
      requiredSampleSize: parseInt(typesForm.value.requiredSampleSize) || 1
    };
    delete payload.chartGroupId;
    let savedType = null;
    if (typesModalMode.value === "create") {
      const { data } = await api.post("/control-chart-types", payload);
      savedType = data;
      successAlert("成功建立管制圖種類：" + data.chartTypeName);
    } else {
      const { data } = await api.put(`/control-chart-types/${typesCurrentId.value}`, payload);
      savedType = data;
      successAlert("成功更新管制圖種類：" + data.chartTypeName);
    }
    const rulesResult = await saveTypeRules(savedType.id);
    savedType.ruleGroupId = rulesResult?.ruleGroupId || null;
    const idx = typesRows.value.findIndex(x => x.id === savedType.id);
    if (idx !== -1) typesRows.value[idx] = savedType;
    else typesRows.value.push(savedType);
    await loadRuleGroups();
    typesShowModal.value = false;
  } catch (e) {
    typesFormErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function deleteType(item) {
  if (!confirm(`確定要刪除管制圖種類「${item.chartTypeCode} (${item.chartTypeName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/control-chart-types/${item.id}`);
    typesRows.value = typesRows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除種類：" + item.chartTypeName);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}


// ==========================================
// 4. UNIFIED LOAD
// ==========================================
async function loadAll() {
  err.value = "";
  loading.value = true;
  try {
    await loadGroups();
    await loadCategories();
    await ensureDefaultCategories();
    await loadRuleGroups();
    await loadRuleLibrary();
    await loadTypes();
  } catch (e) {
    err.value = "載入設定失敗：" + getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

onMounted(async () => {
  syncTabFromRoute();

  await loadAll();
  
  if (route.query.id) {
    const editId = Number(route.query.id);
    if (activeTab.value === "types") {
      const targetType = typesRows.value.find(t => t.id === editId);
      if (targetType) {
        openTypesEdit(targetType);
      }
    } else if (activeTab.value === "groups") {
      const targetGroup = groupsRows.value.find(g => g.id === editId);
      if (targetGroup) {
        openGroupsEdit(targetGroup);
      }
    }
  }
});

watch(() => route.query.tab, syncTabFromRoute);
</script>

<template>
  <section class="space-y-6">
    <!-- Header Banner -->
    <div v-if="!embedded" class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-indigo-600 to-blue-500 rounded-xl shadow-lg shadow-indigo-500/30 text-white">
          <Activity class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">SPC 管制圖配置總管維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">統一管理管制圖大類別，以及具體的小分類與公式參數配置</p>
        </div>
      </div>
      
      <div class="flex items-center gap-3">
        <router-link
          to="/part-process-characteristics"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-amber-50 hover:bg-amber-100 dark:bg-slate-800 dark:hover:bg-slate-700 text-amber-600 dark:text-amber-400 text-sm font-semibold transition-all border border-amber-200 dark:border-slate-700"
        >
          <FolderTree class="w-4 h-4" /> 檢驗基準設定
        </router-link>
        <router-link
          to="/spc"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-50 hover:bg-blue-100 dark:bg-slate-800 dark:hover:bg-slate-700 text-blue-600 dark:text-blue-400 text-sm font-semibold transition-all border border-blue-200 dark:border-slate-700"
        >
          <Activity class="w-4 h-4" /> SPC 管制圖
        </router-link>
        <button
          @click="loadAll"
          type="button"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-sm font-semibold transition-all border border-slate-200 dark:border-slate-700"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> 重新整理
        </button>
      </div>
    </div>

    <!-- Alert Messages -->
    <div v-if="err" class="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500" />
      <div class="text-sm font-semibold">{{ err }}</div>
    </div>
    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ successMsg }}</div>
    </div>

    <!-- 🌟 Tab Header Navigation -->
    <div v-if="!embedded" class="flex border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 rounded-2xl p-1 shadow-sm border">
      <button
        @click="activeTab = 'groups'"
        :class="activeTab === 'groups' ? 'bg-indigo-50 dark:bg-slate-800 text-indigo-600 dark:text-indigo-400 border border-indigo-200 dark:border-slate-700 font-bold' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        class="flex-1 py-3 px-4 rounded-xl text-center text-sm transition-all whitespace-nowrap"
      >
        大類別總管 (Control Groups)
      </button>
      <button
        @click="activeTab = 'categories'"
        :class="activeTab === 'categories' ? 'bg-indigo-50 dark:bg-slate-800 text-indigo-600 dark:text-indigo-400 border border-indigo-200 dark:border-slate-700 font-bold' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        class="flex-1 py-3 px-4 rounded-xl text-center text-sm transition-all whitespace-nowrap"
      >
        中分類維護
      </button>
      <button
        @click="activeTab = 'types'"
        :class="activeTab === 'types' ? 'bg-indigo-50 dark:bg-slate-800 text-indigo-600 dark:text-indigo-400 border border-indigo-200 dark:border-slate-700 font-bold' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        class="flex-1 py-3 px-4 rounded-xl text-center text-sm transition-all whitespace-nowrap"
      >
        小分類與公式配置
      </button>
    </div>

    <!-- ========================================================================================= -->
    <!-- TAB 1: GROUPS VIEW -->
    <!-- ========================================================================================= -->
    <div v-if="activeTab === 'groups'" class="space-y-6 animate-fade-in">
      <div class="p-5 bg-gradient-to-r from-indigo-50 to-blue-50 dark:from-indigo-950/30 dark:to-blue-900/20 border border-indigo-100 dark:border-indigo-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
        <div class="p-2 bg-indigo-100 dark:bg-indigo-900/50 rounded-xl text-indigo-600 dark:text-indigo-400 mt-0.5"><Info class="w-5 h-5" /></div>
        <div>
          <h4 class="text-sm font-bold text-indigo-900 dark:text-indigo-300">模組指南：管制圖大群組主檔 (Control Chart Groups)</h4>
          <p class="text-xs text-indigo-700 dark:text-indigo-400/80 mt-1.5 leading-relaxed">
            此模組用於設定 SPC 系統中最頂層的分類結構（如：PROC 製程管制、CHEM 藥液管制、PROD 產品管制），幫助您有效管理全廠各類型的管制圖類別。
          </p>
        </div>
      </div>

      <div class="flex flex-col sm:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
        <div class="relative w-full sm:w-80">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400"><Search class="w-4 h-4" /></span>
          <input v-model="groupsSearchQuery" type="text" placeholder="搜尋群組代號、名稱..." class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500" />
        </div>
        <div class="flex items-center gap-3 w-full sm:w-auto">
          <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
            <button v-for="f in [{id:'all', label:'全部'}, {id:'active', label:'已啟用'}, {id:'inactive', label:'已停用'}]" :key="f.id" @click="groupsStatusFilter = f.id" type="button" :class="[ 'px-4 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap', groupsStatusFilter === f.id ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-600 dark:text-slate-400 hover:text-slate-850' ]">{{ f.label }}</button>
          </div>
          <button @click="openGroupsCreate" type="button" class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 transition-all whitespace-nowrap"><Plus class="w-4 h-4" /> 新增大群組</button>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
                <th class="py-4 px-6 w-16 text-center">ID</th>
                <th class="py-4 px-6">群組代號 / 名稱</th>
                <th class="py-4 px-6">群組類型</th>
                <th class="py-4 px-6">說明描述</th>
                <th class="py-4 px-6 text-center">狀態</th>
                <th class="py-4 px-6 text-right">操作</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
              <tr v-if="loading && groupsRows.length === 0"><td colspan="6" class="py-12 text-center text-slate-400">正在載入群組清單...</td></tr>
              <tr v-else-if="filteredGroups.length === 0"><td colspan="6" class="py-12 text-center text-slate-400">找不到相符的群組資料</td></tr>
              <tr v-else v-for="g in filteredGroups" :key="g.id" class="hover:bg-indigo-50/50 dark:hover:bg-slate-800/50 transition-colors group">
                <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ g.id }}</td>
                <td class="py-4 px-6">
                  <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2"><Layers class="w-4 h-4 text-indigo-500" /> {{ g.groupCode }}</div>
                  <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ g.groupName }}</div>
                </td>
                <td class="py-4 px-6">
                  <span :class="[ 'px-2.5 py-1 rounded-lg text-xs font-bold border tracking-wide', g.groupType === 'TREND_CHART' ? 'bg-amber-50 text-amber-600 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800' : 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-950/60 dark:text-blue-300 dark:border-blue-800' ]">
                    {{ g.groupType === 'TREND_CHART' ? '趨勢圖' : '管制圖' }}
                  </span>
                </td>
                <td class="py-4 px-6 text-slate-600 dark:text-slate-300">{{ g.description || '-' }}</td>
                <td class="py-4 px-6 text-center">
                  <span :class="[ 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide', g.isEnabled ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700' ]">
                    <span :class="['w-1.5 h-1.5 rounded-full', g.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                    {{ g.isEnabled ? '啟用中' : '已停用' }}
                  </span>
                </td>
                <td class="py-4 px-6 text-right space-x-2">
                  <button @click="openGroupsEdit(g)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-slate-700" title="編輯群組"><Edit class="w-4 h-4" /></button>
                  <button @click="deleteGroup(g)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-955 text-red-600 dark:text-red-400 border border-red-200 dark:border-slate-700" title="刪除群組"><Trash2 class="w-4 h-4" /></button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ========================================================================================= -->
    <!-- TAB 2: CATEGORIES VIEW -->
    <!-- ========================================================================================= -->
    <div v-if="activeTab === 'categories'" class="space-y-6 animate-fade-in">
      <div class="p-5 bg-gradient-to-r from-cyan-50 to-blue-50 dark:from-cyan-950/30 dark:to-blue-900/20 border border-cyan-100 dark:border-cyan-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
        <div class="p-2 bg-cyan-100 dark:bg-cyan-900/50 rounded-xl text-cyan-600 dark:text-cyan-400 mt-0.5"><Info class="w-5 h-5" /></div>
        <div>
          <h4 class="text-sm font-bold text-cyan-900 dark:text-cyan-300">模組指南：管制圖類別主檔 (Control Chart Categories)</h4>
          <p class="text-xs text-cyan-700 dark:text-cyan-400/80 mt-1.5 leading-relaxed">
            此模組用於設定 SPC 系統的「中分類」資料，歸屬於上面的「大群組」之下。
          </p>
        </div>
      </div>

      <!-- 🌟 Visual Quality Dimension Cards -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <!-- 1. 製程管制項目 PROC -->
        <div 
          v-if="procGroupId"
          @click="categoriesGroupFilter = categoriesGroupFilter === procGroupId ? 'all' : procGroupId"
          :class="[ 'relative p-6 rounded-3xl border cursor-pointer transition-all duration-300 transform hover:-translate-y-1 overflow-hidden group shadow-md', categoriesGroupFilter === procGroupId ? 'bg-gradient-to-br from-blue-900 via-indigo-900 to-slate-900 border-blue-500 text-white' : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-100 hover:border-blue-400' ]"
        >
          <div class="absolute -right-6 -bottom-6 w-32 h-32 bg-blue-500/10 rounded-full blur-2xl pointer-events-none"></div>
          <div class="flex items-start justify-between">
            <div class="p-3 rounded-2xl bg-blue-500/10 border border-blue-500/20 text-blue-500"><Layers class="w-6 h-6" /></div>
            <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-blue-500/15 text-blue-500 border border-blue-500/25">XBAR-S / XBAR-R</span>
          </div>
          <h3 class="text-lg font-black mt-4">製程管制項目 (PROC)</h3>
          <p class="text-xs text-slate-400 mt-1 leading-relaxed">針對產線即時製程參數（如咬蝕量、溫度、壓力）進行監控。</p>
          <div class="mt-6 flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-400">類別數</span>
            <span class="text-2xl font-black text-blue-500">{{ categoriesRows.filter(x => x.chartGroupId === procGroupId).length }} 筆</span>
          </div>
        </div>

        <!-- 2. 藥液管制項目 CHEM -->
        <div 
          v-if="chemGroupId"
          @click="categoriesGroupFilter = categoriesGroupFilter === chemGroupId ? 'all' : chemGroupId"
          :class="[ 'relative p-6 rounded-3xl border cursor-pointer transition-all duration-300 transform hover:-translate-y-1 overflow-hidden group shadow-md', categoriesGroupFilter === chemGroupId ? 'bg-gradient-to-br from-teal-900 via-emerald-900 to-slate-900 border-teal-500 text-white' : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-100 hover:border-teal-400' ]"
        >
          <div class="absolute -right-6 -bottom-6 w-32 h-32 bg-teal-500/10 rounded-full blur-2xl pointer-events-none"></div>
          <div class="flex items-start justify-between">
            <div class="p-3 rounded-2xl bg-teal-500/10 border border-teal-500/20 text-teal-500"><RefreshCw class="w-6 h-6" /></div>
            <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-teal-500/15 text-teal-500 border border-teal-500/25">I-MR</span>
          </div>
          <h3 class="text-lg font-black mt-4">藥液管制項目 (CHEM)</h3>
          <p class="text-xs text-slate-400 mt-1 leading-relaxed">監控化驗室槽液分析濃度趨勢（如酸鹼度、離子濃度）。</p>
          <div class="mt-6 flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-400">類別數</span>
            <span class="text-2xl font-black text-teal-500">{{ categoriesRows.filter(x => x.chartGroupId === chemGroupId).length }} 筆</span>
          </div>
        </div>

        <!-- 3. 產品管制項目 PROD -->
        <div 
          v-if="prodGroupId"
          @click="categoriesGroupFilter = categoriesGroupFilter === prodGroupId ? 'all' : prodGroupId"
          :class="[ 'relative p-6 rounded-3xl border cursor-pointer transition-all duration-300 transform hover:-translate-y-1 overflow-hidden group shadow-md', categoriesGroupFilter === prodGroupId ? 'bg-gradient-to-br from-purple-900 via-fuchsia-900 to-slate-900 border-purple-500 text-white' : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-100 hover:border-purple-400' ]"
        >
          <div class="absolute -right-6 -bottom-6 w-32 h-32 bg-purple-500/10 rounded-full blur-2xl pointer-events-none"></div>
          <div class="flex items-start justify-between">
            <div class="p-3 rounded-2xl bg-purple-500/10 border border-purple-500/20 text-purple-500"><FolderTree class="w-6 h-6" /></div>
            <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-purple-500/15 text-purple-500 border border-purple-500/25">規格綁定</span>
          </div>
          <h3 class="text-lg font-black mt-4">產品管制項目 (PROD)</h3>
          <p class="text-xs text-slate-400 mt-1 leading-relaxed">針對實體產品的特定出貨品質特性檢驗指標進行監控。</p>
          <div class="mt-6 flex items-center justify-between">
            <span class="text-xs font-semibold text-slate-400">類別數</span>
            <span class="text-2xl font-black text-purple-500">{{ categoriesRows.filter(x => x.chartGroupId === prodGroupId).length }} 筆</span>
          </div>
        </div>
      </div>

      <div class="flex flex-col lg:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
        <div class="relative w-full lg:w-80">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400"><Search class="w-4 h-4" /></span>
          <input v-model="categoriesSearchQuery" type="text" placeholder="搜尋類別代號、名稱..." class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-cyan-500" />
        </div>
        <div class="flex flex-wrap items-center gap-3 w-full lg:w-auto justify-end">
          <select v-model="categoriesGroupFilter" class="px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-cyan-500">
            <option value="all">所有大群組</option>
            <option v-for="g in groupsRows" :key="g.id" :value="g.id">{{ g.groupCode }} - {{ g.groupName }}</option>
          </select>
          <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
            <button v-for="f in [{id:'all', label:'全部'}, {id:'active', label:'已啟用'}, {id:'inactive', label:'已停用'}]" :key="f.id" @click="categoriesStatusFilter = f.id" type="button" :class="[ 'px-3 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap', categoriesStatusFilter === f.id ? 'bg-white dark:bg-slate-700 text-cyan-600 dark:text-cyan-400 shadow-sm' : 'text-slate-600 dark:text-slate-400' ]">{{ f.label }}</button>
          </div>
          <button @click="openCategoriesCreate" type="button" class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-cyan-600 to-blue-600 text-white text-sm font-bold shadow-lg shadow-cyan-500/25 transition-all"><Plus class="w-4 h-4" /> 新增中分類</button>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
                <th class="py-4 px-6 w-16 text-center">ID</th>
                <th class="py-4 px-6">所屬大群組</th>
                <th class="py-4 px-6">類別代號 / 名稱</th>
                <th class="py-4 px-6">說明描述</th>
                <th class="py-4 px-6 text-center">狀態</th>
                <th class="py-4 px-6 text-right">操作</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
              <tr v-if="loading && categoriesRows.length === 0"><td colspan="6" class="py-12 text-center text-slate-400">正在載入類別清單...</td></tr>
              <tr v-else-if="filteredCategories.length === 0"><td colspan="6" class="py-12 text-center text-slate-400">找不到相符的類別資料</td></tr>
              <tr v-else v-for="item in filteredCategories" :key="item.id" class="hover:bg-cyan-50/50 dark:hover:bg-slate-800/50 transition-colors group">
                <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ item.id }}</td>
                <td class="py-4 px-6">
                  <span class="text-xs font-bold text-indigo-700 dark:text-indigo-300 bg-indigo-50 dark:bg-indigo-950/60 border border-indigo-200 dark:border-indigo-800 px-2.5 py-1 rounded-lg">
                    {{ groupMap[item.chartGroupId] || `群組 #${item.chartGroupId}` }}
                  </span>
                </td>
                <td class="py-4 px-6 font-bold text-slate-900 dark:text-white">
                  <div class="flex items-center gap-2"><FolderTree class="w-4 h-4 text-cyan-500" /> {{ item.categoryCode }}</div>
                  <div class="text-xs text-slate-500 font-normal mt-0.5">{{ item.categoryName }}</div>
                </td>
                <td class="py-4 px-6 text-slate-600 dark:text-slate-300">{{ item.description || '-' }}</td>
                <td class="py-4 px-6 text-center">
                  <span :class="[ 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide', item.isEnabled ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700' ]">
                    <span :class="['w-1.5 h-1.5 rounded-full', item.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                    {{ item.isEnabled ? '啟用中' : '已停用' }}
                  </span>
                </td>
                <td class="py-4 px-6 text-right space-x-2">
                  <button @click="openCategoriesEdit(item)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-slate-700" title="編輯中分類"><Edit class="w-4 h-4" /></button>
                  <button @click="deleteCategory(item)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-955 text-red-600 dark:text-red-400 border border-red-200 dark:border-slate-700" title="刪除中分類"><Trash2 class="w-4 h-4" /></button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ========================================================================================= -->
    <!-- TAB 3: TYPES VIEW -->
    <!-- ========================================================================================= -->
    <div v-if="activeTab === 'types'" class="space-y-6 animate-fade-in">
      <div class="p-5 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-900/20 border border-blue-100 dark:border-indigo-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
        <div class="p-2 bg-blue-100 dark:bg-blue-900/50 rounded-xl text-blue-600 dark:text-blue-400 mt-0.5"><Info class="w-5 h-5" /></div>
        <div>
          <h4 class="text-sm font-bold text-blue-900 dark:text-blue-300">模組指南：管制圖種類與公式配置 (Control Chart Types)</h4>
          <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
            此模組用於設定具體的 SPC 管制圖小分類（如 Xbar-R、I-MR 等），直接歸屬於製程、藥液、產品大類別，並配置管制界限算法、抽樣組數與公式。
          </p>
        </div>
      </div>

      <div class="flex flex-col lg:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
        <div class="relative w-full lg:w-80">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400"><Search class="w-4 h-4" /></span>
          <input v-model="typesSearchQuery" type="text" placeholder="搜尋種類代號、名稱..." class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
        <div class="flex flex-wrap items-center gap-3 w-full lg:w-auto justify-end">
          <select v-model="typesGroupFilter" class="px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option value="all">所有大類別</option>
            <option v-for="g in groupsRows" :key="g.id" :value="g.id">{{ g.groupCode }} - {{ g.groupName }}</option>
          </select>
          <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
            <button v-for="f in [{id:'all', label:'全部'}, {id:'active', label:'已啟用'}, {id:'inactive', label:'已停用'}]" :key="f.id" @click="typesStatusFilter = f.id" type="button" :class="[ 'px-3 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap', typesStatusFilter === f.id ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm' : 'text-slate-600 dark:text-slate-400' ]">{{ f.label }}</button>
          </div>
          <button @click="openTypesCreate" type="button" class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white text-sm font-bold shadow-lg shadow-blue-500/25 transition-all"><Plus class="w-4 h-4" /> 新增小分類</button>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
                <th class="py-4 px-6 w-16 text-center">ID</th>
                <th class="py-4 px-6">所屬大類別</th>
                <th class="py-4 px-6">圖別代號 / 名稱</th>
                <th class="py-4 px-6">屬性分類</th>
                <th class="py-4 px-6 text-center">抽樣數</th>
                <th class="py-4 px-6">管制規則</th>
                <th class="py-4 px-6">公式設定概要</th>
                <th class="py-4 px-6 text-center">狀態</th>
                <th class="py-4 px-6 text-right">操作</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
              <tr v-if="loading && typesRows.length === 0"><td colspan="9" class="py-12 text-center text-slate-400">正在載入管制圖小分類清單...</td></tr>
              <tr v-else-if="filteredTypes.length === 0"><td colspan="9" class="py-12 text-center text-slate-400">找不到相符的小分類資料</td></tr>
              <tr v-else v-for="item in filteredTypes" :key="item.id" class="hover:bg-blue-50/50 dark:hover:bg-slate-800/50 transition-colors group">
                <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ item.id }}</td>
                <td class="py-4 px-6">
                  <span class="text-xs font-bold text-cyan-700 dark:text-cyan-300 bg-cyan-50 dark:bg-cyan-950/60 border border-cyan-200 dark:border-cyan-800 px-2.5 py-1 rounded-lg">
                    {{ getTypeGroupLabel(item) }}
                  </span>
                </td>
                <td class="py-4 px-6 font-bold text-slate-900 dark:text-white">
                  <div class="flex items-center gap-2 text-indigo-600 dark:text-indigo-400 font-mono"><Activity class="w-4 h-4 text-blue-500" /> {{ item.chartTypeCode }}</div>
                  <div class="text-xs text-slate-500 font-sans mt-0.5">{{ item.chartTypeName }}</div>
                </td>
                <td class="py-4 px-6">
                  <span :class="[ 'px-2.5 py-1 rounded-lg text-xs font-bold border tracking-wide', item.dataCategory === 'Variable' ? 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-950/60 dark:text-blue-300 dark:border-blue-800' : 'bg-amber-50 text-amber-600 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800' ]">
                    {{ item.dataCategory === 'Variable' ? '計量' : '計數' }}
                  </span>
                </td>
                <td class="py-4 px-6 text-center font-mono font-bold text-xs text-slate-600 dark:text-slate-300">{{ item.requiredSampleSize }}</td>
                <td class="py-4 px-6 text-xs text-slate-600 dark:text-slate-300">
                  <div v-if="getTypeRuleSummary(item).count" class="space-y-1">
                    <span class="inline-flex items-center px-2 py-0.5 rounded-full bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800 font-bold">
                      已勾選 {{ getTypeRuleSummary(item).count }} 條
                    </span>
                    <div class="max-w-[18rem] truncate" :title="getTypeRuleSummary(item).label">{{ getTypeRuleSummary(item).label }}</div>
                  </div>
                  <span v-else-if="item.ruleGroupId && getTypeRuleSummary(item).count === null" class="font-bold text-amber-600 dark:text-amber-400">{{ getTypeRuleSummary(item).label }}</span>
                  <span v-else class="italic text-slate-400">不套用規則</span>
                </td>
                <td class="py-4 px-6 font-mono text-[11px] text-slate-500 dark:text-slate-400 max-w-xs truncate" :title="item.formulaConfigJson">{{ item.formulaConfigJson }}</td>
                <td class="py-4 px-6 text-center">
                  <span :class="[ 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide', item.isEnabled ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700' ]">
                    <span :class="['w-1.5 h-1.5 rounded-full', item.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                    {{ item.isEnabled ? '啟用中' : '已停用' }}
                  </span>
                </td>
                <td class="py-4 px-6 text-right space-x-2">
                  <button @click="openTypesEdit(item)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-slate-700" title="編輯圖表種類"><Edit class="w-4 h-4" /></button>
                  <button @click="deleteType(item)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-955 text-red-600 dark:text-red-400 border border-red-200 dark:border-slate-700" title="刪除圖表種類"><Trash2 class="w-4 h-4" /></button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ========================================================================================= -->
    <!-- MODALS & SLIDE-OVERS -->
    <!-- ========================================================================================= -->
    
    <!-- 1. Groups Modal -->
    <div v-if="groupsShowModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in">
      <div class="bg-white dark:bg-slate-900 w-full max-w-lg rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-indigo-600 text-white rounded-xl shadow-md"><Layers class="w-5 h-5" /></div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">{{ groupsModalMode === 'create' ? '新增管制圖群組' : '編輯管制圖群組' }}</h3>
          </div>
          <button @click="groupsShowModal = false" type="button" class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400"><X class="w-5 h-5" /></button>
        </div>
        <form @submit.prevent="saveGroup" class="p-6 space-y-5">
          <div v-if="groupsFormErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 rounded-xl text-xs font-bold"><XCircle class="w-4 h-4" /> {{ groupsFormErr }}</div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">群組代號 (Code) *</label>
              <input v-model="groupsForm.groupCode" type="text" required placeholder="例如：PROC" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-805" />
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">群組名稱 (Name) *</label>
              <input v-model="groupsForm.groupName" type="text" required placeholder="例如：製程管制項目" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl" />
            </div>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">群組類型 *</label>
            <select v-model="groupsForm.groupType" required class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-805">
              <option value="CONTROL_CHART">管制圖 (Control Chart)</option>
              <option value="TREND_CHART">趨勢圖 (Trend Chart)</option>
            </select>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">說明描述 (Description)</label>
            <textarea v-model="groupsForm.description" rows="3" placeholder="請輸入描述..." class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm resize-none"></textarea>
          </div>
          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-2">啟用狀態</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input v-model="groupsForm.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ groupsForm.isEnabled ? '啟用' : '停用' }}</span>
            </label>
          </div>
          <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
            <button @click="groupsShowModal = false" type="button" class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-sm font-bold">取消</button>
            <button type="submit" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 text-white text-sm font-bold shadow-lg shadow-indigo-500/20"><Save class="w-4 h-4" /> 儲存</button>
          </div>
        </form>
      </div>
    </div>

    <!-- 2. Categories Modal -->
    <div v-if="categoriesShowModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in">
      <div class="bg-white dark:bg-slate-900 w-full max-w-lg rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-cyan-600 text-white rounded-xl shadow-md"><FolderTree class="w-5 h-5" /></div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">{{ categoriesModalMode === 'create' ? '新增中分類' : '編輯中分類' }}</h3>
          </div>
          <button @click="categoriesShowModal = false" type="button" class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400"><X class="w-5 h-5" /></button>
        </div>
        <form @submit.prevent="saveCategory" class="p-6 space-y-5">
          <div v-if="categoriesFormErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 rounded-xl text-xs font-bold"><XCircle class="w-4 h-4" /> {{ categoriesFormErr }}</div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">所屬大群組 (Chart Group) *</label>
            <select v-model="categoriesForm.chartGroupId" required class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-805">
              <option disabled value="null">-- 請選擇所屬群組 --</option>
              <option v-for="g in groupsRows" :key="g.id" :value="g.id">{{ g.groupCode }} - {{ g.groupName }}</option>
            </select>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">類別代號 (Code) *</label>
              <input v-model="categoriesForm.categoryCode" type="text" required placeholder="例如：VAR_PROC" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-805" />
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">類別名稱 (Name) *</label>
              <input v-model="categoriesForm.categoryName" type="text" required placeholder="例如：計量型製程管制" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl" />
            </div>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">類別說明描述 (Description)</label>
            <textarea v-model="categoriesForm.description" rows="3" placeholder="請輸入說明描述..." class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm resize-none"></textarea>
          </div>
          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-2">啟用狀態</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input v-model="categoriesForm.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-cyan-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ categoriesForm.isEnabled ? '啟用' : '停用' }}</span>
            </label>
          </div>
          <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
            <button @click="categoriesShowModal = false" type="button" class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-sm font-bold">取消</button>
            <button type="submit" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-cyan-600 to-blue-600 text-white text-sm font-bold shadow-lg shadow-cyan-500/20"><Save class="w-4 h-4" /> 儲存</button>
          </div>
        </form>
      </div>
    </div>

    <!-- 3. Types Modal -->
    <div v-if="typesShowModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in overflow-y-auto">
      <div class="bg-white dark:bg-slate-900 w-full max-w-xl rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden my-8">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-blue-600 text-white rounded-xl shadow-md"><Activity class="w-5 h-5" /></div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">{{ typesModalMode === 'create' ? '新增管制圖小分類' : '編輯管制圖小分類' }}</h3>
          </div>
          <button @click="typesShowModal = false" type="button" class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400"><X class="w-5 h-5" /></button>
        </div>
        <form @submit.prevent="saveType" class="p-6 space-y-5">
          <div v-if="typesFormErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 rounded-xl text-xs font-bold"><XCircle class="w-4 h-4" /> {{ typesFormErr }}</div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">所屬管制圖大類別 *</label>
            <select v-model="typesForm.chartGroupId" required class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-805">
              <option disabled value="null">-- 請選擇所屬大類別 --</option>
              <option v-for="g in groupsRows" :key="g.id" :value="g.id">{{ g.groupCode }} - {{ g.groupName }}</option>
            </select>
            <p class="text-[11px] text-slate-400 mt-1">中分類已改為系統內部資料，新增小分類時會自動掛到選定的大類別。</p>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">小分類代號 (Code) *</label>
              <input v-model="typesForm.chartTypeCode" type="text" required placeholder="例如：XBAR_R" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono font-bold text-slate-805" />
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">小分類名稱 (Name) *</label>
              <input v-model="typesForm.chartTypeName" type="text" required placeholder="例如：平均數與全距圖" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl" />
            </div>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">資料類型 (Category)</label>
              <select v-model="typesForm.dataCategory" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-805">
                <option value="Variable">計量型 (Variable)</option>
                <option value="Attribute">計數型 (Attribute)</option>
              </select>
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">標準抽樣數 (Required N)</label>
              <input v-model="typesForm.requiredSampleSize" type="number" min="1" max="25" placeholder="例如：5" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono" />
            </div>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">管制規則套用</label>
            <div class="rounded-2xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/70 p-3 space-y-3">
              <div class="flex items-center justify-between gap-3">
                <p class="text-[11px] text-slate-500 dark:text-slate-400">
                  從 SPC 異常規則庫勾選此小分類要套用的規則，管制圖計算只會帶入已勾選規則。
                </p>
                <div class="flex items-center gap-1.5 shrink-0">
                  <button @click="selectAllTypeRules" type="button" class="px-2.5 py-1 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-[11px] font-bold text-blue-600 dark:text-blue-400">全選</button>
                  <button @click="clearTypeRules" type="button" class="px-2.5 py-1 rounded-lg bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 text-[11px] font-bold text-slate-500 dark:text-slate-400">清除</button>
                </div>
              </div>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-2">
                <label
                  v-for="rule in typeRuleOptions"
                  :key="rule.ruleCode"
                  class="flex items-start gap-2 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-900 px-3 py-2 cursor-pointer hover:border-blue-300 dark:hover:border-blue-700 transition-colors"
                >
                  <input
                    v-model="selectedTypeRuleCodes"
                    :value="rule.ruleCode"
                    type="checkbox"
                    class="mt-0.5 h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                  />
                  <span class="min-w-0">
                    <span class="block text-xs font-black text-slate-700 dark:text-slate-200">規則 {{ Math.floor(rule.priority / 10) }}</span>
                    <span class="block text-[11px] leading-4 text-slate-500 dark:text-slate-400">{{ rule.ruleName }}</span>
                  </span>
                </label>
              </div>
            </div>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">說明描述 (Description)</label>
            <textarea v-model="typesForm.description" rows="2" placeholder="說明描述..." class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm resize-none"></textarea>
          </div>
          <div class="space-y-1.5">
            <div class="flex items-center justify-between flex-wrap gap-2">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 flex items-center gap-1.5">公式配置 JSON</label>
              <div class="flex items-center gap-1.5 flex-wrap">
                <button @click="setStandardFormula" type="button" class="px-2.5 py-1 rounded bg-blue-50 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-blue-800 text-[10px] font-bold">標準全距法</button>
                <button @click="setMrMethodFormula" type="button" class="px-2.5 py-1 rounded bg-purple-50 dark:bg-purple-950/60 text-purple-600 dark:text-purple-400 border border-purple-200 dark:border-purple-800 text-[10px] font-bold">平均移動全距法</button>
                <button @click="setSigmaMethodFormula" type="button" class="px-2.5 py-1 rounded bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800 text-[10px] font-bold">樣本標準差法</button>
              </div>
            </div>
            <textarea v-model="typesForm.formulaConfigJson" rows="5" class="w-full px-4 py-2.5 bg-slate-900 text-emerald-400 dark:bg-black font-mono text-xs rounded-xl border border-slate-700 resize-none"></textarea>
          </div>
          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-2">啟用狀態</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input v-model="typesForm.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ typesForm.isEnabled ? '啟用' : '停用' }}</span>
            </label>
          </div>
          <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
            <button @click="typesShowModal = false" type="button" class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-sm font-bold">取消</button>
            <button type="submit" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white text-sm font-bold shadow-lg shadow-blue-500/20"><Save class="w-4 h-4" /> 儲存</button>
          </div>
        </form>
      </div>
    </div>

  </section>
</template>
