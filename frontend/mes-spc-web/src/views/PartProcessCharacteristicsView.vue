<script setup>
import { onMounted, ref, computed, watch } from "vue";
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
  Cpu
} from "lucide-vue-next";

const rows = ref([]);
const parts = ref([]);
const processes = ref([]);
const characteristics = ref([]);
const chartTypes = ref([]);
const ruleGroups = ref([]);
const ruleLibrary = ref([]);
const categories = ref([]);
const groups = ref([]);
const machines = ref([]);
const lines = ref([]);
const tanks = ref([]);

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
  chartTypeId: null,
  formulaConfigJson: "",
  selectedRuleCodes: [],
  isRequired: true,
  isEnabled: true
});
const formErr = ref("");

const controlScopes = [
  { id: "PROCESS", label: "製程管制", tone: "purple" },
  { id: "CHEMICAL", label: "藥水管制", tone: "teal" },
  { id: "PRODUCT", label: "產品管制", tone: "blue" }
];

const formulaOptions = [
  { id: "", label: "繼承管制圖種類預設公式" },
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
  if (!config) return "繼承預設";
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

const scopeLabel = (scope) => controlScopes.find(s => s.id === (scope || "PRODUCT"))?.label || "產品管制";

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
    characteristics.value = resChar.data || [];
    chartTypes.value = resTypes.data || [];
    ruleGroups.value = resRules.data || [];
    ruleLibrary.value = resRuleLibrary.data || [];
    categories.value = resCats.data || [];
    groups.value = resGroups.data || [];
    machines.value = resMachines.data || [];
    lines.value = resLines.data || [];
    tanks.value = resTanks.data || [];
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

const selectedChartTypeDimension = computed(() => {
  if (!form.value.chartTypeId) return null;
  const type = chartTypes.value.find(t => t.id === Number(form.value.chartTypeId));
  if (!type) return null;
  const cat = categories.value.find(c => c.id === type.chartCategoryId);
  if (!cat) return null;
  const group = groups.value.find(g => g.id === cat.chartGroupId);
  return group ? group.groupCode : null;
});

const availableMachines = computed(() =>
  machines.value.filter(m => m.processId === Number(form.value.processId))
);

const availableTanks = ref([]);

watch(() => form.value.controlScope, (newScope) => {
  if (newScope !== "PRODUCT") {
    form.value.partId = null;
  }
  if (newScope !== "CHEMICAL") {
    form.value.machineId = null;
    form.value.tankId = null;
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
    const matchScope = scopeFilter.value === "all" || (row.controlScope || "PRODUCT") === scopeFilter.value;

    return matchQuery && matchStatus && matchProc && matchScope;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  availableTanks.value = [];
  form.value = {
    controlScope: "PRODUCT",
    partId: parts.value.length > 0 ? parts.value[0].id : null,
    processId: processes.value.length > 0 ? processes.value[0].id : null,
    machineId: null,
    tankId: null,
    characteristicId: characteristics.value.length > 0 ? characteristics.value[0].id : null,
    unit: characteristics.value[0]?.unit || "",
    usl: null, lsl: null, ucl: null, cl: null, lcl: null, targetValue: null,
    sampleSize: 5,
    chartTypeId: chartTypes.value.length > 0 ? chartTypes.value[0].id : null,
    formulaConfigJson: "",
    selectedRuleCodes: [],
    isRequired: true,
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

async function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  formErr.value = "";

  if ((item.controlScope || (item.partId ? "PRODUCT" : "PROCESS")) === "CHEMICAL" && item.machineId) {
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

  form.value = {
    controlScope: item.controlScope || (item.partId ? "PRODUCT" : "PROCESS"),
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
    chartTypeId: item.chartTypeId || null,
    formulaConfigJson: item.formulaConfigJson || "",
    selectedRuleCodes: [],
    isRequired: item.isRequired ?? true,
    isEnabled: item.isEnabled ?? true
  };
  try {
    const { data } = await api.get(`/part-process-characteristics/${item.id}/rules`);
    form.value.selectedRuleCodes = (data?.rules || [])
      .filter(rule => rule.isSelected)
      .map(rule => rule.ruleCode);
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
      ucl: form.value.ucl !== "" && form.value.ucl !== null ? parseFloat(form.value.ucl) : null,
      cl: form.value.cl !== "" && form.value.cl !== null ? parseFloat(form.value.cl) : null,
      lcl: form.value.lcl !== "" && form.value.lcl !== null ? parseFloat(form.value.lcl) : null,
      targetValue: form.value.targetValue !== "" && form.value.targetValue !== null ? parseFloat(form.value.targetValue) : null,
      sampleSize: parseInt(form.value.sampleSize) || 1,
      chartTypeId: form.value.chartTypeId ? parseInt(form.value.chartTypeId) : null
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
      selectedRuleCodes: form.value.selectedRuleCodes
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

onMounted(async () => {
  await load();
  const editId = Number(route.query.editId);
  if (editId) {
    const item = rows.value.find(r => r.id === editId);
    if (item) {
      openEditModal(item);
    }
  }
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

    <!-- Guide / Wizard Tip -->
    <div class="p-5 bg-gradient-to-r from-amber-50 to-orange-50 dark:from-amber-950/30 dark:to-orange-900/20 border border-amber-100 dark:border-amber-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-amber-100 dark:bg-amber-900/50 rounded-xl text-amber-600 dark:text-amber-400 mt-0.5">
        <Info class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-amber-900 dark:text-amber-300">模組指南：SPC 管制項目設定</h4>
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
      </div>
    </div>

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
    <div class="flex flex-col lg:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
      <div class="relative w-full lg:w-80">
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

      <div class="flex flex-wrap items-center gap-3 w-full lg:w-auto">
        <!-- Process Filter Dropdown -->
        <select
          v-model="scopeFilter"
          class="px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
        >
          <option value="all">全部管制類型</option>
          <option v-for="s in controlScopes" :key="s.id" :value="s.id">{{ s.label }}</option>
        </select>

        <select
          v-model="processFilter"
          class="px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
        >
          <option value="all">全廠所有製程</option>
          <option v-for="p in processes" :key="p.id" :value="p.id">
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
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
              <th class="py-4 px-6 w-16 text-center">ID</th>
              <th class="py-4 px-6">工站製程與生產機台 (Process & Machines)</th>
              <th class="py-4 px-6">管制類型與檢驗特性 (Scope & Characteristic)</th>
              <th class="py-4 px-6">規格限值與抽樣配置 (Limits & Sample Size)</th>
              <th class="py-4 px-6">管制圖與規則來源</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">正在載入檢驗基準清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">找不到相符的檢驗基準資料</td>
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
                <div class="mt-2.5 flex flex-wrap items-center gap-2 text-[10px] font-mono text-slate-500">
                  <span>UCL: {{ item.ucl !== null ? item.ucl : '自動' }}</span> |
                  <span>CL: {{ item.cl !== null ? item.cl : '自動' }}</span> |
                  <span>LCL: {{ item.lcl !== null ? item.lcl : '自動' }}</span> |
                  <span class="px-1.5 py-0.5 rounded bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 font-bold border border-blue-200 dark:border-blue-800">
                    N={{ item.sampleSize || 1 }}
                  </span>
                </div>
              </td>
              <td class="py-5 px-6 text-xs space-y-2">
                <div class="flex items-center gap-1.5">
                  <Activity class="w-3.5 h-3.5 text-indigo-500 flex-shrink-0" />
                  <span v-if="item.chartTypeId" class="font-bold text-indigo-600 dark:text-indigo-400">{{ chartTypeMap[item.chartTypeId] || `Chart #${item.chartTypeId}` }}</span>
                  <span v-else class="text-slate-400 italic">自動判斷管制圖</span>
                </div>
                <div class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
                  <AlertTriangle class="w-3 h-3 flex-shrink-0 opacity-70" />
                  <span v-if="getEffectiveRuleGroupId(item)" class="font-semibold">
                    {{ item.ruleGroupId ? '項目專屬規則' : (ruleGroupMap[getEffectiveRuleGroupId(item)] || `Rule #${getEffectiveRuleGroupId(item)}`) }}
                  </span>
                  <span v-else class="italic opacity-80">此管制圖未套用規則</span>
                </div>
                <div class="flex items-center gap-1.5 text-slate-500 dark:text-slate-400">
                  <Sliders class="w-3 h-3 flex-shrink-0 opacity-70" />
                  <span class="font-semibold">公式：{{ formulaLabel(item.formulaConfigJson) }}</span>
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
              <td class="py-5 px-6 text-right space-x-2">
                <button
                  @click="router.push({ path: '/spc', query: { ppcId: item.id } })"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-amber-50 dark:bg-slate-800 hover:bg-amber-100 dark:hover:bg-amber-950 text-amber-600 dark:text-amber-400 transition-all border border-amber-200 dark:border-slate-700"
                  title="查看管制圖與趨勢圖"
                >
                  <Activity class="w-4 h-4" />
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
        <div class="relative w-full max-w-2xl h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-amber-600 text-white rounded-xl shadow-md shadow-amber-500/20">
              <FolderTree class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增 SPC 管制項目' : '編輯 SPC 管制項目' }}
            </h3>
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
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">工站製程 (Process) <span class="text-red-500">*</span></label>
            <select
              v-model="form.processId"
              required
              class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
            >
              <option disabled value="null">-- 選擇製程 --</option>
              <option v-for="pr in processes" :key="pr.id" :value="pr.id">{{ pr.processCode }} - {{ pr.processName }}</option>
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
                  required
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
                  required
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
                <span>產品料號 (Part) <span class="text-red-500">*</span></span>
              </label>
              <select
                v-model="form.partId"
                required
                class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
              >
                <option disabled value="null">-- 選擇料號 --</option>
                <option v-for="p in parts" :key="p.id" :value="p.id">{{ p.partNo }} - {{ p.partName }}</option>
              </select>
            </div>

            <div v-else class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">產品料號 (Part)</label>
              <div class="px-3 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold text-slate-500 dark:text-slate-400">
                {{ form.controlScope === 'CHEMICAL' ? '藥水管制不需產品料號' : '製程管制不需產品料號' }}
              </div>
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">檢驗特性 (Characteristic) <span class="text-red-500">*</span></label>
              <select
                v-model="form.characteristicId"
                @change="applyCharacteristicUnit"
                required
                class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
              >
                <option disabled value="null">-- 選擇特性 --</option>
                <option v-for="ch in characteristics" :key="ch.id" :value="ch.id">{{ ch.characteristicCode }} - {{ ch.characteristicName }}</option>
              </select>
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">量測單位 (Unit)</label>
            <input
              v-model.trim="form.unit"
              type="text"
              maxlength="50"
              placeholder="例如：mm、μm、%、mg/L"
              class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
            />
            <p class="text-[11px] text-slate-400">
              此單位只套用於目前管制項目；切換檢驗特性時會帶入特性主檔的預設單位，之後可自行修改。
            </p>
          </div>

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
                <label class="block text-[11px] font-bold text-slate-600 dark:text-slate-400">目標值 (Target)</label>
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
                <label class="block text-[11px] font-bold text-amber-600 dark:text-amber-400">子組大小 (Sample Size) <span class="text-red-500">*</span></label>
                <input
                  v-model="form.sampleSize"
                  type="number"
                  min="1"
                  max="25"
                  required
                  class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-amber-300 dark:border-amber-700 rounded-xl text-sm font-mono font-bold text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
                />
              </div>
            </div>

            <!-- Custom Control Limits -->
            <div class="pt-2 border-t border-slate-200 dark:border-slate-700">
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
            </div>
          </div>

          <!-- Chart Type -->
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">綁定 SPC 管制圖類型</label>
            <select
              v-model="form.chartTypeId"
              class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
            >
              <option :value="null">-- 無指定 (繼承特性設定) --</option>
              <option v-for="ct in chartTypes" :key="ct.id" :value="ct.id">{{ ct.chartTypeCode }} - {{ ct.chartTypeName }}</option>
            </select>
            <p class="text-[11px] text-slate-400 mt-1">
              未設定項目專屬規則時，會繼承「管制圖配置維護 > 小分類與公式配置」的規則。
              <span v-if="getChartTypeRuleGroupId(Number(form.chartTypeId))" class="font-bold text-amber-600 dark:text-amber-400">
                目前套用：{{ ruleGroupMap[getChartTypeRuleGroupId(Number(form.chartTypeId))] }}
              </span>
              <span v-else class="italic">目前未套用規則。</span>
            </p>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">公式配置</label>
            <select
              v-model="form.formulaConfigJson"
              class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
            >
              <option
                v-if="form.formulaConfigJson && !formulaOptions.some(option => option.id === form.formulaConfigJson)"
                :value="form.formulaConfigJson"
              >
                既有自訂公式
              </option>
              <option v-for="option in formulaOptions" :key="option.label" :value="option.id">{{ option.label }}</option>
            </select>
            <p class="text-[11px] text-slate-400">
              項目選擇的公式會優先於管制圖種類設定；目前公式套用於 X̄-R 管制線計算。
            </p>
          </div>

          <div class="p-4 bg-amber-50/60 dark:bg-amber-950/15 rounded-2xl border border-amber-200 dark:border-amber-900/70 space-y-3">
            <div>
              <h4 class="text-xs font-bold uppercase tracking-wider text-amber-700 dark:text-amber-300 flex items-center gap-2">
                <AlertTriangle class="w-4 h-4" /> 管制規則
              </h4>
              <p class="text-[11px] text-amber-700/70 dark:text-amber-300/70 mt-1">
                勾選後會優先套用此管制項目的專屬規則；全部不勾選時，繼承管制圖種類的規則設定。
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
                <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? '啟用 (Active)' : '停用 (Inactive)' }}</span>
              </label>
            </div>
          </div>

          </div>
          <div class="shrink-0 p-6 bg-slate-50 dark:bg-slate-800/80 border-t border-slate-200 dark:border-slate-800 flex items-center justify-end gap-3">
            <button
              @click="showModal = false"
              type="button"
              class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all"
            >
              取消
            </button>
            <button
              type="submit"
              :disabled="loading"
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-amber-600 to-orange-600 hover:from-amber-500 hover:to-orange-500 text-white text-sm font-bold shadow-lg shadow-amber-500/25 hover:shadow-xl hover:shadow-amber-500/40 transition-all transform hover:-translate-y-0.5"
            >
              <Save class="w-4 h-4" /> 確認儲存
            </button>
          </div>
        </form>
      </div>
      </div>
    </transition>
  </section>
</template>
