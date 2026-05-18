<script setup>
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import { CheckCircle2, ShieldAlert, Sparkles, RefreshCw, AlertTriangle, ArrowRight, Table, Server } from "lucide-vue-next";

const route = useRoute();
const router = useRouter();
const batch = ref(null);
const details = ref([]);
const errors = ref([]);
const err = ref("");
const loading = ref(false);
const batchId = computed(() => route.params.batchId);

async function load() {
  err.value = "";
  try {
    const { data } = await api.get(`/uploads/${batchId.value}/preview`);
    batch.value = data.batch;
    details.value = data.details || [];
    errors.value = data.errors || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}

async function confirmImport() {
  if (errors.value.length > 0 && details.value.length === 0) {
    err.value = "無有效資料可匯入。請先修正上述錯誤。";
    return;
  }
  loading.value = true;
  err.value = "";
  try {
    await api.post(`/uploads/${batchId.value}/confirm`);
    await load();
    if (batch.value?.isConfirmed) {
      // Navigate to SPC interactive chart or dashboard
      router.push(`/spc?uploadBatchId=${batchId.value}`);
    }
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="max-w-6xl mx-auto space-y-6">
    <div class="p-8 rounded-3xl bg-gradient-to-r from-violet-600 via-purple-600 to-indigo-700 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-6">
        <div class="space-y-2">
          <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-cyan-200 border border-white/20">
            <Server class="w-3.5 h-3.5" /> Stage 2: 暫存批次校驗與確認匯入
          </div>
          <h1 class="text-3xl font-black tracking-tight">匯入資料檢核與異常對照預覽</h1>
          <p class="text-indigo-100 text-sm max-w-xl">
            批次識別碼：<code class="px-2 py-0.5 rounded bg-black/30 font-mono text-xs">{{ batchId }}</code>
          </p>
        </div>

        <div v-if="batch" class="flex items-center gap-3">
          <span
            class="px-4 py-2 rounded-2xl text-xs font-bold uppercase tracking-widest flex items-center gap-2 shadow-lg"
            :class="batch.isConfirmed ? 'bg-emerald-500 text-white' : errors.length > 0 ? 'bg-amber-500 text-white' : 'bg-blue-600 text-white'"
          >
            <CheckCircle2 v-if="batch.isConfirmed" class="w-4 h-4" />
            <AlertTriangle v-else-if="errors.length > 0" class="w-4 h-4" />
            <Sparkles v-else class="w-4 h-4" />
            {{ batch.isConfirmed ? '已成功匯入正式資料表' : errors.length > 0 ? '含有格式/主檔異常' : ' Stage 2 檢驗通過' }}
          </span>

          <button
            v-if="!batch.isConfirmed"
            @click="confirmImport"
            :disabled="loading || (details.length === 0)"
            class="flex items-center gap-2 px-6 py-3 rounded-2xl bg-white text-indigo-600 hover:bg-indigo-50 font-black shadow-xl shadow-black/20 disabled:opacity-50 transition-all text-sm"
          >
            <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
            <ArrowRight v-else class="w-4 h-4" />
            確認轉入正式 SPC 運算
          </button>
        </div>
      </div>
    </div>

    <div v-if="err" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ err }}
    </div>

    <!-- Error List Card -->
    <div v-if="errors && errors.length > 0" class="p-6 rounded-3xl bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900/50 shadow-sm space-y-4">
      <div class="flex items-center gap-2 text-red-600 dark:text-red-400 font-bold text-lg">
        <ShieldAlert class="w-5 h-5" /> 系統攔截之異常資料列或主檔不符清單 (共 {{ errors.length }} 筆)
      </div>
      <p class="text-xs text-red-500">以下列因缺少對應之料號、製程、機台主檔或數值格式錯誤，在確認匯入時將被自動忽略：</p>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-200 uppercase text-[11px] font-bold">
            <tr>
              <th class="p-3 rounded-l-xl">行號 / 索引</th>
              <th class="p-3">原始數據 (Raw Payload)</th>
              <th class="p-3 rounded-r-xl">錯誤詳細原因</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-red-200 dark:divide-red-900/40 text-red-700 dark:text-red-300">
            <tr v-for="e in errors" :key="e.id">
              <td class="p-3 font-mono font-bold">Row #{{ e.rowNumber > 0 ? e.rowNumber : e.id }}</td>
              <td class="p-3 font-mono text-[11px] max-w-md truncate">{{ e.rawPayload }}</td>
              <td class="p-3 font-semibold">{{ e.errorMessage }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Details Table -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="text-lg font-bold text-slate-800 dark:text-white flex items-center gap-2">
          <Table class="w-5 h-5 text-blue-500" /> 校驗通過待匯入數據預覽 (共 {{ details?.length || 0 }} 筆)
        </h3>
        <span class="text-xs text-slate-400">已自動對應至各料號與製程基準設定</span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-xs">
          <thead class="bg-slate-50 dark:bg-slate-800/50 text-slate-400 dark:text-slate-500 uppercase text-[11px] font-bold tracking-wider border-b border-slate-200 dark:border-slate-800">
            <tr>
              <th class="p-3 rounded-l-xl">批次明細 ID</th>
              <th class="p-3">料號 / 製程代碼</th>
              <th class="p-3">檢測項目代碼</th>
              <th class="p-3">抽樣值 / 檢驗數量</th>
              <th class="p-3">測量時間</th>
              <th class="p-3 rounded-r-xl">操作員</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-600 dark:text-slate-300 font-medium">
            <tr v-for="d in details" :key="d.id" class="hover:bg-slate-50/80 dark:hover:bg-slate-800/40 transition-colors">
              <td class="p-3 font-mono text-slate-400">#{{ d.id }}</td>
              <td class="p-3 font-bold text-slate-800 dark:text-white">[{{ d.partNo }}] {{ d.processCode }}</td>
              <td class="p-3 text-blue-600 dark:text-blue-400 font-semibold">{{ d.characteristicCode }}</td>
              <td class="p-3 font-mono font-bold">
                {{ d.measuredValue !== null ? `值: ${d.measuredValue}` : `不良/總數: ${d.defectQty || 0} / ${d.inspectedQty || 0}` }}
              </td>
              <td class="p-3 font-mono text-slate-400">{{ d.measuredAt ? new Date(d.measuredAt).toLocaleString() : '即時' }}</td>
              <td class="p-3">{{ d.operator || '系統自動' }}</td>
            </tr>
            <tr v-if="!details || details.length === 0">
              <td colspan="6" class="p-8 text-center text-slate-400 text-sm">無任何檢驗數據明細</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
