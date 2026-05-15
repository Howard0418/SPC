<script setup>
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";

const route = useRoute();
const batch = ref(null);
const details = ref([]);
const errors = ref([]);
const err = ref("");
const loading = ref(false);
const batchId = computed(() => route.params.batchId);

async function load() {
  err.value = "";
  try {
    const { data } = await api.get(`/uploads/${batchId.value}/preview`);
    batch.value = data.batch;
    details.value = data.details;
    errors.value = data.errors;
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}

async function confirmImport() {
  loading.value = true;
  try {
    await api.post(`/uploads/${batchId.value}/confirm`);
    await load();
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>

<template>
  <section class="p-4 space-y-3">
    <h1 class="text-xl font-bold">上傳預覽與錯誤檢查</h1>
    <p v-if="err" class="text-red-600">{{ err }}</p>
    <pre class="text-xs overflow-auto border rounded p-2 bg-white">{{ batch }}</pre>
    <button class="px-3 py-2 rounded bg-green-600 text-white disabled:opacity-50" :disabled="loading" @click="confirmImport">
      確認匯入
    </button>
    <h2 class="font-semibold">錯誤清單</h2>
    <pre class="text-xs overflow-auto border rounded p-2 bg-white">{{ errors }}</pre>
    <h2 class="font-semibold">預覽資料</h2>
    <pre class="text-xs overflow-auto border rounded p-2 bg-white">{{ details }}</pre>
  </section>
</template>
