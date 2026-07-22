<template>
  <div v-if="data && !loading" data-testid="spc-summary-table"
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

    <div v-else ref="topSummaryScroll"
      class="spc-summary-scroll spc-summary-scroll-top overflow-x-scroll overflow-y-hidden"
      @scroll="syncSummaryScroll('top')">
      <div class="h-1 min-w-[2480px]"></div>
    </div>

    <div v-if="data.length > 0" ref="summaryScroll" class="spc-summary-scroll overflow-x-scroll overflow-y-hidden pb-3"
      @scroll="syncSummaryScroll('body')">
      <table class="min-w-[2480px] w-full text-left text-xs border-collapse">
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
              @click="handleSort('chartKind')">
              <div class="flex items-center gap-1">
                <span>圖表類型</span>
                <span class="text-slate-400 dark:text-slate-500">
                  <ArrowUp v-if="sortKey === 'chartKind' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'chartKind' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
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
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('chartType')">
              <div class="flex items-center gap-1">
                <span>管制圖種類</span>
                <ArrowUp v-if="sortKey === 'chartType' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                <ArrowDown v-else-if="sortKey === 'chartType' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
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
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('limitCalculationMethod')">
              <div class="flex items-center gap-1">
                <span>管制界線計算方式</span>
                <ArrowUp v-if="sortKey === 'limitCalculationMethod' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                <ArrowDown v-else-if="sortKey === 'limitCalculationMethod' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
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
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('previousMonthOosCount')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'previousMonthOosCount' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'previousMonthOosCount' && sortOrder === 'desc'"
                    class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>上月OOS</span>
                </span>
              </div>
            </th>
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
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('previousMonthOosPercentage')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'previousMonthOosPercentage' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'previousMonthOosPercentage' && sortOrder === 'desc'"
                    class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>上月%OOS</span>
                </span>
              </div>
            </th>
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
                  <span>Pp</span>
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
                  <span>本期Ppk</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 text-right cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('previousMonthPpk')">
              <div class="flex items-center justify-end gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'previousMonthPpk' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'previousMonthPpk' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>上月Ppk</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('responsibleUser')">
              <div class="flex items-center gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'responsibleUser' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'responsibleUser' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>工程負責人</span>
                </span>
              </div>
            </th>
            <th
              class="px-4 py-3 cursor-pointer select-none hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
              @click="handleSort('remarks')">
              <div class="flex items-center gap-1">
                <span class="flex items-center gap-1">
                  <ArrowUp v-if="sortKey === 'remarks' && sortOrder === 'asc'" class="w-3.5 h-3.5" />
                  <ArrowDown v-else-if="sortKey === 'remarks' && sortOrder === 'desc'" class="w-3.5 h-3.5" />
                  <ArrowUpDown v-else class="w-3.5 h-3.5 opacity-60" />
                  <span>備註</span>
                </span>
              </div>
            </th>
            <th class="px-4 py-3 text-center sticky right-0 bg-slate-50 dark:bg-slate-800">製圖</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-700 dark:text-slate-200">
          <tr v-for="row in sortedData" :key="row.partProcessCharacteristicId"
            class="hover:bg-blue-50/50 dark:hover:bg-blue-950/20 transition-colors">
            <td class="px-4 py-3 font-bold whitespace-nowrap">{{ row.controlCategory || 'N/A' }}</td>
            <td class="px-4 py-3 whitespace-nowrap">
              <span :class="[
                'inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-black border',
                isTrendRow(row)
                  ? 'bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-950/50 dark:text-indigo-300 dark:border-indigo-800'
                  : 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/50 dark:text-blue-300 dark:border-blue-800'
              ]">
                {{ row.chartKind || (isTrendRow(row) ? '趨勢圖' : '管制圖') }}
              </span>
            </td>
            <td class="px-4 py-3 min-w-44">{{ row.lineOrProcessName || 'N/A' }}</td>
            <td class="px-4 py-3 min-w-36">{{ row.slotName || 'N/A' }}</td>
            <td class="px-4 py-3 min-w-44 font-bold text-blue-700 dark:text-blue-300">{{ row.chartName || 'N/A' }}</td>
            <td class="px-4 py-3 whitespace-nowrap">{{ row.chartType || 'N/A' }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.usl) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.lsl) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.ucl) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatLimit(row.lcl) }}</td>
            <td class="px-4 py-3 min-w-40">{{ row.limitCalculationMethod || 'N/A' }}</td>
            <td class="px-4 py-3 text-right font-bold"
              :class="row.oosCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-emerald-600 dark:text-emerald-400'">
              {{ row.oosCount }}
            </td>
            <td class="px-4 py-3 text-right font-bold"
              :class="row.previousMonthOosCount > 0 ? 'text-red-600 dark:text-red-400' : 'text-slate-500 dark:text-slate-400'">
              {{ row.previousMonthOosCount ?? 0 }}
            </td>
            <td class="px-4 py-3 text-right font-bold"
              :class="row.oosPercentage > 0 ? 'text-red-600 dark:text-red-400' : ''">
              {{ formatPercentage(row.oosPercentage) }}
            </td>
            <td class="px-4 py-3 text-right font-bold"
              :class="row.previousMonthOosPercentage > 0 ? 'text-red-600 dark:text-red-400' : 'text-slate-500 dark:text-slate-400'">
              {{ formatPercentage(row.previousMonthOosPercentage) }}
            </td>
            <td class="px-4 py-3 text-right font-mono">{{ formatNumber(row.ca) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatNumber(row.pp) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatNumber(row.ppk) }}</td>
            <td class="px-4 py-3 text-right font-mono">{{ formatNumber(row.previousMonthPpk) }}</td>
            <td class="px-4 py-3 min-w-32">{{ row.responsibleUser || 'N/A' }}</td>
            <td class="px-4 py-3 min-w-56 max-w-xs truncate" :title="row.remarks || ''">{{ row.remarks || 'N/A' }}</td>
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
  </div>
</template>

<script setup>
import { ref, computed } from "vue"; // 新增 computed
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
  }
});

defineEmits(["draw-chart"]);

const topSummaryScroll = ref(null);
const summaryScroll = ref(null);
let syncingSummaryScroll = false;

function syncSummaryScroll(source) {
  if (syncingSummaryScroll) return;
  const top = topSummaryScroll.value;
  const body = summaryScroll.value;
  if (!top || !body) return;

  syncingSummaryScroll = true;
  if (source === "top") {
    body.scrollLeft = top.scrollLeft;
  } else {
    top.scrollLeft = body.scrollLeft;
  }
  requestAnimationFrame(() => {
    syncingSummaryScroll = false;
  });
}

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

function isTrendRow(row) {
  return row?.groupType === "TREND_CHART" || row?.chartKind === "趨勢圖" || !props.isSpc;
}
</script>

<style scoped>
.spc-summary-scroll {
  scrollbar-gutter: stable;
}

.spc-summary-scroll-top {
  border-bottom: 1px solid rgb(226 232 240);
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

:global(.dark) .spc-summary-scroll-top {
  border-bottom-color: rgb(51 65 85);
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
