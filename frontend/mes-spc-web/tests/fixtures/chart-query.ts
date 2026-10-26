import {test,expect,Page} from '@playwright/test';
export const raw='原名 <b>@:key {name}|data</b>';
export const csv='量測值,日期\r\n0,2026-10-26\r\n';
export async function prepare(page:Page,path='/spc',deep=false){
 await page.clock.install({time:new Date('2026-10-26T04:00:00Z')});await page.addInitScript(()=>{localStorage.setItem('mes_spc_token','mock-ui-only-token');localStorage.setItem('mes_spc_user',JSON.stringify({id:101,role:'Editor',username:'chart-query-fixture'}))});
 const reads:URL[]=[],writes:any[]=[];const state={failSummary:false,failChart:0,failExcluded:false,failDownload:false,empty:false,restored:false,failMappings:false,chartType:'I_MR',pValue:0.05,multi:false,violations:false,rejectWrites:false,pointIds:true,remarkExists:false,remarkText:''};
 const scopes=['PROCESS','CHEM','PRODUCT'];
 const mappings=scopes.map((scope,i)=>({id:i+1,controlScope:scope,partId:scope==='PRODUCT'?31:null,processId:21,characteristicId:i+51,chartTypeId:i+1,isEnabled:true,displayMode:'CONTROL_CHART',process:{id:21,processCode:'P-RAW',processName:raw,isEnabled:true},part:scope==='PRODUCT'?{id:31,partNo:'PART-RAW',partName:raw,isEnabled:true}:null,characteristic:{id:i+51,characteristicCode:'CHAR-RAW',characteristicName:raw,dataCategory:'Variable'},lsl:0,usl:10,targetValue:0}));
 await page.route('**/*',async route=>{
  const u=new URL(route.request().url());if(u.origin!==new URL(test.info().project.use.baseURL as string).origin)return route.abort();if(!u.pathname.startsWith('/api/'))return route.continue();const method=route.request().method();
  if(method!=='GET'){
   expect(method+' '+u.pathname).toMatch(/^(DELETE \/api\/v1\/spc\/(point-exclusions\/91|point-remarks\/201)|PUT \/api\/v1\/spc\/(point-remarks|point-exclusions)|POST \/api\/v1\/spc\/(ocap|exclude-batch\/[^/]+))$/);
   const body=method==='DELETE'?null:route.request().postData()?route.request().postDataJSON():null;writes.push({method,path:u.pathname,body});
   if(state.rejectWrites)return route.fulfill({status:409,json:{message:raw}});
   if(u.pathname.includes('/point-exclusions')&&method==='DELETE')state.restored=true;
   if(u.pathname.includes('/point-remarks')){state.remarkExists=method==='PUT';state.remarkText=body?.remark||''}
   return route.fulfill({json:{}});
  }
  reads.push(u);let data:any=[];
  if(u.pathname.endsWith('/version'))data={version:'0.1.72',environment:'test'};
  else if(u.pathname==='/api/part-process-characteristics'){if(state.failMappings)return route.fulfill({status:503,json:{message:raw}});data=mappings}
  else if(u.pathname==='/api/control-chart-types')data=scopes.map((_,i)=>({id:i+1,chartGroupId:i+1,chartTypeCode:'I_MR',dataCategory:'Variable'}));
  else if(u.pathname==='/api/control-chart-groups')data=scopes.map((scope,i)=>({id:i+1,groupCode:scope,groupName:raw+' '+scope,groupType:'CONTROL_CHART',businessScopeCode:scope,isEnabled:true}));
  else if(u.pathname==='/api/v1/spc/summary'){
   if(state.failSummary)return route.fulfill({status:503,json:{message:raw}});const id=scopes.indexOf(u.searchParams.get('dimension')||'PROCESS')+1;data=state.empty?[]:[{partProcessCharacteristicId:id,controlCategory:raw,lineOrProcessName:raw,slotName:raw,chartName:raw,chartType:'I_MR',usl:10,lsl:0,totalCount:3,oosCount:0,oosPercentage:0,ca:0,pp:1.2,ppk:1.1,responsibleUser:raw,remarks:raw}];
  }else if(u.pathname==='/api/v1/spc/chart'){
   if(state.failChart)return route.fulfill({status:state.failChart,json:{message:raw}});const points=[0,0.2,0.4].map((value,i)=>({measuredAt:`2026-10-2${i+1}T12:00:00`,value,variableMeasurementId:state.pointIds?501+i:undefined,violatedRules:[],lotNo:raw,pointRemark:raw,outOfSpec:state.violations}));data={chartType:state.chartType,normality:{pValue:state.pValue,testName:'Anderson-Darling'},limits:{usl:10,lsl:0,target:0,ucl:8,cl:4,lcl:0},chartData:{points},secondaryChartData:{points:points.map(p=>({...p,value:0.1}))},rawDataPoints:points,subgroupSize:1,capability:{sigmaOverall:0,ca:0,pp:1.2,ppk:1.1},monitorContext:{lineName:raw,characteristicName:raw,slotName:raw}};
  }else if(u.pathname==='/api/v1/spc/point-exclusions'){
   if(state.failExcluded)return route.fulfill({status:503,json:{message:raw}});data={data:state.restored?[]:[{id:91,variableMeasurementId:501,state:'ExcludedHidden',updatedAt:'2026-10-26T04:00:00Z'},{Id:92,AttributeMeasurementId:601,State:'ExcludedVisible'},{id:93,state:'UnknownRawState'}]};
  }else if(u.pathname==='/api/v1/spc/point-remarks')data={data:state.remarkExists?[{id:201,variableMeasurementId:501,remark:state.remarkText}]:[]};
  else if(u.pathname.endsWith('/chemical-calculation-traces'))data={data:{applicable:false,rows:[]}};
  else if(u.pathname==='/api/v1/spc/chart/raw-data'){
   if(state.failDownload)return route.fulfill({status:500,json:{message:raw}});
   return route.fulfill({body:csv,headers:{'content-type':'text/csv;charset=utf-8','content-disposition':"attachment; filename*=UTF-8''%E5%8E%9F%E6%AA%94.csv"}});
  }
  if(state.multi && u.pathname.endsWith('/spc/summary') && Array.isArray(data) && data.length)data=[3,1,2].map(n=>({...data[0],partProcessCharacteristicId:n,chartName:'Chart '+n,totalCount:n,oosCount:n,ppk:n/2}));
  return route.fulfill({json:data});
 });
 const url=path+(deep?'?ppcId=1&uploadBatchId=00000000-0000-0000-0000-000000000001&startDate=2026-10-01&endDate=2026-10-26':'');await page.goto(url);await expect(page.getByTestId('chart-query')).toBeVisible();if(!deep)await expect(page.getByTestId('chart-dimension-PROCESS')).toBeVisible();else await expect(page.getByTestId('chart-monitor')).toBeVisible();return {reads,writes,state,url};
}
