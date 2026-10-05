<script setup>
import { computed, onMounted, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import { RefreshCw, Save, Search, TestTube2 } from "lucide-vue-next";

const defaultCells = [
  { cellAddress: "F!B3", standardSolution: "1N NaOH", numericValue: 1.02 },
  { cellAddress: "F!B4", standardSolution: "0.2N HCl", numericValue: 0.983 },
  { cellAddress: "F!B5", standardSolution: "0.4N HCl", numericValue: 0.983 },
  { cellAddress: "F!B6", standardSolution: "0.05N EDTA", numericValue: 1 },
  { cellAddress: "F!B7", standardSolution: "0.1N KMnO4", numericValue: 1 },
  { cellAddress: "F!B8", standardSolution: "0.1N H2SO4", numericValue: 1 },
  { cellAddress: "F!B9", standardSolution: "0.1N I2", numericValue: 1 },
  { cellAddress: "F!B10", standardSolution: "0.1ml Na2S2O3", numericValue: 1 }
];

const loading = ref(false);
const saving = ref(false);
const error = ref("");
const message = ref("");
const active = ref(null);
const form = ref({ versionCode: "", displayName: "", cells: defaultCells.map(x => ({ ...x })) });
const selectedCell = ref("F!B3");
const impactRows = ref([]);
const impactLoading = ref(false);
const formula = ref("Primary * F!B3");
const primaryValue = ref(10);
const formulaResult = ref(null);

const hasActive = computed(() => !!active.value?.versionCode);

function loadIntoForm(detail) {
  form.value.versionCode = `F-${new Date().toISOString().slice(0, 10).replaceAll("-", "")}`;
  form.value.displayName = "F 表新版";
  form.value.cells = (detail?.cells?.length ? detail.cells : defaultCells).map(x => ({
    cellAddress: x.cellAddress,
    standardSolution: x.standardSolution || "",
    numericValue: Number(x.numericValue ?? 0)
  }));
  selectedCell.value = form.value.cells[0]?.cellAddress || "F!B3";
}

async function load() {
  loading.value = true;
  error.value = "";
  message.value = "";
  try {
    const { data } = await api.get("/v1/chemical-f-table/active");
    active.value = data;
    loadIntoForm(data);
  } catch (err) {
    if (err?.response?.status === 404) {
      active.value = null;
      loadIntoForm(null);
      message.value = "尚未建立 F 表，已載入第一版預設資料。";
    } else {
      error.value = getApiErrorMessage(err);
    }
  } finally {
    loading.value = false;
  }
}

async function preview() {
  return apply(false);
}

async function save() {
  return apply(true);
}

async function apply(applyChanges) {
  saving.value = true;
  error.value = "";
  message.value = "";
  try {
    const payload = {
      apply: applyChanges,
      versionCode: form.value.versionCode,
      displayName: form.value.displayName,
      cells: form.value.cells.map(x => ({
        cellAddress: x.cellAddress,
        standardSolution: x.standardSolution,
        numericValue: Number(x.numericValue)
      }))
    };
    const { data } = await api.post("/v1/chemical-f-table/apply", payload);
    message.value = applyChanges
      ? `已套用 ${data.versionCode}，並重建公式引用索引。`
      : `Dry-run 完成：${data.cells?.filter(x => x.action !== "Unchanged").length || 0} 格將異動。`;
    if (applyChanges) await load();
  } catch (err) {
    error.value = getApiErrorMessage(err);
  } finally {
    saving.value = false;
  }
}

async function loadImpact() {
  impactLoading.value = true;
  error.value = "";
  try {
    const { data } = await api.get("/v1/chemical-f-table/impact", { params: { cellAddress: selectedCell.value } });
    impactRows.value = Array.isArray(data) ? data : [];
  } catch (err) {
    error.value = getApiErrorMessage(err);
  } finally {
    impactLoading.value = false;
  }
}

async function evaluateFormula() {
  formulaResult.value = null;
  error.value = "";
  try {
    const { data } = await api.post("/v1/chemical-f-table/evaluate", {
      expression: formula.value,
      variables: { Primary: Number(primaryValue.value) }
    });
    formulaResult.value = data;
  } catch (err) {
    error.value = getApiErrorMessage(err);
  }
}

onMounted(async () => {
  await load();
  await loadImpact();
});
</script>

<template>
  <div class="p-6 space-y-5">
    <div class="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
      <div>
        <h1 class="text-2xl font-black text-slate-900 dark:text-white">藥液 F 表維護</h1>
        <p class="text-sm text-slate-500 dark:text-slate-400">濃度公式可引用 F!B3 這類儲存格，計算時會使用目前啟用版本。</p>
      </div>
      <button @click="load" class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-slate-800 text-white text-sm font-bold hover:bg-slate-700">
        <RefreshCw class="w-4 h-4" /> 重新整理
      </button>
    </div>

    <div v-if="error" class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">{{ error }}</div>
    <div v-if="message" class="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-bold text-emerald-700">{{ message }}</div>

    <section class="grid gap-4 lg:grid-cols-[1fr_360px]">
      <div class="rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
        <div class="mb-4 grid gap-3 md:grid-cols-2">
          <label class="text-xs font-bold text-slate-600 dark:text-slate-300">新版代碼
            <input v-model="form.versionCode" class="mt-1 w-full rounded-lg border px-3 py-2 font-mono dark:bg-slate-950 dark:border-slate-700" />
          </label>
          <label class="text-xs font-bold text-slate-600 dark:text-slate-300">版本名稱
            <input v-model="form.displayName" class="mt-1 w-full rounded-lg border px-3 py-2 dark:bg-slate-950 dark:border-slate-700" />
          </label>
        </div>

        <div class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-950 dark:text-slate-400">
              <tr><th class="px-3 py-2">儲存格</th><th class="px-3 py-2">標準液</th><th class="px-3 py-2">F 值</th></tr>
            </thead>
            <tbody>
              <tr v-for="cell in form.cells" :key="cell.cellAddress" class="border-t dark:border-slate-800">
                <td class="px-3 py-2 font-mono font-bold">{{ cell.cellAddress }}</td>
                <td class="px-3 py-2"><input v-model="cell.standardSolution" class="w-full rounded border px-2 py-1 dark:bg-slate-950 dark:border-slate-700" /></td>
                <td class="px-3 py-2"><input v-model.number="cell.numericValue" type="number" step="0.000001" class="w-32 rounded border px-2 py-1 font-mono dark:bg-slate-950 dark:border-slate-700" /></td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="mt-4 flex gap-2">
          <button @click="preview" :disabled="saving" class="inline-flex items-center gap-2 px-4 py-2 rounded-lg border text-sm font-bold hover:bg-slate-50 dark:border-slate-700 dark:hover:bg-slate-800">
            <Search class="w-4 h-4" /> Dry-run
          </button>
          <button @click="save" :disabled="saving" class="inline-flex items-center gap-2 px-4 py-2 rounded-lg bg-blue-600 text-white text-sm font-bold hover:bg-blue-500">
            <Save class="w-4 h-4" /> 套用新版
          </button>
        </div>
      </div>

      <aside class="space-y-4">
        <div class="rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
          <h2 class="mb-2 text-sm font-black text-slate-800 dark:text-white">目前啟用版本</h2>
          <div v-if="hasActive" class="text-sm text-slate-600 dark:text-slate-300">
            <div class="font-mono font-bold">{{ active.versionCode }}</div>
            <div>{{ active.displayName }}</div>
            <div class="text-xs text-slate-400">{{ new Date(active.effectiveAt).toLocaleString() }}</div>
          </div>
          <div v-else class="text-sm text-slate-500">尚未建立</div>
        </div>

        <div class="rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
          <h2 class="mb-3 text-sm font-black text-slate-800 dark:text-white">公式測試</h2>
          <input v-model="formula" class="mb-2 w-full rounded border px-3 py-2 font-mono text-sm dark:bg-slate-950 dark:border-slate-700" />
          <input v-model.number="primaryValue" type="number" class="mb-2 w-full rounded border px-3 py-2 font-mono text-sm dark:bg-slate-950 dark:border-slate-700" />
          <button @click="evaluateFormula" class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-emerald-600 text-white text-xs font-bold hover:bg-emerald-500">
            <TestTube2 class="w-4 h-4" /> 試算
          </button>
          <div v-if="formulaResult" class="mt-3 rounded bg-slate-50 p-3 text-xs dark:bg-slate-950">
            <div>結果：<span class="font-mono font-bold">{{ formulaResult.value }}</span></div>
            <div>版本：<span class="font-mono">{{ formulaResult.versionCode }}</span></div>
          </div>
        </div>
      </aside>
    </section>

    <section class="rounded-lg border bg-white p-4 shadow-sm dark:bg-slate-900 dark:border-slate-800">
      <div class="mb-3 flex flex-wrap items-center gap-2">
        <h2 class="mr-auto text-sm font-black text-slate-800 dark:text-white">受影響項目</h2>
        <select v-model="selectedCell" class="rounded-lg border px-3 py-2 text-sm dark:bg-slate-950 dark:border-slate-700">
          <option v-for="cell in form.cells" :key="cell.cellAddress" :value="cell.cellAddress">{{ cell.cellAddress }}</option>
        </select>
        <button @click="loadImpact" :disabled="impactLoading" class="inline-flex items-center gap-2 px-3 py-2 rounded-lg bg-slate-800 text-white text-xs font-bold hover:bg-slate-700">
          <Search class="w-4 h-4" /> 查詢
        </button>
      </div>
      <div class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead class="bg-slate-50 text-left text-xs text-slate-500 dark:bg-slate-950 dark:text-slate-400">
            <tr><th class="px-3 py-2">線別/製程</th><th class="px-3 py-2">槽體</th><th class="px-3 py-2">分析項目</th><th class="px-3 py-2">公式</th></tr>
          </thead>
          <tbody>
            <tr v-for="row in impactRows" :key="`${row.partProcessCharacteristicId}-${row.referenceContext}`" class="border-t dark:border-slate-800">
              <td class="px-3 py-2"><div class="font-bold">{{ row.machineName || row.processName }}</div><small class="text-slate-400">{{ row.machineCode || row.processCode }}</small></td>
              <td class="px-3 py-2"><div>{{ row.tankName || "-" }}</div><small class="text-slate-400">{{ row.tankCode }}</small></td>
              <td class="px-3 py-2"><div class="font-bold">{{ row.characteristicName }}</div><small class="text-slate-400">{{ row.characteristicCode }}</small></td>
              <td class="px-3 py-2 font-mono text-xs">{{ row.sourceFormula }}</td>
            </tr>
            <tr v-if="!impactRows.length"><td colspan="4" class="px-3 py-8 text-center text-slate-500">目前沒有引用 {{ selectedCell }} 的項目。</td></tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>
