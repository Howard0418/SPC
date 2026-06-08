<script setup>
import { ref, onMounted, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  LayoutDashboard,
  LineChart,
  UploadCloud,
  FileSpreadsheet,
  BookOpen,
  PlusCircle,
  Package,
  Cpu,
  Layers,
  Sliders,
  FolderTree,
  AlertTriangle,
  Search,
  Activity,
  Sun,
  Moon,
  LogOut,
  ChevronDown,
  ChevronRight,
  Users,
  Map
} from "lucide-vue-next";

const authOn = import.meta.env.VITE_AUTH_ENABLED === "true";
const route = useRoute();
const router = useRouter();
const showNav = computed(() => !authOn || route.path !== "/login");

const isDark = ref(true);

function toggleDarkMode() {
  isDark.value = !isDark.value;
  if (isDark.value) {
    document.documentElement.classList.add("dark");
    localStorage.setItem("theme", "dark");
  } else {
    document.documentElement.classList.remove("dark");
    localStorage.setItem("theme", "light");
  }
}

onMounted(() => {
  const savedTheme = localStorage.getItem("theme");
  if (savedTheme === "dark" || (!savedTheme && window.matchMedia("(prefers-color-scheme: dark)").matches)) {
    isDark.value = true;
    document.documentElement.classList.add("dark");
  } else {
    isDark.value = false;
    document.documentElement.classList.remove("dark");
  }
});

const menuCategories = [
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
      { to: "/sitemap", text: "全系統功能地圖", icon: Map },
      { to: "/guide", text: "系統操作手冊", icon: BookOpen },
      { to: "/operators", text: "作業工程師與權限", icon: Users },
      { to: "/settings/smtp", text: "SMTP 郵件與預警設定", icon: Sliders }
    ]
  }
];

function logout() {
  localStorage.removeItem("mes_spc_token");
  router.push("/login");
}
</script>

<template>
  <div class="min-h-screen flex flex-col bg-slate-100 dark:bg-slate-950 font-sans text-slate-800 dark:text-slate-100 transition-colors duration-300">
    <!-- Premium Top Navigation Bar -->
    <header class="sticky top-0 z-50 flex items-center justify-between h-16 px-6 bg-slate-900/90 dark:bg-slate-900/80 backdrop-blur-md border-b border-slate-700/50 shadow-lg text-white">
      <div class="flex items-center gap-3">
        <div class="p-2.5 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-xl shadow-md shadow-indigo-500/20 flex items-center justify-center">
          <Activity class="w-6 h-6 text-white animate-pulse" />
        </div>
        <div>
          <h1 class="text-xl font-black tracking-wider bg-gradient-to-r from-blue-400 via-indigo-300 to-cyan-300 bg-clip-text text-transparent">
            PMR SPC <span class="text-xs px-2 py-0.5 ml-2 font-bold bg-blue-600/40 text-blue-300 border border-blue-500/30 rounded-full uppercase tracking-widest">Enterprise</span>
          </h1>
          <p class="text-[11px] text-slate-400 -mt-1 tracking-tight font-medium">智能統計製程與製造品質分析系統</p>
        </div>
      </div>
      
      <div class="flex items-center gap-4">
        <!-- Theme Toggle Button -->
        <button
          @click="toggleDarkMode"
          type="button"
          class="p-2 rounded-xl bg-slate-800 dark:bg-slate-800 hover:bg-slate-700 text-yellow-400 dark:text-blue-300 transition-all shadow-sm border border-slate-700"
          :title="isDark ? '切換亮色模式' : '切換深色模式'"
        >
          <Sun v-if="isDark" class="w-5 h-5" />
          <Moon v-else class="w-5 h-5 text-indigo-400" />
        </button>

        <!-- Auth info -->
        <div v-if="authOn" class="flex items-center gap-3 text-sm pl-4 border-l border-slate-700/80">
          <router-link to="/login" class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-blue-600 hover:bg-blue-500 text-white shadow-md shadow-blue-500/20 transition-all">登入系統</router-link>
          <button
            type="button"
            class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-slate-800 hover:bg-red-600/80 hover:text-white border border-slate-700 transition-all text-slate-300"
            @click="logout"
          >
            <LogOut class="w-3.5 h-3.5" /> 登出
          </button>
        </div>
      </div>
    </header>

    <div v-if="showNav" class="flex flex-1 overflow-hidden">
      <!-- Premium Collapsible/Stylized Sidebar -->
      <aside class="w-64 flex-shrink-0 bg-white/95 dark:bg-slate-900/95 border-r border-slate-200 dark:border-slate-800/80 p-4 space-y-6 overflow-y-auto backdrop-blur-sm shadow-xl flex flex-col justify-between">
        <div class="space-y-6">
          <div v-for="cat in menuCategories" :key="cat.title" class="space-y-1.5">
            <h3 class="px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 flex items-center gap-2">
              <span class="w-1.5 h-1.5 rounded-full bg-blue-500"></span> {{ cat.title }}
            </h3>
            <div class="space-y-1">
              <router-link
                v-for="m in cat.items"
                :key="m.to"
                :to="m.to"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-semibold text-slate-600 dark:text-slate-300 hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-slate-800/80 dark:hover:text-blue-400 transition-all group"
                active-class="bg-gradient-to-r from-blue-600 to-indigo-600 text-white dark:from-blue-600 dark:to-indigo-600 dark:text-white shadow-lg shadow-blue-500/20 dark:shadow-blue-900/30 font-bold"
              >
                <component :is="m.icon" class="w-4 h-4 transition-transform group-hover:scale-110" />
                <span class="flex-1 truncate">{{ m.text }}</span>
                <ChevronRight class="w-3.5 h-3.5 opacity-0 group-hover:opacity-100 transition-opacity" />
              </router-link>
            </div>
          </div>
        </div>

        <!-- Footer Info -->
        <div class="pt-4 border-t border-slate-200 dark:border-slate-800/80 text-[11px] text-slate-400 dark:text-slate-500 text-center space-y-1 font-medium">
          <p>© 2026 PMR Quality System</p>
          <p class="text-[10px] text-slate-500/70">v10.0 Enterprise SPC Edition</p>
        </div>
      </aside>

      <!-- Main Workspace -->
      <main class="flex-1 p-6 overflow-y-auto bg-slate-50 dark:bg-slate-950 transition-colors">
        <div class="max-w-[1800px] w-full mx-auto space-y-6">
          <router-view />
        </div>
      </main>
    </div>

    <!-- No Nav View (e.g. Login) -->
    <main v-else class="flex-1 p-6 flex items-center justify-center bg-slate-900/40">
      <router-view />
    </main>
  </div>
</template>
