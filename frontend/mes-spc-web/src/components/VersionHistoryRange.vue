<script setup>
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
const { t } = useI18n({ useScope: "global" });
const messageText = value => displayUiMessage(t, value);
import { ref, watch } from "vue";
const props = defineProps({ loading: Boolean, hasMore: Boolean, resetKey: [Number, String] });
const emit = defineEmits(['change']);
const from = ref(''); const to = ref(''); const page = ref(0); const error = ref('');
let applied = { fromDate: '', toDate: '' };
function send() { emit('change', { ...applied, skip: page.value * 20, take: 21 }); }
function search() {
  error.value = '';
  if (from.value && to.value && from.value > to.value) { error.value = { key: 'spc.versionRange.invalidRange' }; return; }
  applied = { fromDate: from.value, toDate: to.value }; page.value = 0; send();
}
function clear() { from.value = ''; to.value = ''; search(); }
function move(delta) { page.value += delta; send(); }
watch(() => props.resetKey, () => { from.value = ''; to.value = ''; page.value = 0; error.value = ''; applied = { fromDate: '', toDate: '' }; });
</script>
<template>
  <div class="my-3 space-y-2">
    <div role="group" :aria-label="t('spc.versionRange.group')" @keydown.enter.prevent="search" class="flex flex-wrap items-end gap-2 text-sm">
      <label>{{ t('spc.versionRange.from') }}<input data-testid="history-from" v-model="from" :disabled="loading" :aria-label="t('spc.versionRange.from')" type="date" class="mt-1 block rounded border bg-transparent p-2 dark:border-slate-700"></label>
      <label>{{ t('spc.versionRange.to') }}<input data-testid="history-to" v-model="to" :disabled="loading" :aria-label="t('spc.versionRange.to')" type="date" class="mt-1 block rounded border bg-transparent p-2 dark:border-slate-700"></label>
      <button type="button" data-testid="history-search" @click="search" :disabled="loading" class="rounded border px-3 py-2">{{ t('spc.versionRange.search') }}</button>
      <button type="button" :disabled="loading" data-testid="history-clear" @click="clear" class="rounded border px-3 py-2">{{ t('spc.versionRange.clear') }}</button>
    </div>
    <p class="text-xs text-slate-500">{{ t('spc.versionRange.help') }}</p>
    <p v-if="error" role="alert" class="text-sm text-red-600">{{ messageText(error) }}</p>
    <div class="flex flex-wrap items-center gap-2 text-sm"><button type="button" data-testid="history-previous" @click="move(-1)" :disabled="loading || page === 0" class="rounded border px-2 py-1">{{ t('spc.versionRange.previous') }}</button><span>{{ t('spc.versionRange.page', { page: page + 1 }) }}</span><button type="button" data-testid="history-next" @click="move(1)" :disabled="loading || !hasMore" class="rounded border px-2 py-1">{{ t('spc.versionRange.next') }}</button></div>
  </div>
</template>
