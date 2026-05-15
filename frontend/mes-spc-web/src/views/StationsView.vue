<script setup>
import { onMounted, ref } from "vue";
import { api } from "../api/client";

const rows = ref([]);
const form = ref({ stationCode: "", stationName: "", isActive: true });

const load = async () => {
  const { data } = await api.get("/stations");
  rows.value = data;
};

const save = async () => {
  await api.post("/stations", form.value);
  form.value = { stationCode: "", stationName: "", isActive: true };
  await load();
};

onMounted(load);
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">工站管理</h2>
  <div class="bg-white p-4 rounded shadow mb-4">
    <div class="grid md:grid-cols-4 gap-2">
      <input v-model="form.stationCode" placeholder="Station Code" class="border p-2 rounded" />
      <input v-model="form.stationName" placeholder="Station Name" class="border p-2 rounded" />
      <label class="flex items-center gap-2 text-sm">
        <input v-model="form.isActive" type="checkbox" />
        啟用
      </label>
      <button type="button" @click="save" class="bg-blue-600 text-white px-3 py-2 rounded">新增工站</button>
    </div>
  </div>
  <div class="bg-white p-4 rounded shadow overflow-auto">
    <table class="w-full text-sm">
      <thead>
        <tr>
          <th class="text-left py-2">ID</th>
          <th class="text-left py-2">Code</th>
          <th class="text-left py-2">Name</th>
          <th class="text-left py-2">啟用</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r.id" class="border-t">
          <td class="py-2">{{ r.id }}</td>
          <td class="py-2">{{ r.stationCode }}</td>
          <td class="py-2">{{ r.stationName }}</td>
          <td class="py-2">{{ r.isActive ? "Y" : "N" }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
