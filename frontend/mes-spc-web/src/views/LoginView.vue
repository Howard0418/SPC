<script setup>
import { onBeforeMount } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";

const route = useRoute();
const { t } = useI18n({ useScope: "global" });
const portalLaunchUrl = (() => {
  const configured = import.meta.env.VITE_PORTAL_URL || "https://portaltest.pmr.com.tw/";
  const url = new URL(configured, window.location.origin);
  url.pathname = `${url.pathname.replace(/\/login\/?$/i, "").replace(/\/$/, "")}/Login`;
  url.search = "?ReturnUrl=%2FSpc%2FLaunch";
  return url.toString();
})();

onBeforeMount(() => {
  if (import.meta.env.VITE_AUTH_ENABLED !== "true") return;
  if (route.query.access === "page" || route.query.access === "viewer") return;
  const redir = route.query.redirect;
  if (typeof redir === "string" && redir.startsWith("/") && !redir.startsWith("//")) {
    sessionStorage.setItem("mes_spc_post_sso_redirect", redir);
  }
  window.location.replace(portalLaunchUrl);
});

</script>

<template>
  <p v-if="route.query.access === 'page' || route.query.access === 'viewer'" role="alert"
    class="mx-auto mt-10 max-w-lg rounded-xl bg-white p-6 text-slate-800 shadow dark:bg-slate-900 dark:text-slate-100">
    {{ t('spc.help.noAccessiblePage') }}
  </p>
</template>
