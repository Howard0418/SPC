<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { api, getApiErrorMessage } from '../api/client'

const route = useRoute()
const router = useRouter()
const status = ref(null)
const sourceId = ref('')
const equipmentId = ref('')
const result = ref(null)
const error = ref('')
const search = ref(String(route.query.q || ''))
const allowedRoles = ['RUNNING', 'ALARM', 'none']
const role = ref(allowedRoles.includes(String(route.query.role)) ? String(route.query.role) : 'all')

const sources = computed(() => status.value?.sources || [])
const equipments = computed(() => sources.value.find(source => source.sourceId === sourceId.value)?.devices?.map(device => device.equipmentId) || [])
const points = computed(() => result.value?.points || [])
const filteredPoints = computed(() => points.value.filter(point => {
  const keyword = search.value.trim().toLowerCase()
  const matchesText = !keyword || `${point.channelId} ${point.displayName || ''} ${point.category || ''}`.toLowerCase().includes(keyword)
  const matchesRole = role.value === 'all' || (role.value === 'none' ? !point.statusRole : point.statusRole === role.value)
  return matchesText && matchesRole
}))

function syncRoute() {
  if (!sourceId.value || !equipmentId.value) return
  router.replace({
    path: '/equipment-points',
    query: {
      sourceId: sourceId.value,
      equipmentId: equipmentId.value,
      ...(role.value === 'all' ? {} : { role: role.value }),
      ...(search.value.trim() ? { q: search.value.trim() } : {}),
    },
  })
}

async function load() {
  error.value = ''
  if (!sourceId.value || !equipmentId.value) {
    result.value = null
    return
  }
  syncRoute()
  try {
    result.value = (await api.get('/equipment-points', { params: { sourceId: sourceId.value, equipmentId: equipmentId.value } })).data
  } catch (exception) {
    error.value = getApiErrorMessage(exception)
  }
}

async function init() {
  error.value = ''
  try {
    status.value = (await api.get('/equipment-status')).data
    const requestedSource = String(route.query.sourceId || '')
    sourceId.value = sources.value.some(source => source.sourceId === requestedSource) ? requestedSource : (sources.value[0]?.sourceId || '')
    const requestedEquipment = String(route.query.equipmentId || '')
    equipmentId.value = equipments.value.includes(requestedEquipment) ? requestedEquipment : (equipments.value[0] || '')
    await load()
  } catch (exception) {
    error.value = getApiErrorMessage(exception)
  }
}

async function save(point) {
  error.value = ''
  try {
    await api.put('/equipment-points/mapping', {
      sourceId: sourceId.value,
      equipmentId: equipmentId.value,
      channelId: point.channelId,
      displayName: point.displayName,
      unit: point.unit,
      isEnabled: true,
      isPinned: point.isPinned,
      warningLow: point.warningLow,
      warningHigh: point.warningHigh,
      sortOrder: 0,
      statusRole: point.statusRole || null,
      activeWhen: point.activeWhen || 'NONZERO',
    })
    await load()
  } catch (exception) {
    error.value = getApiErrorMessage(exception)
  }
}

function switchSource() {
  equipmentId.value = equipments.value[0] || ''
  load()
}

onMounted(init)
</script>

<template>
  <section class="space-y-4">
    <h1 class="text-2xl font-black">設備點位總覽</h1>
    <p class="text-sm text-slate-500">每台設備可指定一個「運轉狀態」點位，並可指定多個「告警訊號」點位；NC 接點請選擇「等於 0」。</p>
    <p v-if="error" class="text-red-600">{{ error }}</p>
    <div class="flex flex-wrap gap-2">
      <select v-model="sourceId" @change="switchSource"><option v-for="source in sources" :key="source.sourceId" :value="source.sourceId">{{ source.displayName }}</option></select>
      <select v-model="equipmentId" @change="load"><option v-for="equipment in equipments" :key="equipment">{{ equipment }}</option></select>
      <button @click="load">重新讀取</button>
      <input v-model="search" class="min-w-64" placeholder="搜尋名稱、ID 或分類" @change="syncRoute">
      <select v-model="role" @change="syncRoute"><option value="all">全部用途</option><option value="RUNNING">運轉狀態</option><option value="ALARM">告警訊號</option><option value="none">一般點位</option></select>
      <span class="self-center text-sm text-slate-500">{{ filteredPoints.length }} / {{ points.length }} 點</span>
    </div>
    <table>
      <tr><th>名稱</th><th>ID</th><th>值</th><th>狀態用途</th><th>成立條件</th><th>監控</th><th>警戒低/高</th><th></th></tr>
      <tr v-for="point in filteredPoints" :key="point.channelId">
        <td><input v-model="point.displayName"></td><td>{{ point.channelId }}</td><td>{{ point.value }}</td>
        <td><select v-model="point.statusRole"><option :value="null">一般點位</option><option value="RUNNING">運轉狀態</option><option value="ALARM">告警訊號</option></select></td>
        <td><select v-model="point.activeWhen" :disabled="!point.statusRole"><option value="NONZERO">非 0</option><option value="ZERO">等於 0（NC）</option></select></td>
        <td><input v-model="point.isPinned" type="checkbox">釘選</td>
        <td><input v-model.number="point.warningLow" placeholder="低"><input v-model.number="point.warningHigh" placeholder="高"></td>
        <td><button @click="save(point)">儲存</button></td>
      </tr>
    </table>
    <p v-if="!filteredPoints.length" class="p-6 text-center text-slate-500">沒有符合條件的點位。</p>
  </section>
</template>

<style scoped>
table { width: 100%; background: white; }
th, td { padding: .6rem; border-bottom: 1px solid #ddd; text-align: left; }
input, select, button { border: 1px solid #aaa; border-radius: .3rem; padding: .35rem; }
button { color: #075985; font-weight: bold; }
select:disabled { opacity: .5; }
</style>
