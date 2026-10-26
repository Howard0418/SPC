<script setup>
import {useI18n} from "vue-i18n";
import {displayUiMessage} from "../i18n/uiMessage";
const {t}=useI18n({useScope:"global"});
import { computed, ref, onMounted } from "vue";
import { api, getApiMessageDisplay, getApiErrorDisplay } from "../api/client";
import { Sliders, Mail, Save, CheckCircle2, ShieldAlert, RefreshCw, Send, HardDrive, Server, Key, User, AlertTriangle } from "lucide-vue-next";

const form = ref({
  host: "localhost",
  port: 25,
  username: "",
  password: "",
  senderEmail: "spc-alert@pmr.com.tw",
  defaultRecipientEmail: "ihao_ting@pmr.com.tw",
  enableSsl: false,
  saveToLocalDisk: true,
  localDiskFolder: "C:\\Users\\ihao_ting.PMR.000\\Desktop\\MES\\EmailOutbox",
  recipientOperatorIds: [],
  recipientDepartment: ""
});

const loading = ref(false);
const saving = ref(false);
const testLoading = ref(false);
const testEmail = ref("");
const operators = ref([]);
const departments = ref([]);
const successMsg = ref("");
const errorMsg = ref("");
const testResult = ref(null);

const filteredOperators = computed(() => operators.value.filter(user =>
  !form.value.recipientDepartment || user.department === form.value.recipientDepartment));
const selectedRecipientCount = computed(() => form.value.recipientOperatorIds.length);

function toggleRecipient(id) {
  const ids = new Set(form.value.recipientOperatorIds);
  if (ids.has(id)) ids.delete(id);
  else ids.add(id);
  form.value.recipientOperatorIds = Array.from(ids);
}

function selectAllRecipients() {
  form.value.recipientOperatorIds = filteredOperators.value.map(x => x.id);
}

function clearRecipients() {
  form.value.recipientOperatorIds = [];
}

function buildTestSettings() {
  const rawPort = String(form.value.port).trim().toLowerCase();
  if (rawPort.includes("s") || rawPort.includes("ssl")) form.value.enableSsl = true;
  const cleanPort = parseInt(rawPort.replace(/\D/g, "")) || 25;
  form.value.port = cleanPort;
  return {
    host: form.value.host, port: cleanPort, username: form.value.username, password: form.value.password,
    senderEmail: form.value.senderEmail, defaultRecipientEmail: form.value.defaultRecipientEmail,
    enableSsl: form.value.enableSsl, saveToLocalDisk: form.value.saveToLocalDisk, localDiskFolder: form.value.localDiskFolder
  };
}

async function loadSettings() {
  loading.value = true;
  errorMsg.value = "";
  try {
    const res = await api.get("/settings/smtp");
    form.value = { ...res.data.smtp, password: "", recipientOperatorIds: res.data.recipientOperatorIds || [], recipientDepartment: "" };
    operators.value = res.data.operators || [];
    departments.value = res.data.departments || [];
    if (form.value.defaultRecipientEmail) {
      testEmail.value = form.value.defaultRecipientEmail;
    }
  } catch (e) {
    errorMsg.value = {key:"spc.settings.smtpLoadFailed",args:{error:getApiErrorDisplay(e)}};
  } finally {
    loading.value = false;
  }
}

async function saveSettings() {
  saving.value = true;
  successMsg.value = "";
  errorMsg.value = "";

  // Parse 's' or 'ssl' suffix in port
  const rawPort = String(form.value.port).trim().toLowerCase();
  if (rawPort.includes("s") || rawPort.includes("ssl")) {
    form.value.enableSsl = true;
  }
  const cleanPort = parseInt(rawPort.replace(/\D/g, "")) || 25;
  form.value.port = cleanPort;

  try {
    const payload = {
      host: form.value.host,
      port: cleanPort,
      username: form.value.username,
      password: form.value.password,
      senderEmail: form.value.senderEmail,
      defaultRecipientEmail: form.value.defaultRecipientEmail,
      enableSsl: form.value.enableSsl,
      saveToLocalDisk: form.value.saveToLocalDisk,
      localDiskFolder: form.value.localDiskFolder,
      recipientOperatorIds: form.value.recipientOperatorIds
    };
    const res = await api.post("/settings/smtp", payload);
    successMsg.value = getApiMessageDisplay(res.data, res.data?.message) || {key:'spc.settings.smtpSaved'};
    setTimeout(() => { successMsg.value = ""; }, 5000);
  } catch (e) {
    errorMsg.value = {key:"spc.settings.smtpSaveFailed",args:{error:getApiErrorDisplay(e)}};
  } finally {
    saving.value = false;
  }
}

async function sendTestEmail() {
  testLoading.value = true;
  testResult.value = null;
  errorMsg.value = "";

  try {
    const payload = {
      recipientEmail: testEmail.value,
      recipientOperatorIds: form.value.recipientOperatorIds,
      settings: buildTestSettings()
    };
    const res = await api.post("/settings/smtp/test", payload);
    testResult.value = {
      success: res.data.success,
      recipients: res.data.recipients || [],
      host: res.data.host,
      port: res.data.port,
      outboxFolder: res.data.outboxFolder
    };
  } catch (e) {
    errorMsg.value = {key:"spc.settings.smtpTestFailed",args:{error:getApiErrorDisplay(e)}};
  } finally {
    testLoading.value = false;
  }
}

async function sendTestAlertEmail() {
  testLoading.value = true;
  testResult.value = null;
  errorMsg.value = "";

  try {
    const payload = {
      recipientEmail: testEmail.value,
      recipientOperatorIds: form.value.recipientOperatorIds,
      settings: buildTestSettings()
    };
    const res = await api.post("/settings/smtp/test-alert", payload);
    testResult.value = {
      success: res.data.success,
      recipients: res.data.recipients || [],
      host: res.data.host,
      port: res.data.port,
      outboxFolder: res.data.outboxFolder
    };
  } catch (e) {
    errorMsg.value = {key:"spc.settings.smtpAlertFailed",args:{error:getApiErrorDisplay(e)}};
  } finally {
    testLoading.value = false;
  }
}

onMounted(() => {
  loadSettings();
});
</script>

<template>
  <div class="w-full max-w-7xl mx-auto space-y-8">
    <!-- Header Banner -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-violet-600 via-indigo-600 to-blue-700 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-80 h-80 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-violet-200 border border-white/20">
          <Sliders class="w-3.5 h-3.5" /> {{ t('spc.settings.configuration') }}
        </div>
        <h1 class="text-3xl font-black tracking-tight">{{ t('spc.settings.smtpTitle') }}</h1>
        <p class="text-indigo-100 text-sm max-w-xl">
          {{ t('spc.settings.smtpDescription') }}
        </p>
      </div>
    </div>

    <div v-if="loading" class="flex items-center justify-center p-12 text-slate-400">
      <RefreshCw class="w-8 h-8 animate-spin" />
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-3 gap-8">
      <!-- Form Section (Col 1 & 2) -->
      <div class="md:col-span-2 space-y-6">
        <div v-if="successMsg" class="p-4 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-600 dark:text-emerald-400 text-sm font-bold flex items-center gap-3 shadow-sm">
          <CheckCircle2 class="w-5 h-5 text-emerald-500 flex-shrink-0" /> {{ displayUiMessage(t,successMsg) }}
        </div>
        <div v-if="errorMsg" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm font-bold flex items-center gap-3 shadow-sm">
          <ShieldAlert class="w-5 h-5 text-red-500 flex-shrink-0" /> {{ displayUiMessage(t,errorMsg) }}
        </div>

        <form @submit.prevent="saveSettings" class="p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
          <div class="border-b border-slate-100 dark:border-slate-800 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Server class="w-5 h-5 text-indigo-500" /> {{ t('spc.settings.connection') }}
            </h2>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">{{ t('spc.settings.host') }}</label>
              <input data-testid="settings-form.host" v-model="form.host" type="text" :placeholder="t('spc.settings.hostExample')" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">{{ t('spc.settings.port') }}</label>
              <input data-testid="settings-form.port" v-model="form.port" type="text" placeholder="25 / 587 / 465 / 110s" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100 font-mono" />
              <p class="text-[11px] text-slate-400 dark:text-slate-500 mt-1">{{ t('spc.settings.portHelp') }}</p>
            </div>
          </div>

          <div class="space-y-3 rounded-2xl border border-indigo-100 dark:border-slate-700 bg-indigo-50/50 dark:bg-slate-950/50 p-4">
            <div class="flex items-center justify-between gap-3">
              <div>
                <h3 class="text-sm font-bold text-slate-800 dark:text-white">{{ t('spc.settings.alertRecipients') }}</h3>
                <p class="text-[11px] text-slate-500 dark:text-slate-400">{{ t('spc.settings.selectedPrefix') }} {{ selectedRecipientCount }} {{ t('spc.settings.recipientFallback') }}</p>
              </div>
              <div class="flex gap-2">
                <button type="button" data-testid="settings-selectAllRecipients" @click="selectAllRecipients" class="text-xs font-bold text-indigo-600 dark:text-indigo-300">{{ t('spc.settings.selectAll') }}</button>
                <button type="button" data-testid="settings-clearRecipients" @click="clearRecipients" class="text-xs font-bold text-slate-500">{{ t('spc.settings.clear') }}</button>
              </div>
            </div>
            <select data-testid="settings-form.recipientDepartment" v-model="form.recipientDepartment" class="w-full px-3 py-2 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-sm">
              <option value="">{{ t('spc.settings.allDepartments') }}</option>
              <option v-for="department in departments" :key="department" :value="department">{{ department }}</option>
            </select>
            <div class="max-h-48 overflow-y-auto grid grid-cols-1 sm:grid-cols-2 gap-2">
              <label v-for="user in filteredOperators" :key="user.id" class="flex items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-white dark:hover:bg-slate-800 text-xs cursor-pointer">
                <input type="checkbox" :checked="form.recipientOperatorIds.includes(user.id)" @change="toggleRecipient(user.id)" class="w-4 h-4 accent-indigo-600" />
                <span class="truncate text-slate-700 dark:text-slate-200">{{ user.operatorName }} ({{ user.operatorCode }})</span>
              </label>
            </div>
            <p v-if="filteredOperators.length === 0" class="text-xs text-slate-500">{{ t('spc.settings.noActiveEmails') }}</p>
          </div>

          <div class="flex items-center gap-3 pt-2">
            <input data-testid="settings-form.enableSsl" v-model="form.enableSsl" type="checkbox" id="ssl" class="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 border-slate-300" />
            <label for="ssl" class="text-sm font-bold text-slate-700 dark:text-slate-300 cursor-pointer">{{ t('spc.settings.enableSsl') }}</label>
          </div>

          <div class="border-b border-slate-100 dark:border-slate-800 pt-4 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Key class="w-5 h-5 text-indigo-500" /> {{ t('spc.settings.credentials') }}
            </h2>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">{{ t('spc.settings.username') }}</label>
              <input data-testid="settings-form.username" v-model="form.username" type="text" :placeholder="t('spc.settings.usernameExample')" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">{{ t('spc.settings.password') }}</label>
              <input data-testid="settings-form.password" v-model="form.password" type="password" :placeholder="form.hasPassword ? t('spc.settings.passwordKeep') : t('spc.settings.passwordEnter')" autocomplete="new-password" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
          </div>

          <div class="border-b border-slate-100 dark:border-slate-800 pt-4 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Mail class="w-5 h-5 text-indigo-500" /> {{ t('spc.settings.mailboxes') }}
            </h2>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">{{ t('spc.settings.sender') }}</label>
              <input data-testid="settings-form.senderEmail" v-model="form.senderEmail" type="email" required placeholder="spc-alert@pmr.com.tw" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">{{ t('spc.settings.defaultRecipient') }}</label>
              <input data-testid="settings-form.defaultRecipientEmail" v-model="form.defaultRecipientEmail" type="email" required placeholder="manager@pmr.com.tw" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
          </div>

          <div class="border-b border-slate-100 dark:border-slate-800 pt-4 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <HardDrive class="w-5 h-5 text-indigo-500" /> {{ t('spc.settings.archive') }}
            </h2>
          </div>
          <div class="space-y-4">
            <div class="flex items-center gap-3">
              <input data-testid="settings-form.saveToLocalDisk" v-model="form.saveToLocalDisk" type="checkbox" id="local" class="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 border-slate-300" />
              <label for="local" class="text-sm font-bold text-slate-700 dark:text-slate-300 cursor-pointer">{{ t('spc.settings.saveLocal') }}</label>
            </div>
            <div v-if="form.saveToLocalDisk">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">{{ t('spc.settings.outbox') }}</label>
              <input data-testid="settings-form.localDiskFolder" v-model="form.localDiskFolder" type="text" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 font-mono text-xs focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
              <p class="text-[11px] text-slate-400 mt-1">{{ t('spc.settings.outboxHelp') }}</p>
            </div>
          </div>

          <div class="pt-4 flex justify-end">
            <button
              type="submit"
              :disabled="saving"
              class="flex items-center gap-2 px-8 py-3 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 hover:to-blue-500 text-white font-bold shadow-lg shadow-indigo-500/20 disabled:opacity-50 transition-all text-sm"
            >
              <RefreshCw v-if="saving" class="w-4 h-4 animate-spin" />
              <Save v-else class="w-4 h-4" />
              {{ saving ? t('spc.settings.savingConfiguration') : t('spc.settings.saveConfiguration') }}
            </button>
          </div>
        </form>
      </div>

      <!-- Test Section (Col 3) -->
      <div class="space-y-6">
        <div class="p-8 rounded-3xl bg-slate-900 text-white border border-slate-800 shadow-xl space-y-6">
          <div class="space-y-2">
            <h3 class="text-lg font-bold flex items-center gap-2 text-cyan-300">
              <Send class="w-5 h-5 text-cyan-400" /> {{ t('spc.settings.testConnection') }}
            </h3>
            <p class="text-xs text-slate-400 leading-relaxed">
              {{ t('spc.settings.testConnectionHelp') }}
            </p>
          </div>

          <div class="space-y-3">
            <div>
              <label class="block text-xs font-bold text-slate-300 mb-1">{{ t('spc.settings.testRecipient') }}</label>
              <input data-testid="settings-testEmail" v-model="testEmail" type="email" placeholder="test@pmr.com.tw" class="w-full px-4 py-2.5 rounded-xl border border-slate-700 bg-slate-800 text-sm focus:ring-2 focus:ring-cyan-500 text-white" />
            </div>
            <button
              data-testid="settings-sendTestEmail" @click="sendTestEmail"
              :disabled="testLoading || (!testEmail && selectedRecipientCount === 0)"
              class="w-full flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-cyan-600 hover:bg-cyan-500 text-white font-bold shadow-lg shadow-cyan-500/20 disabled:opacity-50 transition-all text-sm"
            >
              <RefreshCw v-if="testLoading" class="w-4 h-4 animate-spin" />
              <Send v-else class="w-4 h-4" />
              {{ testLoading ? t('spc.settings.testSending') : t('spc.settings.testStandard') }}
            </button>
            <button
              data-testid="settings-sendTestAlertEmail" @click="sendTestAlertEmail"
              :disabled="testLoading || (!testEmail && selectedRecipientCount === 0)"
              class="w-full flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-rose-600 hover:bg-rose-500 text-white font-bold shadow-lg shadow-rose-500/20 disabled:opacity-50 transition-all text-sm"
            >
              <RefreshCw v-if="testLoading" class="w-4 h-4 animate-spin" />
              <AlertTriangle v-else class="w-4 h-4" />
              {{ testLoading ? t('spc.settings.alertSending') : t('spc.settings.testAlert') }}
            </button>
          </div>

          <!-- Test Result Box -->
          <div v-if="testResult" class="p-5 rounded-3xl border transition-all space-y-3" :class="testResult.success ? 'bg-emerald-950/40 border-emerald-500/30 text-emerald-300' : 'bg-amber-950/40 border-amber-500/30 text-amber-300'">
            <div class="flex items-center gap-2 font-bold text-base">
              <CheckCircle2 v-if="testResult.success" class="w-5 h-5 text-emerald-400 flex-shrink-0" />
              <ShieldAlert v-else class="w-5 h-5 text-amber-400 flex-shrink-0" />
              {{ testResult.success ? t('spc.settings.testSuccess') : t('spc.settings.testConnectionFailed') }}
            </div>
            <p class="text-xs leading-relaxed text-slate-300">
              <span v-if="testResult.success">{{ t('spc.settings.completedPrefix') }} {{ testResult.recipients?.length || 0 }} {{ t('spc.settings.completedSuffix') }}</span>
              <span v-else>{{ t('spc.settings.hostFailedPrefix') }}<strong>{{ testResult.host }}:{{ testResult.port }}</strong>{{ t('spc.settings.hostFailedSuffix') }}</span>
            </p>

            <div v-if="testResult.recipients?.length" class="space-y-1 text-xs">
              <div v-for="recipient in testResult.recipients" :key="recipient.email" class="flex justify-between gap-2">
                <span class="truncate">{{ recipient.name }} ({{ recipient.email }})</span>
                <span :class="recipient.success ? 'text-emerald-300' : 'text-rose-300'">{{ recipient.success ? t('spc.settings.succeeded') : (recipient.errorMessage || t('spc.settings.failed')) }}</span>
              </div>
            </div>

            <div v-if="!testResult.success && form.saveToLocalDisk" class="p-3.5 rounded-2xl bg-cyan-950/60 border border-cyan-500/30 text-cyan-200 text-xs space-y-1.5">
              <p class="font-bold flex items-center gap-1.5 text-cyan-300 text-sm">
                <CheckCircle2 class="w-4 h-4 text-cyan-400 flex-shrink-0" /> {{ t('spc.settings.localCopyCreated') }}
              </p>
              <p class="text-slate-300 text-[11px] leading-relaxed">{{ t('spc.settings.localCopyHelp') }}</p>
            </div>

            <div v-if="form.saveToLocalDisk" class="pt-2 border-t border-slate-700/50 text-xs font-mono text-cyan-300 break-all select-all">
              {{ t('spc.settings.folder') }}{{ testResult.outboxFolder }}
            </div>
          </div>
        </div>

        <div class="p-6 rounded-3xl bg-indigo-50 dark:bg-slate-900/50 border border-indigo-100 dark:border-slate-800 space-y-3">
          <h4 class="text-xs font-bold text-indigo-900 dark:text-indigo-300 flex items-center gap-2 uppercase tracking-wider">
            {{ t('spc.settings.rulesTitle') }}
          </h4>
          <p class="text-xs text-indigo-700 dark:text-slate-400 leading-relaxed">
            {{ t('spc.settings.rulesHelp') }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
