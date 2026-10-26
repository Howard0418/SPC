<script setup>
import { useI18n } from "vue-i18n";
import LocalePicker from "../components/LocalePicker.vue";
import { displayUiMessage } from "../i18n/uiMessage";
const { t } = useI18n({ useScope: "global" });
const messageText = value => displayUiMessage(t, value);
import { onMounted, ref, computed, watch } from "vue";
import { useRoute } from "vue-router";
import { api, getApiErrorDisplay } from "../api/client";
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

const { embedded, groupsOnly } = defineProps({
  embedded: {
    type: Boolean,
    default: false
  },
  groupsOnly: {
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
  businessScopeCode: "",
  requiresPart: false,
  requiresMachine: false,
  requiresTank: false,
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
  groupsForm.value = { groupCode: "", groupName: "", description: "", groupType: "CONTROL_CHART", businessScopeCode: "", requiresPart: false, requiresMachine: false, requiresTank: false, isEnabled: true };
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
    businessScopeCode: item.businessScopeCode || item.groupCode || "",
    requiresPart: item.requiresPart ?? false,
    requiresMachine: item.requiresMachine ?? false,
    requiresTank: item.requiresTank ?? false,
    isEnabled: item.isEnabled ?? true
  };
  groupsFormErr.value = "";
  groupsShowModal.value = true;
}

async function saveGroup() {
  if (!groupsForm.value.groupCode?.trim() || !groupsForm.value.groupName?.trim() || !groupsForm.value.businessScopeCode?.trim()) {
    groupsFormErr.value = { key: "spc.groups.required" };
    return;
  }
  groupsFormErr.value = "";
  loading.value = true;
  try {
    if (groupsModalMode.value === "create") {
      const { data } = await api.post("/control-chart-groups", groupsForm.value);
      groupsRows.value.push(data);
      successAlert({ key: "spc.groups.created", args: { name: String(data.groupName) } });
    } else {
      const { data } = await api.put(`/control-chart-groups/${groupsCurrentId.value}`, groupsForm.value);
      const idx = groupsRows.value.findIndex(x => x.id === groupsCurrentId.value);
      if (idx !== -1) groupsRows.value[idx] = data;
      successAlert({ key: "spc.groups.updated", args: { name: String(data.groupName) } });
    }
    groupsShowModal.value = false;
  } catch (e) {
    groupsFormErr.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

async function deleteGroup(item) {
  if (!confirm(t("spc.groups.confirmDelete", { code: String(item.groupCode), name: String(item.groupName) }))) return;
  loading.value = true;
  try {
    await api.delete(`/control-chart-groups/${item.id}`);
    groupsRows.value = groupsRows.value.filter(x => x.id !== item.id);
    successAlert({ key: "spc.groups.deleted", args: { name: String(item.groupName) } });
  } catch (e) {
    err.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

// ==========================================
// 2. TYPES STATE & METHODS
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

const groupLabelMap = computed(() => {
  const map = {};
  groupsRows.value.forEach(g => {
    map[g.id] = `${g.groupCode} (${g.groupName})`;
  });
  return map;
});

const defaultGroupId = computed(() => groupsRows.value[0]?.id || null);

function getTypeGroupId(type) {
  return type.chartGroupId || null;
}

function getTypeGroupLabel(type) {
  const groupId = getTypeGroupId(type);
  return groupId ? (groupLabelMap.value[groupId] || `大類別 #${groupId}`) : "未指定大類別";
}

// Display fallback is localized separately from the original search label.
function getTypeGroupDisplayLabel(type) {
  const id = getTypeGroupId(type);
  return id ? (groupLabelMap.value[id] || t('spc.chartTypes.missingGroup', { id })) : t('spc.chartTypes.unassignedGroup');
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
  if (route.query.tab === "groups" || route.query.tab === "types") {
    activeTab.value = route.query.tab;
  }
}

function defaultTypeRuleCodes() {
  return [...ruleLibraryRows.value]
    .sort((a, b) => (Number(a.priority) || 0) - (Number(b.priority) || 0) || (Number(a.id) || 0) - (Number(b.id) || 0))
    .map(rule => rule.ruleCode);
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
    typesFormErr.value = { key: "spc.chartTypes.loadRulesFailed", args: { error: getApiErrorDisplay(e) } };
  }
}

async function saveType() {
  if (!typesForm.value.chartTypeCode?.trim() || !typesForm.value.chartTypeName?.trim() || !typesForm.value.chartGroupId) {
    typesFormErr.value = { key: "spc.chartTypes.required" };
    return;
  }
  try {
    JSON.parse(typesForm.value.formulaConfigJson || "{}");
  } catch(e) {
    typesFormErr.value = { key: "spc.chartTypes.invalidJson" };
    return;
  }
  typesFormErr.value = "";
  loading.value = true;
  try {
    const payload = {
      ...typesForm.value,
      chartGroupId: parseInt(typesForm.value.chartGroupId),
      ruleGroupId: typesForm.value.ruleGroupId ? parseInt(typesForm.value.ruleGroupId) : null,
      requiredSampleSize: parseInt(typesForm.value.requiredSampleSize) || 1
    };

    let savedType = null;
    if (typesModalMode.value === "create") {
      const { data } = await api.post("/control-chart-types", payload);
      savedType = data;
      successAlert({ key: "spc.chartTypes.created", args: { name: String(data.chartTypeName) } });
    } else {
      const { data } = await api.put(`/control-chart-types/${typesCurrentId.value}`, payload);
      savedType = data;
      successAlert({ key: "spc.chartTypes.updated", args: { name: String(data.chartTypeName) } });
    }
    const idx = typesRows.value.findIndex(x => x.id === savedType.id);
    if (idx !== -1) typesRows.value[idx] = savedType;
    else typesRows.value.push(savedType);
    await loadRuleGroups();
    typesShowModal.value = false;
  } catch (e) {
    typesFormErr.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

async function deleteType(item) {
  const label = ["I_MR", "I-MR"].includes(String(item.chartTypeCode || "").toUpperCase())
    ? "I_MR"
    : `${item.chartTypeCode} (${item.chartTypeName})`;
  if (!confirm(t("spc.chartTypes.confirmDelete", { name: label }))) return;
  loading.value = true;
  try {
    await api.delete(`/control-chart-types/${item.id}`);
    typesRows.value = typesRows.value.filter(x => x.id !== item.id);
    successAlert({ key: "spc.chartTypes.deleted", args: { name: String(item.chartTypeName) } });
  } catch (e) {
    err.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

// ==========================================
// 3. UNIFIED LOAD
// ==========================================
async function loadAll() {
  err.value = "";
  loading.value = true;
  try {
    await loadGroups();
    await loadRuleGroups();
    await loadRuleLibrary();
    await loadTypes();
  } catch (e) {
    err.value = { key: "spc.groups.loadFailed", args: { error: getApiErrorDisplay(e) } };
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
      if (targetType) openTypesEdit(targetType);
    } else if (activeTab.value === "groups") {
      const targetGroup = groupsRows.value.find(g => g.id === editId);
      if (targetGroup) openGroupsEdit(targetGroup);
    }
  }
});

watch(() => route.query.tab, syncTabFromRoute);
</script>

<template>
  <section class="space-y-6">
    <!-- Header Banner -->
    <div v-if="!embedded" class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex flex-wrap items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-indigo-600 to-blue-500 rounded-xl shadow-lg shadow-indigo-500/30 text-white">
          <Activity class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">{{ t('spc.groups.title') }}</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ t('spc.groups.subtitle') }}</p>
        </div>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <router-link
          to="/part-process-characteristics"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-amber-50 hover:bg-amber-100 dark:bg-slate-800 dark:hover:bg-slate-700 text-amber-600 dark:text-amber-400 text-sm font-semibold transition-all border border-amber-200 dark:border-slate-700"
        >
          <FolderTree class="w-4 h-4" /> {{ t('spc.groups.standards') }}
        </router-link>
        <router-link
          to="/spc"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-50 hover:bg-blue-100 dark:bg-slate-800 dark:hover:bg-slate-700 text-blue-600 dark:text-blue-400 text-sm font-semibold transition-all border border-blue-200 dark:border-slate-700"
        >
          <Activity class="w-4 h-4" /> {{ t('spc.groups.charts') }}
        </router-link>
        <button
          @click="loadAll"
          type="button"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-sm font-semibold transition-all border border-slate-200 dark:border-slate-700"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> {{ t('common.actions.refresh') }}
        </button>
      </div>
    </div>

    <!-- Alert Messages -->
    <div v-if="err" class="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500" />
      <div class="text-sm font-semibold">{{ messageText(err) }}</div>
    </div>
    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ messageText(successMsg) }}</div>
    </div>

    <!-- Tab Header -->
    <div v-if="!embedded && !groupsOnly" class="flex border-b border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 rounded-2xl p-1 shadow-sm border">
      <button
        @click="activeTab = 'groups'"
        :class="activeTab === 'groups' ? 'bg-indigo-50 dark:bg-slate-800 text-indigo-600 dark:text-indigo-400 border border-indigo-200 dark:border-slate-700 font-bold' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        class="flex-1 py-3 px-4 rounded-xl text-center text-sm transition-all whitespace-nowrap"
      >
        {{ t('spc.chartTypes.groupsTab') }}
      </button>
      <button
        @click="activeTab = 'types'"
        :class="activeTab === 'types' ? 'bg-indigo-50 dark:bg-slate-800 text-indigo-600 dark:text-indigo-400 border border-indigo-200 dark:border-slate-700 font-bold' : 'border-transparent text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'"
        class="flex-1 py-3 px-4 rounded-xl text-center text-sm transition-all whitespace-nowrap"
      >
        {{ t('spc.chartTypes.typesTab') }}
      </button>
    </div>

    <!-- TAB 1: GROUPS VIEW -->
    <div v-if="activeTab === 'groups'" class="space-y-6 animate-fade-in">
      <div class="p-5 bg-gradient-to-r from-indigo-50 to-blue-50 dark:from-indigo-950/30 dark:to-blue-900/20 border border-indigo-100 dark:border-indigo-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
        <div class="p-2 bg-indigo-100 dark:bg-indigo-900/50 rounded-xl text-indigo-600 dark:text-indigo-400 mt-0.5"><Info class="w-5 h-5" /></div>
        <div>
          <h4 class="text-sm font-bold text-indigo-900 dark:text-indigo-300">{{ t('spc.groups.guideTitle') }}</h4>
          <p class="text-xs text-indigo-700 dark:text-indigo-400/80 mt-1.5 leading-relaxed">
            {{ t('spc.groups.guideIntro') }}
          </p>
        </div>
      </div>

      <div class="flex flex-col sm:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
        <div class="relative w-full sm:w-80">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400"><Search class="w-4 h-4" /></span>
          <input v-model="groupsSearchQuery" type="text" :placeholder="t('spc.groups.search')" class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500" />
        </div>
        <div class="flex items-center gap-3 w-full sm:w-auto">
          <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
            <button v-for="f in [{id:'all', label:t('spc.groups.all')}, {id:'active', label:t('common.status.enabled')}, {id:'inactive', label:t('common.status.inactive')}]" :key="f.id" @click="groupsStatusFilter = f.id" type="button" :class="[ 'px-4 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap', groupsStatusFilter === f.id ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm' : 'text-slate-600 dark:text-slate-400 hover:text-slate-850' ]">{{ f.label }}</button>
          </div>
          <button data-testid="group-add" @click="openGroupsCreate" type="button" class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 transition-all whitespace-nowrap"><Plus class="w-4 h-4" /> {{ t('spc.groups.add') }}</button>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
                <th class="py-4 px-6 w-16 text-center">ID</th>
                <th class="py-4 px-6">{{ t('spc.groups.nameColumn') }}</th>
                <th class="py-4 px-6">{{ t('spc.groups.typeColumn') }}</th>
                <th class="py-4 px-6">{{ t('spc.groups.description') }}</th>
                <th class="py-4 px-6 text-center">{{ t('common.columns.status') }}</th>
                <th class="py-4 px-6 text-right">{{ t('common.columns.actions') }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
              <tr v-if="loading && groupsRows.length === 0"><td colspan="6" class="py-12 text-center text-slate-400">{{ t('spc.groups.loading') }}</td></tr>
              <tr v-else-if="filteredGroups.length === 0"><td colspan="6" class="py-12 text-center text-slate-400">{{ t('spc.groups.empty') }}</td></tr>
              <tr v-else v-for="g in filteredGroups" :key="g.id" class="hover:bg-indigo-50/50 dark:hover:bg-slate-800/50 transition-colors group">
                <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ g.id }}</td>
                <td class="py-4 px-6">
                  <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2"><Layers class="w-4 h-4 text-indigo-500" /> {{ g.groupCode }}</div>
                  <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ g.groupName }}</div>
                </td>
                <td class="py-4 px-6">
                  <span :class="[ 'px-2.5 py-1 rounded-lg text-xs font-bold border tracking-wide', g.groupType === 'TREND_CHART' ? 'bg-amber-50 text-amber-600 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800' : 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-950/60 dark:text-blue-300 dark:border-blue-800' ]">
                    {{ g.groupType === 'TREND_CHART' ? t('spc.groups.trend') : t('spc.groups.control') }}
                  </span>
                </td>
                <td class="py-4 px-6 text-slate-600 dark:text-slate-300">{{ g.description || '-' }}</td>
                <td class="py-4 px-6 text-center">
                  <span :class="[ 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide', g.isEnabled ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700' ]">
                    <span :class="['w-1.5 h-1.5 rounded-full', g.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                    {{ g.isEnabled ? t('common.status.active') : t('common.status.inactive') }}
                  </span>
                </td>
                <td class="py-4 px-6 text-right space-x-2">
                  <button data-testid="group-edit" @click="openGroupsEdit(g)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-slate-700" :title="t('spc.groups.edit')"><Edit class="w-4 h-4" /></button>
                  <button data-testid="group-delete" @click="deleteGroup(g)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 text-red-600 dark:text-red-400 border border-red-200 dark:border-slate-700" :title="t('spc.groups.delete')"><Trash2 class="w-4 h-4" /></button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- TAB 2: TYPES VIEW -->
    <div v-if="!groupsOnly && activeTab === 'types'" class="space-y-6 animate-fade-in">
      <div class="p-5 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-900/20 border border-blue-100 dark:border-indigo-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
        <div class="p-2 bg-blue-100 dark:bg-blue-900/50 rounded-xl text-blue-600 dark:text-blue-400 mt-0.5"><Info class="w-5 h-5" /></div>
        <div>
          <h4 class="text-sm font-bold text-blue-900 dark:text-blue-300">{{ t('spc.chartTypes.guideTitle') }}</h4>
          <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
            {{ t('spc.chartTypes.guideDescription') }}
          </p>
        </div>
      </div>

      <div class="flex flex-col lg:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
        <div class="relative w-full lg:w-80">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400"><Search class="w-4 h-4" /></span>
          <input data-testid="type-search" v-model="typesSearchQuery" type="text" :placeholder="t('spc.chartTypes.search')" class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
        <div class="flex flex-wrap items-center gap-3 w-full lg:w-auto justify-end">
          <select data-testid="type-group-filter" v-model="typesGroupFilter" class="px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option value="all">{{ t('spc.chartTypes.allGroups') }}</option>
            <option v-for="g in groupsRows" :key="g.id" :value="g.id">{{ g.groupCode }} - {{ g.groupName }}</option>
          </select>
          <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
            <button v-for="f in [{id:'all', label:t('common.status.all')}, {id:'active', label:t('common.status.enabled')}, {id:'inactive', label:t('common.status.inactive')}]" :key="f.id" @click="typesStatusFilter = f.id" type="button" :class="[ 'px-3 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap', typesStatusFilter === f.id ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm' : 'text-slate-600 dark:text-slate-400' ]">{{ f.label }}</button>
          </div>
          <button data-testid="type-add" @click="openTypesCreate" type="button" class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white text-sm font-bold shadow-lg shadow-blue-500/25 transition-all"><Plus class="w-4 h-4" /> {{ t('spc.chartTypes.add') }}</button>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
                <th class="py-4 px-6 w-16 text-center">ID</th>
                <th class="py-4 px-6">{{ t('spc.chartTypes.groupColumn') }}</th>
                <th class="py-4 px-6">{{ t('spc.chartTypes.typeColumn') }}</th>
                <th class="py-4 px-6">{{ t('spc.chartTypes.categoryColumn') }}</th>
                <th class="py-4 px-6 text-center">{{ t('spc.chartTypes.sampleColumn') }}</th>
                <th class="py-4 px-6">{{ t('spc.chartTypes.rules') }}</th>
                <th class="py-4 px-6">{{ t('spc.chartTypes.formulaColumn') }}</th>
                <th class="py-4 px-6 text-center">{{ t('common.columns.status') }}</th>
                <th class="py-4 px-6 text-right">{{ t('common.columns.actions') }}</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
              <tr v-if="loading && typesRows.length === 0"><td colspan="9" class="py-12 text-center text-slate-400">{{ t('spc.chartTypes.loading') }}</td></tr>
              <tr v-else-if="filteredTypes.length === 0"><td colspan="9" class="py-12 text-center text-slate-400">{{ t('spc.chartTypes.empty') }}</td></tr>
              <tr v-else v-for="item in filteredTypes" :key="item.id" class="hover:bg-blue-50/50 dark:hover:bg-slate-800/50 transition-colors group">
                <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ item.id }}</td>
                <td class="py-4 px-6">
                  <span class="text-xs font-bold text-cyan-700 dark:text-cyan-300 bg-cyan-50 dark:bg-cyan-950/60 border border-cyan-200 dark:border-cyan-800 px-2.5 py-1 rounded-lg">
                    {{ getTypeGroupDisplayLabel(item) }}
                  </span>
                </td>
                <td class="py-4 px-6 font-bold text-slate-900 dark:text-white">
                  <div class="flex items-center gap-2 text-indigo-600 dark:text-indigo-400 font-mono"><Activity class="w-4 h-4 text-blue-500" /> {{ item.chartTypeCode }}</div>
                  <div v-if="!['I_MR', 'I-MR'].includes(String(item.chartTypeCode || '').toUpperCase())" class="text-xs text-slate-500 font-sans mt-0.5">{{ item.chartTypeName }}</div>
                </td>
                <td class="py-4 px-6">
                  <span :class="[ 'px-2.5 py-1 rounded-lg text-xs font-bold border tracking-wide', item.dataCategory === 'Variable' ? 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-950/60 dark:text-blue-300 dark:border-blue-800' : 'bg-amber-50 text-amber-600 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800' ]">
                    {{ item.dataCategory === 'Variable' ? t('spc.characteristics.variable') : t('spc.characteristics.attribute') }}
                  </span>
                </td>
                <td class="py-4 px-6 text-center font-mono font-bold text-xs text-slate-600 dark:text-slate-300">{{ item.requiredSampleSize }}</td>
                <td class="py-4 px-6 text-xs text-slate-600 dark:text-slate-300">
                  <div v-if="getTypeRuleSummary(item).count" class="space-y-1">
                    <span class="inline-flex items-center px-2 py-0.5 rounded-full bg-amber-50 dark:bg-amber-950/60 text-amber-700 dark:text-amber-300 border border-amber-200 dark:border-amber-800 font-bold">
                      {{ t('spc.chartTypes.selectedRules', { count: getTypeRuleSummary(item).count }) }}
                    </span>
                    <div class="max-w-[18rem] truncate" :title="getTypeRuleSummary(item).label">{{ getTypeRuleSummary(item).label }}</div>
                  </div>
                  <span v-else-if="item.ruleGroupId && getTypeRuleSummary(item).count === null" class="font-bold text-amber-600 dark:text-amber-400">{{ getTypeRuleSummary(item).label }}</span>
                  <span v-else class="italic text-slate-400">{{ t('spc.chartTypes.noRules') }}</span>
                </td>
                <td class="py-4 px-6 font-mono text-[11px] text-slate-500 dark:text-slate-400 max-w-xs truncate" :title="item.formulaConfigJson">{{ item.formulaConfigJson }}</td>
                <td class="py-4 px-6 text-center">
                  <span :class="[ 'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide', item.isEnabled ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700' ]">
                    <span :class="['w-1.5 h-1.5 rounded-full', item.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                    {{ item.isEnabled ? t('common.status.active') : t('common.status.inactive') }}
                  </span>
                </td>
                <td class="py-4 px-6 text-right space-x-2">
                  <button data-testid="type-edit" @click="openTypesEdit(item)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-slate-700" :title="t('spc.chartTypes.edit')"><Edit class="w-4 h-4" /></button>
                  <button data-testid="type-delete" @click="deleteType(item)" type="button" class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 text-red-600 dark:text-red-400 border border-red-200 dark:border-slate-700" :title="t('spc.chartTypes.delete')"><Trash2 class="w-4 h-4" /></button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- MODAL: Groups -->
    <div v-if="groupsShowModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in">
      <div role="dialog" class="bg-white dark:bg-slate-900 w-full max-w-lg max-h-[90vh] overflow-y-auto rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex flex-wrap items-center gap-3">
            <div class="p-2.5 bg-indigo-600 text-white rounded-xl shadow-md"><Layers class="w-5 h-5" /></div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">{{ groupsModalMode === 'create' ? t('spc.groups.createTitle') : t('spc.groups.editTitle') }}</h3>
          </div>
          <button :aria-label="t('common.actions.close')" @click="groupsShowModal = false" type="button" class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400"><X class="w-5 h-5" /></button>
        </div>
        <div class="px-6 py-2 shrink-0"><LocalePicker /></div>
        <form @submit.prevent="saveGroup" class="p-6 space-y-5">
          <div v-if="groupsFormErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 rounded-xl text-xs font-bold"><XCircle class="w-4 h-4" /> {{ messageText(groupsFormErr) }}</div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.groups.codeField') }}</label>
              <input data-testid="groupsForm-groupCode" v-model="groupsForm.groupCode" type="text" required :placeholder="t('spc.groups.codeExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.groups.nameField') }}</label>
              <input data-testid="groupsForm-groupName" v-model="groupsForm.groupName" type="text" required :placeholder="t('spc.groups.nameExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </div>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.groups.typeField') }}</label>
            <select data-testid="groupsForm-groupType" v-model="groupsForm.groupType" required class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500">
              <option value="CONTROL_CHART">{{ t('spc.groups.controlOption') }}</option>
              <option value="TREND_CHART">{{ t('spc.groups.trendOption') }}</option>
            </select>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.groups.scopeField') }}</label>
            <input data-testid="groupsForm-businessScopeCode" v-model="groupsForm.businessScopeCode" type="text" required :placeholder="t('spc.groups.scopeExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono font-bold uppercase text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            <p class="text-[11px] text-slate-500">{{ t('spc.groups.scopeHelp') }}</p>
          </div>
          <div class="space-y-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.groups.requiredRules') }}</label>
            <div class="grid grid-cols-1 sm:grid-cols-3 gap-2">
              <label class="flex items-center gap-2 p-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold"><input data-testid="groupsForm-requiresPart" v-model="groupsForm.requiresPart" type="checkbox" class="w-4 h-4" /> {{ t('spc.groups.part') }}</label>
              <label class="flex items-center gap-2 p-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold"><input data-testid="groupsForm-requiresMachine" v-model="groupsForm.requiresMachine" type="checkbox" class="w-4 h-4" /> {{ t('spc.groups.machine') }}</label>
              <label class="flex items-center gap-2 p-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold"><input data-testid="groupsForm-requiresTank" v-model="groupsForm.requiresTank" type="checkbox" class="w-4 h-4" /> {{ t('spc.groups.tank') }}</label>
            </div>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.groups.descriptionField') }}</label>
            <textarea data-testid="groupsForm-description" v-model="groupsForm.description" rows="3" :placeholder="t('spc.groups.descriptionExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm resize-none dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500"></textarea>
          </div>
          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-2">{{ t('spc.groups.enabled') }}</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input data-testid="groupsForm-isEnabled" v-model="groupsForm.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ groupsForm.isEnabled ? t('common.status.on') : t('common.status.off') }}</span>
            </label>
          </div>
          <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
            <button @click="groupsShowModal = false" type="button" class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-sm font-bold">{{ t('common.actions.cancel') }}</button>
            <button type="submit" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 text-white text-sm font-bold shadow-lg shadow-indigo-500/20"><Save class="w-4 h-4" /> {{ t('common.actions.save') }}</button>
          </div>
        </form>
      </div>
    </div>

    <!-- MODAL: Types -->
    <div v-if="typesShowModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in overflow-y-auto">
      <div role="dialog" class="bg-white dark:bg-slate-900 w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden my-8">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-blue-600 text-white rounded-xl shadow-md"><Activity class="w-5 h-5" /></div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">{{ typesModalMode === 'create' ? t('spc.chartTypes.createTitle') : t('spc.chartTypes.editTitle') }}</h3>
          </div>
          <button :aria-label="t('common.actions.close')" @click="typesShowModal = false" type="button" class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400"><X class="w-5 h-5" /></button>
        </div>
        <div class="px-6 py-2"><LocalePicker /></div>
        <form @submit.prevent="saveType" class="p-6 space-y-5">
          <div v-if="typesFormErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 rounded-xl text-xs font-bold"><XCircle class="w-4 h-4" /> {{ messageText(typesFormErr) }}</div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.chartTypes.groupField') }}</label>
            <select data-testid="type-chartGroupId" v-model="typesForm.chartGroupId" required class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option disabled :value="null">{{ t('spc.chartTypes.groupPrompt') }}</option>
              <option v-for="g in groupsRows" :key="g.id" :value="g.id">{{ g.groupCode }} - {{ g.groupName }}</option>
            </select>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.chartTypes.codeField') }}</label>
              <input data-testid="type-chartTypeCode" v-model="typesForm.chartTypeCode" type="text" required :placeholder="t('spc.chartTypes.codeExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono font-bold text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.chartTypes.nameField') }}</label>
              <input data-testid="type-chartTypeName" v-model="typesForm.chartTypeName" type="text" required :placeholder="t('spc.chartTypes.nameExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.chartTypes.categoryField') }}</label>
              <select data-testid="type-dataCategory" v-model="typesForm.dataCategory" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="Variable">{{ t('spc.chartTypes.variableOption') }}</option>
                <option value="Attribute">{{ t('spc.chartTypes.attributeOption') }}</option>
              </select>
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.chartTypes.sampleField') }}</label>
              <input data-testid="type-requiredSampleSize" v-model="typesForm.requiredSampleSize" type="number" min="1" max="50" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono font-bold text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <!-- Rule Selection -->
          <div class="space-y-2 p-4 bg-amber-50 dark:bg-amber-950/30 border border-amber-200 dark:border-amber-800/50 rounded-2xl">
            <label class="block text-xs font-bold text-amber-800 dark:text-amber-300 uppercase tracking-wider flex items-center gap-2"><Sliders class="w-4 h-4" /> {{ t('spc.chartTypes.rules') }}</label>
            <p class="text-[11px] text-amber-700 dark:text-amber-400/80">{{ t('spc.chartTypes.fixedRules') }}</p>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-2">
              <label v-for="rule in typeRuleOptions" :key="rule.ruleCode" class="flex items-start gap-2.5 p-2.5 rounded-xl bg-white dark:bg-slate-800 border border-amber-200 dark:border-slate-700 cursor-pointer hover:border-amber-400 transition-colors">
                <input type="checkbox" :value="rule.ruleCode" v-model="selectedTypeRuleCodes" disabled class="mt-0.5 w-4 h-4 rounded accent-amber-500" />
                <div>
                  <div class="text-xs font-bold text-slate-800 dark:text-slate-200">{{ rule.ruleName }}</div>
                  <div class="text-[10px] text-slate-500 dark:text-slate-400 mt-0.5">{{ rule.description }}</div>
                </div>
              </label>
              <div v-if="typeRuleOptions.length === 0" class="col-span-2 text-xs text-slate-400 text-center py-3">
                {{ t('spc.chartTypes.emptyRules') }}
              </div>
            </div>
          </div>
          <!-- Formula JSON -->
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 flex items-center gap-2"><Code2 class="w-4 h-4" /> {{ t('spc.chartTypes.formulaField') }}</label>
            <textarea data-testid="type-formulaConfigJson" v-model="typesForm.formulaConfigJson" rows="6" spellcheck="false" class="w-full px-4 py-2.5 bg-slate-900 dark:bg-slate-950 border border-slate-700 rounded-xl font-mono text-xs text-green-400 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"></textarea>
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">{{ t('spc.chartTypes.descriptionField') }}</label>
            <textarea data-testid="type-description" v-model="typesForm.description" rows="2" :placeholder="t('spc.chartTypes.descriptionExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm resize-none dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"></textarea>
          </div>
          <div class="space-y-1.5 pt-2">
            <label class="relative inline-flex items-center cursor-pointer">
              <input data-testid="type-isEnabled" v-model="typesForm.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ typesForm.isEnabled ? t('common.status.on') : t('common.status.off') }}</span>
            </label>
          </div>
          <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
            <button @click="typesShowModal = false" type="button" class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 text-sm font-bold">{{ t('common.actions.cancel') }}</button>
            <button type="submit" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white text-sm font-bold shadow-lg shadow-blue-500/20"><Save class="w-4 h-4" /> {{ t('common.actions.save') }}</button>
          </div>
        </form>
      </div>
    </div>
  </section>
</template>
