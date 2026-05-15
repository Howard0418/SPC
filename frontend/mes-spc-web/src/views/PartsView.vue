<script setup>
import { onMounted, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";

const rows = ref([]);
const err = ref("");

async function load() {
  err.value = "";
  try {
    const { data } = await api.get("/parts");
    rows.value = data;
  } catch (e) {
    err.value = getApiErrorMessage(e);
  }
}

onMounted(load);
</script>

<template>
  <section class="p-4">
    <h1 class="text-xl font-bold mb-3">料號維護</h1>
    <p v-if="err" class="text-red-600 mb-2">{{ err }}</p>
    <div class="rounded border p-3 bg-white">
      <p class="mb-2 text-sm text-gray-600">目前先提供清單骨架，後續補完整 CRUD 編輯表單。</p>
      <pre class="text-xs overflow-auto">{{ rows }}</pre>
    </div>
  </section>
</template>
