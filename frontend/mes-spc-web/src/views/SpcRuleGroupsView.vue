<script setup>
import { onMounted, ref, computed } from "vue";
import { api, getApiErrorMessage } from "../api/client";
import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  Edit,
  RefreshCw,
  Save,
  Search,
  Settings2,
  X,
  XCircle
} from "lucide-vue-next";

const rules = ref([]);
const ruleGroups = ref([]);
const err = ref("");
const successMsg = ref("");
const loading = ref(false);
const searchQuery = ref("");
const statusFilter = ref("all");

function formatRuleCode(value) {
  const match = String(value || "").match(/^(?:Rule|Nelson)(\d+)/i);
  return match ? `Rule ${match[1]}` : String(value || "");
}

const showModal = ref(false);
const modalMode = ref("create");
const currentRuleId = ref(null);
const formErr = ref("");
const form = ref(defaultForm());

function defaultForm() {
  return {
    ruleGroupId: null,
    ruleCode: "",
    ruleName: "",
    ruleConfigJson: "{}",
    priority: 100,
    isEnabled: true
  };
}

async function loadAll() {
  err.value = "";
  loading.value = true;
  try {
    const [rulesRes, groupsRes] = await Promise.all([
      api.get("/spc-rules?libraryOnly=true"),
      api.get("/spc-rule-groups")
    ]);
    rules.value = rulesRes.data || [];
    const visibleGroupIds = new Set(rules.value.map(rule => Number(rule.ruleGroupId)));
    ruleGroups.value = (groupsRes.data || []).filter(group => visibleGroupIds.has(Number(group.id)));
  } catch (e) {
    err.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

const defaultRuleGroupId = computed(() => {
  const we = ruleGroups.value.find(x => x.ruleGroupCode === "WE");
  return we?.id || ruleGroups.value[0]?.id || null;
});

const ruleGroupMap = computed(() => {
  const map = {};
  ruleGroups.value.forEach(g => {
    map[g.id] = `${g.ruleGroupCode} (${g.ruleGroupName})`;
  });
  return map;
});

const filteredRules = computed(() => {
  const q = searchQuery.value.trim().toLowerCase();
  return rules.value.filter(rule => {
    const groupLabel = ruleGroupMap.value[rule.ruleGroupId] || "";
    const matchQuery = !q ||
      (rule.ruleCode || "").toLowerCase().includes(q) ||
      (rule.ruleName || "").toLowerCase().includes(q) ||
      groupLabel.toLowerCase().includes(q);
    const matchStatus = statusFilter.value === "all" ||
      (statusFilter.value === "active" && rule.isEnabled) ||
      (statusFilter.value === "inactive" && !rule.isEnabled);

    return matchQuery && matchStatus;
  });
});

function openCreateModal() {
  modalMode.value = "create";
  currentRuleId.value = null;
  form.value = {
    ...defaultForm(),
    ruleGroupId: defaultRuleGroupId.value,
    priority: nextPriority()
  };
  formErr.value = "";
  showModal.value = true;
}

function openEditModal(rule) {
  modalMode.value = "edit";
  currentRuleId.value = rule.id;
  form.value = {
    ruleGroupId: rule.ruleGroupId || defaultRuleGroupId.value,
    ruleCode: rule.ruleCode || "",
    ruleName: rule.ruleName || "",
    ruleConfigJson: rule.ruleConfigJson || "{}",
    priority: rule.priority ?? 100,
    isEnabled: rule.isEnabled ?? true
  };
  formErr.value = "";
  showModal.value = true;
}

function nextPriority() {
  const max = rules.value.reduce((acc, rule) => Math.max(acc, Number(rule.priority) || 0), 0);
  return Math.max(10, Math.ceil((max + 10) / 10) * 10);
}

async function saveRule() {
  if (!form.value.ruleCode?.trim() || !form.value.ruleName?.trim()) {
    formErr.value = "規則代號與規則名稱為必填。";
    return;
  }

  try {
    JSON.parse(form.value.ruleConfigJson || "{}");
  } catch {
    formErr.value = "自訂參數 JSON 格式無效，請檢查語法。";
    return;
  }

  formErr.value = "";
  loading.value = true;
  try {
    const payload = {
      ...form.value,
      ruleGroupId: form.value.ruleGroupId ? parseInt(form.value.ruleGroupId) : 0,
      priority: parseInt(form.value.priority) || 100,
      ruleCode: form.value.ruleCode.trim(),
      ruleName: form.value.ruleName.trim()
    };

    if (modalMode.value === "create") {
      const { data } = await api.post("/spc-rules", payload);
      rules.value.push(data);
      successAlert("成功新增異常規則：" + data.ruleName);
    } else {
      const { data } = await api.put(`/spc-rules/${currentRuleId.value}`, payload);
      const idx = rules.value.findIndex(x => x.id === currentRuleId.value);
      if (idx !== -1) rules.value[idx] = data;
      successAlert("成功更新異常規則：" + data.ruleName);
    }
    showModal.value = false;
    await loadAll();
  } catch (e) {
    formErr.value = getApiErrorMessage(e);
  } finally {
    loading.value = false;
  }
}

async function confirmDelete(rule) {
  if (!confirm(`確定要刪除異常規則「${rule.ruleCode} - ${rule.ruleName}」嗎？`)) return;
  loading.value = true;
  try {
    await api.delete(`/spc-rules/${rule.id}`);
    rules.value = rules.value.filter(x => x.id !== rule.id);
    successAlert("成功刪除異常規則");
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

onMounted(loadAll);
</script>

<template>
  <section class="space-y-6">
    <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 p-6 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800">
      <div class="flex items-center gap-3">
        <div class="p-3 bg-gradient-to-tr from-indigo-600 to-blue-500 rounded-xl shadow-lg shadow-indigo-500/30 text-white">
          <Activity class="w-7 h-7" />
        </div>
        <div>
          <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 tracking-tight">SPC 八大管制規則維護</h1>
          <p class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">固定使用西方電氣規則 1～8，可調整參數及啟用狀態</p>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <button
          @click="loadAll"
          type="button"
          :disabled="loading"
          class="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 text-sm font-semibold transition-all border border-slate-200 dark:border-slate-700"
        >
          <RefreshCw :class="['w-4 h-4', loading ? 'animate-spin' : '']" /> 重新整理
        </button>
      </div>
    </div>

    <div class="p-5 bg-gradient-to-r from-indigo-50 to-blue-50 dark:from-indigo-950/30 dark:to-blue-900/20 border border-indigo-100 dark:border-indigo-800/50 rounded-2xl flex items-start gap-4 shadow-sm">
      <div class="p-2 bg-indigo-100 dark:bg-indigo-900/50 rounded-xl text-indigo-600 dark:text-indigo-400 mt-0.5">
        <Settings2 class="w-5 h-5" />
      </div>
      <div>
        <h4 class="text-sm font-bold text-indigo-900 dark:text-indigo-300">規則庫說明</h4>
        <p class="text-xs text-indigo-700 dark:text-indigo-400/80 mt-1.5 leading-relaxed">
          系統只保留西方電氣規則 1～8，不顯示管制圖或管制項目的內部規則群組，也不可新增第 9 種規則。
        </p>
      </div>
    </div>

    <div v-if="err" class="flex items-center gap-3 p-4 bg-red-50 dark:bg-red-950/50 text-red-700 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-2xl shadow-sm">
      <AlertTriangle class="w-6 h-6 flex-shrink-0 text-red-500" />
      <div class="text-sm font-semibold">{{ err }}</div>
    </div>
    <div v-if="successMsg" class="flex items-center gap-3 p-4 bg-emerald-50 dark:bg-emerald-950/50 text-emerald-700 dark:text-emerald-300 border border-emerald-200 dark:border-emerald-800/80 rounded-2xl shadow-sm">
      <CheckCircle2 class="w-6 h-6 flex-shrink-0 text-emerald-500" />
      <div class="text-sm font-semibold">{{ successMsg }}</div>
    </div>

    <div class="flex flex-col lg:flex-row gap-4 p-4 bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 items-center justify-between">
      <div class="relative w-full lg:w-96">
        <span class="absolute inset-y-0 left-0 flex items-center pl-3.5 pointer-events-none text-slate-400">
          <Search class="w-4 h-4" />
        </span>
        <input
          v-model="searchQuery"
          type="text"
          placeholder="搜尋規則代號、名稱或規則庫..."
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

    <div class="bg-white dark:bg-slate-900 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-800 overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 dark:bg-slate-800/80 text-slate-500 dark:text-slate-400 font-bold text-xs uppercase tracking-wider border-b border-slate-200 dark:border-slate-700">
              <th class="py-4 px-6 w-16 text-center">ID</th>
              <th class="py-4 px-6">規則代號 / 名稱</th>
              <th class="py-4 px-6">規則庫</th>
              <th class="py-4 px-6 text-center">優先序</th>
              <th class="py-4 px-6">參數</th>
              <th class="py-4 px-6 text-center">狀態</th>
              <th class="py-4 px-6 text-right">操作</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 dark:divide-slate-800/80 text-sm font-medium text-slate-700 dark:text-slate-300">
            <tr v-if="loading && rules.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">載入中...</td>
            </tr>
            <tr v-else-if="filteredRules.length === 0">
              <td colspan="7" class="py-12 text-center text-slate-400">找不到相符的異常規則</td>
            </tr>
            <tr v-else v-for="rule in filteredRules" :key="rule.id" class="hover:bg-indigo-50/50 dark:hover:bg-slate-800/50 transition-colors">
              <td class="py-5 px-6 font-mono text-xs text-slate-400 dark:text-slate-500 text-center">#{{ rule.id }}</td>
              <td class="py-5 px-6">
                <div class="font-mono font-black text-indigo-600 dark:text-indigo-400">{{ formatRuleCode(rule.ruleCode) }}</div>
                <div class="text-xs text-slate-500 dark:text-slate-400 mt-0.5">{{ rule.ruleName }}</div>
              </td>
              <td class="py-5 px-6 text-xs text-slate-500 dark:text-slate-400">
                {{ ruleGroupMap[rule.ruleGroupId] || `規則庫 #${rule.ruleGroupId}` }}
              </td>
              <td class="py-5 px-6 text-center font-mono font-bold text-xs">{{ rule.priority }}</td>
              <td class="py-5 px-6 font-mono text-[11px] text-slate-500 dark:text-slate-400 max-w-xs truncate" :title="rule.ruleConfigJson || '{}'">
                {{ rule.ruleConfigJson || '{}' }}
              </td>
              <td class="py-5 px-6 text-center">
                <span :class="['inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold border tracking-wide', rule.isEnabled ? 'bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 border-emerald-300 dark:border-emerald-800' : 'bg-slate-100 dark:bg-slate-800 text-slate-500 dark:text-slate-400 border-slate-300 dark:border-slate-700']">
                  <span :class="['w-1.5 h-1.5 rounded-full', rule.isEnabled ? 'bg-emerald-500 animate-pulse' : 'bg-slate-400']"></span>
                  {{ rule.isEnabled ? '啟用中' : '已停用' }}
                </span>
              </td>
              <td class="py-5 px-6 text-right space-x-2">
                <button
                  @click="openEditModal(rule)"
                  type="button"
                  class="inline-flex items-center justify-center p-2 rounded-xl bg-blue-50 dark:bg-slate-800 hover:bg-blue-100 dark:hover:bg-blue-900 text-blue-600 dark:text-blue-400 transition-all border border-blue-200 dark:border-slate-700"
                  title="編輯異常規則"
                >
                  <Edit class="w-4 h-4" />
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-sm">
      <div class="bg-white dark:bg-slate-900 w-full max-w-2xl rounded-3xl shadow-2xl border border-slate-200 dark:border-slate-800 overflow-hidden">
        <div class="flex items-center justify-between px-6 py-5 bg-slate-50 dark:bg-slate-800/80 border-b border-slate-200 dark:border-slate-700/80">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-indigo-600 text-white rounded-xl shadow-md shadow-indigo-500/20">
              <Activity class="w-5 h-5" />
            </div>
            <h3 class="text-lg font-black text-slate-800 dark:text-white">
              {{ modalMode === 'create' ? '新增異常規則' : '編輯異常規則' }}
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

        <form @submit.prevent="saveRule" class="p-6 space-y-5">
          <div v-if="formErr" class="flex items-center gap-2 p-3 bg-red-50 dark:bg-red-950/50 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-800/80 rounded-xl text-xs font-bold">
            <XCircle class="w-4 h-4 flex-shrink-0" /> {{ formErr }}
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">所屬規則庫</label>
            <select v-model="form.ruleGroupId" disabled class="w-full px-4 py-2.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-bold text-sm text-slate-500 dark:text-slate-400">
              <option v-for="group in ruleGroups" :key="group.id" :value="group.id">{{ group.ruleGroupCode }} - {{ group.ruleGroupName }}</option>
            </select>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">規則代號 <span class="text-red-500">*</span></label>
              <input v-model="form.ruleCode" type="text" readonly class="w-full px-4 py-2.5 bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono font-bold text-sm text-slate-500 dark:text-slate-400" />
            </div>
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">規則名稱 <span class="text-red-500">*</span></label>
              <input v-model="form.ruleName" type="text" required placeholder="例如：連續 3 點中有 2 點超出 2 Sigma" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div class="space-y-1.5">
              <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">排序優先序</label>
              <input v-model="form.priority" type="number" min="1" class="w-full px-4 py-2.5 bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl font-mono text-sm text-slate-800 dark:text-white focus:outline-none focus:ring-2 focus:ring-indigo-500" />
            </div>
            <div class="space-y-1.5 pt-7">
              <label class="relative inline-flex items-center cursor-pointer">
                <input v-model="form.isEnabled" type="checkbox" class="sr-only peer" />
                <div class="w-11 h-6 bg-slate-200 dark:bg-slate-700 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-indigo-300 dark:peer-focus:ring-indigo-800 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-indigo-600"></div>
                <span class="ml-3 text-sm font-bold text-slate-700 dark:text-slate-300">{{ form.isEnabled ? '啟用此規則' : '停用此規則' }}</span>
              </label>
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-slate-600 dark:text-slate-400">自訂參數 JSON</label>
            <textarea v-model="form.ruleConfigJson" rows="5" placeholder="{}" class="w-full px-4 py-2.5 bg-slate-900 text-emerald-400 dark:bg-black font-mono text-xs rounded-xl border border-slate-700 resize-none focus:outline-none focus:ring-2 focus:ring-indigo-500"></textarea>
          </div>

          <div class="flex items-center justify-end gap-3 pt-4 border-t border-slate-200 dark:border-slate-800">
            <button @click="showModal = false" type="button" class="px-5 py-2.5 rounded-xl bg-slate-100 dark:bg-slate-800 hover:bg-slate-200 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-300 text-sm font-bold transition-all">
              取消
            </button>
            <button type="submit" :disabled="loading" class="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-indigo-600 to-blue-600 hover:from-indigo-500 hover:to-blue-500 text-white text-sm font-bold shadow-lg shadow-indigo-500/25 transition-all">
              <Save class="w-4 h-4" /> 儲存
            </button>
          </div>
        </form>
      </div>
    </div>
  </section>
</template>
