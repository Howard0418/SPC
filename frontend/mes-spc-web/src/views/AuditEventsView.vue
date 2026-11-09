<script setup>
import { computed, onMounted, ref } from "vue";
import { Download, RefreshCw, ShieldCheck } from "lucide-vue-next";
import { api, getApiErrorDisplay } from "../api/client";
import { getCurrentUser } from "../utils/auth";
import { useI18n } from "vue-i18n";
import { describeAuditCode, describeAuditTarget } from "../utils/auditEventLabels";

const { t } = useI18n({ useScope: "global" });
const today = new Date();
const day = value => `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, "0")}-${String(value.getDate()).padStart(2, "0")}`;
const filters = ref({ from: day(new Date(today.getTime() - 7 * 86400000)), to: day(today), actor: "", pageKey: "", eventCode: "", target: "", outcome: "" });
const rows = ref([]);
const nextCursor = ref(null);
const loading = ref(false);
const exporting = ref(false);
const error = ref("");
const canExport = computed(() => getCurrentUser()?.permissions?.includes("admin.audit.export"));

function parameters(cursor = null) {
  return {
    fromUtc: `${filters.value.from}T00:00:00.000Z`, toUtc: `${filters.value.to}T23:59:59.999Z`,
    actor: filters.value.actor || undefined, pageKey: filters.value.pageKey || undefined,
    eventCode: filters.value.eventCode || undefined, target: filters.value.target || undefined,
    outcome: filters.value.outcome || undefined, cursor: cursor || undefined, pageSize: 50
  };
}
async function load(append = false) {
  loading.value = true; error.value = "";
  try {
    const { data } = await api.get("/v1/audit/events", { params: parameters(append ? nextCursor.value : null), auditActivityInternal: true });
    const page = data.data;
    rows.value = append ? [...rows.value, ...page.items] : page.items;
    nextCursor.value = page.nextCursor;
  } catch (e) { error.value = getApiErrorDisplay(e); }
  finally { loading.value = false; }
}
async function exportXlsx() {
  exporting.value = true; error.value = "";
  try {
    const response = await api.get("/v1/audit/events/export", { params: parameters(), responseType: "blob", auditActivityInternal: true });
    const url = URL.createObjectURL(response.data); const link = document.createElement("a");
    link.href = url; link.download = `SPC-Audit-${day(today).replaceAll("-", "")}.xlsx`; link.click(); URL.revokeObjectURL(url);
  } catch (e) { error.value = getApiErrorDisplay(e); }
  finally { exporting.value = false; }
}
const actor = row => row.actorDisplayName || row.actorAccount || row.actorServiceCode || row.actorType;
const display = (kind, value) => describeAuditCode(kind, value, t);
const target = row => describeAuditTarget(row.targetType, row.targetId, t);
const summary = row => {
  const value = row.summary; if (!value) return "-"; const parts = [];
  if (value.changedFields?.length) parts.push(`${t('spc.audit.changedFields')}：${value.changedFields.join('、')}`);
  if (value.permissionCodes?.length) parts.push(`${t('spc.audit.permissionsSet')}：${value.permissionCodes.join('、')}`);
  if (value.previousStatus || value.newStatus) parts.push(`${t('spc.audit.statusChange')}：${value.previousStatus || '-'} → ${value.newStatus || '-'}`);
  if (value.recordCount != null) parts.push(`${t('spc.audit.recordCount')}：${value.recordCount}`);
  return parts.join('；') || "-";
};
onMounted(() => load());
</script>

<template>
  <main class="p-4 md:p-6 space-y-5">
    <section class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 p-5 shadow-sm">
      <div class="flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-3"><ShieldCheck class="w-8 h-8 text-indigo-500"/><div><h2 class="text-2xl font-bold">{{ t('spc.audit.title') }}</h2><p class="text-sm text-slate-500">{{ t('spc.audit.subtitle') }}</p></div></div>
        <button v-if="canExport" class="px-4 py-2 rounded-lg bg-emerald-600 text-white disabled:opacity-50 flex gap-2" :disabled="exporting" @click="exportXlsx"><Download class="w-4"/>{{ t('spc.audit.export') }}</button>
      </div>
      <div class="grid grid-cols-1 md:grid-cols-4 gap-3 mt-5">
        <label class="text-sm">{{ t('spc.audit.from') }}<input v-model="filters.from" type="date" class="field"/></label>
        <label class="text-sm">{{ t('spc.audit.to') }}<input v-model="filters.to" type="date" class="field"/></label>
        <label class="text-sm">{{ t('spc.audit.actor') }}<input v-model.trim="filters.actor" class="field"/></label>
        <label class="text-sm">{{ t('spc.audit.page') }}<input v-model.trim="filters.pageKey" class="field" placeholder="analysis.spc.overall"/></label>
        <label class="text-sm">{{ t('spc.audit.event') }}<input v-model.trim="filters.eventCode" class="field" placeholder="read.query.execute"/></label>
        <label class="text-sm">{{ t('spc.audit.target') }}<input v-model.trim="filters.target" class="field"/></label>
        <label class="text-sm">{{ t('spc.audit.outcome') }}<select v-model="filters.outcome" class="field"><option value="">{{ t('spc.audit.all') }}</option><option>Succeeded</option><option>Denied</option><option>Failed</option></select></label>
        <div class="flex items-end"><button class="w-full px-4 py-2 rounded-lg bg-indigo-600 text-white disabled:opacity-50 flex justify-center gap-2" :disabled="loading" @click="load(false)"><RefreshCw class="w-4"/>{{ t('spc.audit.query') }}</button></div>
      </div>
      <p class="text-xs text-slate-500 mt-3">{{ t('spc.audit.rangeHint') }}</p>
      <p v-if="error" class="mt-3 text-red-600">{{ error }}</p>
    </section>
    <section class="rounded-2xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 overflow-auto shadow-sm">
      <table class="min-w-[1450px] w-full text-sm"><thead class="bg-slate-100 dark:bg-slate-800"><tr><th>{{ t('spc.audit.time') }}</th><th>{{ t('spc.audit.actor') }}</th><th>{{ t('spc.audit.event') }}</th><th>{{ t('spc.audit.outcome') }}</th><th>{{ t('spc.audit.page') }}</th><th>{{ t('spc.audit.target') }}</th><th>{{ t('spc.audit.summary') }}</th><th>HTTP</th><th>{{ t('spc.audit.source') }}</th></tr></thead>
        <tbody><tr v-for="row in rows" :key="row.id" class="border-t border-slate-200 dark:border-slate-700"><td>{{ new Date(row.occurredAtUtc).toLocaleString() }}</td><td>{{ actor(row) }}</td><td><span class="audit-label">{{ display('events', row.eventCode).label }}</span><span v-if="display('events', row.eventCode).code" class="audit-code">{{ display('events', row.eventCode).code }}</span></td><td><span class="audit-label">{{ display('outcomes', row.outcome).label }}</span><span v-if="display('outcomes', row.outcome).code" class="audit-code">{{ display('outcomes', row.outcome).code }}</span></td><td><span class="audit-label">{{ display('pages', row.pageKey).label }}</span><span v-if="display('pages', row.pageKey).code" class="audit-code">{{ display('pages', row.pageKey).code }}</span></td><td><span class="audit-label">{{ row.targetDisplayName || target(row).label }}</span><span v-if="target(row).code" class="audit-code">{{ target(row).code }} / {{ row.targetId }}</span></td><td class="max-w-[360px]">{{ summary(row) }}</td><td>{{ [row.httpMethod,row.httpStatus].filter(Boolean).join(' ') || '-' }}</td><td><span class="audit-label">{{ display('sources', row.source).label }}</span><span v-if="display('sources', row.source).code" class="audit-code">{{ display('sources', row.source).code }}</span></td></tr><tr v-if="!loading && !rows.length"><td colspan="9" class="text-center p-8 text-slate-500">{{ t('spc.audit.empty') }}</td></tr></tbody>
      </table>
      <div class="p-4 text-center"><button v-if="nextCursor" class="px-4 py-2 rounded-lg border border-slate-300" :disabled="loading" @click="load(true)">{{ t('spc.audit.more') }}</button></div>
    </section>
  </main>
</template>

<style scoped>
.field { @apply mt-1 block w-full rounded-lg border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-800 px-3 py-2; }
th, td { @apply px-3 py-3 text-left align-top; }
.audit-label { @apply block font-medium text-slate-800 dark:text-slate-100; }
.audit-code { @apply block mt-0.5 text-xs text-slate-500 dark:text-slate-400 font-mono; }
</style>
