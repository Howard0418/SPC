<script setup>
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";

const authOn = import.meta.env.VITE_AUTH_ENABLED === "true";
const route = useRoute();
const router = useRouter();
const showNav = computed(() => !authOn || route.path !== "/login");

const menus = [
  { to: "/", text: "Dashboard" },
  { to: "/products", text: "產品管理" },
  { to: "/stations", text: "工站管理" },
  { to: "/inspection-items", text: "檢測項目管理" },
  { to: "/measurements", text: "量測資料輸入" },
  { to: "/csv-import", text: "CSV 匯入" },
  { to: "/spc", text: "SPC 管制圖" },
  { to: "/alerts", text: "異常清單" },
  { to: "/v2/work-orders", text: "V2 工單管理" },
  { to: "/v2/station-ops", text: "V2 工站作業" },
  { to: "/v2/traceability", text: "V2 追溯查詢" },
  { to: "/v2/alerts-workflow", text: "V2 異常流程" }
];

function logout() {
  localStorage.removeItem("mes_spc_token");
  router.push("/login");
}
</script>

<template>
  <div class="min-h-screen">
    <header class="flex items-center justify-between bg-slate-800 px-4 py-3 text-white">
      <h1 class="text-xl font-bold">MES + SPC MVP</h1>
      <div v-if="authOn" class="flex items-center gap-3 text-sm">
        <router-link to="/login" class="underline">登入</router-link>
        <button type="button" class="rounded bg-slate-600 px-2 py-1" @click="logout">登出</button>
      </div>
    </header>
    <div v-if="showNav" class="flex">
      <aside class="w-56 bg-white border-r min-h-[calc(100vh-64px)] p-3 space-y-2">
        <router-link
          v-for="m in menus"
          :key="m.to"
          :to="m.to"
          class="block px-3 py-2 rounded hover:bg-slate-100"
        >
          {{ m.text }}
        </router-link>
      </aside>
      <main class="flex-1 p-6">
        <router-view />
      </main>
    </div>
    <main v-else class="p-6">
      <router-view />
    </main>
  </div>
</template>
