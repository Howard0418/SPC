<script setup>
import { ref, computed, watch, onBeforeUnmount } from 'vue';
import { api, getApiErrorMessage } from '../api/client';
const props=defineProps({ppcId:{type:[Number,String],required:true},segmentId:{type:Number,default:null},measurementId:{type:Number,default:null},pointMode:Boolean,aggregate:Boolean});
const data=ref(null),busy=ref(false),error=ref(''),skip=ref(0),traceSkip=ref(0),selected=ref(null);
const id=computed(()=>props.measurementId||selected.value);let generation=0;
onBeforeUnmount(()=>generation++);
const date=value=>value?new Date(value).toLocaleString('zh-TW',{timeZone:'Asia/Taipei',hour12:false}):'無迄日';
const status=value=>({Completed:'統計完成',Pending:'資料已落地，統計待處理',Failed:'統計失敗，等待重試',Processing:'統計處理中',NotRecorded:'無已保存統計紀錄'}[value]||'無法確認統計狀態');
function formula(json){try{const v=JSON.parse(json);const read=key=>v[key]??v[Object.keys(v).find(k=>k.toLowerCase()===key.toLowerCase())];return [['濃度',read('concentrationFormula')],['調整',read('adjustmentFormula')],['添加量',read('adjustmentAmountFormula')]].filter(([,v])=>v).map(([k,v])=>`${k}：${v}`).join('\n');}catch{return '公式快照無法解析';}}
async function load(){
 const request=++generation;data.value=null;error.value='';busy.value=false;
 if(props.aggregate || (props.pointMode&&!id.value))return;
 busy.value=true;
 try{
  const response=await api.get(`/v1/part-process-characteristics/${props.ppcId}/chemical-calculation-traces`,{params:{segmentId:id.value?undefined:props.segmentId??undefined,measurementId:id.value||undefined,skip:id.value?0:skip.value,traceSkip:id.value?traceSkip.value:0}});
  const payload=response.data.data;
  if(!payload||typeof payload.applicable!=="boolean"||!Array.isArray(payload.rows))throw new Error("計算證據回應格式不符，請稍後重試。");
  if(request===generation)data.value=payload;
 }catch(e){if(request===generation)error.value=getApiErrorMessage(e);}
 finally{if(request===generation)busy.value=false;}
}
function history(measurementId){selected.value=measurementId;traceSkip.value=0;load();}
function back(){selected.value=null;traceSkip.value=0;load();}
function page(amount){if(id.value)traceSkip.value=Math.max(0,traceSkip.value+amount);else skip.value=Math.max(0,skip.value+amount);load();}
watch(()=>[props.ppcId,props.segmentId,props.measurementId,props.pointMode,props.aggregate],()=>{skip.value=0;traceSkip.value=0;selected.value=null;load();},{immediate:true});
</script>
<template>
 <section aria-label="實際公式計算證據" class="rounded border border-emerald-300 dark:border-emerald-800 p-3 space-y-2 text-sm break-words">
  <h4 class="font-bold">{{ id ? '此量測已保存的公式計算歷程' : '區間量測與實際公式' }}</h4>
  <p class="text-xs">只讀取已保存證據，不重新計算。當時計算判定與目前圖表可能不同，請核對規格與取位。規格為計算時快照；獨立規格版號與管制界限當時版本尚無紀錄。</p>
  <p v-if="aggregate" class="text-amber-700 dark:text-amber-300">此點為多筆子組，未提供全部成員識別，無法確認子組單一公式版本；請由日期分段逐筆查閱，不以第一筆代表整個子組。</p>
  <p v-else-if="pointMode&&!id" class="text-amber-700 dark:text-amber-300">此點為衍生統計值或未提供量測識別，無法指定單筆公式；請由日期分段逐筆查閱。</p>
  <p v-else-if="busy" role="status">載入實際計算證據中…</p>
  <p v-else-if="error" role="alert">{{ error }} <button class="underline" type="button" @click="load">重試計算證據</button></p>
  <template v-else-if="data">
   <p v-if="!data.applicable">非藥液資料，分析公式追溯不適用。</p>
   <template v-else>
    <button v-if="selected&&!pointMode" type="button" class="underline" @click="back">返回區間量測</button>
    <p v-if="!id" class="text-xs">此PPC日期範圍全部量測 {{ data.totalMeasurements }} 筆（不侷限圖表批次篩選）；未保存版本關聯 {{ data.unknownMeasurements }} 筆。</p>
    <p v-if="data.multipleFormulaVersions" class="text-amber-700 dark:text-amber-300">此範圍保存多個公式版本，請逐筆核對，不能視為單一公式。</p>
    <p v-if="!data.rows.length">此範圍沒有量測資料；不推定公式。</p>
    <article v-for="row in data.rows" :key="row.measurementId" class="border-t border-slate-200 dark:border-slate-700 pt-2 space-y-2">
     <h5 class="font-bold">量測 #{{ row.measurementId }}｜{{ date(row.measuredAt) }}（台北）｜{{ row.samplingPhase }}／{{ row.samplingStage }}</h5>
     <p>目前儲存初驗 {{ row.initialStored }}；複驗 {{ row.recheckStored??'無' }}；{{ status(row.statisticsStatus) }}</p>
     <p v-if="row.status==='Unknown'" class="text-amber-700 dark:text-amber-300">無法確認當時版本（舊量測未保存追溯）。</p>
     <p v-else-if="row.status==='Incomplete'" class="text-amber-700 dark:text-amber-300">計算證據不完整，無法確認；不套用目前公式。</p>
     <button v-if="!id&&row.calculations.length" type="button" class="underline" @click="history(row.measurementId)">量測 #{{ row.measurementId }} 所有計算歷程</button>
     <details v-for="calc in row.calculations" :key="`${calc.phase}-${calc.phaseVersion}`" class="rounded bg-emerald-50 dark:bg-emerald-950 p-2">
      <summary class="cursor-pointer">{{ calc.phase==='Initial'?'初驗':'複驗' }}追溯 v{{ calc.phaseVersion }}（{{ calc.isCurrent?'目前值':'歷史留存' }}）｜{{ calc.evidenceStatus==='Recorded'?`公式 v${calc.formulaVersionNo}（ID ${calc.formulaVersionId}）`:'證據無法確認' }}</summary>
      <template v-if="calc.evidenceStatus==='Recorded'">
       <p>保存的量測時點 {{ date(calc.measuredAtUtc) }}（台北）</p>
       <p>適用期間 {{ date(calc.effectiveFromUtc) }} ～ {{ date(calc.effectiveUntilUtcExclusive) }}（迄點不包含）；排程 {{ calc.revisionId }}</p>
       <p>參照文件／修改依據：{{ calc.reason||'未記錄' }}</p>
       <pre class="whitespace-pre-wrap break-words text-xs">{{ formula(calc.formulaConfigJson) }}</pre>
       <p>原始輸入 Primary={{ calc.primary }}；Secondary={{ calc.secondary??'無' }}</p>
       <p>原始計算結果 {{ calc.rawResult }}；儲存結果 {{ calc.storedResult }}；當時計算規格判定 {{ calc.judgment }}</p>
       <p>F歷史快照 {{ calc.fTableHistoryId??'未使用F表' }}</p>
       <p v-for="cell in calc.fCells||[]" :key="cell.address">引用 {{ cell.address }} = {{ cell.numericValue??cell.textValue }}</p>
       <p v-if="calc.specification">計算時規格快照：分段 {{ calc.specification.segmentId??'主檔' }}；LSL {{ calc.specification.lsl??'無' }}／USL {{ calc.specification.usl??'無' }}／目標 {{ calc.specification.target??'無' }}</p>
       <p class="text-xs">計算人 {{ calc.actor }}；{{ calc.engineVersion }}；{{ calc.precisionPolicy }}</p>
      </template>
     </details>
    </article>
    <div class="flex flex-wrap gap-3">
     <button type="button" class="underline disabled:opacity-40" :disabled="(id?traceSkip:skip)===0" @click="page(-20)">{{ id?'歷程':'量測' }}上一頁</button>
     <span>第 {{ (id?traceSkip:skip)/20+1 }} 頁</span>
     <button type="button" class="underline disabled:opacity-40" :disabled="id?!data.hasMoreTrace:!data.hasMore" @click="page(20)">{{ id?'歷程':'量測' }}下一頁</button>
    </div>
   </template>
  </template>
 </section>
</template>
