<script setup>
import { ref, watch } from "vue";
const props = defineProps({ loading: Boolean, hasMore: Boolean, resetKey: [Number, String] });
const emit = defineEmits(['change']);
const from = ref(''); const to = ref(''); const page = ref(0); const error = ref('');
let applied = { fromDate: '', toDate: '' };
function send() { emit('change', { ...applied, skip: page.value * 20, take: 21 }); }
function search() {
  error.value = '';
  if (from.value && to.value && from.value > to.value) { error.value = '起日不可晚於迄日。'; return; }
  applied = { fromDate: from.value, toDate: to.value }; page.value = 0; send();
}
function clear() { from.value = ''; to.value = ''; search(); }
function move(delta) { page.value += delta; send(); }
watch(() => props.resetKey, () => { from.value = ''; to.value = ''; page.value = 0; error.value = ''; applied = { fromDate: '', toDate: '' }; });
</script>
<template>
  <div class="my-3 space-y-2">
    <div role="group" aria-label="修改日期篩選" @keydown.enter.prevent="search" class="flex flex-wrap items-end gap-2 text-sm">
      <label>修改起日<input v-model="from" :disabled="loading" aria-label="修改起日" type="date" class="mt-1 block rounded border bg-transparent p-2 dark:border-slate-700"></label>
      <label>修改迄日<input v-model="to" :disabled="loading" aria-label="修改迄日" type="date" class="mt-1 block rounded border bg-transparent p-2 dark:border-slate-700"></label>
      <button type="button" @click="search" :disabled="loading" class="rounded border px-3 py-2">查詢</button>
      <button type="button" :disabled="loading" @click="clear" class="rounded border px-3 py-2">清除</button>
    </div>
    <p class="text-xs text-slate-500">依修改日期查詢（台北時間，含起迄日全天）；可只填一邊。生效日期不受影響。</p>
    <p v-if="error" role="alert" class="text-sm text-red-600">{{ error }}</p>
    <div class="flex items-center gap-2 text-sm"><button type="button" @click="move(-1)" :disabled="loading || page === 0" class="rounded border px-2 py-1">歷史上一頁</button><span>第 {{ page + 1 }} 頁</span><button type="button" @click="move(1)" :disabled="loading || !hasMore" class="rounded border px-2 py-1">歷史下一頁</button></div>
  </div>
</template>
