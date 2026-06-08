<script setup>
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Package,
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
  Info
} from "lucide-vue-next";

const rows = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all"); // 'all', 'active', 'inactive'

const showModal = ref(false);
const modalMode = ref("create"); // 'create' or 'edit'
const currentId = ref(null);
const form = ref({
  partNo: "",
  partName: "",
  specification: "",
  customer: "",
  isEnabled: true
});
const formErr = ref("");

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const { data } = await api.get("/parts");
    rows.value = data || [];
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
      (row.partNo && row.partNo.toLowerCase().includes(q)) ||
      (row.partName && row.partName.toLowerCase().includes(q)) ||
      (row.specification && row.specification.toLowerCase().includes(q)) ||
      (row.customer && row.customer.toLowerCase().includes(q));
      
    if (statusFilter.value === "active") return matchQuery && row.isEnabled;
    if (statusFilter.value === "inactive") return matchQuery && !row.isEnabled;
    return matchQuery;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    partNo: "",
    partName: "",
    specification: "",
    customer: "",
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    partNo: item.partNo || "",
    partName: item.partName || "",
    specification: item.specification || "",
    customer: item.customer || "",
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.partNo?.trim() || !form.value.partName?.trim()) {
    formErr.value = "料號與品名皆為必填欄位。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    if (modalMode.value === "create") {
      const { data } = await api.post("/parts", form.value);
      rows.value.push(data);
      successAlert("成功建立新料號：" + data.partNo);
    } else {
      const { data } = await api.put(`/parts/${currentId.value}`, form.value);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert("成功更新料號：" + data.partNo);
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(`確定要刪除產品料號「${item.partNo} (${item.partName})」嗎？此動作無法復原。`)) return;
  loading.value = true;
  try {
    await api.delete(`/parts/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除料號：" + item.partNo);
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
    <!-- Title & Top Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-blue-600 to-indigo-600 rounded-xl shadow-lg shadow-blue-500/30 text-white">
          <Package class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">產品料號主檔維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">管理廠內所有生產料號、規格描述及對應客戶資訊</p>
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
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> 新增料號
        </button>
      </div>
    </div>

    <!-- Guide / Wizard Tip -->
    <div class="p-5 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-900/20 border border-blue-100 dark:border-blue-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-blue-100 dark:bg-blue-900/50 rounded-xl text-blue-600 dark:text-blue-400 mt-0.5">
        <Info class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-blue-900 dark:text-blue-300">模組指南：產品料號主檔 (Parts)</h4>
        <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
          此模組用於建檔全廠的生產料號。在 SPC 系統中，所有的抽樣檢驗數據都必須關聯到特定的產品料號。<br/>
          💡 <strong>下一步建議：</strong> 在此處建立料號後，請前往「料號檢驗基準設定」頁面，為該料號配置專屬的上下限 (USL/LSL) 規格。
        </p>
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
    <div class="flex flex-col sm:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
      <div class="relative w-full sm:w-80">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="搜尋料號、品名、規格或客戶..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all"
        />
      </div>

      <div class="flex items-center gap-2 w-full sm:w-auto overflow-x-auto p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
        <button
          v-for="f in [{id:'all', label:'全部狀態'}, {id:'active', label:'已啟用'}, {id:'inactive', label:'已停用'}]"
          :key="f.id"
          @click="statusFilter = f.id"
          type="button"
          :class="[
            'px-4 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap',
            statusFilter === f.id
              ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          ]"
        >
          {{ f.label }}
        </button>
      </div>
    </div>

    <!-- Data Table Container -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden transition-colors">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
              <th class="py-4 px-6">ID</th>
              <th class="py-4 px-6">產品料號 / 品名</th>
              <th class="py-4 px-6">規格描述</th>
              <th class="py-4 px-6">所屬客戶</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6">更新時間</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">正在載入料號清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">找不到相符的料號資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-blue-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-6 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-6 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2 text-base">
                  <Package class="w-4 h-4 text-blue-500" /> {{ item.partNo }}
                </div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ item.partName || '未設品名' }}</div>
              </td>
              <td class="py-6 px-6 text-slate-600 dark:text-slate-300 max-w-xs truncate" :title="item.specification">
                {{ item.specification || '-' }}
              </td>
              <td class="py-6 px-6">
                <span v-if="item.customer" class="px-2.5 py-1 bg-indigo-50 dark:bg-indigo-950/60 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800 rounded-lg text-xs font-semibold">
                  {{ item.customer }}
                </span>
                <span v-else class="text-slate-400 text-xs">-</span>
              </td>
              <td class="py-6 px-6 text-center">
                <span
                  :class="[
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border shadow-xs tracking-wide',
                    item.isEnabled
                      ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800'
                      : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700'
                  ]"
                >
                  <span :class="['w-1.5 h-1.5 rounded-full', item.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                  {{ item.isEnabled ? '啟用中' : '已停用' }}
                </span>
              </td>
              <td class="py-6 px-6 font-mono text-xs text-slate-500 dark:text-slate-400">
                {{ item.updatedAt ? new Date(item.updatedAt).toLocaleString() : '-' }}
              </td>
              <td class="py-6 px-6 text-right space-x-2">
                <button
                  @click="openEditModal(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  title="編輯料號"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除料號"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Footer count -->
      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>顯示第 1 至 {{ filteredRows.length }} 項結果（總計 {{ rows.length }} 筆料號）</span>
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
            <div class="p-2.5 bg-blue-600 text-white rounded-xl shadow-md shadow-blue-500/20">
              <Package class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增產品料號' : '編輯產品料號' }}
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

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">產品料號 (Part No) <span class="text-red-500">*</span></label>
              <input
                v-model="form.partNo"
                type="text"
                required
                placeholder="例如：P-1001"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">產品名稱 (Part Name) <span class="text-red-500">*</span></label>
              <input
                v-model="form.partName"
                type="text"
                required
                placeholder="例如：第一道雷射切割板"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">規格描述 (Specification)</label>
            <textarea
              v-model="form.specification"
              rows="3"
              placeholder="請輸入產品規格細節或製程要求標準..."
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all resize-none"
            ></textarea>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 items-center">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">所屬客戶 (Customer)</label>
              <input
                v-model="form.customer"
                type="text"
                placeholder="例如：Apple / TSMC"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>

            <div class="space-y-1.5 pt-2">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">啟用狀態</label>
              <label class="relative inline-flex items-center cursor-pointer">
                <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
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
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
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
