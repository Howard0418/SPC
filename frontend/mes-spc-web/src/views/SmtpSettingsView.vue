<script setup>
import { ref, onMounted } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import { Sliders, Mail, Save, CheckCircle2, ShieldAlert, RefreshCw, Send, HardDrive, Server, Key, User } from "lucide-vue-next";

const form = ref({
  host: "localhost",
  port: 25,
  username: "",
  password: "",
  senderEmail: "spc-alert@pmr.com.tw",
  defaultRecipientEmail: "ihao_ting@pmr.com.tw",
  enableSsl: false,
  saveToLocalDisk: true,
  localDiskFolder: "C:\\Users\\ihao_ting.PMR.000\\Desktop\\MES\\EmailOutbox"
});

const loading = ref(false);
const saving = ref(false);
const testLoading = ref(false);
const testEmail = ref("");
const successMsg = ref("");
const errorMsg = ref("");
const testResult = ref(null);

async function loadSettings() {
  loading.value = true;
  errorMsg.value = "";
  try {
    const res = await api.get("/settings/smtp");
    form.value = res.data;
    if (form.value.defaultRecipientEmail) {
      testEmail.value = form.value.defaultRecipientEmail;
    }
  } catch (e) {
    errorMsg.value = "載入 SMTP 設定失敗：" + getApiErrorMessage(e);
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
      localDiskFolder: form.value.localDiskFolder
    };
    const res = await api.post("/settings/smtp", payload);
    successMsg.value = res.data?.message || "SMTP 設定已成功更新並生效！";
    setTimeout(() => { successMsg.value = ""; }, 5000);
  } catch (e) {
    errorMsg.value = "儲存 SMTP 設定失敗：" + getApiErrorMessage(e);
  } finally {
    saving.value = false;
  }
}

async function sendTestEmail() {
  testLoading.value = true;
  testResult.value = null;
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
      recipientEmail: testEmail.value,
      settings: {
        host: form.value.host,
        port: cleanPort,
        username: form.value.username,
        password: form.value.password,
        senderEmail: form.value.senderEmail,
        defaultRecipientEmail: form.value.defaultRecipientEmail,
        enableSsl: form.value.enableSsl,
        saveToLocalDisk: form.value.saveToLocalDisk,
        localDiskFolder: form.value.localDiskFolder
      }
    };
    const res = await api.post("/settings/smtp/test", payload);
    testResult.value = {
      success: res.data.success,
      recipient: res.data.recipient,
      host: res.data.host,
      port: res.data.port,
      outboxFolder: res.data.outboxFolder
    };
  } catch (e) {
    errorMsg.value = "測試郵件發送失敗：" + getApiErrorMessage(e);
  } finally {
    testLoading.value = false;
  }
}

onMounted(() => {
  loadSettings();
});
</script>

<template>
  <div class="max-w-5xl mx-auto space-y-8">
    <!-- Header Banner -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-violet-600 via-indigo-600 to-blue-700 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-80 h-80 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-violet-200 border border-white/20">
          <Sliders class="w-3.5 h-3.5" /> 系統組態配置中心
        </div>
        <h1 class="text-3xl font-black tracking-tight">SMTP 郵件伺服器與異常預警通報設定</h1>
        <p class="text-indigo-100 text-sm max-w-xl">
          管理 SPC 管制界限失控 (OOC) 或規格違規 (OOS) 時自動派發電子郵件之發送器參數與本地備份路徑。
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
          <CheckCircle2 class="w-5 h-5 text-emerald-500 flex-shrink-0" /> {{ successMsg }}
        </div>
        <div v-if="errorMsg" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm font-bold flex items-center gap-3 shadow-sm">
          <ShieldAlert class="w-5 h-5 text-red-500 flex-shrink-0" /> {{ errorMsg }}
        </div>

        <form @submit.prevent="saveSettings" class="p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
          <div class="border-b border-slate-100 dark:border-slate-800 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Server class="w-5 h-5 text-indigo-500" /> 伺服器與連線參數
            </h2>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">SMTP 伺服器主機 (Host)</label>
              <input v-model="form.host" type="text" placeholder="例如: smtp.company.com 或 localhost" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">連接埠 (Port)</label>
              <input v-model="form.port" type="text" placeholder="25 / 587 / 465 / 110s" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100 font-mono" />
              <p class="text-[11px] text-slate-400 dark:text-slate-500 mt-1">💡 支援輸入帶有 's' 的連線埠 (如: 110s)，自動啟用 SSL 加密</p>
            </div>
          </div>

          <div class="flex items-center gap-3 pt-2">
            <input v-model="form.enableSsl" type="checkbox" id="ssl" class="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 border-slate-300" />
            <label for="ssl" class="text-sm font-bold text-slate-700 dark:text-slate-300 cursor-pointer">啟用 SSL / TLS 安全連線加密</label>
          </div>

          <div class="border-b border-slate-100 dark:border-slate-800 pt-4 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Key class="w-5 h-5 text-indigo-500" /> 帳號認證憑證 (若無驗證留空即可)
            </h2>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">登入帳號 (Username)</label>
              <input v-model="form.username" type="text" placeholder="例如: spc-bot" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">登入密碼 (Password)</label>
              <input v-model="form.password" type="password" placeholder="••••••••" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
          </div>

          <div class="border-b border-slate-100 dark:border-slate-800 pt-4 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <Mail class="w-5 h-5 text-indigo-500" /> 寄發與接收信箱設定
            </h2>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">系統寄件人信箱 (Sender Email)</label>
              <input v-model="form.senderEmail" type="email" required placeholder="spc-alert@pmr.com.tw" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">預設警報收件人信箱 (Default Recipient)</label>
              <input v-model="form.defaultRecipientEmail" type="email" required placeholder="manager@pmr.com.tw" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 text-sm focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
            </div>
          </div>

          <div class="border-b border-slate-100 dark:border-slate-800 pt-4 pb-4">
            <h2 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
              <HardDrive class="w-5 h-5 text-indigo-500" /> 本地磁碟郵件歸檔與稽核留存
            </h2>
          </div>
          <div class="space-y-4">
            <div class="flex items-center gap-3">
              <input v-model="form.saveToLocalDisk" type="checkbox" id="local" class="w-4 h-4 rounded text-indigo-600 focus:ring-indigo-500 border-slate-300" />
              <label for="local" class="text-sm font-bold text-slate-700 dark:text-slate-300 cursor-pointer">將發送的 HTML 郵件內容同步存檔於本地伺服器資料夾</label>
            </div>
            <div v-if="form.saveToLocalDisk">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 mb-1">本地儲存路徑 (Local Outbox Directory)</label>
              <input v-model="form.localDiskFolder" type="text" class="w-full px-4 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 font-mono text-xs focus:ring-2 focus:ring-indigo-500 text-slate-800 dark:text-slate-100" />
              <p class="text-[11px] text-slate-400 mt-1">系統將於該路徑下自動產生 .html 信件備份，即便未連接真實外部 SMTP 亦可點擊查看信件排版。</p>
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
              {{ saving ? '儲存組態更新中...' : '儲存設定並寫入 appsettings.json' }}
            </button>
          </div>
        </form>
      </div>

      <!-- Test Section (Col 3) -->
      <div class="space-y-6">
        <div class="p-8 rounded-3xl bg-slate-900 text-white border border-slate-800 shadow-xl space-y-6">
          <div class="space-y-2">
            <h3 class="text-lg font-bold flex items-center gap-2 text-cyan-300">
              <Send class="w-5 h-5 text-cyan-400" /> 郵件連線與發送測試
            </h3>
            <p class="text-xs text-slate-400 leading-relaxed">
              可立即發送一封格式化測試通報信件至指定電子信箱，以確認防火牆、主機位址與認證憑證是否正確無誤。
            </p>
          </div>

          <div class="space-y-3">
            <div>
              <label class="block text-xs font-bold text-slate-300 mb-1">測試信件收件人</label>
              <input v-model="testEmail" type="email" placeholder="test@pmr.com.tw" class="w-full px-4 py-2.5 rounded-xl border border-slate-700 bg-slate-800 text-sm focus:ring-2 focus:ring-cyan-500 text-white" />
            </div>
            <button
              @click="sendTestEmail"
              :disabled="testLoading || !testEmail"
              class="w-full flex items-center justify-center gap-2 px-6 py-3 rounded-xl bg-cyan-600 hover:bg-cyan-500 text-white font-bold shadow-lg shadow-cyan-500/20 disabled:opacity-50 transition-all text-sm"
            >
              <RefreshCw v-if="testLoading" class="w-4 h-4 animate-spin" />
              <Send v-else class="w-4 h-4" />
              {{ testLoading ? '正在傳送測試郵件...' : '立即發送測試郵件' }}
            </button>
          </div>

          <!-- Test Result Box -->
          <div v-if="testResult" class="p-5 rounded-3xl border transition-all space-y-3" :class="testResult.success ? 'bg-emerald-950/40 border-emerald-500/30 text-emerald-300' : 'bg-amber-950/40 border-amber-500/30 text-amber-300'">
            <div class="flex items-center gap-2 font-bold text-base">
              <CheckCircle2 v-if="testResult.success" class="w-5 h-5 text-emerald-400 flex-shrink-0" />
              <ShieldAlert v-else class="w-5 h-5 text-amber-400 flex-shrink-0" />
              {{ testResult.success ? '測試派發作業成功' : '外部 SMTP 伺服器傳送連線未建立' }}
            </div>
            <p class="text-xs leading-relaxed text-slate-300">
              <span v-if="testResult.success">已成功向 <strong>{{ testResult.recipient }}</strong> 發出測試通報信件。</span>
              <span v-else>由於測試主機位置 (<strong>{{ testResult.host }}:{{ testResult.port }}</strong>) 拒絕連線或未回應 (伺服器可能未執行或遭防火牆阻擋)，無法完成外部網路派發。</span>
            </p>

            <div v-if="!testResult.success && form.saveToLocalDisk" class="p-3.5 rounded-2xl bg-cyan-950/60 border border-cyan-500/30 text-cyan-200 text-xs space-y-1.5">
              <p class="font-bold flex items-center gap-1.5 text-cyan-300 text-sm">
                <CheckCircle2 class="w-4 h-4 text-cyan-400 flex-shrink-0" /> 本地 HTML 測試郵件檔案已成功產生！
              </p>
              <p class="text-slate-300 text-[11px] leading-relaxed">系統已順利將信件內容同步備份為網頁檔案，即使在無外部 SMTP 網路環境下，您亦可隨時開啟該路徑查看精美的預警通報信件排版效果：</p>
            </div>

            <div v-if="form.saveToLocalDisk" class="pt-2 border-t border-slate-700/50 text-xs font-mono text-cyan-300 break-all select-all">
              📁 存放路徑：{{ testResult.outboxFolder }}
            </div>
          </div>
        </div>

        <div class="p-6 rounded-3xl bg-indigo-50 dark:bg-slate-900/50 border border-indigo-100 dark:border-slate-800 space-y-3">
          <h4 class="text-xs font-bold text-indigo-900 dark:text-indigo-300 flex items-center gap-2 uppercase tracking-wider">
            💡 關於自動通報與西方電氣規則
          </h4>
          <p class="text-xs text-indigo-700 dark:text-slate-400 leading-relaxed">
            當生產現場即時上傳或批次匯入檢驗數據時，SPC 引擎將於幕後平行計算。一旦符合西方電氣 8 大失控法則或超出 USL / LSL，系統即刻套用上述 SMTP 參數寄發品質警報單。
          </p>
        </div>
      </div>
    </div>
  </div>
</template>
