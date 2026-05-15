<script setup>
import { ref } from "vue";
import { api } from "../api/client";

const payload = ref({
  batchNo: `MANUAL-${Date.now()}`,
  productId: 1,
  stationId: 1,
  measuredAt: new Date().toISOString(),
  operatorName: "",
  values: [{ inspectionItemId: 1, sampleNo: 1, valueNumeric: 0 }]
});
const result = ref(null);

const submit = async () => {
  const { data } = await api.post("/measurement-batches", payload.value);
  result.value = data;
};
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">量測資料輸入</h2>
  <div class="bg-white p-4 rounded shadow space-y-3">
    <input v-model.number="payload.productId" class="border p-2 w-full" placeholder="ProductId" />
    <input v-model.number="payload.stationId" class="border p-2 w-full" placeholder="StationId" />
    <input v-model.number="payload.values[0].inspectionItemId" class="border p-2 w-full" placeholder="InspectionItemId" />
    <input v-model.number="payload.values[0].valueNumeric" class="border p-2 w-full" placeholder="ValueNumeric" />
    <button type="button" @click="submit" class="bg-blue-600 text-white px-3 py-2 rounded">送出</button>
    <pre class="text-xs">{{ result }}</pre>
  </div>
</template>
