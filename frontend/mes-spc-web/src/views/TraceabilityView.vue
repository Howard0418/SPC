<script setup>
import { ref } from "vue";
import { api } from "../api/client";

const q = ref({ workOrderNo: "", lotNo: "", serialNo: "" });
const data = ref({ batches: [], alerts: [] });

const search = async () => {
  const { data: res } = await api.get("/v2/traceability", { params: q.value });
  data.value = res;
};
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">V2 追溯查詢</h2>
  <div class="bg-white p-4 rounded shadow mb-4 space-x-2">
    <input v-model="q.workOrderNo" class="border rounded px-2 py-1 w-40" placeholder="WorkOrderNo" />
    <input v-model="q.lotNo" class="border rounded px-2 py-1 w-40" placeholder="LotNo" />
    <input v-model="q.serialNo" class="border rounded px-2 py-1 w-40" placeholder="SerialNo" />
    <button type="button" class="bg-blue-600 text-white px-3 py-1 rounded" @click="search">查詢</button>
  </div>
  <div class="grid grid-cols-2 gap-4">
    <div class="bg-white p-4 rounded shadow text-sm">批次數: {{ data.batches.length }}</div>
    <div class="bg-white p-4 rounded shadow text-sm">關聯異常數: {{ data.alerts.length }}</div>
  </div>
</template>
