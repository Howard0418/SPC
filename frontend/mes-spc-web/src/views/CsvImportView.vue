<script setup>
import { ref } from "vue";
import { api } from "../api/client";

const file = ref(null);
const result = ref(null);
const productId = ref(1);
const stationId = ref(1);

const upload = async () => {
  if (!file.value) return;
  const form = new FormData();
  form.append("file", file.value);
  const { data } = await api.post(`/measurements/import-csv?productId=${productId.value}&stationId=${stationId.value}`, form);
  result.value = data;
};
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">CSV 匯入</h2>
  <div class="bg-white p-4 rounded shadow space-y-3">
    <input type="number" v-model.number="productId" class="border p-2 w-full" />
    <input type="number" v-model.number="stationId" class="border p-2 w-full" />
    <input type="file" @change="file = $event.target.files[0]" />
    <button type="button" @click="upload" class="bg-blue-600 text-white px-3 py-2 rounded">上傳</button>
    <pre class="text-xs">{{ result }}</pre>
  </div>
</template>
