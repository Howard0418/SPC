<script setup>
import { ref } from "vue";
import { api } from "../api/client";

const openForm = ref({ workOrderId: 1, stationId: 1, lotNo: "", serialNo: "", operatorName: "OP" });
const sessionId = ref(null);
const context = ref(null);
const batchNo = ref("");
const result = ref(null);
const measurementInputs = ref({});
const keepValuesAfterSubmit = ref(true);
const submitError = ref("");

const openSession = async () => {
  const { data } = await api.post("/v2/station-ops/open-session", openForm.value);
  sessionId.value = data.sessionId;
  const c = await api.get(`/v2/station-ops/${sessionId.value}/context`);
  context.value = c.data;
  measurementInputs.value = {};
  for (const item of c.data.inspectionItems) {
    measurementInputs.value[item.id] = item.targetValue ?? null;
  }
};

const submit = async () => {
  if (!sessionId.value || !context.value) return;
  submitError.value = "";
  const missingItems = context.value.inspectionItems.filter((x) => measurementInputs.value[x.id] === null || measurementInputs.value[x.id] === "");
  if (missingItems.length > 0) {
    submitError.value = `尚有 ${missingItems.length} 個量測值未填寫，請先完成輸入。`;
    return;
  }
  const values = context.value.inspectionItems.map((x) => ({
    inspectionItemId: x.id,
    sampleNo: 1,
    valueNumeric: measurementInputs.value[x.id] === null || measurementInputs.value[x.id] === ""
      ? null
      : Number(measurementInputs.value[x.id])
  }));
  const { data } = await api.post(`/v2/station-ops/${sessionId.value}/submit-measurements`, {
    batchNo: batchNo.value || `WO-${Date.now()}`,
    measuredAt: new Date().toISOString(),
    values
  });
  result.value = data;
  if (!keepValuesAfterSubmit.value) {
    for (const item of context.value.inspectionItems) {
      measurementInputs.value[item.id] = null;
    }
  }
};

const isOutOfSpec = (item) => {
  const val = measurementInputs.value[item.id];
  if (val === null || val === "") return false;
  if (item.lsl !== null && item.lsl !== undefined && Number(val) < item.lsl) return true;
  if (item.usl !== null && item.usl !== undefined && Number(val) > item.usl) return true;
  return false;
};
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">V2 工站作業</h2>
  <div class="bg-white p-4 rounded shadow mb-4 space-x-2">
    <input v-model.number="openForm.workOrderId" type="number" class="border rounded px-2 py-1 w-24" placeholder="WO ID" />
    <input v-model.number="openForm.stationId" type="number" class="border rounded px-2 py-1 w-24" placeholder="站點ID" />
    <input v-model="openForm.lotNo" class="border rounded px-2 py-1 w-32" placeholder="LotNo" />
    <input v-model="openForm.serialNo" class="border rounded px-2 py-1 w-32" placeholder="SerialNo" />
    <button type="button" class="bg-blue-600 text-white px-3 py-1 rounded" @click="openSession">開站</button>
  </div>
  <div v-if="context" class="bg-white p-4 rounded shadow space-y-2">
    <div>Session: {{ sessionId }} / 工單: {{ context.workOrder.workOrderNo }}</div>
    <div>檢測項目數: {{ context.inspectionItems.length }}</div>
    <label class="inline-flex items-center gap-2 text-sm">
      <input v-model="keepValuesAfterSubmit" type="checkbox" />
      送出後保留輸入值
    </label>
    <div class="overflow-auto">
      <table class="w-full text-sm">
        <thead>
          <tr>
            <th class="text-left">項目</th>
            <th class="text-left">規格</th>
            <th class="text-left">實際值</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in context.inspectionItems" :key="item.id" class="border-t">
            <td>{{ item.itemCode }} / {{ item.itemName }}</td>
            <td>LSL: {{ item.lsl ?? "-" }} / USL: {{ item.usl ?? "-" }}</td>
            <td>
              <input
                v-model.number="measurementInputs[item.id]"
                type="number"
                step="any"
                :class="[
                  'border rounded px-2 py-1 w-36',
                  isOutOfSpec(item) ? 'border-red-500 bg-red-50 text-red-700' : ''
                ]"
              />
              <span v-if="isOutOfSpec(item)" class="ml-2 text-xs text-red-600">超規</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <input v-model="batchNo" class="border rounded px-2 py-1 w-48" placeholder="BatchNo" />
    <button type="button" class="bg-green-600 text-white px-3 py-1 rounded ml-2" @click="submit">送出量測</button>
    <div v-if="submitError" class="text-sm text-red-600">{{ submitError }}</div>
  </div>
  <div v-if="result" class="mt-3 text-sm">BatchId: {{ result.batchId }}，Alerts: {{ result.alertsCount }}</div>
</template>
