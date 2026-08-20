<script setup>
import { ref, onMounted, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  LineChart,
  TrendingUp,
  UploadCloud,
  BookOpen,
  PlusCircle,
  Package,
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
  Map,
  CalendarClock
} from "lucide-vue-next";
import { clearAuthSession, getCurrentUser } from "./utils/auth";
import { api } from "./api/client";
import pkg from "../package.json";

const authOn = import.meta.env.VITE_AUTH_ENABLED === "true";
const appVersion = pkg.version;
const webEnvironment = import.meta.env.VITE_APP_ENV || "unknown";
const apiVersion = ref("--");
const apiEnvironment = ref("unknown");
const apiOnline = ref(false);
const environmentLabel = (value) => value === "production" ? "正式" : value === "test" ? "測試" : "未知";
const environmentMismatch = computed(() =>
  apiOnline.value && apiEnvironment.value !== "unknown" && apiEnvironment.value !== webEnvironment
);
const route = useRoute();
const router = useRouter();
const showNav = computed(() => !authOn || route.path !== "/login");
const currentUser = computed(() => {
  route.fullPath;
  return getCurrentUser();
});
const canEdit = computed(() => !authOn || currentUser.value?.role === "Editor");

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

  api.get("/version", { timeout: 5000 })
    .then(({ data }) => {
      apiVersion.value = data?.version || "--";
      apiEnvironment.value = data?.environment || "unknown";
      apiOnline.value = true;
    })
    .catch(() => {
      apiOnline.value = false;
    });
});

const viewerMenuCategories = [
  {
    title: "高階戰情與分析",
    items: [
      { to: "/spc", text: "SPC 管制圖", icon: LineChart },
      { to: "/trend-chart", text: "量測值趨勢圖", icon: TrendingUp },
      { to: "/monthly-control-chart", text: "SPC 週月報表", icon: CalendarClock }
    ]
  }
];

const editorMenuCategories = [
  ...viewerMenuCategories,
  {
    title: "自動化匯入與採樣",
    items: [
      { to: "/uploads", text: "SPC 資料匯入", icon: UploadCloud }
    ]
  },
  {
    title: "企業品質主檔設定",
    items: [
      { to: "/part-process-characteristics", text: "SPC 管制項目設定", icon: FolderTree },
      { to: "/processes", text: "工站製程主檔", icon: Layers },
      { to: "/parts", text: "產品料號主檔", icon: Package },
      { to: "/characteristics", text: "品質特性項目", icon: Sliders },
      { to: "/traceability-master", text: "線別槽體設定", icon: Layers },
      { to: "/control-chart-groups", text: "管制圖大類別維護", icon: Layers },
      { to: "/spc-rule-groups", text: "SPC 異常規則維護", icon: Activity },
    ]
  },
  {
    title: "異常管理與追溯",
    items: [
      { to: "/alerts", text: "異常通報總覽", icon: AlertTriangle },
      { to: "/alerts-workflow", text: "異常單簽核處置", icon: Activity },
      { to: "/genealogy", text: "產品系譜圖 (Genealogy)", icon: FolderTree },
      { to: "/spc/query", text: "多維度品質履歷查詢", icon: Search }
    ]
  },
  {
    title: "系統管理與通報設定",
    items: [
      { to: "/guide", text: "系統操作手冊", icon: BookOpen },
      { to: "/settings/smtp", text: "SMTP 郵件與預警設定", icon: Sliders },
      { to: "/settings/spc-reports", text: "SPC 週報月報設定", icon: CalendarClock },
      { to: "/operators", text: "系統使用者管理", icon: Users }
    ]
  }
];

const menuCategories = computed(() => canEdit.value ? editorMenuCategories : viewerMenuCategories);

function logout() {
  clearAuthSession();
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
          <div v-if="currentUser" class="text-right leading-tight">
            <div class="text-xs font-bold text-white">{{ currentUser.displayName || currentUser.username }}</div>
            <div class="text-[10px] text-blue-300">{{ currentUser.role === 'Viewer' ? '檢視者' : '編輯者' }}</div>
          </div>
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
        <div class="pt-4 border-t border-slate-200 dark:border-slate-800/80 text-[11px] text-slate-500 dark:text-slate-400 space-y-2 font-medium">
          <div
            class="rounded-lg border px-3 py-2 space-y-1.5"
            :class="webEnvironment === 'production'
              ? 'border-red-300 bg-red-50 dark:border-red-900/70 dark:bg-red-950/30'
              : 'border-amber-300 bg-amber-50 dark:border-amber-900/70 dark:bg-amber-950/30'"
          >
            <div class="flex items-center justify-between">
              <span class="font-black tracking-wider">{{ environmentLabel(webEnvironment) }}環境</span>
              <span class="h-2 w-2 rounded-full" :class="apiOnline ? 'bg-emerald-500' : 'bg-red-500'"></span>
            </div>
            <div class="flex justify-between"><span>WEB</span><span class="font-mono">v{{ appVersion }}</span></div>
            <div class="flex justify-between"><span>API</span><span class="font-mono">{{ apiOnline ? `v${apiVersion}` : '離線' }}</span></div>
            <p v-if="environmentMismatch" class="font-bold text-red-600 dark:text-red-400">⚠ WEB／API 環境不一致</p>
          </div>
          <p class="text-center">© 2026 PMR Quality System</p>
        </div>
      </aside>

      <!-- Main Workspace -->
      <main
        class="flex-1 overflow-y-auto bg-slate-50 dark:bg-slate-950 transition-colors"
        :class="route.path === '/spc' ? 'p-3' : 'p-6'"
      >
        <div
          class="w-full mx-auto space-y-6"
          :class="route.path === '/spc' ? 'max-w-none' : 'max-w-[1800px]'"
        >
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
