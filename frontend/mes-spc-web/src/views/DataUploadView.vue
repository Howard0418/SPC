<script setup>
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import {
  AlertTriangle,
  ArrowRight,
  CheckCircle2,
  FileSearch,
  RefreshCw,
  ShieldAlert,
  Table,
  Wrench
} from "lucide-vue-next";

const router = useRouter();
const loading = ref(false);
const err = ref("");
const batches = ref([]);

const total = computed(() => batches.value.length);
const imported = computed(() => batches.value.filter(x => x.importStatus === "Imported").length);
const failed = computed(() => batches.value.filter(x => Number(x.errorRows || 0) > 0).length);

async function load() {
  loading.value = true;
  err.value = "";
  try {
    const { data } = await api.get("/uploads", { params: { take: 80, _: Date.now() } });
    batches.value = data.batches || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function formatDate(value) {
  if (!value) return "-";
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? value : d.toLocaleString();
}

function statusClass(value) {
  if (value === "Imported") return "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/30 dark:text-emerald-300 dark:border-emerald-800";
  if (value === "PreviewReady") return "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/30 dark:text-blue-300 dark:border-blue-800";
  return "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-950/30 dark:text-amber-300 dark:border-amber-800";
}

function openPreview(id) {
  router.push(`/uploads/${id}/preview`);
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <section class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm overflow-hidden">
      <div class="p-6 flex flex-col lg:flex-row lg:items-center justify-between gap-4 border-b border-slate-100 dark:border-slate-800">
        <div>
          <p class="text-xs font-black uppercase tracking-widest text-blue-600 dark:text-blue-400">TransFiles 直接匯入</p>
          <h1 class="mt-1 text-2xl font-black text-slate-900 dark:text-white">匯入批次與錯誤追蹤</h1>
          <p class="mt-2 text-sm text-slate-500 dark:text-slate-400">
            SPC 網站目前不提供 Excel 上傳匯入；請使用 TransFiles 轉檔工具執行匯入，完成後在此確認批次、錯誤與製圖入口。
          </p>
        </div>
        <button
          type="button"
          @click="load"
          :disabled="loading"
          class="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 disabled:opacity-50 text-white text-sm font-bold"
        >
          <RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" />
          重新整理
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3">
        <div class="p-5 border-b md:border-b-0 md:border-r border-slate-100 dark:border-slate-800">
          <p class="text-xs text-slate-400 font-bold uppercase">近期批次</p>
          <div class="mt-1 text-3xl font-black text-slate-900 dark:text-white">{{ total }}</div>
        </div>
        <div class="p-5 border-b md:border-b-0 md:border-r border-slate-100 dark:border-slate-800">
          <p class="text-xs text-slate-400 font-bold uppercase">已匯入</p>
          <div class="mt-1 text-3xl font-black text-emerald-600 dark:text-emerald-400">{{ imported }}</div>
        </div>
        <div class="p-5">
          <p class="text-xs text-slate-400 font-bold uppercase">含錯誤</p>
          <div class="mt-1 text-3xl font-black text-amber-600 dark:text-amber-400">{{ failed }}</div>
        </div>
      </div>
    </section>

    <div v-if="err" class="p-4 rounded-xl bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-300 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ err }}
    </div>

    <section class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm overflow-hidden">
      <div class="p-5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
        <h2 class="text-lg font-black text-slate-900 dark:text-white flex items-center gap-2">
          <Table class="w-5 h-5 text-blue-600" /> 匯入批次
        </h2>
        <span class="text-xs text-slate-400">來源包含 TransFiles 與既有批次紀錄</span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left text-sm">
          <thead class="bg-slate-50 dark:bg-slate-800/60 text-xs uppercase text-slate-400">
            <tr>
              <th class="px-4 py-3">時間</th>
              <th class="px-4 py-3">來源</th>
              <th class="px-4 py-3">檔案</th>
              <th class="px-4 py-3">狀態</th>
              <th class="px-4 py-3 text-right">總筆數</th>
              <th class="px-4 py-3 text-right">成功</th>
              <th class="px-4 py-3 text-right">錯誤</th>
              <th class="px-4 py-3">第一筆錯誤</th>
              <th class="px-4 py-3 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
            <tr v-for="b in batches" :key="b.uploadBatchId" class="hover:bg-slate-50 dark:hover:bg-slate-800/40">
              <td class="px-4 py-3 whitespace-nowrap text-slate-600 dark:text-slate-300">{{ formatDate(b.confirmedAt || b.createdAt) }}</td>
              <td class="px-4 py-3 font-bold text-slate-700 dark:text-slate-200">{{ b.sourceType || "-" }}</td>
              <td class="px-4 py-3 max-w-sm truncate">{{ b.originalFileName || "TransFiles 直接匯入" }}</td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center gap-1 px-2 py-1 rounded-lg border text-xs font-black" :class="statusClass(b.importStatus)">
                  <CheckCircle2 v-if="b.importStatus === 'Imported'" class="w-3.5 h-3.5" />
                  <AlertTriangle v-else class="w-3.5 h-3.5" />
                  {{ b.importStatus }}
                </span>
              </td>
              <td class="px-4 py-3 text-right font-mono">{{ b.totalRows || 0 }}</td>
              <td class="px-4 py-3 text-right font-mono text-emerald-600">{{ b.validRows || 0 }}</td>
              <td class="px-4 py-3 text-right font-mono" :class="Number(b.errorRows || 0) > 0 ? 'text-amber-600 font-black' : 'text-slate-400'">{{ b.errorRows || 0 }}</td>
              <td class="px-4 py-3 text-xs">
                <div v-if="b.firstErrorCode" class="font-bold text-amber-700 dark:text-amber-300">{{ b.firstErrorCode }}</div>
                <div class="text-slate-500 dark:text-slate-400 truncate max-w-xs">{{ b.firstErrorMessage || "-" }}</div>
              </td>
              <td class="px-4 py-3 text-right">
                <button
                  type="button"
                  @click="openPreview(b.uploadBatchId)"
                  class="inline-flex items-center gap-1 px-3 py-1.5 rounded-lg bg-slate-900 hover:bg-slate-700 dark:bg-slate-700 dark:hover:bg-slate-600 text-white text-xs font-bold"
                >
                  <FileSearch class="w-3.5 h-3.5" /> 查看
                </button>
              </td>
            </tr>
            <tr v-if="!loading && batches.length === 0">
              <td colspan="9" class="px-4 py-10 text-center text-slate-400">
                尚無匯入批次。請先使用 TransFiles 轉檔工具匯入資料。
              </td>
            </tr>
            <tr v-if="loading">
              <td colspan="9" class="px-4 py-10 text-center text-slate-400">
                載入中...
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="rounded-2xl bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-800 p-5">
      <h2 class="text-sm font-black text-slate-800 dark:text-white flex items-center gap-2">
        <Wrench class="w-4 h-4 text-amber-600" /> 格式不符時的處理原則
      </h2>
      <p class="mt-2 text-sm text-slate-600 dark:text-slate-300">
        請回 TransFiles 的錯誤報告修正來源 Excel。錯誤報告必須包含檔案、工作表、列號、欄位、原始值、錯誤代碼與修正建議；格式或主檔對應錯誤的資料不可寫入該筆。
      </p>
      <button
        type="button"
        @click="router.push('/spc')"
        class="mt-4 inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-bold"
      >
        前往 SPC 製圖確認 <ArrowRight class="w-4 h-4" />
      </button>
    </section>
  </div>
</template>
