<script setup>
import { computed, onMounted, ref } from "vue";
import { FlaskConical, RefreshCw, Search, AlertTriangle } from "lucide-vue-next";
import { api, getApiErrorMessage } from "../api/client";

const rows = ref([]);
const loading = ref(false);
const err = ref("");
const keyword = ref("");
const status = ref("all");

const filteredRows = computed(() => rows.value.filter(row => {
  const q = keyword.value.trim().toLowerCase();
  const matchesKeyword = !q || [row.chemicalCode, row.chemicalName, row.chemicalType]
    .some(value => String(value || "").toLowerCase().includes(q));
  const matchesStatus = status.value === "all"
    || (status.value === "active" && row.isActive)
    || (status.value === "inactive" && !row.isActive);
  return matchesKeyword && matchesStatus;
}));

async function load() {
  loading.value = true;
  err.value = "";
  try {
    const { data } = await api.get("/chemicals");
    rows.value = Array.isArray(data) ? data : [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function formatDate(value) {
  if (!value) return "-";
  return new Date(value).toLocaleString("zh-TW", { hour12: false });
}

onMounted(load);
</script>

<template>
  <section class="space-y-5 pb-12">
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="flex items-center gap-3">
        <div class="p-3 rounded-xl bg-gradient-to-tr from-emerald-600 to-teal-500 text-white shadow-lg">
          <FlaskConical class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-white">藥液主表</h1>
          <p class="text-xs text-slate-500 mt-1">唯讀顯示 SPC 資料庫 Chemicals 主檔</p>
        </div>
      </div>
      <button @click="load" :disabled="loading" class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm font-bold">
        <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" />重新整理
      </button>
    </div>

    <div v-if="err" class="flex items-center gap-2 p-4 rounded-xl bg-red-50 dark:bg-red-950/40 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800">
      <AlertTriangle class="w-5 h-5" />{{ err }}
    </div>

    <div class="flex flex-col md:flex-row gap-3 justify-between p-4 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800">
      <div class="relative w-full md:w-96">
        <Search class="absolute left-3 top-3 w-4 h-4 text-slate-400" />
        <input v-model="keyword" class="w-full pl-9 pr-3 py-2.5 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 text-sm" placeholder="搜尋藥液代碼、名稱或類型" />
      </div>
      <div class="flex gap-2">
        <button v-for="item in [{v:'all',t:'全部'}, {v:'active',t:'啟用'}, {v:'inactive',t:'停用'}]" :key="item.v" @click="status=item.v"
          :class="['px-4 py-2 rounded-xl text-xs font-bold', status===item.v ? 'bg-emerald-600 text-white' : 'bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300']">
          {{ item.t }}
        </button>
      </div>
    </div>

    <div class="overflow-hidden bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="px-5 py-3 text-xs text-slate-500 border-b border-slate-200 dark:border-slate-800">共 {{ filteredRows.length }} 筆</div>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="bg-slate-50 dark:bg-slate-800/70 text-slate-500">
            <tr><th class="px-5 py-3 text-left">ID</th><th class="px-5 py-3 text-left">藥液代碼</th><th class="px-5 py-3 text-left">藥液名稱</th><th class="px-5 py-3 text-left">藥液類型</th><th class="px-5 py-3 text-center">狀態</th><th class="px-5 py-3 text-left">建立時間</th><th class="px-5 py-3 text-left">更新時間</th></tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
            <tr v-for="row in filteredRows" :key="row.id" class="hover:bg-slate-50 dark:hover:bg-slate-800/50">
              <td class="px-5 py-3 font-mono text-xs text-slate-400">{{ row.id }}</td>
              <td class="px-5 py-3 font-mono font-bold text-emerald-700 dark:text-emerald-400">{{ row.chemicalCode }}</td>
              <td class="px-5 py-3 font-bold">{{ row.chemicalName }}</td>
              <td class="px-5 py-3">{{ row.chemicalType || '-' }}</td>
              <td class="px-5 py-3 text-center"><span :class="['px-2.5 py-1 rounded-full text-xs font-bold', row.isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-600']">{{ row.isActive ? '啟用' : '停用' }}</span></td>
              <td class="px-5 py-3 text-xs text-slate-500">{{ formatDate(row.createdAt) }}</td>
              <td class="px-5 py-3 text-xs text-slate-500">{{ formatDate(row.updatedAt) }}</td>
            </tr>
            <tr v-if="!loading && filteredRows.length === 0"><td colspan="7" class="px-5 py-12 text-center text-slate-400">查無藥液主檔資料</td></tr>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>
