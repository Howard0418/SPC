<script setup>
import ModuleGuide from "../components/ModuleGuide.vue";
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Sliders,
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
  Check,
  Info
} from "lucide-vue-next";

const { embedded } = defineProps({
  embedded: { type: Boolean, default: false }
});

const rows = ref([]);
const controlGroups = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all");
const categoryFilter = ref("all"); // all, Variable, Attribute

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const form = ref({
  characteristicCode: "",
  characteristicName: "",
  controlScope: "PRODUCT",
  dataCategory: "Variable",
  unit: "",
  isSpcEnabled: true,
  isEnabled: true
});
const formErr = ref("");

function controlScopeLabel(scope) {
  const normalized = String(scope || "PRODUCT").trim().toUpperCase();
  const fixedLabels = {
    CHEMICAL: "藥液",
    PRODUCT: "產品管制",
    PROD: "產品管制",
    PROCESS: "製程管制",
    PROC: "製程管制",
    DUST: "落塵監控"
  };
  if (fixedLabels[normalized]) return fixedLabels[normalized];
  const group = controlGroups.value.find(x => String(x.groupCode).trim().toUpperCase() === normalized);
  return group?.groupName || normalized;
}

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [characteristicsRes, groupsRes] = await Promise.all([
      api.get("/characteristics"),
      api.get("/control-chart-groups")
    ]);
    rows.value = characteristicsRes.data || [];
    controlGroups.value = (groupsRes.data || []).filter(x => x.isEnabled !== false);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const matchQuery = !q || 
      (row.characteristicCode && row.characteristicCode.toLowerCase().includes(q)) ||
      (row.characteristicName && row.characteristicName.toLowerCase().includes(q)) ||
      (row.unit && row.unit.toLowerCase().includes(q));
      
    const matchStatus = statusFilter.value === "all" || 
      (statusFilter.value === "active" && row.isEnabled) ||
      (statusFilter.value === "inactive" && !row.isEnabled);

    const matchCat = categoryFilter.value === "all" || row.dataCategory === categoryFilter.value;

    return matchQuery && matchStatus && matchCat;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    characteristicCode: "",
    characteristicName: "",
    controlScope: controlGroups.value[0]?.groupCode || "PRODUCT",
    dataCategory: "Variable",
    unit: "",
    isSpcEnabled: true,
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    characteristicCode: item.characteristicCode || "",
    characteristicName: item.characteristicName || "",
    controlScope: item.controlScope || "PRODUCT",
    dataCategory: item.dataCategory || "Variable",
    unit: item.unit || "",
    isSpcEnabled: item.isSpcEnabled ?? true,
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.characteristicCode?.trim() || !form.value.characteristicName?.trim()) {
    formErr.value = "特性代號與名稱皆為必填欄位。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = { ...form.value, defaultChartTypeId: null };

    if (modalMode.value === "create") {
      const { data } = await api.post("/characteristics", payload);
      rows.value.push(data);
      successAlert("成功建立新特性：" + data.characteristicName);
    } else {
      const { data } = await api.put(`/characteristics/${currentId.value}`, payload);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert("成功更新特性：" + data.characteristicName);
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(`確定要刪除品質特性「${item.characteristicCode} (${item.characteristicName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/characteristics/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除特性：" + item.characteristicName);
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
    <div v-if="!embedded" class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-pink-600 to-rose-500 rounded-xl shadow-lg shadow-pink-500/30 text-white">
          <Sliders class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">品質檢驗特性主檔維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">定義全廠計量型與計數型檢驗特徵名稱、測量單位及預設 SPC 管制圖</p>
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
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-pink-600 to-rose-600 hover:from-pink-500 hover:to-rose-500 text-white text-sm font-bold shadow-lg shadow-pink-500/25 hover:shadow-xl hover:shadow-pink-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> 新增特性
        </button>
      </div>
    </div>

    <!-- Guide / Wizard Tip -->
    <ModuleGuide v-if="!embedded" title="模組指南：品質檢驗特性主檔">
      <p class="text-xs text-pink-700 dark:text-pink-400/80 mt-1.5 leading-relaxed">
          此模組用於建置全廠的「檢驗項目字典」(如：長度、重量、銅離子濃度等)。<br/>
          💡 <strong>資料類型說明：</strong><br/>
          - <strong>計量：</strong>可以量測出具體數值的特性（如：長度 10.5 mm）。<br/>
          - <strong>計數：</strong>以不良數、不良率或缺點數表示的特性。
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-pink-700 dark:text-pink-400/80 leading-relaxed">
          <div class="font-black text-pink-900 dark:text-pink-300">品質檢驗特性主檔頁面操作說明</div>
          <p><strong>查詢特性：</strong>輸入特性代號、名稱或單位，快速篩選檢驗項目。</p>
          <p><strong>新增特性：</strong>按「新增特性」，填入特性代號、名稱、資料類型與量測單位。</p>
          <p><strong>選擇類型：</strong>連續數值請選計量型；不良數、缺點數或比例資料請選計數型。</p>
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
    <div class="flex flex-col lg:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
      <div class="relative w-full lg:w-80">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="搜尋代號、名稱或單位..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-pink-500/50 focus:border-pink-500 transition-all"
        />
      </div>

      <div class="flex flex-wrap items-center gap-3 w-full lg:w-auto">
        <!-- Category Filter -->
        <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
          <button
            v-for="c in [{id:'all', label:'全屬性'}, {id:'Variable', label:'計量'}, {id:'Attribute', label:'計數'}]"
            :key="c.id"
            @click="categoryFilter = c.id"
            type="button"
            :class="[
              'px-3 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap',
              categoryFilter === c.id
                ? 'bg-white dark:bg-slate-700 text-pink-600 dark:text-pink-400 shadow-sm'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
            ]"
          >
            {{ c.label }}
          </button>
        </div>

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
                ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm'
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
              <th class="py-4 px-6">所屬管制類型</th>
              <th class="py-4 px-6">特性編號 / 名稱</th>
              <th class="py-4 px-6">資料類型</th>
              <th class="py-4 px-6">單位</th>
              <th class="py-4 px-6 text-center">SPC 運算</th>
              <th class="py-4 px-6 text-center">啟用狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="8" class="py-12 text-center text-slate-400">正在載入品質特性清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="8" class="py-12 text-center text-slate-400">找不到相符的品質特性資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-pink-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-6 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-6 px-6">
                <span class="inline-flex px-2.5 py-1 rounded-lg text-xs font-bold border bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/40 dark:text-purple-300 dark:border-purple-800">
                  {{ controlScopeLabel(item.controlScope) }}
                </span>
              </td>
              <td class="py-6 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2 text-base">
                  <Sliders class="w-4 h-4 text-pink-500" /> {{ item.characteristicCode }}
                </div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ item.characteristicName }}</div>
              </td>
              <td class="py-6 px-6">
                <span :class="[
                  'px-2.5 py-1 rounded-lg text-xs font-bold border tracking-wide',
                  item.dataCategory === 'Variable' 
                    ? 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-950/60 dark:text-blue-300 dark:border-blue-800'
                    : 'bg-amber-50 text-amber-600 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800'
                ]">
                  {{ item.dataCategory === 'Variable' ? '計量' : '計數' }}
                </span>
              </td>
              <td class="py-6 px-6 font-semibold text-xs text-slate-600 dark:text-slate-300">
                {{ item.unit || '無 (N/A)' }}
              </td>
              <td class="py-6 px-6 text-center">
                <span :class="['inline-flex items-center justify-center w-7 h-7 rounded-lg border font-bold text-xs', item.isSpcEnabled ? 'bg-indigo-50 border-indigo-300 text-indigo-600 dark:bg-indigo-950/60 dark:border-indigo-800 dark:text-indigo-400' : 'bg-slate-100 border-slate-300 text-slate-400 dark:bg-slate-800 dark:border-slate-700']">
                  <Check v-if="item.isSpcEnabled" class="w-4 h-4" />
                  <span v-else>-</span>
                </span>
              </td>
              <td class="py-6 px-6 text-center">
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
              <td class="py-6 px-6 text-right space-x-2">
                <button
                  @click="openEditModal(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  title="編輯特性"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除特性"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>顯示第 1 至 {{ filteredRows.length }} 項結果（總計 {{ rows.length }} 筆特性）</span>
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
        <div class="relative w-full max-w-md h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-pink-600 text-white rounded-xl shadow-md shadow-pink-500/20">
              <Sliders class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增品質特性' : '編輯品質特性' }}
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

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">所屬管制類型 <span class="text-red-500">*</span></label>
            <select
              v-model="form.controlScope"
              required
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-pink-500 transition-all"
            >
              <option v-for="group in controlGroups" :key="group.id" :value="group.groupCode">
                {{ controlScopeLabel(group.groupCode) }}
              </option>
            </select>
            <p class="text-[11px] text-slate-400">SPC 管制項目會依此欄位分流；例如藥液不會顯示落塵監控特性。</p>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">特性代號 <span class="text-red-500">*</span></label>
              <input
                v-model="form.characteristicCode"
                type="text"
                required
                placeholder="例如：C-101 / CHO"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-pink-500 focus:border-transparent transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">特性名稱 <span class="text-red-500">*</span></label>
              <input
                v-model="form.characteristicName"
                type="text"
                required
                placeholder="例如：銅離子濃度"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-pink-500 focus:border-transparent transition-all"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">資料類型</label>
              <select
                v-model="form.dataCategory"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-pink-500 transition-all"
              >
                <option value="Variable">計量型數值</option>
                <option value="Attribute">計數型資料</option>
              </select>
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">測量單位</label>
              <input
                v-model="form.unit"
                type="text"
                placeholder="例如：g/L, mm, kg, %"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-pink-500 transition-all"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 pt-2">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">SPC 即時運算引擎</label>
              <label class="relative inline-flex items-center cursor-pointer">
                <input v-model="form.isSpcEnabled" type="checkbox" class="sr-only peer" />
                <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-pink-300 dark:peer-focus:ring-pink-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
                <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isSpcEnabled ? '開啟算圖' : '不執行算圖' }}</span>
              </label>
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">啟用設定</label>
              <label class="relative inline-flex items-center cursor-pointer">
                <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-pink-300 dark:peer-focus:ring-pink-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-pink-600"></div>
                <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? '啟用' : '停用' }}</span>
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
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-pink-600 to-rose-600 hover:from-pink-500 hover:to-rose-500 text-white text-sm font-bold shadow-lg shadow-pink-500/25 hover:shadow-xl hover:shadow-pink-500/40 transition-all transform hover:-translate-y-0.5"
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
