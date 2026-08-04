<template>
  <div v-if="data && !loading" ref="summaryContainer" data-testid="spc-summary-table"
    class="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-md overflow-hidden">
    <div
      class="px-4 py-2.5 border-b border-slate-200 dark:border-slate-800 flex flex-wrap items-center justify-between gap-2">
      <div>
        <h3 class="font-black text-slate-800 dark:text-white flex items-center gap-2">
          <List class="w-5 h-5 text-blue-500" /> SPC 管制項目總覽
        </h3>
        <p class="text-[11px] text-slate-400 mt-0.5">共 {{ data.length }} 筆量測資料受管制</p>
      </div>
      <span
        class="px-3 py-1 rounded-full bg-blue-50 dark:bg-blue-950/50 text-blue-600 dark:text-blue-300 text-xs font-bold border border-blue-200 dark:border-blue-800">
        線別：全部 (All)
      </span>
    </div>

    <div v-if="data.length === 0" class="py-10 text-center text-slate-400">
      <BarChart3 class="w-8 h-8 mx-auto mb-2 opacity-50" />
      <p class="font-bold">此條件下沒有可計算之量測點</p>
    </div>

    <div v-if="data.length > 0" ref="summaryScroll" class="spc-summary-scroll overflow-x-scroll overflow-y-hidden pb-3"
      @scroll="syncSummaryScroll('body')">
      <table class="min-w-[1960px] w-full text-left text-xs border-collapse">
        <thead>
          <tr
            class="bg-slate-50 dark:bg-slate-800/70 text-slate-500 dark:text-slate-400 border-b border-slate-200 dark:border-slate-700 whitespace-nowrap">
            <th
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('controlCategory')">
              <div class="flex items-center gap-1">
                <span>管制類別</span>
                <span class="text-slate-400 dark:text-slate-500">
                  <ArrowUp v-if="sortKey === 'controlCategory' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'controlCategory' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('lineOrProcessName')">
              <div class="flex items-center gap-1">
                <span>製程線別</span>
                <ArrowUp v-if="sortKey === 'lineOrProcessName' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                <ArrowDown v-else-if="sortKey === 'lineOrProcessName' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
              </div>
            </th>
            <th
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('slotName')">
              <div class="flex items-center gap-1">
                <span>槽位</span>
                <ArrowUp v-if="sortKey === 'slotName' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                <ArrowDown v-else-if="sortKey === 'slotName' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
              </div>
            </th>
            <th
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('chartName')">
              <div class="flex items-center gap-1">
                <span>管制圖名稱</span>
                <ArrowUp v-if="sortKey === 'chartName' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                <ArrowDown v-else-if="sortKey === 'chartName' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('totalCount')">
              <div class="flex items-center justify-end gap-1">
                <ArrowUp v-if="sortKey === 'totalCount' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                <ArrowDown v-else-if="sortKey === 'totalCount' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                <span>匯入資料數</span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('usl')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'usl' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'usl' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>USL</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('lsl')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'lsl' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'lsl' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>LSL</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('ucl')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'ucl' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'ucl' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>UCL</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('lcl')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'lcl' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'lcl' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>LCL</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('oosCount')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'oosCount' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'oosCount' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>本期OOS</span>
                </span>
              </div>
            </th>
            <th v-if="comparisonPeriod" class="px-4 py-3 text-right cursor-pointer select-none" @click="handleSort('previousMonthOosCount')">
              <span class="flex items-center justify-end gap-1">
                <ArrowUpDown class="w-3.5 h-3.5 opacity-60" />
                <span>{{ comparisonPrefix }}OOS</span>
              </span>
            </th>
            <th v-if="comparisonPeriod === 'MONTH'" class="px-4 py-3 text-right whitespace-nowrap">OOS 差異</th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('oosPercentage')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'oosPercentage' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'oosPercentage' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>本期%OOS</span>
                </span>
              </div>
            </th>
            <th v-if="comparisonPeriod" class="px-4 py-3 text-right cursor-pointer select-none" @click="handleSort('previousMonthOosPercentage')">
              <span class="flex items-center justify-end gap-1">
                <ArrowUpDown class="w-3.5 h-3.5 opacity-60" />
                <span>{{ comparisonPrefix }}%OOS</span>
              </span>
            </th>
            <th v-if="comparisonPeriod === 'MONTH'" class="px-4 py-3 text-right whitespace-nowrap">%OOS 差異</th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('ca')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'ca' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'ca' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>Ca</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('pp')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'pp' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'pp' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>Cp</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('ppk')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'ppk' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'ppk' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>本期Cpk</span>
                </span>
              </div>
            </th>
            <th v-if="comparisonPeriod" class="px-4 py-3 text-right cursor-pointer select-none" @click="handleSort('previousMonthPpk')">
              <span class="flex items-center justify-end gap-1">
                <ArrowUpDown class="w-3.5 h-3.5 opacity-60" />
                <span>{{ comparisonPrefix }}Cpk</span>
              </span>
            </th>
            <th v-if="comparisonPeriod === 'MONTH'" class="px-4 py-3 text-right whitespace-nowrap">Cpk 差異</th>
            <th class="px-4 py-3 text-center sticky right-0 bg-slate-50 dark:bg-slate-800">製圖</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-700 dark:text-slate-200">
          <tr v-for="row in sortedData" :key="row.partProcessCharacteristicId"
            class="hover:bg-blue-50/50 dark:hover:bg-blue-950/20 transition-colors">
            <td class="px-4 py-3 font-bold whitespace-nowrap">{{ row.controlCategory || 'N/A' }}</td>
            <td class="px-4 py-3 min-w-44" :title="row.lineOrProcessName || ''">{{ formatLineName(row.lineOrProcessName) }}</td>
            <td class="px-4 py-3 min-w-36" :title="row.slotName || ''">{{ formatSlotName(row.slotName) }}</td>
            <td class="px-4 py-3 min-w-44 font-bold text-blue-700 dark:text-blue-300">{{ row.chartName || 'N/A' }}</td>
            <td class="px-4 py-3 text-right font-mono font-bold">{{ row.totalCount ?? 0 }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.usl) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.lsl) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.ucl) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.lcl) }}</td>
            <td class="px-4 py-3 text-right font-bold"
              :class="row.oosCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-emerald-600 dark:text-emerald-400'">
              {{ row.oosCount }}
            </td>
            <td v-if="comparisonPeriod" class="px-4 py-3 text-right font-bold"
              :class="row.previousMonthOosCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-slate-500 dark:text-slate-400'">
              {{ row.previousMonthOosCount ?? 0 }}
            </td>
            <td v-if="comparisonPeriod === 'MONTH'" class="px-4 py-3 text-right font-bold"
              :class="differenceClass(row.oosCount, row.previousMonthOosCount, false)">
              {{ formatDifference(row.oosCount, row.previousMonthOosCount, 0) }}
            </td>
            <td class="px-4 py-3 text-right font-bold"
              :class="row.oosPercentage > 0 ? 'text-red-600 dark:text-red-400' : ''">
              {{ formatPercentage(row.oosPercentage) }}
            </td>
            <td v-if="comparisonPeriod" class="px-4 py-3 text-right font-bold"
              :class="row.previousMonthOosPercentage > 0 ? 'text-red-600 dark:text-red-400' : 'text-slate-500 dark:text-slate-400'">
              {{ formatPercentage(row.previousMonthOosPercentage) }}
            </td>
            <td v-if="comparisonPeriod === 'MONTH'" class="px-4 py-3 text-right font-bold"
              :class="differenceClass(row.oosPercentage, row.previousMonthOosPercentage, false)">
              {{ formatDifference(row.oosPercentage, row.previousMonthOosPercentage, 2) }}%
            </td>
            <td class="px-4 py-3 text-right font-mono">{{ formatNumber(row.ca) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatNumber(row.pp) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatNumber(row.ppk) }}</td>
            <td v-if="comparisonPeriod" class="px-4 py-3 text-right font-mono">{{ formatNumber(row.previousMonthPpk) }}</td>
            <td v-if="comparisonPeriod === 'MONTH'" class="px-4 py-3 text-right font-mono font-bold"
              :class="differenceClass(row.ppk, row.previousMonthPpk, true)">
              {{ formatDifference(row.ppk, row.previousMonthPpk, 2) }}
            </td>
            <td class="px-4 py-3 text-center sticky right-0 bg-white dark:bg-slate-900">
              <button type="button" :data-testid="`draw-chart-${row.partProcessCharacteristicId}`"
                @click="$emit('draw-chart', row)"
                class="inline-flex items-center gap-1.5 px-3 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold shadow-sm whitespace-nowrap">
                <TrendingUp v-if="isTrendRow(row)" class="w-4 h-4" />
                <LineChart v-else class="w-4 h-4" />
                製圖
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="showFloatingScroll" ref="floatingSummaryScroll"
      class="spc-summary-scroll spc-summary-scroll-floating fixed overflow-x-scroll overflow-y-hidden"
      :style="floatingScrollStyle"
      @scroll="syncSummaryScroll('floating')">
      <div class="h-1" :style="{ width: `${floatingContentWidth}px` }"></div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, nextTick, onBeforeUnmount, onMounted, watch } from "vue";
import { List, BarChart3, LineChart, TrendingUp, ArrowUpDown, ArrowUp, ArrowDown } from "lucide-vue-next"; // 新增三個排序圖示

// 1. 新增排序狀態
const sortKey = ref("");
const sortOrder = ref("asc"); // 'asc' 或 'desc'

// 2. 切換排序欄位與方向
function handleSort(key) {
  if (sortKey.value === key) {
    sortOrder.value = sortOrder.value === "asc" ? "desc" : "asc";
  } else {
    sortKey.value = key;
    sortOrder.value = "asc";
  }
}

// 3. 建立排序後資料的計算屬性
const sortedData = computed(() => {
  if (!sortKey.value) return props.data;
  return [...props.data].sort((a, b) => {
    let valA = a[sortKey.value];
    let valB = b[sortKey.value];

    // 特殊欄位對應
    if (sortKey.value === "chartKind") {
      valA = a.chartKind || (isTrendRow(a) ? "趨勢圖" : "管制圖");
      valB = b.chartKind || (isTrendRow(b) ? "趨勢圖" : "管制圖");
    }

    if (valA == null && valB == null) return 0;
    if (valA == null) return 1;  // 空值置後
    if (valB == null) return -1;

    let comparison = 0;
    if (typeof valA === "number" && typeof valB === "number") {
      comparison = valA - valB;
    } else {
      comparison = String(valA).localeCompare(String(valB), "zh-Hant-TW", { numeric: true });
    }
    return sortOrder.value === "asc" ? comparison : -comparison;
  });
});

const props = defineProps({
  data: {
    type: Array,
    required: true
  },
  loading: {
    type: Boolean,
    default: false
  },
  isSpc: {
    type: Boolean,
    default: true
  },
  comparisonPeriod: {
    type: String,
    default: ""
  }
});

const comparisonPrefix = computed(() => props.comparisonPeriod === "WEEK" ? "上週" : "上月");

defineEmits(["draw-chart"]);

const summaryContainer = ref(null);
const summaryScroll = ref(null);
const floatingSummaryScroll = ref(null);
const showFloatingScroll = ref(false);
const floatingContentWidth = ref(0);
const floatingScrollStyle = ref({});
let syncingSummaryScroll = false;
let summaryResizeObserver = null;

function syncSummaryScroll(source) {
  if (syncingSummaryScroll) return;
  const body = summaryScroll.value;
  const floating = floatingSummaryScroll.value;
  if (!body) return;

  syncingSummaryScroll = true;
  if (source === "floating" && floating) {
    body.scrollLeft = floating.scrollLeft;
  } else {
    if (floating) floating.scrollLeft = body.scrollLeft;
  }
  window.requestAnimationFrame(() => {
    syncingSummaryScroll = false;
  });
}

function updateFloatingScroll() {
  const container = summaryContainer.value;
  const body = summaryScroll.value;
  if (!container || !body || props.data.length === 0) {
    showFloatingScroll.value = false;
    return;
  }

  const rect = container.getBoundingClientRect();
  const hasHorizontalOverflow = body.scrollWidth > body.clientWidth + 1;
  showFloatingScroll.value = hasHorizontalOverflow
    && rect.top < window.innerHeight
    && rect.bottom > window.innerHeight;
  floatingContentWidth.value = body.scrollWidth;
  floatingScrollStyle.value = {
    left: `${Math.max(0, rect.left)}px`,
    width: `${Math.min(rect.width, window.innerWidth - Math.max(0, rect.left))}px`
  };

  nextTick(() => {
    if (floatingSummaryScroll.value) {
      floatingSummaryScroll.value.scrollLeft = body.scrollLeft;
    }
  });
}

onMounted(() => {
  window.addEventListener("scroll", updateFloatingScroll, { passive: true });
  window.addEventListener("resize", updateFloatingScroll);
  if (typeof ResizeObserver !== "undefined") {
    summaryResizeObserver = new ResizeObserver(updateFloatingScroll);
    if (summaryContainer.value) summaryResizeObserver.observe(summaryContainer.value);
  }
  nextTick(updateFloatingScroll);
});

onBeforeUnmount(() => {
  window.removeEventListener("scroll", updateFloatingScroll);
  window.removeEventListener("resize", updateFloatingScroll);
  summaryResizeObserver?.disconnect();
});

watch(() => props.data, () => nextTick(updateFloatingScroll), { deep: true });

function formatNumber(val) {
  if (val == null) return "-";
  return typeof val === "number" ? val.toFixed(2) : val;
}
function formatLimit(val) {
  if (val == null) return "-";
  return typeof val === "number" ? val.toFixed(3) : val;
}
function formatPercentage(val) {
  if (val == null) return "-";
  return typeof val === "number" ? val.toFixed(2) + "%" : val;
}
function formatSlotName(val) {
  if (!val) return "N/A";
  return String(val).split(/[（(]/, 1)[0].trim() || "N/A";
}
function formatLineName(val) {
  if (!val) return "N/A";
  return String(val).split(/\s*[／/]\s*/, 1)[0].trim() || "N/A";
}
function formatDifference(current, previous, digits) {
  if (current == null || previous == null) return "-";
  const difference = Number(current) - Number(previous);
  return `${difference > 0 ? "+" : ""}${difference.toFixed(digits)}`;
}
function differenceClass(current, previous, higherIsBetter) {
  if (current == null || previous == null || Number(current) === Number(previous)) return "text-slate-500 dark:text-slate-400";
  const improved = higherIsBetter
    ? Number(current) > Number(previous)
    : Number(current) < Number(previous);
  return improved ? "text-emerald-600 dark:text-emerald-400" : "text-red-600 dark:text-red-400";
}

function isTrendRow(row) {
  return row?.groupType === "TREND_CHART" || row?.chartKind === "趨勢圖" || !props.isSpc;
}
</script>

<style scoped>
.spc-summary-scroll {
  scrollbar-gutter: stable;
}

.spc-summary-scroll-floating {
  z-index: 60;
  bottom: 0;
  height: 20px;
  background: rgb(241 245 249);
  border: 1px solid rgb(203 213 225);
  border-bottom: 0;
  border-radius: 10px 10px 0 0;
  box-shadow: 0 -4px 12px rgb(15 23 42 / 0.16);
}

.spc-summary-scroll::-webkit-scrollbar {
  height: 14px;
}

.spc-summary-scroll::-webkit-scrollbar-track {
  background: rgb(241 245 249);
  border-top: 1px solid rgb(226 232 240);
}

.spc-summary-scroll::-webkit-scrollbar-thumb {
  background: rgb(59 130 246);
  border: 3px solid rgb(241 245 249);
  border-radius: 999px;
}

.spc-summary-scroll::-webkit-scrollbar-thumb:hover {
  background: rgb(37 99 235);
}

:global(.dark) .spc-summary-scroll-floating {
  background: rgb(15 23 42);
  border-color: rgb(51 65 85);
}

:global(.dark) .spc-summary-scroll::-webkit-scrollbar-track {
  background: rgb(15 23 42);
  border-top-color: rgb(51 65 85);
}

:global(.dark) .spc-summary-scroll::-webkit-scrollbar-thumb {
  background: rgb(59 130 246);
  border-color: rgb(15 23 42);
}
</style>
