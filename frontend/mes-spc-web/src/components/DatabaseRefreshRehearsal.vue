<script setup>
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { api } from "../api/client";

const { t } = useI18n();
const path = "/database-refresh-rehearsal";
const ticket = ref(null);
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

function preflight() {
  return run(async () => {
    const { data } = await api.post(path + "/preflight");
    ticket.value = data.confirmationId;
    job.value = null;
  });
}

function confirm() {
  return run(async () => {
    const { data } = await api.post(path + "/jobs/" + ticket.value);
    job.value = data;
    ticket.value = null;
  });
}

function check() {
  return run(async () => {
    const { data } = await api.get(path + "/jobs/" + job.value.id);
    job.value = data;
  });
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-3">
    <button type="button" class="rounded-xl bg-blue-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" :disabled="busy" @click="preflight">
      {{ t("spc.settings.refreshRehearsal.preflight") }}
    </button>
    <button v-if="ticket" type="button" class="rounded-xl bg-blue-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" :disabled="busy" @click="confirm">
      {{ t("spc.settings.refreshRehearsal.confirm") }}
    </button>
    <button v-if="job" type="button" class="rounded-xl bg-blue-700 px-4 py-2 text-sm font-semibold text-white disabled:opacity-50" :disabled="busy" @click="check">
      {{ t("spc.settings.refreshRehearsal.check") }}
    </button>
    <p role="status">
      <template v-if="error">{{ t("spc.settings.refreshRehearsal.error") }}</template>
      <template v-else-if="job">{{ t("spc.settings.refreshRehearsal." + job.status) }} · {{ job.id }}</template>
      <template v-else-if="ticket">{{ t("spc.settings.refreshRehearsal.ready") }}</template>
    </p>
  </div>
</template>
