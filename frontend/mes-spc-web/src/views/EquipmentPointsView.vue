<script setup>
import {useI18n} from "vue-i18n";
const {t}=useI18n({useScope:"global"});
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
    <h1 class="text-2xl font-black">{{ t('spc.equipment.pointsTitle') }}</h1>
    <p class="text-sm text-slate-500">{{ t('spc.equipment.pointsHelp') }}</p>
    <p v-if="error" class="text-red-600">{{ error }}</p>
    <div class="flex flex-wrap gap-2">
      <select data-testid="equipment-sourceId" v-model="sourceId" @change="switchSource"><option v-for="source in sources" :key="source.sourceId" :value="source.sourceId">{{ source.displayName }}</option></select>
      <select data-testid="equipment-equipmentId" v-model="equipmentId" @change="load"><option v-for="equipment in equipments" :key="equipment">{{ equipment }}</option></select>
      <button data-testid="equipment-reload" @click="load">{{ t('spc.equipment.reload') }}</button>
      <input data-testid="equipment-search" v-model="search" class="min-w-64" :placeholder="t('spc.equipment.pointSearch')" @change="syncRoute">
      <select data-testid="equipment-role" v-model="role" @change="syncRoute"><option value="all">{{ t('spc.equipment.allRoles') }}</option><option value="RUNNING">{{ t('spc.equipment.runningRole') }}</option><option value="ALARM">{{ t('spc.equipment.alarmRole') }}</option><option value="none">{{ t('spc.equipment.generalRole') }}</option></select>
      <span class="self-center text-sm text-slate-500">{{ filteredPoints.length }} / {{ points.length }} {{ t('spc.equipment.pointUnit') }}</span>
    </div>
    <div class="overflow-x-auto"><table>
      <tr><th>{{ t('spc.equipment.name') }}</th><th>ID</th><th>{{ t('spc.equipment.value') }}</th><th>{{ t('spc.equipment.statusRole') }}</th><th>{{ t('spc.equipment.activeWhen') }}</th><th>{{ t('spc.equipment.monitoring') }}</th><th>{{ t('spc.equipment.warningRange') }}</th><th></th></tr>
      <tr v-for="point in filteredPoints" :key="point.channelId">
        <td><input data-testid="equipment-point.displayName" v-model="point.displayName"></td><td>{{ point.channelId }}</td><td>{{ point.value }}</td>
        <td><select data-testid="equipment-point.statusRole" v-model="point.statusRole"><option :value="null">{{ t('spc.equipment.generalRole') }}</option><option value="RUNNING">{{ t('spc.equipment.runningRole') }}</option><option value="ALARM">{{ t('spc.equipment.alarmRole') }}</option></select></td>
        <td><select data-testid="equipment-point.activeWhen" v-model="point.activeWhen" :disabled="!point.statusRole"><option value="NONZERO">{{ t('spc.equipment.nonzero') }}</option><option value="ZERO">{{ t('spc.equipment.zero') }}</option></select></td>
        <td><input data-testid="equipment-point.isPinned" v-model="point.isPinned" type="checkbox">{{ t('spc.equipment.pin') }}</td>
        <td><input data-testid="equipment-point.warningLow" v-model.number="point.warningLow" :placeholder="t('spc.equipment.low')"><input data-testid="equipment-point.warningHigh" v-model.number="point.warningHigh" :placeholder="t('spc.equipment.high')"></td>
        <td><button data-testid="equipment-save" @click="save(point)">{{ t('spc.equipment.save') }}</button></td>
      </tr>
    </table></div>
    <p v-if="!filteredPoints.length" class="p-6 text-center text-slate-500">{{ t('spc.equipment.noPoints') }}</p>
  </section>
</template>

<style scoped>
table { width: 100%; background: white; }
th, td { padding: .6rem; border-bottom: 1px solid #ddd; text-align: left; }
input, select, button { border: 1px solid #aaa; border-radius: .3rem; padding: .35rem; }
button { color: #075985; font-weight: bold; }
select:disabled { opacity: .5; }
</style>
