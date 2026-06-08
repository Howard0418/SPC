<script setup>
import { ref, onMounted, nextTick } from "vue";
import * as echarts from "echarts";
import { api } from "../api/client.js";
import { Search, Info, MapPin, AlertTriangle, Layers, X, FolderTree } from "lucide-vue-next";

const searchType = ref("lot"); // 'lot' or 'workorder'
const searchQuery = ref("");
const isLoading = ref(false);
const errorMsg = ref("");

const chartRef = ref(null);
let myChart = null;

// Details Panel
const showPanel = ref(false);
const activeNode = ref(null); // The currently clicked lot node
const activeDetails = ref({ routingHistory: [], qualityEvents: [] });

// Transform Genealogy Response to ECharts Tree format
function buildTreeDataFromLotResponse(res) {
  const buildNode = (lot, isTarget = false) => ({
    name: lot.lotNo,
    value: lot.subLotNo || "N/A",
    itemStyle: {
      color: isTarget ? "#3b82f6" : "#64748b",
      borderColor: isTarget ? "#1d4ed8" : "#475569"
    },
    label: { color: isTarget ? "#fff" : "#333", fontWeight: isTarget ? "bold" : "normal" },
    meta: { ...lot, routingHistory: res.routingHistory, qualityEvents: res.qualityEvents } // Attach details only for the target
  });

  const current = buildNode(res.currentLot, true);
  if (res.childLots && res.childLots.length > 0) {
    current.children = res.childLots.map(c => buildNode(c));
  }

  if (res.parentLot) {
    const root = buildNode(res.parentLot);
    root.children = [current];
    return root;
  }
  return current;
}

function buildTreeDataFromWorkOrder(res) {
  return {
    name: res.workOrderNo,
    itemStyle: { color: "#8b5cf6" },
    children: res.rootLots.map(lot => ({
      name: lot.lotNo,
      value: lot.subLotNo,
      itemStyle: { color: "#64748b" },
      meta: lot
    }))
  };
}

async function performSearch(queryStr = null) {
  const query = queryStr || searchQuery.value.trim();
  if (!query) return;

  isLoading.value = true;
  errorMsg.value = "";
  showPanel.value = false;
  activeNode.value = null;

  try {
    let treeData;
    if (searchType.value === "lot") {
      const { data } = await api.get(`/genealogy/lot/${query}`);
      treeData = buildTreeDataFromLotResponse(data);
      // Automatically show panel for the searched lot
      activeNode.value = data.currentLot;
      activeDetails.value = {
        routingHistory: data.routingHistory || [],
        qualityEvents: data.qualityEvents || []
      };
      showPanel.value = true;
    } else {
      const { data } = await api.get(`/genealogy/workorder/${query}`);
      treeData = buildTreeDataFromWorkOrder(data);
    }
    renderChart(treeData);
  } catch (err) {
    console.error(err);
    errorMsg.value = err.response?.data || "查無資料或發生錯誤";
    if (myChart) myChart.clear();
  } finally {
    isLoading.value = false;
  }
}

function renderChart(treeData) {
  if (!myChart && chartRef.value) {
    myChart = echarts.init(chartRef.value);
    
    myChart.on('click', async (params) => {
      if (params.data && params.data.meta && params.data.meta.lotNo) {
        // If clicking on a Lot node, fetch its detailed genealogy and re-center the tree
        searchType.value = "lot";
        searchQuery.value = params.data.meta.lotNo;
        await performSearch();
      }
    });
  }

  const option = {
    tooltip: {
      trigger: "item",
      triggerOn: "mousemove",
      formatter: (info) => {
        const { name, value, data } = info;
        if (!data.meta) return `<b>${name}</b>`;
        return `
          <div style="font-weight:bold">${name}</div>
          <div>SubLot: ${value || '-'}</div>
          <div>Status: ${data.meta.status || '-'}</div>
          <div>Qty: ${data.meta.currentQty || '-'}</div>
        `;
      }
    },
    series: [
      {
        type: "tree",
        data: [treeData],
        top: "10%",
        left: "15%",
        bottom: "10%",
        right: "15%",
        symbolSize: 45,
        edgeShape: "polyline",
        initialTreeDepth: 3,
        label: {
          position: "top",
          verticalAlign: "middle",
          align: "center",
          fontSize: 13,
          distance: 10,
          backgroundColor: "rgba(255,255,255,0.8)",
          padding: 4,
          borderRadius: 4
        },
        leaves: {
          label: {
            position: "bottom",
            verticalAlign: "middle",
            align: "center"
          }
        },
        itemStyle: {
          shadowColor: 'rgba(0, 0, 0, 0.2)',
          shadowBlur: 10
        },
        lineStyle: {
          color: '#cbd5e1',
          width: 2,
          curveness: 0.5
        },
        expandAndCollapse: true,
        animationDuration: 550,
        animationDurationUpdate: 750
      }
    ]
  };

  myChart.setOption(option);
}

onMounted(() => {
  window.addEventListener('resize', () => {
    if (myChart) myChart.resize();
  });
});
</script>

<template>
  <div class="h-[calc(100vh-120px)] flex flex-col bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 relative overflow-hidden">
    
    <!-- Top Search Bar -->
    <div class="flex items-center gap-4 p-5 border-b border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-900/50">
      <div class="flex items-center gap-2 bg-white dark:bg-slate-800 rounded-lg p-1 shadow-sm border border-slate-200 dark:border-slate-700">
        <button 
          @click="searchType = 'lot'" 
          :class="['px-4 py-1.5 rounded-md text-sm font-semibold transition-all', searchType === 'lot' ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' : 'text-slate-500 hover:text-slate-700']"
        >批號查詢</button>
        <button 
          @click="searchType = 'workorder'" 
          :class="['px-4 py-1.5 rounded-md text-sm font-semibold transition-all', searchType === 'workorder' ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400' : 'text-slate-500 hover:text-slate-700']"
        >工單查詢</button>
      </div>
      
      <div class="flex-1 max-w-md relative">
        <Search class="w-5 h-5 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
        <input 
          v-model="searchQuery" 
          @keyup.enter="performSearch()"
          type="text" 
          :placeholder="searchType === 'lot' ? '輸入 LotNo...' : '輸入 WorkOrderNo...'" 
          class="w-full pl-10 pr-4 py-2 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none text-slate-800 dark:text-slate-100"
        />
      </div>
      <button @click="performSearch()" class="px-5 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg shadow-md shadow-blue-500/20 transition-all flex items-center gap-2">
        查詢樹狀圖
      </button>

      <div v-if="isLoading" class="flex items-center gap-2 text-sm text-blue-600 font-semibold ml-4">
        <svg class="animate-spin h-5 w-5 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        載入中...
      </div>
      <div v-if="errorMsg" class="text-sm text-red-500 font-semibold ml-4 bg-red-50 px-3 py-1 rounded-md">{{ errorMsg }}</div>
    </div>

    <!-- Guide / Operation Tip -->
    <div class="px-5 py-3 border-b border-blue-100 dark:border-blue-900/50 bg-blue-50/80 dark:bg-blue-950/20 text-xs text-blue-700 dark:text-blue-300">
      <div class="flex items-start gap-3">
        <div class="p-1.5 bg-blue-100 dark:bg-blue-900/50 rounded-lg text-blue-600 dark:text-blue-400 mt-0.5">
          <Info class="w-4 h-4" />
        </div>
        <div class="space-y-1.5 leading-relaxed">
          <h4 class="font-black text-blue-900 dark:text-blue-200">模組指南：產品系譜圖 (Genealogy)</h4>
          <p>此頁面用於依批號或工單查詢上下游生產關係，並以樹狀圖呈現父批、目標批與子批的追溯脈絡。</p>
          <div class="space-y-1">
            <div class="font-black text-blue-900 dark:text-blue-200">產品系譜圖頁面操作說明</div>
            <p><strong>選擇查詢方式：</strong>可切換「批號查詢」或「工單查詢」。</p>
            <p><strong>執行查詢：</strong>輸入 LotNo 或 WorkOrderNo 後按「查詢樹狀圖」。</p>
            <p><strong>查看節點：</strong>點選圖上的批號節點，右側會顯示生產軌跡與品質事件。</p>
            <p><strong>追溯上下游：</strong>點選不同節點可重新置中查看父批、子批與相關批次關係。</p>
          </div>
        </div>
      </div>
    </div>

    <!-- Canvas Area -->
    <div class="flex-1 relative bg-slate-50/50 dark:bg-slate-900/20">
      <div ref="chartRef" class="w-full h-full"></div>

      <!-- Placeholder -->
      <div v-if="!activeNode && !isLoading && !myChart" class="absolute inset-0 flex flex-col items-center justify-center text-slate-400">
        <FolderTree class="w-20 h-20 mb-4 opacity-20" />
        <p class="text-lg font-medium">請輸入批號或工單號以繪製系譜圖</p>
      </div>
    </div>

    <!-- Side Panel (Drawer) for Traceability Details -->
    <div 
      class="absolute top-0 right-0 h-full w-96 bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 shadow-2xl transition-transform duration-300 transform flex flex-col"
      :class="showPanel ? 'translate-x-0' : 'translate-x-full'"
    >
      <div class="p-4 border-b border-slate-200 dark:border-slate-800 flex items-center justify-between bg-slate-50 dark:bg-slate-900/50">
        <div>
          <h3 class="font-bold text-lg text-slate-800 dark:text-slate-100 flex items-center gap-2">
            <Info class="w-5 h-5 text-blue-500" />
            批號歷程詳情
          </h3>
          <p v-if="activeNode" class="text-xs text-slate-500 mt-1 font-mono">{{ activeNode.lotNo }}</p>
        </div>
        <button @click="showPanel = false" class="p-1.5 hover:bg-slate-200 dark:hover:bg-slate-800 rounded-lg transition-colors">
          <X class="w-5 h-5 text-slate-500" />
        </button>
      </div>

      <div class="flex-1 overflow-y-auto p-4 space-y-6">
        
        <!-- Physical Routing -->
        <section>
          <h4 class="text-sm font-bold text-slate-800 dark:text-slate-200 mb-3 flex items-center gap-1.5">
            <MapPin class="w-4 h-4 text-emerald-500" /> 物理流轉軌跡
          </h4>
          <div v-if="activeDetails.routingHistory.length === 0" class="text-xs text-slate-400 italic">無軌跡紀錄</div>
          <div v-else class="relative border-l-2 border-emerald-200 dark:border-emerald-900/50 ml-2 space-y-4">
            <div v-for="(route, i) in activeDetails.routingHistory" :key="i" class="relative pl-4">
              <div class="absolute -left-[5px] top-1.5 w-2 h-2 rounded-full bg-emerald-500 ring-4 ring-white dark:ring-slate-900"></div>
              <div class="bg-slate-50 dark:bg-slate-800/50 rounded-lg p-2.5 border border-slate-100 dark:border-slate-800">
                <div class="text-xs font-bold text-slate-700 dark:text-slate-300 flex justify-between">
                  <span>{{ route.lineCode }} ➔ {{ route.tankCode }}</span>
                  <span class="text-emerald-600 dark:text-emerald-400">槽位: {{ route.slotCode }}</span>
                </div>
                <div class="text-[10px] text-slate-500 mt-1 flex justify-between">
                  <span>In: {{ new Date(route.entryTime).toLocaleString() }}</span>
                  <span v-if="route.exitTime">Out: {{ new Date(route.exitTime).toLocaleString() }}</span>
                </div>
                <div class="text-[10px] text-slate-400 mt-1">OP: {{ route.operator || 'System' }}</div>
              </div>
            </div>
          </div>
        </section>

        <!-- Quality Events -->
        <section>
          <h4 class="text-sm font-bold text-slate-800 dark:text-slate-200 mb-3 flex items-center gap-1.5">
            <AlertTriangle class="w-4 h-4 text-amber-500" /> 品質與檢驗事件
          </h4>
          <div v-if="activeDetails.qualityEvents.length === 0" class="text-xs text-slate-400 italic">無檢驗紀錄</div>
          <div v-else class="space-y-2.5">
            <div v-for="(evt, i) in activeDetails.qualityEvents" :key="i" class="bg-amber-50/50 dark:bg-amber-900/10 rounded-lg p-2.5 border border-amber-100 dark:border-amber-900/30 text-sm">
              <div class="flex justify-between items-start mb-1">
                <span class="font-bold text-amber-700 dark:text-amber-500 text-xs">{{ evt.type }}</span>
                <span class="text-[10px] text-slate-400">{{ new Date(evt.eventTime).toLocaleString() }}</span>
              </div>
              <div class="text-xs text-slate-600 dark:text-slate-300">
                數值: <span class="font-mono font-bold">{{ evt.measuredValue ?? evt.defectQty ?? evt.defectCount ?? 'N/A' }}</span>
              </div>
              <div v-if="evt.chemicalName" class="text-[10px] text-slate-500 mt-1">
                藥水: {{ evt.chemicalName }}
              </div>
            </div>
          </div>
        </section>

      </div>
    </div>
    
  </div>
</template>
