<script setup>
import { onMounted, ref } from "vue";
import { api } from "../api/client";

const rows = ref([]);
const form = ref({
  itemCode: "",
  itemName: "",
  dataType: 1,
  unit: "mm",
  usl: null,
  lsl: null,
  ucl: null,
  lcl: null,
  targetValue: null,
  isSpcEnabled: true
});

const load = async () => {
  const { data } = await api.get("/inspection-items");
  rows.value = data;
};

const save = async () => {
  await api.post("/inspection-items", form.value);
  form.value = {
    itemCode: "",
    itemName: "",
    dataType: 1,
    unit: "mm",
    usl: null,
    lsl: null,
    ucl: null,
    lcl: null,
    targetValue: null,
    isSpcEnabled: true
  };
  await load();
};

onMounted(load);
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">檢測項目管理</h2>
  <div class="bg-white p-4 rounded shadow mb-4">
    <div class="grid md:grid-cols-4 gap-2">
      <input v-model="form.itemCode" placeholder="Item Code" class="border p-2 rounded" />
      <input v-model="form.itemName" placeholder="Item Name" class="border p-2 rounded" />
      <select v-model.number="form.dataType" class="border p-2 rounded">
        <option :value="1">Numeric</option>
        <option :value="2">Text</option>
        <option :value="3">Boolean</option>
      </select>
      <input v-model="form.unit" placeholder="Unit" class="border p-2 rounded" />
      <input v-model.number="form.usl" type="number" step="0.001" placeholder="USL" class="border p-2 rounded" />
      <input v-model.number="form.lsl" type="number" step="0.001" placeholder="LSL" class="border p-2 rounded" />
      <input v-model.number="form.ucl" type="number" step="0.001" placeholder="UCL" class="border p-2 rounded" />
      <input v-model.number="form.lcl" type="number" step="0.001" placeholder="LCL" class="border p-2 rounded" />
      <input v-model.number="form.targetValue" type="number" step="0.001" placeholder="TargetValue" class="border p-2 rounded" />
      <label class="flex items-center gap-2 text-sm">
        <input v-model="form.isSpcEnabled" type="checkbox" />
        啟用 SPC
      </label>
      <button type="button" @click="save" class="bg-blue-600 text-white px-3 py-2 rounded">新增檢測項目</button>
    </div>
  </div>
  <div class="bg-white p-4 rounded shadow overflow-auto">
    <table class="w-full text-sm">
      <thead>
        <tr>
          <th class="text-left py-2">ID</th>
          <th class="text-left py-2">Code</th>
          <th class="text-left py-2">Name</th>
          <th class="text-left py-2">Type</th>
          <th class="text-left py-2">USL</th>
          <th class="text-left py-2">LSL</th>
          <th class="text-left py-2">SPC</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r.id" class="border-t">
          <td class="py-2">{{ r.id }}</td>
          <td class="py-2">{{ r.itemCode }}</td>
          <td class="py-2">{{ r.itemName }}</td>
          <td class="py-2">{{ r.dataType }}</td>
          <td class="py-2">{{ r.usl }}</td>
          <td class="py-2">{{ r.lsl }}</td>
          <td class="py-2">{{ r.isSpcEnabled ? "Y" : "N" }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
