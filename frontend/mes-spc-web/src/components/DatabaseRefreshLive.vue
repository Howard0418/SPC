<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { api } from "../api/client";

const { t } = useI18n();
const base = "/database-refresh";
const targetDatabase = "PMR_SPC_TEST";
const preflight = ref(null);
const target = ref("");
const job = ref(null);
const busy = ref(false);
const error = ref(false);

async function run(action) {
  busy.value = true;
  error.value = false;
  try {
    await action();
  } catch {
    error.value = true;
  } finally {
    busy.value = false;
  }
}

function inspect() {
  return run(async () => {
    preflight.value = null;
    target.value = "";
    const { data } = await api.post(base + "/preflight");
    preflight.value = data;
  });
}

function submit() {
  return run(async () => {
    const { data } = await api.post(base + "/jobs/" + preflight.value.confirmationId, null, { params: { target: target.value } });
    job.value = data;
    preflight.value = null;
  });
}

function refresh() {
  return run(async () => {
    const { data } = await api.get(base + "/jobs/" + job.value.id);
    job.value = data;
  });
}
</script>

<template>
  <section class="space-y-3">
    <button type="button" class="rounded-xl bg-blue-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50"
      :disabled="busy || job?.status === 'queued' || job?.status === 'running'" @click="inspect">
      {{ t("spc.settings.refreshLive.preflight") }}
    </button>
    <form v-if="preflight" class="space-y-2" @submit.prevent="submit">
      <p class="font-mono">{{ preflight.snapshot.source }} → {{ preflight.snapshot.target }}</p>
      <p>{{ t("spc.settings.refreshLive.scope", { tables: preflight.snapshot.replacementTables, rows: preflight.snapshot.measurementRows }) }}</p>
      <p>{{ new Date(preflight.snapshot.snapshotUtc).toLocaleString() }}</p>
      <label>
        {{ t("spc.settings.refreshLive.target") }}
        <input v-model="target" autocomplete="off" class="ml-2 rounded border px-2 py-1 dark:bg-slate-900" />
      </label>
      <button type="submit" class="ml-2 rounded bg-blue-700 px-4 py-2 text-white disabled:opacity-50"
        :disabled="busy || target !== targetDatabase">
        {{ t("spc.settings.refreshLive.confirm") }}
      </button>
    </form>
    <p v-if="error" role="alert">{{ t("spc.settings.refreshLive.error") }}</p>
    <div v-if="job" class="flex items-center gap-3">
      <p role="status">{{ t("spc.settings.refreshLive." + job.status) }} · {{ job.id }}</p>
      <button type="button" class="rounded border px-3 py-1" :disabled="busy" @click="refresh">
        {{ t("spc.settings.refreshLive.check") }}
      </button>
    </div>
  </section>
</template>
