<script setup>
import { 
  BookOpen, ArrowRight,
  LayoutDashboard, LineChart, UploadCloud, FileSpreadsheet,
  Package, Cpu, Layers, Sliders, FolderTree,
  Activity, AlertTriangle, Search, Users
} from "lucide-vue-next";
import { useRouter } from "vue-router";

const router = useRouter();

const siteMapCategories = [
  {
    title: "高階戰情與分析",
    items: [
      { to: "/", text: "儀表板", icon: LayoutDashboard },
      { to: "/spc", text: "SPC 管制圖", icon: LineChart }
    ]
  },
  {
    title: "自動化匯入與採樣",
    items: [
      { to: "/measurements", text: "現場量測數據錄入", icon: Activity },
      { to: "/uploads/variable", text: "計量型資料匯入", icon: UploadCloud },
      { to: "/uploads/attribute", text: "計數型資料匯入", icon: FileSpreadsheet }
    ]
  },
  {
    title: "企業品質主檔設定",
    items: [
      { to: "/parts", text: "產品料號主檔", icon: Package },
      { to: "/processes", text: "工站製程主檔", icon: Layers },
      { to: "/machines", text: "生產機台主檔", icon: Cpu },
      { to: "/characteristics", text: "品質特性項目", icon: Sliders },
      { to: "/part-process-characteristics", text: "料號檢驗基準設定", icon: FolderTree },
      { to: "/traceability-master", text: "產線槽位追溯設定", icon: Layers }
    ]
  },
  {
    title: "管制圖與西方電氣規則",
    items: [
      { to: "/control-chart-groups", text: "管制圖分類總管", icon: Layers },
      { to: "/control-chart-categories", text: "管制圖分類維護", icon: FolderTree },
      { to: "/control-chart-types", text: "管制圖參數配置", icon: Activity },
      { to: "/spc-rule-groups", text: "SPC 異常規則維護", icon: Activity }
    ]
  },
  {
    title: "異常管理與追溯",
    items: [
      { to: "/alerts", text: "異常通報總覽", icon: AlertTriangle },
      { to: "/spc/query", text: "多維度品質履歷與查詢", icon: Search },
      { to: "/alerts-workflow", text: "異常單簽核處置", icon: Activity },
      { to: "/genealogy", text: "產品系譜圖 (Genealogy)", icon: FolderTree }
    ]
  },
  {
    title: "系統管理與通報設定",
    items: [
      { to: "/guide", text: "系統操作手冊", icon: BookOpen },
      { to: "/operators", text: "作業工程師與權限", icon: Users },
      { to: "/settings/smtp", text: "SMTP 郵件與預警設定", icon: Sliders }
    ]
  }
];

const navigateTo = (path) => {
  router.push(path);
};
</script>

<template>
  <div class="w-full mx-auto pb-12">
    <!-- Header Banner -->
    <div class="p-8 rounded-3xl bg-gradient-to-r from-emerald-600 via-teal-600 to-emerald-800 text-white shadow-xl relative overflow-hidden mb-8">
      <div class="absolute right-0 top-0 w-64 h-64 bg-white/5 rounded-full blur-3xl pointer-events-none"></div>
      <div class="relative z-10 space-y-2">
        <div class="flex items-center gap-2 px-3 py-1 rounded-full bg-white/10 w-max text-xs font-bold text-emerald-100 border border-white/20">
          <BookOpen class="w-3.5 h-3.5" /> 系統導覽
        </div>
        <h1 class="text-3xl font-black tracking-tight text-white">
          全系統功能地圖 (Site Map)
        </h1>
        <p class="text-emerald-100 text-sm max-w-2xl leading-relaxed">
          完整呈現 MES-SPC 企業級品質分析系統的所有功能模組，幫助您快速找到需要的管理設定與作業畫面。
        </p>
      </div>
    </div>

    <!-- Site Map Section -->
    <div class="bg-white dark:bg-slate-900 rounded-3xl p-8 border border-slate-200 dark:border-slate-800 shadow-sm">
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
        <div v-for="cat in siteMapCategories" :key="cat.title" class="space-y-4">
          <h3 class="text-sm font-bold text-slate-500 dark:text-slate-400 uppercase tracking-widest border-b border-slate-100 dark:border-slate-800 pb-2">{{ cat.title }}</h3>
          <div class="flex flex-col gap-2">
            <button 
              v-for="item in cat.items" 
              :key="item.to"
              @click="navigateTo(item.to)"
              class="flex items-center justify-between p-3 rounded-xl bg-slate-50 dark:bg-slate-800/50 hover:bg-emerald-50 dark:hover:bg-emerald-900/20 border border-slate-200 dark:border-slate-700 hover:border-emerald-200 dark:hover:border-emerald-800 text-left transition-all group"
            >
              <div class="flex items-center gap-3">
                <component :is="item.icon" class="w-4 h-4 text-slate-400 group-hover:text-emerald-500 transition-colors" />
                <span class="text-sm font-bold text-slate-700 dark:text-slate-300 group-hover:text-emerald-700 dark:group-hover:text-emerald-300 transition-colors">{{ item.text }}</span>
              </div>
              <ArrowRight class="w-4 h-4 text-slate-300 dark:text-slate-600 group-hover:text-emerald-500 opacity-0 group-hover:opacity-100 transition-all transform -translate-x-2 group-hover:translate-x-0" />
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
