<script setup>
import { ref, computed } from "vue";
import { useRouter } from "vue-router";
import { api, getApiErrorMessage } from "../api/client";
import { parseUploadSpreadsheet } from "../utils/parseUploadSpreadsheet";
import {
  UploadCloud,
  FileSpreadsheet,
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
const jsonInput = ref('[{"PartNo":"P-1002","ProcessCode":"ST-02","MachineCode":"M-02","CharacteristicCode":"DEFECT_RATE","LotNo":"L-20260518-A","SampleNo":1,"InspectedQty":500,"DefectQty":12,"DefectCount":15,"UnitCount":500,"MeasuredAt":"2026-05-18T09:00:00","Operator":"OP-02"}]');
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
  { key: "InspectedQty", label: "總檢驗數 (InspectedQty) *", required: true, altNames: ["總數", "檢驗數", "抽樣數", "總檢驗數", "inspectedqty", "inspected_qty", "total", "qty"] },
  { key: "DefectQty", label: "不良品數 (DefectQty) *", required: true, altNames: ["不良數", "不良品", "defectqty", "defect_qty", "defects"] },
  { key: "DefectCount", label: "總缺點數 (DefectCount)", required: false, altNames: ["缺點數", "瑕疵數", "defectcount", "defect_count", "errors"] },
  { key: "UnitCount", label: "單位檢驗數 (UnitCount)", required: false, altNames: ["單位數", "unitcount", "unit_count", "units"] },
  { key: "LotNo", label: "生產批號 (LotNo)", required: false, altNames: ["批號", "lot", "lotno", "lot_no", "batch"] },
  { key: "MeasuredAt", label: "量測時間 (MeasuredAt)", required: false, altNames: ["時間", "日期", "時間戳記", "measuredat", "measured_at", "time", "date", "timestamp"] },
  { key: "Operator", label: "作業人員 (Operator)", required: false, altNames: ["作業員", "人員", "operator", "op", "user"] }
];

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
    const parsed = await parseUploadSpreadsheet(file);
    fileHeaders.value = parsed.headers;
    rawRowsData.value = parsed.rows;
    runFuzzyAutoMapping();
    isMappingMode.value = true;
  } catch (ex) {
    err.value = ex?.message || String(ex);
    selectedFile.value = null;
  } finally {
    loading.value = false;
  }
}

function runFuzzyAutoMapping() {
  systemFields.forEach(field => {
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
    const res = await api.get("/uploads/template/attribute", { responseType: "blob" });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement("a");
    link.href = url;
    link.setAttribute("download", "Attribute_Import_Template.xlsx");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  } catch (e) {
    alert("下載範本失敗: " + getApiErrorMessage(e));
  }
}

async function uploadMappedData() {
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
      if (!obj.MachineCode && obj.ProcessCode) {
        obj.MachineCode = `${obj.ProcessCode}-M02`;
      }
      return obj;
    });

    const res = await api.post("/uploads/attribute", transformedRows);
    successBatchId.value = res.data.uploadBatchId;
    router.push(`/uploads/${successBatchId.value}/preview`);
  } catch (e) {
    if (e?.response?.status === 409) {
      err.value = "系統防呆：這個檔案之前已經匯入過了，為保護 SPC 資料準確度，請勿重複匯入相同的檔案！";
    } else {
      err.value = "送出映射數據失敗：" + getApiErrorMessage(e);
    }
  } finally {
    loading.value = false;
  }
}

async function testUploadDemoExcel() {
  loading.value = true;
  err.value = "";
  try {
    const res = await api.get("/uploads/template/attribute", { responseType: "blob" });
    const file = new File([res.data], "Attribute_Demo_Template.xlsx", { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" });
    selectedFile.value = file;
    
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
    const res = await api.post("/uploads/attribute", payload);
    successBatchId.value = res.data.uploadBatchId;
    router.push(`/uploads/${successBatchId.value}/preview`);
  } catch (e) {
    if (e?.response?.status === 409) {
      err.value = "系統防呆：這個 JSON 之前已經匯入過了，請勿重複匯入相同的資料！";
    } else {
      err.value = getApiErrorMessage(e);
    }
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="max-w-4xl mx-auto space-y-6">
    <!-- Jumbotron banner -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-teal-600 via-emerald-600 to-cyan-705 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-cyan-200 border border-white/20">
          <FileSpreadsheet class="w-3.5 h-3.5" /> Stage 1: 計數型檢驗數據上傳與暫存區
        </div>
        <h1 class="text-3xl font-black tracking-tight">計數型 (Attribute) 抽樣檢驗數據上傳</h1>
        <p class="text-emerald-100 text-sm max-w-xl">
          支援 P, NP, C, U 管制圖所需之總數、不良數、缺點數與檢驗單位數等資料匯入。配備「智能對照對應器」，無痛對接各種客製化表頭。
        </p>
      </div>
    </div>

    <!-- Mode Selector & Template Download -->
    <div class="flex border-b border-slate-200 dark:border-slate-800 gap-4 justify-between items-center flex-wrap">
      <div class="flex gap-4">
        <button
          @click="mode = 'excel'"
          class="pb-3 text-sm font-bold flex items-center gap-2 transition-all"
          :class="mode === 'excel' ? 'text-teal-600 dark:text-teal-400 border-b-2 border-teal-600 dark:border-teal-400' : 'text-slate-400 dark:text-slate-500 hover:text-slate-600'"
        >
          <FileText class="w-4 h-4" /> 智慧對照 Excel / CSV 匯入
        </button>
        <button
          @click="mode = 'json'"
          class="pb-3 text-sm font-bold flex items-center gap-2 transition-all"
          :class="mode === 'json' ? 'text-teal-600 dark:text-teal-400 border-b-2 border-teal-600 dark:border-teal-400' : 'text-slate-400 dark:text-slate-500 hover:text-slate-600'"
        >
          <Sparkles class="w-4 h-4" /> JSON 格式直接送出
        </button>
      </div>
      <button
        @click="downloadTemplate"
        class="flex items-center gap-2 mb-2 px-4 py-2 rounded-xl bg-teal-600 hover:bg-teal-500 text-white font-bold shadow-md shadow-teal-500/20 transition-all text-xs"
      >
        <Download class="w-4 h-4" /> 下載標準計數型 Excel 範本
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
          class="border-2 border-dashed border-slate-300 dark:border-slate-700 hover:border-teal-500 dark:hover:border-teal-500 rounded-3xl p-12 text-center cursor-pointer transition-all bg-slate-50/50 dark:bg-slate-800/40 group flex flex-col items-center justify-center space-y-3"
        >
          <input ref="fileInput" type="file" accept=".xlsx,.xls,.csv" class="hidden" @change="handleFileChange" />
          <div class="p-4 rounded-full bg-teal-50 dark:bg-teal-900/30 text-teal-600 dark:text-teal-400 group-hover:scale-110 transition-transform">
            <FileSpreadsheet class="w-8 h-8" />
          </div>
          <div>
            <p class="text-sm font-bold text-slate-800 dark:text-white">點擊選擇檔案或將 Excel / CSV 拖曳至此處</p>
            <p class="text-xs text-slate-400 mt-1">系統將讀取欄位，引導進行智慧欄位對照（免改 Excel 即可匯入）</p>
          </div>
          <div v-if="selectedFile" class="mt-4 px-4 py-2 rounded-xl bg-teal-600/10 text-teal-600 dark:text-teal-400 text-xs font-semibold flex items-center gap-2 border border-teal-500/30">
            <CheckCircle2 class="w-4 h-4 text-teal-500" /> 已選擇檔案：{{ selectedFile.name }} ({{ (selectedFile.size / 1024).toFixed(1) }} KB)
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
          <Settings class="w-5 h-5 text-teal-550" />
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
            <select v-model="columnMappings[field.key]" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-600 bg-white dark:bg-slate-900 text-xs font-bold text-slate-800 dark:text-white focus:ring-2 focus:ring-teal-500">
              <option value="">-- 不對應（留空） --</option>
              <option v-for="h in fileHeaders" :key="h" :value="h">{{ h }}</option>
            </select>
          </div>
        </div>

        <!-- Mapped Live Preview -->
        <div class="space-y-3">
          <h4 class="text-xs font-black text-slate-700 dark:text-slate-300 flex items-center gap-1.5">
            <Table class="w-4 h-4 text-emerald-500" /> 即時對照前 3 筆資料預覽 (Data Mapping Preview)
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
          <button @click="uploadMappedData" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-teal-600 hover:bg-teal-500 text-white font-extrabold shadow-lg shadow-teal-500/20 text-xs transition-all">
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
      <textarea v-model="jsonInput" rows="10" class="w-full p-4 rounded-2xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-950 font-mono text-xs text-slate-800 dark:text-slate-200 focus:ring-2 focus:ring-teal-500"></textarea>
      
      <div class="flex justify-end">
        <button
          @click="uploadJson"
          :disabled="loading"
          class="flex items-center gap-2 px-6 py-3 rounded-xl bg-teal-600 hover:bg-teal-500 text-white font-bold shadow-lg shadow-teal-500/20 disabled:opacity-50 transition-all text-sm"
        >
          <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
          <ArrowRight v-else class="w-4 h-4" />
          {{ loading ? '資料傳送與處理中...' : '送出 JSON 並進入 Stage 2 檢核' }}
        </button>
      </div>
    </div>
  </div>
</template>
