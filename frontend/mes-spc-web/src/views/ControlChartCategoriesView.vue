<script setup>
import { onMounted, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";
const rows = ref([]);
const err = ref("");
onMounted(async () => {
  try { rows.value = (await api.get("/control-chart-categories")).data; }
  catch (e) { err.value = getApiErrorMessage(e); }
});
</script>

<template>
  <section class="p-4">
    <h1 class="text-xl font-bold mb-3">管制圖類別維護</h1>
    <p v-if="err" class="text-red-600 mb-2">{{ err }}</p>
    <pre class="text-xs overflow-auto rounded border p-3 bg-white">{{ rows }}</pre>
  </section>
</template>
