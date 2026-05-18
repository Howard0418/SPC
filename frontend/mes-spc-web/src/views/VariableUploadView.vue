<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import { UploadCloud, FileText, CheckCircle2, ShieldAlert, Sparkles, ArrowRight, RefreshCw } from "lucide-vue-next";

const router = useRouter();
const fileInput = ref(null);
const selectedFile = ref(null);
const mode = ref("excel"); // 'excel' or 'json'
const jsonInput = ref('[{"PartNo":"P-1001","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"THICKNESS","LotNo":"L-20260518","SampleNo":1,"MeasuredValue":10.05,"MeasuredAt":"2026-05-18T08:00:00","Operator":"OP-01"}]');
const loading = ref(false);
const err = ref("");
const successBatchId = ref("");

function handleFileChange(e) {
  const files = e.target.files;
  if (files && files.length > 0) {
    selectedFile.value = files[0];
    err.value = "";
  }
}

async function uploadExcel() {
  if (!selectedFile.value) {
    err.value = "請先選擇 Excel 或 CSV 檔案";
    return;
  }
  loading.value = true;
  err.value = "";
  const formData = new FormData();
  formData.append("file", selectedFile.value);

  try {
    const res = await api.post("/uploads/variable/excel", formData, {
      headers: { "Content-Type": "multipart/form-data" }
    });
    successBatchId.value = res.data.uploadBatchId;
    router.push(`/uploads/${successBatchId.value}/preview`);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function uploadJson() {
  loading.value = true;
  err.value = "";
  try {
    const payload = JSON.parse(jsonInput.value);
    const res = await api.post("/uploads/variable", payload);
    successBatchId.value = res.data.uploadBatchId;
    router.push(`/uploads/${successBatchId.value}/preview`);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <div class="p-8 rounded-3xl bg-gradient-to-r from-blue-600 via-indigo-600 to-indigo-800 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-cyan-200 border border-white/20">
          <UploadCloud class="w-3.5 h-3.5" /> Stage 1: 計量型量測數據上傳與暫存區
        </div>
        <h1 class="text-3xl font-black tracking-tight">計量型 (Variable) 抽樣檢驗數據上傳</h1>
        <p class="text-indigo-100 text-sm max-w-xl">
          支援廠區自動量測機台匯出之 Excel / CSV 檔案直接上傳，或可通過 JSON 格式傳遞。系統將進行主檔檢核與暫存。
        </p>
      </div>
    </div>

    <!-- Mode Selector -->
    <div class="flex border-b border-slate-200 dark:border-slate-800 gap-4">
      <button
        @click="mode = 'excel'"
        class="pb-3 text-sm font-bold flex items-center gap-2 transition-all"
        :class="mode === 'excel' ? 'text-blue-600 dark:text-blue-400 border-b-2 border-blue-600 dark:border-blue-400' : 'text-slate-400 dark:text-slate-500 hover:text-slate-600'"
      >
        <FileText class="w-4 h-4" /> Excel / CSV 檔案上傳
      </button>
      <button
        @click="mode = 'json'"
        class="pb-3 text-sm font-bold flex items-center gap-2 transition-all"
        :class="mode === 'json' ? 'text-blue-600 dark:text-blue-400 border-b-2 border-blue-600 dark:border-blue-400' : 'text-slate-400 dark:text-slate-500 hover:text-slate-600'"
      >
        <Sparkles class="w-4 h-4" /> JSON 格式直接送出
      </button>
    </div>

    <div v-if="err" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ err }}
    </div>

    <!-- Excel Mode -->
    <div v-if="mode === 'excel'" class="p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
      <div
        @click="fileInput?.click()"
        class="border-2 border-dashed border-slate-300 dark:border-slate-700 hover:border-blue-500 dark:hover:border-blue-500 rounded-3xl p-12 text-center cursor-pointer transition-all bg-slate-50/50 dark:bg-slate-800/40 group flex flex-col items-center justify-center space-y-3"
      >
        <input ref="fileInput" type="file" accept=".xlsx,.xls,.csv" class="hidden" @change="handleFileChange" />
        <div class="p-4 rounded-full bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 group-hover:scale-110 transition-transform">
          <UploadCloud class="w-8 h-8" />
        </div>
        <div>
          <p class="text-sm font-bold text-slate-800 dark:text-white">點擊選擇檔案或將 Excel / CSV 拖曳至此處</p>
          <p class="text-xs text-slate-400 mt-1">支援 XLSX, XLS, CSV 格式。表頭需含：料號、測量值、日期 等欄位</p>
        </div>
        <div v-if="selectedFile" class="mt-4 px-4 py-2 rounded-xl bg-blue-600/10 text-blue-600 dark:text-blue-400 text-xs font-semibold flex items-center gap-2 border border-blue-500/30">
          <CheckCircle2 class="w-4 h-4 text-blue-500" /> 已選擇檔案：{{ selectedFile.name }} ({{ (selectedFile.size / 1024).toFixed(1) }} KB)
        </div>
      </div>

      <div class="flex justify-end">
        <button
          @click="uploadExcel"
          :disabled="loading || !selectedFile"
          class="flex items-center gap-2 px-6 py-3 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold shadow-lg shadow-blue-500/20 disabled:opacity-50 transition-all text-sm"
        >
          <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
          <ArrowRight v-else class="w-4 h-4" />
          {{ loading ? '檔案解析與上傳中...' : '開始上傳並進入 Stage 2 檢核預覽' }}
        </button>
      </div>
    </div>

    <!-- JSON Mode -->
    <div v-if="mode === 'json'" class="p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-bold text-slate-800 dark:text-white">貼上原始檢驗 JSON 陣列</h3>
        <span class="text-xs text-slate-400 font-mono">支援 PartNo, ProcessCode, MachineCode, CharacteristicCode</span>
      </div>
      <textarea v-model="jsonInput" rows="10" class="w-full p-4 rounded-2xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 font-mono text-xs text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-blue-500"></textarea>
      
      <div class="flex justify-end">
        <button
          @click="uploadJson"
          :disabled="loading"
          class="flex items-center gap-2 px-6 py-3 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold shadow-lg shadow-blue-500/20 disabled:opacity-50 transition-all text-sm"
        >
          <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
          <ArrowRight v-else class="w-4 h-4" />
          {{ loading ? '資料傳送與處理中...' : '送出 JSON 並進入 Stage 2 檢核' }}
        </button>
      </div>
    </div>
  </div>
</template>
