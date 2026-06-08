<script setup>
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Users,
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
  Mail,
  Building2,
  UserCheck,
  Info
} from "lucide-vue-next";

const rows = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const form = ref({
  operatorCode: "",
  operatorName: "",
  department: "",
  email: "",
  isActive: true
});
const formErr = ref("");

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const res = await api.get("/operators");
    rows.value = res.data || [];
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
      (row.operatorCode && row.operatorCode.toLowerCase().includes(q)) ||
      (row.operatorName && row.operatorName.toLowerCase().includes(q)) ||
      (row.department && row.department.toLowerCase().includes(q)) ||
      (row.email && row.email.toLowerCase().includes(q));
      
    const matchStatus = statusFilter.value === "all" || 
      (statusFilter.value === "active" && row.isActive) ||
      (statusFilter.value === "inactive" && !row.isActive);

    return matchQuery && matchStatus;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    operatorCode: "",
    operatorName: "",
    department: "",
    email: "",
    isActive: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    operatorCode: item.operatorCode || "",
    operatorName: item.operatorName || "",
    department: item.department || "",
    email: item.email || "",
    isActive: item.isActive ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.operatorCode?.trim() || !form.value.operatorName?.trim()) {
    formErr.value = "作業員工號與姓名皆為必填欄位。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = { ...form.value };

    if (modalMode.value === "create") {
      const { data } = await api.post("/operators", payload);
      rows.value.push(data);
      successAlert("成功建立新作業人員：" + data.operatorName);
    } else {
      const { data } = await api.put(`/operators/${currentId.value}`, payload);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert("成功更新作業人員：" + data.operatorName);
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(`確定要刪除作業人員「${item.operatorCode} (${item.operatorName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/operators/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除作業人員：" + item.operatorName);
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
  <section class="space-y-6 pb-12">
    <!-- Title & Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-xl shadow-lg shadow-blue-500/30 text-white">
          <Users class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">作業人員與權限主檔維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">管理全廠產線量測作業工程師、工號、所屬部門及電子郵件通知設定</p>
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
          <Plus class="w-4 h-4" /> 新增人員
        </button>
      </div>
    </div>

    <!-- Guide / Wizard Tip -->
    <div class="p-5 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-900/20 border border-blue-100 dark:border-blue-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-blue-100 dark:bg-blue-900/50 rounded-xl text-blue-600 dark:text-blue-400 mt-0.5">
        <Info class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-blue-900 dark:text-blue-300">模組指南：作業人員主檔 (Operators)</h4>
        <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
          此模組用於設定現場量測人員的基本資料。當 SPC 數據發生異常時，系統會將告警信件發送至這裡設定的電子郵件信箱。
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
          placeholder="搜尋工號、姓名、部門或 Email..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
        />
      </div>

      <div class="flex items-center gap-2 bg-slate-100 dark:bg-slate-800/60 p-1 rounded-xl border border-slate-200 dark:border-slate-700 w-full sm:w-auto">
        <button
          v-for="f in [{id:'all', label:'全部人員'}, {id:'active', label:'在職中'}, {id:'inactive', label:'已離職/停權'}]"
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

    <!-- Data Table Container -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden transition-colors">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
              <th class="py-4 px-6">ID</th>
              <th class="py-4 px-6">工號 / 姓名</th>
              <th class="py-4 px-6">所屬部門</th>
              <th class="py-4 px-6">電子郵件 (警報通知)</th>
              <th class="py-4 px-6 text-center">在職狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="6" class="py-12 text-center text-slate-400">正在載入人員清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="6" class="py-12 text-center text-slate-400 font-medium">找不到相符的作業人員資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-blue-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-4 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2">
                  <UserCheck class="w-4 h-4 text-blue-500" /> {{ item.operatorCode }}
                </div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ item.operatorName }}</div>
              </td>
              <td class="py-4 px-6 font-semibold text-xs text-slate-600 dark:text-slate-300">
                <div class="flex items-center gap-1.5">
                  <Building2 class="w-3.5 h-3.5 text-slate-400" /> {{ item.department || '未指派' }}
                </div>
              </td>
              <td class="py-4 px-6 font-mono text-xs text-slate-600 dark:text-slate-300">
                <div class="flex items-center gap-1.5">
                  <Mail class="w-3.5 h-3.5 text-slate-400" /> {{ item.email || '-' }}
                </div>
              </td>
              <td class="py-4 px-6 text-center">
                <span
                  :class="[
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide',
                    item.isActive
                      ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800'
                      : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700'
                  ]"
                >
                  <span :class="['w-1.5 h-1.5 rounded-full', item.isActive ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                  {{ item.isActive ? '在職中' : '停權/離職' }}
                </span>
              </td>
              <td class="py-4 px-6 text-right space-x-2">
                <button
                  @click="openEditModal(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  title="編輯人員"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除人員"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>顯示第 1 至 {{ filteredRows.length }} 項結果（總計 {{ rows.length }} 位人員）</span>
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
              <Users class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增作業人員' : '編輯人員資料' }}
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
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">工號 (Code) <span class="text-red-500">*</span></label>
              <input
                v-model="form.operatorCode"
                type="text"
                required
                placeholder="例如：OP-101 / PMR-01"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">姓名 (Name) <span class="text-red-500">*</span></label>
              <input
                v-model="form.operatorName"
                type="text"
                required
                placeholder="例如：王大明"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">所屬部門 (Dept)</label>
              <input
                v-model="form.department"
                type="text"
                placeholder="例如：製造部一部 / 品管科"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">聯絡 Email (異常通報用)</label>
              <input
                v-model="form.email"
                type="email"
                placeholder="例如：user@pmr.com.tw"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all font-mono"
              />
            </div>
          </div>

          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">在職/帳號啟用設定</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input v-model="form.isActive" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isActive ? '啟用中 (Active)' : '停權/離職 (Inactive)' }}</span>
            </label>
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
