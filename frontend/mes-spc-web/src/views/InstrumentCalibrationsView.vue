<script setup>
import {useI18n} from "vue-i18n";
import {displayUiMessage} from "../i18n/uiMessage";
import LocalePicker from "../components/LocalePicker.vue";
const {t}=useI18n({useScope:"global"});
import { ref, onMounted, computed, watch } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import InstrumentImportPanel from "../components/InstrumentImportPanel.vue";
import {
  Clock,
  Plus,
  Search,
  Edit,
  FileText,
  Upload,
  Download,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  X,
  Save,
  Sliders,
  Calendar,
  Building2,
  User,
  RefreshCw,
  Send
} from "lucide-vue-next";

const loading = ref(false);
const err = ref("");
const successMsg = ref("");

// 儀器列表與分頁
const instruments = ref([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(100);

// 摘要
const summary = ref({
  asOfDate: "",
  windowDays: 30,
  upcomingCount: 0,
  dueTodayCount: 0,
  overdueCount: 0,
  inCalibrationCount: 0
});

// 篩選條件
const filterDepartment = ref("");
const filterStatus = ref("");
const searchQuery = ref("");

// 系統人員名單（供選擇保管人與收件人）
const operators = ref([]);

// 儀器新增/編輯 Modal
const instrumentTableViewport = ref(null);
function scrollInstrumentTable(direction) {
  const viewport = instrumentTableViewport.value;
  if (viewport) viewport.scrollBy({ left: direction * viewport.clientWidth * 0.8, behavior: "smooth" });
}
const showInstrumentModal = ref(false);
const isEditing = ref(false);
const instrumentForm = ref({
  id: null,
  code: "",
  name: "",
  department: "",
  location: "",
  calibrationMethod: "",
  measurementSpecification: "",
  precision: "",
  remarks: "",
  calibrationStandard: "",
  acceptanceCriteria: "",

  custodianOperatorId: null,
  cycleMonths: 12,
  lastCalibrationDate: "",
  nextCalibrationDate: "",
  usageStatus: "Active",
  includeCustodian: true,
  recipientOperatorIds: [],
  reason: "",
  version: null
});
const instrumentFormErr = ref("");

// 登記校正 Modal
const showRecordModal = ref(false);
const selectedInstrument = ref(null);
const recordForm = ref({
  calibrationDate: new Date().toISOString().slice(0, 10),
  result: "Passed",
  nextDueDate: "",
  reason: "",
  certificateFile: null
});
const recordFormErr = ref("");

// 校正歷史與證書 Modal
const showHistoryModal = ref(false);
const historyRecords = ref([]);
const historyLoading = ref(false);

// 提醒設定 Modal
const showSettingsModal = ref(false);
const settingsForm = ref({
  reminderDays: [30, 7, 0],
  isEnabled: false,
  notificationChannel: "Email",
  chatWebhookConfigured: false
});
const chatWebhookUrl = ref("");
const customReminderDay = ref("");
const reminderError = ref("");
const reminderOptions = computed(() => [...new Set([30, 14, 7, 3, 1, 0, ...settingsForm.value.reminderDays])].sort((a, b) => b - a));
function addReminderDay() {
  const day = Number(customReminderDay.value);
  if (customReminderDay.value === "" || !Number.isInteger(day) || day < 0 || day > 365) {
    reminderError.value = {key:'spc.calibration.reminderInvalid'};
    return;
  }
  if (!settingsForm.value.reminderDays.includes(day)) {
    if (settingsForm.value.reminderDays.length >= 12) {
      reminderError.value = {key:'spc.calibration.reminderLimit'};
      return;
    }
    settingsForm.value.reminderDays.push(day);
  }
  customReminderDay.value = "";
  reminderError.value = "";
}
const testEmail = ref("");
const testLoading = ref(false);
const testResult = ref("");
const testError = ref("");
watch(() => settingsForm.value.notificationChannel, () => { testResult.value = ""; testError.value = ""; });
watch(showSettingsModal, visible => { if (!visible) chatWebhookUrl.value = ""; });

// 部門清單選項
const departments = computed(() => {
  const depts = new Set();
  instruments.value.forEach(i => { if (i.department) depts.add(i.department); });
  operators.value.forEach(o => { if (o.department) depts.add(o.department); });
  return Array.from(depts);
});

async function loadSummary() {
  try {
    const res = await api.get("/v1/instrument-calibrations/summary");
    if (res.data?.success && res.data.data) {
      summary.value = res.data.data;
    }
  } catch (e) {
    console.error("載入摘要失敗", e);
  }
}

async function loadOperators() {
  try {
    const res = await api.get("/operators");
    operators.value = res.data?.filter(x => x.isActive) || [];
  } catch (e) {
    console.error("載入人員清單失敗", e);
  }
}

async function loadInstruments() {
  loading.value = true;
  err.value = "";
  try {
    const params = {
      page: page.value,
      pageSize: pageSize.value,
      department: filterDepartment.value || undefined,
      status: filterStatus.value || undefined,
      search: searchQuery.value.trim() || undefined
    };
    const res = await api.get("/v1/instruments", { params });
    if (res.data?.success) {
      instruments.value = res.data.data || [];
      total.value = res.data.total || 0;
    }
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function openCreateModal() {
  isEditing.value = false;
  instrumentForm.value = {
    id: null,
    code: "",
    name: "",
    department: "",
    location: "",
    calibrationMethod: "",
  measurementSpecification: "",
  precision: "",
  remarks: "",
  calibrationStandard: "",
  acceptanceCriteria: "",

    custodianOperatorId: operators.value[0]?.id || null,
    cycleMonths: 12,
    lastCalibrationDate: "",
    nextCalibrationDate: new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10),
    usageStatus: "Active",
    includeCustodian: true,
    recipientOperatorIds: [],
    reason: "",
    version: null
  };
  instrumentFormErr.value = "";
  showInstrumentModal.value = true;
}

function openEditModal(item) {
  isEditing.value = true;
  instrumentForm.value = {
    id: item.id,
    code: item.code,
    name: item.name,
    department: item.department,
    location: item.location || "",
    calibrationMethod: item.calibrationMethod || "",
    measurementSpecification: item.measurementSpecification || "",
    precision: item.precision || "",
    remarks: item.remarks || "",
    calibrationStandard: item.calibrationStandard || "",
    acceptanceCriteria: item.acceptanceCriteria || "",

    custodianOperatorId: item.custodianOperatorId,
    cycleMonths: item.cycleMonths,
    lastCalibrationDate: item.lastCalibrationDate || "",
    nextCalibrationDate: item.nextCalibrationDate || "",
    usageStatus: item.usageStatus,
    includeCustodian: item.includeCustodian,
    recipientOperatorIds: [...(item.recipientOperatorIds || [])],
    reason: "",
    version: item.version
  };
  instrumentFormErr.value = "";
  showInstrumentModal.value = true;
}

async function saveInstrument() {
  instrumentFormErr.value = "";
  if (!instrumentForm.value.code.trim()) {
    instrumentFormErr.value = {key:'spc.calibration.codeMissing'};
    return;
  }
  if (!instrumentForm.value.name.trim()) {
    instrumentFormErr.value = {key:'spc.calibration.nameMissing'};
    return;
  }
  if (!instrumentForm.value.department.trim()) {
    instrumentFormErr.value = {key:'spc.calibration.departmentMissing'};
    return;
  }
  if (!instrumentForm.value.custodianOperatorId) {
    instrumentFormErr.value = {key:'spc.calibration.custodianMissing'};
    return;
  }

  const payload = {
    code: instrumentForm.value.code.trim(),
    name: instrumentForm.value.name.trim(),
    department: instrumentForm.value.department.trim(),
    location: (instrumentForm.value.location || "").trim() || null,
    calibrationMethod: (instrumentForm.value.calibrationMethod || "").trim() || null,
    measurementSpecification: (instrumentForm.value.measurementSpecification || "").trim() || null,
    precision: (instrumentForm.value.precision || "").trim() || null,
    remarks: (instrumentForm.value.remarks || "").trim() || null,
    calibrationStandard: (instrumentForm.value.calibrationStandard || "").trim() || null,
    acceptanceCriteria: (instrumentForm.value.acceptanceCriteria || "").trim() || null,

    custodianOperatorId: instrumentForm.value.custodianOperatorId,
    cycleMonths: instrumentForm.value.cycleMonths,
    lastCalibrationDate: instrumentForm.value.lastCalibrationDate || null,
    nextCalibrationDate: instrumentForm.value.nextCalibrationDate || null,
    usageStatus: instrumentForm.value.usageStatus,
    recipientOperatorIds: instrumentForm.value.recipientOperatorIds,
    includeCustodian: instrumentForm.value.includeCustodian,
    version: instrumentForm.value.version,
    reason: instrumentForm.value.reason?.trim() || null
  };

  try {
    if (isEditing.value) {
      await api.put(`/v1/instruments/${instrumentForm.value.id}`, payload);
      successMsg.value = {key:"spc.calibration.updated",args:{code:payload.code}};
    } else {
      await api.post("/v1/instruments", payload);
      successMsg.value = {key:"spc.calibration.created",args:{code:payload.code}};
    }
    showInstrumentModal.value = false;
    await Promise.all([loadInstruments(), loadSummary()]);
  } catch (e) {
    instrumentFormErr.value = e.response?.data?.message || getApiErrorMessage(e);
  }
}

function todayTaipei() {
  return summary.value.asOfDate || "";
}

function openRecordModal(item) {
  selectedInstrument.value = item;
  const today = todayTaipei();
  recordForm.value = {
    calibrationDate: today,
    result: "Passed",
    nextDueDate: calculateNextDue(today, item.cycleMonths),
    reason: "",
    certificateFile: null
  };
  recordFormErr.value = "";
  showRecordModal.value = true;
}

function calculateNextDue(dateStr, months) {
  if (!dateStr) return "";
  const [year, month, day] = dateStr.split("-").map(Number);
  const m = Number(months);
  const targetMonthIndex = month - 1 + m;
  const targetYear = year + Math.floor(targetMonthIndex / 12);
  const monthIndex = ((targetMonthIndex % 12) + 12) % 12;
  const lastDay = new Date(Date.UTC(targetYear, monthIndex + 1, 0)).getUTCDate();
  const clamped = Math.min(day, lastDay);
  return `${targetYear}-${String(monthIndex + 1).padStart(2, "0")}-${String(clamped).padStart(2, "0")}`;
}

function onCalibrationDateChange() {
  if (recordForm.value.result === "Passed" && selectedInstrument.value) {
    recordForm.value.nextDueDate = calculateNextDue(recordForm.value.calibrationDate, selectedInstrument.value.cycleMonths);
  }
}

function onCertificateFileSelected(e) {
  const file = e.target.files?.[0];
  if (file) {
    if (file.size > 10 * 1024 * 1024) {
      recordFormErr.value = {key:'spc.calibration.certificateTooLarge'};
      return;
    }
    recordForm.value.certificateFile = file;
    recordFormErr.value = "";
  }
}

async function saveCalibrationRecord() {
  recordFormErr.value = "";
  if (!recordForm.value.calibrationDate) {
    recordFormErr.value = {key:'spc.calibration.dateMissing'};
    return;
  }
  const payload = {
    calibrationDate: recordForm.value.calibrationDate,
    result: recordForm.value.result,
    nextDueDate: recordForm.value.result === "Passed" && recordForm.value.reason?.trim()
      ? recordForm.value.nextDueDate || null
      : null,
    reason: recordForm.value.reason?.trim() || null,
    correctsRecordId: null,
    requestId: crypto.randomUUID(),
    version: selectedInstrument.value.version
  };

  try {
    const res = await api.post(`/v1/instruments/${selectedInstrument.value.id}/calibrations`, payload);
    const recordId = res.data?.data?.id;

    // 上傳證書 (若有選擇檔案)
    if (recordForm.value.certificateFile && recordId) {
      const formData = new FormData();
      formData.append("file", recordForm.value.certificateFile);
      await api.post(`/v1/calibrations/${recordId}/certificates`, formData, {
        headers: { "Content-Type": "multipart/form-data" }
      });
    }

    successMsg.value = {key:"spc.calibration.recordSaved",args:{code:selectedInstrument.value.code}};
    showRecordModal.value = false;
    await Promise.all([loadInstruments(), loadSummary()]);
  } catch (e) {
    recordFormErr.value = e.response?.data?.message || getApiErrorMessage(e);
  }
}

async function openHistoryModal(item) {
  selectedInstrument.value = item;
  historyRecords.value = [];
  historyLoading.value = true;
  showHistoryModal.value = true;
  try {
    const res = await api.get(`/v1/instruments/${item.id}/calibrations`);
    historyRecords.value = res.data?.data || [];
  } catch (e) {
    console.error("載入歷史失敗", e);
  } finally {
    historyLoading.value = false;
  }
}

async function downloadCert(certId, fileName) {
  try {
    const res = await api.get(`/v1/calibration-certificates/${certId}`, { responseType: "blob" });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", fileName);
    document.body.appendChild(link);
    link.click();
    link.remove();
  } catch (e) {
    alert(t('spc.calibration.downloadFailed',{error:getApiErrorMessage(e)}));
  }
}

async function openSettingsModal() {
  try {
    const res = await api.get("/v1/instrument-calibrations/settings");
    if (res.data?.success && res.data.data) {
      settingsForm.value = {
        reminderDays: res.data.data.reminderDays || [30, 7, 0],
        isEnabled: res.data.data.isEnabled,
        notificationChannel: res.data.data.notificationChannel || "Email",
        chatWebhookConfigured: !!res.data.data.chatWebhookConfigured
      };
      chatWebhookUrl.value = "";
      customReminderDay.value = "";
      reminderError.value = "";
      testEmail.value = testEmail.value || "";
      testResult.value = "";
      testError.value = "";
    }
  } catch (e) {
    alert(t('spc.calibration.settingsLoadFailed',{error:getApiErrorMessage(e)}));
    return;
  }
  showSettingsModal.value = true;
}

async function saveSettings() {
  const parts = [...new Set(settingsForm.value.reminderDays)].sort((a, b) => b - a);
  if (parts.length === 0 || parts.length > 12) {
    reminderError.value = {key:'spc.calibration.reminderSelection'};
    return;
  }
  if (customReminderDay.value !== "") {
    reminderError.value = {key:'spc.calibration.reminderPending'};
    return;
  }
  reminderError.value = "";

  try {
    await api.put("/v1/instrument-calibrations/settings", {
      reminderDays: parts,
      isEnabled: settingsForm.value.isEnabled,
      notificationChannel: settingsForm.value.notificationChannel,
      chatWebhookUrl: chatWebhookUrl.value.trim() || undefined
    });
    successMsg.value = {key:'spc.calibration.settingsSaved'};
    chatWebhookUrl.value = "";
    showSettingsModal.value = false;
  } catch (e) {
    alert(t('spc.calibration.settingsSaveFailed',{error:e.response?.data?.message || getApiErrorMessage(e)}));
  }
}

async function sendTestNotification() {
  const email = testEmail.value.trim();
  const isChat = settingsForm.value.notificationChannel === "SynologyChat";
  if (isChat && !chatWebhookUrl.value.trim() && !settingsForm.value.chatWebhookConfigured) {
    testError.value = {key:'spc.calibration.webhookMissing'};
    return;
  }
  if (!isChat && !email) {
    testError.value = {key:'spc.calibration.emailMissing'};
    testResult.value = "";
    return;
  }
  testLoading.value = true;
  testError.value = "";
  testResult.value = "";
  try {
    const res = isChat
      ? await api.post("/v1/instrument-calibrations/test-chat", { chatWebhookUrl: chatWebhookUrl.value.trim() || undefined })
      : await api.post("/v1/instrument-calibrations/test-email", { email });
    if (isChat) {
      if (res.data?.success) testResult.value = res.data.message;
      else testError.value = res.data?.message || {key:'spc.calibration.chatFailed'};
      return;
    }
    if (res.data?.success) {
      const data = res.data.data || {};
      testResult.value = {key:"spc.calibration.testSent",args:{recipient:data.recipient||email,code:data.instrumentCode||"-"}};
    } else {
      const folder = res.data?.data?.outboxFolder;
      testError.value = folder ? {key:res.data?.message?"spc.calibration.backupError":"spc.calibration.smtpBackupFailed",args:{error:res.data?.message,folder}} : (res.data?.message || {key:"spc.calibration.smtpFailed"});
    }
  } catch (e) {
    testError.value = e.response?.data?.message || getApiErrorMessage(e);
  } finally {
    testLoading.value = false;
  }
}

function getStatusBadge(status) {
  switch (status) {
    case "Active": return { label: t('spc.calibration.active'), class: "bg-emerald-100 text-emerald-700 border-emerald-200" };
    case "InCalibration": return { label: t('spc.calibration.inCalibration'), class: "bg-sky-100 text-sky-700 border-sky-200" };
    case "Inactive": return { label: t('spc.calibration.inactive'), class: "bg-slate-100 text-slate-600 border-slate-200" };
    case "Retired": return { label: t('spc.calibration.retired'), class: "bg-rose-100 text-rose-700 border-rose-200" };
    default: return { label: status, class: "bg-slate-100 text-slate-600 border-slate-200" };
  }
}

function getDueDateBadge(dateStr, status) {
  if (status === "Inactive" || status === "Retired") {
    return { class: "text-slate-400" };
  }
  const today = todayTaipei();
  if (!today || !dateStr) return { class: "text-slate-500" };
  if (dateStr < today) {
    return { class: "text-rose-600 font-bold flex items-center gap-1", label: t('spc.calibration.overdue') };
  }
  if (dateStr === today) {
    return { class: "text-orange-600 font-bold flex items-center gap-1", label: t('spc.calibration.dueToday') };
  }
  const windowDays = summary.value.windowDays || 30;
  const [y, m, d] = today.split("-").map(Number);
  const until = new Date(Date.UTC(y, m - 1, d + windowDays)).toISOString().slice(0, 10);
  if (dateStr <= until) {
    return { class: "text-amber-600 flex items-center gap-1", label: t('spc.calibration.dueSoonPlain') };
  }
  return { class: "text-slate-700" };
}

onMounted(async () => {
  await Promise.all([loadSummary(), loadOperators(), loadInstruments()]);
});
</script>

<template>
  <div class="space-y-6 rounded-2xl bg-gradient-to-br from-cyan-50 via-white to-amber-50 p-5 text-slate-800 shadow-sm ring-1 ring-cyan-100" style="color-scheme: light;">
    <!-- Header -->
    <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <div>
        <h1 class="text-2xl font-bold flex items-center gap-2 text-slate-800">
          <Clock class="w-7 h-7 text-cyan-500" />
          {{ t('spc.calibration.title') }}
        </h1>
        <p class="text-sm text-slate-500 mt-1">{{ t('spc.calibration.description') }}</p>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <InstrumentImportPanel :operators="operators" @imported="loadInstruments(); loadSummary()" />
        <button
          data-testid="calibration-openSettingsModal" @click="openSettingsModal"
          class="px-3 py-2 rounded-lg bg-white hover:bg-cyan-50 text-cyan-700 text-sm font-medium border border-cyan-200 flex items-center gap-2 transition"
        >
          <Sliders class="w-4 h-4 text-cyan-500" />
          {{ t('spc.calibration.reminderSettings') }}
        </button>
        <button
          data-testid="calibration-openCreateModal" @click="openCreateModal"
          class="px-4 py-2 rounded-lg bg-cyan-500 hover:bg-cyan-400 text-white text-sm font-medium flex items-center gap-2 transition shadow-sm shadow-cyan-200"
        >
          <Plus class="w-4 h-4" />
          {{ t('spc.calibration.create') }}
        </button>
      </div>
    </div>

    <!-- 摘要卡片群 -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div class="p-4 rounded-xl bg-amber-100 border border-amber-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-amber-700 font-medium">{{ t('spc.calibration.dueSoon') }}</div>
          <div class="text-2xl font-bold text-amber-600 mt-1">{{ summary.upcomingCount }}</div>
        </div>
        <AlertTriangle class="w-8 h-8 text-amber-400" />
      </div>

      <div class="p-4 rounded-xl bg-orange-100 border border-orange-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-orange-700 font-medium">{{ t('spc.calibration.dueToday') }}</div>
          <div class="text-2xl font-bold text-orange-600 mt-1">{{ summary.dueTodayCount }}</div>
        </div>
        <Clock class="w-8 h-8 text-orange-400" />
      </div>

      <div class="p-4 rounded-xl bg-rose-100 border border-rose-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-rose-700 font-medium">{{ t('spc.calibration.overdue') }}</div>
          <div class="text-2xl font-bold text-rose-600 mt-1">{{ summary.overdueCount }}</div>
        </div>
        <XCircle class="w-8 h-8 text-rose-400" />
      </div>

      <div class="p-4 rounded-xl bg-sky-100 border border-sky-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-sky-700 font-medium">{{ t('spc.calibration.inCalibrationSummary') }}</div>
          <div class="text-2xl font-bold text-sky-600 mt-1">{{ summary.inCalibrationCount }}</div>
        </div>
        <RefreshCw class="w-8 h-8 text-sky-400" />
      </div>
    </div>

    <!-- Alert 訊息 -->
    <div v-if="successMsg" class="p-4 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-700 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <CheckCircle2 class="w-5 h-5 flex-shrink-0" />
        <span>{{ displayUiMessage(t,successMsg) }}</span>
      </div>
      <button @click="successMsg = ''" class="text-slate-400 hover:text-slate-700"><X class="w-4 h-4" /></button>
    </div>

    <div v-if="err" class="p-4 rounded-lg bg-rose-50 border border-rose-200 text-rose-700 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <AlertTriangle class="w-5 h-5 flex-shrink-0" />
        <span>{{ err }}</span>
      </div>
      <button @click="err = ''" class="text-slate-400 hover:text-slate-700"><X class="w-4 h-4" /></button>
    </div>

    <!-- 篩選器與搜尋 -->
    <div class="p-4 rounded-xl bg-white/90 border border-cyan-100 flex flex-wrap gap-4 items-center justify-between">
      <div class="flex flex-wrap gap-3 items-center">
        <div class="relative w-64">
          <Search class="w-4 h-4 text-cyan-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            data-testid="calibration-searchQuery" v-model="searchQuery"
            @keyup.enter="loadInstruments"
            :placeholder="t('spc.calibration.search')"
            class="w-full bg-white border border-cyan-200 rounded-lg pl-9 pr-3 py-1.5 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:border-cyan-400"
          />
        </div>

        <select
          data-testid="calibration-filterDepartment" v-model="filterDepartment"
          @change="loadInstruments"
          class="bg-white border border-cyan-200 rounded-lg px-3 py-1.5 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
        >
          <option value="">{{ t('spc.calibration.allDepartments') }}</option>
          <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
        </select>

        <select
          data-testid="calibration-filterStatus" v-model="filterStatus"
          @change="loadInstruments"
          class="bg-white border border-cyan-200 rounded-lg px-3 py-1.5 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
        >
          <option value="">{{ t('spc.calibration.allStatuses') }}</option>
          <option value="Active">{{ t('spc.calibration.active') }}</option>
          <option value="InCalibration">{{ t('spc.calibration.inCalibration') }}</option>
          <option value="Inactive">{{ t('spc.calibration.inactive') }}</option>
          <option value="Retired">{{ t('spc.calibration.retired') }}</option>
        </select>

        <button
          @click="loadInstruments"
          class="px-3 py-1.5 bg-cyan-500 hover:bg-cyan-400 text-white text-sm rounded-lg transition"
        >
          {{ t('spc.calibration.filter') }}
        </button>
      </div>

      <div class="text-xs text-slate-500">
        {{ t('spc.calibration.totalPrefix') }} {{ total }} {{ t('spc.calibration.instrumentUnit') }}
      </div>
    </div>

    <!-- 儀器表格 -->
    <div class="rounded-xl border border-cyan-100 overflow-hidden bg-white shadow-sm">
      <div class="flex flex-wrap items-center justify-between gap-2 px-3 py-2 bg-sky-50 border-b border-cyan-100">
        <p id="instrument-scroll-help" class="text-xs text-slate-600">{{ t('spc.calibration.scrollHelp') }}</p>
        <div class="flex gap-2">
          <button type="button" :aria-label="t('spc.calibration.scrollLeftLabel')" @click="scrollInstrumentTable(-1)" class="px-3 py-2 rounded-lg border border-cyan-200 bg-white text-cyan-800 text-sm hover:bg-cyan-100">{{ t('spc.calibration.scrollLeft') }}</button>
          <button type="button" :aria-label="t('spc.calibration.scrollRightLabel')" @click="scrollInstrumentTable(1)" class="px-3 py-2 rounded-lg border border-cyan-200 bg-white text-cyan-800 text-sm hover:bg-cyan-100">{{ t('spc.calibration.scrollRight') }}</button>
        </div>
      </div>
      <div ref="instrumentTableViewport" role="region" :aria-label="t('spc.calibration.tableLabel')" aria-describedby="instrument-scroll-help" tabindex="0" class="max-h-[60dvh] overflow-auto overscroll-contain focus-visible:outline focus-visible:outline-2 focus-visible:outline-cyan-500">
        <table class="w-full min-w-[2200px] text-left text-sm border-separate border-spacing-0">
          <thead class="sticky top-0 z-10 bg-cyan-100">
            <tr class="bg-gradient-to-r from-cyan-100 to-sky-100 text-cyan-800 font-semibold border-b border-cyan-100 whitespace-nowrap">
              <th class="py-3 px-4">{{ t('spc.calibration.code') }}</th>
              <th class="py-3 px-4">{{ t('spc.calibration.name') }}</th>
              <th class="py-3 px-4">{{ t('spc.calibration.department') }}</th>
              <th class="py-3 px-4">{{ t('spc.calibration.location') }}</th>
              <th class="py-3 px-4">{{ t('spc.calibration.cycle') }}</th>
              <th class="py-3 px-4">{{ t('spc.calibration.method') }}</th>
              <th class="py-3 px-4 whitespace-nowrap">{{ t('spc.calibration.specification') }}</th>
              <th class="py-3 px-4 whitespace-nowrap">{{ t('spc.calibration.precision') }}</th>
              <th class="py-3 px-4 whitespace-nowrap">{{ t('spc.calibration.remarks') }}</th>
              <th class="py-3 px-4 whitespace-nowrap">{{ t('spc.calibration.standard') }}</th>
              <th class="py-3 px-4 whitespace-nowrap">{{ t('spc.calibration.acceptance') }}</th>

              <th class="py-3 px-4">{{ t('spc.calibration.lastCalibration') }}</th>
              <th class="py-3 px-4">{{ t('spc.calibration.nextCalibration') }}</th>
              <th class="py-3 px-4">{{ t('spc.calibration.usageStatus') }}</th>
              <th class="py-3 px-4 text-right">{{ t('spc.calibration.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-cyan-50">
            <tr v-if="loading" class="text-center">
              <td colspan="15" class="py-8 text-slate-400">{{ t('spc.calibration.loading') }}</td>
            </tr>
            <tr v-else-if="instruments.length === 0" class="text-center">
              <td colspan="15" class="py-8 text-slate-400">{{ t('spc.calibration.empty') }}</td>
            </tr>
            <tr
              v-for="item in instruments"
              :key="item.id"
              class="hover:bg-cyan-50/70 transition-colors"
            >
              <td class="py-3 px-4 font-mono font-semibold text-cyan-700">{{ item.code }}</td>
              <td class="py-3 px-4 font-medium text-slate-800">{{ item.name }}</td>
              <td class="py-3 px-4 text-slate-600">{{ item.department }}</td>
              <td class="py-3 px-4 text-slate-600">{{ item.location || '—' }}</td>
              <td class="py-3 px-4 text-slate-600">{{ item.cycleMonths }} {{ t('spc.calibration.months') }}</td>
              <td class="py-3 px-4 text-slate-600">{{ item.calibrationMethod || '—' }}</td>
              <td class="py-3 px-4 min-w-44 max-w-xs whitespace-pre-wrap break-words text-slate-600">{{ item.measurementSpecification || '—' }}</td>
              <td class="py-3 px-4 min-w-44 max-w-xs whitespace-pre-wrap break-words text-slate-600">{{ item.precision || '—' }}</td>
              <td class="py-3 px-4 min-w-44 max-w-xs whitespace-pre-wrap break-words text-slate-600">{{ item.remarks || '—' }}</td>
              <td class="py-3 px-4 min-w-44 max-w-xs whitespace-pre-wrap break-words text-slate-600">{{ item.calibrationStandard || '—' }}</td>
              <td class="py-3 px-4 min-w-44 max-w-xs whitespace-pre-wrap break-words text-slate-600">{{ item.acceptanceCriteria || '—' }}</td>

              <td class="py-3 px-4 text-slate-500 font-mono text-xs">{{ item.lastCalibrationDate || "—" }}</td>
              <td class="py-3 px-4 font-mono text-xs">
                <span :class="getDueDateBadge(item.nextCalibrationDate, item.usageStatus).class">
                  {{ item.nextCalibrationDate || "—" }}
                  <span v-if="getDueDateBadge(item.nextCalibrationDate, item.usageStatus).label" class="ml-1 text-[11px] px-1.5 py-0.5 rounded bg-amber-100 text-amber-700 font-normal">
                    {{ getDueDateBadge(item.nextCalibrationDate, item.usageStatus).label }}
                  </span>
                </span>
              </td>
              <td class="py-3 px-4">
                <span :class="['px-2.5 py-0.5 rounded-full text-xs border font-medium', getStatusBadge(item.usageStatus).class]">
                  {{ getStatusBadge(item.usageStatus).label }}
                </span>
              </td>
              <td class="py-3 px-4 text-right">
                <div class="flex items-center justify-end gap-2">
                  <button
                    data-testid="calibration-openRecordModal" @click="openRecordModal(item)"
                    :title="t('spc.calibration.recordTitle')"
                    class="px-2 py-1 rounded bg-cyan-100 hover:bg-cyan-200 text-cyan-700 border border-cyan-200 text-xs font-medium flex items-center gap-1 transition"
                  >
                    <CheckCircle2 class="w-3.5 h-3.5" />
                    {{ t('spc.calibration.record') }}
                  </button>
                  <button
                    data-testid="calibration-openHistoryModal" @click="openHistoryModal(item)"
                    :title="t('spc.calibration.historyLabel')"
                    class="px-2 py-1 rounded bg-white hover:bg-sky-50 text-sky-700 border border-sky-200 text-xs font-medium flex items-center gap-1 transition"
                  >
                    <FileText class="w-3.5 h-3.5" />
                    {{ t('spc.calibration.history') }}
                  </button>
                  <button
                    data-testid="calibration-openEditModal" @click="openEditModal(item)"
                    :title="t('spc.calibration.edit')"
                    class="p-1 rounded hover:bg-cyan-100 text-cyan-500 hover:text-cyan-700 transition"
                  >
                    <Edit class="w-4 h-4" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 新增 / 編輯 儀器 Modal -->
    <div v-if="showInstrumentModal" class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div data-testid="calibration-instrument-dialog" role="dialog" aria-modal="true" :aria-label="isEditing ? t('spc.calibration.editTitle') : t('spc.calibration.create')" class="bg-white border border-cyan-200 rounded-xl w-full max-w-lg overflow-hidden shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <h3 class="font-bold text-lg text-slate-800 flex items-center gap-2">
            <Clock class="w-5 h-5 text-cyan-500" />
            {{ isEditing ? t('spc.calibration.editTitle') : t('spc.calibration.create') }}
          </h3>
          <button @click="showInstrumentModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 space-y-4 max-h-[75vh] overflow-y-auto">
<LocalePicker />
          <div v-if="instrumentFormErr" class="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded text-sm">
            {{ displayUiMessage(t,instrumentFormErr) }}
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.codeRequired') }}</label>
              <input
                data-testid="calibration-instrumentForm.code" v-model="instrumentForm.code"
                :placeholder="t('spc.calibration.codeExample')"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.nameRequired') }}</label>
              <input
                data-testid="calibration-instrumentForm.name" v-model="instrumentForm.name"
                :placeholder="t('spc.calibration.nameExample')"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.departmentRequired') }}</label>
              <input
                data-testid="calibration-instrumentForm.department" v-model="instrumentForm.department"
                :placeholder="t('spc.calibration.departmentExample')"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.location') }}</label>
              <input
                data-testid="calibration-instrumentForm.location" v-model="instrumentForm.location"
                :placeholder="t('spc.calibration.locationExample')"
                maxlength="100"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.custodianRequired') }}</label>
              <select
                data-testid="calibration-instrumentForm.custodianOperatorId" v-model="instrumentForm.custodianOperatorId"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              >
                <option v-for="op in operators" :key="op.id" :value="op.id">
                  {{ op.operatorName }} ({{ op.department || op.operatorCode }})
                </option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.method') }}</label>
              <input
                data-testid="calibration-instrumentForm.calibrationMethod" v-model="instrumentForm.calibrationMethod"
                :placeholder="t('spc.calibration.methodExample')"
                maxlength="100"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <fieldset class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <legend class="text-sm font-semibold text-slate-700 mb-2">{{ t('spc.calibration.measurementInfo') }}</legend>
            <label class="block text-xs font-medium text-slate-500">{{ t('spc.calibration.specification') }}
              <textarea data-testid="calibration-instrumentForm.measurementSpecification" v-model="instrumentForm.measurementSpecification" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">{{ t('spc.calibration.precision') }}
              <textarea data-testid="calibration-instrumentForm.precision" v-model="instrumentForm.precision" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">{{ t('spc.calibration.remarks') }}
              <textarea data-testid="calibration-instrumentForm.remarks" v-model="instrumentForm.remarks" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">{{ t('spc.calibration.standard') }}
              <textarea data-testid="calibration-instrumentForm.calibrationStandard" v-model="instrumentForm.calibrationStandard" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">{{ t('spc.calibration.acceptance') }}
              <textarea data-testid="calibration-instrumentForm.acceptanceCriteria" v-model="instrumentForm.acceptanceCriteria" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
          </fieldset>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.cycleRequired') }}</label>
              <input
                type="number"
                min="1"
                max="120"
                data-testid="calibration-instrumentForm.cycleMonths" v-model.number="instrumentForm.cycleMonths"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.usageStatus') }}</label>
              <select
                data-testid="calibration-instrumentForm.usageStatus" v-model="instrumentForm.usageStatus"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              >
                <option value="Active">{{ t('spc.calibration.activeOption') }}</option>
                <option value="InCalibration">{{ t('spc.calibration.inCalibrationOption') }}</option>
                <option value="Inactive">{{ t('spc.calibration.inactiveOption') }}</option>
                <option value="Retired">{{ t('spc.calibration.retiredOption') }}</option>
              </select>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.lastDateOptional') }}</label>
              <input
                type="date"
                data-testid="calibration-instrumentForm.lastCalibrationDate" v-model="instrumentForm.lastCalibrationDate"
                :aria-label="t('spc.calibration.lastDate')"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.nextDateOptional') }}</label>
              <input
                type="date"
                data-testid="calibration-instrumentForm.nextCalibrationDate" v-model="instrumentForm.nextCalibrationDate"
                :aria-label="t('spc.calibration.nextDate')"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <div v-if="isEditing">
            <p class="mb-3 text-xs text-slate-600">{{ t('spc.calibration.dateEditHelp') }}</p>
            <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.reasonLabel') }}</label>
            <input
              data-testid="calibration-instrumentForm.reason" v-model="instrumentForm.reason"
              :placeholder="t('spc.calibration.reasonPlaceholder')"
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            />
          </div>

          <div class="pt-2 border-t border-cyan-100">
            <label class="flex items-center gap-2 text-sm text-slate-600">
              <input type="checkbox" data-testid="calibration-instrumentForm.includeCustodian" v-model="instrumentForm.includeCustodian" class="rounded bg-white border-cyan-300 text-cyan-600 focus:ring-cyan-400" />
              {{ t('spc.calibration.notifyCustodian') }}
            </label>
          </div>
        </div>

        <div class="p-4 bg-sky-50 border-t border-cyan-100 flex justify-end gap-3">
          <button @click="showInstrumentModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">{{ t('spc.calibration.cancel') }}</button>
          <button data-testid="calibration-saveInstrument" @click="saveInstrument" class="px-4 py-2 rounded-lg bg-cyan-500 text-white text-sm font-medium hover:bg-cyan-400 transition flex items-center gap-2">
            <Save class="w-4 h-4" />
            {{ t('spc.calibration.saveInstrument') }}
          </button>
        </div>
      </div>
    </div>

    <!-- 登記校正 Modal -->
    <div v-if="showRecordModal" class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div data-testid="calibration-record-dialog" role="dialog" :aria-label="t('spc.calibration.recordTitle')" class="bg-white border border-cyan-200 rounded-xl w-full max-w-lg overflow-hidden shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <div>
            <h3 class="font-bold text-lg text-slate-800">{{ t('spc.calibration.recordTitle') }}</h3>
            <p class="text-xs text-slate-400 mt-0.5">{{ t('spc.calibration.instrumentPrefix') }}{{ selectedInstrument?.code }} - {{ selectedInstrument?.name }}</p>
          </div>
          <button @click="showRecordModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 space-y-4">
<LocalePicker />
          <div v-if="recordFormErr" class="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded text-sm">
            {{ displayUiMessage(t,recordFormErr) }}
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.calibrationDateRequired') }}</label>
              <input
                type="date"
                data-testid="calibration-recordForm.calibrationDate" v-model="recordForm.calibrationDate"
                @change="onCalibrationDateChange"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.resultRequired') }}</label>
              <select
                data-testid="calibration-recordForm.result" v-model="recordForm.result"
                @change="onCalibrationDateChange"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              >
                <option value="Passed">{{ t('spc.calibration.passedOption') }}</option>
                <option value="Failed">{{ t('spc.calibration.failedOption') }}</option>
              </select>
            </div>
          </div>

          <div v-if="recordForm.result === 'Passed'">
            <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.updatedNextDue') }}</label>
            <input
              type="date"
              data-testid="calibration-recordForm.nextDueDate" v-model="recordForm.nextDueDate"
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            />
            <small class="text-xs text-slate-500 mt-1 block">{{ t('spc.calibration.calculated',{months:selectedInstrument?.cycleMonths}) }}</small>
          </div>

          <div>
            <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.recordRemarks') }}</label>
            <textarea
              data-testid="calibration-recordForm.reason" v-model="recordForm.reason"
              rows="2"
              :placeholder="t('spc.calibration.recordRemarksPlaceholder')"
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            ></textarea>
          </div>

          <div>
            <label class="block text-xs font-medium text-slate-500 mb-1">{{ t('spc.calibration.certificateUpload') }}</label>
            <input
              type="file"
              accept=".pdf,.png,.jpg,.jpeg"
              @change="onCertificateFileSelected"
              class="w-full text-sm text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-cyan-500 file:text-white hover:file:bg-cyan-400"
            />
          </div>
        </div>

        <div class="p-4 bg-sky-50 border-t border-cyan-100 flex justify-end gap-3">
          <button @click="showRecordModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">{{ t('spc.calibration.cancel') }}</button>
          <button data-testid="calibration-saveCalibrationRecord" @click="saveCalibrationRecord" class="px-4 py-2 rounded-lg bg-emerald-600 text-white text-sm font-medium hover:bg-emerald-500 transition flex items-center gap-2">
            <CheckCircle2 class="w-4 h-4" />
            {{ t('spc.calibration.confirmRecord') }}
          </button>
        </div>
      </div>
    </div>

    <!-- 校正歷史與證書 Modal -->
    <div v-if="showHistoryModal" class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div data-testid="calibration-history-dialog" role="dialog" :aria-label="t('spc.calibration.historyTitle')" class="bg-white border border-cyan-200 rounded-xl w-full max-w-2xl overflow-hidden shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <div>
            <h3 class="font-bold text-lg text-slate-800">{{ t('spc.calibration.historyTitle') }}</h3>
            <p class="text-xs text-slate-400 mt-0.5">{{ selectedInstrument?.code }} - {{ selectedInstrument?.name }}</p>
          </div>
          <button @click="showHistoryModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 max-h-[70vh] overflow-y-auto space-y-4">
<LocalePicker />
          <div v-if="historyLoading" class="text-center py-6 text-slate-400">{{ t('spc.calibration.historyLoading') }}</div>
          <div v-else-if="historyRecords.length === 0" class="text-center py-6 text-slate-500">{{ t('spc.calibration.historyEmpty') }}</div>

          <div v-for="rec in historyRecords" :key="rec.id" class="p-4 rounded-xl bg-sky-50 border border-cyan-100 space-y-2">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span :class="['px-2 py-0.5 rounded text-xs font-semibold', rec.result === 'Passed' ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700']">
                  {{ rec.result === 'Passed' ? t('spc.calibration.passed') : t('spc.calibration.failed') }}
                </span>
                <span class="font-mono text-sm text-slate-800 font-bold">{{ t('spc.calibration.calibrationDatePrefix') }}{{ rec.calibrationDate }}</span>
              </div>
              <span class="text-xs text-slate-400">{{ t('spc.calibration.recordedBy') }}{{ rec.createdBy }} ({{ rec.createdAt }})</span>
            </div>

            <div class="text-xs text-slate-500 grid grid-cols-2 gap-2 pt-1 border-t border-cyan-100">
              <div>{{ t('spc.calibration.originalDue') }}<span class="font-mono text-slate-700">{{ rec.previousDueDate }}</span></div>
              <div>{{ t('spc.calibration.updatedDue') }}<span class="font-mono text-slate-700">{{ rec.nextDueDate }}</span></div>
            </div>

            <div v-if="rec.reason" class="text-xs text-slate-600 bg-white p-2 rounded border border-cyan-50">
              {{ t('spc.calibration.remarksPrefix') }}{{ rec.reason }}
            </div>

            <!-- 附件下載按鈕 -->
            <div v-if="rec.certificates && rec.certificates.length > 0" class="pt-2 flex flex-wrap gap-2">
              <button
                v-for="cert in rec.certificates"
                :key="cert.id"
                @click="downloadCert(cert.id, cert.originalName)"
                class="px-2.5 py-1 rounded bg-white hover:bg-cyan-50 border border-cyan-200 text-cyan-700 text-xs flex items-center gap-1.5 transition"
              >
                <Download class="w-3.5 h-3.5" />
                {{ cert.originalName }} ({{ Math.round(cert.size / 1024) }} KB)
              </button>
            </div>
          </div>
        </div>

        <div class="p-4 bg-sky-50 border-t border-cyan-100 flex justify-end">
          <button @click="showHistoryModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">{{ t('spc.calibration.close') }}</button>
        </div>
      </div>
    </div>

    <!-- 提醒天數設定 Modal -->
    <div v-if="showSettingsModal" class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div data-testid="calibration-settings-dialog" role="dialog" :aria-label="t('spc.calibration.settingsTitle')" class="bg-white border border-cyan-200 rounded-xl w-full max-w-md max-h-[90dvh] overflow-y-auto shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <h3 class="font-bold text-lg text-slate-800 flex items-center gap-2">
            <Sliders class="w-5 h-5 text-cyan-500" />
            {{ t('spc.calibration.settingsTitle') }}
          </h3>
          <button @click="showSettingsModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 space-y-4">
<LocalePicker />
          <div>
            <p class="text-sm font-medium text-slate-700 mb-2">{{ t('spc.calibration.whenRemind') }}</p>
            <div class="grid grid-cols-2 gap-2">
              <label v-for="day in reminderOptions" :key="day" class="flex items-center gap-2 rounded-lg border border-cyan-200 p-2 text-sm text-slate-700 cursor-pointer">
                <input data-testid="calibration-settingsForm.reminderDays" v-model="settingsForm.reminderDays" type="checkbox" :value="day" class="rounded text-cyan-600" />
                {{ day === 0 ? t('spc.calibration.dueDay') : t('spc.calibration.daysBefore',{day}) }}
              </label>
            </div>
            <div class="flex items-end gap-2 mt-3">
              <label class="flex-1 min-w-0 text-xs text-slate-600">{{ t('spc.calibration.customDays') }}
                <input data-testid="calibration-customReminderDay" v-model="customReminderDay" type="number" min="0" max="365" step="1" :placeholder="t('spc.calibration.customDaysExample')" @keydown.enter.prevent="addReminderDay"
                  class="mt-1 w-full rounded-lg border border-cyan-200 px-3 py-2 text-sm" />
              </label>
              <button type="button" data-testid="calibration-addReminderDay" @click="addReminderDay" class="rounded-lg bg-cyan-100 px-3 py-2 text-sm text-cyan-800">{{ t('spc.calibration.add') }}</button>
            </div>
            <button type="button" @click="settingsForm.reminderDays = [30, 7, 0]; customReminderDay = ''; reminderError = ''" class="mt-2 text-xs text-cyan-700 underline">{{ t('spc.calibration.resetDefaults') }}</button>
            <p v-if="reminderError" role="alert" class="mt-2 text-xs text-rose-600">{{ displayUiMessage(t,reminderError) }}</p>
          </div>

          <fieldset :disabled="testLoading" class="space-y-2 border-t border-cyan-100 pt-3">
            <legend class="text-sm font-medium text-slate-700">{{ t('spc.calibration.notificationChannel') }}</legend>
            <div class="grid grid-cols-2 gap-2">
              <label class="flex items-center gap-2 border border-cyan-200 rounded-lg p-3 text-sm text-slate-700">
                <input data-testid="calibration-settingsForm.notificationChannel" v-model="settingsForm.notificationChannel" type="radio" value="Email" name="notification-channel" />Email
              </label>
              <label class="flex items-center gap-2 border border-cyan-200 rounded-lg p-3 text-sm text-slate-700">
                <input data-testid="calibration-settingsForm.notificationChannel" v-model="settingsForm.notificationChannel" type="radio" value="SynologyChat" name="notification-channel" />Synology Chat
              </label>
            </div>
            <p v-if="settingsForm.notificationChannel === 'Email'" class="text-xs text-slate-500">{{ t('spc.calibration.emailHelp') }}</p>
            <div v-else class="space-y-2">
              <p class="text-xs text-slate-500">{{ t('spc.calibration.chatHelp') }}</p>
              <label class="block text-xs text-slate-600">{{ t('spc.calibration.webhook') }}
                <input data-testid="calibration-chatWebhookUrl" v-model="chatWebhookUrl" type="password" autocomplete="off" :disabled="testLoading"
                  :placeholder="settingsForm.chatWebhookConfigured ? t('spc.calibration.webhookKeep') : t('spc.calibration.webhookPaste')"
                  class="mt-1 w-full border border-cyan-200 rounded-lg px-3 py-2 text-sm" />
              </label>
              <p v-if="settingsForm.chatWebhookConfigured" class="text-xs text-emerald-700">{{ t('spc.calibration.webhookStored') }}</p>
              <details class="text-xs text-slate-500">
                <summary class="cursor-pointer text-cyan-700">{{ t('spc.calibration.webhookHow') }}</summary>
                <p class="mt-1">{{ t('spc.calibration.webhookInstructions') }}</p>
              </details>
            </div>
          </fieldset>

          <div class="pt-2">
            <label class="flex items-center gap-2 text-sm text-slate-600">
              <input type="checkbox" data-testid="calibration-settingsForm.isEnabled" v-model="settingsForm.isEnabled" class="rounded bg-white border-cyan-300 text-cyan-600 focus:ring-cyan-400" />
              {{ t('spc.calibration.enableDaily') }}
            </label>
          </div>

          <div class="pt-3 border-t border-cyan-100 space-y-2">
            <label class="block text-xs font-medium text-slate-400">{{ t('spc.calibration.sendTest') }}</label>
            <p v-if="settingsForm.notificationChannel === 'Email'" class="text-xs text-slate-500">{{ t('spc.calibration.emailTestHelp') }}</p>
            <p v-else class="text-xs text-slate-500">{{ t('spc.calibration.chatTestHelp') }}</p>
            <input
              v-if="settingsForm.notificationChannel === 'Email'"
              data-testid="calibration-testEmail" v-model="testEmail"
              type="email"
              placeholder="test@pmr.com.tw"
              :aria-label="t('spc.calibration.testRecipient')"
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            />
            <button
              type="button"
              data-testid="calibration-sendTestNotification" @click="sendTestNotification"
              :disabled="testLoading"
              class="w-full px-3 py-2 rounded-lg bg-cyan-500 hover:bg-cyan-400 disabled:opacity-50 text-white text-sm font-medium flex items-center justify-center gap-2 transition"
            >
              <RefreshCw v-if="testLoading" class="w-4 h-4 animate-spin" />
              <Send v-else class="w-4 h-4" />
              {{ testLoading ? t('spc.calibration.testSending') : settingsForm.notificationChannel === "SynologyChat" ? t('spc.calibration.testChat') : t('spc.calibration.sendTest') }}
            </button>
            <p v-if="testResult" class="text-xs text-emerald-600">{{ displayUiMessage(t,testResult) }}</p>
            <p v-if="testError" class="text-xs text-rose-600">{{ displayUiMessage(t,testError) }}</p>
          </div>
        </div>

        <div class="p-4 bg-sky-50 border-t border-cyan-100 flex justify-end gap-3">
          <button @click="showSettingsModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">{{ t('spc.calibration.cancel') }}</button>
          <button data-testid="calibration-saveSettings" @click="saveSettings" :disabled="testLoading" class="px-4 py-2 rounded-lg bg-cyan-500 text-white text-sm font-medium hover:bg-cyan-400 transition flex items-center gap-2">
            <Save class="w-4 h-4" />
            {{ t('spc.calibration.saveSettings') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
