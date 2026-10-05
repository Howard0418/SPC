<template>
  <main class="min-h-screen flex items-center justify-center bg-slate-100 p-6">
    <div class="rounded-xl bg-white p-8 shadow text-center">
      <h1 class="text-xl font-semibold mb-3">正在登入 SPC</h1>
      <p :class="error ? 'text-red-600' : 'text-slate-500'">{{ error || '請稍候，正在驗證 PmrPortal 登入資訊…' }}</p>
      <router-link v-if="error" to="/login" class="inline-block mt-4 text-blue-600">前往 SPC 登入頁</router-link>
    </div>
  </main>
</template>

<script setup>
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { setAuthSession } from "../utils/auth";
import { markSsoGracePeriod } from "../api/client";

const router = useRouter();
const error = ref("");

onMounted(async () => {
  const token = new URLSearchParams(window.location.hash.slice(1)).get("token") || "";
  history.replaceState(null, "", "/portal-sso");
  if (!token) { error.value = "缺少 SPC 單一登入資訊。"; return; }
  try {
    const encoded = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const binary = atob(encoded.padEnd(encoded.length + (4 - encoded.length % 4) % 4, '='));
    const bytes = Uint8Array.from(binary, character => character.charCodeAt(0));
    const payload = JSON.parse(new TextDecoder("utf-8").decode(bytes));
    const user = {
      id: Number(payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || 0),
      username: payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || payload.unique_name,
      displayName: payload.displayName,
      operatorCode: payload.operatorCode,
      role: payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || payload.role
    };
    const permissionClaim = payload.permission;
    user.permissions = Array.isArray(permissionClaim) ? permissionClaim : permissionClaim ? [permissionClaim] : [];
    setAuthSession(token, user);
    markSsoGracePeriod();
    const requestedPath = sessionStorage.getItem("mes_spc_post_sso_redirect") || "/spc";
    sessionStorage.removeItem("mes_spc_post_sso_redirect");
    const safePath = requestedPath.startsWith("/") && !requestedPath.startsWith("//") ? requestedPath : "/spc";
    await router.replace(safePath);
  } catch (e) {
    error.value = "SPC 單一登入資訊無效，請重新由 PmrPortal 進入。";
  }
});
</script>
