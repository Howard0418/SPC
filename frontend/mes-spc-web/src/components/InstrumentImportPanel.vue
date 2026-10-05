<script setup>
import { computed, onMounted, ref } from 'vue';
import { api, getApiErrorMessage } from '../api/client';

defineProps({ operators: { type: Array, default: () => [] } });
const emit = defineEmits(['imported']);
const canManage = ref(false);
const open = ref(false);
const busy = ref(false);
const error = ref('');
const accessError = ref('');
const file = ref(null);
const preview = ref(null);
const selected = ref([]);
const result = ref(null);
const confirmed = ref(false);
const department = ref('');
const custodian = ref('');
const status = ref('');
const includeCustodian = ref(true);
const ready = computed(() => preview.value?.rows.filter(row => row.status === 'Ready') || []);
const canSubmit = computed(() => !busy.value && !result.value && selected.value.length > 0 && department.value.trim() && custodian.value && status.value && confirmed.value);
const inputClass = 'w-full rounded border border-slate-300 bg-white px-3 py-2 text-slate-900 dark:border-slate-600 dark:bg-slate-900 dark:text-slate-100';

onMounted(async () => {
  try { canManage.value = !!(await api.get('/v1/instruments/import/access')).data?.data?.canManage; }
  catch (e) { accessError.value = `無法確認匯入權限：${getApiErrorMessage(e)}`; }
});
function changeFile(event) {
  file.value = event.target.files?.[0] || null;
  preview.value = null; selected.value = []; result.value = null; confirmed.value = false; error.value = '';
}
async function readPreview() {
  if (!file.value || !/\.xlsx$/i.test(file.value.name) || file.value.size > 10 * 1024 * 1024) {
    error.value = '請選擇 10 MB 以內的 .xlsx 檔案。'; return;
  }
  busy.value = true; error.value = ''; result.value = null; preview.value = null; selected.value = []; confirmed.value = false;
  try {
    const data = new FormData(); data.append('file', file.value);
    preview.value = (await api.post('/v1/instruments/import/preview', data)).data.data;
  } catch (e) { error.value = getApiErrorMessage(e); }
  finally { busy.value = false; }
}
function toggleAll(event) { selected.value = event.target.checked ? ready.value.map(row => row.rowNumber) : []; }
async function commit() {
  if (!canSubmit.value) return;
  busy.value = true; error.value = '';
  try {
    const data = new FormData(); data.append('file', file.value);
    data.append('options', JSON.stringify({ hash: preview.value.hash, selectedRows: selected.value,
      department: department.value.trim(), custodianOperatorId: Number(custodian.value), usageStatus: status.value,
      includeCustodian: includeCustodian.value }));
    result.value = (await api.post('/v1/instruments/import/commit', data)).data.data;
    emit('imported');
  } catch (e) {
    result.value = e.response?.data?.data || null;
    error.value = getApiErrorMessage(e);
  } finally { busy.value = false; }
}
const statusLabel = value => ({ Ready: '可匯入', Invalid: '不可匯入', Existing: '已存在／略過', Added: '已新增', Skipped: '已略過', Failed: '失敗／未新增' })[value] || value;
</script>

<template>
  <span v-if="accessError" role="alert" class="text-sm text-rose-500">{{ accessError }}</span>
  <button v-if="canManage" type="button" @click="open = true" class="rounded-lg bg-cyan-500 px-4 py-2 text-sm font-medium text-white hover:bg-cyan-400">Excel 批次匯入</button>
  <Teleport to="body">
    <div v-if="open && canManage" class="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-3 md:p-6">
      <section role="dialog" aria-modal="true" aria-labelledby="instrument-import-title" class="flex max-h-[94vh] w-full max-w-6xl flex-col rounded-xl border border-slate-300 bg-white text-slate-900 shadow-xl dark:border-slate-700 dark:bg-slate-800 dark:text-slate-100">
        <header class="flex items-center justify-between border-b border-slate-300 p-4 dark:border-slate-700">
          <h2 id="instrument-import-title" class="text-lg font-bold">Excel 批次匯入儀器</h2>
          <button :disabled="busy" @click="open = false" aria-label="關閉匯入" class="rounded px-3 py-1 disabled:opacity-40">關閉</button>
        </header>
        <div class="min-w-0 space-y-4 overflow-x-hidden overflow-y-auto p-4">
          <p class="text-sm text-slate-600 dark:text-slate-300">上傳 → 預覽 → 勾選儀器 → 補齊主檔 → 確認匯入。只讀第一個工作表；已有編號會略過。</p>
          <div class="flex flex-col gap-3 md:flex-row md:items-center">
            <label class="flex-1 text-sm">儀器校驗管制一覽表（.xlsx，上限 10 MB）
              <input aria-label="儀器 Excel 檔案" type="file" accept=".xlsx" :disabled="busy" @change="changeFile" class="mt-1 block w-full text-sm" />
            </label>
            <button :disabled="busy || !file" @click="readPreview" class="rounded bg-indigo-600 px-4 py-2 text-white disabled:opacity-40">{{ busy ? '處理中…' : '解析／重新預覽' }}</button>
          </div>
          <p v-if="error" role="alert" class="rounded bg-rose-100 p-3 text-sm text-rose-800 dark:bg-rose-950 dark:text-rose-200">{{ error }}</p>
          <template v-if="preview">
            <p class="text-sm">工作表：{{ preview.sheetName }} · 共 {{ preview.rows.length }} 列 · 可匯入 {{ ready.length }} 列 · 已勾選 {{ selected.length }} 列</p>
            <p class="text-sm text-amber-700 dark:text-amber-300">公式日期採 Excel 已儲存的結果，請確認已重算儲存。未知週期、未校、預計日期須先在上傳副本修正。匯入不代表完成合格校正。</p>
            <fieldset :disabled="busy || !!result" class="min-w-0 space-y-3 disabled:opacity-70">
              <legend class="font-semibold">套用至本次勾選儀器（不同部門可分批匯入）</legend>
              <div class="grid gap-3 md:grid-cols-3">
                <label class="text-sm">部門 *<input aria-label="匯入部門" v-model="department" maxlength="100" :class="inputClass" placeholder="請填寫部門" /></label>
                <label class="text-sm">保管人 *<select aria-label="匯入保管人" v-model="custodian" :class="inputClass"><option value="">請選擇保管人</option><option v-for="op in operators" :key="op.id" :value="op.id">{{ op.operatorName }}（{{ op.operatorCode }}）</option></select></label>
                <label class="text-sm">使用狀態 *<select aria-label="匯入使用狀態" v-model="status" :class="inputClass"><option value="">請確認使用狀態</option><option value="Active">正常使用</option><option value="InCalibration">送校中</option><option value="Inactive">已停用</option><option value="Retired">已報廢</option></select></label>
              </div>
              <p v-if="!operators.length" role="alert" class="text-sm text-rose-500">沒有可選擇的啟用人員，請確認人員資料或重新載入頁面。</p>
              <label class="flex items-center gap-2 text-sm"><input type="checkbox" v-model="includeCustodian" />依既有通知規則包含保管人（匯入當下不直接寄信）</label>
              <div class="max-h-[38vh] overflow-auto rounded border border-slate-300 dark:border-slate-600">
                <table class="w-full text-left text-sm">
                  <thead class="sticky top-0 bg-slate-100 dark:bg-slate-900"><tr>
                    <th class="p-2"><input aria-label="全選可匯入儀器" type="checkbox" :checked="ready.length > 0 && selected.length === ready.length" :disabled="!ready.length" @change="toggleAll" /></th>
                    <th class="p-2">列</th><th class="p-2">編號／名稱</th><th class="p-2">放置地點</th><th class="p-2">週期</th><th class="p-2">校驗方式</th><th class="p-2">量測規格</th><th class="p-2">精度</th><th class="p-2">備註</th><th class="p-2">校驗規範</th><th class="p-2">允收標準</th><th class="p-2">上次校正日</th><th class="p-2">下次到期日</th><th class="p-2">狀態／原因</th>
                  </tr></thead>
                  <tbody><tr v-for="row in preview.rows" :key="row.rowNumber" class="border-t border-slate-200 align-top dark:border-slate-700">
                    <td class="p-2"><input type="checkbox" v-model="selected" :value="row.rowNumber" :disabled="row.status !== 'Ready'" :aria-label="`選取第 ${row.rowNumber} 列`" /></td>
                    <td class="p-2">{{ row.rowNumber }}</td><td class="min-w-40 p-2"><div>{{ row.code || '缺少編號' }}</div><div>{{ row.name || '缺少名稱' }}</div></td>
                    <td class="min-w-28 p-2">{{ row.location || '—' }}</td>
                    <td class="whitespace-nowrap p-2">{{ row.cycleMonths ? `${row.cycleMonths} 月` : '—' }}<small class="block text-slate-500 dark:text-slate-400">{{ row.rawCycle }}</small></td>
                    <td class="min-w-20 p-2">{{ row.calibrationMethod || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.measurementSpecification || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.precision || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.remarks || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.calibrationStandard || '—' }}</td>
                    <td class="min-w-40 p-2 whitespace-pre-wrap break-words">{{ row.acceptanceCriteria || '—' }}</td>

                    <td class="whitespace-nowrap p-2">{{ row.lastCalibrationDate || '—' }}<small class="block text-slate-500 dark:text-slate-400">原值：{{ row.rawLastDate || '空白' }}</small></td>
                    <td class="whitespace-nowrap p-2">{{ row.nextCalibrationDate || '—' }}<small class="block text-slate-500 dark:text-slate-400">原值：{{ row.rawNextDate || '空白' }}</small></td>
                    <td class="min-w-60 p-2"><strong>{{ statusLabel(row.status) }}</strong><p v-for="message in row.errors" :key="message" class="text-rose-700 dark:text-rose-300">{{ message }}</p><p v-for="message in row.warnings" :key="message" class="text-amber-700 dark:text-amber-300">{{ message }}</p></td>
                  </tr></tbody>
                </table>
              </div>
              <label class="flex items-start gap-2 text-sm"><input v-model="confirmed" type="checkbox" aria-label="確認匯入資料" class="mt-1" />我已核對勾選儀器的日期、警示及上述主檔資料。</label>
            </fieldset>
          </template>
          <div v-if="result" role="status" class="space-y-2 rounded border border-slate-300 p-3 dark:border-slate-600">
            <p class="font-bold">新增 {{ result.added }} 筆／略過 {{ result.skipped }} 筆／失敗 {{ result.failed }} 筆</p>
            <ul class="max-h-40 overflow-y-auto text-sm"><li v-for="row in result.rows" :key="row.rowNumber">第 {{ row.rowNumber }} 列 · {{ row.code }} · {{ statusLabel(row.status) }}：{{ row.message }}</li></ul>
            <p class="text-sm">如需再次匯入，請先按「解析／重新預覽」。</p>
          </div>
        </div>
        <footer class="flex justify-end gap-3 border-t border-slate-300 p-4 dark:border-slate-700">
          <button :disabled="busy" @click="open = false" class="rounded border border-slate-400 px-4 py-2 disabled:opacity-40">關閉</button>
          <button :disabled="!canSubmit" @click="commit" class="rounded bg-teal-700 px-4 py-2 text-white disabled:opacity-40">{{ busy ? '處理中…' : `確認匯入 ${selected.length} 筆` }}</button>
        </footer>
      </section>
    </div>
  </Teleport>
</template>
