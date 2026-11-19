<script setup>
import { computed, onMounted, ref } from 'vue';
import { getCurrentUser } from '../utils/auth';
import { cleanCell, editableFields, parseAdjustmentPaste, positiveOptional } from '../utils/chemicalAdjustmentPaste';

const portalOrigin = (import.meta.env.VITE_PORTAL_URL || 'https://portaltest.pmr.com.tw/').replace(/\/$/, '');
const mappings = ref([]);
const machineId = ref('');
const alertMessage = ref('');
const successMessage = ref('');
const touched = ref(new Set());
const preview = ref([]);
const saving = ref(false);
const operatorName = computed(() => getCurrentUser()?.displayName || getCurrentUser()?.username || '—');
const text = value => String(value ?? '').trim();
const machineLabel = item => text(item.machine?.machineName) || text(item.machine?.machineCode) || `機台 ${item.machineId}`;
const machines = computed(() => [...new Map(mappings.value.filter(x => x.machineId).map(x => [Number(x.machineId), machineLabel(x)])).entries()].sort((a,b) => a[1].localeCompare(b[1], 'zh-TW')));
const rows = computed(() => mappings.value.filter(x => Number(x.machineId) === Number(machineId.value)).sort((a,b) => (a.tank?.sequenceNo ?? 0) - (b.tank?.sequenceNo ?? 0) || (a.sequenceNo ?? 0) - (b.sequenceNo ?? 0) || a.id - b.id));

function config(item) { try { return item.chemicalAnalysisConfigJson ? JSON.parse(item.chemicalAnalysisConfigJson) : {}; } catch { return {}; } }
function value(item, field) { return config(item)[field] ?? ''; }
function currentPayload(item) { const c = config(item); return { chemicalName:text(c.chemicalName)||null, tankCapacityLiters:c.tankCapacityLiters??null, additionControlValue:c.additionControlValue??null, dilutionControlValue:c.dilutionControlValue??null, adjustmentRemark:text(c.adjustmentRemark)||null }; }
function resetState() { touched.value = new Set(); preview.value = []; alertMessage.value = ''; successMessage.value = ''; }
function markTouched(item) { touched.value = new Set(touched.value).add(Number(item.id)); preview.value = []; successMessage.value = ''; }

function onPaste(event, startIndex) {
  const parsed = parseAdjustmentPaste(event.clipboardData?.getData('text'));
  if (!parsed.rows.length) return;
  event.preventDefault();
  if (startIndex + parsed.rows.length > rows.value.length) { alertMessage.value = `貼上資料 ${parsed.rows.length} 列超過格線剩餘 ${rows.value.length - startIndex} 列。`; return; }
  parsed.rows.forEach((cells, offset) => {
    const item = rows.value[startIndex + offset]; const c = config(item);
    parsed.fields.forEach((field, column) => { if (!field) return; let v = cleanCell(cells[column]); if (!['chemicalName','adjustmentRemark'].includes(field) && /^(NA|N\/A|-)$/i.test(v)) v=''; if(v==='') return; c[field]=v; });
    item.chemicalAnalysisConfigJson = JSON.stringify(c); touched.value.add(Number(item.id));
  });
  touched.value = new Set(touched.value); preview.value = []; alertMessage.value = '';
}

function buildPreview() {
  if (!machineId.value) throw new Error('請先選擇線別／機台。');
  const result = rows.value.filter(item => touched.value.has(Number(item.id))).map(item => {
    const c = config(item); const payload = currentPayload(item); const errors = [];
    for (const field of ['tankCapacityLiters','additionControlValue','dilutionControlValue']) { const parsed=positiveOptional(cleanCell(c[field])); if(Number.isNaN(parsed)) errors.push(`${field==='tankCapacityLiters'?'槽容':field==='additionControlValue'?'添加管制值':'稀釋管制值'}必須大於 0`); else if(parsed!==undefined) payload[field]=parsed; else payload[field]=null; }
    payload.chemicalName = text(c.chemicalName) || null; payload.adjustmentRemark = text(c.adjustmentRemark) || null;
    if(payload.chemicalName?.length>100) errors.push('藥液不可超過 100 字'); if(payload.adjustmentRemark?.length>500) errors.push('備註不可超過 500 字');
    return { item, payload, errors };
  });
  if(!result.length) throw new Error('請修改或貼上至少一筆資料。');
  return result;
}
function previewChanges(){ try { alertMessage.value=''; preview.value=buildPreview(); } catch(error){ preview.value=[]; alertMessage.value=error.message; } }
async function portalFetch(path, options={}) { const response=await fetch(`${portalOrigin}${path}`, { credentials:'include', ...options }); if(response.status===401) throw new Error('入口網站登入已過期，請由入口網站重新進入 SPC。'); if(!response.ok){ const error=await response.json().catch(()=>({})); throw new Error(error.message||`作業失敗 (HTTP ${response.status})`); } return response.status===204?null:response.json(); }
async function saveAll(){ if(!preview.value.length||preview.value.some(x=>x.errors.length)) return; saving.value=true; let completed=0; try { for(const entry of preview.value){ const saved=await portalFetch(`/api/spc/chemical-adjustment-parameters/${entry.item.id}`,{method:'PUT',headers:{'Content-Type':'application/json'},body:JSON.stringify(entry.payload)}); entry.item.chemicalAnalysisConfigJson=saved.chemicalAnalysisConfigJson; completed++; } successMessage.value=`已成功儲存 ${completed} 筆資料。`; resetState(); successMessage.value=`已成功儲存 ${completed} 筆資料。`; } catch(error){ alertMessage.value=`已儲存 ${completed} 筆後停止：${error.message}`; preview.value=[]; } finally { saving.value=false; } }
async function load(){ try { mappings.value=await portalFetch('/api/spc/master-data'); } catch(error){ alertMessage.value=error.message; } }
onMounted(load);
</script>

<template>
  <main class="w-full p-4 md:p-6 space-y-5">
    <section class="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm p-5">
      <h2 class="text-2xl font-black text-slate-900 dark:text-white">藥液量測調整公式</h2>
      <p class="mt-1 text-sm text-slate-500">依藥液量測主檔固定順序維護槽容與管制值；規格、範圍及公式不在此頁修改。</p>
      <div class="mt-5 grid gap-4 lg:grid-cols-[minmax(18rem,28rem)_1fr] items-end">
        <label class="text-sm font-bold">線別／機台
          <select v-model="machineId" @change="resetState" class="mt-2 w-full rounded-lg border border-blue-400 bg-white dark:bg-slate-950 px-3 py-2">
            <option value="">請選擇線別／機台</option><option v-for="machine in machines" :key="machine[0]" :value="machine[0]">{{ machine[1] }}</option>
          </select>
        </label>
        <div class="text-sm text-slate-500">作業人員：{{ operatorName }}；{{ machineId ? `共 ${rows.length} 個分析項目` : '請先選擇線別／機台' }}</div>
      </div>
    </section>
    <p v-if="alertMessage" class="rounded-xl bg-red-100 text-red-800 px-4 py-3">{{ alertMessage }}</p>
    <p v-if="successMessage" class="rounded-xl bg-emerald-100 text-emerald-800 px-4 py-3">{{ successMessage }}</p>
    <section class="rounded-2xl border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 shadow-sm overflow-hidden">
      <div class="flex flex-wrap justify-between gap-3 p-4 border-b border-slate-200 dark:border-slate-800">
        <div><h3 class="font-bold">Excel 批次貼上</h3><p class="text-sm text-slate-500">槽位與分析項目由系統帶入；可貼上藥液、槽容、添加、稀釋及備註五欄。</p></div>
        <div class="flex gap-2"><button @click="previewChanges" class="rounded-lg border border-blue-500 px-4 py-2 text-blue-600">預覽貼上資料</button><button @click="saveAll" :disabled="saving || !preview.length || preview.some(x=>x.errors.length)" class="rounded-lg bg-blue-600 disabled:bg-slate-400 text-white px-4 py-2">{{ saving ? '儲存中…' : '批次儲存' }}</button></div>
      </div>
      <div class="overflow-x-auto">
        <table class="min-w-[1050px] w-full text-sm border-collapse"><thead class="bg-slate-100 dark:bg-slate-800"><tr><th class="p-3 text-left">槽位</th><th class="p-3 text-left">分析項目</th><th v-for="label in ['藥液','槽容(L)','添加管制值','稀釋管制值','備註']" :key="label" class="p-3 text-left">{{ label }}</th></tr></thead>
          <tbody><tr v-if="!rows.length"><td colspan="7" class="p-8 text-center text-slate-500">請先選擇有分析項目的線別／機台。</td></tr>
          <tr v-for="(item,index) in rows" :key="item.id" class="border-t border-slate-200 dark:border-slate-800"><td class="p-2">{{ item.tank?.tankName || item.tank?.tankCode || '—' }}</td><td class="p-2 font-semibold">{{ item.characteristic?.characteristicName || item.characteristic?.characteristicCode || '—' }}</td>
            <td v-for="field in editableFields" :key="field" class="p-2"><input :value="value(item,field)" @input="event=>{ const c=config(item); c[field]=event.target.value; item.chemicalAnalysisConfigJson=JSON.stringify(c); markTouched(item); }" @paste="event=>onPaste(event,index)" :maxlength="field==='chemicalName'?100:field==='adjustmentRemark'?500:undefined" class="w-full min-w-32 rounded border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-950 px-2 py-2"></td>
          </tr></tbody></table>
      </div>
      <div v-if="preview.length" class="p-4 border-t border-slate-200 dark:border-slate-800"><p :class="preview.some(x=>x.errors.length)?'text-red-600':'text-emerald-600'">共 {{ preview.length }} 筆；錯誤 {{ preview.filter(x=>x.errors.length).length }} 筆。</p><ul class="mt-2 text-sm text-red-600"><li v-for="entry in preview.filter(x=>x.errors.length)" :key="entry.item.id">{{ entry.item.tank?.tankName }}／{{ entry.item.characteristic?.characteristicName }}：{{ entry.errors.join('；') }}</li></ul></div>
    </section>
  </main>
</template>
