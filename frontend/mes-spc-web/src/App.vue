<script setup>
import { ref, onMounted, computed, watch, provide } from "vue";
import LocalePicker from "./components/LocalePicker.vue";
import { localeControlKey } from "./i18n/context";
import { useRoute } from "vue-router";
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
  House,
  LogOut,
  ChevronDown,
  ChevronRight,
  Users,
  Map,
  CalendarClock
  ,FlaskConical, Wind
} from "lucide-vue-next";
import { clearAuthSession, getCurrentUser } from "./utils/auth";
import { api, setApiEnvironment } from "./api/client";
import { useI18n } from "vue-i18n";
import { readLocale, saveLocale, supportedLocales } from "./i18n/preferences";
import pkg from "../package.json";

const { t, locale } = useI18n({ useScope: "global" });
const authOn = import.meta.env.VITE_AUTH_ENABLED === "true";
const portalUrl = import.meta.env.VITE_PORTAL_URL || "http://172.16.110.27/";
const portalHomeUrl = (() => {
  const url = new URL(portalUrl, window.location.origin);
  url.pathname = url.pathname.replace(/\/login\/?$/i, "/");
  url.search = "";
  return url.toString();
})();
const portalLogoutUrl = (() => {
  const url = new URL(portalUrl, window.location.origin);
  url.pathname = `${url.pathname.replace(/\/login\/?$/i, "").replace(/\/$/, "")}/Spc/Logout`;
  url.search = "";
  return url.toString();
})();
const appVersion = pkg.version;
const webEnvironment = import.meta.env.VITE_APP_ENV || "unknown";
const apiVersion = ref("--");
const apiEnvironment = ref("unknown");
const apiOnline = ref(false);
const environmentLabel = value => t(`spc.shell.${["production", "test"].includes(value) ? value : "unknown"}`);
const environmentMismatch = computed(() =>
  apiOnline.value && apiEnvironment.value !== "unknown" && apiEnvironment.value !== webEnvironment
);
const route = useRoute();
const stackedMobileNav = computed(() => [
  '/calibration-instruments', '/processes', '/traceability-master',
  '/control-chart-groups', '/spc-rule-groups',
].includes(route.path));
const showNav = computed(() => !authOn || route.path !== "/login");
const currentUser = computed(() => {
  route.fullPath;
  return getCurrentUser();
});
const canEdit = computed(() => !authOn || currentUser.value?.role === "Editor");
// Storage access itself can throw in restricted browser contexts.
const localeStorage = {
  getItem: key => window.localStorage.getItem(key),
  setItem: (key, value) => window.localStorage.setItem(key, value),
};
const localeSubject = computed(() => authOn && currentUser.value ? (currentUser.value.id ?? 0) : null);
const localeUnavailable = ref(false);
watch(localeSubject, subject => {
  const saved = readLocale(localeStorage, webEnvironment, subject);
  locale.value = saved.locale;
  localeUnavailable.value = !saved.available;
}, { immediate: true });
watch(locale, value => { document.documentElement.lang = value; }, { immediate: true });
function changeLocale(event) {
  const value = event.target.value;
  if (!supportedLocales.includes(value)) return;
  locale.value = value;
  localeUnavailable.value = !saveLocale(localeStorage, webEnvironment, localeSubject.value, value);
}
provide(localeControlKey, changeLocale);


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
      setApiEnvironment(apiEnvironment.value);
      apiOnline.value = true;
    })
    .catch(() => {
      setApiEnvironment("unknown");
      apiOnline.value = false;
    });
});

const viewerMenuCategories = [
  {
    title: "spc.navigation.processAnalysis",
    items: [
      { to: "/spc", text: "spc.navigation.spcAnalysis", icon: LineChart },
      { to: "/particle-monitoring", text: "spc.navigation.particleMonitoring", icon: Wind },
      { to: "/trend-chart", text: "spc.navigation.measurementTrends", icon: TrendingUp },
      { to: "/monthly-control-chart", text: "spc.navigation.periodReports", icon: CalendarClock }
    ]
  },
  {
    title: "spc.navigation.equipmentMonitoring",
    items: [
      { to: "/equipment-status", text: "spc.navigation.equipmentStatus", icon: Activity },
      { to: "/equipment-points", text: "spc.navigation.equipmentPoints", icon: Search },
      { to: "/equipment-monitor", text: "spc.navigation.keyPoints", icon: Activity }
    ]
  }
];

const editorMenuCategories = [
  ...viewerMenuCategories,
  {
    title: "spc.navigation.importSampling",
    items: [
      { to: "/uploads", text: "spc.navigation.importBatches", icon: UploadCloud }
    ]
  },
  {
    title: "spc.navigation.qualityMaster",
    items: [
      { to: "/part-process-characteristics", text: "spc.navigation.controlItems", icon: FolderTree },
      { to: "/chemical-analysis-overview", text: "spc.navigation.lineAnalysis", icon: FlaskConical },
      { to: "/chemical-f-table", text: "spc.navigation.chemicalTables", icon: FlaskConical },
      { to: "/processes", text: "spc.navigation.processes", icon: Layers },
      { to: "/parts", text: "spc.navigation.parts", icon: Package },
      { to: "/characteristics", text: "spc.navigation.characteristics", icon: Sliders },
      { to: "/traceability-master", text: "spc.navigation.linesTanks", icon: Layers },
      { to: "/control-chart-groups", text: "spc.navigation.chartCategories", icon: Layers },
      { to: "/spc-rule-groups", text: "spc.navigation.alertRules", icon: Activity },
    ]
  },
  {
    title: "spc.navigation.alertsTraceability",
    items: [
      { to: "/alerts", text: "spc.navigation.alerts", icon: AlertTriangle },
      { to: "/alerts-workflow", text: "spc.navigation.alertWorkflow", icon: Activity },
      { to: "/genealogy", text: "spc.navigation.genealogy", icon: FolderTree },
      { to: "/spc/query", text: "spc.navigation.qualityHistory", icon: Search }
    ]
  },
  {
    title: "spc.navigation.administration",
    items: [
      { to: "/guide", text: "spc.navigation.guide", icon: BookOpen },
      { to: "/calibration-instruments", text: "spc.navigation.calibration", icon: CalendarClock },
      { to: "/settings/smtp", text: "spc.navigation.smtp", icon: Sliders },
      { to: "/settings/spc-reports", text: "spc.navigation.reportSettings", icon: CalendarClock },
      { to: "/operators", text: "spc.navigation.users", icon: Users }
    ]
  }
];

const menuCategories = computed(() => canEdit.value ? editorMenuCategories : viewerMenuCategories);

function logout() {
  clearAuthSession();
  window.location.assign(portalLogoutUrl);
}

function returnToPortal() {
  window.location.assign(portalHomeUrl);
}
</script>

<template>
  <div class="min-h-screen flex flex-col bg-slate-100 dark:bg-slate-950 font-sans text-slate-800 dark:text-slate-100 transition-colors duration-300">
    <!-- Premium Top Navigation Bar -->
    <header class="sticky top-0 z-50 flex items-center justify-between min-h-16 flex-wrap gap-3 px-3 py-3 md:px-6 bg-slate-900/90 dark:bg-slate-900/80 backdrop-blur-md border-b border-slate-700/50 shadow-lg text-white">
      <div class="flex items-center gap-3">
        <div class="p-2.5 bg-gradient-to-tr from-blue-600 to-indigo-500 rounded-xl shadow-md shadow-indigo-500/20 flex items-center justify-center">
          <Activity class="w-6 h-6 text-white animate-pulse" />
        </div>
        <div>
          <h1 class="text-xl font-black tracking-wider bg-gradient-to-r from-blue-400 via-indigo-300 to-cyan-300 bg-clip-text text-transparent">
            PMR SPC <span class="text-xs px-2 py-0.5 ml-2 font-bold bg-blue-600/40 text-blue-300 border border-blue-500/30 rounded-full uppercase tracking-widest">Enterprise</span>
          </h1>
          <p class="text-[11px] text-slate-400 -mt-1 tracking-tight font-medium">{{ t("spc.shell.subtitle") }}</p>
        </div>
      </div>
      
      <div class="flex items-center flex-wrap gap-3 min-w-0">
        <LocalePicker />
        <!-- Theme Toggle Button -->
        <button
          @click="toggleDarkMode"
          type="button"
          class="p-2 rounded-xl bg-slate-800 dark:bg-slate-800 hover:bg-slate-700 text-yellow-400 dark:text-blue-300 transition-all shadow-sm border border-slate-700"
          :title="t(isDark ? 'common.theme.light' : 'common.theme.dark')"
          :aria-label="t(isDark ? 'common.theme.light' : 'common.theme.dark')"
        >
          <Sun v-if="isDark" class="w-5 h-5" />
          <Moon v-else class="w-5 h-5 text-indigo-400" />
        </button>

        <!-- Auth info -->
        <div v-if="authOn" class="flex flex-wrap items-center gap-3 text-sm pl-2 border-l border-slate-700/80">
          <button
            type="button"
            class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-slate-800 hover:bg-blue-600/80 hover:text-white border border-slate-700 transition-all text-slate-300"
            :title="t('spc.shell.portalTitle')"
            @click="returnToPortal"
          >
            <House class="w-3.5 h-3.5" /> {{ t("spc.shell.portal") }}
          </button>
          <div v-if="currentUser" class="text-right leading-tight">
            <div class="text-xs font-bold text-white">{{ currentUser.displayName || currentUser.username }}</div>
            <div class="text-[10px] text-blue-300">{{ t(currentUser.role === 'Viewer' ? 'spc.shell.viewer' : 'spc.shell.editor') }}</div>
          </div>
          <button
            type="button"
            class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-slate-800 hover:bg-red-600/80 hover:text-white border border-slate-700 transition-all text-slate-300"
            @click="logout"
          >
            <LogOut class="w-3.5 h-3.5" /> {{ t("spc.shell.logout") }}
          </button>
        </div>
      </div>
    </header>
    <p v-if="localeUnavailable" role="status" class="px-4 py-2 bg-amber-100 text-amber-950 dark:bg-amber-950 dark:text-amber-100">{{ t('common.language.unavailable') }}</p>

    <div v-if="showNav" class="flex flex-1 overflow-hidden" :class="stackedMobileNav ? 'flex-col md:flex-row' : ''">
      <!-- Premium Collapsible/Stylized Sidebar -->
      <aside :class="stackedMobileNav ? 'max-md:w-full max-md:max-h-40' : ''" class="w-64 flex-shrink-0 bg-white/95 dark:bg-slate-900/95 border-r border-slate-200 dark:border-slate-800/80 p-4 space-y-6 overflow-y-auto backdrop-blur-sm shadow-xl flex flex-col justify-between">
        <div class="space-y-6">
          <div v-for="cat in menuCategories" :key="cat.title" class="space-y-1.5">
            <h3 class="px-3 text-[11px] font-bold uppercase tracking-wider text-slate-400 dark:text-slate-500 flex items-center gap-2">
              <span class="w-1.5 h-1.5 rounded-full bg-blue-500"></span> {{ t(cat.title) }}
            </h3>
            <div class="space-y-1">
              <router-link
                v-for="m in cat.items"
                :key="m.to"
                :to="m.to"
                class="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-semibold text-slate-600 dark:text-slate-300 hover:bg-blue-50 hover:text-blue-600 dark:hover:bg-slate-800/80 dark:hover:text-blue-400 transition-all group"
                active-class="bg-gradient-to-r from-blue-600 to-indigo-600 text-white dark:from-blue-600 dark:to-indigo-600 dark:text-white shadow-lg shadow-blue-500/20 dark:shadow-blue-900/30 font-bold"
              >
                <component :is="m.icon" class="w-4 h-4 shrink-0 transition-transform group-hover:scale-110" />
                <span class="flex-1 min-w-0 whitespace-normal break-words">{{ t(m.text) }}</span>
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
              <span class="font-black tracking-wider">{{ environmentLabel(webEnvironment) }}</span>
              <span class="h-2 w-2 rounded-full" :class="apiOnline ? 'bg-emerald-500' : 'bg-red-500'"></span>
            </div>
            <div class="flex justify-between"><span>WEB</span><span class="font-mono">v{{ appVersion }}</span></div>
            <div class="flex justify-between"><span>API</span><span class="font-mono">{{ apiOnline ? `v${apiVersion}` : t('spc.shell.offline') }}</span></div>
            <p v-if="environmentMismatch" class="font-bold text-red-600 dark:text-red-400">{{ t('spc.shell.mismatch') }}</p>
          </div>
          <p class="text-center">© 2026 PMR Quality System</p>
        </div>
      </aside>

      <!-- Main Workspace -->
      <main
        class="flex-1 overflow-y-auto bg-slate-50 dark:bg-slate-950 transition-colors"
        :class="[route.path === '/spc' ? 'p-3' : 'p-6', stackedMobileNav ? 'min-w-0' : '']"
      >
        <div
          class="w-full mx-auto space-y-6"
          :class="route.path === '/spc' ? 'max-w-none' : 'max-w-[1800px]'"
        >
          <router-view :key="$route.path" />
        </div>
      </main>
    </div>

    <!-- No Nav View (e.g. Login) -->
    <main v-else class="flex-1 p-6 flex items-center justify-center bg-slate-900/40">
      <router-view :key="$route.path" />
    </main>
  </div>
</template>
