import axios from "axios";

const baseURL = import.meta.env.DEV
  ? "/api"
  : import.meta.env.VITE_API_BASE || "http://localhost:5243/api";

export const api = axios.create({ baseURL });

const tokenKey = "mes_spc_token";

api.interceptors.request.use((config) => {
  const t = localStorage.getItem(tokenKey);
  if (t) {
    config.headers.Authorization = `Bearer ${t}`;
  }
  return config;
});

if (import.meta.env.VITE_AUTH_ENABLED === "true") {
  api.interceptors.response.use(
    (r) => r,
    (err) => {
      if (err?.response?.status === 401 && typeof window !== "undefined") {
        localStorage.removeItem(tokenKey);
        if (!window.location.pathname.includes("/login")) {
          window.location.href = `/login?redirect=${encodeURIComponent(window.location.pathname)}`;
        }
      }
      return Promise.reject(err);
    }
  );
}

export function getStoredToken() {
  return localStorage.getItem(tokenKey);
}

export function setStoredToken(token) {
  if (token) localStorage.setItem(tokenKey, token);
  else localStorage.removeItem(tokenKey);
}

/** 附帶於 catch，讓頁面顯示可讀訊息（含後端未啟動） */
export function getApiErrorMessage(err) {
  const msg = err?.message || "";
  const code = err?.code || "";
  if (!err?.response && (code === "ERR_NETWORK" || code === "ECONNREFUSED" || /Network Error/i.test(msg))) {
    return "無法連線到後端 API。請先在同一台電腦於專案根目錄執行：dotnet run --project backend/MesSpc.Api/MesSpc.Api.csproj（預設 http://localhost:5243），再重新整理此頁。";
  }
  if (err?.response?.status === 401) {
    return "未授權（401）。若已啟用登入，請先至登入頁取得 Token。";
  }
  return err?.response?.data?.title || err?.response?.data?.message || msg || "請求失敗";
}
