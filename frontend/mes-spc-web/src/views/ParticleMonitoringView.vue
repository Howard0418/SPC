<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue';
import * as echarts from 'echarts';
import { Search, RefreshCw, AlertTriangle, Database } from 'lucide-vue-next';
import { api, getApiErrorMessage } from '../api/client';

const locations = Array.from({ length: 9 }, (_, index) => `R${index + 1}`);
const sizes = ['0.5', '1', '5', '10'];
const today = new Date();
const prior = new Date(today); prior.setDate(today.getDate() - 30);
const dateText = value => value.toISOString().slice(0, 10);
const filters = ref({ from: dateText(prior), to: dateText(today), location: 'R1', particleSize: '0.5', deviceCode: '', chartType: 'C' });
const loading = ref(false);
const error = ref('');
const chartResult = ref(null);
const comparison = ref(null);
const measurements = ref({ items: [], total: 0, page: 1, pageSize: 50 });
const chartEl = ref(null);
const comparisonEl = ref(null);
let chart;
let comparisonChart;

const queryRange = computed(() => ({
  from: `${filters.value.from}T00:00:00+08:00`,
  to: `${filters.value.to}T23:59:59+08:00`,
  location: filters.value.location,
  particleSize: filters.value.particleSize,
  ...(filters.value.deviceCode.trim() ? { deviceCode: filters.value.deviceCode.trim() } : {})
}));

function formatTime(value) {
  return value ? new Intl.DateTimeFormat('zh-TW', { dateStyle: 'short', timeStyle: 'short', hour12: false }).format(new Date(value)) : '-';
}

function renderCharts() {
  if (!chartEl.value || !comparisonEl.value) return;
  chart ||= echarts.init(chartEl.value);
  comparisonChart ||= echarts.init(comparisonEl.value);
  const points = chartResult.value?.points || [];
  const categories = points.map(point => formatTime(point.time));
  const line = (name, key, color, dashed = false) => ({ name, type: 'line', data: points.map(point => point[key]), symbol: 'none', lineStyle: { color, width: 1.5, type: dashed ? 'dashed' : 'solid' } });
  chart.setOption({
    animationDuration: 350,
    tooltip: { trigger: 'axis' },
    legend: { top: 0, textStyle: { color: '#64748b' } },
    grid: { left: 58, right: 24, top: 42, bottom: 58 },
    xAxis: { type: 'category', data: categories, axisLabel: { rotate: 30, color: '#64748b' } },
    yAxis: { type: 'value', name: chartResult.value?.chartType === 'U_CHART' ? `count/${chartResult.value?.samplingVolumeUnit || 'volume'}` : 'count', nameTextStyle: { color: '#64748b' }, axisLabel: { color: '#64748b' }, splitLine: { lineStyle: { color: '#e2e8f0' } } },
    series: [
      { name: chartResult.value?.chartType === 'U_CHART' ? 'U 值' : 'Count', type: 'line', data: points.map(point => ({ value: point.value ?? point.count, itemStyle: { color: point.isOutOfControl ? '#dc2626' : '#2563eb' } })), symbolSize: 7, lineStyle: { color: '#2563eb', width: 2 } },
      line('UCL', 'ucl', '#dc2626', true), line('CL', 'cl', '#16a34a', true), line('LCL', 'lcl', '#dc2626', true)
    ]
  }, true);
  const items = comparison.value?.items || [];
  comparisonChart.setOption({
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 55, right: 20, top: 20, bottom: 35 },
    xAxis: { type: 'category', data: items.map(item => item.location), axisLabel: { color: '#64748b' } },
    yAxis: { type: 'value', name: 'count', axisLabel: { color: '#64748b' }, splitLine: { lineStyle: { color: '#e2e8f0' } } },
    series: [{ type: 'bar', data: items.map(item => ({ value: item.count, itemStyle: { color: item.isMissing ? '#cbd5e1' : '#0891b2' } })), barMaxWidth: 38 }]
  }, true);
}

async function load() {
  if (!filters.value.from || !filters.value.to || filters.value.from > filters.value.to) { error.value = '請設定有效的查詢日期範圍。'; return; }
  loading.value = true; error.value = '';
  try {
    const common = queryRange.value;
    const [spcResponse, measurementsResponse] = await Promise.all([
      api.get('/v1/particles/spc', { params: { ...common, chartType: filters.value.chartType } }),
      api.get('/v1/particles/measurements', { params: { from: common.from, to: common.to, locations: filters.value.location, particleSizes: filters.value.particleSize, page: 1, pageSize: 50, sort: 'desc', ...(common.deviceCode ? { deviceCode: common.deviceCode } : {}) } })
    ]);
    chartResult.value = spcResponse.data;
    measurements.value = measurementsResponse.data;
    const event = chartResult.value?.points?.at(-1);
    comparison.value = event ? (await api.get('/v1/particles/location-comparison', { params: { measurementTime: event.time, particleSize: filters.value.particleSize, ...(common.deviceCode ? { deviceCode: common.deviceCode } : {}) } })).data : { items: [] };
    await nextTick(); renderCharts();
  } catch (reason) {
    error.value = getApiErrorMessage(reason);
    chartResult.value = null; comparison.value = null;
  } finally { loading.value = false; }
}

function resizeCharts() { chart?.resize(); comparisonChart?.resize(); }
onMounted(() => { window.addEventListener('resize', resizeCharts); load(); });
onBeforeUnmount(() => { window.removeEventListener('resize', resizeCharts); chart?.dispose(); comparisonChart?.dispose(); });
</script>

<template>
  <section class="mx-auto w-full max-w-[1600px] space-y-4">
    <header class="flex flex-wrap items-end justify-between gap-3 border-b border-slate-200 pb-4 dark:border-slate-800">
      <div><h2 class="text-2xl font-black text-slate-900 dark:text-white">Particle Monitoring</h2><p class="mt-1 text-sm text-slate-500">落塵粒子計數、位置比較與統計管制</p></div>
      <div class="flex items-center gap-2 text-xs font-semibold text-slate-500"><Database class="h-4 w-4" /> {{ measurements.total }} 筆符合資料</div>
    </header>

    <form class="grid gap-3 border-b border-slate-200 pb-4 dark:border-slate-800 md:grid-cols-3 xl:grid-cols-7" @submit.prevent="load">
      <label class="text-xs font-bold text-slate-500">開始日期<input v-model="filters.from" type="date" class="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900" /></label>
      <label class="text-xs font-bold text-slate-500">結束日期<input v-model="filters.to" type="date" class="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900" /></label>
      <label class="text-xs font-bold text-slate-500">位置<select v-model="filters.location" class="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"><option v-for="item in locations" :key="item">{{ item }}</option></select></label>
      <label class="text-xs font-bold text-slate-500">粒徑 (µm)<select v-model="filters.particleSize" class="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900"><option v-for="item in sizes" :key="item">{{ item }}</option></select></label>
      <label class="text-xs font-bold text-slate-500">儀器代碼<input v-model="filters.deviceCode" placeholder="全部" class="mt-1 w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm dark:border-slate-700 dark:bg-slate-900" /></label>
      <div class="text-xs font-bold text-slate-500">圖型<div class="mt-1 flex h-[38px] overflow-hidden rounded-md border border-slate-300 dark:border-slate-700"><button v-for="type in ['C','U']" :key="type" type="button" class="flex-1 px-3 text-sm" :class="filters.chartType === type ? 'bg-blue-600 font-bold text-white' : 'bg-white text-slate-600 dark:bg-slate-900 dark:text-slate-300'" :aria-pressed="filters.chartType === type" @click="filters.chartType = type">{{ type }}-chart</button></div></div>
      <button type="submit" class="mt-5 flex h-[38px] items-center justify-center gap-2 rounded-md bg-blue-600 px-4 text-sm font-bold text-white hover:bg-blue-700 disabled:opacity-60" :disabled="loading"><RefreshCw v-if="loading" class="h-4 w-4 animate-spin" /><Search v-else class="h-4 w-4" />查詢</button>
    </form>

    <div v-if="error" role="alert" class="flex items-start gap-2 border-l-4 border-red-500 bg-red-50 px-4 py-3 text-sm text-red-800 dark:bg-red-950/30 dark:text-red-300"><AlertTriangle class="mt-0.5 h-4 w-4 shrink-0" />{{ error }}</div>
    <div v-if="chartResult" class="flex flex-wrap items-center gap-x-5 gap-y-2 text-sm"><strong>{{ chartResult.chartType }}</strong><span>{{ chartResult.pointCount }} 點</span><span :class="chartResult.controlStatus === 'ready' ? 'text-emerald-600' : 'text-amber-600'">{{ chartResult.controlStatus === 'ready' ? '管制界線已建立' : '資料不足 20 點' }}</span><span class="text-slate-500">{{ chartResult.warning }}</span></div>

    <div class="grid gap-4 xl:grid-cols-[minmax(0,2fr)_minmax(320px,1fr)]">
      <section><h3 class="mb-2 text-sm font-black text-slate-700 dark:text-slate-200">管制趨勢</h3><div ref="chartEl" class="h-[390px] w-full border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900"></div></section>
      <section><h3 class="mb-2 text-sm font-black text-slate-700 dark:text-slate-200">最新事件 R1-R9 比較</h3><div ref="comparisonEl" class="h-[390px] w-full border border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900"></div></section>
    </div>

    <section class="overflow-hidden border border-slate-200 dark:border-slate-800"><div class="flex items-center justify-between border-b border-slate-200 px-4 py-3 dark:border-slate-800"><h3 class="text-sm font-black">原始量測</h3><span class="text-xs text-slate-500">最近 50 筆</span></div><div class="overflow-x-auto"><table class="w-full min-w-[900px] text-left text-sm"><thead class="bg-slate-100 text-xs text-slate-500 dark:bg-slate-900"><tr><th class="px-4 py-2">時間</th><th>位置</th><th>粒徑</th><th>Count</th><th>抽樣體積</th><th>儀器</th><th>來源</th></tr></thead><tbody><tr v-for="row in measurements.items" :key="row.id" class="border-t border-slate-100 dark:border-slate-800"><td class="px-4 py-2">{{ formatTime(row.measurementTime) }}</td><td class="font-bold">{{ row.location }}</td><td>{{ row.particleSize }} µm</td><td class="font-mono font-bold">{{ row.count }}</td><td>{{ row.samplingVolume == null ? '-' : `${row.samplingVolume} ${row.samplingVolumeUnit || ''}` }}</td><td>{{ row.deviceCode || '-' }}</td><td>{{ row.sourceSheet }} / {{ row.sourceColumn }}{{ row.sourceRow }}</td></tr><tr v-if="!measurements.items?.length"><td colspan="7" class="px-4 py-10 text-center text-slate-400">查無資料</td></tr></tbody></table></div></section>
  </section>
</template>
