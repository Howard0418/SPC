<script setup>
import ModuleGuide from "../components/ModuleGuide.vue";
import { ref, onMounted } from 'vue';
import { api, getApiErrorMessage } from '../api/client.js';
import { Plus, Edit2, Trash2, Save, X, Server, Layers, Info } from 'lucide-vue-next';

const lines = ref([]);
const tanks = ref([]);

const selectedLine = ref(null);

const errorMsg = ref('');
const successMsg = ref('');

// Fetch Data
const fetchLines = async () => {
  try {
    const res = await api.get('/v1/traceability-master/lines');
    lines.value = res.data.map(x => ({ ...x, _isEditing: false }));
    selectedLine.value = null;
    tanks.value = [];
  } catch (err) {
    errorMsg.value = getApiErrorMessage(err);
  }
};

const fetchTanks = async (lineId) => {
  if (!lineId) return;
  try {
    const res = await api.get(`/v1/traceability-master/tanks?lineId=${lineId}`);
    tanks.value = res.data.map(x => ({ ...x, _isEditing: false }));
  } catch (err) {
    errorMsg.value = getApiErrorMessage(err);
  }
};

onMounted(() => {
  fetchLines();
});

// Selection Handlers
const selectLine = (line) => {
  if (line._isEditing || line.id === 0) return;
  selectedLine.value = line;
  fetchTanks(line.id);
};

// --- CRUD Generic Helpers ---
const addNewItem = (list, defaultObj) => {
  list.unshift({ ...defaultObj, id: 0, _isEditing: true });
};

const saveItem = async (type, item) => {
  errorMsg.value = '';
  successMsg.value = '';
  try {
    let res;
    const url = `/v1/traceability-master/${type}`;
    if (item.id === 0) {
      res = await api.post(url, item);
      successMsg.value = '新增成功';
    } else {
      res = await api.put(`${url}/${item.id}`, item);
      successMsg.value = '更新成功';
    }
    Object.assign(item, res.data);
    item._isEditing = false;
  } catch (err) {
    errorMsg.value = getApiErrorMessage(err);
  }
};

const deleteItem = async (type, list, index, id) => {
  if (id === 0) {
    list.splice(index, 1);
    return;
  }
  if (!confirm('確定要刪除此筆資料嗎？')) return;
  try {
    await api.delete(`/v1/traceability-master/${type}/${id}`);
    list.splice(index, 1);
    successMsg.value = '刪除成功';
    if (type === 'lines' && selectedLine.value?.id === id) {
      selectedLine.value = null; tanks.value = [];
    }
  } catch (err) {
    errorMsg.value = getApiErrorMessage(err);
  }
};

const cancelEdit = (list, item, index) => {
  if (item.id === 0) list.splice(index, 1);
  else item._isEditing = false;
};
</script>

<template>
  <div class="h-full flex flex-col p-6 max-w-7xl mx-auto space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
    <header class="flex justify-between items-center bg-white dark:bg-slate-800 p-6 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-700">
      <div>
        <h1 class="text-2xl font-black text-slate-800 dark:text-slate-100 flex items-center gap-3">
          <Layers class="w-8 h-8 text-blue-500" />
          線別槽體設定
        </h1>
        <p class="text-slate-500 text-sm mt-1">設定每個線別所包含的藥水槽體</p>
      </div>
      
      <div v-if="errorMsg" class="bg-red-50 text-red-600 px-4 py-2 rounded-lg text-sm font-bold border border-red-200">{{ errorMsg }}</div>
      <div v-if="successMsg" class="bg-emerald-50 text-emerald-600 px-4 py-2 rounded-lg text-sm font-bold border border-emerald-200">{{ successMsg }}</div>
    </header>

    <!-- Guide / Wizard Tip -->
    <ModuleGuide title="模組指南：線別槽體設定">
      <p class="text-xs text-blue-700 dark:text-blue-400/80 mt-1.5 leading-relaxed">
          此頁面用來設定各線別內有哪些藥水槽體。SPC 資料匯入與藥液管制項目會依照「線別 → 槽體」對應到實際生產位置。<br/>
          💡 <strong>操作建議：</strong> 請先到「工站製程主檔」建立製程，再於此處選取製程並設定槽體。
        </p>
        <div class="mt-3 space-y-1.5 text-xs text-blue-700 dark:text-blue-400/80 leading-relaxed">
          <div class="font-black text-blue-900 dark:text-blue-300">線別槽體設定操作說明</div>
          <p><strong>線別來源：</strong>左側直接顯示「工站製程主檔」，不需要在本頁重複建立線別。</p>
          <p><strong>建立槽體：</strong>先選取工站製程，再於右側新增槽體；代碼由系統自動產生。</p>
          <p><strong>顯示順序：</strong>製程沿用工站製程主檔順序；槽體依各槽體的排列順序顯示。</p>
        </div>
    </ModuleGuide>

    <div class="grid grid-cols-1 md:grid-cols-2 gap-6 flex-1 min-h-[500px]">
      
      <!-- Lines Column -->
      <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-700 flex flex-col overflow-hidden">
        <div class="p-4 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/50 flex justify-between items-center">
          <div>
            <h2 class="font-bold flex items-center gap-2"><Server class="w-5 h-5 text-blue-500" /> 1. 工站製程</h2>
            <p class="text-[10px] text-slate-400 mt-1">資料來源：工站製程主檔</p>
          </div>
        </div>
        <div class="flex-1 overflow-y-auto p-3 space-y-2">
          <div v-for="(line, idx) in lines" :key="line.id" 
               @click="selectLine(line)"
               :class="['p-3 rounded-xl border transition-all cursor-pointer', selectedLine?.id === line.id ? 'border-blue-500 bg-blue-50/50 dark:bg-blue-900/20 shadow-sm' : 'border-slate-200 dark:border-slate-700 hover:border-blue-300 bg-white dark:bg-slate-800']">
            <div class="flex justify-between items-center">
              <div>
                <div class="font-bold text-slate-800 dark:text-slate-100">{{ line.lineCode }}</div>
                <div class="text-xs text-slate-500">{{ line.lineName || '---' }}</div>
                <div v-if="line.lineNameEn" class="text-[10px] text-cyan-600 dark:text-cyan-400">{{ line.lineNameEn }}</div>
              </div>
              <span :class="line.isActive ? 'text-emerald-500' : 'text-slate-400'" class="text-[10px] font-bold">{{ line.isActive ? '啟用' : '停用' }}</span>
            </div>
          </div>
          <div v-if="lines.length === 0" class="text-center p-6 text-slate-400 text-sm">無資料</div>
        </div>
      </div>

      <!-- Tanks Column -->
      <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-700 flex flex-col overflow-hidden relative">
        <div v-if="!selectedLine" class="absolute inset-0 bg-slate-50/80 dark:bg-slate-900/80 backdrop-blur-[2px] flex items-center justify-center z-10">
          <p class="text-slate-400 font-bold">請先選擇一個線別</p>
        </div>
        <div class="p-4 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/50 flex justify-between items-center">
          <h2 class="font-bold flex items-center gap-2"><Layers class="w-5 h-5 text-indigo-500" /> 2. 槽體 (Tanks)</h2>
          <button @click="addNewItem(tanks, { tankCode: '', tankName: '', tankNameEn: '', sequenceNo: 0, isActive: true, lineId: selectedLine?.id })" class="p-1.5 bg-indigo-100 text-indigo-600 hover:bg-indigo-200 rounded-md transition-colors"><Plus class="w-4 h-4" /></button>
        </div>
        <div class="flex-1 overflow-y-auto p-3 space-y-2">
          <div v-for="(tank, idx) in tanks" :key="tank.id" 
               class="p-3 rounded-xl border border-slate-200 dark:border-slate-700 hover:border-indigo-300 bg-white dark:bg-slate-800 transition-all">
            <div v-if="tank._isEditing" class="space-y-2" @click.stop>
              <input :value="tank.id ? tank.tankCode : '儲存後自動產生'" readonly class="w-full text-sm p-1.5 border rounded bg-slate-100 text-slate-500 font-mono" />
              <input v-model="tank.tankName" required placeholder="槽體中文名稱（必填）" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-indigo-500 outline-none" />
              <input v-model="tank.tankNameEn" placeholder="槽體英文名稱（選填）" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-indigo-500 outline-none" />
              <input v-model.number="tank.sequenceNo" type="number" min="0" step="1" placeholder="排列順序" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-indigo-500 outline-none" />
              <label class="flex items-center gap-2 text-xs text-slate-500 cursor-pointer"><input type="checkbox" v-model="tank.isActive" /> 啟用</label>
              <div class="flex justify-end gap-2 pt-2">
                <button @click="cancelEdit(tanks, tank, idx)" class="p-1 text-slate-400 hover:text-slate-600"><X class="w-4 h-4" /></button>
                <button @click="saveItem('tanks', tank)" class="p-1 text-emerald-500 hover:text-emerald-700"><Save class="w-4 h-4" /></button>
              </div>
            </div>
            <div v-else class="flex justify-between items-center">
              <div>
                <div class="font-bold text-slate-800 dark:text-slate-100">{{ tank.tankCode }}</div>
                <div class="text-xs text-slate-500">{{ tank.tankName || '---' }}</div>
                <div v-if="tank.tankNameEn" class="text-[10px] text-cyan-600 dark:text-cyan-400">{{ tank.tankNameEn }}</div>
                <div class="text-[10px] text-indigo-500 font-bold">順序 {{ tank.sequenceNo ?? 0 }}</div>
              </div>
              <div class="flex gap-1" @click.stop>
                <button @click="tank._isEditing = true" class="p-1.5 text-slate-400 hover:text-indigo-500 rounded"><Edit2 class="w-3.5 h-3.5" /></button>
                <button @click="deleteItem('tanks', tanks, idx, tank.id)" class="p-1.5 text-slate-400 hover:text-red-500 rounded"><Trash2 class="w-3.5 h-3.5" /></button>
              </div>
            </div>
          </div>
          <div v-if="tanks.length === 0" class="text-center p-6 text-slate-400 text-sm">無資料</div>
        </div>
      </div>

    </div>
  </div>
</template>
