<script setup>
import ModuleGuide from "../components/ModuleGuide.vue";
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  AlertTriangle,
  RefreshCw,
  Mail,
  Zap,
  CheckCircle2,
  Clock,
  Filter,
  Search,
  Check,
  ShieldAlert,
  ArrowUpRight,
  ExternalLink,
  FileText
} from "lucide-vue-next";

const rows = ref([]);
const loading = ref(false);
const err = ref("");
const successMsg = ref("");

// 測試發送相關狀態
const testEmailInput = ref("ihao_ting@pmr.com.tw");
const isSendingEmail = ref(false);
const outboxPath = ref("");

// 模擬警報表單
const showSimulateModal = ref(false);
const simulateForm = ref({
  partId: 101,
  processId: 201,
  actualValue: 105.85,
  alertType: "OOS",
  message: "量測數值超出規格上限 USL (100.0)",
  targetEmail: "ihao_ting@pmr.com.tw"
});

// 篩選與搜尋
const filterType = ref("all");
const searchQuery = ref("");

const filteredRows = computed(() => {
  return rows.value.filter(r => {
    const matchesSearch = !searchQuery.value || 
      r.message?.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      r.partNo?.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      r.processCode?.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      r.characteristicCode?.toLowerCase().includes(searchQuery.value.toLowerCase()) ||
      r.partId?.toString().includes(searchQuery.value) ||
      r.id?.toString().includes(searchQuery.value);
    
    if (!matchesSearch) return false;

    if (filterType.value === "oos") return r.alertType === "OutOfSpec" || r.alertType === 0;
    if (filterType.value === "ooc") return r.alertType === "OutOfControl" || r.alertType === 1;
    if (filterType.value === "pending") return !r.isAcknowledged;
    if (filterType.value === "acked") return r.isAcknowledged;
    return true;
  });
});

const stats = computed(() => {
  const total = rows.value.length;
  const pending = rows.value.filter(r => !r.isAcknowledged).length;
  const acked = rows.value.filter(r => r.isAcknowledged).length;
  const oosCount = rows.value.filter(r => r.alertType === "OutOfSpec" || r.alertType === 0).length;
  const oocCount = rows.value.filter(r => r.alertType === "OutOfControl" || r.alertType === 1).length;
  return { total, pending, acked, oosCount, oocCount };
});

async function load() {
  err.value = "";
  successMsg.value = "";
  loading.value = true;
  try {
    const res = await api.get("/alerts");
    rows.value = res.data || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function ack(id) {
  err.value = "";
  try {
    await api.post(`/alerts/${id}/ack`);
    successMsg.value = `警報單 #ALT-${String(id).padStart(5, '0')} 已成功簽收！`;
    await load();
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}

async function sendTestEmail() {
  err.value = "";
  successMsg.value = "";
  isSendingEmail.value = true;
  try {
    const res = await api.post("/alerts/test-email", null, {
      params: { email: testEmailInput.value }
    });
    outboxPath.value = res.data?.outboxFolder || "";
    successMsg.value = `測試通報信件已成功派發至 ${res.data?.recipient}！(郵件副本已同步儲存於 ${outboxPath.value})`;
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    isSendingEmail.value = false;
  }
}

async function triggerSimulateAlert() {
  err.value = "";
  successMsg.value = "";
  try {
    const res = await api.post("/alerts/simulate", simulateForm.value);
    outboxPath.value = res.data?.outboxFolder || "";
    successMsg.value = `成功觸發即時警報 #ALT-${String(res.data?.alert?.id).padStart(5, '0')}，且已自動寄出警報通知 Email 至 ${res.data?.recipient}！`;
    showSimulateModal.value = false;
    await load();
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6 pb-12">
    <!-- Header Banner -->
    <div class="p-6 rounded-2xl bg-gradient-to-r from-slate-900 via-indigo-950 to-slate-900 text-white shadow-xl border border-slate-800 flex flex-col md:flex-row md:items-center justify-between gap-6 relative overflow-hidden">
      <div class="absolute -right-12 -top-12 w-64 h-64 bg-red-500/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="absolute right-1/3 -bottom-12 w-64 h-64 bg-indigo-500/10 rounded-full blur-3xl pointer-events-none"></div>

      <div class="flex items-start gap-4 z-10">
        <div class="p-3 bg-red-500/20 text-red-400 border border-red-500/30 rounded-2xl shadow-lg">
          <ShieldAlert class="w-8 h-8 animate-pulse" />
        </div>
        <div>
          <div class="flex items-center gap-2">
            <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-red-500/20 text-red-300 border border-red-500/30">IATF 16949 / ISO 9001 規範</span>
            <span class="text-xs text-slate-400 font-mono">Real-time Alert Dispatcher</span>
          </div>
          <h1 class="text-2xl font-black mt-1 bg-gradient-to-r from-white via-slate-100 to-slate-300 bg-clip-text text-transparent">
            工業品質異常通報總覽與 SMTP 自動化警報中心
          </h1>
          <p class="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
            當產線進料或製程檢驗觸發規格超標 (OOS) 或 3-Sigma 管制界限失控 (OOC) 時，系統即時產生警報單並透過 SMTP 發送通知至品管負責人。
          </p>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="flex flex-wrap items-center gap-3 z-10">
        <button
          type="button"
          @click="showSimulateModal = true"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl font-bold text-xs bg-gradient-to-r from-amber-500 to-orange-600 hover:from-amber-400 hover:to-orange-500 text-white shadow-lg shadow-orange-500/20 border border-orange-400/30 transition-all hover:scale-105"
        >
          <Zap class="w-4 h-4 fill-current" /> ⚡ 模擬觸發異常警報 (OOS/OOC)
        </button>

        <button
          type="button"
          @click="load"
          :disabled="loading"
          class="flex items-center gap-2 px-3.5 py-2.5 rounded-xl font-semibold text-xs bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-all shadow-sm"
        >
          <RefreshCw :class="{ 'animate-spin': loading }" class="w-4 h-4 text-blue-400" /> {{ loading ? '同步中...' : '重新整理' }}
        </button>
      </div>
    </div>

    <!-- Guide / Operation Tip -->
    <ModuleGuide title="模組指南：異常通報總覽 (Alerts)">
      <p class="text-xs text-red-700 dark:text-red-400/80 mt-1.5 leading-relaxed">
          此頁面集中顯示由 SPC 判定產生的 OOS、OOC 異常通報，協助品管人員快速掌握待處理警報、已簽收案件與通知狀態。
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-red-700 dark:text-red-400/80 leading-relaxed">
          <div class="font-black text-red-900 dark:text-red-300">異常通報總覽頁面操作說明</div>
          <p><strong>查看統計：</strong>上方卡片可快速查看總警報、待簽收、已簽收、OOS 與 OOC 數量。</p>
          <p><strong>篩選異常：</strong>使用搜尋框與類型篩選，依異常單號、訊息、OOS/OOC 或簽收狀態查詢。</p>
          <p><strong>簽收處理：</strong>確認異常後可執行簽收，表示該異常已被人員接手確認。</p>
          <p><strong>前往處置：</strong>需要填寫真因與對策時，點選處置入口前往「異常單簽核處置」。</p>
          <p><strong>測試通知：</strong>可使用模擬警報或 SMTP 測試區確認異常通知與郵件流程。</p>
        </div>
    </ModuleGuide>

    <!-- Alert Messages Box -->
    <div v-if="err" class="p-4 rounded-xl bg-red-500/10 dark:bg-red-950/50 border border-red-500/30 text-red-600 dark:text-red-300 text-sm flex items-center gap-3 animate-fade-in shadow-sm">
      <ShieldAlert class="w-5 h-5 flex-shrink-0 text-red-500" />
      <div>{{ err }}</div>
    </div>

    <div v-if="successMsg" class="p-4 rounded-xl bg-emerald-500/10 dark:bg-emerald-950/50 border border-emerald-500/30 text-emerald-600 dark:text-emerald-300 text-sm flex items-start gap-3 animate-fade-in shadow-sm">
      <CheckCircle2 class="w-5 h-5 flex-shrink-0 text-emerald-500 mt-0.5" />
      <div class="space-y-1">
        <div class="font-bold">{{ successMsg }}</div>
        <div v-if="outboxPath" class="text-xs text-slate-500 dark:text-slate-400 font-mono bg-emerald-500/5 dark:bg-slate-900/50 p-2 rounded border border-emerald-500/20 flex items-center gap-2 mt-1">
          <FileText class="w-4 h-4 text-emerald-500 flex-shrink-0" />
          <span>本地郵件副本已生成於 Windows 檔案總管：<strong>{{ outboxPath }}</strong></span>
        </div>
      </div>
    </div>

    <!-- SMTP Email Test Sandbox Section -->
    <div class="p-5 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-md">
      <div class="flex items-center justify-between gap-4 mb-3 pb-3 border-b border-slate-100 dark:border-slate-800/80">
        <div class="flex items-center gap-2.5">
          <div class="w-8 h-8 rounded-lg bg-blue-500/10 text-blue-500 border border-blue-500/20 flex items-center justify-center font-bold">
            <Mail class="w-4 h-4" />
          </div>
          <div>
            <h3 class="text-sm font-bold text-slate-800 dark:text-slate-100">📧 SMTP 自動化警報發信測試沙盒</h3>
            <p class="text-[11px] text-slate-500 dark:text-slate-400">測試系統能否成功建置 HTML 通報郵件並進行寄發或存檔</p>
          </div>
        </div>
        <span class="px-2.5 py-1 rounded-lg text-[11px] font-mono bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 flex items-center gap-1.5">
          <span class="w-2 h-2 rounded-full bg-emerald-500 animate-ping"></span> 系統通報伺服器就緒
        </span>
      </div>

      <div class="flex flex-col sm:flex-row items-center gap-3">
        <div class="relative flex-1 w-full">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
            <Mail class="w-4 h-4" />
          </span>
          <input
            v-model="testEmailInput"
            type="email"
            class="w-full pl-9 pr-4 py-2.5 text-sm rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 focus:ring-2 focus:ring-blue-500 font-mono text-slate-800 dark:text-slate-200 placeholder:text-slate-400"
            placeholder="請輸入接收測試通報的 Email 信箱..."
          />
        </div>
        <button
          type="button"
          @click="sendTestEmail"
          :disabled="isSendingEmail || !testEmailInput"
          class="w-full sm:w-auto px-5 py-2.5 rounded-xl font-bold text-xs bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/20 transition-all flex items-center justify-center gap-2 disabled:opacity-50"
        >
          <RefreshCw v-if="isSendingEmail" class="w-4 h-4 animate-spin" />
          <Mail v-else class="w-4 h-4" />
          {{ isSendingEmail ? '正在連線發送...' : '發送測試通報郵件' }}
        </button>
      </div>
      <p class="text-[11px] text-slate-400 dark:text-slate-500 mt-2 italic">
        💡 提示：系統不僅會嘗試透過 SMTP 寄出信件，同時會在您的主機 <code>C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox</code> 產生精美的 HTML 檔案副本供您隨時點開預覽！
      </p>
    </div>

    <!-- Summary Stats Matrix -->
    <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-blue-500/10 text-blue-500 rounded-xl"><AlertTriangle class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">總警報次數</div>
          <div class="text-2xl font-black text-slate-800 dark:text-white mt-0.5">{{ stats.total }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-amber-500/10 text-amber-500 rounded-xl"><Clock class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">待簽收處置 (Open)</div>
          <div class="text-2xl font-black text-amber-600 dark:text-amber-400 mt-0.5">{{ stats.pending }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-emerald-500/10 text-emerald-500 rounded-xl"><CheckCircle2 class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">已完成簽收 (Ack)</div>
          <div class="text-2xl font-black text-emerald-600 dark:text-emerald-400 mt-0.5">{{ stats.acked }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-red-500/10 text-red-500 rounded-xl"><ShieldAlert class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">規格違規 (OOS)</div>
          <div class="text-2xl font-black text-red-600 dark:text-red-400 mt-0.5">{{ stats.oosCount }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4 col-span-2 md:col-span-1">
        <div class="p-3 bg-orange-500/10 text-orange-500 rounded-xl"><Zap class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">管制失控 (OOC)</div>
          <div class="text-2xl font-black text-orange-600 dark:text-orange-400 mt-0.5">{{ stats.oocCount }}</div>
        </div>
      </div>
    </div>

    <!-- Filtering & Search Bar -->
    <div class="flex flex-col sm:flex-row items-center justify-between gap-4 bg-white dark:bg-slate-900 p-4 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <!-- Filter Tabs -->
      <div class="flex items-center gap-1 bg-slate-100 dark:bg-slate-800/80 p-1 rounded-xl w-full sm:w-auto overflow-x-auto">
        <button
          type="button"
          @click="filterType = 'all'"
          :class="filterType === 'all' ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-slate-900'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          全部警報
        </button>
        <button
          type="button"
          @click="filterType = 'oos'"
          :class="filterType === 'oos' ? 'bg-red-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-red-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          🚨 規格違規 (OOS)
        </button>
        <button
          type="button"
          @click="filterType = 'ooc'"
          :class="filterType === 'ooc' ? 'bg-orange-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-orange-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          ⚠️ 管制失控 (OOC)
        </button>
        <button
          type="button"
          @click="filterType = 'pending'"
          :class="filterType === 'pending' ? 'bg-amber-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-amber-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          ⏳ 待簽收 (Open)
        </button>
        <button
          type="button"
          @click="filterType = 'acked'"
          :class="filterType === 'acked' ? 'bg-emerald-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-emerald-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          ✅ 已簽收 (Acked)
        </button>
      </div>

      <!-- Search Box -->
      <div class="relative w-full sm:w-72">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          v-model="searchQuery"
          type="text"
          class="w-full pl-9 pr-4 py-1.5 text-xs rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 focus:ring-2 focus:ring-blue-500 text-slate-800 dark:text-slate-200 placeholder:text-slate-400"
          placeholder="搜尋警報訊息、料號或單號..."
        />
      </div>
    </div>

    <!-- Main Alerts Table -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-md overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              <th class="py-3.5 px-4">單號 / 類型</th>
              <th class="py-3.5 px-4">發生時間</th>
              <th class="py-3.5 px-4">料號 / 站別</th>
              <th class="py-3.5 px-4">量測數據</th>
              <th class="py-3.5 px-6">違規判定說明</th>
              <th class="py-3.5 px-4 text-center">簽收狀態</th>
              <th class="py-3.5 px-4 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/60 text-sm">
            <tr v-if="loading && rows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">正在同步雲端警報資料庫...</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400 font-medium">沒有符合條件的異常通報單。</td>
            </tr>
            <tr
              v-for="r in filteredRows"
              :key="r.id"
              class="hover:bg-slate-50 dark:hover:bg-slate-800/40 transition-colors group"
            >
              <!-- ID & Type -->
              <td class="py-3.5 px-4">
                <div class="font-mono font-bold text-slate-800 dark:text-slate-100">#ALT-{{ String(r.id).padStart(5, '0') }}</div>
                <div class="mt-1">
                  <span
                    v-if="r.alertType === 'OutOfSpec' || r.alertType === 0"
                    class="px-2 py-0.5 rounded-md text-[11px] font-bold bg-red-500/10 text-red-600 dark:text-red-400 border border-red-500/20 inline-flex items-center gap-1"
                  >
                    <ShieldAlert class="w-3 h-3 text-red-500" /> 規格違規 (OOS)
                  </span>
                  <span
                    v-else
                    class="px-2 py-0.5 rounded-md text-[11px] font-bold bg-orange-500/10 text-orange-600 dark:text-orange-400 border border-orange-500/20 inline-flex items-center gap-1"
                  >
                    <Zap class="w-3 h-3 text-orange-500" /> 管制失控 (OOC)
                  </span>
                </div>
              </td>

              <!-- Timestamp -->
              <td class="py-3.5 px-4">
                <div class="font-medium text-slate-700 dark:text-slate-200">
                  {{ new Date(r.occurredAt).toLocaleDateString() }}
                </div>
                <div class="text-xs text-slate-400 font-mono">
                  {{ new Date(r.occurredAt).toLocaleTimeString() }}
                </div>
              </td>

              <!-- Part / Station -->
              <td class="py-3.5 px-4">
                <div class="font-bold text-slate-800 dark:text-white">{{ r.partNo || `Part ID: ${r.partId || 'N/A'}` }}</div>
                <div class="text-xs text-slate-400 font-mono">{{ r.processCode || `Process ID: ${r.processId || 'N/A'}` }} / {{ r.characteristicCode || `Characteristic ID: ${r.characteristicId || 'N/A'}` }}</div>
              </td>

              <!-- Value -->
              <td class="py-3.5 px-4">
                <span class="font-mono text-base font-black text-red-600 dark:text-red-400 px-2 py-1 rounded bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900/50">
                  {{ r.actualValue != null ? Number(r.actualValue).toFixed(4) : 'N/A' }}
                </span>
              </td>

              <!-- Message -->
              <td class="py-3.5 px-6 max-w-md">
                <div class="text-slate-700 dark:text-slate-300 font-medium break-words leading-relaxed">
                  {{ r.message }}
                </div>
                <div v-if="r.rootCause" class="mt-1 text-xs text-slate-500 dark:text-slate-400 bg-slate-50 dark:bg-slate-800/80 p-1.5 rounded border border-slate-200 dark:border-slate-700">
                  <span class="font-bold text-blue-500">真因分析：</span>{{ r.rootCause }}
                </div>
              </td>

              <!-- Status -->
              <td class="py-3.5 px-4 text-center">
                <span
                  v-if="r.isAcknowledged"
                  class="px-3 py-1 rounded-full text-xs font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 inline-flex items-center gap-1.5"
                >
                  <CheckCircle2 class="w-3.5 h-3.5" /> 已完成簽收
                </span>
                <span
                  v-else
                  class="px-3 py-1 rounded-full text-xs font-bold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/30 inline-flex items-center gap-1.5 animate-pulse"
                >
                  <Clock class="w-3.5 h-3.5" /> 待處置 (Open)
                </span>
              </td>

              <!-- Action -->
              <td class="py-3.5 px-4 text-right">
                <div class="flex items-center justify-end gap-2">
                  <button
                    v-if="!r.isAcknowledged"
                    type="button"
                    @click="ack(r.id)"
                    class="px-3 py-1.5 rounded-lg font-bold text-xs bg-blue-600 hover:bg-blue-500 text-white shadow-md shadow-blue-500/20 transition-all flex items-center gap-1.5"
                  >
                    <Check class="w-3.5 h-3.5" /> 立即簽收
                  </button>
                  <router-link
                    v-if="r.ppcId"
                    :to="`/spc?ppcId=${r.ppcId}`"
                    class="p-1.5 rounded-lg bg-blue-50 dark:bg-blue-900/30 hover:bg-blue-100 dark:hover:bg-blue-900/50 text-blue-600 dark:text-blue-400 transition-colors"
                    title="查看 V1 SPC 管制圖"
                  >
                    <ExternalLink class="w-4 h-4" />
                  </router-link>
                  <router-link
                    to="/alerts-workflow"
                    class="p-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors"
                    title="前往異常單 V2 閉環處置"
                  >
                    <ArrowUpRight class="w-4 h-4" />
                  </router-link>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 模擬觸發異常警報 Modal -->
    <div v-if="showSimulateModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-950/80 backdrop-blur-sm animate-fade-in">
      <div class="bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 shadow-2xl max-w-lg w-full overflow-hidden">
        <div class="p-6 bg-gradient-to-r from-amber-500 to-orange-600 text-white flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-white/20 rounded-2xl backdrop-blur"><Zap class="w-6 h-6 fill-current" /></div>
            <div>
              <h3 class="text-lg font-bold tracking-wide">⚡ 模擬觸發即時 SPC 異常警報</h3>
              <p class="text-xs text-orange-100 opacity-90">驗證 Web 即時建檔與 SMTP 自動化郵件派發</p>
            </div>
          </div>
          <button type="button" @click="showSimulateModal = false" class="p-2 rounded-xl bg-black/10 hover:bg-black/20 text-white transition-colors">✕</button>
        </div>

        <div class="p-6 space-y-4">
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">產品料號 ID (PartId)</label>
              <input v-model="simulateForm.partId" type="number" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 font-mono text-sm" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">量測工站 ID (ProcessId)</label>
              <input v-model="simulateForm.processId" type="number" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 font-mono text-sm" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">警報類型</label>
              <select v-model="simulateForm.alertType" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold">
                <option value="OOS">🚨 規格違規 (OutOfSpec)</option>
                <option value="OOC">⚠️ 管制失控 (OutOfControl)</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">量測數據 (異常數值)</label>
              <input v-model="simulateForm.actualValue" type="number" step="0.01" class="w-full px-3 py-2 rounded-xl bg-red-50 dark:bg-red-950/40 text-red-600 dark:text-red-400 font-mono font-bold border border-red-300 dark:border-red-900 text-sm" />
            </div>
          </div>

          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1">違規判定說明 (Email 顯示主旨內容)</label>
            <input v-model="simulateForm.message" type="text" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-medium" />
          </div>

          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1">警報收件工程師 Email</label>
            <input v-model="simulateForm.targetEmail" type="email" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 font-mono text-sm" />
          </div>
        </div>

        <div class="p-6 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-100 dark:border-slate-800 flex items-center justify-end gap-3">
          <button type="button" @click="showSimulateModal = false" class="px-4 py-2 rounded-xl text-xs font-bold text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
            取消
          </button>
          <button type="button" @click="triggerSimulateAlert" class="px-6 py-2.5 rounded-xl text-xs font-bold bg-gradient-to-r from-orange-500 to-red-600 hover:from-orange-400 hover:to-red-500 text-white shadow-lg shadow-orange-500/20 transition-all">
            ⚡ 立即送出警報通報
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
