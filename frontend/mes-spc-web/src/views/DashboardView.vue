<script setup>
import { onMounted, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";

const summary = ref({ todayBatchCount: 0, todayAlertCount: 0, alertRate: 0, recentAlerts: [] });
const loadError = ref("");
onMounted(async () => {
  loadError.value = "";
  try {
    const { data } = await api.get("/dashboard/summary");
    summary.value = data;
  } catch (e) {
    loadError.value = getApiErrorMessage(e);
  }
});
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">Dashboard</h2>
  <p v-if="loadError" class="mb-4 rounded border border-red-200 bg-red-50 p-3 text-sm text-red-800">{{ loadError }}</p>
  <div class="grid grid-cols-3 gap-4 mb-6">
    <div class="bg-white p-4 rounded shadow">今日批次：{{ summary.todayBatchCount }}</div>
    <div class="bg-white p-4 rounded shadow">今日異常：{{ summary.todayAlertCount }}</div>
    <div class="bg-white p-4 rounded shadow">異常率：{{ (summary.alertRate * 100).toFixed(2) }}%</div>
  </div>
  <div class="bg-white p-4 rounded shadow">
    <h3 class="font-semibold mb-3">最近異常</h3>
    <ul class="space-y-2">
      <li v-for="a in summary.recentAlerts" :key="a.id" class="text-sm border-b pb-1">
        {{ new Date(a.occurredAt).toLocaleString() }} - {{ a.message }}
      </li>
    </ul>
  </div>
</template>
