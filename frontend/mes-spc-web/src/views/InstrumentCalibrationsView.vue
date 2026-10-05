<script setup>
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
    reminderError.value = "請輸入 0～365 的整數天數。";
    return;
  }
  if (!settingsForm.value.reminderDays.includes(day)) {
    if (settingsForm.value.reminderDays.length >= 12) {
      reminderError.value = "最多選擇 12 個提醒時間。";
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
    instrumentFormErr.value = "請填寫儀器編號。";
    return;
  }
  if (!instrumentForm.value.name.trim()) {
    instrumentFormErr.value = "請填寫儀器名稱。";
    return;
  }
  if (!instrumentForm.value.department.trim()) {
    instrumentFormErr.value = "請填寫部門。";
    return;
  }
  if (!instrumentForm.value.custodianOperatorId) {
    instrumentFormErr.value = "請指定保管人。";
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
      successMsg.value = `儀器 ${payload.code} 更新成功。`;
    } else {
      await api.post("/v1/instruments", payload);
      successMsg.value = `儀器 ${payload.code} 建立成功。`;
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
      recordFormErr.value = "檔案不能超過 10 MB。";
      return;
    }
    recordForm.value.certificateFile = file;
    recordFormErr.value = "";
  }
}

async function saveCalibrationRecord() {
  recordFormErr.value = "";
  if (!recordForm.value.calibrationDate) {
    recordFormErr.value = "請選擇校正日期。";
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

    successMsg.value = `儀器 ${selectedInstrument.value.code} 校正紀錄已成功登記。`;
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
    alert("下載證書失敗：" + getApiErrorMessage(e));
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
    alert("載入設定失敗：" + getApiErrorMessage(e));
    return;
  }
  showSettingsModal.value = true;
}

async function saveSettings() {
  const parts = [...new Set(settingsForm.value.reminderDays)].sort((a, b) => b - a);
  if (parts.length === 0 || parts.length > 12) {
    reminderError.value = "請選擇 1～12 個提醒時間。";
    return;
  }
  if (customReminderDay.value !== "") {
    reminderError.value = "自訂天數尚未加入，請先按「加入」或清空輸入。";
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
    successMsg.value = "校正提醒設定已儲存。";
    chatWebhookUrl.value = "";
    showSettingsModal.value = false;
  } catch (e) {
    alert("儲存設定失敗：" + (e.response?.data?.message || getApiErrorMessage(e)));
  }
}

async function sendTestNotification() {
  const email = testEmail.value.trim();
  const isChat = settingsForm.value.notificationChannel === "SynologyChat";
  if (isChat && !chatWebhookUrl.value.trim() && !settingsForm.value.chatWebhookConfigured) {
    testError.value = "請先貼上 Synology Chat Webhook 網址。";
    return;
  }
  if (!isChat && !email) {
    testError.value = "請先填寫測試收件人 Email。";
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
      else testError.value = res.data?.message || "Chat 傳送失敗，請檢查群組設定。";
      return;
    }
    if (res.data?.success) {
      const data = res.data.data || {};
      testResult.value = `已寄出測試通知至 ${data.recipient || email}（儀器 ${data.instrumentCode || "-"}）。此信不寫入每日提醒紀錄。`;
    } else {
      const folder = res.data?.data?.outboxFolder;
      testError.value = (res.data?.message || "SMTP 未成功。") + (folder ? ` 本機備份：${folder}` : "");
    }
  } catch (e) {
    testError.value = e.response?.data?.message || getApiErrorMessage(e);
  } finally {
    testLoading.value = false;
  }
}

function getStatusBadge(status) {
  switch (status) {
    case "Active": return { label: "正常使用", class: "bg-emerald-100 text-emerald-700 border-emerald-200" };
    case "InCalibration": return { label: "送校中", class: "bg-sky-100 text-sky-700 border-sky-200" };
    case "Inactive": return { label: "已停用", class: "bg-slate-100 text-slate-600 border-slate-200" };
    case "Retired": return { label: "已報廢", class: "bg-rose-100 text-rose-700 border-rose-200" };
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
    return { class: "text-rose-600 font-bold flex items-center gap-1", label: "已逾期" };
  }
  if (dateStr === today) {
    return { class: "text-orange-600 font-bold flex items-center gap-1", label: "今日到期" };
  }
  const windowDays = summary.value.windowDays || 30;
  const [y, m, d] = today.split("-").map(Number);
  const until = new Date(Date.UTC(y, m - 1, d + windowDays)).toISOString().slice(0, 10);
  if (dateStr <= until) {
    return { class: "text-amber-600 flex items-center gap-1", label: "即將到期" };
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
          儀器校正到期管理
        </h1>
        <p class="text-sm text-slate-500 mt-1">管理各部門儀器主檔、校正週期與證書歷史，提供定時到期提醒。</p>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <InstrumentImportPanel :operators="operators" @imported="loadInstruments(); loadSummary()" />
        <button
          @click="openSettingsModal"
          class="px-3 py-2 rounded-lg bg-white hover:bg-cyan-50 text-cyan-700 text-sm font-medium border border-cyan-200 flex items-center gap-2 transition"
        >
          <Sliders class="w-4 h-4 text-cyan-500" />
          提醒天數設定
        </button>
        <button
          @click="openCreateModal"
          class="px-4 py-2 rounded-lg bg-cyan-500 hover:bg-cyan-400 text-white text-sm font-medium flex items-center gap-2 transition shadow-sm shadow-cyan-200"
        >
          <Plus class="w-4 h-4" />
          新增儀器主檔
        </button>
      </div>
    </div>

    <!-- 摘要卡片群 -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div class="p-4 rounded-xl bg-amber-100 border border-amber-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-amber-700 font-medium">即將到期 (30天內)</div>
          <div class="text-2xl font-bold text-amber-600 mt-1">{{ summary.upcomingCount }}</div>
        </div>
        <AlertTriangle class="w-8 h-8 text-amber-400" />
      </div>

      <div class="p-4 rounded-xl bg-orange-100 border border-orange-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-orange-700 font-medium">今日到期</div>
          <div class="text-2xl font-bold text-orange-600 mt-1">{{ summary.dueTodayCount }}</div>
        </div>
        <Clock class="w-8 h-8 text-orange-400" />
      </div>

      <div class="p-4 rounded-xl bg-rose-100 border border-rose-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-rose-700 font-medium">已逾期</div>
          <div class="text-2xl font-bold text-rose-600 mt-1">{{ summary.overdueCount }}</div>
        </div>
        <XCircle class="w-8 h-8 text-rose-400" />
      </div>

      <div class="p-4 rounded-xl bg-sky-100 border border-sky-200 flex items-center justify-between">
        <div>
          <div class="text-xs text-sky-700 font-medium">送校中處理</div>
          <div class="text-2xl font-bold text-sky-600 mt-1">{{ summary.inCalibrationCount }}</div>
        </div>
        <RefreshCw class="w-8 h-8 text-sky-400" />
      </div>
    </div>

    <!-- Alert 訊息 -->
    <div v-if="successMsg" class="p-4 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-700 flex items-center justify-between">
      <div class="flex items-center gap-2">
        <CheckCircle2 class="w-5 h-5 flex-shrink-0" />
        <span>{{ successMsg }}</span>
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
            v-model="searchQuery"
            @keyup.enter="loadInstruments"
            placeholder="搜尋編號或儀器名稱..."
            class="w-full bg-white border border-cyan-200 rounded-lg pl-9 pr-3 py-1.5 text-sm text-slate-800 placeholder-slate-400 focus:outline-none focus:border-cyan-400"
          />
        </div>

        <select
          v-model="filterDepartment"
          @change="loadInstruments"
          class="bg-white border border-cyan-200 rounded-lg px-3 py-1.5 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
        >
          <option value="">全部部門</option>
          <option v-for="d in departments" :key="d" :value="d">{{ d }}</option>
        </select>

        <select
          v-model="filterStatus"
          @change="loadInstruments"
          class="bg-white border border-cyan-200 rounded-lg px-3 py-1.5 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
        >
          <option value="">全部狀態</option>
          <option value="Active">正常使用</option>
          <option value="InCalibration">送校中</option>
          <option value="Inactive">已停用</option>
          <option value="Retired">已報廢</option>
        </select>

        <button
          @click="loadInstruments"
          class="px-3 py-1.5 bg-cyan-500 hover:bg-cyan-400 text-white text-sm rounded-lg transition"
        >
          篩選
        </button>
      </div>

      <div class="text-xs text-slate-500">
        共計 {{ total }} 台儀器
      </div>
    </div>

    <!-- 儀器表格 -->
    <div class="rounded-xl border border-cyan-100 overflow-hidden bg-white shadow-sm">
      <div class="flex flex-wrap items-center justify-between gap-2 px-3 py-2 bg-sky-50 border-b border-cyan-100">
        <p id="instrument-scroll-help" class="text-xs text-slate-600">表格內上下捲動；左右按鈕、底部捲軸或觸控滑動可查看全部欄位。</p>
        <div class="flex gap-2">
          <button type="button" aria-label="儀器表格向左" @click="scrollInstrumentTable(-1)" class="px-3 py-2 rounded-lg border border-cyan-200 bg-white text-cyan-800 text-sm hover:bg-cyan-100">← 向左</button>
          <button type="button" aria-label="儀器表格向右" @click="scrollInstrumentTable(1)" class="px-3 py-2 rounded-lg border border-cyan-200 bg-white text-cyan-800 text-sm hover:bg-cyan-100">向右 →</button>
        </div>
      </div>
      <div ref="instrumentTableViewport" role="region" aria-label="儀器資料表格" aria-describedby="instrument-scroll-help" tabindex="0" class="max-h-[60dvh] overflow-auto overscroll-contain focus-visible:outline focus-visible:outline-2 focus-visible:outline-cyan-500">
        <table class="w-full min-w-[2200px] text-left text-sm border-separate border-spacing-0">
          <thead class="sticky top-0 z-10 bg-cyan-100">
            <tr class="bg-gradient-to-r from-cyan-100 to-sky-100 text-cyan-800 font-semibold border-b border-cyan-100 whitespace-nowrap">
              <th class="py-3 px-4">儀器編號</th>
              <th class="py-3 px-4">儀器名稱</th>
              <th class="py-3 px-4">部門</th>
              <th class="py-3 px-4">放置地點</th>
              <th class="py-3 px-4">週期</th>
              <th class="py-3 px-4">校驗方式</th>
              <th class="py-3 px-4 whitespace-nowrap">量測規格</th>
              <th class="py-3 px-4 whitespace-nowrap">精度</th>
              <th class="py-3 px-4 whitespace-nowrap">備註</th>
              <th class="py-3 px-4 whitespace-nowrap">校驗規範</th>
              <th class="py-3 px-4 whitespace-nowrap">允收標準</th>

              <th class="py-3 px-4">上次校正</th>
              <th class="py-3 px-4">下次校正期限</th>
              <th class="py-3 px-4">使用狀態</th>
              <th class="py-3 px-4 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-cyan-50">
            <tr v-if="loading" class="text-center">
              <td colspan="15" class="py-8 text-slate-400">載入中...</td>
            </tr>
            <tr v-else-if="instruments.length === 0" class="text-center">
              <td colspan="15" class="py-8 text-slate-400">目前尚無符合條件之儀器資料。</td>
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
              <td class="py-3 px-4 text-slate-600">{{ item.cycleMonths }} 個月</td>
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
                    @click="openRecordModal(item)"
                    title="登記校正結果"
                    class="px-2 py-1 rounded bg-cyan-100 hover:bg-cyan-200 text-cyan-700 border border-cyan-200 text-xs font-medium flex items-center gap-1 transition"
                  >
                    <CheckCircle2 class="w-3.5 h-3.5" />
                    登記校正
                  </button>
                  <button
                    @click="openHistoryModal(item)"
                    title="檢視校正歷史與證書"
                    class="px-2 py-1 rounded bg-white hover:bg-sky-50 text-sky-700 border border-sky-200 text-xs font-medium flex items-center gap-1 transition"
                  >
                    <FileText class="w-3.5 h-3.5" />
                    歷史
                  </button>
                  <button
                    @click="openEditModal(item)"
                    title="編輯儀器"
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
      <div role="dialog" aria-modal="true" :aria-label="isEditing ? '編輯儀器主檔' : '新增儀器主檔'" class="bg-white border border-cyan-200 rounded-xl w-full max-w-lg overflow-hidden shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <h3 class="font-bold text-lg text-slate-800 flex items-center gap-2">
            <Clock class="w-5 h-5 text-cyan-500" />
            {{ isEditing ? '編輯儀器主檔' : '新增儀器主檔' }}
          </h3>
          <button @click="showInstrumentModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 space-y-4 max-h-[75vh] overflow-y-auto">
          <div v-if="instrumentFormErr" class="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded text-sm">
            {{ instrumentFormErr }}
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">儀器編號 *</label>
              <input
                v-model="instrumentForm.code"
                placeholder="例如：CAL-001"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">儀器名稱 *</label>
              <input
                v-model="instrumentForm.name"
                placeholder="例如：電子微量分度天平"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">所屬部門 *</label>
              <input
                v-model="instrumentForm.department"
                placeholder="例如：品保組"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">放置地點</label>
              <input
                v-model="instrumentForm.location"
                placeholder="例如：發泡實驗室"
                maxlength="100"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">保管人 *</label>
              <select
                v-model="instrumentForm.custodianOperatorId"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              >
                <option v-for="op in operators" :key="op.id" :value="op.id">
                  {{ op.operatorName }} ({{ op.department || op.operatorCode }})
                </option>
              </select>
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">校驗方式</label>
              <input
                v-model="instrumentForm.calibrationMethod"
                placeholder="例如：外校、內校、免校"
                maxlength="100"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <fieldset class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <legend class="text-sm font-semibold text-slate-700 mb-2">量測與校驗資訊</legend>
            <label class="block text-xs font-medium text-slate-500">量測規格
              <textarea v-model="instrumentForm.measurementSpecification" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">精度
              <textarea v-model="instrumentForm.precision" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">備註
              <textarea v-model="instrumentForm.remarks" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">校驗規範
              <textarea v-model="instrumentForm.calibrationStandard" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
            <label class="block text-xs font-medium text-slate-500">允收標準
              <textarea v-model="instrumentForm.acceptanceCriteria" maxlength="2000" rows="2" class="mt-1 w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800" />
            </label>
          </fieldset>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">校正週期 (月) *</label>
              <input
                type="number"
                min="1"
                max="120"
                v-model.number="instrumentForm.cycleMonths"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">使用狀態</label>
              <select
                v-model="instrumentForm.usageStatus"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              >
                <option value="Active">正常使用 (Active)</option>
                <option value="InCalibration">送校中 (InCalibration)</option>
                <option value="Inactive">已停用 (Inactive)</option>
                <option value="Retired">已報廢 (Retired)</option>
              </select>
            </div>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">上次校正日期 (選填)</label>
              <input
                type="date"
                v-model="instrumentForm.lastCalibrationDate"
                aria-label="上次校正日期"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">下次校正期限（可後補）</label>
              <input
                type="date"
                v-model="instrumentForm.nextCalibrationDate"
                aria-label="下次校正日期"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
          </div>

          <div v-if="isEditing">
            <p class="mb-3 text-xs text-slate-600">可直接修改上次與下次校正日期；儲存會留下異動紀錄，不會改寫校正歷史或自動重算下次日期。</p>
            <label class="block text-xs font-medium text-slate-500 mb-1">變更理由（若已有下次期限再修改時必填）</label>
            <input
              v-model="instrumentForm.reason"
              placeholder="請填寫手動調整原因..."
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            />
          </div>

          <div class="pt-2 border-t border-cyan-100">
            <label class="flex items-center gap-2 text-sm text-slate-600">
              <input type="checkbox" v-model="instrumentForm.includeCustodian" class="rounded bg-white border-cyan-300 text-cyan-600 focus:ring-cyan-400" />
              自動發送提醒 Email 給保管人
            </label>
          </div>
        </div>

        <div class="p-4 bg-sky-50 border-t border-cyan-100 flex justify-end gap-3">
          <button @click="showInstrumentModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">取消</button>
          <button @click="saveInstrument" class="px-4 py-2 rounded-lg bg-cyan-500 text-white text-sm font-medium hover:bg-cyan-400 transition flex items-center gap-2">
            <Save class="w-4 h-4" />
            儲存儀器
          </button>
        </div>
      </div>
    </div>

    <!-- 登記校正 Modal -->
    <div v-if="showRecordModal" class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-white border border-cyan-200 rounded-xl w-full max-w-lg overflow-hidden shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <div>
            <h3 class="font-bold text-lg text-slate-800">登記校正結果</h3>
            <p class="text-xs text-slate-400 mt-0.5">儀器：{{ selectedInstrument?.code }} - {{ selectedInstrument?.name }}</p>
          </div>
          <button @click="showRecordModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 space-y-4">
          <div v-if="recordFormErr" class="p-3 bg-rose-50 border border-rose-200 text-rose-700 rounded text-sm">
            {{ recordFormErr }}
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">本次校正日期 *</label>
              <input
                type="date"
                v-model="recordForm.calibrationDate"
                @change="onCalibrationDateChange"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-500 mb-1">校正結果 *</label>
              <select
                v-model="recordForm.result"
                @change="onCalibrationDateChange"
                class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
              >
                <option value="Passed">合格 (Passed)</option>
                <option value="Failed">不合格 (Failed)</option>
              </select>
            </div>
          </div>

          <div v-if="recordForm.result === 'Passed'">
            <label class="block text-xs font-medium text-slate-500 mb-1">更新後下次校正期限</label>
            <input
              type="date"
              v-model="recordForm.nextDueDate"
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            />
            <small class="text-xs text-slate-500 mt-1 block">已依校正週期 {{ selectedInstrument?.cycleMonths }} 個月自動推算。</small>
          </div>

          <div>
            <label class="block text-xs font-medium text-slate-500 mb-1">校正說明 / 備註</label>
            <textarea
              v-model="recordForm.reason"
              rows="2"
              placeholder="填寫校正廠商、證書編號或異常描述..."
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            ></textarea>
          </div>

          <div>
            <label class="block text-xs font-medium text-slate-500 mb-1">上傳校正證書附件 (PDF / 圖片，上限 10 MB)</label>
            <input
              type="file"
              accept=".pdf,.png,.jpg,.jpeg"
              @change="onCertificateFileSelected"
              class="w-full text-sm text-slate-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-cyan-500 file:text-white hover:file:bg-cyan-400"
            />
          </div>
        </div>

        <div class="p-4 bg-sky-50 border-t border-cyan-100 flex justify-end gap-3">
          <button @click="showRecordModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">取消</button>
          <button @click="saveCalibrationRecord" class="px-4 py-2 rounded-lg bg-emerald-600 text-white text-sm font-medium hover:bg-emerald-500 transition flex items-center gap-2">
            <CheckCircle2 class="w-4 h-4" />
            確認登記
          </button>
        </div>
      </div>
    </div>

    <!-- 校正歷史與證書 Modal -->
    <div v-if="showHistoryModal" class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-white border border-cyan-200 rounded-xl w-full max-w-2xl overflow-hidden shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <div>
            <h3 class="font-bold text-lg text-slate-800">歷次校正與證書紀錄</h3>
            <p class="text-xs text-slate-400 mt-0.5">{{ selectedInstrument?.code }} - {{ selectedInstrument?.name }}</p>
          </div>
          <button @click="showHistoryModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 max-h-[70vh] overflow-y-auto space-y-4">
          <div v-if="historyLoading" class="text-center py-6 text-slate-400">載入歷史紀錄中...</div>
          <div v-else-if="historyRecords.length === 0" class="text-center py-6 text-slate-500">目前尚無歷史校正紀錄。</div>

          <div v-for="rec in historyRecords" :key="rec.id" class="p-4 rounded-xl bg-sky-50 border border-cyan-100 space-y-2">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span :class="['px-2 py-0.5 rounded text-xs font-semibold', rec.result === 'Passed' ? 'bg-emerald-100 text-emerald-700' : 'bg-rose-100 text-rose-700']">
                  {{ rec.result === 'Passed' ? '合格' : '不合格' }}
                </span>
                <span class="font-mono text-sm text-slate-800 font-bold">校正日期：{{ rec.calibrationDate }}</span>
              </div>
              <span class="text-xs text-slate-400">登記人員：{{ rec.createdBy }} ({{ rec.createdAt }})</span>
            </div>

            <div class="text-xs text-slate-500 grid grid-cols-2 gap-2 pt-1 border-t border-cyan-100">
              <div>原到期日：<span class="font-mono text-slate-700">{{ rec.previousDueDate }}</span></div>
              <div>更新後到期日：<span class="font-mono text-slate-700">{{ rec.nextDueDate }}</span></div>
            </div>

            <div v-if="rec.reason" class="text-xs text-slate-600 bg-white p-2 rounded border border-cyan-50">
              備註：{{ rec.reason }}
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
          <button @click="showHistoryModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">關閉</button>
        </div>
      </div>
    </div>

    <!-- 提醒天數設定 Modal -->
    <div v-if="showSettingsModal" class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-white border border-cyan-200 rounded-xl w-full max-w-md max-h-[90dvh] overflow-y-auto shadow-2xl">
        <div class="p-4 border-b border-cyan-100 flex items-center justify-between">
          <h3 class="font-bold text-lg text-slate-800 flex items-center gap-2">
            <Sliders class="w-5 h-5 text-cyan-500" />
            校正提醒天數設定
          </h3>
          <button @click="showSettingsModal = false" class="text-slate-400 hover:text-slate-800"><X class="w-5 h-5" /></button>
        </div>

        <div class="p-5 space-y-4">
          <div>
            <p class="text-sm font-medium text-slate-700 mb-2">什麼時候提醒？（可複選）</p>
            <div class="grid grid-cols-2 gap-2">
              <label v-for="day in reminderOptions" :key="day" class="flex items-center gap-2 rounded-lg border border-cyan-200 p-2 text-sm text-slate-700 cursor-pointer">
                <input v-model="settingsForm.reminderDays" type="checkbox" :value="day" class="rounded text-cyan-600" />
                {{ day === 0 ? '到期當天' : '提前 ' + day + ' 天' }}
              </label>
            </div>
            <div class="flex items-end gap-2 mt-3">
              <label class="flex-1 min-w-0 text-xs text-slate-600">自訂提前天數
                <input v-model="customReminderDay" type="number" min="0" max="365" step="1" placeholder="例如 45" @keydown.enter.prevent="addReminderDay"
                  class="mt-1 w-full rounded-lg border border-cyan-200 px-3 py-2 text-sm" />
              </label>
              <button type="button" @click="addReminderDay" class="rounded-lg bg-cyan-100 px-3 py-2 text-sm text-cyan-800">加入</button>
            </div>
            <button type="button" @click="settingsForm.reminderDays = [30, 7, 0]; customReminderDay = ''; reminderError = ''" class="mt-2 text-xs text-cyan-700 underline">恢復預設（提前 30 天、7 天、到期當天）</button>
            <p v-if="reminderError" role="alert" class="mt-2 text-xs text-rose-600">{{ reminderError }}</p>
          </div>

          <fieldset :disabled="testLoading" class="space-y-2 border-t border-cyan-100 pt-3">
            <legend class="text-sm font-medium text-slate-700">通知方式</legend>
            <div class="grid grid-cols-2 gap-2">
              <label class="flex items-center gap-2 border border-cyan-200 rounded-lg p-3 text-sm text-slate-700">
                <input v-model="settingsForm.notificationChannel" type="radio" value="Email" name="notification-channel" />Email
              </label>
              <label class="flex items-center gap-2 border border-cyan-200 rounded-lg p-3 text-sm text-slate-700">
                <input v-model="settingsForm.notificationChannel" type="radio" value="SynologyChat" name="notification-channel" />Synology Chat
              </label>
            </div>
            <p v-if="settingsForm.notificationChannel === 'Email'" class="text-xs text-slate-500">每日提醒寄給各儀器設定的 Email 收件人。</p>
            <div v-else class="space-y-2">
              <p class="text-xs text-slate-500">每日提醒送到此 Webhook 指定的 Chat 群組。每台儀器每個提醒階段送一則訊息。</p>
              <label class="block text-xs text-slate-600">Synology Chat Webhook 網址
                <input v-model="chatWebhookUrl" type="password" autocomplete="off" :disabled="testLoading"
                  :placeholder="settingsForm.chatWebhookConfigured ? '已設定；留空保留原網址' : '貼上傳入 Webhook 網址'"
                  class="mt-1 w-full border border-cyan-200 rounded-lg px-3 py-2 text-sm" />
              </label>
              <p v-if="settingsForm.chatWebhookConfigured" class="text-xs text-emerald-700">已儲存 Webhook；可直接測試，或貼上新網址替換。</p>
              <details class="text-xs text-slate-500">
                <summary class="cursor-pointer text-cyan-700">如何取得 Webhook 網址？</summary>
                <p class="mt-1">在 Synology Chat 點個人頭像 → 整合（Integration）→ 傳入 Webhook（Incoming Webhooks），新增並選擇接收群組，再複製完整網址貼到上方。</p>
              </details>
            </div>
          </fieldset>

          <div class="pt-2">
            <label class="flex items-center gap-2 text-sm text-slate-600">
              <input type="checkbox" v-model="settingsForm.isEnabled" class="rounded bg-white border-cyan-300 text-cyan-600 focus:ring-cyan-400" />
              啟用每日定時通知 (08:00 台北時間)
            </label>
          </div>

          <div class="pt-3 border-t border-cyan-100 space-y-2">
            <label class="block text-xs font-medium text-slate-400">發送測試校正通知</label>
            <p v-if="settingsForm.notificationChannel === 'Email'" class="text-xs text-slate-500">只寄給下方指定信箱，主旨含【測試】，不寫入到期通知紀錄，也不開啟每日自動寄信。</p>
            <p v-else class="text-xs text-slate-500">測試會立即送一則【測試】訊息到指定群組，不開啟每日排程。新網址需按「儲存設定」才會保留。</p>
            <input
              v-if="settingsForm.notificationChannel === 'Email'"
              v-model="testEmail"
              type="email"
              placeholder="test@pmr.com.tw"
              aria-label="測試通知收件人"
              class="w-full bg-white border border-cyan-200 rounded-lg px-3 py-2 text-sm text-slate-800 focus:outline-none focus:border-cyan-400"
            />
            <button
              type="button"
              @click="sendTestNotification"
              :disabled="testLoading"
              class="w-full px-3 py-2 rounded-lg bg-cyan-500 hover:bg-cyan-400 disabled:opacity-50 text-white text-sm font-medium flex items-center justify-center gap-2 transition"
            >
              <RefreshCw v-if="testLoading" class="w-4 h-4 animate-spin" />
              <Send v-else class="w-4 h-4" />
              {{ testLoading ? "正在傳送測試通知…" : settingsForm.notificationChannel === "SynologyChat" ? "發送測試 Chat 通知" : "發送測試校正通知" }}
            </button>
            <p v-if="testResult" class="text-xs text-emerald-600">{{ testResult }}</p>
            <p v-if="testError" class="text-xs text-rose-600">{{ testError }}</p>
          </div>
        </div>

        <div class="p-4 bg-sky-50 border-t border-cyan-100 flex justify-end gap-3">
          <button @click="showSettingsModal = false" class="px-4 py-2 rounded-lg bg-slate-100 text-slate-600 text-sm font-medium hover:bg-slate-200 transition">取消</button>
          <button @click="saveSettings" :disabled="testLoading" class="px-4 py-2 rounded-lg bg-cyan-500 text-white text-sm font-medium hover:bg-cyan-400 transition flex items-center gap-2">
            <Save class="w-4 h-4" />
            儲存設定
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
