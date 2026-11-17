import test from 'node:test';import assert from 'node:assert/strict';import vm from 'node:vm';import {readFileSync} from 'node:fs';
const source=readFileSync(new URL('../src/views/PartProcessCharacteristicsView.vue',import.meta.url),'utf8');
test('all scopes expose the four non-formula editor tabs',()=>{
 const expression=source.match(/const formTabs = computed\(\(\) => \[([\s\S]*?)\n\]\);/)[1];
 for(const scope of ['CHEM','PROCESS','PRODUCT']){
  const context={form:{value:{controlScope:scope}},CONTROL_SCOPE:{CHEM:'CHEM'},showChemicalFormulaTab:false,t:x=>x};
  const flag=source.match(/const showChemicalFormulaTab = .*?;/)[0];
  const ids=vm.runInNewContext(flag+'['+expression+']',context).map(x=>x.id);
  assert.deepEqual(Array.from(ids),[1,2,3,5]);
 }
});
test('step navigation skips the hidden formula step and keeps boundaries',()=>{
 const next=source.match(/function nextStep\(\) \{[\s\S]*?\n\}/)[0],prev=source.match(/function prevStep\(\) \{[\s\S]*?\n\}/)[0];
 const context={currentStep:{value:3},formTabs:{value:[1,2,3,5].map(id=>({id}))},validateStep:()=>true,scrollToModalTop:()=>{}};
 vm.runInNewContext(next+prev+';nextStep()',context);assert.equal(context.currentStep.value,5);
 vm.runInNewContext('nextStep()',context);assert.equal(context.currentStep.value,5);
 vm.runInNewContext('prevStep()',context);assert.equal(context.currentStep.value,3);
 context.currentStep.value=1;vm.runInNewContext('prevStep()',context);assert.equal(context.currentStep.value,1);
});
test('saving unrelated fields preserves the exact existing formula JSON including unknown properties',()=>{
 const raw='{"enabled":true,"concentrationFormula":"Primary * 2","decimalPlaces":3,"recheckConcentrationFormula":"Primary * 3","future":{"preserve":true}}';
 const fn=source.match(/function buildChemicalAnalysisConfigJson\(\) \{([\s\S]*?)\n\}/)[0];
 const parsed={enabled:true,concentrationFormula:'Primary * 2'};
 const context={form:{value:{controlScope:'CHEM',chemicalAnalysis:parsed,chemicalAnalysisConfigJson:raw}},modalMode:{value:'edit'},CONTROL_SCOPE:{CHEM:'CHEM'},parseChemicalAnalysisConfig:()=>parsed};
 assert.equal(vm.runInNewContext(fn+';buildChemicalAnalysisConfigJson()',context),raw);
});
