<script setup>
import { onMounted, ref, computed } from "vue";
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
  Activity
} from "lucide-vue-next";

const rows = ref([]);
const parts = ref([]);
const processes = ref([]);
const characteristics = ref([]);
const chartTypes = ref([]);
const ruleGroups = ref([]);

const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all");
const processFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);

const form = ref({
  partId: null,
  processId: null,
  characteristicId: null,
  usl: null,
  lsl: null,
  ucl: null,
  cl: null,
  lcl: null,
  targetValue: null,
  sampleSize: 5,
  chartTypeId: null,
  ruleGroupId: null,
  isRequired: true,
  isEnabled: true
});
const formErr = ref("");

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [resMain, resParts, resProc, resChar, resTypes, resRules] = await Promise.all([
      api.get("/part-process-characteristics"),
      api.get("/parts"),
      api.get("/processes"),
      api.get("/characteristics"),
      api.get("/control-chart-types"),
      api.get("/control-chart-groups")
    ]);
    rows.value = resMain.data || [];
    parts.value = resParts.data || [];
    processes.value = resProc.data || [];
    characteristics.value = resChar.data || [];
    chartTypes.value = resTypes.data || [];
    ruleGroups.value = resRules.data || [];
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
  ruleGroups.value.forEach(rg => { m[rg.id] = `${rg.groupCode} (${rg.groupName})`; });
  return m;
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

    const matchQuery = !q || 
      pNo.toLowerCase().includes(q) || pName.toLowerCase().includes(q) ||
      prCode.toLowerCase().includes(q) || prName.toLowerCase().includes(q) ||
      chCode.toLowerCase().includes(q) || chName.toLowerCase().includes(q);

    const matchStatus = statusFilter.value === "all" ||
      (statusFilter.value === "active" && row.isEnabled) ||
      (statusFilter.value === "inactive" && !row.isEnabled);

    const matchProc = processFilter.value === "all" || row.processId === parseInt(processFilter.value);

    return matchQuery && matchStatus && matchProc;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    partId: parts.value.length > 0 ? parts.value[0].id : null,
    processId: processes.value.length > 0 ? processes.value[0].id : null,
    characteristicId: characteristics.value.length > 0 ? characteristics.value[0].id : null,
    usl: null, lsl: null, ucl: null, cl: null, lcl: null, targetValue: null,
    sampleSize: 5,
    chartTypeId: chartTypes.value.length > 0 ? chartTypes.value[0].id : null,
    ruleGroupId: ruleGroups.value.length > 0 ? ruleGroups.value[0].id : null,
    isRequired: true,
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    partId: item.partId || null,
    processId: item.processId || null,
    characteristicId: item.characteristicId || null,
    usl: item.usl ?? null,
    lsl: item.lsl ?? null,
    ucl: item.ucl ?? null,
    cl: item.cl ?? null,
    lcl: item.lcl ?? null,
    targetValue: item.targetValue ?? null,
    sampleSize: item.sampleSize ?? 5,
    chartTypeId: item.chartTypeId || null,
    ruleGroupId: item.ruleGroupId || null,
    isRequired: item.isRequired ?? true,
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.partId || !form.value.processId || !form.value.characteristicId) {
    formErr.value = "產品料號、工站製程與檢驗特性皆為必填項目。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = {
      ...form.value,
      partId: parseInt(form.value.partId),
      processId: parseInt(form.value.processId),
      characteristicId: parseInt(form.value.characteristicId),
      usl: form.value.usl !== "" && form.value.usl !== null ? parseFloat(form.value.usl) : null,
      lsl: form.value.lsl !== "" && form.value.lsl !== null ? parseFloat(form.value.lsl) : null,
      ucl: form.value.ucl !== "" && form.value.ucl !== null ? parseFloat(form.value.ucl) : null,
      cl: form.value.cl !== "" && form.value.cl !== null ? parseFloat(form.value.cl) : null,
      lcl: form.value.lcl !== "" && form.value.lcl !== null ? parseFloat(form.value.lcl) : null,
      targetValue: form.value.targetValue !== "" && form.value.targetValue !== null ? parseFloat(form.value.targetValue) : null,
      sampleSize: parseInt(form.value.sampleSize) || 1,
      chartTypeId: form.value.chartTypeId ? parseInt(form.value.chartTypeId) : null,
      ruleGroupId: form.value.ruleGroupId ? parseInt(form.value.ruleGroupId) : null
    };

    if (modalMode.value === "create") {
      await api.post("/part-process-characteristics", payload);
      successAlert("成功建立新檢驗基準");
    } else {
      await api.put(`/part-process-characteristics/${currentId.value}`, payload);
      successAlert("成功更新檢驗基準");
    }
    showModal.value = false;
    await load();
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  const pNo = item.part?.partNo || item.partId;
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

onMounted(load);
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
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">料號檢驗基準設定與規格維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">關聯產品料號、工站製程與檢驗項目，設置 USL/LSL 規格界限與子組樣本數</p>
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
          <Plus class="w-4 h-4" /> 新增檢驗基準
        </button>
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
              <th class="py-4 px-6">ID</th>
              <th class="py-4 px-6">產品料號</th>
              <th class="py-4 px-6">工站製程</th>
              <th class="py-4 px-6">檢驗特性 (單位)</th>
              <th class="py-4 px-6 text-center">規格界限 (LSL ~ USL)</th>
              <th class="py-4 px-6 text-center">抽樣(N)</th>
              <th class="py-4 px-6">套用管制圖 / 判定規則</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="9" class="py-12 text-center text-slate-400">正在載入檢驗基準清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="9" class="py-12 text-center text-slate-400">找不到相符的檢驗基準資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-amber-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-4 px-6 font-bold text-slate-900 dark:text-white">
                <div class="flex items-center gap-1.5">
                  <Package class="w-4 h-4 text-blue-500 flex-shrink-0" />
                  <span>{{ item.part?.partNo || `Part #${item.partId}` }}</span>
                </div>
                <div class="text-xs text-slate-500 font-normal mt-0.5">{{ item.part?.partName }}</div>
              </td>
              <td class="py-4 px-6 font-semibold text-slate-800 dark:text-slate-200 text-xs">
                <div class="flex items-center gap-1.5">
                  <Layers class="w-3.5 h-3.5 text-cyan-500 flex-shrink-0" />
                  <span>{{ item.process?.processCode || `Proc #${item.processId}` }}</span>
                </div>
                <div class="text-xs text-slate-500 font-normal mt-0.5">{{ item.process?.processName }}</div>
              </td>
              <td class="py-4 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-1.5">
                  <Sliders class="w-3.5 h-3.5 text-pink-500 flex-shrink-0" />
                  <span>{{ item.characteristic?.characteristicName || `Char #${item.characteristicId}` }}</span>
                </div>
                <div class="text-xs text-slate-500 font-mono mt-0.5">代號: {{ item.characteristic?.characteristicCode }} ({{ item.characteristic?.unit || '無單位' }})</div>
              </td>
              <td class="py-4 px-6 text-center font-mono text-xs font-bold">
                <span class="px-2.5 py-1 bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 rounded-lg border border-slate-200 dark:border-slate-700">
                  {{ item.lsl !== null ? item.lsl : '-∞' }} ~ {{ item.usl !== null ? item.usl : '+∞' }}
                </span>
              </td>
              <td class="py-4 px-6 text-center font-mono font-bold text-amber-600 dark:text-amber-400 text-xs">
                N = {{ item.sampleSize || 1 }}
              </td>
              <td class="py-4 px-6 text-xs space-y-1">
                <div v-if="item.chartTypeId" class="font-bold text-blue-600 dark:text-blue-400 flex items-center gap-1">
                  <Activity class="w-3.5 h-3.5 flex-shrink-0" /> {{ chartTypeMap[item.chartTypeId] || `Chart #${item.chartTypeId}` }}
                </div>
                <div v-if="item.ruleGroupId" class="text-slate-500 dark:text-slate-400">
                  規則組: {{ ruleGroupMap[item.ruleGroupId] || `Rule #${item.ruleGroupId}` }}
                </div>
              </td>
              <td class="py-4 px-6 text-center">
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
              <td class="py-4 px-6 text-right space-x-2">
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

    <!-- Modal Dialog (Add / Edit) -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in overflow-y-auto">
      <div class="bg-white dark:bg-slate-900 w-full max-w-2xl rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden transform transition-all my-8">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-amber-600 text-white rounded-xl shadow-md shadow-amber-500/20">
              <FolderTree class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增料號檢驗基準設定' : '編輯料號檢驗基準設定' }}
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

        <form @submit.prevent="save" class="p-6 space-y-5">
          <div v-if="formErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
            <XCircle class="w-4 h-4 flex-shrink-0" /> {{ formErr }}
          </div>

          <!-- Product, Process, Characteristic -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">產品料號 (Part) <span class="text-red-500">*</span></label>
              <select
                v-model="form.partId"
                required
                class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
              >
                <option disabled value="null">-- 選擇料號 --</option>
                <option v-for="p in parts" :key="p.id" :value="p.id">{{ p.partNo }} - {{ p.partName }}</option>
              </select>
            </div>
            
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

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">檢驗特性 (Characteristic) <span class="text-red-500">*</span></label>
              <select
                v-model="form.characteristicId"
                required
                class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 transition-all"
              >
                <option disabled value="null">-- 選擇特性 --</option>
                <option v-for="ch in characteristics" :key="ch.id" :value="ch.id">{{ ch.characteristicCode }} - {{ ch.characteristicName }}</option>
              </select>
            </div>
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

          <!-- Chart Type & Rules -->
          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">綁定 SPC 管制圖類型</label>
              <select
                v-model="form.chartTypeId"
                class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
              >
                <option :value="null">-- 無指定 (繼承特性設定) --</option>
                <option v-for="ct in chartTypes" :key="ct.id" :value="ct.id">{{ ct.chartTypeCode }} - {{ ct.chartTypeName }}</option>
              </select>
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">綁定異常檢驗規則組</label>
              <select
                v-model="form.ruleGroupId"
                class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:ring-2 focus:ring-amber-500 transition-all"
              >
                <option :value="null">-- 無指定 (全廠預設 Western Electric 規則) --</option>
                <option v-for="rg in ruleGroups" :key="rg.id" :value="rg.id">{{ rg.groupCode }} - {{ rg.groupName }}</option>
              </select>
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
                <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? '啟用 (Active)' : '停用 (Inactive)' }}</span>
              </label>
            </div>
          </div>

          <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
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
  </section>
</template>
