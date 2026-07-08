<script setup>
import ModuleGuide from "../components/ModuleGuide.vue";
import { onMounted, ref, computed } from "vue";
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
  Info
} from "lucide-vue-next";

const rows = ref([]);
const chartGroups = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all");
const groupFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const form = ref({
  chartGroupId: null,
  categoryCode: "",
  categoryName: "",
  description: "",
  isEnabled: true
});
const formErr = ref("");

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [resCats, resGroups] = await Promise.all([
      api.get("/control-chart-categories"),
      api.get("/control-chart-groups")
    ]);
    rows.value = resCats.data || [];
    chartGroups.value = resGroups.data || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const groupMap = computed(() => {
  const map = {};
  chartGroups.value.forEach(g => {
    map[g.id] = `${g.groupCode} (${g.groupName})`;
  });
  return map;
});

const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const gName = row.chartGroupId ? (groupMap.value[row.chartGroupId] || "") : "";
    const matchQuery = !q || 
      (row.categoryCode && row.categoryCode.toLowerCase().includes(q)) ||
      (row.categoryName && row.categoryName.toLowerCase().includes(q)) ||
      (row.description && row.description.toLowerCase().includes(q)) ||
      gName.toLowerCase().includes(q);
      
    const matchStatus = statusFilter.value === "all" ||
      (statusFilter.value === "active" && row.isEnabled) ||
      (statusFilter.value === "inactive" && !row.isEnabled);

    const matchGroup = groupFilter.value === "all" || row.chartGroupId === parseInt(groupFilter.value);

    return matchQuery && matchStatus && matchGroup;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    chartGroupId: chartGroups.value.length > 0 ? chartGroups.value[0].id : null,
    categoryCode: "",
    categoryName: "",
    description: "",
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    chartGroupId: item.chartGroupId || (chartGroups.value.length > 0 ? chartGroups.value[0].id : null),
    categoryCode: item.categoryCode || "",
    categoryName: item.categoryName || "",
    description: item.description || "",
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.categoryCode?.trim() || !form.value.categoryName?.trim() || !form.value.chartGroupId) {
    formErr.value = "所屬群組、類別代號與名稱皆為必填欄位。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = {
      ...form.value,
      chartGroupId: parseInt(form.value.chartGroupId)
    };
    if (modalMode.value === "create") {
      const { data } = await api.post("/control-chart-categories", payload);
      rows.value.push(data);
      successAlert("成功建立新管制圖類別：" + data.categoryName);
    } else {
      const { data } = await api.put(`/control-chart-categories/${currentId.value}`, payload);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert("成功更新類別：" + data.categoryName);
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(`確定要刪除管制圖類別「${item.categoryCode} (${item.categoryName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/control-chart-categories/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除類別：" + item.categoryName);
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

const procGroupId = computed(() => chartGroups.value.find(g => g.groupCode === "PROC")?.id || 1);
const chemGroupId = computed(() => chartGroups.value.find(g => g.groupCode === "CHEM")?.id || 2);
const prodGroupId = computed(() => chartGroups.value.find(g => g.groupCode === "PROD")?.id || 3);

onMounted(load);
</script>

<template>
  <section class="space-y-6">
    <!-- Title & Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-cyan-600 to-blue-600 rounded-xl shadow-lg shadow-cyan-500/30 text-white">
          <FolderTree class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">管制圖類別主檔維護 (中分類)</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">管理歸屬於各大群組下之計量或計數型次分類項目</p>
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
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-cyan-600 to-blue-600 hover:from-cyan-500 hover:to-blue-500 text-white text-sm font-bold shadow-lg shadow-cyan-500/25 hover:shadow-xl hover:shadow-cyan-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> 新增類別
        </button>
      </div>
    </div>

    <!-- Guide / Wizard Tip -->
    <ModuleGuide title="模組指南：管制圖類別主檔 (Control Chart Categories)">
      <p class="text-xs text-cyan-700 dark:text-cyan-400/80 mt-1.5 leading-relaxed">
          此模組用於設定 SPC 系統的「中分類」資料，歸屬於「大群組」之下。
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-cyan-700 dark:text-cyan-400/80 leading-relaxed">
          <div class="font-black text-cyan-900 dark:text-cyan-300">管制圖類別主檔頁面操作說明</div>
          <p><strong>選擇群組：</strong>先確認類別要歸屬的管制圖大群組。</p>
          <p><strong>新增類別：</strong>按「新增類別」，填入類別代號、類別名稱與排序。</p>
          <p><strong>切換檢視：</strong>可透過畫面上的分類卡或篩選條件查看不同大群組下的類別。</p>
          <p><strong>後續設定：</strong>類別建立後，請到「管制圖種類配置」設定實際管制圖種類。</p>
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

    <!-- 🌟 新增：高質感品質維度互動結構柱 (Visual Quality Dimension Cards) -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <!-- 1. 製程管制項目 PROC -->
      <div 
        @click="groupFilter = groupFilter === procGroupId ? 'all' : procGroupId"
        :class="[
          'relative p-6 rounded-3xl border cursor-pointer transition-all duration-300 transform hover:-translate-y-1 overflow-hidden group shadow-md',
          groupFilter === procGroupId 
            ? 'bg-gradient-to-br from-blue-900 via-indigo-900 to-slate-900 border-blue-500 shadow-blue-500/20 text-white' 
            : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-100 hover:border-blue-400'
        ]"
      >
        <div class="absolute -right-6 -bottom-6 w-32 h-32 bg-blue-500/10 rounded-full blur-2xl group-hover:bg-blue-500/20 transition-all pointer-events-none"></div>
        <div class="flex items-start justify-between">
          <div class="p-3 rounded-2xl bg-blue-500/10 border border-blue-500/20 text-blue-500">
            <Layers class="w-6 h-6" />
          </div>
          <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-blue-500/15 text-blue-500 border border-blue-500/25">XBAR-S / XBAR-R</span>
        </div>
        <h3 class="text-lg font-black mt-4">製程管制項目 (PROC)</h3>
        <p class="text-xs text-slate-400 mt-1 leading-relaxed">
          針對產線即時製程參數（如咬蝕量、溫度、壓力）進行高頻監控，防止製程失控。
        </p>
        <div class="mt-6 flex items-center justify-between">
          <span class="text-xs font-semibold text-slate-400">目前類別</span>
          <span class="text-2xl font-black text-blue-500">{{ rows.filter(x => x.chartGroupId === procGroupId).length }} 筆</span>
        </div>
        <!-- Active indicator -->
        <div v-if="groupFilter === procGroupId" class="absolute bottom-0 inset-x-0 h-1 bg-blue-500"></div>
      </div>

      <!-- 2. 藥液管制項目 CHEM -->
      <div 
        @click="groupFilter = groupFilter === chemGroupId ? 'all' : chemGroupId"
        :class="[
          'relative p-6 rounded-3xl border cursor-pointer transition-all duration-300 transform hover:-translate-y-1 overflow-hidden group shadow-md',
          groupFilter === chemGroupId 
            ? 'bg-gradient-to-br from-teal-900 via-emerald-900 to-slate-900 border-teal-500 shadow-teal-500/20 text-white' 
            : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-100 hover:border-teal-400'
        ]"
      >
        <div class="absolute -right-6 -bottom-6 w-32 h-32 bg-teal-500/10 rounded-full blur-2xl group-hover:bg-teal-500/20 transition-all pointer-events-none"></div>
        <div class="flex items-start justify-between">
          <div class="p-3 rounded-2xl bg-teal-500/10 border border-teal-500/20 text-teal-500">
            <RefreshCw class="w-6 h-6" />
          </div>
          <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-teal-500/15 text-teal-500 border border-teal-500/25">IX-MR</span>
        </div>
        <h3 class="text-lg font-black mt-4">藥液管制項目 (CHEM)</h3>
        <p class="text-xs text-slate-400 mt-1 leading-relaxed">
          監控化驗室槽液分析濃度趨勢（如酸鹼度、金屬離子濃度），採低頻率精準分析。
        </p>
        <div class="mt-6 flex items-center justify-between">
          <span class="text-xs font-semibold text-slate-400">目前類別</span>
          <span class="text-2xl font-black text-teal-500">{{ rows.filter(x => x.chartGroupId === chemGroupId).length }} 筆</span>
        </div>
        <div v-if="groupFilter === chemGroupId" class="absolute bottom-0 inset-x-0 h-1 bg-teal-500"></div>
      </div>

      <!-- 3. 產品管制項目 PROD -->
      <div 
        @click="groupFilter = groupFilter === prodGroupId ? 'all' : prodGroupId"
        :class="[
          'relative p-6 rounded-3xl border cursor-pointer transition-all duration-300 transform hover:-translate-y-1 overflow-hidden group shadow-md',
          groupFilter === prodGroupId 
            ? 'bg-gradient-to-br from-purple-900 via-fuchsia-900 to-slate-900 border-purple-500 shadow-purple-500/20 text-white' 
            : 'bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800 text-slate-800 dark:text-slate-100 hover:border-purple-400'
        ]"
      >
        <div class="absolute -right-6 -bottom-6 w-32 h-32 bg-purple-500/10 rounded-full blur-2xl group-hover:bg-purple-500/20 transition-all pointer-events-none"></div>
        <div class="flex items-start justify-between">
          <div class="p-3 rounded-2xl bg-purple-500/10 border border-purple-500/20 text-purple-500">
            <FolderTree class="w-6 h-6" />
          </div>
          <span class="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-purple-500/15 text-purple-500 border border-purple-500/25">規格綁定</span>
        </div>
        <h3 class="text-lg font-black mt-4">產品管制項目 (PROD)</h3>
        <p class="text-xs text-slate-400 mt-1 leading-relaxed">
          針對實體料號在特定工站的出貨檢驗（如外觀、成品尺寸），直接與工單 Lot 連結。
        </p>
        <div class="mt-6 flex items-center justify-between">
          <span class="text-xs font-semibold text-slate-400">目前類別</span>
          <span class="text-2xl font-black text-purple-500">{{ rows.filter(x => x.chartGroupId === prodGroupId).length }} 筆</span>
        </div>
        <div v-if="groupFilter === prodGroupId" class="absolute bottom-0 inset-x-0 h-1 bg-purple-500"></div>
      </div>
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
          placeholder="搜尋類別代號、名稱或描述..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-cyan-500/50 focus:border-cyan-500 transition-all"
        />
      </div>

      <div class="flex flex-wrap items-center gap-3 w-full lg:w-auto">
        <!-- Group Filter Dropdown -->
        <select
          v-model="groupFilter"
          class="px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-cyan-500 transition-all"
        >
          <option value="all">所有大群組</option>
          <option v-for="g in chartGroups" :key="g.id" :value="g.id">
            {{ g.groupCode }} - {{ g.groupName }}
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
                ? 'bg-white dark:bg-slate-700 text-cyan-600 dark:text-cyan-400 shadow-sm'
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
              <th class="py-4 px-6">所屬大群組</th>
              <th class="py-4 px-6">類別代號 / 名稱</th>
              <th class="py-4 px-6">說明描述</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="6" class="py-12 text-center text-slate-400">正在載入類別清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="6" class="py-12 text-center text-slate-400">找不到相符的類別資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-cyan-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-4 px-6">
                <span v-if="item.chartGroupId" class="text-xs font-bold text-indigo-700 dark:text-indigo-300 bg-indigo-50 dark:bg-indigo-950/60 border border-indigo-200 dark:border-indigo-800 px-2.5 py-1 rounded-lg">
                  {{ groupMap[item.chartGroupId] || `群組 #${item.chartGroupId}` }}
                </span>
                <span v-else class="text-slate-400 text-xs">-</span>
              </td>
              <td class="py-4 px-6 font-bold text-slate-900 dark:text-white">
                <div class="flex items-center gap-2">
                  <FolderTree class="w-4 h-4 text-cyan-500" /> {{ item.categoryCode }}
                </div>
                <div class="text-xs text-slate-500 font-normal mt-0.5">{{ item.categoryName }}</div>
              </td>
              <td class="py-4 px-6 text-slate-600 dark:text-slate-300">
                {{ item.description || '-' }}
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
                  title="編輯類別"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除類別"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>顯示第 1 至 {{ filteredRows.length }} 項結果（總計 {{ rows.length }} 筆類別）</span>
      </div>
    </div>

    <!-- Modal Dialog (Add / Edit) -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in">
      <div class="bg-white dark:bg-slate-900 w-full max-w-lg rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden transform transition-all">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-cyan-600 text-white rounded-xl shadow-md shadow-cyan-500/20">
              <FolderTree class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增管制圖類別' : '編輯管制圖類別' }}
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

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">所屬大群組 (Chart Group) <span class="text-red-500">*</span></label>
            <select
              v-model="form.chartGroupId"
              required
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-cyan-500 transition-all"
            >
              <option disabled value="null">-- 請選擇所屬群組 --</option>
              <option v-for="g in chartGroups" :key="g.id" :value="g.id">
                {{ g.groupCode }} - {{ g.groupName }}
              </option>
            </select>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">類別代號 (Code) <span class="text-red-500">*</span></label>
              <input
                v-model="form.categoryCode"
                type="text"
                required
                placeholder="例如：CAT-100 / VARIABLE"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-cyan-500 focus:border-transparent transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">類別名稱 (Name) <span class="text-red-500">*</span></label>
              <input
                v-model="form.categoryName"
                type="text"
                required
                placeholder="例如：計量型管制類別"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-cyan-500 focus:border-transparent transition-all"
              />
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">類別說明描述 (Description)</label>
            <textarea
              v-model="form.description"
              rows="3"
              placeholder="請輸入類別定義或歸屬項目指引..."
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-cyan-500 focus:border-transparent transition-all resize-none"
            ></textarea>
          </div>

          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">啟用狀態</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-cyan-300 dark:peer-focus:ring-cyan-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-cyan-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? '啟用 (Active)' : '停用 (Inactive)' }}</span>
            </label>
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
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-cyan-600 to-blue-600 hover:from-cyan-500 hover:to-blue-500 text-white text-sm font-bold shadow-lg shadow-cyan-500/25 hover:shadow-xl hover:shadow-cyan-500/40 transition-all transform hover:-translate-y-0.5"
            >
              <Save class="w-4 h-4" /> 確認儲存
            </button>
          </div>
        </form>
      </div>
    </div>
  </section>
</template>
