<script setup>
import { computed, onMounted, ref } from 'vue'
import { Activity, ExternalLink, RefreshCw } from 'lucide-vue-next'
import { api, getApiErrorMessage } from '../api/client'

const portalUrl = (import.meta.env.VITE_PORTAL_URL || 'http://172.16.110.27/').replace(/\/$/, '')
const lineCodes = ['PT1', 'PT2', 'QE1', 'QE2']
const characteristics = [
  { code: 'ETCH_A_AVG', name: 'A Side 平均咬蝕量' },
  { code: 'ETCH_B_AVG', name: 'B Side 平均咬蝕量' },
  { code: 'ETCH_RATE', name: '整體咬蝕速率 ER' },
  { code: 'ETCH_LINE_SPEED', name: '實際線速' },
]
const mappings = ref([])
const selectedLine = ref('PT1')
const loading = ref(false)
const error = ref('')
const today = new Date()
const localDate = date => `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
const startDate = ref(localDate(new Date(today.getFullYear(), today.getMonth(), 1)))
const endDate = ref(localDate(today))

const etchMappings = computed(() => mappings.value.filter(item =>
  item.isEnabled
  && String(item.controlScope).toUpperCase() === 'PROCESS'
  && lineCodes.includes(item.machine?.machineCode)
  && characteristics.some(characteristic => characteristic.code === item.characteristic?.characteristicCode),
))
const selectedItems = computed(() => characteristics.map(characteristic => ({
  ...characteristic,
  mapping: etchMappings.value.find(item => item.machine?.machineCode === selectedLine.value && item.characteristic?.characteristicCode === characteristic.code),
})))

function analysisQuery(mapping) {
  return { ppcId: mapping.id, startDate: startDate.value, endDate: endDate.value }
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    mappings.value = (await api.get('/part-process-characteristics')).data || []
  } catch (exception) {
    error.value = getApiErrorMessage(exception)
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <section class="space-y-5">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="flex items-center gap-2 text-2xl font-black text-slate-800 dark:text-white"><Activity class="h-7 w-7 text-cyan-500" />咬蝕量分析</h1>
        <p class="mt-1 text-sm text-slate-500">資料來源為 PmrPortal 咬蝕量日報；正／背面以完整 25／50 點計算 X̄-S；整體速率與實際線速使用 I-MR。舊單值摘要保留於歷史查詢。</p>
      </div>
      <a :href="`${portalUrl}/QualityAssurance/EtchAmountEntry`" class="action primary" target="_blank" rel="noopener">前往咬蝕量日報輸入<ExternalLink class="h-4 w-4" /></a>
    </header>

    <p v-if="error" class="rounded-xl bg-red-50 p-3 text-sm text-red-700 dark:bg-red-950/30 dark:text-red-300">{{ error }}</p>

    <div class="flex flex-wrap gap-3 rounded-xl border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-900">
      <label><span>線別</span><select v-model="selectedLine"><option v-for="line in lineCodes" :key="line">{{ line }}</option></select></label>
      <label><span>起始日期</span><input v-model="startDate" type="date"></label>
      <label><span>結束日期</span><input v-model="endDate" type="date"></label>
      <button class="action" :disabled="loading" @click="load"><RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />重新讀取主檔</button>
    </div>

    <div class="grid gap-4 md:grid-cols-3">
      <article v-for="item in selectedItems" :key="item.code" class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <p class="text-xs font-bold text-cyan-600">{{ selectedLine }}</p>
        <h2 class="mt-1 text-lg font-black text-slate-800 dark:text-white">{{ item.name }}</h2>
        <p class="mt-1 font-mono text-xs text-slate-400">{{ item.code }}</p>
        <template v-if="item.mapping">
          <dl class="mt-4 space-y-2 text-sm"><div><dt>規格範圍</dt><dd>{{ item.mapping.lsl ?? '—' }} ～ {{ item.mapping.usl ?? '—' }} {{ item.mapping.unit || '' }}</dd></div><div><dt>目標值</dt><dd>{{ item.mapping.targetValue ?? '—' }}</dd></div><div><dt>累積資料</dt><dd>{{ item.mapping.measurementCount || 0 }} 筆</dd></div></dl>
          <div class="mt-5 flex flex-wrap gap-2"><RouterLink class="action primary" :to="{ path: '/spc', query: analysisQuery(item.mapping) }">SPC 管制圖</RouterLink><RouterLink class="action" :to="{ path: '/trend-chart', query: analysisQuery(item.mapping) }">量測趨勢</RouterLink></div>
        </template>
        <p v-else class="mt-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-700 dark:bg-amber-950/30 dark:text-amber-300">找不到此線別的啟用中 PPC，請先確認 SPC 管制項目設定。</p>
      </article>
    </div>
  </section>
</template>

<style scoped>
label { display: grid; gap: .25rem; color: #64748b; font-size: .75rem; font-weight: 700; }
input, select { min-width: 10rem; border: 1px solid #cbd5e1; border-radius: .55rem; background: transparent; padding: .45rem .65rem; color: inherit; }
.action { display: inline-flex; align-items: center; justify-content: center; gap: .35rem; align-self: end; border: 1px solid #94a3b8; border-radius: .55rem; padding: .45rem .7rem; color: #0369a1; font-size: .75rem; font-weight: 700; }
.action.primary { border-color: #0284c7; background: #0284c7; color: white; }
dl div { display: flex; justify-content: space-between; gap: 1rem; }
dt { color: #64748b; }
dd { font-weight: 700; text-align: right; }
</style>
