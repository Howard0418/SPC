<script setup>
import { computed, onMounted, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  CalendarClock,
  CheckCircle2,
  Download,
  Mail,
  Send,
  RefreshCw,
  Save,
  Users
} from "lucide-vue-next";

const loading = ref(false);
const saving = ref(false);
const exporting = ref(false);
const sendingNow = ref("");
const err = ref("");
const successMsg = ref("");
const sendNowResult = ref(null);
const operators = ref([]);
const departments = ref([]);
const form = ref({
  department: "",
  recipientOperatorIds: [],
  weeklyEnabled: true,
  weeklyDayOfWeek: 1,
  monthlyEnabled: true,
  monthlyDay: 1,
  sendTime: "08:00",
  isEnabled: true
});

const weekDays = [
  { value: 1, label: "星期一" },
  { value: 2, label: "星期二" },
  { value: 3, label: "星期三" },
  { value: 4, label: "星期四" },
  { value: 5, label: "星期五" },
  { value: 6, label: "星期六" },
  { value: 0, label: "星期日" }
];

const filteredOperators = computed(() => {
  const department = form.value.department;
  return operators.value.filter(user => {
    const hasMail = !!user.email;
    return hasMail && (!department || user.department === department);
  });
});

const selectedCount = computed(() => form.value.recipientOperatorIds.length);

function normalizeTime(value) {
  if (!value) return "08:00";
  return String(value).slice(0, 5);
}

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const { data } = await api.get("/v1/spc-report-settings");
    operators.value = data?.operators || [];
    departments.value = data?.departments || [];
    const schedule = data?.schedule || {};
    form.value = {
      department: schedule.department || "",
      recipientOperatorIds: data?.recipientOperatorIds || [],
      weeklyEnabled: schedule.weeklyEnabled ?? true,
      weeklyDayOfWeek: schedule.weeklyDayOfWeek ?? 1,
      monthlyEnabled: schedule.monthlyEnabled ?? true,
      monthlyDay: schedule.monthlyDay ?? 1,
      sendTime: normalizeTime(schedule.sendTime || "08:00"),
      isEnabled: schedule.isEnabled ?? true
    };
    pruneRecipients();
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function pruneRecipients() {
  const validIds = new Set(filteredOperators.value.map(x => x.id));
  form.value.recipientOperatorIds = form.value.recipientOperatorIds.filter(id => validIds.has(id));
}

function toggleRecipient(id) {
  const ids = new Set(form.value.recipientOperatorIds);
  if (ids.has(id)) ids.delete(id);
  else ids.add(id);
  form.value.recipientOperatorIds = Array.from(ids);
}

function selectAllVisible() {
  form.value.recipientOperatorIds = filteredOperators.value.map(x => x.id);
}

function clearRecipients() {
  form.value.recipientOperatorIds = [];
}

async function save() {
  err.value = "";
  successMsg.value = "";
  sendNowResult.value = null;
  if (form.value.recipientOperatorIds.length === 0) {
    err.value = "請至少勾選一位有 Email 的收件人。";
    return;
  }
  saving.value = true;
  try {
    await api.put("/v1/spc-report-settings", {
      ...form.value,
      weeklyDayOfWeek: Number(form.value.weeklyDayOfWeek),
      monthlyDay: Number(form.value.monthlyDay)
    });
    successMsg.value = "SPC 週報/月報寄送設定已儲存。";
    await load();
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    saving.value = false;
  }
}

async function sendNow(reportType) {
  err.value = "";
  successMsg.value = "";
  sendNowResult.value = null;
  if (form.value.recipientOperatorIds.length === 0) {
    err.value = "請至少勾選一位有 Email 的收件人。";
    return;
  }
  sendingNow.value = reportType;
  try {
    const { data } = await api.post("/v1/spc-report-settings/send-now", {
      reportType,
      recipientOperatorIds: form.value.recipientOperatorIds
    });
    sendNowResult.value = data;
    successMsg.value = `${data.displayType || "報表"}立即寄送完成：成功 ${data.success} 位，失敗 ${data.failed} 位。`;
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    sendingNow.value = "";
  }
}

async function exportExcel() {
  err.value = "";
  exporting.value = true;
  try {
    const end = new Date();
    const start = new Date();
    start.setMonth(start.getMonth() - 1);
    const { data } = await api.get("/v1/spc-report-settings/export", {
      params: { start: start.toISOString(), end: end.toISOString() },
      responseType: "blob"
    });
    const url = URL.createObjectURL(data);
    const a = document.createElement("a");
    a.href = url;
    a.download = `SPC_Item_Overview_${new Date().toISOString().slice(0, 10)}.xlsx`;
    a.click();
    URL.revokeObjectURL(url);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    exporting.value = false;
  }
}

onMounted(load);
</script>

<template>
  <section class="space-y-6">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h2 class="text-2xl font-black text-slate-800 dark:text-white flex items-center gap-2">
          <CalendarClock class="w-6 h-6 text-blue-500" /> SPC 報表寄送設定
        </h2>
        <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">
          設定 SPC 管制項目總覽週報與月報收件人，系統於指定時間匯出 Excel 並寄送。
        </p>
      </div>
      <div class="flex gap-2">
        <button
          type="button"
          @click="load"
          :disabled="loading"
          class="inline-flex items-center gap-2 px-4 py-2 rounded-lg border border-slate-300 dark:border-slate-700 text-sm font-bold text-slate-600 dark:text-slate-200 hover:bg-slate-100 dark:hover:bg-slate-800"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> 重新整理
        </button>
        <button
          type="button"
          @click="exportExcel"
          :disabled="exporting"
          class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-bold shadow-sm"
        >
          <Download class="w-4 h-4" /> 匯出 Excel
        </button>
      </div>
    </div>

    <div v-if="err" class="p-4 rounded-lg bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900 text-sm font-semibold text-red-600 dark:text-red-300">
      {{ err }}
    </div>
    <div v-if="successMsg" class="p-4 rounded-lg bg-emerald-50 dark:bg-emerald-950/40 border border-emerald-200 dark:border-emerald-900 text-sm font-semibold text-emerald-700 dark:text-emerald-300 flex items-center gap-2">
      <CheckCircle2 class="w-4 h-4" /> {{ successMsg }}
    </div>
    <div v-if="sendNowResult" class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg shadow-sm overflow-hidden">
      <div class="px-5 py-3 border-b border-slate-200 dark:border-slate-800 flex flex-wrap items-center justify-between gap-2">
        <div>
          <h3 class="font-black text-slate-800 dark:text-white">立即寄送結果</h3>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
            {{ sendNowResult.displayType }} · {{ sendNowResult.periodLabel }} · {{ sendNowResult.fileName }}
          </p>
        </div>
        <span
          :class="[
            'px-3 py-1 rounded-full text-xs font-black border',
            sendNowResult.failed > 0
              ? 'bg-red-50 text-red-700 border-red-200 dark:bg-red-950/40 dark:text-red-300 dark:border-red-800'
              : 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800'
          ]"
        >
          成功 {{ sendNowResult.success }} / 失敗 {{ sendNowResult.failed }}
        </span>
      </div>
      <div class="divide-y divide-slate-100 dark:divide-slate-800">
        <div
          v-for="recipient in sendNowResult.recipients || []"
          :key="recipient.id"
          class="px-5 py-3 flex flex-wrap items-center justify-between gap-2 text-sm"
        >
          <div class="min-w-0">
            <div class="font-bold text-slate-800 dark:text-slate-100">
              {{ recipient.operatorName }} <span class="text-xs text-slate-400">({{ recipient.operatorCode }})</span>
            </div>
            <div class="text-xs text-slate-500 dark:text-slate-400 truncate">{{ recipient.email }}</div>
          </div>
          <div
            :class="[
              'text-xs font-black',
              recipient.success ? 'text-emerald-600 dark:text-emerald-300' : 'text-red-600 dark:text-red-300'
            ]"
          >
            {{ recipient.success ? '寄送成功' : recipient.errorMessage || '寄送失敗' }}
          </div>
        </div>
      </div>
    </div>

    <form @submit.prevent="save" class="grid grid-cols-1 xl:grid-cols-[minmax(360px,520px)_1fr] gap-6">
      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg shadow-sm p-5 space-y-5">
        <div class="flex items-center gap-2 text-sm font-black text-slate-700 dark:text-slate-200">
          <CalendarClock class="w-4 h-4 text-blue-500" /> 排程
        </div>

        <label class="flex items-center gap-3">
          <input v-model="form.isEnabled" type="checkbox" class="w-4 h-4 accent-blue-600" />
          <span class="text-sm font-bold text-slate-700 dark:text-slate-200">啟用自動寄送</span>
        </label>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-bold text-slate-500 mb-1">寄送時間</label>
            <input v-model="form.sendTime" type="time" class="w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm font-semibold" />
          </div>
          <div>
            <label class="block text-xs font-bold text-slate-500 mb-1">部門</label>
            <select v-model="form.department" @change="pruneRecipients" class="w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm font-semibold">
              <option value="">全部部門</option>
              <option v-for="dept in departments" :key="dept" :value="dept">{{ dept }}</option>
            </select>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <label class="space-y-2 rounded-lg border border-slate-200 dark:border-slate-800 p-3">
            <span class="flex items-center gap-2 text-sm font-bold text-slate-700 dark:text-slate-200">
              <input v-model="form.weeklyEnabled" type="checkbox" class="w-4 h-4 accent-blue-600" /> 週報
            </span>
            <select v-model="form.weeklyDayOfWeek" class="w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm">
              <option v-for="day in weekDays" :key="day.value" :value="day.value">{{ day.label }}</option>
            </select>
          </label>

          <label class="space-y-2 rounded-lg border border-slate-200 dark:border-slate-800 p-3">
            <span class="flex items-center gap-2 text-sm font-bold text-slate-700 dark:text-slate-200">
              <input v-model="form.monthlyEnabled" type="checkbox" class="w-4 h-4 accent-blue-600" /> 月報
            </span>
            <input v-model="form.monthlyDay" type="number" min="1" max="28" class="w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 text-sm" />
          </label>
        </div>

        <button
          type="submit"
          :disabled="saving"
          class="w-full inline-flex items-center justify-center gap-2 px-4 py-3 rounded-lg bg-blue-600 hover:bg-blue-500 disabled:opacity-60 text-white text-sm font-black shadow-sm"
        >
          <Save class="w-4 h-4" /> {{ saving ? "儲存中..." : "儲存寄送設定" }}
        </button>

        <div class="grid grid-cols-2 gap-3 pt-1">
          <button
            type="button"
            @click="sendNow('weekly')"
            :disabled="!!sendingNow"
            class="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-60 text-white text-xs font-black shadow-sm"
          >
            <Send class="w-4 h-4" /> {{ sendingNow === 'weekly' ? '寄送中...' : '立即寄送週報' }}
          </button>
          <button
            type="button"
            @click="sendNow('monthly')"
            :disabled="!!sendingNow"
            class="inline-flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg bg-violet-600 hover:bg-violet-500 disabled:opacity-60 text-white text-xs font-black shadow-sm"
          >
            <Send class="w-4 h-4" /> {{ sendingNow === 'monthly' ? '寄送中...' : '立即寄送月報' }}
          </button>
        </div>
      </div>

      <div class="bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 rounded-lg shadow-sm overflow-hidden">
        <div class="px-5 py-4 border-b border-slate-200 dark:border-slate-800 flex flex-wrap items-center justify-between gap-2">
          <div>
            <h3 class="font-black text-slate-800 dark:text-white flex items-center gap-2">
              <Users class="w-5 h-5 text-indigo-500" /> 收件人
            </h3>
            <p class="text-xs text-slate-400 mt-0.5">已勾選 {{ selectedCount }} 位，僅列出有 Email 的使用者。</p>
          </div>
          <div class="flex gap-2">
            <button type="button" @click="selectAllVisible" class="px-3 py-1.5 rounded-lg bg-indigo-50 dark:bg-indigo-950/40 text-indigo-700 dark:text-indigo-300 text-xs font-bold border border-indigo-200 dark:border-indigo-800">
              全選
            </button>
            <button type="button" @click="clearRecipients" class="px-3 py-1.5 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 text-xs font-bold">
              清除
            </button>
          </div>
        </div>

        <div v-if="loading" class="py-12 text-center text-sm font-bold text-slate-400">載入設定中...</div>
        <div v-else-if="filteredOperators.length === 0" class="py-12 text-center text-sm font-bold text-slate-400">
          此部門沒有可寄送 Email 的使用者
        </div>
        <div v-else class="max-h-[560px] overflow-y-auto divide-y divide-slate-100 dark:divide-slate-800">
          <label
            v-for="user in filteredOperators"
            :key="user.id"
            class="flex items-center gap-3 px-5 py-3 hover:bg-blue-50/60 dark:hover:bg-slate-800/70 cursor-pointer"
          >
            <input
              type="checkbox"
              class="w-4 h-4 accent-blue-600"
              :checked="form.recipientOperatorIds.includes(user.id)"
              @change="toggleRecipient(user.id)"
            />
            <Mail class="w-4 h-4 text-slate-400" />
            <div class="min-w-0 flex-1">
              <div class="text-sm font-bold text-slate-800 dark:text-slate-100 truncate">
                {{ user.operatorName }} <span class="text-xs text-slate-400">({{ user.operatorCode }})</span>
              </div>
              <div class="text-xs text-slate-500 dark:text-slate-400 truncate">
                {{ user.department || "未指派部門" }} · {{ user.email }}
              </div>
            </div>
          </label>
        </div>
      </div>
    </form>
  </section>
</template>
