<script setup>
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import { 
  ShieldAlert, 
  Wrench, 
  CheckCircle2, 
  Clock, 
  User, 
  MessageSquare, 
  X, 
  Save, 
  RefreshCw,
  Search,
  Filter,
  ArrowRight
} from "lucide-vue-next";

const rows = ref([]);
const loading = ref(false);
const err = ref("");
const successMsg = ref("");

// Filter state
const statusFilter = ref("Open"); // "Open", "InProgress", "Closed", "All"
const searchQuery = ref("");

// Modal state
const showModal = ref(false);
const saving = ref(false);
const formErr = ref("");
const currentAlert = ref(null);
const form = ref({
  status: "InProgress",
  rootCause: "",
  correctiveAction: "",
  responsibleUser: ""
});

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const { data } = await api.get("/v2/alerts");
    rows.value = data || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const filteredRows = computed(() => {
  return rows.value.filter(r => {
    // 1. Search Query
    const q = searchQuery.value.toLowerCase();
    const matchSearch = !q || 
      (r.message && r.message.toLowerCase().includes(q)) ||
      (r.rootCause && r.rootCause.toLowerCase().includes(q)) ||
      (r.correctiveAction && r.correctiveAction.toLowerCase().includes(q)) ||
      (r.id.toString().includes(q));

    // 2. Status Filter
    let matchStatus = true;
    if (statusFilter.value === "Open") matchStatus = !r.status || r.status === "Open" || !r.isAcknowledged;
    else if (statusFilter.value === "InProgress") matchStatus = r.status === "InProgress";
    else if (statusFilter.value === "Closed") matchStatus = r.status === "Closed" || r.isAcknowledged;

    return matchSearch && matchStatus;
  });
});

const stats = computed(() => {
  return {
    open: rows.value.filter(r => !r.status || r.status === "Open" || !r.isAcknowledged).length,
    inProgress: rows.value.filter(r => r.status === "InProgress").length,
    closed: rows.value.filter(r => r.status === "Closed" || r.isAcknowledged).length
  };
});

function openResolveModal(alert) {
  currentAlert.value = alert;
  form.value = {
    status: alert.status === "Closed" || alert.isAcknowledged ? "Closed" : (alert.status || "InProgress"),
    rootCause: alert.rootCause || "",
    correctiveAction: alert.correctiveAction || "",
    responsibleUser: alert.responsibleUser || ""
  };
  formErr.value = "";
  showModal.value = true;
}

async function submitWorkflow() {
  formErr.value = "";
  saving.value = true;
  try {
    await api.put(`/v2/alerts/${currentAlert.value.id}/workflow`, {
      status: form.value.status,
      rootCause: form.value.rootCause,
      correctiveAction: form.value.correctiveAction,
      responsibleUser: form.value.responsibleUser
    });
    
    // 如果設為 Closed，順便呼叫 ACK 確保一致性
    if (form.value.status === "Closed" && !currentAlert.value.isAcknowledged) {
      await api.post(`/alerts/${currentAlert.value.id}/ack`).catch(() => {});
    }

    successMsg.value = `異常單 #ALT-${String(currentAlert.value.id).padStart(5, '0')} 處置進度已更新！`;
    setTimeout(() => { successMsg.value = ""; }, 3000);
    showModal.value = false;
    await load();
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    saving.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6 pb-12">
    <!-- Header Banner -->
    <div class="p-6 rounded-2xl bg-gradient-to-r from-slate-900 via-sky-950 to-slate-900 text-white shadow-xl border border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-6 relative overflow-hidden">
      <div class="absolute -right-12 -top-12 w-64 h-64 bg-amber-500/10 rounded-full blur-3xl pointer-events-none"></div>
      
      <div class="flex items-start gap-4 z-10">
        <div class="p-3 bg-sky-500/20 text-sky-400 border border-sky-500/30 rounded-2xl shadow-lg">
          <Wrench class="w-8 h-8" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30">OOC 通報維護</span>
          </div>
          <h1 class="text-2xl font-black mt-1 bg-gradient-to-r from-white via-slate-100 to-slate-300 bg-clip-text text-transparent">
            異常處置閉環工作流 (Alert Workflow)
          </h1>
          <p class="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
            追蹤所有 OOS/OOC 警報，填寫真因分析 (Root Cause) 與對策 (Action)，確保每一個品質異常皆被妥善處理與結案，符合 8D 報告與稽核要求。
          </p>
        </div>
      </div>

      <div class="z-10">
        <button
          type="button"
          @click="load"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl font-semibold text-xs bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-all shadow-sm"
        >
          <RefreshCw :class="{ 'animate-spin': loading }" class="w-4 h-4 text-sky-400" /> 同步最新狀態
        </button>
      </div>
    </div>

    <!-- Alert Messages Box -->
    <div v-if="err" class="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-600 text-sm flex items-center gap-3 animate-fade-in shadow-sm">
      <ShieldAlert class="w-5 h-5 flex-shrink-0 text-red-500" />
      <div>{{ err }}</div>
    </div>
    <div v-if="successMsg" class="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-600 text-sm flex items-center gap-3 animate-fade-in shadow-sm">
      <CheckCircle2 class="w-5 h-5 flex-shrink-0 text-emerald-500" />
      <div class="font-bold">{{ successMsg }}</div>
    </div>

    <!-- Stats & Filters -->
    <div class="flex flex-col lg:flex-row items-center justify-between gap-4 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <!-- Status Badges / Filters -->
      <div class="flex items-center gap-2 w-full lg:w-auto overflow-x-auto pb-2 lg:pb-0">
        <button
          @click="statusFilter = 'All'"
          :class="statusFilter === 'All' ? 'bg-slate-800 text-white shadow-md' : 'bg-slate-100 dark:bg-slate-800/50 text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700'"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all whitespace-nowrap"
        >
          全部單據 ({{ rows.length }})
        </button>
        <button
          @click="statusFilter = 'Open'"
          :class="statusFilter === 'Open' ? 'bg-red-600 text-white shadow-md' : 'bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 border border-red-100 dark:border-red-900/30 hover:bg-red-100'"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all whitespace-nowrap"
        >
          <ShieldAlert class="w-4 h-4" /> 新警報/未處置 ({{ stats.open }})
        </button>
        <button
          @click="statusFilter = 'InProgress'"
          :class="statusFilter === 'InProgress' ? 'bg-amber-500 text-white shadow-md' : 'bg-amber-50 dark:bg-amber-900/20 text-amber-600 dark:text-amber-400 border border-amber-100 dark:border-amber-900/30 hover:bg-amber-100'"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all whitespace-nowrap"
        >
          <Clock class="w-4 h-4" /> 處置中 ({{ stats.inProgress }})
        </button>
        <button
          @click="statusFilter = 'Closed'"
          :class="statusFilter === 'Closed' ? 'bg-emerald-600 text-white shadow-md' : 'bg-emerald-50 dark:bg-emerald-900/20 text-emerald-600 dark:text-emerald-400 border border-emerald-100 dark:border-emerald-900/30 hover:bg-emerald-100'"
          class="flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-bold transition-all whitespace-nowrap"
        >
          <CheckCircle2 class="w-4 h-4" /> 已結案 ({{ stats.closed }})
        </button>
      </div>

      <!-- Search Box -->
      <div class="relative w-full lg:w-72 flex-shrink-0">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          v-model="searchQuery"
          type="text"
          class="w-full pl-9 pr-4 py-2 text-sm rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 focus:ring-2 focus:ring-sky-500 text-slate-800 dark:text-slate-200 placeholder:text-slate-400 transition-all"
          placeholder="搜尋真因、對策或單號..."
        />
      </div>
    </div>

    <!-- Main Table -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-md overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              <th class="py-4 px-5">異常單號 / 時間</th>
              <th class="py-4 px-5">警報內容 (問題描述)</th>
              <th class="py-4 px-5 w-1/3">真因分析與對策</th>
              <th class="py-4 px-5 text-center">狀態 / 負責人</th>
              <th class="py-4 px-5 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/60 text-sm">
            <tr v-if="loading && rows.length === 0">
              <td colspan="5" class="py-12 text-center text-slate-400">載入警報單中...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="5" class="py-12 text-center text-slate-400 font-medium flex-col items-center justify-center">
                <div class="flex justify-center mb-2"><CheckCircle2 class="w-8 h-8 text-emerald-400" /></div>
                目前沒有符合條件的異常單
              </td>
            </tr>
            <tr
              v-for="r in filteredRows"
              :key="r.id"
              class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors"
            >
              <!-- ID & Time -->
              <td class="py-4 px-5">
                <div class="font-mono font-bold text-slate-800 dark:text-slate-100 text-sm">#ALT-{{ String(r.id).padStart(5, '0') }}</div>
                <div class="mt-1 text-xs text-slate-400">
                  {{ new Date(r.occurredAt).toLocaleString() }}
                </div>
              </td>

              <!-- Message -->
              <td class="py-4 px-5">
                <div class="font-medium text-red-600 dark:text-red-400 leading-relaxed text-xs">
                  {{ r.message }}
                </div>
                <div class="mt-1 text-[11px] text-slate-500 font-mono">
                  Part: {{ r.productId || '-' }} | Station: {{ r.stationId || '-' }} | Value: {{ r.actualValue != null ? Number(r.actualValue).toFixed(3) : '-' }}
                </div>
              </td>

              <!-- Root Cause & Corrective Action -->
              <td class="py-4 px-5">
                <div v-if="r.rootCause || r.correctiveAction" class="space-y-2">
                  <div v-if="r.rootCause" class="bg-amber-50 dark:bg-amber-950/30 p-2 rounded-lg border border-amber-100 dark:border-amber-900/50">
                    <div class="text-[10px] font-bold text-amber-600 uppercase mb-0.5">真因分析 (Root Cause)</div>
                    <div class="text-xs text-slate-700 dark:text-slate-300">{{ r.rootCause }}</div>
                  </div>
                  <div v-if="r.correctiveAction" class="bg-emerald-50 dark:bg-emerald-950/30 p-2 rounded-lg border border-emerald-100 dark:border-emerald-900/50">
                    <div class="text-[10px] font-bold text-emerald-600 uppercase mb-0.5">處置對策 (Action)</div>
                    <div class="text-xs text-slate-700 dark:text-slate-300">{{ r.correctiveAction }}</div>
                  </div>
                </div>
                <div v-else class="text-xs text-slate-400 italic">尚未填寫真因與對策</div>
              </td>

              <!-- Status & Owner -->
              <td class="py-4 px-5 text-center">
                <div class="flex flex-col items-center gap-1.5">
                  <span
                    v-if="r.status === 'Closed' || r.isAcknowledged"
                    class="px-2.5 py-1 rounded-md text-[10px] font-bold bg-emerald-500 text-white tracking-widest inline-flex items-center gap-1"
                  >
                    <CheckCircle2 class="w-3 h-3" /> CLOSED
                  </span>
                  <span
                    v-else-if="r.status === 'InProgress'"
                    class="px-2.5 py-1 rounded-md text-[10px] font-bold bg-amber-500 text-white tracking-widest inline-flex items-center gap-1"
                  >
                    <Clock class="w-3 h-3" /> IN PROGRESS
                  </span>
                  <span
                    v-else
                    class="px-2.5 py-1 rounded-md text-[10px] font-bold bg-red-500 text-white tracking-widest inline-flex items-center gap-1 animate-pulse"
                  >
                    <ShieldAlert class="w-3 h-3" /> OPEN
                  </span>

                  <div v-if="r.responsibleUser" class="text-[11px] font-bold text-slate-500 dark:text-slate-400 flex items-center gap-1 mt-1">
                    <User class="w-3 h-3" /> {{ r.responsibleUser }}
                  </div>
                </div>
              </td>

              <!-- Action -->
              <td class="py-4 px-5 text-right">
                <button
                  type="button"
                  @click="openResolveModal(r)"
                  class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-bold transition-all border shadow-sm"
                  :class="r.status === 'Closed' || r.isAcknowledged 
                    ? 'bg-white dark:bg-slate-800 text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700 hover:bg-slate-50' 
                    : 'bg-sky-50 dark:bg-sky-900/30 text-sky-600 dark:text-sky-400 border-sky-200 dark:border-sky-800 hover:bg-sky-100'"
                >
                  <Wrench class="w-3.5 h-3.5" />
                  {{ r.status === 'Closed' || r.isAcknowledged ? '檢視/修改' : '填寫處置' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Modal (Resolve Alert) -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/70 backdrop-blur-sm animate-fade-in">
      <div class="bg-white dark:bg-slate-900 w-full max-w-2xl rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden transform transition-all flex flex-col max-h-[90vh]">
        
        <!-- Modal Header -->
        <div class="flex items-center justify-between px-6 py-5 bg-gradient-to-r from-sky-600 to-indigo-600 text-white">
          <div class="flex items-center gap-3">
            <div class="p-2 bg-white/20 rounded-xl backdrop-blur shadow-sm">
              <Wrench class="w-5 h-5" />
            </div>
            <div>
              <h3 class="text-lg font-black tracking-wide">異常單號 #ALT-{{ String(currentAlert?.id).padStart(5, '0') }} 處置追蹤</h3>
              <p class="text-[11px] text-sky-100 font-medium">Root Cause & Corrective Action</p>
            </div>
          </div>
          <button @click="showModal = false" type="button" class="p-2 rounded-xl hover:bg-white/20 transition-all text-white">
            <X class="w-5 h-5" />
          </button>
        </div>

        <div class="overflow-y-auto p-6">
          <div v-if="formErr" class="mb-4 p-3 rounded-xl bg-red-50 text-red-600 text-sm border border-red-200 font-bold flex items-center gap-2">
            <ShieldAlert class="w-4 h-4" /> {{ formErr }}
          </div>

          <!-- Problem Description -->
          <div class="mb-6 p-4 rounded-xl bg-red-50 dark:bg-red-950/20 border border-red-100 dark:border-red-900/30">
            <div class="text-[11px] font-bold text-red-500 uppercase tracking-wider mb-1">問題描述 (Problem)</div>
            <div class="text-sm text-red-700 dark:text-red-300 font-medium">{{ currentAlert?.message }}</div>
            <div class="text-xs text-red-400 mt-1 font-mono">Occurred At: {{ new Date(currentAlert?.occurredAt).toLocaleString() }}</div>
          </div>

          <form @submit.prevent="submitWorkflow" class="space-y-5">
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-5">
              <div class="space-y-2">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">處理階段 (Status)</label>
                <div class="flex gap-2">
                  <label class="flex-1 cursor-pointer">
                    <input type="radio" v-model="form.status" value="InProgress" class="peer sr-only" />
                    <div class="px-3 py-2.5 text-center rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-600 dark:text-slate-400 peer-checked:bg-amber-500 peer-checked:text-white peer-checked:border-amber-600 font-bold text-xs transition-all flex items-center justify-center gap-1.5">
                      <Clock class="w-4 h-4" /> 處置中
                    </div>
                  </label>
                  <label class="flex-1 cursor-pointer">
                    <input type="radio" v-model="form.status" value="Closed" class="peer sr-only" />
                    <div class="px-3 py-2.5 text-center rounded-xl border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-slate-600 dark:text-slate-400 peer-checked:bg-emerald-500 peer-checked:text-white peer-checked:border-emerald-600 font-bold text-xs transition-all flex items-center justify-center gap-1.5">
                      <CheckCircle2 class="w-4 h-4" /> 結案 (Closed)
                    </div>
                  </label>
                </div>
              </div>

              <div class="space-y-2">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">負責人員 (Owner)</label>
                <div class="relative">
                  <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400"><User class="w-4 h-4" /></span>
                  <input
                    v-model="form.responsibleUser"
                    type="text"
                    placeholder="輸入工程師姓名或工號"
                    class="w-full pl-9 pr-4 py-2.5 bg-white dark:bg-slate-900 border border-slate-300 dark:border-slate-700 rounded-xl text-sm font-bold text-slate-800 dark:text-white focus:ring-2 focus:ring-sky-500 focus:border-transparent transition-all"
                  />
                </div>
              </div>
            </div>

            <div class="space-y-2">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">真因分析 (Root Cause)</label>
              <textarea
                v-model="form.rootCause"
                rows="3"
                placeholder="請描述發生此次異常的根本原因 (如：參數設定錯誤、材料變異)..."
                class="w-full p-4 bg-amber-50/50 dark:bg-amber-950/10 border border-amber-200 dark:border-amber-900/50 rounded-xl text-sm font-medium text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-amber-500 focus:border-transparent transition-all placeholder:text-amber-700/40"
              ></textarea>
            </div>

            <div class="space-y-2">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">處置對策 (Corrective Action)</label>
              <textarea
                v-model="form.correctiveAction"
                rows="3"
                placeholder="請描述為防止再次發生所採取的行動 (如：修正機台參數、更換刀具)..."
                class="w-full p-4 bg-emerald-50/50 dark:bg-emerald-950/10 border border-emerald-200 dark:border-emerald-900/50 rounded-xl text-sm font-medium text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all placeholder:text-emerald-700/40"
              ></textarea>
            </div>
            
            <div class="pt-4 border-t border-slate-100 dark:border-slate-800 flex items-center justify-end gap-3">
              <button
                @click="showModal = false"
                type="button"
                class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 text-slate-600 dark:text-slate-300 font-bold text-sm transition-all"
              >
                取消
              </button>
              <button
                type="submit"
                :disabled="saving"
                class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-sky-600 to-indigo-600 hover:from-sky-500 hover:to-indigo-500 text-white font-bold text-sm shadow-lg shadow-sky-500/25 transition-all transform hover:-translate-y-0.5"
              >
                <RefreshCw v-if="saving" class="w-4 h-4 animate-spin" />
                <Save v-else class="w-4 h-4" />
                儲存處置紀錄
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>
