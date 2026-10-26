<script setup>
import {useI18n} from "vue-i18n";
import {displayUiMessage} from "../i18n/uiMessage";
const {t}=useI18n({useScope:"global"});
import LocalePicker from "../components/LocalePicker.vue";
import ModuleGuide from "../components/ModuleGuide.vue";
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorDisplay } from "../api/client";
import {
  Users,
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
  Mail,
  Building2,
  UserCheck,
  Info
} from "lucide-vue-next";

const rows = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);
const comparison = ref(null);
const comparing = ref(false);
const selectedPortalIds = ref([]);
const permissionCatalog = ref([]);

const searchQuery = ref("");
const statusFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentId = ref(null);
const form = ref({
  operatorCode: "",
  operatorName: "",
  department: "",
  email: "",
  username: "",
  role: "Viewer",
  isActive: true
  ,pagePermissions: []
});
const formErr = ref("");

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const [res, catalog] = await Promise.all([api.get("/operators"), api.get("/operators/permissions-catalog")]);
    rows.value = res.data || [];
    permissionCatalog.value = catalog.data || [];
  } catch (e) {
    err.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

async function comparePortalUsers() {
  err.value = "";
  comparing.value = true;
  try {
    const { data } = await api.get("/v1/operators/directory-comparison");
    comparison.value = data;
    selectedPortalIds.value = [];
  } catch (e) {
    err.value = getApiErrorDisplay(e);
  } finally {
    comparing.value = false;
  }
}

async function syncSelected() {
  if (!selectedPortalIds.value.length) return;
  if (!confirm(t('spc.settings.confirmSync',{count:selectedPortalIds.value.length}))) return;
  comparing.value = true;
  err.value = "";
  try {
    const { data } = await api.post("/v1/operators/directory-comparison/sync", {
      items: selectedPortalIds.value.map(portalUserId => ({ portalUserId }))
    });
    successAlert({key:"spc.settings.syncComplete",args:{count:data.succeeded||0}});
    await load();
    await comparePortalUsers();
  } catch (e) {
    err.value = getApiErrorDisplay(e);
  } finally {
    comparing.value = false;
  }
}

const comparisonLabel = status => ({
  Matched: t('spc.settings.matched'), Different: t('spc.settings.different'), PortalOnly: t('spc.settings.portalOnly'), SpcOnly: t('spc.settings.spcOnly'), Conflict: t('spc.settings.conflict')
}[status] || status);

const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const matchQuery = !q || 
      (row.operatorCode && row.operatorCode.toLowerCase().includes(q)) ||
      (row.operatorName && row.operatorName.toLowerCase().includes(q)) ||
      (row.department && row.department.toLowerCase().includes(q)) ||
      (row.email && row.email.toLowerCase().includes(q));
      
    const matchStatus = statusFilter.value === "all" || 
      (statusFilter.value === "active" && row.isActive) ||
      (statusFilter.value === "inactive" && !row.isActive);

    return matchQuery && matchStatus;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    operatorCode: "",
    operatorName: "",
    department: "",
    email: "",
    username: "",
    role: "Viewer",
    isActive: true
    ,pagePermissions: permissionCatalog.value.filter(x => x.viewerDefault).map(x => x.code)
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    operatorCode: item.operatorCode || "",
    operatorName: item.operatorName || "",
    department: item.department || "",
    email: item.email || "",
    username: item.username || "",
    role: item.role || "Viewer",
    isActive: item.isActive ?? true
    ,pagePermissions: [...(item.pagePermissions || [])]
  };
  formErr.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.operatorCode?.trim() || !form.value.operatorName?.trim()) {
    formErr.value = {key:'spc.settings.operatorRequired'};
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    const payload = { ...form.value };

    if (modalMode.value === "create") {
      const { data } = await api.post("/operators", payload);
      rows.value.push(data);
      successAlert({key:"spc.settings.operatorCreated",args:{name:data.operatorName}});
    } else {
      const { data } = await api.put(`/operators/${currentId.value}`, payload);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert({key:"spc.settings.operatorUpdated",args:{name:data.operatorName}});
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(t('spc.settings.confirmDelete',{code:item.operatorCode,name:item.operatorName}))) return;
  loading.value = true;
  try {
    await api.delete(`/operators/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert({key:"spc.settings.operatorDeleted",args:{name:item.operatorName}});
  } catch (e) {
    err.value = getApiErrorDisplay(e);
  } finally {
    loading.value = false;
  }
}

function successAlert(msg) {
  successMsg.value = msg;
  setTimeout(() => { successMsg.value = ""; }, 3000);
}

onMounted(load);
</script>

<template>
  <section class="space-y-6 pb-12">
    <!-- Title & Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <button
          data-testid="settings-comparePortalUsers" @click="comparePortalUsers"
          type="button"
          :disabled="comparing"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-semibold transition-all disabled:opacity-60"
        >
          <RefreshCw :class="['w-4 h-4', comparing ? 'animate-spin' : '']" /> {{ t('spc.settings.comparePortal') }}
        </button>
        <div class="p-3 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-xl shadow-lg shadow-blue-500/30 text-white">
          <Users class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">{{ t('spc.settings.operatorsTitle') }}</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ t('spc.settings.operatorsDescription') }}</p>
        </div>
      </div>
      
      <div class="flex items-center gap-3">
        <button
          data-testid="settings-load" @click="load"
          type="button"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-sm font-semibold transition-all border border-slate-200 dark:border-slate-700"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> {{ t('spc.settings.refresh') }}
        </button>
        <button
          data-testid="settings-openCreateModal" @click="openCreateModal"
          type="button"
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> {{ t('spc.settings.createOperator') }}
        </button>
      </div>
    </div>

    <!-- Guide / Wizard Tip -->
    <ModuleGuide :title="t('spc.settings.operatorsGuide')">
      <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
          {{ t('spc.settings.operatorsGuideDescription') }}
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-blue-700 dark:text-blue-400/80 leading-relaxed">
          <div class="font-black text-blue-900 dark:text-blue-300">{{ t('spc.settings.operatorsInstructions') }}</div>
          <p><strong>{{ t('spc.settings.searchPeople') }}</strong>{{ t('spc.settings.searchPeopleHelp') }}</p>
          <p><strong>{{ t('spc.settings.addPeople') }}</strong>{{ t('spc.settings.addPeopleHelp') }}</p>
          <p><strong>{{ t('spc.settings.notifications') }}</strong>{{ t('spc.settings.notificationsHelp') }}</p>
          <p><strong>{{ t('spc.settings.maintainStatus') }}</strong>{{ t('spc.settings.maintainStatusHelp') }}</p>
        </div>
    </ModuleGuide>

    <!-- Alert Messages -->
    <div v-if="err" class="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm animate-fade-in">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500" />
      <div class="text-sm font-semibold">{{ displayUiMessage(t,err) }}</div>
    </div>

    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm animate-fade-in">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ displayUiMessage(t,successMsg) }}</div>
    </div>

    <div v-if="comparison" class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
      <div class="p-5 border-b border-slate-200 dark:border-slate-800">
        <div class="flex items-center justify-between gap-3">
          <h2 class="font-black text-slate-800 dark:text-white">{{ t('spc.settings.comparisonTitle') }}</h2>
          <button data-testid="settings-syncSelected" @click="syncSelected" :disabled="!selectedPortalIds.length || comparing" class="px-4 py-2 rounded-lg bg-blue-600 text-white font-bold disabled:opacity-40">
            {{ t('spc.settings.syncPrefix') }}{{ selectedPortalIds.length }}）
          </button>
        </div>
        <p class="mt-1 text-xs text-slate-500">{{ t('spc.settings.comparisonPrefix') }} {{ comparison.portalCount }} {{ t('spc.settings.comparisonMiddle') }} {{ comparison.spcCount }} {{ t('spc.settings.comparisonSuffix') }}</p>
      </div>
      <div class="overflow-x-auto max-h-[32rem]">
        <table class="w-full text-left text-xs">
          <thead class="sticky top-0 bg-slate-50 dark:bg-slate-800"><tr><th class="p-3">{{ t('spc.settings.select') }}</th><th class="p-3">{{ t('spc.settings.status') }}</th><th class="p-3">{{ t('spc.settings.accountCode') }}</th><th class="p-3">Portal</th><th class="p-3">SPC</th><th class="p-3">{{ t('spc.settings.description') }}</th></tr></thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800">
            <tr v-for="item in comparison.rows" :key="`${item.status}-${item.portalUserId || 0}-${item.spcOperatorId || 0}`">
              <td class="p-3"><input v-if="item.portalUserId && item.status !== 'Matched' && item.status !== 'Conflict'" data-testid="settings-selectedPortalIds" v-model="selectedPortalIds" type="checkbox" :value="item.portalUserId" class="h-4 w-4" /><span v-else>-</span></td>
              <td class="p-3 font-bold" :class="item.status === 'Matched' ? 'text-emerald-600' : item.status === 'Conflict' ? 'text-red-600' : 'text-amber-600'">{{ comparisonLabel(item.status) }}</td>
              <td class="p-3"><div>{{ item.adAccount || '-' }}</div><div class="text-slate-500">{{ item.employeeNo || '-' }}</div></td>
              <td class="p-3"><div>{{ item.portalName || '-' }}</div><div class="text-slate-500">{{ item.portalDepartment || '-' }}／{{ item.portalEmail || '-' }}</div></td>
              <td class="p-3"><div>{{ item.spcName || '-' }}</div><div class="text-slate-500">{{ item.spcDepartment || '-' }}／{{ item.spcEmail || '-' }}</div><div v-if="item.spcRole">{{ item.spcRole }}</div></td>
              <td class="p-3 text-slate-500">{{ item.message || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Filters & Search Bar -->
    <div class="flex flex-col sm:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
      <div class="relative w-full sm:w-80">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          data-testid="settings-searchQuery" v-model="searchQuery"
          type="text"
          :placeholder="t('spc.settings.operatorSearch')"
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
        />
      </div>

      <div class="flex items-center gap-2 bg-slate-100 dark:bg-slate-800/60 p-1 rounded-xl border border-slate-200 dark:border-slate-700 w-full sm:w-auto">
        <button
          v-for="f in [{id:'all', label:t('spc.settings.allPeople')}, {id:'active', label:t('spc.settings.employed')}, {id:'inactive', label:t('spc.settings.inactivePeople')}]"
          :key="f.id"
          @click="statusFilter = f.id"
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap',
            statusFilter === f.id
              ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          ]"
        >
          {{ f.label }}
        </button>
      </div>
    </div>

    <!-- Data Table Container -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden transition-colors">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
              <th class="py-4 px-6">ID</th>
              <th class="py-4 px-6">{{ t('spc.settings.nameCode') }}</th>
              <th class="py-4 px-6">{{ t('spc.settings.operatorDepartment') }}</th>
              <th class="py-4 px-6">{{ t('spc.settings.accountRole') }}</th>
              <th class="py-4 px-6">{{ t('spc.settings.alertEmail') }}</th>
              <th class="py-4 px-6 text-center">{{ t('spc.settings.employment') }}</th>
              <th class="py-4 px-6 text-right">{{ t('spc.settings.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">{{ t('spc.settings.loadingPeople') }}</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400 font-medium">{{ t('spc.settings.noPeople') }}</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-blue-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-4 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-4 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2">
                  <UserCheck class="w-4 h-4 text-blue-500" /> {{ item.operatorName || t('spc.settings.unnamed') }}
                </div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5 font-mono">{{ item.operatorCode || '-' }}</div>
              </td>
              <td class="py-4 px-6 font-semibold text-xs text-slate-600 dark:text-slate-300">
                <div class="flex items-center gap-1.5">
                  <Building2 class="w-3.5 h-3.5 text-slate-400" /> {{ item.department || t('spc.settings.unassigned') }}
                </div>
              </td>
              <td class="py-4 px-6 text-xs">
                <div class="font-mono text-slate-700 dark:text-slate-200">{{ item.username || t('spc.settings.notConfigured') }}</div>
                <span :class="[
                  'mt-1 inline-flex rounded-full px-2 py-0.5 font-bold',
                  item.role === 'Editor'
                    ? 'bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300'
                    : 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300'
                ]">
                  {{ item.role === 'Editor' ? t('spc.settings.editor') : t('spc.settings.viewer') }}
                </span>
              </td>
              <td class="py-4 px-6 font-mono text-xs text-slate-600 dark:text-slate-300">
                <div class="flex items-center gap-1.5">
                  <Mail class="w-3.5 h-3.5 text-slate-400" /> {{ item.email || '-' }}
                </div>
              </td>
              <td class="py-4 px-6 text-center">
                <span
                  :class="[
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide',
                    item.isActive
                      ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800'
                      : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700'
                  ]"
                >
                  <span :class="['w-1.5 h-1.5 rounded-full', item.isActive ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                  {{ item.isActive ? t('spc.settings.employed') : t('spc.settings.departed') }}
                </span>
              </td>
              <td class="py-4 px-6 text-right space-x-2">
                <button
                  data-testid="settings-openEditModal" @click="openEditModal(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  :title="t('spc.settings.editOperator')"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  data-testid="settings-confirmDelete" @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  :title="t('spc.settings.deleteOperator')"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>{{ t('spc.settings.rangePrefix') }} {{ filteredRows.length }} {{ t('spc.settings.rangeMiddle') }} {{ rows.length }} {{ t('spc.settings.rangeSuffix') }}</span>
      </div>
    </div>

    <!-- Slide-over Panel (Add / Edit) -->
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
        <div data-testid="settings-operator-dialog" role="dialog" :aria-label="t('spc.settings.editOperatorTitle')" class="relative w-full max-w-md h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-blue-600 text-white rounded-xl shadow-md shadow-blue-500/20">
              <Users class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? t('spc.settings.createOperatorTitle') : t('spc.settings.editOperatorTitle') }}
            </h3>
          </div>
          <button
            @click="showModal = false"
            type="button"
            class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-all"
          >
            <X class="w-5 h-5" />
          </button>
        </div>

        <form @submit.prevent="save" class="flex flex-col h-full overflow-hidden">
          <div class="flex-1 overflow-y-auto p-6 space-y-5">
<LocalePicker />
          <div v-if="formErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
            <XCircle class="w-4 h-4 flex-shrink-0" /> {{ displayUiMessage(t,formErr) }}
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.settings.operatorCode') }} <span class="text-red-500">*</span></label>
              <input
                data-testid="settings-form.operatorCode" v-model="form.operatorCode"
                type="text"
                required
                :placeholder="t('spc.settings.operatorCodeExample')"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.settings.operatorName') }} <span class="text-red-500">*</span></label>
              <input
                data-testid="settings-form.operatorName" v-model="form.operatorName"
                type="text"
                required
                :placeholder="t('spc.settings.operatorNameExample')"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>
          </div>

          <div class="space-y-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.settings.visiblePages') }}</label>
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 p-3 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700">
              <label v-for="permission in permissionCatalog" :key="permission.code" class="flex items-center gap-2 text-xs">
                <input data-testid="settings-form.pagePermissions" v-model="form.pagePermissions" type="checkbox" :value="permission.code" class="h-4 w-4" />
                <span>{{ permission.name }}</span>
              </label>
            </div>
            <p class="text-xs text-slate-500">{{ t('spc.settings.permissionHelp') }}</p>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.settings.loginAccount') }}</label>
              <input
                data-testid="settings-form.username" v-model="form.username"
                type="text"
                autocomplete="off"
                :placeholder="t('spc.settings.noLogin')"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white"
              />
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.settings.systemRole') }}</label>
            <select
              data-testid="settings-form.role" v-model="form.role"
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white"
            >
              <option value="Viewer">{{ t('spc.settings.viewerOption') }}</option>
              <option value="Editor">{{ t('spc.settings.editorOption') }}</option>
            </select>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.settings.departmentLabel') }}</label>
              <input
                data-testid="settings-form.department" v-model="form.department"
                type="text"
                :placeholder="t('spc.settings.departmentExample')"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all"
              />
            </div>

            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.settings.contactEmail') }}</label>
              <input
                data-testid="settings-form.email" v-model="form.email"
                type="email"
                :placeholder="t('spc.settings.emailExample',{email:'user@pmr.com.tw'})"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 transition-all font-mono"
              />
            </div>
          </div>

          <div class="space-y-1.5 pt-2">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">{{ t('spc.settings.accountStatus') }}</label>
            <label class="relative inline-flex items-center cursor-pointer">
              <input data-testid="settings-form.isActive" v-model="form.isActive" type="checkbox" class="sr-only peer" />
              <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
              <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isActive ? t('spc.settings.activeOption') : t('spc.settings.inactiveOption') }}</span>
            </label>
          </div>

          </div>
          <div class="shrink-0 p-6 bg-slate-50 dark:bg-slate-800/80 border-t border-slate-200 dark:border-slate-800 flex items-center justify-end gap-3">
            <button
              @click="showModal = false"
              type="button"
              class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all"
            >
              {{ t('spc.settings.cancel') }}
            </button>
            <button
              type="submit"
              :disabled="loading"
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
            >
              <Save class="w-4 h-4" /> {{ t('spc.settings.save') }}
            </button>
          </div>
        </form>
      </div>
      </div>
    </transition>
  </section>
</template>
