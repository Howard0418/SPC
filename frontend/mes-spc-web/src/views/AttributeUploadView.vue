<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";

const router = useRouter();
const input = ref('[{"PartNo":"","ProcessCode":"","MachineCode":"","CharacteristicCode":"","LotNo":"","SampleNo":"1","InspectedQty":"0","DefectQty":"0","DefectCount":"0","UnitCount":"0","MeasuredAt":"","Operator":""}]');
const err = ref("");

async function submit() {
  err.value = "";
  try {
    const payload = JSON.parse(input.value);
    const { data } = await api.post("/uploads/attribute", payload);
    await router.push(`/uploads/${data.uploadBatchId}/preview`);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}
</script>

<template>
  <section class="p-4 space-y-3">
    <h1 class="text-xl font-bold">計數資料上傳</h1>
    <textarea v-model="input" class="w-full h-52 border rounded p-2 text-xs font-mono"></textarea>
    <button class="px-3 py-2 rounded bg-blue-600 text-white" @click="submit">上傳並預覽</button>
    <p v-if="err" class="text-red-600">{{ err }}</p>
  </section>
</template>
