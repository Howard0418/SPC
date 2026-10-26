<script setup>
import { useI18n } from "vue-i18n";
import { displayUiMessage } from "../i18n/uiMessage";
const { t } = useI18n({ useScope: "global" });
import { ref, computed, watch, onBeforeUnmount } from 'vue';
import { api, getApiErrorDisplay } from '../api/client';
const invalidResponseError = new Error('Invalid calculation evidence response');
const props=defineProps({ppcId:{type:[Number,String],required:true},segmentId:{type:Number,default:null},measurementId:{type:Number,default:null},pointMode:Boolean,aggregate:Boolean});
const data=ref(null),busy=ref(false),error=ref(''),skip=ref(0),traceSkip=ref(0),selected=ref(null);
const id=computed(()=>props.measurementId||selected.value);let generation=0;
onBeforeUnmount(()=>generation++);
const date=value=>value?new Date(value).toLocaleString('zh-TW',{timeZone:'Asia/Taipei',hour12:false}):t('spc.calculationEvidence.noEnd');
const status=value=>({Completed:t('spc.calculationEvidence.completed'),Pending:t('spc.calculationEvidence.pending'),Failed:t('spc.calculationEvidence.failed'),Processing:t('spc.calculationEvidence.processing'),NotRecorded:t('spc.calculationEvidence.noStatistics')}[value]||t('spc.calculationEvidence.unknownStatistics'));
function formula(json){try{const v=JSON.parse(json);const read=key=>v[key]??v[Object.keys(v).find(k=>k.toLowerCase()===key.toLowerCase())];return [[t('spc.calculationEvidence.concentration'),read('concentrationFormula')],[t('spc.calculationEvidence.adjustment'),read('adjustmentFormula')],[t('spc.calculationEvidence.amount'),read('adjustmentAmountFormula')]].filter(([,v])=>v).map(([k,v])=>`${k}：${v}`).join('\n');}catch{return t('spc.calculationEvidence.invalidFormula');}}
async function load(){
 const request=++generation;data.value=null;error.value='';busy.value=false;
 if(props.aggregate || (props.pointMode&&!id.value))return;
 busy.value=true;
 try{
  const response=await api.get(`/v1/part-process-characteristics/${props.ppcId}/chemical-calculation-traces`,{params:{segmentId:id.value?undefined:props.segmentId??undefined,measurementId:id.value||undefined,skip:id.value?0:skip.value,traceSkip:id.value?traceSkip.value:0}});
  const payload=response.data.data;
  if(!payload||typeof payload.applicable!=="boolean"||!Array.isArray(payload.rows))throw invalidResponseError;
  if(request===generation)data.value=payload;
 }catch(e){if(request===generation)error.value=e === invalidResponseError ? { key: 'spc.calculationEvidence.invalidResponse' } : getApiErrorDisplay(e);}
 finally{if(request===generation)busy.value=false;}
}
function history(measurementId){selected.value=measurementId;traceSkip.value=0;load();}
function back(){selected.value=null;traceSkip.value=0;load();}
function page(amount){if(id.value)traceSkip.value=Math.max(0,traceSkip.value+amount);else skip.value=Math.max(0,skip.value+amount);load();}
watch(()=>[props.ppcId,props.segmentId,props.measurementId,props.pointMode,props.aggregate],()=>{skip.value=0;traceSkip.value=0;selected.value=null;load();},{immediate:true});
</script>
<template>
 <section data-testid="calculation-evidence" :aria-label="t('spc.calculationEvidence.title')" class="rounded border border-emerald-300 dark:border-emerald-800 p-3 space-y-2 text-sm break-words">
  <h4 class="font-bold">{{ id ? t('spc.calculationEvidence.measurementTitle') : t('spc.calculationEvidence.rangeTitle') }}</h4>
  <p class="text-xs">{{ t('spc.calculationEvidence.notice') }}</p>
  <p v-if="aggregate" class="text-amber-700 dark:text-amber-300">{{ t('spc.calculationEvidence.aggregate') }}</p>
  <p v-else-if="pointMode&&!id" class="text-amber-700 dark:text-amber-300">{{ t('spc.calculationEvidence.noId') }}</p>
  <p v-else-if="busy" role="status">{{ t('spc.calculationEvidence.loading') }}</p>
  <p v-else-if="error" role="alert">{{ displayUiMessage(t, error) }} <button class="underline" type="button" @click="load">{{ t('spc.calculationEvidence.retry') }}</button></p>
  <template v-else-if="data">
   <p v-if="!data.applicable">{{ t('spc.calculationEvidence.notApplicable') }}</p>
   <template v-else>
    <button v-if="selected&&!pointMode" type="button" class="underline" @click="back">{{ t('spc.calculationEvidence.back') }}</button>
    <p v-if="!id" class="text-xs">{{ t('spc.calculationEvidence.rangeCount', { total: data.totalMeasurements, unknown: data.unknownMeasurements }) }}</p>
    <p v-if="data.multipleFormulaVersions" class="text-amber-700 dark:text-amber-300">{{ t('spc.calculationEvidence.multiple') }}</p>
    <p v-if="!data.rows.length">{{ t('spc.calculationEvidence.empty') }}</p>
    <article v-for="row in data.rows" :key="row.measurementId" class="border-t border-slate-200 dark:border-slate-700 pt-2 space-y-2">
     <h5 class="font-bold">{{ t('spc.calculationEvidence.measurement', { id: row.measurementId, date: date(row.measuredAt), phase: row.samplingPhase, stage: row.samplingStage }) }}</h5>
     <p>{{ t('spc.calculationEvidence.stored', { initial: row.initialStored, recheck: row.recheckStored ?? t('spc.calculationEvidence.none'), status: status(row.statisticsStatus) }) }}</p>
     <p v-if="row.status==='Unknown'" class="text-amber-700 dark:text-amber-300">{{ t('spc.calculationEvidence.unknown') }}</p>
     <p v-else-if="row.status==='Incomplete'" class="text-amber-700 dark:text-amber-300">{{ t('spc.calculationEvidence.incomplete') }}</p>
     <button v-if="!id&&row.calculations.length" type="button" class="underline" @click="history(row.measurementId)">{{ t('spc.calculationEvidence.allHistory', { id: row.measurementId }) }}</button>
     <details v-for="calc in row.calculations" :key="`${calc.phase}-${calc.phaseVersion}`" class="rounded bg-emerald-50 dark:bg-emerald-950 p-2">
      <summary class="cursor-pointer">{{ t('spc.calculationEvidence.trace', { phase: calc.phase === 'Initial' ? t('spc.calculationEvidence.initial') : t('spc.calculationEvidence.recheck'), version: calc.phaseVersion, current: calc.isCurrent ? t('spc.calculationEvidence.current') : t('spc.calculationEvidence.historical'), evidence: calc.evidenceStatus === 'Recorded' ? t('spc.calculationEvidence.formulaVersion', { version: calc.formulaVersionNo, id: calc.formulaVersionId }) : t('spc.calculationEvidence.unconfirmed') }) }}</summary>
      <template v-if="calc.evidenceStatus==='Recorded'">
       <p>{{ t('spc.calculationEvidence.measuredAt', { date: date(calc.measuredAtUtc) }) }}</p>
       <p>{{ t('spc.calculationEvidence.effective', { start: date(calc.effectiveFromUtc), end: date(calc.effectiveUntilUtcExclusive), revision: calc.revisionId }) }}</p>
       <p>{{ t('spc.calculationEvidence.reason', { reason: calc.reason || t('spc.calculationEvidence.notRecorded') }) }}</p>
       <pre class="whitespace-pre-wrap break-words text-xs">{{ formula(calc.formulaConfigJson) }}</pre>
       <p>{{ t('spc.calculationEvidence.inputs', { primary: calc.primary, secondary: calc.secondary ?? t('spc.calculationEvidence.none') }) }}</p>
       <p>{{ t('spc.calculationEvidence.results', { raw: calc.rawResult, stored: calc.storedResult, judgment: calc.judgment }) }}</p>
       <p>{{ t('spc.calculationEvidence.fHistory', { id: calc.fTableHistoryId ?? t('spc.calculationEvidence.noF') }) }}</p>
       <p v-for="cell in calc.fCells||[]" :key="cell.address">{{ t('spc.calculationEvidence.cell', { address: cell.address, value: cell.numericValue ?? cell.textValue }) }}</p>
       <p v-if="calc.specification">{{ t('spc.calculationEvidence.specification', { segment: calc.specification.segmentId ?? t('spc.calculationEvidence.master'), lsl: calc.specification.lsl ?? t('spc.calculationEvidence.none'), usl: calc.specification.usl ?? t('spc.calculationEvidence.none'), target: calc.specification.target ?? t('spc.calculationEvidence.none') }) }}</p>
       <p class="text-xs">{{ t('spc.calculationEvidence.actor', { actor: calc.actor, engine: calc.engineVersion, precision: calc.precisionPolicy }) }}</p>
      </template>
     </details>
    </article>
    <div class="flex flex-wrap gap-3">
     <button type="button" class="underline disabled:opacity-40" :disabled="(id?traceSkip:skip)===0" @click="page(-20)">{{ id ? t('spc.calculationEvidence.tracePrevious') : t('spc.calculationEvidence.measurementPrevious') }}</button>
     <span>{{ t('spc.calculationEvidence.page', { page: (id ? traceSkip : skip)/20+1 }) }}</span>
     <button type="button" class="underline disabled:opacity-40" :disabled="id?!data.hasMoreTrace:!data.hasMore" @click="page(20)">{{ id ? t('spc.calculationEvidence.traceNext') : t('spc.calculationEvidence.measurementNext') }}</button>
    </div>
   </template>
  </template>
 </section>
</template>
