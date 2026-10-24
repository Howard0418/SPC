<script setup>
import { useI18n } from "vue-i18n";
import LocalePicker from "../components/LocalePicker.vue";
import { displayUiMessage } from "../i18n/uiMessage";
const { t } = useI18n({ useScope: "global" });
const messageText = value => displayUiMessage(t, value);
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Layers,
  Plus,
  Search,
  Edit,
  Trash2,
  X,
  Save,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  RefreshCw,
  Info,
  Cpu
} from "lucide-vue-next";

// Data Lists
const rows = ref([]); // Processes
const machines = ref([]);

// Core Page State
const err = ref("");
const successMsg = ref("");
const loading = ref(false);
const searchQuery = ref("");
const statusFilter = ref("all");

const selectedProcessId = ref(null);

// Computed values for selected process
const selectedProcess = computed(() => {
  return rows.value.find(p => p.id === selectedProcessId.value) || null;
});

const showMeasurementsModal = ref(false);
const measurementRows = ref([]);
const measurementTotal = ref(0);
const measurementType = ref("variable");
const measurementLoading = ref(false);
const measurementProcess = ref(null);

// Computed values for filtered processes list (left column)
const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const matchQuery = !q || 
      (row.processName && row.processName.toLowerCase().includes(q)) ||
      (row.processNameEn && row.processNameEn.toLowerCase().includes(q)) ||
      (row.description && row.description.toLowerCase().includes(q));
      
    if (statusFilter.value === "active") return matchQuery && row.isEnabled;
    if (statusFilter.value === "inactive") return matchQuery && !row.isEnabled;
    return matchQuery;
  }).sort((a, b) => (a.sequenceNo ?? 0) - (b.sequenceNo ?? 0) || (a.processCode || "").localeCompare(b.processCode || ""));
});

// Computed values for current selected process machines
const currentProcessMachines = computed(() => {
  if (!selectedProcessId.value) return [];
  return machines.value.filter(m => m.processId === selectedProcessId.value);
});

// Data Loading
async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [resProc, resMach] = await Promise.all([
      api.get("/processes"),
      api.get("/machines")
    ]);
    rows.value = resProc.data || [];
    machines.value = resMach.data || [];

    // Auto-select first process if none selected
    if (!selectedProcessId.value && rows.value.length > 0) {
      selectedProcessId.value = rows.value[0].id;
    }
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

// ==========================================
// Process CRUD (Modal & Actions)
// ==========================================
const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const form = ref({
  processCode: "",
  processName: "",
  processNameEn: "",
  sequenceNo: 0,
  description: "",
  isEnabled: true
});
const formErr = ref("");

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    processCode: "",
    processName: "",
    processNameEn: "",
    sequenceNo: 0,
    description: "",
    isEnabled: true
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    processCode: item.processCode || "",
    processName: item.processName || "",
    processNameEn: item.processNameEn || "",
    sequenceNo: item.sequenceNo ?? 0,
    description: item.description || "",
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.processName?.trim()) {
    formErr.value = { key: "spc.processes.required" };
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    if (modalMode.value === "create") {
      const { data } = await api.post("/processes", form.value);
      rows.value.push(data);
      selectedProcessId.value = data.id; // Switch focus to new process
      successAlert({ key: "spc.processes.created", args: { name: String(data.processName) } });
    } else {
      const { data } = await api.put(`/processes/${currentId.value}`, form.value);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert({ key: "spc.processes.updated", args: { name: String(data.processName) } });
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(t("spc.processes.confirmDelete", { name: String(item.processName) }))) return;
  loading.value = true;
  try {
    await api.delete(`/processes/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    if (selectedProcessId.value === item.id) {
      selectedProcessId.value = rows.value.length > 0 ? rows.value[0].id : null;
    }
    successAlert({ key: "spc.processes.deleted", args: { name: String(item.processName) } });
  } catch (e) {
    const status = e?.response?.status;
    const data = e?.response?.data;
    if (status === 409 && data?.relatedItems?.length > 0) {
      err.value = `${data.message}\n${data.relatedItems.map((r, i) => `  ${i + 1}. ${r}`).join('\n')}`;
    } else {
      err.value = getApiErrorMessage(e);
    }
  } finally {
    loading.value = false;
  }
}

async function openMeasurementsModal(type = "variable") {
  if (!selectedProcessId.value) return;
  measurementType.value = type;
  showMeasurementsModal.value = true;
  measurementLoading.value = true;
  measurementRows.value = [];
  measurementTotal.value = 0;
  try {
    const { data } = await api.get(`/processes/${selectedProcessId.value}/measurements`, {
      params: { type, take: 500 }
    });
    measurementProcess.value = data.process;
    measurementRows.value = data.rows || [];
    measurementTotal.value = data.total || 0;
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    measurementLoading.value = false;
  }
}

// ==========================================
// Machine Configuration CRUD
// ==========================================
const showMachineModal = ref(false);
const machineModalMode = ref("create");
const machineCurrentId = ref(null);
const machineForm = ref({
  machineCode: "",
  machineName: "",
  location: "",
  status: "IDLE",
  isEnabled: true,
  tanks: []
});
const machineFormErr = ref("");

function emptyMachineForm() {
  return {
    machineCode: "",
    machineName: "",
    location: "",
    status: "IDLE",
    isEnabled: true,
    tanks: []
  };
}

function openCreateMachineModal() {
  machineModalMode.value = "create";
  machineCurrentId.value = null;
  machineForm.value = emptyMachineForm();
  machineFormErr.value = "";
  showMachineModal.value = true;
}

async function openEditMachineModal(item) {
  machineModalMode.value = "edit";
  machineCurrentId.value = item.id;
  machineForm.value = {
    machineCode: item.machineCode || "",
    machineName: item.machineName || "",
    location: item.location || "",
    status: item.status || "IDLE",
    isEnabled: item.isEnabled ?? true,
    tanks: []
  };
  machineFormErr.value = "";
  showMachineModal.value = true;
  try {
    const { data } = await api.get(`/machines/${item.id}/tanks`);
    const prefix = `${machineForm.value.machineCode}-`;
    machineForm.value.tanks = (data || []).map(t => ({
      id: t.id,
      tankCode: (t.tankCode || "").startsWith(prefix) ? t.tankCode.slice(prefix.length) : (t.tankCode || ""),
      tankName: t.tankName || "",
      tankNameEn: t.tankNameEn || "",
      sequenceNo: t.sequenceNo ?? 0,
      isActive: t.isActive ?? true
    }));
  } catch (e) {
    machineFormErr.value = getApiErrorMessage(e);
  }
}

function addMachineTank() {
  machineForm.value.tanks.push({
    id: null,
    tankCode: "",
    tankName: "",
    tankNameEn: "",
    sequenceNo: 0,
    isActive: true
  });
}

function removeMachineTank(index) {
  machineForm.value.tanks.splice(index, 1);
}

async function saveMachine() {
  if (!machineForm.value.machineCode?.trim() || !machineForm.value.machineName?.trim()) {
    machineFormErr.value = { key: "spc.processes.machineRequired" };
    return;
  }
  const hasInvalidTank = machineForm.value.tanks.some(t => !t.tankName?.trim());
  if (hasInvalidTank) {
    machineFormErr.value = { key: "spc.processes.tankRequired" };
    return;
  }
  machineFormErr.value = "";
  loading.value = true;

  try {
    const payload = {
      ...machineForm.value,
      tanks: machineForm.value.tanks.map(t => ({
        ...t,
        tankName: t.tankName?.trim(),
        tankNameEn: t.tankNameEn?.trim() || null,
        sequenceNo: Number(t.sequenceNo) || 0,
        isActive: t.isActive ?? true
      })),
      processId: selectedProcessId.value
    };
    if (machineModalMode.value === "create") {
      const { data } = await api.post("/machines", payload);
      machines.value.push(data);
      successAlert({ key: "spc.processes.machineCreated", args: { name: String(data.machineName) } });
    } else {
      const { data } = await api.put(`/machines/${machineCurrentId.value}`, payload);
      const idx = machines.value.findIndex(x => x.id === machineCurrentId.value);
      if (idx !== -1) machines.value[idx] = data;
      successAlert({ key: "spc.processes.machineUpdated", args: { name: String(data.machineName) } });
    }
    showMachineModal.value = false;
  } catch (e) {
    machineFormErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDeleteMachine(item) {
  if (!confirm(t("spc.processes.confirmDeleteMachine", { code: String(item.machineCode), name: String(item.machineName) }))) return;
  loading.value = true;
  try {
    await api.delete(`/machines/${item.id}`);
    machines.value = machines.value.filter(x => x.id !== item.id);
    successAlert({ key: "spc.processes.machineDeleted", args: { name: String(item.machineName) } });
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

// Helpers
function successAlert(msg) {
  successMsg.value = msg;
  setTimeout(() => { successMsg.value = ""; }, 3000);
}

onMounted(load);
</script>

<template>
  <section class="space-y-6">
    <!-- Title & Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex flex-wrap items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-blue-600 to-cyan-500 rounded-xl shadow-lg shadow-blue-500/30 text-white">
          <Layers class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">{{ t('spc.processes.title') }}</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ t('spc.processes.subtitle') }}</p>
        </div>
      </div>
      
      <div class="flex flex-wrap items-center gap-3">
        <button
          @click="load"
          type="button"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-sm font-semibold transition-all border border-slate-200 dark:border-slate-700"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> {{ t('common.actions.refresh') }}
        </button>
        <button
          data-testid="process-add" @click="openCreateModal"
          type="button"
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> {{ t('spc.processes.add') }}
        </button>
      </div>
    </div>

    <!-- Wizard Tip -->
    <div class="p-5 bg-gradient-to-r from-cyan-50 to-blue-50 dark:from-cyan-950/30 dark:to-blue-900/20 border border-cyan-100 dark:border-cyan-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-cyan-100 dark:bg-cyan-900/50 rounded-xl text-cyan-600 dark:text-cyan-400 mt-0.5">
        <Info class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-cyan-900 dark:text-cyan-300">{{ t('spc.processes.guideTitle') }}</h4>
        <p class="text-xs text-cyan-700 dark:text-cyan-400/80 mt-1.5 leading-relaxed">
          💡 <strong>{{ t('spc.processes.guideFlow') }}</strong><br/>
          {{ t('spc.processes.guideFirst') }}<br/>
          {{ t('spc.processes.guideSecondBefore') }}<strong>{{ t('spc.processes.guideSecondLabel') }}</strong>{{ t('spc.processes.guideSecondAfter') }}<br/>
          {{ t('spc.processes.guideThird') }}
        </p>
      </div>
    </div>

    <!-- Error/Success Alerts -->
    <div v-if="err" class="flex items-start gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm animate-fade-in">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500 mt-0.5" />
      <div class="text-sm font-semibold whitespace-pre-line">{{ messageText(err) }}</div>
    </div>

    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm animate-fade-in">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ messageText(successMsg) }}</div>
    </div>

    <!-- Main Content Split Layout -->
    <div class="grid grid-cols-1 xl:grid-cols-5 gap-6 items-start">
      <!-- Left Column: Process List (xl:col-span-2) -->
      <div class="xl:col-span-2 space-y-4">
        <!-- Search & Filter bar -->
        <div class="flex flex-col gap-3 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
          <div class="relative w-full">
            <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
              <Search class="w-4 h-4" />
            </span>
            <input
              v-model="searchQuery"
              type="text"
              :placeholder="t('spc.processes.search')"
              class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-xs text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all"
            />
          </div>

          <div class="flex items-center gap-1.5 p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80 justify-center">
            <button
              v-for="f in [{id:'all', label:t('spc.processes.all')}, {id:'active', label:t('common.status.active')}, {id:'inactive', label:t('common.status.inactive')}]"
              :key="f.id"
              @click="statusFilter = f.id"
              type="button"
              :class="[
                'px-3 py-1.5 rounded-lg text-[10px] font-bold transition-all whitespace-nowrap flex-1 text-center',
                statusFilter === f.id
                  ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm'
                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
              ]"
            >
              {{ f.label }}
            </button>
          </div>
        </div>

        <!-- Processes List Table Card -->
        <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
          <div class="overflow-x-auto max-h-[600px]">
            <table class="w-full text-left border-collapse">
              <thead>
                <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase border-b border-slate-200 dark:border-slate-700">
                  <th class="py-3 px-4">{{ t('spc.processes.nameColumn') }}</th>
                  <th class="py-3 px-3 text-center w-20">{{ t('common.columns.status') }}</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-xs font-semibold text-slate-700 dark:text-slate-300">
                <tr v-if="loading && rows.length === 0">
                  <td colspan="2" class="py-8 text-center text-slate-400">{{ t('spc.processes.loading') }}</td>
                </tr>
                <tr v-else-if="filteredRows.length === 0">
                  <td colspan="2" class="py-8 text-center text-slate-400">{{ t('spc.processes.empty') }}</td>
                </tr>
                <tr
                  v-else
                  v-for="item in filteredRows"
                  :key="item.id"
                  @click="selectedProcessId = item.id"
                  :class="[
                    'hover:bg-blue-50/30 dark:hover:bg-slate-800/40 cursor-pointer transition-all border-l-4',
                    selectedProcessId === item.id
                      ? 'bg-blue-50/70 dark:bg-blue-950/20 border-blue-500 text-blue-600 dark:text-blue-400'
                      : 'border-transparent'
                  ]"
                >
                  <td class="py-3.5 px-4">
                    <div class="font-bold flex items-center gap-1.5 text-sm">
                      <Layers class="w-3.5 h-3.5" :class="selectedProcessId === item.id ? 'text-blue-500' : 'text-slate-400'" />
                      <span>{{ item.processName }}</span>
                    </div>
                    <div v-if="item.processNameEn" class="text-[10px] text-cyan-600 dark:text-cyan-400 font-normal mt-0.5">{{ item.processNameEn }}</div>
                  </td>
                  <td class="py-3.5 px-3 text-center">
                    <span
                      :class="[
                        'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[9px] font-bold border',
                        item.isEnabled
                          ? 'bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 border-emerald-200 dark:border-emerald-900'
                          : 'bg-slate-100 dark:bg-slate-800 text-slate-500 border-slate-200 dark:border-slate-700'
                      ]"
                    >
                      <span :class="['w-1 h-1 rounded-full', item.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                      {{ item.isEnabled ? t('common.status.enabled') : t('common.status.inactive') }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div class="px-4 py-3 bg-slate-50 dark:bg-slate-800/30 border-t border-slate-200 dark:border-slate-800 text-[10px] text-slate-400 font-bold flex justify-between">
            <span>{{ t('spc.processes.shown', { start: String(filteredRows.length ? 1 : 0), count: String(filteredRows.length) }) }}</span>
            <span>{{ t('spc.processes.total', { count: String(rows.length) }) }}</span>
          </div>
        </div>
      </div>

      <!-- Right Column: Configuration Hub (xl:col-span-3) -->
      <div class="xl:col-span-3 bg-white dark:bg-slate-900 rounded-2xl border border-slate-200 dark:border-slate-800 shadow-sm overflow-hidden min-h-[550px]">
        <div v-if="!selectedProcess" class="flex flex-col items-center justify-center p-12 text-center h-full min-h-[550px] text-slate-400 dark:text-slate-500">
          <Layers class="w-16 h-16 text-slate-300 dark:text-slate-700 stroke-1 mb-4" />
          <h3 class="text-sm font-bold text-slate-600 dark:text-slate-400">{{ t('spc.processes.noneSelected') }}</h3>
          <p class="text-xs text-slate-400 mt-1 max-w-xs leading-relaxed">{{ t('spc.processes.selectHelp') }}</p>
        </div>

        <div v-else class="flex flex-col h-full min-h-[550px]">
          <!-- Process Info Header -->
          <div class="p-6 bg-slate-50/50 dark:bg-slate-800/30 border-b border-slate-200 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <div class="flex items-center gap-2">
                <span class="text-[10px] font-mono font-bold bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 px-2 py-0.5 rounded border border-blue-200 dark:border-blue-800">{{ t('spc.processes.station') }}</span>
                <h2 class="text-base font-black text-slate-800 dark:text-white flex items-center gap-1.5">
                  {{ selectedProcess.processName }}{{ selectedProcess.processNameEn ? ` / ${selectedProcess.processNameEn}` : '' }}
                </h2>
              </div>
              <p class="text-xs text-slate-500 dark:text-slate-400 mt-1 leading-relaxed">{{ selectedProcess.description || t('spc.processes.noDescription') }}</p>
            </div>
            <div class="flex items-center gap-2 self-end sm:self-auto shrink-0">
              <button data-testid="measurements-open" @click="openMeasurementsModal('variable')" type="button" class="inline-flex items-center justify-center px-3 py-1.5 rounded-xl bg-indigo-50 dark:bg-slate-800 hover:bg-indigo-100 dark:hover:bg-indigo-950 text-indigo-600 dark:text-indigo-400 transition-all border border-indigo-200 dark:border-slate-700 text-xs font-bold" :title="t('spc.processes.viewRecordsTitle')">
                <Info class="w-3.5 h-3.5 mr-1" /> {{ t('spc.processes.viewRecords') }}
              </button>
              <button data-testid="process-edit" @click="openEditModal(selectedProcess)" type="button" class="inline-flex items-center justify-center px-3 py-1.5 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-950 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700 text-xs font-bold" :title="t('spc.processes.edit')">
                <Edit class="w-3.5 h-3.5 mr-1" /> {{ t('spc.processes.edit') }}
              </button>
              <button @click="confirmDelete(selectedProcess)" type="button" class="inline-flex items-center justify-center px-3 py-1.5 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700 text-xs font-bold" :title="t('spc.processes.delete')">
                <Trash2 class="w-3.5 h-3.5 mr-1" /> {{ t('spc.processes.delete') }}
              </button>
            </div>
          </div>

          <!-- Machines Header -->
          <div class="flex border-b border-slate-200 dark:border-slate-800 bg-slate-50/20 px-6 py-3 shrink-0">
            <div class="flex items-center gap-1.5 text-xs font-bold text-blue-600 dark:text-blue-400">
              {{ t('spc.processes.configure') }}
              <span class="px-1.5 py-0.2 rounded-full text-[9px] bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border border-slate-200 dark:border-slate-700 font-mono">{{ currentProcessMachines.length }}</span>
            </div>
          </div>

          <!-- Tab Content Area -->
          <div class="p-6 flex-1 overflow-y-auto space-y-6">
            
            <!-- ====== PRODUCTION MACHINES TAB ====== -->
            <div class="space-y-4">
              <div class="flex justify-between items-center">
                <div class="text-xs text-slate-500 dark:text-slate-400 font-bold">
                  {{ t('spc.processes.machineList') }}
                </div>
                <button
                  data-testid="machine-add" @click="openCreateMachineModal"
                  type="button"
                  class="flex items-center gap-1 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold shadow-md shadow-blue-500/20 transition-all"
                >
                  <Plus class="w-3 h-3" /> {{ t('spc.processes.configure') }}
                </button>
              </div>

              <!-- Machines Grid -->
              <div v-if="currentProcessMachines.length === 0" class="flex flex-col items-center justify-center p-8 border border-dashed border-slate-200 dark:border-slate-700 rounded-2xl text-slate-400 text-center">
                <Cpu class="w-8 h-8 text-slate-300 dark:text-slate-700 stroke-1 mb-2" />
                <span class="text-xs">{{ t('spc.processes.noMachines') }}</span>
              </div>
              
              <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div
                  v-for="mach in currentProcessMachines"
                  :key="mach.id"
                  class="p-4 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-200 dark:border-slate-700/80 flex flex-col justify-between hover:shadow-xs transition-all group relative"
                >
                  <div class="flex justify-between items-start">
                    <div class="flex gap-2">
                      <div class="p-2 bg-blue-100/60 dark:bg-blue-900/30 rounded-lg text-blue-600 dark:text-blue-400 flex items-center justify-center h-9 w-9 shrink-0 animate-fade-in">
                        <Cpu class="w-5 h-5" />
                      </div>
                      <div>
                        <h4 class="text-xs font-bold text-slate-800 dark:text-white">{{ mach.machineCode }}</h4>
                        <p class="text-[10px] text-slate-400 font-normal mt-0.5">{{ mach.machineName }}</p>
                      </div>
                    </div>
                    <!-- Actions -->
                    <div class="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                      <button data-testid="machine-edit" @click="openEditMachineModal(mach)" type="button" class="p-1 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-blue-600" :title="t('spc.processes.editMachine')"><Edit class="w-3.5 h-3.5" /></button>
                      <button @click="confirmDeleteMachine(mach)" type="button" class="p-1 rounded hover:bg-slate-200 dark:hover:bg-slate-700 text-red-600" :title="t('spc.processes.deleteMachine')"><Trash2 class="w-3.5 h-3.5" /></button>
                    </div>
                  </div>
                  
                  <div class="mt-4 pt-3 border-t border-slate-200/50 dark:border-slate-700/50 flex justify-between items-center text-[10px] font-bold text-slate-500 dark:text-slate-400">
                    <div>
                      <span>{{ t('spc.processes.locationPrefix') }} </span>
                      <span class="font-mono text-slate-700 dark:text-slate-300">{{ mach.location || t('spc.processes.unset') }}</span>
                    </div>
                    <div class="flex items-center gap-1.5">
                      <span :class="[
                        'px-1.5 py-0.2 rounded text-[9px] uppercase font-bold border',
                        mach.status === 'RUNNING' ? 'bg-emerald-50 dark:bg-emerald-950/40 text-emerald-600 border-emerald-200 dark:border-emerald-900' :
                        mach.status === 'DOWN' ? 'bg-red-50 dark:bg-red-950/40 text-red-600 border-red-200 dark:border-red-900' :
                        mach.status === 'MAINTENANCE' ? 'bg-amber-50 dark:bg-amber-950/40 text-amber-600 border-amber-200 dark:border-amber-900' :
                        'bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-300 border-slate-200'
                      ]">
                        {{ mach.status }}
                      </span>
                      <span class="h-1.5 w-1.5 rounded-full" :class="mach.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400'"></span>
                    </div>
                  </div>
                </div>
              </div>
            </div>

          </div>
        </div>
      </div>
    </div>

    <!-- ====================================================================== -->
    <!-- SLIDE OVER PANEL: CREATE/EDIT PROCESS -->
    <!-- ====================================================================== -->
    <transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 translate-x-full"
      enter-to-class="opacity-100 translate-x-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-x-0"
      leave-to-class="opacity-0 translate-x-full"
    >
      <div v-if="showModal" class="fixed inset-0 z-50 flex justify-end bg-slate-900/60 backdrop-blur-sm">
        <div class="absolute inset-0 cursor-pointer" @click="showModal = false"></div>
        <div role="dialog" class="relative w-full max-w-md h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
            <div class="flex flex-wrap items-center gap-3">
              <div class="p-2.5 bg-cyan-600 text-white rounded-xl shadow-md shadow-cyan-500/20">
                <Layers class="w-5 h-5" />
              </div>
              <h3 class="text-lg font-black text-slate-800 dark:text-white">
                {{ modalMode === 'create' ? t('spc.processes.createTitle') : t('spc.processes.editTitle') }}
              </h3>
            </div>
            <button
              :aria-label="t('common.actions.close')" @click="showModal = false"
              type="button"
              class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-all"
            >
              <X class="w-5 h-5" />
            </button>
          </div>

          <div class="px-6 py-2 shrink-0"><LocalePicker /></div>
        <form @submit.prevent="save" class="flex flex-col h-full overflow-hidden">
            <div class="flex-1 overflow-y-auto p-6 space-y-5">
              <div v-if="formErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
                <XCircle class="w-4 h-4 flex-shrink-0" /> {{ messageText(formErr) }}
              </div>

              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.processName') }} <span class="text-red-500">*</span></label>
                  <input
                    data-testid="form-processName" v-model="form.processName"
                    type="text"
                    required
                    :placeholder="t('spc.processes.nameExample')"
                    class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
                  />
                </div>
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.processNameEn') }}</label>
                  <input data-testid="form-processNameEn" v-model="form.processNameEn" type="text" :placeholder="t('spc.processes.nameEnExample')" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all" />
                </div>
              </div>

              <div class="space-y-1.5">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.sequence') }}</label>
                <input data-testid="form-sequenceNo" v-model.number="form.sequenceNo" type="number" min="0" step="1" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>

              <div class="space-y-1.5">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.description') }}</label>
                <textarea
                  data-testid="form-description" v-model="form.description"
                  rows="3"
                  :placeholder="t('spc.processes.descriptionExample')"
                  class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all resize-none"
                ></textarea>
              </div>

              <div class="space-y-1.5 pt-2">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">{{ t('spc.processes.enabled') }}</label>
                <label class="relative inline-flex items-center cursor-pointer">
                  <input data-testid="form-isEnabled" v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                  <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-cyan-600"></div>
                  <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? t('common.status.on') : t('common.status.off') }}</span>
                </label>
              </div>
            </div>

            <div class="shrink-0 p-6 bg-slate-50 dark:bg-slate-800/80 border-t border-slate-200 dark:border-slate-800 flex items-center justify-end gap-3">
              <button
                @click="showModal = false"
                type="button"
                class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all"
              >
                {{ t('common.actions.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="loading"
                class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 text-white text-sm font-bold shadow-lg shadow-cyan-500/25 hover:shadow-xl hover:shadow-cyan-500/40 transition-all transform hover:-translate-y-0.5"
              >
                <Save class="w-4 h-4" /> {{ t('common.actions.save') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </transition>

    <!-- ====================================================================== -->
    <!-- SLIDE OVER PANEL: CREATE/EDIT MACHINE -->
    <!-- ====================================================================== -->
    <transition
      enter-active-class="transition-all duration-300 ease-out"
      enter-from-class="opacity-0 translate-x-full"
      enter-to-class="opacity-100 translate-x-0"
      leave-active-class="transition-all duration-200 ease-in"
      leave-from-class="opacity-100 translate-x-0"
      leave-to-class="opacity-0 translate-x-full"
    >
      <div v-if="showMachineModal" class="fixed inset-0 z-50 flex justify-end bg-slate-900/60 backdrop-blur-sm">
        <div class="absolute inset-0 cursor-pointer" @click="showMachineModal = false"></div>
        <div role="dialog" class="relative w-full max-w-md h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
            <div class="flex flex-wrap items-center gap-3">
              <div class="p-2.5 bg-blue-600 text-white rounded-xl shadow-md shadow-blue-500/20">
                <Cpu class="w-5 h-5" />
              </div>
              <h3 class="text-lg font-black text-slate-800 dark:text-white">
                {{ machineModalMode === 'create' ? t('spc.processes.configure') : t('spc.processes.editMachineTitle') }}
              </h3>
            </div>
            <button
              :aria-label="t('common.actions.close')" @click="showMachineModal = false"
              type="button"
              class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-all"
            >
              <X class="w-5 h-5" />
            </button>
          </div>

          <div class="px-6 py-2 shrink-0"><LocalePicker /></div>
        <form @submit.prevent="saveMachine" class="flex flex-col h-full overflow-hidden">
            <div class="flex-1 overflow-y-auto p-6 space-y-5">
              <div v-if="machineFormErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
                <XCircle class="w-4 h-4 flex-shrink-0" /> {{ messageText(machineFormErr) }}
              </div>

              <div class="space-y-1.5">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.parentProcess') }}</label>
                <div class="px-4 py-2.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm font-bold text-slate-500">
                  {{ selectedProcess?.processName }}{{ selectedProcess?.processNameEn ? ` / ${selectedProcess.processNameEn}` : '' }}
                </div>
              </div>

              <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.machineCode') }} <span class="text-red-500">*</span></label>
                  <input
                    data-testid="machineForm-machineCode" v-model="machineForm.machineCode"
                    type="text"
                    required
                    :placeholder="t('spc.processes.machineCodeExample')"
                    class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
                  />
                </div>
                
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.machineName') }} <span class="text-red-500">*</span></label>
                  <input
                    data-testid="machineForm-machineName" v-model="machineForm.machineName"
                    type="text"
                    required
                    :placeholder="t('spc.processes.machineNameExample')"
                    class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
                  />
                </div>
              </div>

              <div class="space-y-1.5">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.location') }}</label>
                <input
                  data-testid="machineForm-location" v-model="machineForm.location"
                  type="text"
                  :placeholder="t('spc.processes.locationExample')"
                  class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
                />
              </div>

              <div class="space-y-1.5">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.machineStatus') }}</label>
                <select
                  data-testid="machineForm-status" v-model="machineForm.status"
                  class="w-full px-3 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                >
                  <option value="RUNNING">{{ t('spc.processes.running') }}</option>
                  <option value="IDLE">{{ t('spc.processes.idle') }}</option>
                  <option value="DOWN">{{ t('spc.processes.down') }}</option>
                  <option value="MAINTENANCE">{{ t('spc.processes.maintenance') }}</option>
                </select>
              </div>

              <div class="space-y-3 p-4 rounded-2xl border border-blue-100 dark:border-blue-900/50 bg-blue-50/50 dark:bg-blue-950/10">
                <div class="flex items-center justify-between gap-3">
                  <div>
                    <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.processes.tanks') }}</label>
                    <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">{{ t('spc.processes.tankMatchHelp') }}</p>
                  </div>
                  <button
                    data-testid="machine-tank-add" @click="addMachineTank"
                    type="button"
                    class="shrink-0 inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold transition-colors"
                  >
                    <Plus class="w-3.5 h-3.5" /> {{ t('spc.processes.addTank') }}
                  </button>
                </div>

                <div v-if="machineForm.tanks.length === 0" class="px-3 py-3 rounded-xl border border-dashed border-blue-200 dark:border-blue-800 text-xs text-slate-500 dark:text-slate-400">
                  {{ t('spc.processes.tankEmptyHelp') }}
                </div>
                <div v-else class="space-y-2">
                  <div
                    v-for="(tank, index) in machineForm.tanks"
                    :key="tank.id || index"
                    class="grid grid-cols-1 md:grid-cols-[1fr_1.3fr_1.3fr_6rem_auto_auto] gap-2 items-center"
                  >
                    <input
                      :value="tank.id ? tank.tankCode : t('spc.processes.autoCode')"
                      type="text"
                      readonly
                      class="w-full px-3 py-2 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono text-sm text-slate-500 dark:text-slate-400"
                    />
                    <input
                      data-testid="tank-tankName" v-model="tank.tankName"
                      type="text"
                      required
                      :placeholder="t('spc.processes.tankNameExample')"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                    />
                    <input
                      data-testid="tank-tankNameEn" v-model="tank.tankNameEn"
                      type="text"
                      :placeholder="t('spc.processes.tankEnExample')"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                    />
                    <input
                      data-testid="tank-sequenceNo" v-model.number="tank.sequenceNo"
                      type="number"
                      min="0"
                      step="1"
                      :placeholder="t('spc.processes.order')"
                      class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
                    />
                    <label class="inline-flex items-center gap-2 text-xs font-bold text-slate-600 dark:text-slate-300">
                      <input data-testid="tank-isActive" v-model="tank.isActive" type="checkbox" class="rounded border-slate-300 text-blue-600 focus:ring-blue-500" />
                      {{ t('common.status.on') }}
                    </label>
                    <button
                      @click="removeMachineTank(index)"
                      type="button"
                      class="inline-flex items-center justify-center w-9 h-9 rounded-lg text-red-600 hover:bg-red-50 dark:hover:bg-red-950/30 transition-colors"
                      :title="t('spc.processes.removeTank')"
                    >
                      <Trash2 class="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </div>

              <div class="space-y-1.5 pt-2">
                <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">{{ t('spc.processes.enabled') }}</label>
                <label class="relative inline-flex items-center cursor-pointer">
                  <input data-testid="machineForm-isEnabled" v-model="machineForm.isEnabled" type="checkbox" class="sr-only peer" />
                  <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                  <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ machineForm.isEnabled ? t('common.status.on') : t('common.status.off') }}</span>
                </label>
              </div>
            </div>

            <div class="shrink-0 p-6 bg-slate-50 dark:bg-slate-800/80 border-t border-slate-200 dark:border-slate-800 flex items-center justify-end gap-3">
              <button
                @click="showMachineModal = false"
                type="button"
                class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all"
              >
                {{ t('common.actions.cancel') }}
              </button>
              <button
                type="submit"
                :disabled="loading"
                class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-500 hover:to-cyan-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 transition-all transform hover:-translate-y-0.5"
              >
                <Save class="w-4 h-4" /> {{ t('common.actions.save') }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </transition>

    <!-- Measurement Records Modal -->
    <transition enter-active-class="transition-all duration-200 ease-out" enter-from-class="opacity-0" enter-to-class="opacity-100" leave-active-class="transition-all duration-150 ease-in" leave-from-class="opacity-100" leave-to-class="opacity-0">
      <div v-if="showMeasurementsModal" class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/60 backdrop-blur-sm p-4">
        <div role="dialog" class="w-full max-w-6xl max-h-[85vh] bg-white dark:bg-slate-900 rounded-2xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden flex flex-col">
          <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700 flex items-center justify-between">
            <div>
              <h3 class="text-base font-black text-slate-800 dark:text-white">{{ t('spc.processes.recordsTitle') }}</h3>
              <p class="text-xs text-slate-500 dark:text-slate-400 mt-1">
                {{ t('spc.processes.measurementCount', { name: String(measurementProcess?.processName || selectedProcess?.processName || ''), count: String(measurementTotal) }) }}
              </p>
            </div>
            <div class="flex items-center gap-2">
              <button type="button" data-testid="measurements-open" @click="openMeasurementsModal('variable')" :class="['px-3 py-1.5 rounded-xl text-xs font-bold border', measurementType === 'variable' ? 'bg-indigo-600 text-white border-indigo-600' : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700']">{{ t('spc.processes.variable') }}</button>
              <button type="button" @click="openMeasurementsModal('attribute')" :class="['px-3 py-1.5 rounded-xl text-xs font-bold border', measurementType === 'attribute' ? 'bg-teal-600 text-white border-teal-600' : 'bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-300 border-slate-200 dark:border-slate-700']">{{ t('spc.processes.attribute') }}</button>
              <button type="button" :aria-label="t('common.actions.close')" @click="showMeasurementsModal = false" class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400">
                <X class="w-5 h-5" />
              </button>
            </div>
          </div>

          <div class="px-6 py-2"><LocalePicker /></div>
          <div class="overflow-auto">
            <table class="w-full text-left border-collapse text-xs">
              <thead class="sticky top-0 bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 font-black uppercase">
                <tr>
                  <th class="px-4 py-3">ID</th>
                  <th class="px-4 py-3">{{ t('spc.processes.measuredAt') }}</th>
                  <th class="px-4 py-3">PPC</th>
                  <th class="px-4 py-3">{{ t('spc.processes.characteristicId') }}</th>
                  <th class="px-4 py-3">{{ t('spc.processes.lotSerial') }}</th>
                  <th class="px-4 py-3">{{ t('spc.processes.sample') }}</th>
                  <th class="px-4 py-3">{{ t('spc.processes.value') }}</th>
                  <th class="px-4 py-3">{{ t('spc.processes.operator') }}</th>
                  <th class="px-4 py-3">UploadBatch</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-slate-100 dark:divide-slate-800 text-slate-700 dark:text-slate-300">
                <tr v-if="measurementLoading">
                  <td colspan="9" class="px-4 py-10 text-center text-slate-400">{{ t('spc.processes.recordsLoading') }}</td>
                </tr>
                <tr v-else-if="measurementRows.length === 0">
                  <td colspan="9" class="px-4 py-10 text-center text-slate-400">{{ t('spc.processes.recordsEmpty') }}</td>
                </tr>
                <tr v-for="m in measurementRows" :key="`${m.dataType}-${m.id}`" class="hover:bg-slate-50 dark:hover:bg-slate-800/50">
                  <td class="px-4 py-2 font-mono">#{{ m.id }}</td>
                  <td class="px-4 py-2 font-mono">{{ m.measuredAt ? new Date(m.measuredAt).toLocaleString('zh-TW', { hour12: false }) : '-' }}</td>
                  <td class="px-4 py-2 font-mono">{{ m.partProcessCharacteristicId }}</td>
                  <td class="px-4 py-2 font-mono">{{ m.characteristicId }}</td>
                  <td class="px-4 py-2">
                    <div class="font-bold">{{ m.lotNo || '-' }}</div>
                    <div class="text-[10px] text-slate-400">{{ m.serialNo || '-' }}</div>
                  </td>
                  <td class="px-4 py-2 font-mono">{{ m.sampleNo }}</td>
                  <td class="px-4 py-2 font-mono font-bold">
                    <span v-if="measurementType === 'variable'">{{ m.value }}</span>
                    <span v-else>{{ t('spc.processes.attributeValues', { inspected: String(m.inspectedQty ?? '-'), defects: String(m.defectQty ?? '-'), count: String(m.defectCount ?? '-') }) }}</span>
                  </td>
                  <td class="px-4 py-2">{{ m.operator || '-' }}</td>
                  <td class="px-4 py-2 font-mono text-[10px]">{{ m.uploadBatchId }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </transition>

  </section>
</template>
