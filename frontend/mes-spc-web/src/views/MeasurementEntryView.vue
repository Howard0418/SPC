<script setup>
import { ref, computed, onMounted, watch } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Activity,
  CheckCircle2,
  AlertTriangle,
  User,
  Info,
  RefreshCw,
  Scan,
  MonitorCheck,
  ArrowRight,
  Settings
} from "lucide-vue-next";

// Master Data
const products = ref([]);
const stations = ref([]);
const inspectionItems = ref([]);
const mappings = ref([]);

// Local Binding State
const boundStationId = ref(null);
const showBindModal = ref(false);

// Work Order & Part State
const inputWoNo = ref("");
const selectedWorkOrder = ref(null);
const selectedProductId = ref(null);
const selectedProductName = ref("");
const woLoading = ref(false);

// Measurement Entry State
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

// Soft Hold State
const softHoldActive = ref(false);
const activeAlerts = ref([]);
const handleForm = ref({ rootCause: "", correctiveAction: "" });
const handlingAlerts = ref(false);

// Inputs refs for Enter tabbing
const inputRefs = ref([]);
const setInputRef = (el, index) => { if (el) inputRefs.value[index] = el; };

onMounted(async () => {
  resetForm();
  await loadMetadata();
  checkBinding();
});

function checkBinding() {
  const saved = localStorage.getItem("spc_bound_station_id");
  if (saved) {
    boundStationId.value = Number(saved);
  } else {
    showBindModal.value = true;
  }
}

function saveBinding() {
  if (boundStationId.value) {
    localStorage.setItem("spc_bound_station_id", boundStationId.value);
    showBindModal.value = false;
  }
}

const boundStationName = computed(() => {
  if (!boundStationId.value || stations.value.length === 0) return "尚未綁定";
  const st = stations.value.find(s => s.id === boundStationId.value);
  return st ? st.stationName : "未知工站";
});

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
    measuredAt: new Date().toISOString().substring(0, 16),
    operatorName: payload.value.operatorName || "", 
    values: []
  };
  
  inputWoNo.value = "";
  selectedWorkOrder.value = null;
  selectedProductId.value = null;
  selectedProductName.value = "";
  selectedItemId.value = "";
  successResult.value = null;
  error.value = "";
}

async function fetchWorkOrder() {
  if (!inputWoNo.value.trim() || !boundStationId.value) return;
  woLoading.value = true;
  error.value = "";
  successResult.value = null;
  try {
    const res = await api.get(`/v2/work-orders?workOrderNo=${encodeURIComponent(inputWoNo.value.trim())}`);
    const wos = res.data;
    if (wos && wos.length > 0) {
      selectedWorkOrder.value = wos[0];
      selectedProductId.value = wos[0].productId;
      
      const p = products.value.find(x => x.id === selectedProductId.value);
      selectedProductName.value = p ? `[${p.productCode}] ${p.productName}` : "未知料號";
      
      payload.value.workOrderId = selectedWorkOrder.value.id;
      payload.value.productId = selectedProductId.value;
      payload.value.stationId = boundStationId.value;
      payload.value.lotNo = selectedWorkOrder.value.workOrderNo;
      
      // Auto-select item if only one
      if (filteredItems.value.length === 1) {
        selectedItemId.value = filteredItems.value[0].id;
      } else {
        selectedItemId.value = "";
      }
    } else {
      error.value = `找不到工單號碼: ${inputWoNo.value}`;
      selectedWorkOrder.value = null;
      selectedProductId.value = null;
    }
  } catch (e) {
    error.value = "查詢工單失敗：" + getApiErrorMessage(e);
  } finally {
    woLoading.value = false;
  }
}

const filteredItems = computed(() => {
  if (!selectedProductId.value || !boundStationId.value) return [];
  const activeItemIds = mappings.value
    .filter(m => m.productId === Number(selectedProductId.value) && 
                 m.stationId === Number(boundStationId.value) && 
                 m.isActive)
    .map(m => m.inspectionItemId);
  return inspectionItems.value.filter(i => activeItemIds.includes(i.id) && i.isActive);
});

const selectedMapping = computed(() => {
  if (!selectedProductId.value || !boundStationId.value || !selectedItemId.value) return null;
  return mappings.value.find(m => 
    m.productId === Number(selectedProductId.value) && 
    m.stationId === Number(boundStationId.value) && 
    m.inspectionItemId === Number(selectedItemId.value)
  );
});

const selectedItemDetails = computed(() => {
  if (!selectedItemId.value) return null;
  return inspectionItems.value.find(i => i.id === Number(selectedItemId.value));
});

const sampleSize = computed(() => selectedMapping.value ? selectedMapping.value.sampleSize : 1);

watch(selectedItemId, () => {
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

function handleEnter(index) {
  if (index < payload.value.values.length - 1) {
    const nextEl = inputRefs.value[index + 1];
    if (nextEl) { nextEl.focus(); nextEl.select(); }
  } else {
    submitBatch();
  }
}

const submitBatch = async () => {
  if (!selectedProductId.value || !boundStationId.value || !selectedItemId.value) {
    error.value = "請確認檢驗項目已選擇。";
    return;
  }
  if (!payload.value.operatorName.trim()) {
    error.value = "請輸入作業人員名稱。";
    return;
  }
  
  const hasEmptyVal = payload.value.values.some(v => v.valueNumeric === null || v.valueNumeric === undefined || v.valueNumeric === "");
  if (hasEmptyVal) {
    error.value = "請完整輸入所有樣本之量測值。";
    return;
  }

  submitting.value = true;
  error.value = "";
  successResult.value = null;

  try {
    const finalPayload = JSON.parse(JSON.stringify(payload.value));
    finalPayload.measuredAt = new Date(finalPayload.measuredAt).toISOString();
    finalPayload.values.forEach(v => { v.valueNumeric = Number(v.valueNumeric); });

    const res = await api.post("/measurement-batches", finalPayload);
    
    successResult.value = res.data.batch || res.data;
    
    if (res.data.alerts && res.data.alerts.length > 0) {
      activeAlerts.value = res.data.alerts;
      softHoldActive.value = true;
      handleForm.value = { rootCause: "", correctiveAction: "" };
      return; 
    }
    
    const operator = payload.value.operatorName;
    resetForm();
    payload.value.operatorName = operator;
  } catch (e) {
    error.value = "儲存量測資料失敗：" + getApiErrorMessage(e);
  } finally {
    submitting.value = false;
  }
};

async function submitHandleAlerts() {
  if (!handleForm.value.rootCause.trim() || !handleForm.value.correctiveAction.trim()) {
    alert("發生原因與初步對策為必填欄位！");
    return;
  }
  
  handlingAlerts.value = true;
  try {
    for (const alertObj of activeAlerts.value) {
      await api.post(`/alerts/${alertObj.id}/handle`, handleForm.value);
    }
    softHoldActive.value = false;
    activeAlerts.value = [];
    
    const operator = payload.value.operatorName;
    resetForm();
    payload.value.operatorName = operator; 
  } catch (e) {
    alert("提交失敗：" + getApiErrorMessage(e));
  } finally {
    handlingAlerts.value = false;
  }
}
</script>

<template>
  <div class="max-w-3xl mx-auto space-y-6">
    <!-- Local Station Binding Info -->
    <div class="flex items-center justify-between px-4 py-2 bg-indigo-50 dark:bg-indigo-900/30 border border-indigo-200 dark:border-indigo-800 rounded-xl shadow-sm">
      <div class="flex items-center gap-2">
        <MonitorCheck class="w-5 h-5 text-indigo-600 dark:text-indigo-400" />
        <span class="text-sm font-bold text-indigo-900 dark:text-indigo-300">本機預設綁定工站：</span>
        <span class="text-sm font-black text-indigo-700 dark:text-indigo-400 bg-white dark:bg-slate-800 px-3 py-1 rounded-md border border-indigo-100 dark:border-indigo-700">{{ boundStationName }}</span>
      </div>
      <button @click="showBindModal = true" class="text-xs font-bold text-indigo-600 hover:text-indigo-800 dark:text-indigo-400 dark:hover:text-indigo-300 flex items-center gap-1 bg-white/50 dark:bg-black/20 px-2 py-1.5 rounded-lg transition-colors">
        <Settings class="w-3.5 h-3.5" /> 變更綁定設定
      </button>
    </div>

    <!-- Header -->
    <div class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm flex items-center justify-between">
      <div>
        <div class="flex items-center gap-2 text-xs font-bold uppercase tracking-wider text-blue-600 dark:text-blue-400 mb-1">
          <Activity class="w-4 h-4" /> 掃描驅動零失誤錄入
        </div>
        <h1 class="text-2xl font-black text-slate-800 dark:text-white">現場量測數據錄入系統</h1>
      </div>
      <button @click="resetForm" class="p-2 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-colors" title="重設表單">
        <RefreshCw class="w-5 h-5" />
      </button>
    </div>

    <!-- Guide / Wizard Tip -->
    <div class="p-5 bg-gradient-to-r from-blue-50 to-indigo-50 dark:from-blue-950/30 dark:to-indigo-900/20 border border-blue-100 dark:border-blue-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-blue-100 dark:bg-blue-900/50 rounded-xl text-blue-600 dark:text-blue-400 mt-0.5">
        <Info class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-blue-900 dark:text-blue-300">模組指南：現場量測數據錄入系統 (Measurement Entry)</h4>
        <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
          此模組用於產線現場人員以手動或掃碼方式輸入即時量測檢驗數據。系統將自動比對檢驗基準配置與西方電氣規則，並於異常時即時彈出警報單填寫畫面。
        </p>
      </div>
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
      
      <!-- Scan Work Order Section -->
      <div class="space-y-2 relative">
        <label class="block text-sm font-black text-slate-700 dark:text-slate-300">請掃描或輸入工單號碼 (Work Order No) *</label>
        <div class="relative flex items-center">
          <Scan class="absolute left-4 w-6 h-6 text-indigo-400" />
          <input
            v-model="inputWoNo"
            @keydown.enter.prevent="fetchWorkOrder"
            :disabled="!boundStationId"
            placeholder="使用掃描槍讀取條碼或手動輸入..."
            class="w-full pl-12 pr-4 py-4 rounded-2xl border-2 border-indigo-200 dark:border-indigo-800 bg-indigo-50/50 dark:bg-slate-800/80 text-xl font-bold text-indigo-900 dark:text-indigo-100 focus:border-indigo-500 focus:ring-4 focus:ring-indigo-500/20 transition-all disabled:opacity-50"
          />
          <button 
            @click="fetchWorkOrder"
            :disabled="!inputWoNo || !boundStationId"
            class="absolute right-3 px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-white font-bold rounded-xl text-sm transition-colors disabled:opacity-50"
          >
            <RefreshCw v-if="woLoading" class="w-4 h-4 animate-spin" />
            <span v-else>查詢帶入</span>
          </button>
        </div>
        <p class="text-xs font-bold text-slate-500 mt-1 pl-2">💡 輸入完成請按下 Enter 鍵，系統將自動解析料號並帶出檢驗項目。</p>
      </div>

      <transition enter-active-class="transition-all duration-300" enter-from-class="opacity-0 -translate-y-2" enter-to-class="opacity-100 translate-y-0">
        <div v-if="selectedWorkOrder" class="space-y-6 border-t border-slate-200 dark:border-slate-800 pt-6">
          
          <div class="p-4 bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 rounded-2xl grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <div class="text-[10px] font-black text-slate-400 uppercase tracking-wider mb-0.5">系統已自動鎖定料號</div>
              <div class="text-sm font-bold text-slate-800 dark:text-slate-200">{{ selectedProductName }}</div>
            </div>
            <div>
              <label class="block text-[10px] font-black text-slate-400 uppercase tracking-wider mb-1">作業人員 (Operator) *</label>
              <div class="relative">
                <input v-model="payload.operatorName" placeholder="輸入人員名稱或工號..." class="w-full pl-8 pr-3 py-1.5 rounded-lg border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
                <User class="w-3.5 h-3.5 text-slate-400 absolute left-2.5 top-2" />
              </div>
            </div>
          </div>

          <div>
            <label class="block text-sm font-black text-slate-700 dark:text-slate-300 mb-2">請選擇檢驗項目 (Inspection Item) *</label>
            <select v-model="selectedItemId" class="w-full px-4 py-3 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-base font-bold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
              <option value="">-- 請選擇 --</option>
              <option v-for="i in filteredItems" :key="i.id" :value="i.id">{{ i.itemName }}</option>
            </select>
          </div>

          <!-- Traceability Details (Accordion-style or simplified) -->
          <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
            <div>
              <label class="block text-[11px] font-bold text-slate-400 mb-1">生產批號 (Lot No)</label>
              <input v-model="payload.lotNo" placeholder="如: LOT-2026A" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label class="block text-[11px] font-bold text-slate-400 mb-1">零件序號 (Serial No)</label>
              <input v-model="payload.serialNo" placeholder="如: SN-0988" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label class="block text-[11px] font-bold text-slate-400 mb-1">量測時間</label>
              <input type="datetime-local" v-model="payload.measuredAt" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-xs font-semibold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
        </div>
      </transition>

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

    <!-- Soft Hold Interlock Overlay (Modal) -->
    <transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 scale-95"
      enter-to-class="opacity-100 scale-100"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 scale-100"
      leave-to-class="opacity-0 scale-95"
    >
      <div v-if="softHoldActive" class="fixed inset-0 z-50 flex items-center justify-center bg-red-950/80 backdrop-blur-md p-4">
        <div class="relative w-full max-w-2xl bg-white dark:bg-slate-900 rounded-3xl shadow-2xl border border-red-200 dark:border-red-900 overflow-hidden flex flex-col max-h-[90vh]">
          <!-- Danger Header -->
          <div class="bg-red-600 px-6 py-5 flex items-center gap-4 shrink-0">
            <div class="p-3 bg-white/20 rounded-full animate-pulse">
              <AlertTriangle class="w-8 h-8 text-white" />
            </div>
            <div>
              <h2 class="text-xl font-black text-white tracking-wide">品質異常強制卡控 (Soft Hold)</h2>
              <p class="text-red-100 text-sm font-medium mt-0.5">系統偵測到剛輸入的數據違反品質管制規則，請立刻填寫處置對策解鎖畫面。</p>
            </div>
          </div>

          <div class="p-6 overflow-y-auto flex-1 space-y-6">
            <!-- Alert Details List -->
            <div class="space-y-3">
              <h3 class="text-xs font-bold text-slate-500 uppercase tracking-wider">觸發異常清單</h3>
              <div v-for="al in activeAlerts" :key="al.id" class="p-4 bg-red-50 dark:bg-red-900/20 border-l-4 border-red-500 rounded-r-xl">
                <div class="flex items-start justify-between">
                  <div>
                    <div class="font-bold text-red-800 dark:text-red-300 text-sm mb-1">{{ al.message }}</div>
                    <div class="text-xs text-red-600 dark:text-red-400 font-mono">觸發數值: {{ al.actualValue }}</div>
                  </div>
                  <span class="px-2 py-1 bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-200 text-[10px] font-black rounded">{{ al.alertType }}</span>
                </div>
              </div>
            </div>

            <!-- Mandatory Action Form -->
            <div class="space-y-4 pt-4 border-t border-slate-200 dark:border-slate-800">
              <h3 class="text-xs font-bold text-slate-500 uppercase tracking-wider flex items-center gap-2">
                <CheckCircle2 class="w-4 h-4 text-emerald-500" /> 強制處置紀錄 (必填)
              </h3>
              <div class="space-y-2">
                <label class="block text-sm font-bold text-slate-700 dark:text-slate-200">初步發生原因 (Root Cause)</label>
                <textarea v-model="handleForm.rootCause" rows="2" placeholder="請描述機台狀況、人員操作或材料問題..." class="w-full px-4 py-3 bg-slate-50 dark:bg-slate-800 border border-red-300 dark:border-red-700 rounded-xl text-sm font-medium text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-red-500 transition-all"></textarea>
              </div>
              <div class="space-y-2">
                <label class="block text-sm font-bold text-slate-700 dark:text-slate-200">緊急處置對策 (Corrective Action)</label>
                <textarea v-model="handleForm.correctiveAction" rows="2" placeholder="例如：已停機隔離該批產品、重新校正刀具等..." class="w-full px-4 py-3 bg-slate-50 dark:bg-slate-800 border border-red-300 dark:border-red-700 rounded-xl text-sm font-medium text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-red-500 transition-all"></textarea>
              </div>
            </div>
          </div>

          <!-- Footer Actions -->
          <div class="px-6 py-5 bg-slate-50 dark:bg-slate-800 border-t border-slate-200 dark:border-slate-700 shrink-0 flex justify-end">
            <button type="button" @click="submitHandleAlerts" :disabled="handlingAlerts" class="flex items-center gap-2 px-8 py-3 rounded-xl bg-red-600 hover:bg-red-500 text-white font-extrabold shadow-lg shadow-red-500/30 disabled:opacity-50 transition-all text-sm">
              <RefreshCw v-if="handlingAlerts" class="w-4 h-4 animate-spin" />
              {{ handlingAlerts ? '正在提交...' : '確認提交處置並解除卡控' }}
            </button>
          </div>
        </div>
      </div>
    </transition>

    <!-- Station Binding Modal -->
    <transition enter-active-class="transition-all duration-300" enter-from-class="opacity-0" enter-to-class="opacity-100">
      <div v-if="showBindModal" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/80 backdrop-blur-sm p-4">
        <div class="w-full max-w-md bg-white dark:bg-slate-900 rounded-3xl shadow-2xl p-8 space-y-6">
          <div class="text-center space-y-2">
            <div class="mx-auto w-16 h-16 bg-indigo-100 dark:bg-indigo-900/50 rounded-full flex items-center justify-center mb-4">
              <MonitorCheck class="w-8 h-8 text-indigo-600 dark:text-indigo-400" />
            </div>
            <h2 class="text-2xl font-black text-slate-800 dark:text-white">設定本機所屬工站</h2>
            <p class="text-sm text-slate-500 dark:text-slate-400">請設定這台電腦或平版，目前固定擺放在哪一個生產工站？系統將以此為基準自動帶出檢驗條件。</p>
          </div>
          <div>
            <select v-model="boundStationId" class="w-full px-4 py-3 rounded-xl border-2 border-indigo-200 dark:border-indigo-800 bg-indigo-50/50 dark:bg-slate-800 text-base font-bold text-slate-800 dark:text-white focus:ring-indigo-500 focus:border-indigo-500 transition-all">
              <option :value="null">-- 請選擇固定工站 --</option>
              <option v-for="s in stations" :key="s.id" :value="s.id">{{ s.stationName }}</option>
            </select>
          </div>
          <button @click="saveBinding" :disabled="!boundStationId" class="w-full py-3 bg-indigo-600 hover:bg-indigo-500 text-white font-bold rounded-xl shadow-lg shadow-indigo-500/30 transition-all disabled:opacity-50">
            確認綁定設定
          </button>
        </div>
      </div>
    </transition>

  </div>
</template>
