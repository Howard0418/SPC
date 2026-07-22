<script setup>
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { UploadCloud, FileSpreadsheet } from "lucide-vue-next";
import VariableUploadView from "./VariableUploadView.vue";
import AttributeUploadView from "./AttributeUploadView.vue";

const route = useRoute();
const router = useRouter();
const activeTab = computed(() => route.query.tab === "attribute" ? "attribute" : "variable");

function switchTab(tab) {
  router.replace({ path: "/uploads", query: tab === "attribute" ? { tab } : {} });
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm">
      <div>
        <h1 class="text-xl font-black text-slate-900 dark:text-white">SPC 資料匯入</h1>
        <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">依資料型態切換匯入方式，原有匯入與檢核功能維持不變。</p>
      </div>

      <div class="inline-flex p-1 rounded-xl bg-slate-100 dark:bg-slate-800" role="tablist" aria-label="資料匯入型態">
        <button
          type="button"
          role="tab"
          :aria-selected="activeTab === 'variable'"
          class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-bold transition-all"
          :class="activeTab === 'variable' ? 'bg-blue-600 text-white shadow' : 'text-slate-600 dark:text-slate-300 hover:text-blue-600'"
          @click="switchTab('variable')"
        >
          <UploadCloud class="w-4 h-4" /> 計量型資料
        </button>
        <button
          type="button"
          role="tab"
          :aria-selected="activeTab === 'attribute'"
          class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-bold transition-all"
          :class="activeTab === 'attribute' ? 'bg-teal-600 text-white shadow' : 'text-slate-600 dark:text-slate-300 hover:text-teal-600'"
          @click="switchTab('attribute')"
        >
          <FileSpreadsheet class="w-4 h-4" /> 計數型資料
        </button>
      </div>
    </div>

    <VariableUploadView v-if="activeTab === 'variable'" />
    <AttributeUploadView v-else />
  </div>
</template>
