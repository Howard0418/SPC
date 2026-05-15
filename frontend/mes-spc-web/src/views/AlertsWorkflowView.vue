<script setup>
import { onMounted, ref } from "vue";
import { api } from "../api/client";

const rows = ref([]);
const status = ref("");
const edit = ref({});

const load = async () => {
  const { data } = await api.get("/v2/alerts", { params: status.value ? { status: status.value } : {} });
  rows.value = data;
};

const save = async (id) => {
  const row = edit.value[id] || {};
  await api.put(`/v2/alerts/${id}/workflow`, {
    status: row.status || "InProgress",
    rootCause: row.rootCause || null,
    correctiveAction: row.correctiveAction || null,
    responsibleUser: row.responsibleUser || null
  });
  await load();
};

const ensureEdit = (row) => {
  if (!edit.value[row.id]) {
    edit.value[row.id] = {
      status: row.status,
      rootCause: row.rootCause || "",
      correctiveAction: row.correctiveAction || "",
      responsibleUser: row.responsibleUser || ""
    };
  }
  return edit.value[row.id];
};

onMounted(load);
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">V2 異常流程</h2>
  <div class="mb-4">
    <input v-model="status" class="border rounded px-2 py-1 w-32" placeholder="status" />
    <button type="button" class="ml-2 bg-slate-700 text-white px-3 py-1 rounded" @click="load">篩選</button>
  </div>
  <div class="bg-white p-4 rounded shadow overflow-auto">
    <table class="w-full text-sm">
      <thead><tr><th class="text-left">ID</th><th class="text-left">訊息</th><th class="text-left">狀態</th><th></th></tr></thead>
      <tbody>
        <tr v-for="r in rows" :key="r.id" class="border-t">
          <td>{{ r.id }}</td>
          <td>{{ r.message }}</td>
          <td><input v-model="ensureEdit(r).status" class="border rounded px-2 py-1 w-28" :placeholder="r.status" /></td>
          <td><button type="button" class="text-blue-600" @click="save(r.id)">更新流程</button></td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
