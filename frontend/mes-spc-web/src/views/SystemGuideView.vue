<script setup>
import {useI18n} from "vue-i18n";
const {t}=useI18n({useScope:"global"});
import { BookOpen, Map, ArrowRight } from "lucide-vue-next";
</script>

<template>
  <div class="w-full space-y-6 pb-12">
    <!-- Jumbotron banner -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-slate-900 via-indigo-900 to-slate-900 text-white shadow-xl relative overflow-hidden">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/5 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-indigo-200 border border-white/20">
          <BookOpen class="w-3.5 h-3.5" /> {{ t('spc.help.helpCenter') }}
        </div>
        <h1 class="text-3xl font-black tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-white to-indigo-200">
          {{ t('spc.help.title') }}
        </h1>
        <p class="text-indigo-200 text-sm max-w-2xl leading-relaxed">
          {{ t('spc.help.description') }}
        </p>
      </div>
    </div>

    <!-- Content Sections -->
    <div class="space-y-6">
      
      <!-- Workflow Diagram Section -->
      <div class="bg-white dark:bg-slate-900 rounded-3xl p-8 border border-slate-200 dark:border-slate-800 shadow-sm">
        <h2 class="text-xl font-black text-slate-800 dark:text-white flex items-center gap-2 mb-6">
          <Map class="w-5 h-5 text-indigo-500" /> {{ t('spc.help.workflow') }}
        </h2>
        <div class="flex flex-col md:flex-row gap-4 items-stretch justify-center">
          
          <div class="flex-1 bg-slate-50 dark:bg-slate-800/50 p-5 rounded-2xl border border-slate-200 dark:border-slate-700">
            <div class="text-xs font-bold text-indigo-500 uppercase tracking-widest mb-3">Phase 1: Setup</div>
            <div class="space-y-3">
              <div class="p-3 bg-white dark:bg-slate-800 rounded-xl shadow-sm text-sm font-bold text-slate-700 dark:text-slate-300 text-center border border-slate-100 dark:border-slate-700">{{ t('spc.help.createMasters') }}</div>
              <div class="flex justify-center text-slate-300 dark:text-slate-600"><ArrowRight class="w-4 h-4 rotate-90 md:rotate-0" /></div>
              <div class="p-3 bg-white dark:bg-slate-800 rounded-xl shadow-sm text-sm font-bold text-slate-700 dark:text-slate-300 text-center border border-slate-100 dark:border-slate-700">{{ t('spc.help.setLimits') }}</div>
            </div>
          </div>

          <div class="flex-1 bg-blue-50 dark:bg-blue-900/20 p-5 rounded-2xl border border-blue-100 dark:border-blue-900/50">
            <div class="text-xs font-bold text-blue-500 uppercase tracking-widest mb-3">Phase 2: Execution</div>
            <div class="space-y-3 h-full flex flex-col justify-center">
              <div class="p-3 bg-white dark:bg-slate-800 rounded-xl shadow-sm text-sm font-bold text-blue-700 dark:text-blue-300 text-center border border-blue-100 dark:border-blue-800">{{ t('spc.help.enterData') }}</div>
              <div class="flex justify-center text-blue-300 dark:text-blue-700"><ArrowRight class="w-4 h-4 rotate-90 md:rotate-0" /></div>
              <div class="p-3 bg-blue-600 text-white rounded-xl shadow-md text-sm font-black text-center border border-blue-500">{{ t('spc.help.calculate') }}</div>
            </div>
          </div>

          <div class="flex-1 bg-emerald-50 dark:bg-emerald-900/20 p-5 rounded-2xl border border-emerald-100 dark:border-emerald-900/50 flex flex-col">
            <div class="text-xs font-bold text-emerald-500 uppercase tracking-widest mb-3">Phase 3: Result</div>
            <div class="space-y-3 flex-1 flex flex-col justify-center">
              <div class="p-3 bg-white dark:bg-slate-800 rounded-xl shadow-sm text-sm font-bold text-emerald-700 dark:text-emerald-300 text-center border border-emerald-100 dark:border-emerald-800">{{ t('spc.help.archive') }}</div>
              <div class="p-3 bg-red-50 dark:bg-red-900/20 rounded-xl shadow-sm text-sm font-bold text-red-700 dark:text-red-300 text-center border border-red-100 dark:border-red-900">{{ t('spc.help.alertEmail') }}</div>
              <div class="p-3 bg-amber-50 dark:bg-amber-900/20 rounded-xl shadow-sm text-sm font-bold text-amber-700 dark:text-amber-300 text-center border border-amber-100 dark:border-amber-900">{{ t('spc.help.resolve') }}</div>
            </div>
          </div>

        </div>
      </div>

      <!-- Step by Step Guide -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        
        <div class="bg-white dark:bg-slate-900 rounded-3xl p-8 border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
          <h3 class="text-lg font-black text-slate-800 dark:text-white">{{ t('spc.help.initialSetup') }}</h3>
          
          <div class="space-y-4">
            <div class="relative pl-6 before:absolute before:left-0 before:top-2 before:w-2 before:h-2 before:bg-indigo-500 before:rounded-full">
              <div class="font-bold text-sm text-slate-700 dark:text-slate-200 mb-1">{{ t('spc.help.stepOne') }}</div>
              <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
                {{ t('spc.help.stepOneHelp') }}
              </p>
            </div>
            
            <div class="relative pl-6 before:absolute before:left-0 before:top-2 before:w-2 before:h-2 before:bg-indigo-500 before:rounded-full">
              <div class="font-bold text-sm text-slate-700 dark:text-slate-200 mb-1">{{ t('spc.help.stepTwo') }}</div>
              <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
                {{ t('spc.help.stepTwoHelp') }}
              </p>
            </div>
            
            <div class="relative pl-6 before:absolute before:left-0 before:top-2 before:w-2 before:h-2 before:bg-indigo-500 before:rounded-full">
              <div class="font-bold text-sm text-slate-700 dark:text-slate-200 mb-1">{{ t('spc.help.stepThree') }}</div>
              <p class="text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
                {{ t('spc.help.stepThreeHelp') }}
              </p>
            </div>
          </div>
        </div>

        <div class="bg-white dark:bg-slate-900 rounded-3xl p-8 border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
          <h3 class="text-lg font-black text-slate-800 dark:text-white">{{ t('spc.help.results') }}</h3>
          
          <div class="space-y-4">
            <div class="p-4 bg-sky-50 dark:bg-sky-900/20 rounded-2xl border border-sky-100 dark:border-sky-800/50">
              <div class="font-bold text-sm text-sky-800 dark:text-sky-300 mb-1">{{ t('spc.help.chartResult') }}</div>
              <p class="text-xs text-sky-600 dark:text-sky-400/80 leading-relaxed">
                {{ t('spc.help.chartResultHelp') }}
              </p>
            </div>
            
            <div class="p-4 bg-red-50 dark:bg-red-900/20 rounded-2xl border border-red-100 dark:border-red-800/50">
              <div class="font-bold text-sm text-red-800 dark:text-red-300 mb-1">{{ t('spc.help.alertResult') }}</div>
              <p class="text-xs text-red-600 dark:text-red-400/80 leading-relaxed">
                {{ t('spc.help.alertResultHelp') }}
              </p>
            </div>
            
            <div class="p-4 bg-amber-50 dark:bg-amber-900/20 rounded-2xl border border-amber-100 dark:border-amber-800/50">
              <div class="font-bold text-sm text-amber-800 dark:text-amber-300 mb-1">{{ t('spc.help.workflowResult') }}</div>
              <p class="text-xs text-amber-600 dark:text-amber-400/80 leading-relaxed">
                {{ t('spc.help.workflowResultHelp') }}
              </p>
            </div>
            
            <div class="p-4 bg-purple-50 dark:bg-purple-900/20 rounded-2xl border border-purple-100 dark:border-purple-800/50">
              <div class="font-bold text-sm text-purple-800 dark:text-purple-300 mb-1">{{ t('spc.help.excludeTitle') }}</div>
              <div class="space-y-3 text-xs text-purple-600 dark:text-purple-400/80 leading-relaxed">
                <div>
                  <div class="font-black text-purple-800 dark:text-purple-300">{{ t('spc.help.openChart') }}</div>
                  <p>{{ t('spc.help.openChartHelp') }}</p>
                </div>
                <div>
                  <div class="font-black text-purple-800 dark:text-purple-300">{{ t('spc.help.selectPoint') }}</div>
                  <p>{{ t('spc.help.selectPointHelp') }}</p>
                </div>
                <div>
                  <div class="font-black text-purple-800 dark:text-purple-300">{{ t('spc.help.excludePoint') }}</div>
                  <p>{{ t('spc.help.excludePointHelp') }}</p>
                </div>
                <div>
                  <div class="font-black text-purple-800 dark:text-purple-300">{{ t('spc.help.checkResult') }}</div>
                  <p>{{ t('spc.help.checkResultHelp') }}</p>
                </div>
                <div>
                  <div class="font-black text-purple-800 dark:text-purple-300">{{ t('spc.help.restore') }}</div>
                  <p>{{ t('spc.help.restoreHelp') }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Page Operation Guide -->
      <div class="bg-white dark:bg-slate-900 rounded-3xl p-8 border border-slate-200 dark:border-slate-800 shadow-sm space-y-6">
        <h2 class="text-xl font-black text-slate-800 dark:text-white flex items-center gap-2">
          <BookOpen class="w-5 h-5 text-indigo-500" /> {{ t('spc.help.pageInstructions') }}
        </h2>

        <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
          <div class="p-5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 space-y-3">
            <div class="font-black text-sm text-slate-800 dark:text-white">{{ t('spc.help.parts') }}</div>
            <div class="space-y-2 text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.enterPage') }}</span>{{ t('spc.help.partsPage') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.search') }}</span>{{ t('spc.help.partsSearch') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.addPart') }}</span>{{ t('spc.help.partsAdd') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.enable') }}</span>{{ t('spc.help.partsEnable') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.save') }}</span>{{ t('spc.help.partsSave') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 space-y-3">
            <div class="font-black text-sm text-slate-800 dark:text-white">{{ t('spc.help.processes') }}</div>
            <div class="space-y-2 text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.enterPage') }}</span>{{ t('spc.help.processesPage') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.createProcess') }}</span>{{ t('spc.help.processesAdd') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.configureMachine') }}</span>{{ t('spc.help.processesEquipment') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.enable') }}</span>{{ t('spc.help.processesEnable') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-slate-50 dark:bg-slate-800/50 border border-slate-200 dark:border-slate-700 space-y-3">
            <div class="font-black text-sm text-slate-800 dark:text-white">{{ t('spc.help.characteristics') }}</div>
            <div class="space-y-2 text-xs text-slate-500 dark:text-slate-400 leading-relaxed">
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.enterPage') }}</span>{{ t('spc.help.characteristicsPage') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.addCharacteristic') }}</span>{{ t('spc.help.characteristicsAdd') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.chooseDataCategory') }}</span>{{ t('spc.help.dataCategoryHelp') }}</p>
              <p><span class="font-bold text-slate-700 dark:text-slate-200">{{ t('spc.help.assignChart') }}</span>{{ t('spc.help.assignChartHelp') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-blue-50 dark:bg-blue-900/20 border border-blue-100 dark:border-blue-900/50 space-y-3">
            <div class="font-black text-sm text-blue-800 dark:text-blue-300">{{ t('spc.help.controlItems') }}</div>
            <div class="space-y-2 text-xs text-blue-600 dark:text-blue-400/80 leading-relaxed">
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.enterPage') }}</span>{{ t('spc.help.controlItemsPage') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.createMapping') }}</span>{{ t('spc.help.createMappingHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.setSpecification') }}</span>{{ t('spc.help.setSpecificationHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.chooseChart') }}</span>{{ t('spc.help.chooseChartHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.confirmEnabled') }}</span>{{ t('spc.help.confirmEnabledHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.chartNavigation') }}</span>{{ t('spc.help.chartNavigationHelp') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-purple-50 dark:bg-purple-900/20 border border-purple-100 dark:border-purple-900/50 space-y-3">
            <div class="font-black text-sm text-purple-800 dark:text-purple-300 flex items-center gap-1.5">
              <span>{{ t('spc.help.segments') }}</span>
              <span class="px-1.5 py-0.5 rounded bg-purple-100 dark:bg-purple-900/60 text-[10px] text-purple-600 dark:text-purple-400 font-bold border border-purple-200 dark:border-purple-800 animate-pulse">New</span>
            </div>
            <div class="space-y-2 text-xs text-purple-600 dark:text-purple-400/80 leading-relaxed">
              <p><span class="font-bold text-purple-800 dark:text-purple-300">{{ t('spc.help.openSettings') }}</span>{{ t('spc.help.openSettingsHelp') }}</p>
              <p><span class="font-bold text-purple-800 dark:text-purple-300">{{ t('spc.help.calculateLimits') }}</span>{{ t('spc.help.calculateLimitsHelp') }}</p>
              <p><span class="font-bold text-purple-800 dark:text-purple-300">{{ t('spc.help.createSegment') }}</span>{{ t('spc.help.createSegmentHelp') }}</p>
              <p><span class="font-bold text-purple-800 dark:text-purple-300">{{ t('spc.help.howSegmentsWork') }}</span>{{ t('spc.help.segmentsHelp') }}</p>
              <p><span class="font-bold text-purple-800 text-amber-500">{{ t('spc.help.overlapCheck') }}</span>{{ t('spc.help.overlapHelp') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-blue-50 dark:bg-blue-900/20 border border-blue-100 dark:border-blue-900/50 space-y-3 lg:col-span-2">
            <div class="font-black text-sm text-blue-800 dark:text-blue-300">{{ t('spc.help.transfiles') }}</div>
            <div class="space-y-2 text-xs text-blue-600 dark:text-blue-400/80 leading-relaxed">
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.importMethod') }}</span>{{ t('spc.help.importMethodHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.viewBatch') }}</span>{{ t('spc.help.viewBatchHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.fixErrors') }}</span>{{ t('spc.help.fixErrorsHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.confirmChart') }}</span>{{ t('spc.help.confirmChartHelp') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-blue-50 dark:bg-blue-900/20 border border-blue-100 dark:border-blue-900/50 space-y-3">
            <div class="font-black text-sm text-blue-800 dark:text-blue-300">{{ t('spc.help.spcQuery') }}</div>
            <div class="space-y-2 text-xs text-blue-600 dark:text-blue-400/80 leading-relaxed">
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.enterPage') }}</span>{{ t('spc.help.spcQueryPage') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.selectConditions') }}</span>{{ t('spc.help.selectConditionsHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.viewJudgment') }}</span>{{ t('spc.help.viewJudgmentHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.viewDetails') }}</span>{{ t('spc.help.viewDetailsHelp') }}</p>
              <p><span class="font-bold text-blue-800 dark:text-blue-300">{{ t('spc.help.exclude') }}</span>{{ t('spc.help.excludeHelp') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-amber-50 dark:bg-amber-900/20 border border-amber-100 dark:border-amber-900/50 space-y-3">
            <div class="font-black text-sm text-amber-800 dark:text-amber-300">{{ t('spc.help.alertWorkflow') }}</div>
            <div class="space-y-2 text-xs text-amber-600 dark:text-amber-400/80 leading-relaxed">
              <p><span class="font-bold text-amber-800 dark:text-amber-300">{{ t('spc.help.openOverview') }}</span>{{ t('spc.help.openOverviewHelp') }}</p>
              <p><span class="font-bold text-amber-800 dark:text-amber-300">{{ t('spc.help.viewAlert') }}</span>{{ t('spc.help.viewAlertHelp') }}</p>
              <p><span class="font-bold text-amber-800 dark:text-amber-300">{{ t('spc.help.resolution') }}</span>{{ t('spc.help.resolutionHelp') }}</p>
              <p><span class="font-bold text-amber-800 dark:text-amber-300">{{ t('spc.help.closure') }}</span>{{ t('spc.help.closureHelp') }}</p>
            </div>
          </div>

          <div class="p-5 rounded-2xl bg-emerald-50 dark:bg-emerald-900/20 border border-emerald-100 dark:border-emerald-900/50 space-y-3">
            <div class="font-black text-sm text-emerald-800 dark:text-emerald-300">{{ t('spc.help.smtp') }}</div>
            <div class="space-y-2 text-xs text-emerald-600 dark:text-emerald-400/80 leading-relaxed">
              <p><span class="font-bold text-emerald-800 dark:text-emerald-300">{{ t('spc.help.enterPage') }}</span>{{ t('spc.help.smtpPage') }}</p>
              <p><span class="font-bold text-emerald-800 dark:text-emerald-300">{{ t('spc.help.configureHost') }}</span>{{ t('spc.help.configureHostHelp') }}</p>
              <p><span class="font-bold text-emerald-800 dark:text-emerald-300">{{ t('spc.help.saveSettings') }}</span>{{ t('spc.help.saveSettingsHelp') }}</p>
              <p><span class="font-bold text-emerald-800 dark:text-emerald-300">{{ t('spc.help.testMail') }}</span>{{ t('spc.help.testMailHelp') }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
