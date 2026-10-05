<script setup>
import { onBeforeMount } from "vue";
import { useRoute } from "vue-router";

const route = useRoute();
const portalLaunchUrl = (() => {
  const configured = import.meta.env.VITE_PORTAL_URL || "http://172.16.110.27/";
  const url = new URL(configured, window.location.origin);
  url.pathname = `${url.pathname.replace(/\/login\/?$/i, "").replace(/\/$/, "")}/Spc/Launch`;
  url.search = "";
  return url.toString();
})();

onBeforeMount(() => {
  if (import.meta.env.VITE_AUTH_ENABLED !== "true") return;
  const redir = route.query.redirect;
  if (typeof redir === "string" && redir.startsWith("/") && !redir.startsWith("//")) {
    sessionStorage.setItem("mes_spc_post_sso_redirect", redir);
  }
  window.location.replace(portalLaunchUrl);
});

</script>

<template></template>
