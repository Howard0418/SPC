<script setup>
import { ref } from 'vue';
import { Info, ChevronDown, ChevronUp } from 'lucide-vue-next';

const props = defineProps({
  title: {
    type: String,
    default: '模組指南'
  },
  defaultExpanded: {
    type: Boolean,
    default: false
  }
});

const isExpanded = ref(props.defaultExpanded);

function toggle() {
  isExpanded.value = !isExpanded.value;
}
</script>

<template>
  <div class="mb-6 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-900/20 border border-blue-100 dark:border-blue-800/50 rounded-2xl shadow-sm transition-all duration-300">
    <div class="flex items-center justify-between p-4 cursor-pointer select-none" @click="toggle">
      <div class="flex items-center gap-4">
        <div class="p-2 bg-blue-100 dark:bg-blue-900/50 rounded-xl text-blue-600 dark:text-blue-400 flex-shrink-0">
          <Info class="w-5 h-5" />
        </div>
        <h4 class="text-sm font-bold text-blue-900 dark:text-blue-300 m-0">{{ title }}</h4>
      </div>
      <button class="p-1.5 rounded-lg hover:bg-blue-200/50 dark:hover:bg-blue-800/50 text-blue-600 dark:text-blue-400 transition-colors focus:outline-none">
        <ChevronUp v-if="isExpanded" class="w-5 h-5" />
        <ChevronDown v-else class="w-5 h-5" />
      </button>
    </div>
    
    <div v-show="isExpanded" class="px-5 pb-5">
      <div class="text-xs text-blue-700 dark:text-blue-400/80 leading-relaxed border-t border-blue-200/50 dark:border-blue-800/50 pt-4">
        <slot></slot>
      </div>
    </div>
  </div>
</template>
