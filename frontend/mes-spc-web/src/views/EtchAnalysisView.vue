<script setup>
import {useI18n} from "vue-i18n";
import {displayUiMessage} from "../i18n/uiMessage";
const {t}=useI18n({useScope:"global"});
import { computed, onMounted, ref } from 'vue'
import { Activity, ExternalLink, RefreshCw } from 'lucide-vue-next'
import { api, getApiErrorDisplay } from '../api/client'

const portalUrl = (import.meta.env.VITE_PORTAL_URL || 'https://portaltest.pmr.com.tw/').replace(/\/$/, '')
const lineCodes = ['PT1', 'PT2', 'QE1', 'QE2']
const characteristics = computed(() => [
  { code: 'ETCH_A_AVG', name: t('spc.trace.etchA') },
  { code: 'ETCH_B_AVG', name: t('spc.trace.etchB') },
  { code: 'ETCH_RATE', name: t('spc.trace.etchRate') },
  { code: 'ETCH_LINE_SPEED', name: t('spc.trace.lineSpeed') },
])
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
  && characteristics.value.some(characteristic => characteristic.code === item.characteristic?.characteristicCode),
))
const selectedItems = computed(() => characteristics.value.map(characteristic => ({
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
    error.value = getApiErrorDisplay(exception)
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
        <h1 class="flex items-center gap-2 text-2xl font-black text-slate-800 dark:text-white"><Activity class="h-7 w-7 text-cyan-500" />{{ t('spc.trace.etchTitle') }}</h1>
        <p class="mt-1 text-sm text-slate-500">{{ t('spc.trace.etchDescription') }}</p>
      </div>
      <a :href="`${portalUrl}/QualityAssurance/EtchAmountEntry`" class="action primary" target="_blank" rel="noopener">{{ t('spc.trace.etchEntry') }}<ExternalLink class="h-4 w-4" /></a>
    </header>

    <p v-if="error" class="rounded-xl bg-red-50 p-3 text-sm text-red-700 dark:bg-red-950/30 dark:text-red-300">{{ displayUiMessage(t, error) }}</p>

    <div class="flex flex-wrap gap-3 rounded-xl border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-900">
      <label><span>{{ t('spc.trace.line') }}</span><select data-testid="trace-selectedLine" v-model="selectedLine"><option v-for="line in lineCodes" :key="line">{{ line }}</option></select></label>
      <label><span>{{ t('spc.trace.startDate') }}</span><input data-testid="trace-startDate" v-model="startDate" type="date"></label>
      <label><span>{{ t('spc.trace.endDate') }}</span><input data-testid="trace-endDate" v-model="endDate" type="date"></label>
      <button class="action" :disabled="loading" data-testid="trace-load" @click="load"><RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />{{ t('spc.trace.reloadMappings') }}</button>
    </div>

    <div class="grid gap-4 md:grid-cols-3">
      <article v-for="item in selectedItems" :key="item.code" class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <p class="text-xs font-bold text-cyan-600">{{ selectedLine }}</p>
        <h2 class="mt-1 text-lg font-black text-slate-800 dark:text-white">{{ item.name }}</h2>
        <p class="mt-1 font-mono text-xs text-slate-400">{{ item.code }}</p>
        <template v-if="item.mapping">
          <dl class="mt-4 space-y-2 text-sm"><div><dt>{{ t('spc.trace.specification') }}</dt><dd>{{ item.mapping.lsl ?? '—' }} ～ {{ item.mapping.usl ?? '—' }} {{ item.mapping.unit || '' }}</dd></div><div><dt>{{ t('spc.trace.target') }}</dt><dd>{{ item.mapping.targetValue ?? '—' }}</dd></div><div><dt>{{ t('spc.trace.measurements') }}</dt><dd>{{ item.mapping.measurementCount || 0 }} {{ t('spc.trace.recordUnit') }}</dd></div></dl>
          <div class="mt-5 flex flex-wrap gap-2"><RouterLink class="action primary" :to="{ path: '/spc', query: analysisQuery(item.mapping) }">{{ t('spc.trace.controlChart') }}</RouterLink><RouterLink class="action" :to="{ path: '/trend-chart', query: analysisQuery(item.mapping) }">{{ t('spc.trace.trend') }}</RouterLink></div>
        </template>
        <p v-else class="mt-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-700 dark:bg-amber-950/30 dark:text-amber-300">{{ t('spc.trace.noMapping') }}</p>
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
