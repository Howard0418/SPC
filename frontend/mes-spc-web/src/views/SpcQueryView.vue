<script setup>
import { ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";

const ppcId = ref("");
const points = ref([]);
const err = ref("");

async function query() {
  err.value = "";
  try {
    const { data } = await api.get("/spc/chart", { params: { partProcessCharacteristicId: ppcId.value } });
    points.value = data;
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}
</script>

<template>
  <section class="p-4 space-y-3">
    <h1 class="text-xl font-bold">SPC 管制圖查詢</h1>
    <div class="flex gap-2">
      <input v-model="ppcId" class="border rounded px-2 py-1" placeholder="PartProcessCharacteristicId" />
      <button class="px-3 py-2 rounded bg-blue-600 text-white" @click="query">查詢</button>
    </div>
    <p v-if="err" class="text-red-600">{{ err }}</p>
    <pre class="text-xs overflow-auto border rounded p-2 bg-white">{{ points }}</pre>
  </section>
</template>
