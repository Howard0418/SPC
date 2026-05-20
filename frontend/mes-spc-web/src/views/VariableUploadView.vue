<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import {
  UploadCloud,
  FileText,
  CheckCircle2,
  ShieldAlert,
  Sparkles,
  ArrowRight,
  RefreshCw,
  Download,
  Settings,
  Table,
  HelpCircle
} from "lucide-vue-next";

const router = useRouter();
const fileInput = ref(null);
const selectedFile = ref(null);
const mode = ref("excel"); // 'excel' or 'json'
const jsonInput = ref('[{"PartNo":"P-1001","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"THICKNESS","LotNo":"L-20260518","SampleNo":1,"MeasuredValue":10.05,"MeasuredAt":"2026-05-18T08:00:00","Operator":"OP-01"}]');
const loading = ref(false);
const err = ref("");
const successBatchId = ref("");

// Column Mapping state
const fileHeaders = ref([]);
const rawRowsData = ref([]);
const isMappingMode = ref(false);
const columnMappings = ref({}); // SystemKey -> FileHeader

const systemFields = [
  { key: "PartNo", label: "產品料號 (PartNo) *", required: true, altNames: ["料號", "產品", "part", "partno", "part_no", "product"] },
  { key: "ProcessCode", label: "工站製程代碼 (ProcessCode) *", required: true, altNames: ["製程", "工站", "process", "processcode", "process_code", "station"] },
  { key: "CharacteristicCode", label: "檢驗項目代碼 (CharacteristicCode) *", required: true, altNames: ["項目", "特性", "檢驗項目", "characteristic", "characteristiccode", "char_code", "item"] },
  { key: "MachineCode", label: "生產機台代碼 (MachineCode)", required: false, altNames: ["機台", "設備", "machine", "machinecode", "machine_code", "eqp", "device"] },
  { key: "MeasuredValue", label: "量測數值 (MeasuredValue) *", required: true, altNames: ["測量值", "數值", "值", "measuredvalue", "measured_value", "value", "val"] },
  { key: "LotNo", label: "生產批號 (LotNo)", required: false, altNames: ["批號", "lot", "lotno", "lot_no", "batch"] },
  { key: "SerialNo", label: "零件序號 (SerialNo)", required: false, altNames: ["序號", "工單", "serial", "serialno", "serial_no", "sn", "workorder"] },
  { key: "MeasuredAt", label: "量測時間 (MeasuredAt)", required: false, altNames: ["時間", "日期", "時間戳記", "measuredat", "measured_at", "time", "date", "timestamp"] },
  { key: "Operator", label: "作業人員 (Operator)", required: false, altNames: ["作業員", "人員", "operator", "op", "user"] },
  { key: "SampleNo", label: "樣本序號 (SampleNo)", required: false, altNames: ["樣本", "樣本編號", "sampleno", "sample_no", "sample"] }
];

// Load SheetJS dynamically from CDN
function loadXlsxLib() {
  return new Promise((resolve, reject) => {
    if (window.XLSX) {
      resolve(window.XLSX);
      return;
    }
    const script = document.createElement("script");
    script.src = "https://cdn.jsdelivr.net/npm/xlsx@0.18.5/dist/xlsx.full.min.js";
    script.onload = () => resolve(window.XLSX);
    script.onerror = () => reject(new Error("無法載入 Excel 解析程式庫。請檢查您的網路連線。"));
    document.head.appendChild(script);
  });
}

async function handleFileChange(e) {
  const files = e.target.files;
  if (!files || files.length === 0) return;

  const file = files[0];
  selectedFile.value = file;
  err.value = "";
  isMappingMode.value = false;
  fileHeaders.value = [];
  rawRowsData.value = [];
  columnMappings.value = {};

  loading.value = true;
  try {
    const XLSX = await loadXlsxLib();
    const reader = new FileReader();

    reader.onload = (evt) => {
      try {
        const data = new Uint8Array(evt.target.result);
        const workbook = XLSX.read(data, { type: "array" });
        if (workbook.SheetNames.length === 0) {
          throw new Error("Excel 檔案中找不到任何工作表。");
        }
        const ws = workbook.Sheets[workbook.SheetNames[0]];
        const jsonData = XLSX.utils.sheet_to_json(ws, { defval: "" });

        if (jsonData.length === 0) {
          throw new Error("工作表中沒有任何數據列。");
        }

        // Get Headers from keys of all items to be safe
        const headersSet = new Set();
        jsonData.forEach(row => {
          Object.keys(row).forEach(k => headersSet.add(k));
        });
        fileHeaders.value = Array.from(headersSet);
        rawRowsData.value = jsonData;

        // Perform smart fuzzy auto-mapping
        runFuzzyAutoMapping();
        isMappingMode.value = true;
      } catch (ex) {
        err.value = "解析 Excel 失敗：" + ex.message;
        selectedFile.value = null;
      } finally {
        loading.value = false;
      }
    };

    reader.readAsArrayBuffer(file);
  } catch (ex) {
    err.value = ex.message;
    selectedFile.value = null;
    loading.value = false;
  }
}

function runFuzzyAutoMapping() {
  systemFields.forEach(field => {
    // Try to find a matching file header using altNames or exact match
    const match = fileHeaders.value.find(h => {
      const hClean = h.toLowerCase().trim().replace(/[\s-_]/g, "");
      return field.altNames.some(alt => {
        const altClean = alt.toLowerCase().trim().replace(/[\s-_]/g, "");
        return hClean === altClean || hClean.includes(altClean) || altClean.includes(hClean);
      });
    });
    if (match) {
      columnMappings.value[field.key] = match;
    } else {
      columnMappings.value[field.key] = "";
    }
  });
}

// Preview first 3 mapped rows
const mappedPreviewData = computed(() => {
  return rawRowsData.value.slice(0, 3).map(row => {
    const result = {};
    systemFields.forEach(field => {
      const mappedHeader = columnMappings.value[field.key];
      result[field.key] = mappedHeader ? row[mappedHeader] : "";
    });
    return result;
  });
});

async function downloadTemplate() {
  try {
    const res = await api.get("/uploads/template/variable", { responseType: "blob" });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", "Variable_Import_Template.xlsx");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  } catch (e) {
    alert("下載範本失敗: " + getApiErrorMessage(e));
  }
}

async function uploadMappedData() {
  // Validate that all required system fields are mapped
  const missingFields = systemFields
    .filter(f => f.required && !columnMappings.value[f.key])
    .map(f => f.label);
  
  if (missingFields.length > 0) {
    err.value = "請先對應所有必填欄位：" + missingFields.join(", ");
    return;
  }

  loading.value = true;
  err.value = "";

  try {
    // Transform all raw rows according to mapping configuration
    const transformedRows = rawRowsData.value.map(row => {
      const obj = {};
      systemFields.forEach(field => {
        const mappedHeader = columnMappings.value[field.key];
        if (mappedHeader && row[mappedHeader] !== undefined && row[mappedHeader] !== null) {
          obj[field.key] = String(row[mappedHeader]).trim();
        } else {
          obj[field.key] = "";
        }
      });
      // Fallback machine code if blank
      if (!obj.MachineCode && obj.ProcessCode) {
        obj.MachineCode = `${obj.ProcessCode}-M01`;
      }
      return obj;
    });

    const res = await api.post("/uploads/variable", transformedRows);
    successBatchId.value = res.data.uploadBatchId;
    router.push(`/uploads/${successBatchId.value}/preview`);
  } catch (e) {
    err.value = "送出映射數據失敗：" + getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function testUploadDemoExcel() {
  loading.value = true;
  err.value = "";
  try {
    const res = await api.get("/uploads/template/variable", { responseType: "blob" });
    const file = new File([res.data], "Variable_Demo_Template.xlsx", { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
    selectedFile.value = file;
    
    // Simulate file input trigger manually
    const mockEvent = { target: { files: [file] } };
    await handleFileChange(mockEvent);
  } catch (e) {
    err.value = "自動載入範本失敗：" + getApiErrorMessage(e);
    loading.value = false;
  }
}

async function uploadJson() {
  loading.value = true;
  err.value = "";
  try {
    const payload = JSON.parse(jsonInput.value);
    const res = await api.post("/uploads/variable", payload);
    successBatchId.value = res.data.uploadBatchId;
    router.push(`/uploads/${successBatchId.value}/preview`);
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Jumbotron banner -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-blue-600 via-indigo-600 to-indigo-850 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-cyan-200 border border-white/20">
          <UploadCloud class="w-3.5 h-3.5" /> Stage 1: 計量型量測數據上傳與暫存區
        </div>
        <h1 class="text-3xl font-black tracking-tight">計量型 (Variable) 抽樣檢驗數據上傳</h1>
        <p class="text-indigo-100 text-sm max-w-xl">
          支援廠區自動量測機台匯出之 Excel / CSV 檔案上傳。配備「智能對照對應器」，非標準表頭欄位亦可輕鬆對應。
        </p>
      </div>
    </div>

    <!-- Mode Selector & Template Download -->
    <div class="flex border-b border-slate-200 dark:border-slate-800 gap-4 justify-between items-center flex-wrap">
      <div class="flex gap-4">
        <button
          @click="mode = 'excel'"
          class="pb-3 text-sm font-bold flex items-center gap-2 transition-all"
          :class="mode === 'excel' ? 'text-blue-600 dark:text-blue-400 border-b-2 border-blue-600 dark:border-blue-400' : 'text-slate-400 dark:text-slate-500 hover:text-slate-600'"
        >
          <FileText class="w-4 h-4" /> 智慧對照 Excel / CSV 匯入
        </button>
        <button
          @click="mode = 'json'"
          class="pb-3 text-sm font-bold flex items-center gap-2 transition-all"
          :class="mode === 'json' ? 'text-blue-600 dark:text-blue-400 border-b-2 border-blue-600 dark:border-blue-400' : 'text-slate-400 dark:text-slate-500 hover:text-slate-600'"
        >
          <Sparkles class="w-4 h-4" /> JSON 格式直接送出
        </button>
      </div>
      <button
        @click="downloadTemplate"
        class="flex items-center gap-2 mb-2 px-4 py-2 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold shadow-md shadow-blue-500/20 transition-all text-xs"
      >
        <Download class="w-4 h-4" /> 下載標準計量型 Excel 範本
      </button>
    </div>

    <div v-if="err" class="p-4 rounded-2xl bg-red-500/10 border border-red-500/30 text-red-500 text-sm flex items-center gap-3">
      <ShieldAlert class="w-5 h-5 flex-shrink-0" /> {{ err }}
    </div>

    <!-- Excel Mode -->
    <div v-if="mode === 'excel'" class="space-y-6">
      <!-- File Selector Dropzone -->
      <div class="p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
        <div
          @click="fileInput?.click()"
          class="border-2 border-dashed border-slate-300 dark:border-slate-700 hover:border-blue-500 dark:hover:border-blue-500 rounded-3xl p-12 text-center cursor-pointer transition-all bg-slate-50/50 dark:bg-slate-800/40 group flex flex-col items-center justify-center space-y-3"
        >
          <input ref="fileInput" type="file" accept=".xlsx,.xls,.csv" class="hidden" @change="handleFileChange" />
          <div class="p-4 rounded-full bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 group-hover:scale-110 transition-transform">
            <UploadCloud class="w-8 h-8" />
          </div>
          <div>
            <p class="text-sm font-bold text-slate-800 dark:text-white">點擊選擇檔案或將 Excel / CSV 拖曳至此處</p>
            <p class="text-xs text-slate-400 mt-1">系統將讀取欄位，引導進行智慧欄位對照（免改 Excel 即可匯入）</p>
          </div>
          <div v-if="selectedFile" class="mt-4 px-4 py-2 rounded-xl bg-blue-600/10 text-blue-600 dark:text-blue-400 text-xs font-semibold flex items-center gap-2 border border-blue-500/30">
            <CheckCircle2 class="w-4 h-4 text-blue-500" /> 已選擇檔案：{{ selectedFile.name }} ({{ (selectedFile.size / 1024).toFixed(1) }} KB)
          </div>
        </div>

        <div v-if="!isMappingMode" class="flex flex-wrap items-center justify-end gap-3">
          <button
            @click="testUploadDemoExcel"
            :disabled="loading"
            class="flex items-center gap-2 px-5 py-3 rounded-xl bg-slate-800 hover:bg-slate-700 text-cyan-300 font-bold shadow-md transition-all text-sm border border-cyan-500/30"
          >
            <Sparkles class="w-4 h-4 text-cyan-400" :class="{ 'animate-spin': loading }" />
            ⚡ 載入標準範本測試
          </button>
        </div>
      </div>

      <!-- Column Mapping Workspace -->
      <div v-if="isMappingMode" class="p-6 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-xl space-y-6">
        <div class="flex items-center gap-2 border-b border-slate-200 dark:border-slate-800 pb-3">
          <Settings class="w-5 h-5 text-blue-500" />
          <h3 class="text-lg font-black text-slate-800 dark:text-white">智慧欄位對照器 (Excel Mapping Manager)</h3>
        </div>
        
        <p class="text-xs text-slate-400">系統已完成初步模糊配對，請檢查或自訂各 SPC 資料欄位對應的工作表欄位：</p>

        <!-- Fields Mapping Matrix Grid -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div v-for="field in systemFields" :key="field.key" class="p-3 rounded-2xl bg-slate-50 dark:bg-slate-800/40 border border-slate-200 dark:border-slate-700 flex flex-col justify-between gap-2">
            <div class="flex items-center justify-between">
              <span class="text-xs font-black text-slate-700 dark:text-slate-300">
                {{ field.label }}
              </span>
              <HelpCircle v-if="!field.required" class="w-3.5 h-3.5 text-slate-400 cursor-help" title="此為選填項目，若檔案無此欄位可保留空白。" />
            </div>
            <select v-model="columnMappings[field.key]" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-900 text-xs font-bold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
              <option value="">-- 不對應（留空） --</option>
              <option v-for="h in fileHeaders" :key="h" :value="h">{{ h }}</option>
            </select>
          </div>
        </div>

        <!-- Mapped Live Preview -->
        <div class="space-y-3">
          <h4 class="text-xs font-black text-slate-700 dark:text-slate-300 flex items-center gap-1.5">
            <Table class="w-4 h-4 text-indigo-500" /> 即時對照前 3 筆資料預覽 (Data Mapping Preview)
          </h4>
          <div class="overflow-x-auto rounded-2xl border border-slate-200 dark:border-slate-800">
            <table class="w-full text-left text-xs">
              <thead class="bg-slate-50 dark:bg-slate-800/60 text-slate-400 font-bold uppercase">
                <tr>
                  <th v-for="f in systemFields" :key="f.key" class="p-2.5 font-bold text-[10px]">
                    {{ f.key }}
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-600 dark:text-slate-300 font-medium">
                <tr v-for="(pRow, pIdx) in mappedPreviewData" :key="pIdx" class="hover:bg-slate-50/50 dark:hover:bg-slate-850/50">
                  <td v-for="f in systemFields" :key="f.key" class="p-2.5 font-mono max-w-[120px] truncate">
                    {{ pRow[f.key] || '-' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- Mapping submit bar -->
        <div class="flex justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
          <button @click="isMappingMode = false; selectedFile = null" :disabled="loading" class="px-5 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 text-slate-600 dark:text-slate-300 text-xs font-bold hover:bg-slate-50 dark:hover:bg-slate-850">
            重新選檔
          </button>
          <button @click="uploadMappedData" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-extrabold shadow-lg shadow-blue-500/20 text-xs transition-all">
            <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
            <ArrowRight v-else class="w-4 h-4" />
            確認映射並上傳批次
          </button>
        </div>
      </div>
    </div>

    <!-- JSON Mode -->
    <div v-if="mode === 'json'" class="p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-bold text-slate-800 dark:text-white">貼上原始檢驗 JSON 陣列</h3>
        <span class="text-xs text-slate-400 font-mono">支援 PartNo, ProcessCode, MachineCode, CharacteristicCode</span>
      </div>
      <textarea v-model="jsonInput" rows="10" class="w-full p-4 rounded-2xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 font-mono text-xs text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-blue-500"></textarea>
      
      <div class="flex justify-end">
        <button
          @click="uploadJson"
          :disabled="loading"
          class="flex items-center gap-2 px-6 py-3 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-bold shadow-lg shadow-blue-500/20 disabled:opacity-50 transition-all text-sm"
        >
          <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
          <ArrowRight v-else class="w-4 h-4" />
          {{ loading ? '資料傳送與處理中...' : '送出 JSON 並進入 Stage 2 檢核' }}
        </button>
      </div>
    </div>
  </div>
</template>
