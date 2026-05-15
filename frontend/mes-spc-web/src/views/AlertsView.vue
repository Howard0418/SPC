<script setup>
import { onMounted, ref } from "vue";
import { api } from "../api/client";

const rows = ref([]);
const load = async () => {
  const { data } = await api.get("/alerts");
  rows.value = data;
};
const ack = async (id) => {
  await api.post(`/alerts/${id}/ack`);
  await load();
};
onMounted(load);
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">異常清單</h2>
  <div class="bg-white p-4 rounded shadow">
    <table class="w-full text-sm">
      <thead><tr><th class="text-left">時間</th><th class="text-left">訊息</th><th></th></tr></thead>
      <tbody>
        <tr v-for="r in rows" :key="r.id" class="border-t">
          <td>{{ new Date(r.occurredAt).toLocaleString() }}</td>
          <td>{{ r.message }}</td>
          <td><button v-if="!r.isAcknowledged" type="button" @click="ack(r.id)" class="text-blue-600">Ack</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
