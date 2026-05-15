<script setup>
import { onMounted, ref } from "vue";
import { api } from "../api/client";

const rows = ref([]);
const form = ref({
  workOrderNo: "",
  productId: 1,
  plannedQty: 100
});

const load = async () => {
  const { data } = await api.get("/v2/work-orders");
  rows.value = data;
};

const create = async () => {
  await api.post("/v2/work-orders", form.value);
  form.value.workOrderNo = "";
  await load();
};

const startWo = async (id) => {
  await api.post(`/v2/work-orders/${id}/start`);
  await load();
};

const completeWo = async (id) => {
  await api.post(`/v2/work-orders/${id}/complete`);
  await load();
};

onMounted(load);
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">V2 工單管理</h2>
  <div class="bg-white p-4 rounded shadow mb-4 flex gap-2 items-end">
    <label class="text-sm">工單號<input v-model="form.workOrderNo" class="border rounded px-2 py-1 ml-2" /></label>
    <label class="text-sm">產品ID<input v-model.number="form.productId" type="number" class="border rounded px-2 py-1 ml-2 w-24" /></label>
    <label class="text-sm">計畫數量<input v-model.number="form.plannedQty" type="number" class="border rounded px-2 py-1 ml-2 w-24" /></label>
    <button type="button" class="bg-blue-600 text-white px-3 py-1 rounded" @click="create">新增</button>
  </div>

  <div class="bg-white p-4 rounded shadow">
    <table class="w-full text-sm">
      <thead><tr><th class="text-left">工單</th><th class="text-left">狀態</th><th class="text-left">計畫/實際</th><th></th></tr></thead>
      <tbody>
        <tr v-for="r in rows" :key="r.id" class="border-t">
          <td>{{ r.workOrderNo }}</td>
          <td>{{ r.status }}</td>
          <td>{{ r.plannedQty }} / {{ r.actualQty }}</td>
          <td class="space-x-2">
            <button type="button" class="text-blue-600" @click="startWo(r.id)">開始</button>
            <button type="button" class="text-green-700" @click="completeWo(r.id)">完工</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
