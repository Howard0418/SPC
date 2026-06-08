<script setup>
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Cpu,
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
  Activity,
  Info
} from "lucide-vue-next";

const rows = ref([]);
const processes = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const form = ref({
  machineCode: "",
  machineName: "",
  processId: null,
  location: "",
  status: "IDLE", // IDLE, RUNNING, DOWN, MAINTENANCE
  isEnabled: true
});
const formErr = ref("");

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [resMachines, resProcesses] = await Promise.all([
      api.get("/machines"),
      api.get("/processes")
    ]);
    rows.value = resMachines.data || [];
    processes.value = resProcesses.data || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const processMap = computed(() => {
  const map = {};
  processes.value.forEach(p => {
    map[p.id] = `${p.processCode} (${p.processName})`;
  });
  return map;
});

const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const pName = row.processId ? (processMap.value[row.processId] || "") : "";
    const matchQuery = !q || 
      (row.machineCode && row.machineCode.toLowerCase().includes(q)) ||
      (row.machineName && row.machineName.toLowerCase().includes(q)) ||
      (row.location && row.location.toLowerCase().includes(q)) ||
      (row.status && row.status.toLowerCase().includes(q)) ||
      pName.toLowerCase().includes(q);
      
    if (statusFilter.value === "active") return matchQuery && row.isEnabled;
    if (statusFilter.value === "inactive") return matchQuery && !row.isEnabled;
    return matchQuery;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    machineCode: "",
    machineName: "",
    processId: processes.value.length > 0 ? processes.value[0].id : null,
    location: "",
    status: "IDLE",
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    machineCode: item.machineCode || "",
    machineName: item.machineName || "",
    processId: item.processId || (processes.value.length > 0 ? processes.value[0].id : null),
    location: item.location || "",
    status: item.status || "IDLE",
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.machineCode?.trim() || !form.value.machineName?.trim() || !form.value.processId) {
    formErr.value = "機台代號、機台名稱與所屬製程皆為必填欄位。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = {
      ...form.value,
      processId: parseInt(form.value.processId)
    };
    if (modalMode.value === "create") {
      const { data } = await api.post("/machines", payload);
      rows.value.push(data);
      successAlert("成功建立新機台：" + data.machineName);
    } else {
      const { data } = await api.put(`/machines/${currentId.value}`, payload);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert("成功更新機台：" + data.machineName);
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(`確定要刪除機台「${item.machineCode} (${item.machineName})」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/machines/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("成功刪除機台：" + item.machineName);
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

function getStatusClass(st) {
  switch(st) {
    case 'RUNNING': return 'bg-emerald-50 text-emerald-600 border-emerald-300 dark:bg-emerald-950/60 dark:text-emerald-400 dark:border-emerald-800';
    case 'IDLE': return 'bg-blue-50 text-blue-600 border-blue-300 dark:bg-blue-950/60 dark:text-blue-400 dark:border-blue-800';
    case 'DOWN': return 'bg-red-50 text-red-600 border-red-300 dark:bg-red-950/60 dark:text-red-400 dark:border-red-800';
    case 'MAINTENANCE': return 'bg-amber-50 text-amber-600 border-amber-300 dark:bg-amber-950/60 dark:text-amber-400 dark:border-amber-800';
    default: return 'bg-slate-50 text-slate-600 border-slate-300 dark:bg-slate-800 dark:text-slate-400 dark:border-slate-700';
  }
}

function getStatusText(st) {
  switch(st) {
    case 'RUNNING': return '運轉中 (Running)';
    case 'IDLE': return '閒置中 (Idle)';
    case 'DOWN': return '停機異常 (Down)';
    case 'MAINTENANCE': return '保養中 (PM)';
    default: return st;
  }
}

onMounted(load);
</script>

<template>
  <section class="space-y-6">
    <!-- Title & Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-violet-600 to-purple-600 rounded-xl shadow-lg shadow-purple-500/30 text-white">
          <Cpu class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">生產機台主檔維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">管理各工站所屬生產設備、機台位置與即時運行狀態</p>
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
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-violet-600 to-purple-600 hover:from-violet-500 hover:to-purple-500 text-white text-sm font-bold shadow-lg shadow-purple-500/25 hover:shadow-xl hover:shadow-purple-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> 新增機台
        </button>
      </div>
    </div>

    <!-- Guide / Wizard Tip -->
    <div class="p-5 bg-gradient-to-r from-purple-50 to-violet-50 dark:from-purple-950/30 dark:to-violet-900/20 border border-purple-100 dark:border-purple-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-purple-100 dark:bg-purple-900/50 rounded-xl text-purple-600 dark:text-purple-400 mt-0.5">
        <Info class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-purple-900 dark:text-purple-300">模組指南：生產機台主檔 (Machines)</h4>
        <p class="text-xs text-purple-700 dark:text-purple-400/80 mt-1.5 leading-relaxed">
          此模組用於建檔廠內所有生產設備，並將其綁定至特定的「製程工站」。您可以在此檢視機台目前的運轉狀態。<br/>
          💡 <strong>功能說明：</strong> 在收集檢驗數據時，系統會記錄該批次數據是由哪一台機台生產的，方便未來利用管制圖進行「單一機台」的品質追溯。
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
          placeholder="搜尋代號、名稱、製程或廠置..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500/50 focus:border-purple-500 transition-all"
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
              ? 'bg-white dark:bg-slate-700 text-purple-600 dark:text-purple-400 shadow-sm'
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
              <th class="py-4 px-6">機台編號 / 名稱</th>
              <th class="py-4 px-6">所屬製程</th>
              <th class="py-4 px-6">廠房位置</th>
              <th class="py-4 px-6 text-center">運行狀態</th>
              <th class="py-4 px-6 text-center">啟用設定</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">正在載入機台清單...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">找不到相符的機台資料</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-purple-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-6 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-6 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2 text-base">
                  <Cpu class="w-4 h-4 text-purple-500" /> {{ item.machineCode }}
                </div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ item.machineName }}</div>
              </td>
              <td class="py-6 px-6">
                <span v-if="item.processId" class="text-slate-800 dark:text-slate-200 font-semibold text-xs bg-slate-100 dark:bg-slate-800 px-2.5 py-1 rounded-lg border border-slate-200 dark:border-slate-700">
                  {{ processMap[item.processId] || `製程 #${item.processId}` }}
                </span>
                <span v-else class="text-slate-400 text-xs">-</span>
              </td>
              <td class="py-6 px-6 text-slate-600 dark:text-slate-300 text-xs font-semibold">
                {{ item.location || '-' }}
              </td>
              <td class="py-6 px-6 text-center">
                <span :class="['px-3 py-1 rounded-full text-xs font-bold border tracking-wide inline-flex items-center gap-1.5', getStatusClass(item.status)]">
                  <Activity class="w-3 h-3" /> {{ getStatusText(item.status) }}
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
                  title="編輯機台"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除機台"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>顯示第 1 至 {{ filteredRows.length }} 項結果（總計 {{ rows.length }} 筆機台）</span>
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
            <div class="p-2.5 bg-purple-600 text-white rounded-xl shadow-md shadow-purple-500/20">
              <Cpu class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增生產機台' : '編輯生產機台' }}
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
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">機台編號 (Machine Code) <span class="text-red-500">*</span></label>
              <input
                v-model="form.machineCode"
                type="text"
                required
                placeholder="例如：M-101"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">機台名稱 (Machine Name) <span class="text-red-500">*</span></label>
              <input
                v-model="form.machineName"
                type="text"
                required
                placeholder="例如：日製精密雷射機"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all"
              />
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">所屬工站製程 <span class="text-red-500">*</span></label>
            <select
              v-model="form.processId"
              required
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-purple-500 transition-all"
            >
              <option disabled value="null">-- 請選擇所屬製程 --</option>
              <option v-for="p in processes" :key="p.id" :value="p.id">
                {{ p.processCode }} - {{ p.processName }}
              </option>
            </select>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">廠房位置 (Location)</label>
              <input
                v-model="form.location"
                type="text"
                placeholder="例如：廠房 A 棟 1F"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-purple-500 transition-all"
              />
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">即時狀態 (Status)</label>
              <select
                v-model="form.status"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-semibold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-purple-500 transition-all"
              >
                <option value="IDLE">閒置中 (IDLE)</option>
                <option value="RUNNING">正常運轉中 (RUNNING)</option>
                <option value="DOWN">停機異常 (DOWN)</option>
                <option value="MAINTENANCE">例行保養中 (MAINTENANCE)</option>
              </select>
            </div>
          </div>

          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">啟用設定</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-purple-300 dark:peer-focus:ring-purple-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-purple-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? '啟用 (Active)' : '停用 (Inactive)' }}</span>
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
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-violet-600 to-purple-600 hover:from-violet-500 hover:to-purple-500 text-white text-sm font-bold shadow-lg shadow-purple-500/25 hover:shadow-xl hover:shadow-purple-500/40 transition-all transform hover:-translate-y-0.5"
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
