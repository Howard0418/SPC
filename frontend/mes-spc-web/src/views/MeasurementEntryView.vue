<script setup>
import { ref, computed, onMounted, watch } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Activity,
  CheckCircle2,
  AlertTriangle,
  User,
  Calendar,
  Hash,
  ArrowRight,
  RefreshCw,
  Info
} from "lucide-vue-next";

// Form State
const products = ref([]);
const stations = ref([]);
const inspectionItems = ref([]);
const mappings = ref([]);

const selectedProductId = ref("");
const selectedStationId = ref("");
const selectedItemId = ref("");

const payload = ref({
  batchNo: "",
  productId: null,
  stationId: null,
  workOrderId: null,
  lotNo: "",
  serialNo: "",
  measuredAt: "",
  operatorName: "",
  values: []
});

const loading = ref(false);
const submitting = ref(false);
const error = ref("");
const successResult = ref(null);

// Inputs refs for Enter tabbing
const inputRefs = ref([]);

// Set input element ref dynamically
const setInputRef = (el, index) => {
  if (el) {
    inputRefs.value[index] = el;
  }
};

onMounted(async () => {
  // Initialize date & auto batch no
  resetForm();
  await loadMetadata();
});

function resetForm() {
  const now = new Date();
  const dateStr = now.getFullYear().toString() + 
                  (now.getMonth() + 1).toString().padStart(2, "0") + 
                  now.getDate().toString().padStart(2, "0");
  const timeStr = now.getHours().toString().padStart(2, "0") + 
                  now.getMinutes().toString().padStart(2, "0") + 
                  now.getSeconds().toString().padStart(2, "0");
  
  payload.value = {
    batchNo: `MANUAL-${dateStr}-${timeStr}`,
    productId: null,
    stationId: null,
    workOrderId: null,
    lotNo: "",
    serialNo: "",
    measuredAt: new Date().toISOString().substring(0, 16), // datetime-local format
    operatorName: payload.value.operatorName || "", // retain operator name for ease
    values: []
  };
  
  selectedProductId.value = "";
  selectedStationId.value = "";
  selectedItemId.value = "";
  successResult.value = null;
  error.value = "";
}

async function loadMetadata() {
  loading.value = true;
  error.value = "";
  try {
    const [resProducts, resStations, resItems, resMappings] = await Promise.all([
      api.get("/products"),
      api.get("/stations"),
      api.get("/inspection-items"),
      api.get("/product-station-items")
    ]);

    products.value = resProducts.data || [];
    stations.value = resStations.data || [];
    inspectionItems.value = resItems.data || [];
    mappings.value = resMappings.data || [];
  } catch (e) {
    error.value = "載入主檔資料失敗：" + getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

// Cascading computed properties
const filteredStations = computed(() => {
  if (!selectedProductId.value) return [];
  const activeStationIds = mappings.value
    .filter(m => m.productId === Number(selectedProductId.value) && m.isActive)
    .map(m => m.stationId);
  return stations.value.filter(s => activeStationIds.includes(s.id) && s.isActive);
});

const filteredItems = computed(() => {
  if (!selectedProductId.value || !selectedStationId.value) return [];
  const activeItemIds = mappings.value
    .filter(m => m.productId === Number(selectedProductId.value) && 
                 m.stationId === Number(selectedStationId.value) && 
                 m.isActive)
    .map(m => m.inspectionItemId);
  return inspectionItems.value.filter(i => activeItemIds.includes(i.id) && i.isActive);
});

const selectedMapping = computed(() => {
  if (!selectedProductId.value || !selectedStationId.value || !selectedItemId.value) return null;
  return mappings.value.find(m => 
    m.productId === Number(selectedProductId.value) && 
    m.stationId === Number(selectedStationId.value) && 
    m.inspectionItemId === Number(selectedItemId.value)
  );
});

const selectedItemDetails = computed(() => {
  if (!selectedItemId.value) return null;
  return inspectionItems.value.find(i => i.id === Number(selectedItemId.value));
});

const sampleSize = computed(() => {
  return selectedMapping.value ? selectedMapping.value.sampleSize : 1;
});

// Watch cascading dropdowns and build payload values list
watch([selectedProductId, selectedStationId, selectedItemId], () => {
  payload.value.productId = selectedProductId.value ? Number(selectedProductId.value) : null;
  payload.value.stationId = selectedStationId.value ? Number(selectedStationId.value) : null;

  if (selectedItemId.value) {
    inputRefs.value = [];
    payload.value.values = Array.from({ length: sampleSize.value }, (_, i) => ({
      inspectionItemId: Number(selectedItemId.value),
      sampleNo: i + 1,
      valueNumeric: null
    }));
  } else {
    payload.value.values = [];
  }
});

// Real-time specs checker
function checkOutOfSpec(value) {
  if (value === null || value === undefined || value === "") return false;
  const numVal = Number(value);
  if (Number.isNaN(numVal)) return false;
  
  const item = selectedItemDetails.value;
  if (!item) return false;

  const oosUpper = item.usl !== null && item.usl !== undefined && numVal > item.usl;
  const oosLower = item.lsl !== null && item.lsl !== undefined && numVal < item.lsl;
  return oosUpper || oosLower;
}

// Enter-key navigation
function handleEnter(index) {
  if (index < payload.value.values.length - 1) {
    // Focus next input box
    const nextEl = inputRefs.value[index + 1];
    if (nextEl) {
      nextEl.focus();
      nextEl.select();
    }
  } else {
    // Last input box, submit form!
    submitBatch();
  }
}

const submitBatch = async () => {
  if (!selectedProductId.value || !selectedStationId.value || !selectedItemId.value) {
    error.value = "請先完整選擇產品、工站與檢驗項目。";
    return;
  }
  if (!payload.value.operatorName.trim()) {
    error.value = "請輸入作業人員名稱。";
    return;
  }
  
  // Validate that all fields are filled
  const hasEmptyVal = payload.value.values.some(v => v.valueNumeric === null || v.valueNumeric === undefined || v.valueNumeric === "");
  if (hasEmptyVal) {
    error.value = "請完整輸入所有樣本之量測值。";
    return;
  }

  submitting.value = true;
  error.value = "";
  successResult.value = null;

  try {
    // Convert inputs to numbers
    const finalPayload = JSON.parse(JSON.stringify(payload.value));
    finalPayload.measuredAt = new Date(finalPayload.measuredAt).toISOString();
    finalPayload.values.forEach(v => {
      v.valueNumeric = Number(v.valueNumeric);
    });

    const res = await api.post("/measurement-batches", finalPayload);
    successResult.value = res.data;
    
    // Quick success animation reset
    const operator = payload.value.operatorName;
    resetForm();
    payload.value.operatorName = operator; // Retain operator name
  } catch (e) {
    error.value = "儲存量測資料失敗：" + getApiErrorMessage(e);
  } finally {
    submitting.value = false;
  }
};
</script>

<template>
  <div class="max-w-3xl mx-auto space-y-6">
    <!-- Header -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center justify-between">
      <div>
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-blue-600 dark:text-blue-400 mb-1">
          <Activity class="w-4 h-4" /> 零失誤手動錄入工作區
        </div>
        <h1 class="text-2xl font-black text-slate-800 dark:text-white">現場量測數據錄入系統</h1>
      </div>
      <button @click="resetForm" class="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors" title="重設表單">
        <RefreshCw class="w-5 h-5" />
      </button>
    </div>

    <!-- Alert / Messages -->
    <div v-if="error" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <AlertTriangle class="w-5 h-5 flex-shrink-0" /> {{ error }}
    </div>

    <div v-if="successResult" class="p-5 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-700 dark:text-emerald-400 text-sm space-y-2">
      <div class="flex items-center gap-2 font-bold">
        <CheckCircle2 class="w-5 h-5" /> 量測數據上傳成功！
      </div>
      <p class="text-xs">系統已自動建立檢驗批號 <strong>{{ successResult.batchNo }}</strong>，共計錄入 {{ successResult.values?.length }} 筆量測點。點位已進入即時 SPC 管制引擎分析。</p>
      <div class="pt-2">
        <router-link :to="`/spc/?partProcessCharacteristicId=${selectedMapping?.id}&batchId=${successResult.lotNo || successResult.batchNo}`" class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-emerald-600 hover:bg-emerald-500 text-white font-bold text-xs transition-all shadow-sm">
          前往 SPC 管制圖查看趨勢 <ArrowRight class="w-3.5 h-3.5" />
        </router-link>
      </div>
    </div>

    <!-- Main Entry Panel -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl space-y-6">
      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Part Selection -->
        <div>
          <label class="block text-xs font-bold text-slate-500 dark:text-slate-400 mb-1.5">1. 選擇產品料號 (Part No) *</label>
          <select v-model="selectedProductId" class="w-full px-3 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
            <option value="">-- 請選擇 --</option>
            <option v-for="p in products" :key="p.id" :value="p.id">[{{ p.productCode }}] {{ p.productName }}</option>
          </select>
        </div>

        <!-- Station Selection -->
        <div>
          <label class="block text-xs font-bold text-slate-500 dark:text-slate-400 mb-1.5">2. 選擇工站 (Station) *</label>
          <select v-model="selectedStationId" :disabled="!selectedProductId" class="w-full px-3 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
            <option value="">-- 請選擇 --</option>
            <option v-for="s in filteredStations" :key="s.id" :value="s.id">{{ s.stationName }}</option>
          </select>
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
        <!-- Characteristic / Inspection Item -->
        <div>
          <label class="block text-xs font-bold text-slate-500 dark:text-slate-400 mb-1.5">3. 檢驗項目 (Inspection Item) *</label>
          <select v-model="selectedItemId" :disabled="!selectedStationId" class="w-full px-3 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500 disabled:opacity-50">
            <option value="">-- 請選擇 --</option>
            <option v-for="i in filteredItems" :key="i.id" :value="i.id">{{ i.itemName }}</option>
          </select>
        </div>

        <!-- Operator Name -->
        <div>
          <label class="block text-xs font-bold text-slate-500 dark:text-slate-400 mb-1.5">作業人員 (Operator) *</label>
          <div class="relative">
            <input v-model="payload.operatorName" placeholder="輸入人員名稱或工號..." class="w-full pl-9 pr-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-sm font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
            <User class="w-4 h-4 text-slate-400 absolute left-3 top-3.5" />
          </div>
        </div>
      </div>

      <!-- Traceability Details (Accordion-style or simplified) -->
      <div class="p-4 rounded-2xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-800 space-y-4">
        <div class="flex items-center gap-1.5 text-xs font-bold text-slate-500 dark:text-slate-400">
          <Info class="w-4 h-4 text-blue-500" /> 批次追溯欄位配置 (可選)
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div>
            <label class="block text-[11px] font-bold text-slate-400 mb-1">生產批號 (Lot No)</p>
            <input v-model="payload.lotNo" placeholder="如: LOT-2026A" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label class="block text-[11px] font-bold text-slate-400 mb-1">零件序號 (Serial No)</p>
            <input v-model="payload.serialNo" placeholder="如: SN-0988" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label class="block text-[11px] font-bold text-slate-400 mb-1">量測時間</p>
            <input type="datetime-local" v-model="payload.measuredAt" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
          </div>
        </div>
      </div>

      <!-- Dynamic Samples Grid -->
      <div v-if="selectedItemId" class="space-y-4 border-t border-slate-200 dark:border-slate-800 pt-6">
        <div class="flex items-center justify-between">
          <h3 class="text-sm font-black text-slate-800 dark:text-white">
            量測值鍵入區 (基準組數 N = {{ sampleSize }})
          </h3>
          <span v-if="selectedItemDetails" class="text-xs font-bold text-slate-400 bg-slate-100 dark:bg-slate-800 px-3 py-1 rounded-full border border-slate-200 dark:border-slate-700">
            規格界限：LSL = <strong class="text-slate-600 dark:text-slate-300">{{ selectedItemDetails.lsl ?? 'N/A' }}</strong>, 
            USL = <strong class="text-slate-600 dark:text-slate-300">{{ selectedItemDetails.usl ?? 'N/A' }}</strong>
          </span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
          <div v-for="(val, idx) in payload.values" :key="idx" class="space-y-1">
            <div class="flex items-center justify-between text-xs font-bold">
              <span class="text-slate-500">樣本 #{{ val.sampleNo }}</span>
              <span v-if="checkOutOfSpec(val.valueNumeric)" class="text-red-500 text-[10px] animate-pulse">⚠️ 超出規格</span>
            </div>
            <input
              :ref="el => setInputRef(el, idx)"
              type="number"
              step="any"
              v-model.number="val.valueNumeric"
              @keydown.enter.prevent="handleEnter(idx)"
              placeholder="輸入數值..."
              class="w-full px-4 py-3 rounded-xl border text-base font-extrabold text-slate-800 dark:text-white bg-slate-50 dark:bg-slate-800/80 focus:ring-2 transition-all"
              :class="checkOutOfSpec(val.valueNumeric) 
                ? 'border-red-500 ring-2 ring-red-500/10 shadow-[0_0_10px_rgba(239,68,68,0.25)]' 
                : 'border-slate-300 dark:border-slate-700 focus:ring-blue-500'"
            />
          </div>
        </div>
        <p class="text-[11px] font-bold text-slate-400 italic">💡 提示：輸入數值後按 <kbd class="px-1.5 py-0.5 rounded bg-slate-100 dark:bg-slate-800 text-[10px] font-black border dark:border-slate-700">Enter</kbd> 鍵可自動跳轉至下一個樣本，在最後一格按 Enter 將自動送出！</p>
      </div>

      <!-- Submit Footer -->
      <div v-if="selectedItemId" class="flex justify-end pt-4 border-t border-slate-200 dark:border-slate-800">
        <button
          type="button"
          @click="submitBatch"
          :disabled="submitting"
          class="flex items-center gap-2 px-8 py-3 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-extrabold shadow-lg shadow-blue-500/20 disabled:opacity-50 transition-all text-sm"
        >
          <RefreshCw v-if="submitting" class="w-4 h-4 animate-spin" />
          {{ submitting ? "正在儲存點位..." : "儲存並上傳批次" }}
        </button>
      </div>
    </div>
  </div>
</template>
