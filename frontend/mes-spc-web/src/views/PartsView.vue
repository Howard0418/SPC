<script setup>
import { useI18n } from "vue-i18n";
import LocalePicker from "../components/LocalePicker.vue";
import ModuleGuide from "../components/ModuleGuide.vue";
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Package,
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
  Info
} from "lucide-vue-next";

const { t } = useI18n({ useScope: "global" });

const rows = ref([]);
const err = ref("");
const successMsg = ref(null);
const successText = computed(() => successMsg.value ? t(successMsg.value.key, successMsg.value.args) : "");
const loading = ref(false);

const searchQuery = ref("");
const statusFilter = ref("all"); // 'all', 'active', 'inactive'

const showModal = ref(false);
const modalMode = ref("create"); // 'create' or 'edit'
const currentId = ref(null);
const form = ref({
  partNo: "",
  partName: "",
  specification: "",
  customer: "",
  isEnabled: true
});
const formErr = ref("");
const formErrKey = ref("");
const formError = computed(() => formErrKey.value ? t(formErrKey.value) : formErr.value);

async function load() {
  err.value = "";
  loading.value = true;
  try {
    const { data } = await api.get("/parts");
    rows.value = data || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const filteredRows = computed(() => {
  return rows.value.filter(row => {
    const q = searchQuery.value.toLowerCase();
    const matchQuery = !q || 
      (row.partNo && row.partNo.toLowerCase().includes(q)) ||
      (row.partName && row.partName.toLowerCase().includes(q)) ||
      (row.specification && row.specification.toLowerCase().includes(q)) ||
      (row.customer && row.customer.toLowerCase().includes(q));
      
    if (statusFilter.value === "active") return matchQuery && row.isEnabled;
    if (statusFilter.value === "inactive") return matchQuery && !row.isEnabled;
    return matchQuery;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentId.value = null;
  form.value = {
    partNo: "",
    partName: "",
    specification: "",
    customer: "",
    isEnabled: true
  };
  formErr.value = "";
  formErrKey.value = "";
  showModal.value = true;
}

function openEditModal(item) {
  modalMode.value = "edit";
  currentId.value = item.id;
  form.value = {
    partNo: item.partNo || "",
    partName: item.partName || "",
    specification: item.specification || "",
    customer: item.customer || "",
    isEnabled: item.isEnabled ?? true
  };
  formErr.value = "";
  formErrKey.value = "";
  showModal.value = true;
}

async function save() {
  if (!form.value.partNo?.trim() || !form.value.partName?.trim()) {
    formErrKey.value = "spc.parts.required";
    return;
  }
  formErr.value = "";
  formErrKey.value = "";
  loading.value = true;

  try {
    if (modalMode.value === "create") {
      const { data } = await api.post("/parts", form.value);
      rows.value.push(data);
      successAlert("spc.parts.created", { partNo: String(data.partNo) });
    } else {
      const { data } = await api.put(`/parts/${currentId.value}`, form.value);
      const idx = rows.value.findIndex(x => x.id === currentId.value);
      if (idx !== -1) rows.value[idx] = data;
      successAlert("spc.parts.updated", { partNo: String(data.partNo) });
    }
    showModal.value = false;
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(item) {
  if (!confirm(t("spc.parts.confirmDelete", { partNo: String(item.partNo), partName: String(item.partName) }))) return;
  loading.value = true;
  try {
    await api.delete(`/parts/${item.id}`);
    rows.value = rows.value.filter(x => x.id !== item.id);
    successAlert("spc.parts.deleted", { partNo: String(item.partNo) });
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function successAlert(key, args) {
  successMsg.value = { key, args };
  setTimeout(() => { successMsg.value = null; }, 3000);
}

onMounted(load);
</script>

<template>
  <section class="space-y-6">
    <!-- Title & Top Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex flex-wrap items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-blue-600 to-indigo-600 rounded-xl shadow-lg shadow-blue-500/30 text-white">
          <Package class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black break-words text-slate-800 dark:text-slate-100 tracking-tight">{{ t('spc.parts.title') }}</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ t('spc.parts.subtitle') }}</p>
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
          data-testid="master-add" @click="openCreateModal"
          type="button"
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> {{ t('spc.parts.add') }}
        </button>
      </div>
    </div>

    <!-- Guide / Wizard Tip -->
    <ModuleGuide :toggle-label="t('common.actions.toggleGuide')" :title="t('spc.parts.guideTitle')">
      <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
          {{ t('spc.parts.guideIntro') }}<br/>
          💡 <strong>{{ t('spc.parts.nextStep') }}</strong> {{ t('spc.parts.guideNext') }}
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-blue-700 dark:text-blue-400/80 leading-relaxed">
          <div class="font-black text-blue-900 dark:text-blue-300">{{ t('spc.parts.guideHeading') }}</div>
          <p><strong>{{ t('spc.parts.guideSearchLabel') }}</strong>{{ t('spc.parts.guideSearch') }}</p>
          <p><strong>{{ t('spc.parts.guideAddLabel') }}</strong>{{ t('spc.parts.guideAdd') }}</p>
          <p><strong>{{ t('spc.parts.guideEditLabel') }}</strong>{{ t('spc.parts.guideEdit') }}</p>
          <p><strong>{{ t('spc.parts.guideSetupLabel') }}</strong>{{ t('spc.parts.guideSetup') }}</p>
        </div>
    </ModuleGuide>

    <!-- Alert Messages -->
    <div v-if="err" class="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm animate-fade-in">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500" />
      <div class="text-sm font-semibold">{{ err }}</div>
    </div>

    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm animate-fade-in">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ successText }}</div>
    </div>

    <!-- Filters & Search Bar -->
    <div class="flex flex-col sm:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
      <div class="relative w-full sm:w-80">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          data-testid="master-search" v-model="searchQuery"
          type="text"
          :placeholder="t('spc.parts.search')"
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500/50 focus:border-blue-500 transition-all"
        />
      </div>

      <div class="flex items-center gap-2 w-full sm:w-auto overflow-x-auto p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
        <button
          v-for="f in [{id:'all', label:'common.status.all'}, {id:'active', label:'common.status.enabled'}, {id:'inactive', label:'common.status.inactive'}]"
          :key="f.id"
          @click="statusFilter = f.id"
          type="button"
          :class="[
            'px-4 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap',
            statusFilter === f.id
              ? 'bg-white dark:bg-slate-700 text-blue-600 dark:text-blue-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          ]"
        >
          {{ t(f.label) }}
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
              <th class="py-4 px-6">{{ t('spc.parts.partNameColumn') }}</th>
              <th class="py-4 px-6">{{ t('spc.parts.specification') }}</th>
              <th class="py-4 px-6">{{ t('spc.parts.customer') }}</th>
              <th class="py-4 px-6 text-center">{{ t('common.columns.status') }}</th>
              <th class="py-4 px-6">{{ t('common.columns.updatedAt') }}</th>
              <th class="py-4 px-6 text-right">{{ t('common.columns.actions') }}</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">{{ t('spc.parts.loading') }}</td>
            </tr>
            <tr v-else-if="filteredRows.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">{{ t('spc.parts.empty') }}</td>
            </tr>
            <tr
              v-else
              v-for="item in filteredRows"
              :key="item.id"
              class="hover:bg-blue-50/50 dark:hover:bg-slate-800/50 transition-colors group"
            >
              <td class="py-6 px-6 font-mono text-xs text-slate-400 dark:text-slate-500">#{{ item.id }}</td>
              <td class="py-6 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2 text-base">
                  <Package class="w-4 h-4 text-blue-500" /> {{ item.partNo }}
                </div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ item.partName || t('spc.parts.unnamed') }}</div>
              </td>
              <td class="py-6 px-6 text-slate-600 dark:text-slate-300 max-w-xs truncate" :title="item.specification">
                {{ item.specification || '-' }}
              </td>
              <td class="py-6 px-6">
                <span v-if="item.customer" class="px-2.5 py-1 bg-indigo-50 dark:bg-indigo-950/60 text-indigo-700 dark:text-indigo-300 border border-indigo-200 dark:border-indigo-800 rounded-lg text-xs font-semibold">
                  {{ item.customer }}
                </span>
                <span v-else class="text-slate-400 text-xs">-</span>
              </td>
              <td class="py-6 px-6 text-center">
                <span
                  :class="[
                    'inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border shadow-xs tracking-wide',
                    item.isEnabled
                      ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800'
                      : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700'
                  ]"
                >
                  <span :class="['w-1.5 h-1.5 rounded-full', item.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                  {{ t(item.isEnabled ? 'common.status.active' : 'common.status.inactive') }}
                </span>
              </td>
              <td class="py-6 px-6 font-mono text-xs text-slate-500 dark:text-slate-400">
                {{ item.updatedAt ? new Date(item.updatedAt).toLocaleString() : '-' }}
              </td>
              <td class="py-6 px-6 text-right space-x-2">
                <button
                  data-testid="master-edit" @click="openEditModal(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  :title="t('spc.parts.edit')"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  data-testid="master-delete" @click="confirmDelete(item)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  :title="t('spc.parts.delete')"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Footer count -->
      <div class="px-6 py-4 bg-slate-50 dark:bg-slate-800/50 border-t border-slate-200 dark:border-slate-800 flex items-center justify-between text-xs font-semibold text-slate-500 dark:text-slate-400">
        <span>{{ t('spc.parts.count', { start: String(filteredRows.length ? 1 : 0), end: String(filteredRows.length), total: String(rows.length) }) }}</span>
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
        <div role="dialog" aria-modal="true" :aria-label="t(modalMode === 'create' ? 'spc.parts.createTitle' : 'spc.parts.editTitle')" class="relative w-full max-w-md h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
          <div class="flex flex-wrap items-center gap-3">
            <div class="p-2.5 bg-blue-600 text-white rounded-xl shadow-md shadow-blue-500/20">
              <Package class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black break-words text-slate-800 dark:text-white">
              {{ t(modalMode === 'create' ? 'spc.parts.createTitle' : 'spc.parts.editTitle') }}
            </h3>
          </div>
          <button
            :aria-label="t('common.actions.close')"
            @click="showModal = false"
            type="button"
            class="p-2 rounded-xl hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-400 hover:text-slate-600 dark:hover:text-slate-200 transition-all"
          >
            <X class="w-5 h-5" />
          </button>
        </div>

        <div class="px-6 py-2 border-b border-slate-200 dark:border-slate-700 shrink-0"><LocalePicker /></div>
        <form @submit.prevent="save" class="flex flex-col h-full overflow-hidden">
          <div class="flex-1 overflow-y-auto p-6 space-y-5">
          <div v-if="formError" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
            <XCircle class="w-4 h-4 flex-shrink-0" /> {{ formError }}
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.parts.partNoField') }} <span class="text-red-500">*</span></label>
              <input
                data-testid="partNo" v-model="form.partNo"
                type="text"
                required
                :placeholder="t('spc.parts.partNoPlaceholder')"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>
            
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.parts.partNameField') }} <span class="text-red-500">*</span></label>
              <input
                data-testid="partName" v-model="form.partName"
                type="text"
                required
                :placeholder="t('spc.parts.partNamePlaceholder')"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.parts.specificationField') }}</label>
            <textarea
              data-testid="specification" v-model="form.specification"
              rows="3"
              :placeholder="t('spc.parts.specificationPlaceholder')"
              class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all resize-none"
            ></textarea>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4 items-center">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">{{ t('spc.parts.customerField') }}</label>
              <input
                data-testid="customer" v-model="form.customer"
                type="text"
                :placeholder="t('spc.parts.customerPlaceholder')"
                class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-slate-800 dark:text-white placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all"
              />
            </div>

            <div class="space-y-1.5 pt-2">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider mb-2">{{ t('spc.parts.enabledField') }}</label>
              <label class="relative inline-flex items-center cursor-pointer">
                <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 dark:peer-focus:ring-blue-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ t(form.isEnabled ? 'spc.parts.formActive' : 'spc.parts.formInactive') }}</span>
              </label>
            </div>
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
              class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-500 hover:to-indigo-500 text-white text-sm font-bold shadow-lg shadow-blue-500/25 hover:shadow-xl hover:shadow-blue-500/40 transition-all transform hover:-translate-y-0.5"
            >
              <Save class="w-4 h-4" /> {{ t('common.actions.save') }}
            </button>
          </div>
        </form>
      </div>
      </div>
    </transition>
  </section>
</template>
