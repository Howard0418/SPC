<script setup>
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import * as XLSX from "xlsx";
import { Download, FlaskConical, RefreshCw, Search, Settings2 } from "lucide-vue-next";
import { api, getApiErrorMessage } from "../api/client";

const router = useRouter();
const loading = ref(false);
const error = ref("");
const items = ref([]);
const line = ref("");
const tank = ref("");
const status = ref("all");

function configOf(item) {
  try { return JSON.parse(item.chemicalAnalysisConfigJson || "{}"); } catch { return {}; }
}
function rowOf(item) {
  const config = configOf(item);
  const formulaMissing = !config.enabled || !String(config.concentrationFormula || "").trim();
  const limitsMissing = item.lsl == null && item.usl == null && item.targetValue == null;
  return {
    id: item.id,
    line: item.machine?.machineCode || item.process?.processCode || "未設定",
    tank: item.tank?.tankName || item.tank?.tankCode || "未設定",
    tankCode: item.tank?.tankCode || "",
    chemical: item.characteristic?.characteristicName || item.characteristic?.characteristicCode || "未命名",
    enabled: item.isEnabled === true,
    secondary: String(config.secondaryInputLabel || "").trim(),
    formulaMissing,
    limitsMissing,
    formula: String(config.concentrationFormula || ""),
    issue: formulaMissing || limitsMissing
  };
}
const chemicalRows = computed(() => items.value
  .filter(x => String(x.controlScope || "").toUpperCase() === "CHEM")
  .map(rowOf));
const lines = computed(() => [...new Set(chemicalRows.value.map(x => x.line))].sort());
const tanks = computed(() => [...new Set(chemicalRows.value.filter(x => !line.value || x.line === line.value).map(x => x.tank))].sort());
const filtered = computed(() => chemicalRows.value.filter(x =>
  (!line.value || x.line === line.value) && (!tank.value || x.tank === tank.value) &&
  (status.value === "all" || status.value === "enabled" && x.enabled || status.value === "issues" && x.issue || status.value === "secondary" && !!x.secondary)
));
const summaries = computed(() => lines.value.map(currentLine => {
  const rows = chemicalRows.value.filter(x => x.line === currentLine);
  return { line: currentLine, tanks: new Set(rows.map(x => x.tank)).size, enabled: rows.filter(x => x.enabled).length, disabled: rows.filter(x => !x.enabled).length, secondary: rows.filter(x => !!x.secondary).length, issues: rows.filter(x => x.issue).length };
}));

async function load() {
  loading.value = true; error.value = "";
  try { const { data } = await api.get("/part-process-characteristics"); items.value = Array.isArray(data) ? data : []; }
  catch (e) { error.value = getApiErrorMessage(e); }
  finally { loading.value = false; }
}
function showLine(value) { line.value = value; tank.value = ""; }
function edit(row) { router.push({ path: "/part-process-characteristics", query: { editId: row.id } }); }
function exportExcel() {
  const rows = filtered.value.map(x => ({ 線別: x.line, 槽體: x.tank, 槽體代碼: x.tankCode, 分析項目: x.chemical, 狀態: x.enabled ? "啟用" : "停用", 滴定值2: x.secondary || "無", 濃度公式: x.formula || "未設定", 設定警示: x.formulaMissing ? "缺濃度公式" : x.limitsMissing ? "缺規格" : "完整" }));
  const book = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(book, XLSX.utils.json_to_sheet(rows), "藥液分析項目");
  XLSX.writeFile(book, `SPC_藥液分析項目總覽_${new Date().toISOString().slice(0, 10)}.xlsx`);
}
onMounted(load);
</script>

<template>
  <section class="space-y-6">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div><h1 class="text-2xl font-black text-slate-800 dark:text-white flex items-center gap-2"><FlaskConical class="w-7 h-7 text-emerald-500" /> 線別分析項目總覽</h1><p class="text-sm text-slate-500 mt-1">統計已設定的 CHEM 藥液管制項目；啟用項目即為量測頁可填寫的分析項目。</p></div>
      <div class="flex gap-2"><button @click="load" :disabled="loading" class="btn-outline"><RefreshCw class="w-4 h-4" :class="{ 'animate-spin': loading }" /> 重新整理</button><button @click="exportExcel" class="btn-primary"><Download class="w-4 h-4" /> 匯出 Excel</button></div>
    </header>
    <div v-if="error" class="rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-700">{{ error }}</div>
    <div class="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4"><button v-for="item in summaries" :key="item.line" @click="showLine(item.line)" class="rounded-2xl border bg-white p-4 text-left shadow-sm hover:border-emerald-400 dark:bg-slate-800 dark:border-slate-700"><div class="flex justify-between"><strong>{{ item.line }}</strong><span class="text-xs text-slate-500">{{ item.tanks }} 槽</span></div><div class="mt-3 text-3xl font-black text-emerald-600">{{ item.enabled }}<span class="ml-1 text-sm text-slate-500">啟用項目</span></div><div class="mt-2 text-xs text-slate-500">滴定值 2：{{ item.secondary }}　停用：{{ item.disabled }}　<span :class="item.issues ? 'text-amber-600 font-bold' : ''">警示：{{ item.issues }}</span></div></button></div>
    <div class="rounded-2xl border bg-white p-4 shadow-sm dark:bg-slate-800 dark:border-slate-700"><div class="grid gap-3 md:grid-cols-4"><select v-model="line" @change="tank=''" class="field"><option value="">全部線別</option><option v-for="value in lines" :key="value">{{ value }}</option></select><select v-model="tank" class="field"><option value="">全部槽體</option><option v-for="value in tanks" :key="value">{{ value }}</option></select><select v-model="status" class="field"><option value="all">全部狀態</option><option value="enabled">僅啟用</option><option value="secondary">僅有滴定值 2</option><option value="issues">僅設定警示</option></select><div class="flex items-center text-sm text-slate-500"><Search class="mr-2 w-4 h-4" />顯示 {{ filtered.length }} 個項目</div></div></div>
    <div class="overflow-x-auto rounded-2xl border bg-white shadow-sm dark:bg-slate-800 dark:border-slate-700"><table class="min-w-full text-sm"><thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-900"><tr><th>線別</th><th>槽體</th><th>分析項目</th><th>狀態</th><th>滴定值 2</th><th>設定狀態</th><th></th></tr></thead><tbody><tr v-for="item in filtered" :key="item.id" class="border-t dark:border-slate-700"><td>{{ item.line }}</td><td><div>{{ item.tank }}</div><small class="text-slate-400">{{ item.tankCode }}</small></td><td class="font-bold">{{ item.chemical }}</td><td><span :class="item.enabled ? 'badge-green' : 'badge-gray'">{{ item.enabled ? '啟用' : '停用' }}</span></td><td>{{ item.secondary || '—' }}</td><td><span :class="item.issue ? 'text-amber-600 font-bold' : 'text-emerald-600'">{{ item.formulaMissing ? '缺濃度公式' : item.limitsMissing ? '缺規格' : '完整' }}</span></td><td><button @click="edit(item)" class="text-blue-600 hover:underline"><Settings2 class="inline w-4 h-4" /> 編輯</button></td></tr><tr v-if="!loading && !filtered.length"><td colspan="7" class="p-8 text-center text-slate-500">沒有符合條件的藥液分析項目。</td></tr></tbody></table></div>
  </section>
</template>

<style scoped>
th,td{padding:.8rem 1rem}.field{width:100%;border:1px solid #cbd5e1;border-radius:.65rem;padding:.55rem .7rem;background:transparent}.btn-primary,.btn-outline{display:inline-flex;align-items:center;gap:.4rem;border-radius:.7rem;padding:.55rem .8rem;font-size:.875rem;font-weight:700}.btn-primary{background:#059669;color:white}.btn-outline{border:1px solid #94a3b8}.badge-green,.badge-gray{border-radius:9999px;padding:.2rem .55rem;font-size:.75rem;font-weight:700}.badge-green{background:#d1fae5;color:#047857}.badge-gray{background:#e2e8f0;color:#475569}
</style>
