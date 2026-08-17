<script setup>
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import * as XLSX from "xlsx";
import { api, getApiErrorMessage } from "../api/client";
import { parseUploadSpreadsheet } from "../utils/parseUploadSpreadsheet";
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
const jsonInput = ref('[{"ControlScope":"PROCESS","ProcessCode":"ST-01","MachineCode":"M-01","CharacteristicCode":"THICKNESS","LotNo":"L-20260518","SampleNo":1,"MeasuredValue":10.05,"RecheckValue":10.04,"AdjustAction":"添加","AdjustAmount":0.5,"MeasuredAt":"2026-05-18T08:00:00","Operator":"OP-01"}]');
const loading = ref(false);
const err = ref("");
const successBatchId = ref("");
const uploadProgress = ref(null);

function createClientBatchId() {
  if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID();
  const bytes = new Uint8Array(16);
  if (globalThis.crypto?.getRandomValues) globalThis.crypto.getRandomValues(bytes);
  else for (let i = 0; i < bytes.length; i++) bytes[i] = Math.floor(Math.random() * 256);
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, b => b.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

// Group & Process selection state
const groups = ref([]);
const selectedGroupId = ref("");

// Column Mapping state
const fileHeaders = ref([]);
const rawRowsData = ref([]);
const isMappingMode = ref(false);
const columnMappings = ref({}); // SystemKey -> FileHeader

// Dust Monitoring Custom Rows
const mappedDustRows = ref([]);

const isDustMode = computed(() => {
  if (!selectedGroupId.value) return false;
  const grp = groups.value.find(g => g.id === Number(selectedGroupId.value));
  return grp && (grp.groupCode === "DUST" || (grp.groupName && grp.groupName.includes("落塵")));
});

const selectedGroup = computed(() =>
  groups.value.find(g => g.id === Number(selectedGroupId.value)) || null
);

const selectedControlScope = computed(() => {
  const group = selectedGroup.value;
  const configuredScope = String(group?.businessScopeCode || "").trim().toUpperCase();
  if (configuredScope) return configuredScope;
  const groupCode = String(group?.groupCode || "").trim().toUpperCase();
  if (groupCode === "CHEM" || groupCode === "CHEM_TREND") return "CHEM";
  if (groupCode === "PROD") return "PRODUCT";
  return "PROCESS";
});

const systemFields = computed(() => [
  { key: "ControlScope", label: "管制類型 (ControlScope)", required: false, altNames: ["管制類型", "類別", "scope", "controlscope", "control_scope", "類型"] },
  { key: "PartNo", label: "產品料號 (PartNo，僅產品管制必填)", required: false, altNames: ["料號", "產品", "part", "partno", "part_no", "product"] },
  { key: "ProcessCode", label: "工站製程代碼 (ProcessCode)", required: selectedControlScope.value !== "CHEM", altNames: ["製程", "工站", "process", "processcode", "process_code", "station"] },
  { key: "CharacteristicCode", label: "檢驗項目代碼／名稱 (CharacteristicCode) *", required: true, altNames: ["管制項目", "項目", "特性", "檢驗項目", "characteristic", "characteristiccode", "char_code", "item"] },
  { key: "Unit", label: "單位（協助對應管制項目）", required: false, altNames: ["單位", "unit", "uom"] },
  { key: "MachineCode", label: "線別／機台代碼或名稱 (MachineCode) *", required: selectedGroup.value?.requiresMachine !== false, altNames: ["線別", "機台", "設備", "machine", "machinecode", "machine_code", "eqp", "device"] },
  { key: "TankCode", label: "槽位 (TankCode)", required: selectedGroup.value?.requiresTank === true || selectedControlScope.value === "CHEM", altNames: ["槽位", "槽體", "tank", "tankcode", "tank_code"] },
  { key: "Specification", label: "規格／目標值 (Specification)", required: false, altNames: ["規格", "規格值", "目標值", "spec", "specification", "target"] },
  { key: "SpecificationRange", label: "規格範圍 (SpecificationRange)", required: false, altNames: ["範圍", "規格範圍", "上下限", "range", "specificationrange", "specification_range"] },
  { key: "MeasuredValue", label: "量測數值 (MeasuredValue) *", required: true, altNames: ["量測值", "測量值", "測定值", "分析值", "檢測值", "數值", "measuredvalue", "measured_value", "value", "val", "值"] },
  { key: "RecheckValue", label: "複驗值 (RecheckValue)", required: false, altNames: ["複驗", "複驗值", "recheck", "recheckvalue", "recheck_value", "review_value"] },
  { key: "AdjustAction", label: "調整 (AdjustAction)", required: false, altNames: ["調整", "調整方式", "調整動作", "adjust", "adjustaction", "adjust_action", "action"] },
  { key: "AdjustAmount", label: "調整量 (AdjustAmount)", required: false, altNames: ["調整量", "添加量", "稀釋量", "adjustamount", "adjust_amount", "amount"] },
  { key: "LotNo", label: "生產批號 (LotNo)", required: false, altNames: ["批號", "lot", "lotno", "lot_no", "batch"] },
  { key: "SerialNo", label: "零件序號 (SerialNo)", required: false, altNames: ["序號", "工單", "serial", "serialno", "serial_no", "sn", "workorder"] },
  { key: "MeasuredAt", label: "量測日期 (MeasuredAt)", required: false, altNames: ["量測日期", "日期", "時間戳記", "measuredat", "measured_at", "date", "timestamp"] },
  { key: "MeasuredTime", label: "量測時間（與日期合併）", required: false, altNames: ["量測時間", "time", "measurementtime"] },
  { key: "Operator", label: "作業人員 (Operator)", required: false, altNames: ["量測員", "作業員", "人員", "operator", "op", "user"] },
  { key: "SampleNo", label: "樣本序號 (SampleNo)", required: false, altNames: ["樣本", "樣本編號", "sampleno", "sample_no", "sample"] }
]);

async function loadGroupsAndProcesses() {
  loading.value = true;
  err.value = "";
  try {
    const res = await api.get("/control-chart-groups");
    groups.value = (res.data || []).filter(g => g.isEnabled !== false);
    
    // Auto-select first group
    if (groups.value.length > 0) {
      selectedGroupId.value = String(groups.value[0].id);
    }
  } catch (e) {
    err.value = "無法載入管制群組清單：" + getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  loadGroupsAndProcesses();
});

function parseDustMatrix(rawRows) {
  if (rawRows.length < 2) {
    throw new Error("Excel 格式不正確：列數太少，無法解析標題與位置/粒徑。");
  }

  // Row 0 is location row
  const locationRow = rawRows[0] || [];
  // Row 1 is header row (particle sizes)
  const headerRow = rawRows[1] || [];

  // Parse Locations with forward fill for merged cells
  const locations = [];
  let lastLocation = "";
  // Columns 1 to 48 represent the locations R1 to R12
  for (let col = 1; col <= 48; col++) {
    const val = String(locationRow[col] || "").trim();
    if (val && val !== "null") {
      lastLocation = val;
    }
    locations[col] = lastLocation;
  }

  const resultRows = [];

  // Parse Data Rows starting from index 2
  for (let r = 2; r < rawRows.length; r++) {
    const row = rawRows[r];
    if (!row || row.length === 0) continue;
    
    const dateTimeStr = String(row[0] || "").trim();
    if (!dateTimeStr || dateTimeStr === "null") continue; // Skip empty dates

    // Iterate columns 1 to 48 (ignore column 49 "備註")
    for (let col = 1; col <= 48; col++) {
      const loc = locations[col];
      const partSize = String(headerRow[col] || "").trim();
      const val = row[col];

      if (!loc || !partSize) continue;
      if (val === "" || val === undefined || val === null) continue; // Skip empty cells

      // Clean characteristic code (e.g. "0.5 um" -> "0.5um")
      const charCode = partSize.toLowerCase().replace(/\s/g, "");

      resultRows.push({
        ControlScope: "PROCESS",
        ProcessCode: loc,   // For dust monitoring, locations R1 to R12 represent "區域 (Processes)"
        MachineCode: loc,   // Keep MachineCode aligned or fallback
        CharacteristicCode: charCode,
        MeasuredValue: String(val).trim(),
        MeasuredAt: dateTimeStr.replace(/\r\n/g, " ").replace(/\n/g, " "), // normalize newlines in timestamps
        LotNo: "落塵監控",
        Operator: "SYSTEM"
      });
    }
  }

  return resultRows;
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
  mappedDustRows.value = [];

  loading.value = true;
  try {
    if (isDustMode.value) {
      // Custom Dust Matrix Parser
      const parsedRows = await new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (evt) => {
          try {
            const data = new Uint8Array(evt.target.result);
            const workbook = XLSX.read(data, { type: "array" });
            if (workbook.SheetNames.length === 0) {
              throw new Error("Excel 檔案中找不到任何工作表。");
            }
            const ws = workbook.Sheets[workbook.SheetNames[0]];
            const raw = XLSX.utils.sheet_to_json(ws, { header: 1 });
            resolve(raw);
          } catch (ex) {
            reject(ex);
          }
        };
        reader.onerror = () => reject(new Error("讀取檔案失敗。"));
        reader.readAsArrayBuffer(file);
      });

      const unpivoted = parseDustMatrix(parsedRows);
      if (unpivoted.length === 0) {
        throw new Error("無塵室落塵交叉表解析結果為空，請檢查表單標頭或數值是否有值。");
      }
      mappedDustRows.value = unpivoted;
      isMappingMode.value = true;
    } else {
      // Standard Column Mapping Parser
      const parsed = await parseUploadSpreadsheet(file);
      fileHeaders.value = parsed.headers;
      rawRowsData.value = parsed.rows;
      runFuzzyAutoMapping();
      isMappingMode.value = true;
    }
  } catch (ex) {
    err.value = ex?.message || String(ex);
    selectedFile.value = null;
  } finally {
    loading.value = false;
  }
}

function runFuzzyAutoMapping() {
  const normalizeHeader = value => String(value || "").toLowerCase().trim().replace(/[\s-_]/g, "");
  const usedHeaders = new Set();
  systemFields.value.forEach(field => {
    const candidates = [field.key, ...field.altNames]
      .map(normalizeHeader)
      .filter(Boolean);
    // Exact names win. This prevents generic aliases such as "值" from stealing
    // columns like 規格值 before MeasuredValue gets a chance to map 量測值.
    let match = fileHeaders.value.find(h =>
      !usedHeaders.has(h) && candidates.includes(normalizeHeader(h)));
    if (!match) {
      const fuzzyCandidates = candidates.filter(x => x.length >= 2 && !["value", "val"].includes(x));
      match = fileHeaders.value.find(h => {
        if (usedHeaders.has(h)) return false;
        const normalized = normalizeHeader(h);
        return fuzzyCandidates.some(candidate =>
          normalized.includes(candidate) || candidate.includes(normalized));
      });
    }
    if (match) {
      columnMappings.value[field.key] = match;
      usedHeaders.add(match);
    } else {
      columnMappings.value[field.key] = "";
    }
  });
}

// Preview first 3 mapped rows (Standard Mode)
const mappedPreviewData = computed(() => {
  return rawRowsData.value.slice(0, 3).map(row => {
    const result = {};
    systemFields.value.forEach(field => {
      const mappedHeader = columnMappings.value[field.key];
      result[field.key] = mappedHeader ? row[mappedHeader] : "";
    });
    return result;
  });
});

function generateDustTemplate() {
  const wb = XLSX.utils.book_new();
  
  // Custom structured matrix data matching exactly the sample file layout (R1 to R12)
  const row0 = ["日期\\時間\\位置"];
  const row1 = [""];
  const row2 = ["2026/7/2\r\n08:11"];
  const row3 = ["2026/7/3\r\n08:15"];
  
  const merges = [
    { s: { r: 0, c: 0 }, e: { r: 1, c: 0 } },  // Date merge A1:A2
    { s: { r: 0, c: 49 }, e: { r: 1, c: 49 } } // Remarks merge AX1:AX2
  ];
  
  for (let r = 1; r <= 12; r++) {
    const loc = `R${r}`;
    row0.push(loc, null, null, null);
    row1.push("0.5 um", 1, 5, 10);
    
    // Merge Location across 4 columns in Row 0
    const startCol = 1 + (r - 1) * 4;
    const endCol = startCol + 3;
    merges.push({ s: { r: 0, c: startCol }, e: { r: 0, c: endCol } });
    
    // Dummy values
    row2.push(r === 4 ? 23 : 0, 0, 0, 0);
    row3.push(0, r === 1 ? 23 : 0, 0, 0);
  }
  
  row0.push("備註");
  row1.push("");
  row2.push("作業前");
  row3.push("作業後");
  
  const data = [row0, row1, row2, row3];
  const ws = XLSX.utils.aoa_to_sheet(data);
  ws["!merges"] = merges;
  
  XLSX.utils.book_append_sheet(wb, ws, "落塵監控表單");
  const wbout = XLSX.write(wb, { bookType: "xlsx", type: "array" });
  
  const blob = new Blob([wbout], { type: "application/octet-stream" });
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.setAttribute("download", "Dust_Monitoring_Template.xlsx");
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

async function downloadTemplate() {
  if (isDustMode.value) {
    generateDustTemplate();
    return;
  }

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
  loading.value = true;
  err.value = "";

  try {
    let payload = [];
    if (isDustMode.value) {
      payload = mappedDustRows.value;
    } else {
      // Validate required columns
      const missingFields = systemFields.value
        .filter(f => f.required && !columnMappings.value[f.key])
        .map(f => f.label);
      
      if (missingFields.length > 0) {
        err.value = "請先對應所有必填欄位：" + missingFields.join(", ");
        loading.value = false;
        return;
      }

      // Convert standard rows
      payload = rawRowsData.value.map(row => {
        const obj = {};
        systemFields.value.forEach(field => {
          const mappedHeader = columnMappings.value[field.key];
          if (mappedHeader && row[mappedHeader] !== undefined && row[mappedHeader] !== null) {
            obj[field.key] = String(row[mappedHeader]).trim();
          } else {
            obj[field.key] = "";
          }
        });
        if (!obj.MachineCode && obj.ProcessCode) {
          obj.MachineCode = `${obj.ProcessCode}-M01`;
        }
        if (obj.MeasuredAt && obj.MeasuredTime) {
          obj.MeasuredAt = `${obj.MeasuredAt} ${obj.MeasuredTime}`.trim();
        }
        delete obj.MeasuredTime;
        obj.ControlScope = selectedControlScope.value;
        return obj;
      });
    }

    const clientBatchId = createClientBatchId();
    uploadProgress.value = { processed: 0, total: payload.length, percent: 0 };
    const progressTimer = window.setInterval(async () => {
      try {
        const { data } = await api.get(`/uploads/${clientBatchId}/progress`);
        const total = Number(data.total || payload.length || 1);
        const processed = Number(data.processed || 0);
        uploadProgress.value = {
          processed,
          total,
          percent: Math.min(100, Math.round(processed * 100 / total))
        };
      } catch {}
    }, 1000);
    let res;
    try {
      res = await api.post("/uploads/variable", payload, { params: { clientBatchId } });
      uploadProgress.value = { processed: payload.length, total: payload.length, percent: 100 };
    } finally {
      window.clearInterval(progressTimer);
    }
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
    uploadProgress.value = null;
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
    <div v-if="uploadProgress" class="fixed inset-0 z-[100] bg-slate-950/60 flex items-center justify-center p-4">
      <div class="w-full max-w-lg rounded-3xl bg-white dark:bg-slate-900 p-7 shadow-2xl">
        <div class="flex justify-between text-sm font-black text-slate-800 dark:text-white mb-3">
          <span>正在驗證匯入資料</span>
          <span>{{ uploadProgress.processed }} / {{ uploadProgress.total }}（{{ uploadProgress.percent }}%）</span>
        </div>
        <div class="h-4 rounded-full bg-slate-200 dark:bg-slate-700 overflow-hidden">
          <div class="h-full bg-blue-600 transition-all duration-300" :style="{ width: `${uploadProgress.percent}%` }"></div>
        </div>
        <p class="mt-3 text-xs text-slate-500">請勿關閉頁面，完成後會自動進入結果頁。</p>
      </div>
    </div>
    <!-- Jumbotron banner -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-blue-600 via-indigo-600 to-indigo-850 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/10 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-cyan-200 border border-white/20">
          <UploadCloud class="w-3.5 h-3.5" /> Stage 1: 計量型量測數據上傳與暫存區
        </div>
        <h1 class="text-3xl font-black tracking-tight">計量型 (Variable) 抽樣檢驗數據上傳</h1>
        <p class="text-indigo-100 text-sm max-w-xl">
          支援廠區自動量測機台匯出之 Excel / CSV 檔案上傳。配備「智慧對照對應器」，非標準表頭欄位亦可輕鬆對應。
        </p>
      </div>
    </div>

    <!-- 管制圖群組選取區 -->
    <div class="p-5 bg-white dark:bg-slate-900 rounded-3xl border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div>
        <label class="block text-xs font-bold text-slate-400 dark:text-slate-500 mb-1.5">🎯 1. 選擇要匯入的管制群組 (Import Group)</label>
        <select v-model="selectedGroupId" class="w-full px-3 py-2 rounded-xl border border-slate-300 dark:border-slate-700 bg-slate-50 dark:bg-slate-800 text-xs font-bold text-slate-800 dark:text-white focus:ring-2 focus:ring-blue-500">
          <option v-for="g in groups" :key="g.id" :value="String(g.id)">{{ g.groupName }} ({{ g.groupCode }})</option>
        </select>
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
          v-if="!isDustMode"
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
        <Download class="w-4 h-4" />
        {{ isDustMode ? '下載落塵監控區域 (R1-R12) Excel 範本' : '下載標準計量型 Excel 範本' }}
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
            <p class="text-sm font-bold text-slate-800 dark:text-white">
              {{ isDustMode ? '點擊選擇落塵監控區域表單或拖曳至此處' : '點擊選擇檔案或將 Excel / CSV 拖曳至此處' }}
            </p>
            <p class="text-xs text-slate-400 mt-1">
              {{ isDustMode ? '系統將自動解析 R1 ~ R12 各區域與粒徑交叉點數據，進行攤平匯入。' : '系統將讀取欄位，引導進行智慧欄位對照（免改 Excel 即可匯入）' }}
            </p>
          </div>
          <div v-if="selectedFile" class="mt-4 px-4 py-2 rounded-xl bg-blue-600/10 text-blue-600 dark:text-blue-400 text-xs font-semibold flex items-center gap-2 border border-blue-500/30">
            <CheckCircle2 class="w-4 h-4 text-blue-500" /> 已選擇檔案：{{ selectedFile.name }} ({{ (selectedFile.size / 1024).toFixed(1) }} KB)
          </div>
        </div>

        <div v-if="!isMappingMode && !isDustMode" class="flex flex-wrap items-center justify-end gap-3">
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
        
        <!-- Case A: Standard Column Mapping -->
        <template v-if="!isDustMode">
          <div class="flex items-center gap-2 border-b border-slate-200 dark:border-slate-800 pb-3">
            <Settings class="w-5 h-5 text-blue-500" />
            <h3 class="text-lg font-black text-slate-800 dark:text-white">智慧欄位對照器 (Excel Mapping Manager)</h3>
          </div>
          
          <p class="text-xs text-slate-400">系統已完成初步模糊配對，請檢查或自訂各 SPC 資料欄位對應的工作表欄位：</p>

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
        </template>

        <!-- Case B: Dust Custom Matrix Parser Preview -->
        <template v-else>
          <div class="flex items-center gap-2 border-b border-slate-200 dark:border-slate-800 pb-3">
            <Settings class="w-5 h-5 text-indigo-500" />
            <h3 class="text-lg font-black text-slate-800 dark:text-white">落塵監控區域交叉表自動解析預覽</h3>
          </div>

          <p class="text-xs text-slate-400">已自動解析 Excel 中各別監控區域（R1 ~ R12）與粒徑交叉欄位。以下為攤平（Unpivoted）後的量測值：</p>

          <div class="space-y-3">
            <h4 class="text-xs font-black text-slate-700 dark:text-slate-300 flex items-center gap-1.5">
              <Table class="w-4 h-4 text-indigo-500" /> 解析結果預覽（共計 {{ mappedDustRows.length }} 筆量測點）
            </h4>
            <div class="overflow-x-auto rounded-2xl border border-slate-200 dark:border-slate-800 max-h-96">
              <table class="w-full text-left text-xs">
                <thead class="bg-slate-50 dark:bg-slate-800/60 text-slate-400 font-bold uppercase sticky top-0">
                  <tr>
                    <th class="p-2.5 font-bold text-[10px]">時間 (MeasuredAt)</th>
                    <th class="p-2.5 font-bold text-[10px]">監控區域 (ProcessCode)</th>
                    <th class="p-2.5 font-bold text-[10px]">量測位置 (MachineCode)</th>
                    <th class="p-2.5 font-bold text-[10px]">粒徑項目 (CharacteristicCode)</th>
                    <th class="p-2.5 font-bold text-[10px]">量測數值 (MeasuredValue)</th>
                  </tr>
                </thead>
                <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-600 dark:text-slate-300 font-medium">
                  <tr v-for="(pRow, pIdx) in mappedDustRows.slice(0, 100)" :key="pIdx" class="hover:bg-slate-50/50 dark:hover:bg-slate-850/50">
                    <td class="p-2.5 font-mono">{{ pRow.MeasuredAt }}</td>
                    <td class="p-2.5 font-mono font-bold text-blue-600 dark:text-blue-400">{{ pRow.ProcessCode }}</td>
                    <td class="p-2.5 font-mono font-semibold text-slate-600 dark:text-slate-400">{{ pRow.MachineCode }}</td>
                    <td class="p-2.5 font-mono font-bold text-amber-600 dark:text-amber-400">{{ pRow.CharacteristicCode }}</td>
                    <td class="p-2.5 font-mono font-black text-emerald-600">{{ pRow.MeasuredValue }}</td>
                  </tr>
                  <tr v-if="mappedDustRows.length > 100">
                    <td colspan="5" class="p-2.5 text-center text-slate-400 font-bold bg-slate-50 dark:bg-slate-850">
                      ... 僅預覽前 100 筆數據 ...
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </div>
        </template>

        <!-- Mapping submit bar -->
        <div class="flex justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
          <button @click="isMappingMode = false; selectedFile = null; mappedDustRows = []" :disabled="loading" class="px-5 py-2.5 rounded-xl border border-slate-300 dark:border-slate-700 text-slate-600 dark:text-slate-300 text-xs font-bold hover:bg-slate-50 dark:hover:bg-slate-850">
            重新選檔
          </button>
          <button @click="uploadMappedData" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-blue-600 hover:bg-blue-500 text-white font-extrabold shadow-lg shadow-blue-500/20 text-xs transition-all">
            <RefreshCw v-if="loading" class="w-4 h-4 animate-spin" />
            <ArrowRight v-else class="w-4 h-4" />
            確認並送出匯入批次
          </button>
        </div>
      </div>
    </div>

    <!-- JSON Mode -->
    <div v-if="mode === 'json' && !isDustMode" class="p-8 rounded-3xl bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 shadow-sm space-y-4">
      <div class="flex items-center justify-between">
        <h3 class="text-sm font-bold text-slate-800 dark:text-white">貼上原始檢驗 JSON 陣列</h3>
        <span class="text-xs text-slate-400 font-mono">支援 ControlScope, PartNo, ProcessCode, MachineCode, CharacteristicCode</span>
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
