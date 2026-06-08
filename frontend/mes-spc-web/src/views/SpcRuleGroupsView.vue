<script setup>
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  FolderTree,
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
  Activity,
  Layers,
  Settings2
} from "lucide-vue-next";

const groups = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);
const searchQuery = ref("");
const statusFilter = ref("all");

const showModal = ref(false);
const modalMode = ref("create");
const currentGroupId = ref(null);

const form = ref({
  ruleGroupCode: "",
  ruleGroupName: "",
  description: "",
  isEnabled: true
});

const formErr = ref("");
const groupRules = ref([]);
const rulesLoading = ref(false);

async function loadGroups() {
  err.value = "";
  loading.value = true;
  try {
    const res = await api.get("/spc-rule-groups");
    groups.value = res.data || [];
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const filteredGroups = computed(() => {
  return groups.value.filter(g => {
    const q = searchQuery.value.toLowerCase();
    const codeMatch = (g.ruleGroupCode || "").toLowerCase().includes(q);
    const nameMatch = (g.ruleGroupName || "").toLowerCase().includes(q);
    
    const matchQuery = !q || codeMatch || nameMatch;
    const matchStatus = statusFilter.value === "all" ||
      (statusFilter.value === "active" && g.isEnabled) ||
      (statusFilter.value === "inactive" && !g.isEnabled);

    return matchQuery && matchStatus;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentGroupId.value = null;
  form.value = {
    ruleGroupCode: "",
    ruleGroupName: "",
    description: "",
    isEnabled: true
  };
  groupRules.value = [];
  formErr.value = "";
  showModal.value = true;
}

async function openEditModal(group) {
  modalMode.value = "edit";
  currentGroupId.value = group.id;
  form.value = {
    ruleGroupCode: group.ruleGroupCode,
    ruleGroupName: group.ruleGroupName,
    description: group.description || "",
    isEnabled: group.isEnabled
  };
  formErr.value = "";
  showModal.value = true;
  
  // Load rules for this group
  await loadRulesForGroup(group.id);
}

async function loadRulesForGroup(groupId) {
  rulesLoading.value = true;
  try {
    const res = await api.get(`/spc-rules?groupId=${groupId}`);
    groupRules.value = res.data || [];
  } catch (e) {
    console.error("Failed to load rules:", e);
  } finally {
    rulesLoading.value = false;
  }
}

async function saveGroup() {
  if (!form.value.ruleGroupCode || !form.value.ruleGroupName) {
    formErr.value = "規則組代號與名稱為必填項目。";
    return;
  }
  formErr.value = "";
  loading.value = true;

  try {
    if (modalMode.value === "create") {
      const res = await api.post("/spc-rule-groups", form.value);
      successAlert("成功建立規則組");
      currentGroupId.value = res.data.id;
      modalMode.value = "edit";
      // Don't close modal, let user add rules
    } else {
      await api.put(`/spc-rule-groups/${currentGroupId.value}`, form.value);
      
      // Update rules
      for (const rule of groupRules.value) {
        if (rule.id) {
          await api.put(`/spc-rules/${rule.id}`, rule);
        } else {
          rule.ruleGroupId = currentGroupId.value;
          await api.post(`/spc-rules`, rule);
        }
      }
      successAlert("成功更新規則組與子規則");
      showModal.value = false;
    }
    await loadGroups();
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function addNewRule() {
  groupRules.value.push({
    ruleCode: `RULE-${groupRules.value.length + 1}`,
    ruleName: "新規則",
    ruleConfigJson: "{}",
    priority: (groupRules.value.length + 1) * 10,
    isEnabled: true
  });
}

function removeRule(index) {
  groupRules.value.splice(index, 1);
}

async function confirmDelete(group) {
  if (!confirm(`確定要刪除「${group.ruleGroupCode} - ${group.ruleGroupName}」及其所有規則嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/spc-rule-groups/${group.id}`);
    groups.value = groups.value.filter(x => x.id !== group.id);
    successAlert("成功刪除規則組");
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

function successAlert(msg) {
  successMsg.value = msg;
  setTimeout(() => { successMsg.value = ""; }, 3000);
}

onMounted(loadGroups);
</script>

<template>
  <section class="space-y-6">
    <!-- Title & Actions -->
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-indigo-600 to-blue-500 rounded-xl shadow-lg shadow-indigo-500/30 text-white">
          <Activity class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">SPC 異常檢驗規則組維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">管理西方電氣規則 (Western Electric Rules) 等管制圖判定異常標準</p>
        </div>
      </div>
      
      <div class="flex items-center gap-3">
        <button
          @click="loadGroups"
          type="button"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-sm font-semibold transition-all border border-slate-200 dark:border-slate-700"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> 重新整理
        </button>
        <button
          @click="openCreateModal"
          type="button"
          class="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 hover:to-blue-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 hover:shadow-xl hover:shadow-indigo-500/40 transition-all transform hover:-translate-y-0.5"
        >
          <Plus class="w-4 h-4" /> 新增規則組
        </button>
      </div>
    </div>

    <!-- Guide Alert -->
    <div class="p-5 bg-gradient-to-r from-indigo-50 to-blue-50 dark:from-indigo-950/30 dark:to-blue-900/20 border border-indigo-100 dark:border-indigo-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-indigo-100 dark:bg-indigo-900/50 rounded-xl text-indigo-600 dark:text-indigo-400 mt-0.5">
        <Settings2 class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-indigo-900 dark:text-indigo-300">模組指南：管制圖異常判定規則 (SPC Rules)</h4>
        <p class="text-xs text-indigo-700 dark:text-indigo-400/80 mt-1.5 leading-relaxed">
          這裡是管理管制圖如何判定製程出現「非隨機變異」的規則核心。系統預設內建了著名的西方電氣規則 (Western Electric Rules)。<br/>
          💡 <strong>注意事項：</strong> 後端引擎高度依賴規則代號 (RuleCode，如 WECO-1)，建議您僅修改其啟用狀態、權重或描述，除非您熟悉引擎邏輯，否則請勿隨意修改代號。
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-indigo-700 dark:text-indigo-400/80 leading-relaxed">
          <div class="font-black text-indigo-900 dark:text-indigo-300">SPC 異常判定規則頁面操作說明</div>
          <p><strong>查詢規則組：</strong>輸入規則組代號或名稱，快速找到要維護的判定規則。</p>
          <p><strong>新增規則組：</strong>按「新增規則組」，設定規則組名稱、描述與啟用狀態。</p>
          <p><strong>維護規則：</strong>依需要調整規則啟用狀態、權重或說明文字。</p>
          <p><strong>使用提醒：</strong>規則代號會被 SPC 引擎引用，除非確認影響範圍，請避免任意更改代號。</p>
        </div>
      </div>
    </div>

    <!-- Alerts -->
    <div v-if="err" class="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500" />
      <div class="text-sm font-semibold">{{ err }}</div>
    </div>
    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ successMsg }}</div>
    </div>

    <!-- Filters -->
    <div class="flex flex-col lg:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
      <div class="relative w-full lg:w-80">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="搜尋規則組代號或名稱..."
          class="w-full pl-10 pr-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-slate-100 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-indigo-500/50 focus:border-indigo-500 transition-all"
        />
      </div>

      <div class="flex items-center p-1 bg-slate-100 dark:bg-slate-800/60 rounded-xl border border-slate-200 dark:border-slate-700/80">
        <button
          v-for="f in [{id:'all', label:'全部狀態'}, {id:'active', label:'已啟用'}, {id:'inactive', label:'已停用'}]"
          :key="f.id"
          @click="statusFilter = f.id"
          type="button"
          :class="[
            'px-3 py-1.5 rounded-lg text-xs font-bold transition-all whitespace-nowrap',
            statusFilter === f.id
              ? 'bg-white dark:bg-slate-700 text-indigo-600 dark:text-indigo-400 shadow-sm'
              : 'text-slate-600 dark:text-slate-400 hover:text-slate-800 dark:hover:text-slate-200'
          ]"
        >
          {{ f.label }}
        </button>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
              <th class="py-4 px-6 w-16 text-center">ID</th>
              <th class="py-4 px-6">規則群組代號 (Rule Group)</th>
              <th class="py-4 px-6">說明與描述</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && groups.length === 0">
              <td colspan="5" class="py-12 text-center text-slate-400">載入中...</td>
            </tr>
            <tr v-else-if="filteredGroups.length === 0">
              <td colspan="5" class="py-12 text-center text-slate-400">找不到相符的規則組</td>
            </tr>
            <tr v-else v-for="g in filteredGroups" :key="g.id" class="hover:bg-indigo-50/50 dark:hover:bg-slate-800/50 transition-colors group">
              <td class="py-5 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ g.id }}</td>
              <td class="py-5 px-6">
                <div class="font-bold text-slate-900 dark:text-white flex items-center gap-2 text-base">
                  <FolderTree class="w-4 h-4 text-indigo-500 flex-shrink-0" />
                  {{ g.ruleGroupCode }}
                </div>
                <div class="text-xs text-slate-500 font-normal mt-0.5 ml-6">{{ g.ruleGroupName }}</div>
              </td>
              <td class="py-5 px-6 text-xs text-slate-500 dark:text-slate-400">
                {{ g.description || '-' }}
              </td>
              <td class="py-5 px-6 text-center">
                <span :class="['inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide', g.isEnabled ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700']">
                  <span :class="['w-1.5 h-1.5 rounded-full', g.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                  {{ g.isEnabled ? '啟用中' : '已停用' }}
                </span>
              </td>
              <td class="py-5 px-6 text-right space-x-2">
                <button
                  @click="openEditModal(g)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  title="編輯群組與細項規則"
                >
                  <Edit class="w-4 h-4" />
                </button>
                <button
                  @click="confirmDelete(g)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-red-50 dark:bg-slate-800 hover:bg-red-100 dark:hover:bg-red-950 text-red-600 dark:text-red-400 transition-all border border-red-200 dark:border-slate-700"
                  title="刪除規則群組"
                >
                  <Trash2 class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Slide-over Edit Panel -->
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
        <div class="relative w-full max-w-3xl h-full bg-white dark:bg-slate-900 shadow-2xl border-l border-slate-200 dark:border-slate-800 flex flex-col" @click.stop>
          
          <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80 shrink-0">
            <div class="flex items-center gap-3">
              <div class="p-2.5 bg-indigo-600 text-white rounded-xl shadow-md shadow-indigo-500/20">
                <Activity class="w-5 h-5" />
              </div>
              <h3 class="text-lg font-black text-slate-800 dark:text-white">
                {{ modalMode === 'create' ? '新增 SPC 異常檢驗規則組' : '編輯檢驗規則與細項' }}
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

          <form @submit.prevent="saveGroup" class="flex flex-col h-full overflow-hidden">
            <div class="flex-1 overflow-y-auto p-6 space-y-6">
              <div v-if="formErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
                <XCircle class="w-4 h-4 flex-shrink-0" /> {{ formErr }}
              </div>

              <!-- Group Header -->
              <div class="p-5 bg-slate-50 dark:bg-slate-800/50 rounded-2xl border border-slate-200 dark:border-slate-700 space-y-4">
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div class="space-y-1.5">
                    <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">群組代號 <span class="text-red-500">*</span></label>
                    <input v-model="form.ruleGroupCode" type="text" required placeholder="例如: WECO" class="w-full px-3 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all" />
                  </div>
                  <div class="space-y-1.5">
                    <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">群組名稱 <span class="text-red-500">*</span></label>
                    <input v-model="form.ruleGroupName" type="text" required placeholder="例如: 西方電氣規則" class="w-full px-3 py-2.5 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all" />
                  </div>
                </div>
                <div class="space-y-1.5">
                  <label class="block text-xs font-bold text-slate-600 dark:text-slate-400 uppercase tracking-wider">描述與說明</label>
                  <textarea v-model="form.description" rows="2" class="w-full px-3 py-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500 transition-all"></textarea>
                </div>
                <div class="flex items-center gap-3">
                  <label class="relative inline-flex items-center cursor-pointer">
                    <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                    <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-indigo-300 dark:peer-focus:ring-indigo-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
                    <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">啟用整個規則組</span>
                  </label>
                </div>
              </div>

              <!-- Rules Array -->
              <div v-if="modalMode === 'edit'" class="space-y-4">
                <div class="flex items-center justify-between">
                  <h4 class="text-sm font-bold text-slate-800 dark:text-slate-200 flex items-center gap-2">
                    <Layers class="w-4 h-4 text-indigo-500" />
                    包含的子規則清單
                  </h4>
                  <button
                    type="button"
                    @click="addNewRule"
                    class="text-xs font-bold px-3 py-1.5 bg-indigo-50 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 rounded-lg hover:bg-indigo-100 transition-colors flex items-center gap-1 border border-indigo-200 dark:border-indigo-800"
                  >
                    <Plus class="w-3.5 h-3.5" /> 新增規則
                  </button>
                </div>

                <div v-if="rulesLoading" class="p-6 text-center text-slate-400 text-sm">載入規則中...</div>
                
                <div v-else class="space-y-3">
                  <div v-if="groupRules.length === 0" class="p-6 text-center text-slate-400 text-xs border border-dashed border-slate-300 dark:border-slate-700 rounded-xl">
                    尚未設定任何子規則
                  </div>
                  <div
                    v-for="(rule, index) in groupRules"
                    :key="index"
                    class="p-4 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-sm relative group"
                  >
                    <button
                      type="button"
                      @click="removeRule(index)"
                      class="absolute -top-2 -right-2 p-1.5 bg-red-100 dark:bg-red-900 text-red-600 dark:text-red-400 rounded-full shadow-sm hover:bg-red-200 transition-colors opacity-0 group-hover:opacity-100"
                    >
                      <X class="w-3.5 h-3.5" />
                    </button>
                    
                    <div class="grid grid-cols-1 md:grid-cols-12 gap-4 items-start">
                      <div class="md:col-span-3 space-y-1">
                        <label class="block text-[10px] font-bold text-slate-500 uppercase">規則代號</label>
                        <input v-model="rule.ruleCode" type="text" class="w-full px-2.5 py-1.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-mono font-bold text-indigo-700 dark:text-indigo-400" />
                      </div>
                      <div class="md:col-span-4 space-y-1">
                        <label class="block text-[10px] font-bold text-slate-500 uppercase">規則判定名稱 / 說明</label>
                        <input v-model="rule.ruleName" type="text" class="w-full px-2.5 py-1.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-bold text-slate-800 dark:text-slate-200" />
                      </div>
                      <div class="md:col-span-2 space-y-1">
                        <label class="block text-[10px] font-bold text-slate-500 uppercase">判斷權重</label>
                        <input v-model="rule.priority" type="number" class="w-full px-2.5 py-1.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-xs font-mono font-bold" />
                      </div>
                      <div class="md:col-span-3 space-y-1">
                        <label class="block text-[10px] font-bold text-slate-500 uppercase mb-2">是否啟用</label>
                        <label class="relative inline-flex items-center cursor-pointer">
                          <input v-model="rule.isEnabled" type="checkbox" class="sr-only peer" />
                          <div class="w-8 h-4 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-3 after:w-3 after:transition-all peer-checked:bg-emerald-500"></div>
                          <span class="ml-2 text-xs font-bold text-slate-600 dark:text-slate-400">{{ rule.isEnabled ? '啟用' : '停用' }}</span>
                        </label>
                      </div>
                      <div class="md:col-span-12 space-y-1">
                         <label class="block text-[10px] font-bold text-slate-500 uppercase">自訂參數 (JSON)</label>
                         <input v-model="rule.ruleConfigJson" type="text" placeholder="{}" class="w-full px-2.5 py-1.5 bg-slate-50 dark:bg-slate-900 border border-slate-200 dark:border-slate-700 rounded-lg text-[11px] font-mono text-slate-600 dark:text-slate-400" />
                      </div>
                    </div>
                  </div>
                </div>
              </div>
              <div v-else class="p-6 bg-slate-50 dark:bg-slate-800/50 rounded-xl text-center text-xs text-slate-500 border border-dashed border-slate-300 dark:border-slate-700">
                請先儲存建立此群組後，再開始新增子項規則。
              </div>

            </div>
            
            <div class="shrink-0 p-6 bg-slate-50 dark:bg-slate-800/80 border-t border-slate-200 dark:border-slate-800 flex items-center justify-end gap-3">
              <button
                @click="showModal = false"
                type="button"
                class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all"
              >
                取消
              </button>
              <button
                type="submit"
                :disabled="loading"
                class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 hover:to-blue-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 hover:shadow-xl hover:shadow-indigo-500/40 transition-all transform hover:-translate-y-0.5"
              >
                <Save class="w-4 h-4" /> {{ modalMode === 'create' ? '建立群組並繼續新增規則' : '確認儲存所有變更' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </transition>
  </section>
</template>
