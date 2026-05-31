<script setup>
import { ref, onMounted } from 'vue';
import { api, getApiErrorMessage } from '../api/client.js';
import { Plus, Edit2, Trash2, Save, X, Server, Layers, Box } from 'lucide-vue-next';

const lines = ref([]);
const tanks = ref([]);
const slots = ref([]);

const selectedLine = ref(null);
const selectedTank = ref(null);

const errorMsg = ref('');
const successMsg = ref('');

// Fetch Data
const fetchLines = async () => {
  try {
    const res = await api.get('/v1/traceability-master/lines');
    lines.value = res.data.map(x => ({ ...x, _isEditing: false }));
    selectedLine.value = null;
    tanks.value = [];
    slots.value = [];
  } catch (err) {
    errorMsg.value = getApiErrorMessage(err);
  }
};

const fetchTanks = async (lineId) => {
  if (!lineId) return;
  try {
    const res = await api.get(`/v1/traceability-master/tanks?lineId=${lineId}`);
    tanks.value = res.data.map(x => ({ ...x, _isEditing: false }));
    selectedTank.value = null;
    slots.value = [];
  } catch (err) {
    errorMsg.value = getApiErrorMessage(err);
  }
};

const fetchSlots = async (tankId) => {
  if (!tankId) return;
  try {
    const res = await api.get(`/v1/traceability-master/slots?tankId=${tankId}`);
    slots.value = res.data.map(x => ({ ...x, _isEditing: false }));
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

const selectTank = (tank) => {
  if (tank._isEditing || tank.id === 0) return;
  selectedTank.value = tank;
  fetchSlots(tank.id);
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
      selectedLine.value = null; tanks.value = []; slots.value = [];
    }
    if (type === 'tanks' && selectedTank.value?.id === id) {
      selectedTank.value = null; slots.value = [];
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
          追溯主檔設定 (Traceability Config)
        </h1>
        <p class="text-slate-500 text-sm mt-1">管理產線 (Line)、槽體 (Tank) 與槽位 (Slot) 的連動關係</p>
      </div>
      
      <div v-if="errorMsg" class="bg-red-50 text-red-600 px-4 py-2 rounded-lg text-sm font-bold border border-red-200">{{ errorMsg }}</div>
      <div v-if="successMsg" class="bg-emerald-50 text-emerald-600 px-4 py-2 rounded-lg text-sm font-bold border border-emerald-200">{{ successMsg }}</div>
    </header>

    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 flex-1 min-h-[500px]">
      
      <!-- Lines Column -->
      <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-700 flex flex-col overflow-hidden">
        <div class="p-4 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/50 flex justify-between items-center">
          <h2 class="font-bold flex items-center gap-2"><Server class="w-5 h-5 text-blue-500" /> 1. 產線 (Lines)</h2>
          <button @click="addNewItem(lines, { lineCode: '', lineName: '', isActive: true, factoryId: 1 })" class="p-1.5 bg-blue-100 text-blue-600 hover:bg-blue-200 rounded-md transition-colors"><Plus class="w-4 h-4" /></button>
        </div>
        <div class="flex-1 overflow-y-auto p-3 space-y-2">
          <div v-for="(line, idx) in lines" :key="line.id" 
               @click="selectLine(line)"
               :class="['p-3 rounded-xl border transition-all cursor-pointer', selectedLine?.id === line.id ? 'border-blue-500 bg-blue-50/50 dark:bg-blue-900/20 shadow-sm' : 'border-slate-200 dark:border-slate-700 hover:border-blue-300 bg-white dark:bg-slate-800']">
            <div v-if="line._isEditing" class="space-y-2" @click.stop>
              <input v-model="line.lineCode" placeholder="Line Code" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-blue-500 outline-none" />
              <input v-model="line.lineName" placeholder="Line Name" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-blue-500 outline-none" />
              <label class="flex items-center gap-2 text-xs text-slate-500 cursor-pointer"><input type="checkbox" v-model="line.isActive" /> 啟用</label>
              <div class="flex justify-end gap-2 pt-2">
                <button @click="cancelEdit(lines, line, idx)" class="p-1 text-slate-400 hover:text-slate-600"><X class="w-4 h-4" /></button>
                <button @click="saveItem('lines', line)" class="p-1 text-emerald-500 hover:text-emerald-700"><Save class="w-4 h-4" /></button>
              </div>
            </div>
            <div v-else class="flex justify-between items-center">
              <div>
                <div class="font-bold text-slate-800 dark:text-slate-100">{{ line.lineCode }}</div>
                <div class="text-xs text-slate-500">{{ line.lineName || '---' }}</div>
              </div>
              <div class="flex gap-1" @click.stop>
                <button @click="line._isEditing = true" class="p-1.5 text-slate-400 hover:text-blue-500 rounded"><Edit2 class="w-3.5 h-3.5" /></button>
                <button @click="deleteItem('lines', lines, idx, line.id)" class="p-1.5 text-slate-400 hover:text-red-500 rounded"><Trash2 class="w-3.5 h-3.5" /></button>
              </div>
            </div>
          </div>
          <div v-if="lines.length === 0" class="text-center p-6 text-slate-400 text-sm">無資料</div>
        </div>
      </div>

      <!-- Tanks Column -->
      <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-700 flex flex-col overflow-hidden relative">
        <div v-if="!selectedLine" class="absolute inset-0 bg-slate-50/80 dark:bg-slate-900/80 backdrop-blur-[2px] flex items-center justify-center z-10">
          <p class="text-slate-400 font-bold">請先選擇一條產線</p>
        </div>
        <div class="p-4 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/50 flex justify-between items-center">
          <h2 class="font-bold flex items-center gap-2"><Layers class="w-5 h-5 text-indigo-500" /> 2. 槽體 (Tanks)</h2>
          <button @click="addNewItem(tanks, { tankCode: '', tankName: '', isActive: true, lineId: selectedLine?.id })" class="p-1.5 bg-indigo-100 text-indigo-600 hover:bg-indigo-200 rounded-md transition-colors"><Plus class="w-4 h-4" /></button>
        </div>
        <div class="flex-1 overflow-y-auto p-3 space-y-2">
          <div v-for="(tank, idx) in tanks" :key="tank.id" 
               @click="selectTank(tank)"
               :class="['p-3 rounded-xl border transition-all cursor-pointer', selectedTank?.id === tank.id ? 'border-indigo-500 bg-indigo-50/50 dark:bg-indigo-900/20 shadow-sm' : 'border-slate-200 dark:border-slate-700 hover:border-indigo-300 bg-white dark:bg-slate-800']">
            <div v-if="tank._isEditing" class="space-y-2" @click.stop>
              <input v-model="tank.tankCode" placeholder="Tank Code" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-indigo-500 outline-none" />
              <input v-model="tank.tankName" placeholder="Tank Name" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-indigo-500 outline-none" />
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

      <!-- Slots Column -->
      <div class="bg-white dark:bg-slate-800 rounded-2xl shadow-sm border border-slate-200 dark:border-slate-700 flex flex-col overflow-hidden relative">
        <div v-if="!selectedTank" class="absolute inset-0 bg-slate-50/80 dark:bg-slate-900/80 backdrop-blur-[2px] flex items-center justify-center z-10">
          <p class="text-slate-400 font-bold">請先選擇一個槽體</p>
        </div>
        <div class="p-4 border-b border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-900/50 flex justify-between items-center">
          <h2 class="font-bold flex items-center gap-2"><Box class="w-5 h-5 text-emerald-500" /> 3. 槽位 (Slots)</h2>
          <button @click="addNewItem(slots, { slotCode: '', slotName: '', isActive: true, tankId: selectedTank?.id })" class="p-1.5 bg-emerald-100 text-emerald-600 hover:bg-emerald-200 rounded-md transition-colors"><Plus class="w-4 h-4" /></button>
        </div>
        <div class="flex-1 overflow-y-auto p-3 space-y-2">
          <div v-for="(slot, idx) in slots" :key="slot.id" 
               class="p-3 rounded-xl border border-slate-200 dark:border-slate-700 bg-white dark:bg-slate-800">
            <div v-if="slot._isEditing" class="space-y-2">
              <input v-model="slot.slotCode" placeholder="Slot Code" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-emerald-500 outline-none" />
              <input v-model="slot.slotName" placeholder="Slot Name" class="w-full text-sm p-1.5 border rounded focus:ring-2 focus:ring-emerald-500 outline-none" />
              <label class="flex items-center gap-2 text-xs text-slate-500 cursor-pointer"><input type="checkbox" v-model="slot.isActive" /> 啟用</label>
              <div class="flex justify-end gap-2 pt-2">
                <button @click="cancelEdit(slots, slot, idx)" class="p-1 text-slate-400 hover:text-slate-600"><X class="w-4 h-4" /></button>
                <button @click="saveItem('slots', slot)" class="p-1 text-emerald-500 hover:text-emerald-700"><Save class="w-4 h-4" /></button>
              </div>
            </div>
            <div v-else class="flex justify-between items-center">
              <div>
                <div class="font-bold text-slate-800 dark:text-slate-100">{{ slot.slotCode }}</div>
                <div class="text-xs text-slate-500">{{ slot.slotName || '---' }}</div>
              </div>
              <div class="flex gap-1">
                <button @click="slot._isEditing = true" class="p-1.5 text-slate-400 hover:text-emerald-500 rounded"><Edit2 class="w-3.5 h-3.5" /></button>
                <button @click="deleteItem('slots', slots, idx, slot.id)" class="p-1.5 text-slate-400 hover:text-red-500 rounded"><Trash2 class="w-3.5 h-3.5" /></button>
              </div>
            </div>
          </div>
          <div v-if="slots.length === 0" class="text-center p-6 text-slate-400 text-sm">無資料</div>
        </div>
      </div>

    </div>
  </div>
</template>
