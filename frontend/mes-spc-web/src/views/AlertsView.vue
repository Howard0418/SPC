<script setup>
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
import LocalePicker from "../components/LocalePicker.vue";
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

const { t } = useI18n({ useScope: "global" });
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
    successMsg.value = {key:'spc.alerts.ackSuccess',args:{id:String(id).padStart(5,'0')}};
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
    successMsg.value = {key:"spc.alerts.emailSuccess",args:{recipient:res.data?.recipient,path:outboxPath.value}};
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
    successMsg.value = {key:"spc.alerts.simulateSuccess",args:{id:String(res.data?.alert?.id).padStart(5,"0"),recipient:res.data?.recipient}};
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
            <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-red-500/20 text-red-300 border border-red-500/30">{{ t('spc.alerts.standards') }}</span>
            <span class="text-xs text-slate-400 font-mono">Real-time Alert Dispatcher</span>
          </div>
          <h1 class="text-2xl font-black mt-1 bg-gradient-to-r from-white via-slate-100 to-slate-300 bg-clip-text text-transparent">
            {{ t('spc.alerts.title') }}
          </h1>
          <p class="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
            {{ t('spc.alerts.description') }}
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
          <Zap class="w-4 h-4 fill-current" /> {{ t('spc.alerts.simulate') }}
        </button>

        <button
          type="button"
          data-testid="alerts-load" @click="load"
          :disabled="loading"
          class="flex items-center gap-2 px-3.5 py-2.5 rounded-xl font-semibold text-xs bg-slate-800 hover:bg-slate-700 text-slate-200 border border-slate-700 transition-all shadow-sm"
        >
          <RefreshCw :class="{ 'animate-spin': loading }" class="w-4 h-4 text-blue-400" /> {{ loading ? t('spc.alerts.syncing') : t('spc.alerts.refresh') }}
        </button>
      </div>
    </div>

    <!-- Guide / Operation Tip -->
    <ModuleGuide :title="t('spc.alerts.guide')">
      <p class="text-xs text-red-700 dark:text-red-400/80 mt-1.5 leading-relaxed">
          {{ t('spc.alerts.guideDescription') }}
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-red-700 dark:text-red-400/80 leading-relaxed">
          <div class="font-black text-red-900 dark:text-red-300">{{ t('spc.alerts.instructions') }}</div>
          <p><strong>{{ t('spc.alerts.viewStats') }}</strong>{{ t('spc.alerts.viewStatsHelp') }}</p>
          <p><strong>{{ t('spc.alerts.filterAlerts') }}</strong>{{ t('spc.alerts.filterHelp') }}</p>
          <p><strong>{{ t('spc.alerts.acknowledge') }}</strong>{{ t('spc.alerts.ackHelp') }}</p>
          <p><strong>{{ t('spc.alerts.resolve') }}</strong>{{ t('spc.alerts.resolveHelp') }}</p>
          <p><strong>{{ t('spc.alerts.testNotifications') }}</strong>{{ t('spc.alerts.testHelp') }}</p>
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
        <div class="font-bold">{{ displayUiMessage(t, successMsg) }}</div>
        <div v-if="outboxPath" class="text-xs text-slate-500 dark:text-slate-400 font-mono bg-emerald-500/5 dark:bg-slate-900/50 p-2 rounded border border-emerald-500/20 flex items-center gap-2 mt-1">
          <FileText class="w-4 h-4 text-emerald-500 flex-shrink-0" />
          <span>{{ t('spc.alerts.localCopy') }}<strong>{{ outboxPath }}</strong></span>
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
            <h3 class="text-sm font-bold text-slate-800 dark:text-slate-100">{{ t('spc.alerts.smtpSandbox') }}</h3>
            <p class="text-[11px] text-slate-500 dark:text-slate-400">{{ t('spc.alerts.smtpDescription') }}</p>
          </div>
        </div>
        <span class="px-2.5 py-1 rounded-lg text-[11px] font-mono bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 flex items-center gap-1.5">
          <span class="w-2 h-2 rounded-full bg-emerald-500 animate-ping"></span> {{ t('spc.alerts.serverReady') }}
        </span>
      </div>

      <div class="flex flex-col sm:flex-row items-center gap-3">
        <div class="relative flex-1 w-full">
          <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
            <Mail class="w-4 h-4" />
          </span>
          <input
            data-testid="alerts-testEmailInput" v-model="testEmailInput"
            type="email"
            class="w-full pl-9 pr-4 py-2.5 text-sm rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 focus:ring-2 focus:ring-blue-500 font-mono text-slate-800 dark:text-slate-200 placeholder:text-slate-400"
            :placeholder="t('spc.alerts.emailPlaceholder')"
          />
        </div>
        <button
          type="button"
          data-testid="alerts-sendTestEmail" @click="sendTestEmail"
          :disabled="isSendingEmail || !testEmailInput"
          class="w-full sm:w-auto px-5 py-2.5 rounded-xl font-bold text-xs bg-blue-600 hover:bg-blue-500 text-white shadow-lg shadow-blue-500/20 transition-all flex items-center justify-center gap-2 disabled:opacity-50"
        >
          <RefreshCw v-if="isSendingEmail" class="w-4 h-4 animate-spin" />
          <Mail v-else class="w-4 h-4" />
          {{ isSendingEmail ? t('spc.alerts.sending') : t('spc.alerts.sendTest') }}
        </button>
      </div>
      <p class="text-[11px] text-slate-400 dark:text-slate-500 mt-2 italic">
        {{ t('spc.alerts.copyTip') }} <code>C:\Users\ihao_ting.PMR.000\Desktop\MES\EmailOutbox</code> {{ t('spc.alerts.copyTipEnd') }}
      </p>
    </div>

    <!-- Summary Stats Matrix -->
    <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-blue-500/10 text-blue-500 rounded-xl"><AlertTriangle class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">{{ t('spc.alerts.total') }}</div>
          <div class="text-2xl font-black text-slate-800 dark:text-white mt-0.5">{{ stats.total }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-amber-500/10 text-amber-500 rounded-xl"><Clock class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">{{ t('spc.alerts.pending') }}</div>
          <div class="text-2xl font-black text-amber-600 dark:text-amber-400 mt-0.5">{{ stats.pending }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-emerald-500/10 text-emerald-500 rounded-xl"><CheckCircle2 class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">{{ t('spc.alerts.acked') }}</div>
          <div class="text-2xl font-black text-emerald-600 dark:text-emerald-400 mt-0.5">{{ stats.acked }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4">
        <div class="p-3 bg-red-500/10 text-red-500 rounded-xl"><ShieldAlert class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">{{ t('spc.alerts.oos') }}</div>
          <div class="text-2xl font-black text-red-600 dark:text-red-400 mt-0.5">{{ stats.oosCount }}</div>
        </div>
      </div>
      <div class="p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center gap-4 col-span-2 md:col-span-1">
        <div class="p-3 bg-orange-500/10 text-orange-500 rounded-xl"><Zap class="w-6 h-6" /></div>
        <div>
          <div class="text-[11px] text-slate-400 font-bold uppercase">{{ t('spc.alerts.ooc') }}</div>
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
          {{ t('spc.alerts.all') }}
        </button>
        <button
          type="button"
          @click="filterType = 'oos'"
          :class="filterType === 'oos' ? 'bg-red-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-red-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          {{ t('spc.alerts.filterOos') }}
        </button>
        <button
          type="button"
          @click="filterType = 'ooc'"
          :class="filterType === 'ooc' ? 'bg-orange-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-orange-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          {{ t('spc.alerts.filterOoc') }}
        </button>
        <button
          type="button"
          @click="filterType = 'pending'"
          :class="filterType === 'pending' ? 'bg-amber-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-amber-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          {{ t('spc.alerts.filterPending') }}
        </button>
        <button
          type="button"
          @click="filterType = 'acked'"
          :class="filterType === 'acked' ? 'bg-emerald-600 text-white shadow-sm font-bold' : 'text-slate-600 dark:text-slate-400 hover:text-emerald-500'"
          class="px-3.5 py-1.5 rounded-lg text-xs font-medium transition-all whitespace-nowrap"
        >
          {{ t('spc.alerts.filterAcked') }}
        </button>
      </div>

      <!-- Search Box -->
      <div class="relative w-full sm:w-72">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3 text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          data-testid="alerts-searchQuery" v-model="searchQuery"
          type="text"
          class="w-full pl-9 pr-4 py-1.5 text-xs rounded-xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700/80 focus:ring-2 focus:ring-blue-500 text-slate-800 dark:text-slate-200 placeholder:text-slate-400"
          :placeholder="t('spc.alerts.search')"
        />
      </div>
    </div>

    <!-- Main Alerts Table -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-md overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/60 border-b border-slate-200 dark:border-slate-800 text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider">
              <th class="py-3.5 px-4">{{ t('spc.alerts.idType') }}</th>
              <th class="py-3.5 px-4">{{ t('spc.alerts.occurredAt') }}</th>
              <th class="py-3.5 px-4">{{ t('spc.alerts.partProcess') }}</th>
              <th class="py-3.5 px-4">{{ t('spc.alerts.value') }}</th>
              <th class="py-3.5 px-6">{{ t('spc.alerts.violation') }}</th>
              <th class="py-3.5 px-4 text-center">{{ t('spc.alerts.ackStatus') }}</th>
              <th class="py-3.5 px-4 text-right">{{ t('spc.alerts.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/60 text-sm">
            <tr v-if="loading && rows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">{{ t('spc.alerts.loading') }}</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400 font-medium">{{ t('spc.alerts.empty') }}</td>
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
                    <ShieldAlert class="w-3 h-3 text-red-500" /> {{ t('spc.alerts.oos') }}
                  </span>
                  <span
                    v-else
                    class="px-2 py-0.5 rounded-md text-[11px] font-bold bg-orange-500/10 text-orange-600 dark:text-orange-400 border border-orange-500/20 inline-flex items-center gap-1"
                  >
                    <Zap class="w-3 h-3 text-orange-500" /> {{ t('spc.alerts.ooc') }}
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
                  <span class="font-bold text-blue-500">{{ t('spc.alerts.rootCausePrefix') }}</span>{{ r.rootCause }}
                </div>
              </td>

              <!-- Status -->
              <td class="py-3.5 px-4 text-center">
                <span
                  v-if="r.isAcknowledged"
                  class="px-3 py-1 rounded-full text-xs font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/30 inline-flex items-center gap-1.5"
                >
                  <CheckCircle2 class="w-3.5 h-3.5" /> {{ t('spc.alerts.ackComplete') }}
                </span>
                <span
                  v-else
                  class="px-3 py-1 rounded-full text-xs font-bold bg-amber-500/10 text-amber-600 dark:text-amber-400 border border-amber-500/30 inline-flex items-center gap-1.5 animate-pulse"
                >
                  <Clock class="w-3.5 h-3.5" /> {{ t('spc.alerts.pendingAction') }}
                </span>
              </td>

              <!-- Action -->
              <td class="py-3.5 px-4 text-right">
                <div class="flex items-center justify-end gap-2">
                  <button
                    v-if="!r.isAcknowledged"
                    type="button"
                    data-testid="alerts-ack" @click="ack(r.id)"
                    class="px-3 py-1.5 rounded-lg font-bold text-xs bg-blue-600 hover:bg-blue-500 text-white shadow-md shadow-blue-500/20 transition-all flex items-center gap-1.5"
                  >
                    <Check class="w-3.5 h-3.5" /> {{ t('spc.alerts.ackNow') }}
                  </button>
                  <router-link
                    v-if="r.ppcId"
                    :to="`/spc?ppcId=${r.ppcId}`"
                    class="p-1.5 rounded-lg bg-blue-50 dark:bg-blue-900/30 hover:bg-blue-100 dark:hover:bg-blue-900/50 text-blue-600 dark:text-blue-400 transition-colors"
                    :title="t('spc.alerts.viewV1')"
                  >
                    <ExternalLink class="w-4 h-4" />
                  </router-link>
                  <router-link
                    to="/alerts-workflow"
                    class="p-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-600 dark:text-slate-300 transition-colors"
                    :title="t('spc.alerts.goWorkflow')"
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
      <div data-testid="alerts-simulate-dialog" role="dialog" :aria-label="t('spc.alerts.simulateTitle')" class="max-h-[calc(100dvh-2rem)] overflow-y-auto bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 shadow-2xl max-w-lg w-full">
        <div class="p-6 bg-gradient-to-r from-amber-500 to-orange-600 text-white flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-white/20 rounded-2xl backdrop-blur"><Zap class="w-6 h-6 fill-current" /></div>
            <div>
              <h3 class="text-lg font-bold tracking-wide">{{ t('spc.alerts.simulateTitle') }}</h3>
              <p class="text-xs text-orange-100 opacity-90">{{ t('spc.alerts.simulateDescription') }}</p>
            </div>
          </div>
          <button type="button" @click="showSimulateModal = false" class="p-2 rounded-xl bg-black/10 hover:bg-black/20 text-white transition-colors">✕</button>
        </div>

        <div class="p-6 space-y-4">
<LocalePicker />
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.alerts.partId') }}</label>
              <input data-testid="alerts-simulateForm.partId" v-model="simulateForm.partId" type="number" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 font-mono text-sm" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.alerts.processId') }}</label>
              <input data-testid="alerts-simulateForm.processId" v-model="simulateForm.processId" type="number" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 font-mono text-sm" />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.alerts.type') }}</label>
              <select data-testid="alerts-simulateForm.alertType" v-model="simulateForm.alertType" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold">
                <option value="OOS">{{ t('spc.alerts.typeOos') }}</option>
                <option value="OOC">{{ t('spc.alerts.typeOoc') }}</option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.alerts.abnormalValue') }}</label>
              <input data-testid="alerts-simulateForm.actualValue" v-model="simulateForm.actualValue" type="number" step="0.01" class="w-full px-3 py-2 rounded-xl bg-red-50 dark:bg-red-950/40 text-red-600 dark:text-red-400 font-mono font-bold border border-red-300 dark:border-red-900 text-sm" />
            </div>
          </div>

          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.alerts.emailSubject') }}</label>
            <input data-testid="alerts-simulateForm.message" v-model="simulateForm.message" type="text" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-medium" />
          </div>

          <div>
            <label class="block text-xs font-bold text-slate-500 uppercase mb-1">{{ t('spc.alerts.recipient') }}</label>
            <input data-testid="alerts-simulateForm.targetEmail" v-model="simulateForm.targetEmail" type="email" class="w-full px-3 py-2 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 font-mono text-sm" />
          </div>
        </div>

        <div class="p-6 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-100 dark:border-slate-800 flex items-center justify-end gap-3">
          <button type="button" @click="showSimulateModal = false" class="px-4 py-2 rounded-xl text-xs font-bold text-slate-600 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 transition-colors">
            {{ t('spc.alerts.cancel') }}
          </button>
          <button type="button" data-testid="alerts-triggerSimulateAlert" @click="triggerSimulateAlert" class="px-6 py-2.5 rounded-xl text-xs font-bold bg-gradient-to-r from-orange-500 to-red-600 hover:from-orange-400 hover:to-red-500 text-white shadow-lg shadow-orange-500/20 transition-all">
            {{ t('spc.alerts.sendAlert') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
