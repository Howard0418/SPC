<script setup>
import { ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import axios from "axios";

const route = useRoute();
const router = useRouter();
const username = ref("demo");
const password = ref("demo123");
const err = ref("");
const baseURL = import.meta.env.DEV ? "/api/v1" : import.meta.env.VITE_API_BASE || "http://localhost:5243/api/v1";

const submit = async () => {
  err.value = "";
  try {
    const { data } = await axios.post(`${baseURL}/auth/login`, {
      username: username.value,
      password: password.value
    });
    localStorage.setItem("mes_spc_token", data.token);
    const redir = route.query.redirect;
    router.replace(typeof redir === "string" ? redir : "/");
  } catch (e) {
    err.value = e?.response?.data?.message || e?.message || "登入失敗";
  }
};
</script>

<template>
  <div class="mx-auto max-w-sm rounded-lg bg-white p-6 shadow">
    <h2 class="mb-4 text-xl font-semibold">登入</h2>
    <p v-if="err" class="mb-3 text-sm text-red-600">{{ err }}</p>
    <div class="space-y-3">
      <input v-model="username" class="w-full rounded border p-2" placeholder="帳號" autocomplete="username" />
      <input
        v-model="password"
        type="password"
        class="w-full rounded border p-2"
        placeholder="密碼"
        autocomplete="current-password"
      />
      <button type="button" class="w-full rounded bg-blue-600 py-2 text-white" @click="submit">登入</button>
    </div>
    <p class="mt-4 text-xs text-gray-500">預設帳密與後端 appsettings.json <code class="text-gray-700">Auth:DemoUsername</code> 一致。</p>
  </div>
</template>
