<script setup>
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Activity,
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
  Code2
} from "lucide-vue-next";

const rows = ref([]);
const chartCategories = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all");
const categoryFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const form = ref({
  chartCategoryId: null,
  chartTypeCode: "",
  chartTypeName: "",
  dataCategory: "Variable",
  requiredSampleSize: 5,
  description: "",
  formulaConfigJson: "{}",
  isEnabled: true
});
const formErr = ref("");

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [resTypes, resCats] = await Promise.all([
      api.get("/control-chart-types"),
      api.get("/control-chart-categories")
    ]);
    rows.value = resTypes.data || [];
    chartCategories.value = resCats.data || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const categoryMap = computed(() => {
  const map = {};
  chartCategories.value.forEach(c => {
    map[c.id] = `${c.categoryCode} (${c.categoryName})`;
  });
  return map;
});

const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const cName = row.chartCategoryId ? (categoryMap.value[row.chartCategoryId] || "") : "";
    const matchQuery = !q || 
      (row.chartTypeCode && row.chartTypeCode.toLowerCase().includes(q)) ||
      (row.chartTypeName && row.chartTypeName.toLowerCase().includes(q)) ||
      (row.description && row.description.toLowerCase().includes(q)) ||
      cName.toLowerCase().includes(q);
      
    const matchStatus = statusFilter.value === "all" ||
      (statusFilter.value === "active" && row.isEnabled) ||
      (statusFilter.value === "inactive" && !row.isEnabled);

    const matchCat = categoryFilter.value === "all" || row.chartCategoryId === parseInt(categoryFilter.value);

    return matchQuery && matchStatus && matchCat;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    chartCategoryId: chartCategories.value.length > 0 ? chartCategories.value[0].id : null,
    chartTypeCode: "",
    chartTypeName: "",
    dataCategory: "Variable",
    requiredSampleSize: 5,
    description: "",
    formulaConfigJson: "{\n  \"UclFormula\": \"Xbar + A2 * Rbar\",\n  \"LclFormula\": \"Xbar - A2 * Rbar\"\n}",
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    chartCategoryId: item.chartCategoryId || (chartCategories.value.length > 0 ? chartCategories.value[0].id : null),
    chartTypeCode: item.chartTypeCode || "",
    chartTypeName: item.chartTypeName || "",
    dataCategory: item.dataCategory || "Variable",
    requiredSampleSize: item.requiredSampleSize || 5,
    description: item.description || "",
    formulaConfigJson: item.formulaConfigJson || "{}",
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

function setStandardFormula() {
  form.value.formulaConfigJson = JSON.stringify({
    XbarCalculationMethod: "STANDARD_RBAR",
    UclFormula: "XDoubleBar + (A2 * Rbar)",
    LclFormula: "XDoubleBar - (A2 * Rbar)"
  }, null, 2);
}

function setMrMethodFormula() {
  form.value.formulaConfigJson = JSON.stringify({
    XbarCalculationMethod: "MOVING_RANGE_OF_XBAR",
    MrMultiplier: 2.66,
    UclFormula: "XDoubleBar + (2.66 * MRbar_Xbar)",
    LclFormula: "XDoubleBar - (2.66 * MRbar_Xbar)"
  }, null, 2);
}

function setSigmaMethodFormula() {
  form.value.formulaConfigJson = JSON.stringify({
    XbarCalculationMethod: "SIGMA_METHOD",
    Multiplier: 3.0,
    UclFormula: "XDoubleBar + (3.0 * S_Xbar)",
    LclFormula: "XDoubleBar - (3.0 * S_Xbar)"
  }, null, 2);
}

async function save() {
  if (!form.value.chartTypeCode?.trim() || !form.value.chartTypeName?.trim() || !form.value.chartCategoryId) {
    formErr.value = "所屬類別、管制圖代號與名稱皆為必填欄位。";
    return;
  }
  try {
    JSON.parse(form.value.formulaConfigJson || "{}");
  } catch(e) {
    formErr.value = "公式設定 JSON 格式無效，請檢查語法。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = {
      ...form.value,
      chartCategoryId: parseInt(form.value.chartCategoryId),
      requiredSampleSize: parseInt(form.value.requiredSampleSize) || 1
    };
    if (modalMode.value === "create") {
      const { data } = await api.post("/control-chart-types", payload);
      rows.value.push(data);
      successAlert("成功建立新管制圖種類：" + data.chartTypeName);
    } else {
      const { data } = await api.put(`/control-chart-types/${currentId.value}`, payload);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert("成功更新管制圖種類：" + data.chartTypeName);
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(`確定要刪除管制圖種類「${item.chartTypeCode} (${item.chartTypeName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/control-chart-types/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除種類：" + item.chartTypeName);
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
        <div class="p-3 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-xl shadow-lg shadow-indigo-500/30 text-white">
          <Activity class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">SPC 管制圖種類配置與公式設定 (小分類)</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">配置具體六大管制圖 (Xbar-R, I-MR, P, U, C, NP) 之計算公式與抽樣規範</p>
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
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 hover:shadow-xl hover:shadow-indigo-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> 新增管制圖種類
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
          placeholder="搜尋代號、名稱或描述..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all"
        />
      </div>

      <div class="flex flex-wrap items-center gap-3 w-full lg:w-auto">
        <!-- Category Filter Dropdown -->
        <select
          v-model="categoryFilter"
          class="px-3 py-2 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs font-bold text-slate-700 dark:text-slate-300 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
        >
          <option value="all">所有所屬類別</option>
          <option v-for="c in chartCategories" :key="c.id" :value="c.id">
            {{ c.categoryCode }} - {{ c.categoryName }}
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
              <th class="py-4 px-6">所屬中分類</th>
              <th class="py-4 px-6">圖別代號 / 名稱</th>
              <th class="py-4 px-6">屬性分類</th>
              <th class="py-4 px-6 text-center">抽樣數</th>
              <th class="py-4 px-6">公式設定概要</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="8" class="py-12 text-center text-slate-400">正在載入管制圖種類清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="8" class="py-12 text-center text-slate-400">找不到相符的管制圖種類資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-blue-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-4 px-6">
                <span v-if="item.chartCategoryId" class="text-xs font-bold text-cyan-700 dark:text-cyan-300 bg-cyan-50 dark:bg-cyan-950/60 border border-cyan-200 dark:border-cyan-800 px-2.5 py-1 rounded-lg">
                  {{ categoryMap[item.chartCategoryId] || `類別 #${item.chartCategoryId}` }}
                </span>
                <span v-else class="text-slate-400 text-xs">-</span>
              </td>
              <td class="py-4 px-6 font-bold text-slate-900 dark:text-white">
                <div class="flex items-center gap-2 text-indigo-600 dark:text-indigo-400 font-mono">
                  <Activity class="w-4 h-4 text-blue-500 flex-shrink-0" /> {{ item.chartTypeCode }}
                </div>
                <div class="text-xs text-slate-500 font-sans font-semibold mt-0.5">{{ item.chartTypeName }}</div>
              </td>
              <td class="py-4 px-6">
                <span :class="[
                  'px-2.5 py-1 rounded-lg text-xs font-bold border tracking-wide',
                  item.dataCategory === 'Variable' 
                    ? 'bg-blue-50 text-blue-600 border-blue-200 dark:bg-blue-950/60 dark:text-blue-300 dark:border-blue-800'
                    : 'bg-amber-50 text-amber-600 border-amber-200 dark:bg-amber-950/60 dark:text-amber-300 dark:border-amber-800'
                ]">
                  {{ item.dataCategory === 'Variable' ? '計量 (Variable)' : '計數 (Attribute)' }}
                </span>
              </td>
              <td class="py-4 px-6 text-center font-mono font-bold text-xs text-slate-600 dark:text-slate-300">
                {{ item.requiredSampleSize }}
              </td>
              <td class="py-4 px-6 font-mono text-[11px] text-slate-500 dark:text-slate-400 max-w-xs truncate" :title="item.formulaConfigJson">
                {{ item.formulaConfigJson || '{}' }}
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
                  title="編輯種類"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除種類"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>顯示第 1 至 {{ filteredRows.length }} 項結果（總計 {{ rows.length }} 筆種類）</span>
      </div>
    </div>

    <!-- Modal Dialog (Add / Edit) -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm animate-fade-in overflow-y-auto">
      <div class="bg-white dark:bg-slate-900 w-full max-w-xl rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden transform transition-all my-8">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-blue-600 text-white rounded-xl shadow-md shadow-blue-500/20">
              <Activity class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增管制圖種類' : '編輯管制圖種類' }}
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
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">所屬管制圖中分類 (Category) <span class="text-red-500">*</span></label>
            <select
              v-model="form.chartCategoryId"
              required
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
            >
              <option disabled value="null">-- 請選擇所屬類別 --</option>
              <option v-for="c in chartCategories" :key="c.id" :value="c.id">
                {{ c.categoryCode }} - {{ c.categoryName }}
              </option>
            </select>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">種類代號 (Code) <span class="text-red-500">*</span></label>
              <input
                v-model="form.chartTypeCode"
                type="text"
                required
                placeholder="例如：XBAR_R / I_MR / P / U"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">種類名稱 (Name) <span class="text-red-500">*</span></label>
              <input
                v-model="form.chartTypeName"
                type="text"
                required
                placeholder="例如：平均值與全距管制圖"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">資料類型 (Category)</label>
              <select
                v-model="form.dataCategory"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              >
                <option value="Variable">計量型 (Variable)</option>
                <option value="Attribute">計數型 (Attribute)</option>
              </select>
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">標準抽樣數 (Required N)</label>
              <input
                v-model="form.requiredSampleSize"
                type="number"
                min="1"
                max="25"
                placeholder="例如：5 (Xbar-R) / 1 (I-MR)"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">說明描述 (Description)</label>
            <textarea
              v-model="form.description"
              rows="2"
              placeholder="請輸入圖表適用場景說明..."
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all resize-none"
            ></textarea>
          </div>

          <div class="space-y-1.5">
            <div class="flex items-center justify-between">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider flex items-center gap-1.5">
                <Code2 class="w-4 h-4 text-indigo-500" /> 公式配置 JSON (Formula Config)
              </label>
              <div class="flex items-center gap-2">
                <button
                  @click="setStandardFormula"
                  type="button"
                  class="px-2.5 py-1 rounded-lg bg-blue-50 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 hover:bg-blue-100 border border-blue-200 dark:border-blue-800 text-[11px] font-bold transition-all shadow-xs"
                >
                  ✨ 預設公式：標準全距法
                </button>
                <button
                  @click="setMrMethodFormula"
                  type="button"
                  class="px-2.5 py-1 rounded-lg bg-purple-50 dark:bg-purple-950/60 text-purple-600 dark:text-purple-400 hover:bg-purple-100 border border-purple-200 dark:border-purple-800 text-[11px] font-bold transition-all shadow-xs"
                >
                  🧪 平均移動全距法 (MR-Method)
                </button>
                <button
                  @click="setSigmaMethodFormula"
                  type="button"
                  class="px-2.5 py-1 rounded-lg bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 hover:bg-emerald-100 border border-emerald-200 dark:border-emerald-800 text-[11px] font-bold transition-all shadow-xs"
                >
                  🧪 樣本標準差法 (Sigma-Method)
                </button>
              </div>
            </div>
            <textarea
              v-model="form.formulaConfigJson"
              rows="5"
              class="w-full px-4 py-2.5 bg-slate-900 text-emerald-400 dark:bg-black dark:text-emerald-300 font-mono text-xs rounded-xl border border-slate-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all resize-none"
            ></textarea>
          </div>

          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">啟用狀態</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
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
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
            >
              <Save class="w-4 h-4" /> 確認儲存
            </button>
          </div>
        </form>
      </div>
    </div>
  </section>
</template>
