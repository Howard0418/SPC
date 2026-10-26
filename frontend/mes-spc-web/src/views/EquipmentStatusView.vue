<script setup>
const dtoText = (data, fallback) => displayUiMessage(t, getApiMessageDisplay(data, fallback));
import {useI18n} from "vue-i18n";
import {displayUiMessage} from "../i18n/uiMessage";
const {t}=useI18n({useScope:"global"});
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import { Activity, AlertCircle, CircleCheck, Clock3, RefreshCw, Search, ServerCrash, Wifi } from "lucide-vue-next";
import { api, getApiMessageDisplay, getApiErrorDisplay } from "../api/client";

const loading = ref(false);
const error = ref("");
const result = ref(null);
const search = ref("");
const statusFilter = ref("all");
const sourceFilter = ref("all");
const alarmOnly = ref(false);
let refreshTimer;

const sources = computed(() => result.value?.sources || []);
const devices = computed(() => sources.value.flatMap(source => source.devices || []));
const refreshedAt = computed(() => formatDate(result.value?.refreshedAt));
const onlineCount = computed(() => devices.value.filter(x => x.status === "online").length);
const problemCount = computed(() => devices.value.filter(x => x.status !== "online").length);
const activeAlarmCount = computed(() => devices.value.reduce((total, device) => total + (device.activeAlarmCount || 0), 0));
const alarmDeviceCount = computed(() => devices.value.filter(device => device.activeAlarmCount > 0).length);
const thresholdSeconds = computed(() => result.value?.freshnessThresholdSeconds || 90);
const statusRank = { unavailable: 0, stale: 1, online: 2 };
const filteredSources = computed(() => sources.value
  .filter(source => sourceFilter.value === "all" || source.sourceId === sourceFilter.value)
  .map(source => ({
    ...source,
    allDevices: source.devices || [],
    devices: [...(source.devices || [])]
      .filter(device => statusFilter.value === "all" || device.status === statusFilter.value)
      .filter(device => !alarmOnly.value || device.activeAlarmCount > 0)
      .filter(device => !search.value.trim() || device.equipmentId.toLowerCase().includes(search.value.trim().toLowerCase()))
      .sort((a, b) => (statusRank[a.status] ?? 9) - (statusRank[b.status] ?? 9) || a.equipmentId.localeCompare(b.equipmentId))
  }))
  .filter(source => source.devices.length || (!search.value.trim() && statusFilter.value === "all" && !alarmOnly.value)));

function formatDate(value) {
  if (!value) return "—";
  return new Intl.DateTimeFormat("zh-TW", {
    year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit", second: "2-digit", hour12: false
  }).format(new Date(value));
}
function formatAge(seconds) {
  if (seconds == null) return "—";
  if (seconds < 10) return t('spc.equipment.justNow');
  if (seconds < 60) return t('spc.equipment.secondsAgo',{seconds:Math.round(seconds)});
  if (seconds < 3600) return t('spc.equipment.minutesAgo',{minutes:Math.floor(seconds/60),seconds:Math.round(seconds%60)});
  if (seconds < 86400) return t('spc.equipment.hoursAgo',{hours:Math.floor(seconds/3600),minutes:Math.floor((seconds%3600)/60)});
  return t('spc.equipment.daysAgo',{days:Math.floor(seconds/86400)});
}
function statusMeta(status) {
  if (status === "online") return { text: t('spc.equipment.online'), className: "bg-emerald-100 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-300", icon: CircleCheck };
  if (status === "stale") return { text: t('spc.equipment.stale'), className: "bg-amber-100 text-amber-700 dark:bg-amber-950/50 dark:text-amber-300", icon: Clock3 };
  return { text: t('spc.equipment.unavailable'), className: "bg-red-100 text-red-700 dark:bg-red-950/50 dark:text-red-300", icon: ServerCrash };
}
function sourceMeta(source) {
  if (source.status !== "online") return { text: t('spc.equipment.sourceOffline'), className: "badge-unavailable" };
  const allDevices = source.allDevices || source.devices;
  const normal = allDevices.filter(x => x.status === "online").length;
  const stale = allDevices.filter(x => x.status === "stale").length;
  const unavailable = allDevices.length - normal - stale;
  const text = t('spc.equipment.sourceSummary',{normal,stale}) + (unavailable ? t('spc.equipment.unavailableSummary',{count:unavailable}) : '');
  return { text, className: stale || unavailable ? "badge-warning" : "badge-online" };
}
function operatingMeta(state) {
  if (state === "running") return { text: t('spc.equipment.running'), className: "text-emerald-700 bg-emerald-50 dark:text-emerald-300 dark:bg-emerald-950/40" };
  if (state === "stopped") return { text: t('spc.equipment.stopped'), className: "text-slate-700 bg-slate-100 dark:text-slate-300 dark:bg-slate-900" };
  if (state === "unknown") return { text: t('spc.equipment.unknown'), className: "text-amber-700 bg-amber-50 dark:text-amber-300 dark:bg-amber-950/40" };
  return { text: t('spc.equipment.noRunningMapping'), className: "text-slate-500 bg-slate-50 dark:bg-slate-900" };
}
async function load() {
  if (loading.value) return;
  loading.value = true;
  error.value = "";
  try {
    const { data } = await api.get("/equipment-status", { timeout: 15000 });
    result.value = data;
    if (!data?.isConfigured) error.value = getApiMessageDisplay(data, data?.message) || {key:"spc.equipment.notConfigured"};
  } catch (e) {
    error.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}
onMounted(() => {
  load();
  refreshTimer = window.setInterval(load, 30000);
});
onBeforeUnmount(() => window.clearInterval(refreshTimer));
</script>

<template>
  <section class="space-y-6">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="flex items-center gap-2 text-2xl font-black text-slate-800 dark:text-white"><Activity class="h-7 w-7 text-cyan-500" /> {{ t('spc.equipment.statusTitle') }}</h1>
        <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">{{ t('spc.equipment.refreshHelp',{seconds:thresholdSeconds}) }}</p>
      </div>
      <button type="button" class="btn-outline" :disabled="loading" data-testid="equipment-reload" @click="load"><RefreshCw class="h-4 w-4" :class="{ 'animate-spin': loading }" />{{ t('spc.equipment.refresh') }}</button>
    </header>

    <div v-if="error" class="flex items-center gap-2 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-900 dark:bg-red-950/40 dark:text-red-300"><AlertCircle class="h-5 w-5 shrink-0" />{{ displayUiMessage(t,error) }}</div>

    <div v-if="result?.isConfigured" class="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-4">
      <div class="stat-card"><Wifi class="h-5 w-5 text-emerald-500" /><div><p>{{ t('spc.equipment.online') }}</p><strong class="text-emerald-600">{{ onlineCount }} {{ t('spc.equipment.devices') }}</strong></div></div>
      <div class="stat-card"><AlertCircle class="h-5 w-5 text-amber-500" /><div><p>{{ t('spc.equipment.attention') }}</p><strong class="text-amber-600">{{ problemCount }} {{ t('spc.equipment.devices') }}</strong></div></div>
      <button type="button" class="stat-card text-left" :class="{ 'ring-2 ring-red-400': alarmOnly }" @click="alarmOnly = !alarmOnly"><AlertCircle class="h-5 w-5 text-red-500" /><div><p>{{ t('spc.equipment.activeAlarms') }}</p><strong class="text-red-600">{{ activeAlarmCount }} {{ t('spc.equipment.alarmDeviceSeparator') }}{{ alarmDeviceCount }} {{ t('spc.equipment.devices') }}</strong></div></button>
      <div class="stat-card"><Clock3 class="h-5 w-5 text-blue-500" /><div><p>{{ t('spc.equipment.refreshedAt') }}</p><strong class="text-base">{{ refreshedAt }}</strong></div></div>
    </div>

    <div v-if="result?.isConfigured" class="flex flex-wrap gap-3 rounded-xl border border-slate-200 bg-white p-3 dark:border-slate-700 dark:bg-slate-800">
      <label class="relative min-w-56 flex-1"><Search class="absolute left-3 top-2.5 h-4 w-4 text-slate-400" /><input data-testid="equipment-search" v-model="search" class="filter-control pl-9" :placeholder="t('spc.equipment.search')" /></label>
      <select data-testid="equipment-sourceFilter" v-model="sourceFilter" class="filter-control sm:w-52"><option value="all">{{ t('spc.equipment.allSources') }}</option><option v-for="source in sources" :key="source.sourceId" :value="source.sourceId">{{ source.displayName }}</option></select>
      <select data-testid="equipment-statusFilter" v-model="statusFilter" class="filter-control sm:w-44"><option value="all">{{ t('spc.equipment.allStatuses') }}</option><option value="online">{{ t('spc.equipment.online') }}</option><option value="stale">{{ t('spc.equipment.stale') }}</option><option value="unavailable">{{ t('spc.equipment.unavailable') }}</option></select>
      <label class="inline-flex items-center gap-2 rounded-lg border border-slate-300 px-3 text-sm font-bold text-slate-700 dark:border-slate-600 dark:text-slate-200"><input data-testid="equipment-alarmOnly" v-model="alarmOnly" type="checkbox">{{ t('spc.equipment.alarmsOnly') }}</label>
    </div>

    <div v-if="result?.isConfigured" class="space-y-6">
      <section v-for="source in filteredSources" :key="source.sourceId" class="space-y-3">
        <div class="flex flex-wrap items-center justify-between gap-2"><div><h2 class="font-black text-slate-800 dark:text-white">{{ source.displayName }}</h2><p class="text-xs text-slate-500">{{ t('spc.equipment.sourceCode') }}{{ source.sourceId }}</p></div><span :class="sourceMeta(source).className">{{ sourceMeta(source).text }}</span></div>
        <p v-if="source.message" class="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-700 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-300">{{ dtoText(source, source.message) }}</p>
        <div v-if="source.devices.length" class="grid grid-cols-1 gap-4 lg:grid-cols-3">
      <article v-for="device in source.devices" :key="`${device.sourceId}-${device.equipmentId}`" class="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm dark:border-slate-700 dark:bg-slate-800">
        <div class="flex items-start justify-between gap-3"><div><h2 class="text-lg font-black text-slate-800 dark:text-white">{{ device.equipmentId }}</h2><p class="mt-1 text-xs text-slate-500">{{ t('spc.equipment.sourceDescription') }}</p></div><span class="inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-bold" :class="statusMeta(device.status).className"><component :is="statusMeta(device.status).icon" class="h-3.5 w-3.5" />{{ statusMeta(device.status).text }}</span></div>
        <dl class="mt-5 space-y-3 text-sm"><div class="flex justify-between gap-4"><dt class="text-slate-500">{{ t('spc.equipment.lastData') }}</dt><dd class="font-semibold text-right">{{ formatDate(device.lastUpdatedAt) }}</dd></div><div class="flex justify-between gap-4"><dt class="text-slate-500">{{ t('spc.equipment.age') }}</dt><dd class="font-semibold">{{ formatAge(device.ageSeconds) }}</dd></div><div class="flex justify-between gap-4"><dt class="text-slate-500">{{ t('spc.equipment.channels') }}</dt><dd class="font-semibold">{{ device.channelCount }} {{ t('spc.equipment.items') }}</dd></div></dl>
        <p v-if="device.message" class="mt-4 rounded-lg bg-red-50 p-2 text-xs text-red-700 dark:bg-red-950/40 dark:text-red-300">{{ dtoText(device, device.message) }}</p>
        <div v-else class="mt-4 grid grid-cols-2 gap-2 text-xs font-bold">
          <div class="rounded-lg p-2" :class="operatingMeta(device.operatingState).className">{{ operatingMeta(device.operatingState).text }}</div>
          <div class="rounded-lg p-2" :class="!device.configuredAlarmCount ? 'bg-slate-50 text-slate-500 dark:bg-slate-900' : device.activeAlarmCount > 0 ? 'bg-red-50 text-red-700 dark:bg-red-950/40 dark:text-red-300' : 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/40 dark:text-emerald-300'">{{ device.configuredAlarmCount ? t('spc.equipment.alarmingCount',{count:device.activeAlarmCount}) : t('spc.equipment.noAlarmMapping') }}</div>
        </div>
        <p v-if="device.operatingChannelId" class="mt-2 text-xs text-slate-500">{{ t('spc.equipment.runningSignal') }}{{ device.operatingPointName }}（{{ device.operatingChannelId }} = {{ device.operatingRawValue ?? '—' }}）</p>
        <div v-if="device.activeAlarms?.length" class="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-800 dark:border-red-900 dark:bg-red-950/40 dark:text-red-200">
          <p class="mb-2 font-black">{{ t('spc.equipment.activeAlarms') }}</p>
          <ul class="space-y-1"><li v-for="alarm in device.activeAlarms.slice(0, 3)" :key="alarm.channelId"><RouterLink class="font-bold hover:underline" :to="{ path: '/equipment-points', query: { sourceId: device.sourceId, equipmentId: device.equipmentId, role: 'ALARM', q: alarm.channelId } }">{{ alarm.name }} <span class="text-red-500">{{ alarm.channelId }} = {{ alarm.rawValue }}</span></RouterLink></li></ul>
          <p v-if="device.activeAlarms.length > 3" class="mt-2">{{ t('spc.equipment.more') }} {{ device.activeAlarms.length - 3 }} {{ t('spc.equipment.alarmUnit') }}</p>
        </div>
        <div class="mt-4 flex flex-wrap gap-2 border-t border-slate-100 pt-4 dark:border-slate-700">
          <RouterLink class="device-action" :to="{ path: '/equipment-points', query: { sourceId: device.sourceId, equipmentId: device.equipmentId } }">{{ t('spc.equipment.viewPoints') }}</RouterLink>
          <RouterLink v-if="device.configuredAlarmCount" class="device-action" :to="{ path: '/equipment-points', query: { sourceId: device.sourceId, equipmentId: device.equipmentId, role: 'ALARM' } }">{{ t('spc.equipment.alarmSettings') }}</RouterLink>
          <RouterLink class="device-action" :to="{ path: '/equipment-monitor', query: { sourceId: device.sourceId, equipmentId: device.equipmentId } }">{{ t('spc.equipment.monitor') }}</RouterLink>
        </div>
      </article>
        </div>
      </section>
      <p v-if="!filteredSources.length" class="rounded-xl border border-dashed border-slate-300 p-8 text-center text-sm text-slate-500 dark:border-slate-700">{{ t('spc.equipment.noDevices') }}</p>
    </div>
  </section>
</template>

<style scoped>
.btn-outline,.stat-card{display:inline-flex;align-items:center;gap:.5rem;border-radius:.75rem;font-size:.875rem;font-weight:700}.btn-outline{border:1px solid #94a3b8;padding:.55rem .8rem}.stat-card{min-height:76px;border:1px solid #e2e8f0;background:#fff;padding:1rem;box-shadow:0 1px 2px rgb(15 23 42 / .05)}.stat-card p{font-size:.75rem;color:#64748b}.stat-card strong{display:block;font-size:1.35rem;color:#0f172a}.dark .stat-card{border-color:#334155;background:#1e293b}.dark .stat-card strong{color:#f8fafc}
.filter-control{width:100%;border:1px solid #cbd5e1;border-radius:.65rem;background:#fff;padding:.5rem .75rem;font-size:.875rem}.dark .filter-control{border-color:#475569;background:#0f172a;color:#f8fafc}.badge-online,.badge-warning,.badge-unavailable{border-radius:9999px;padding:.35rem .65rem;font-size:.75rem;font-weight:700}.badge-online{background:#d1fae5;color:#047857}.badge-warning{background:#fef3c7;color:#b45309}.badge-unavailable{background:#fee2e2;color:#b91c1c}.dark .badge-online{background:rgb(6 78 59 / .5);color:#6ee7b7}.dark .badge-warning{background:rgb(120 53 15 / .5);color:#fcd34d}.dark .badge-unavailable{background:rgb(127 29 29 / .5);color:#fca5a5}
.device-action{border:1px solid #cbd5e1;border-radius:.55rem;padding:.4rem .65rem;font-size:.75rem;font-weight:700;color:#0369a1}.device-action:hover{background:#f0f9ff}.dark .device-action{border-color:#475569;color:#7dd3fc}.dark .device-action:hover{background:#0f172a}
</style>
