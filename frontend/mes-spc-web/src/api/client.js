import axios from "axios";
import { clearAuthSession } from "../utils/auth";

const baseURL = import.meta.env.VITE_API_BASE
  || (import.meta.env.DEV ? "/api" : "http://172.16.110.27:8082/api");
const webEnvironment = import.meta.env.VITE_APP_ENV || "unknown";
let apiEnvironment = "unknown";

export const api = axios.create({ baseURL });

const tokenKey = "mes_spc_token";

api.interceptors.request.use((config) => {
  const method = (config.method || "get").toLowerCase();
  const isWrite = !["get", "head", "options"].includes(method);
  if (isWrite && apiEnvironment !== "unknown" && apiEnvironment !== webEnvironment) {
    const error = new Error(`安全性阻擋：WEB 為 ${webEnvironment}，API 為 ${apiEnvironment}，環境不一致，禁止寫入。`);
    error.code = "ENVIRONMENT_MISMATCH";
    return Promise.reject(error);
  }
  const t = localStorage.getItem(tokenKey);
  if (t) {
    config.headers.Authorization = `Bearer ${t}`;
  }
  return config;
});

api.interceptors.response.use(
  (r) => r,
  (err) => {
    if (err?.response?.status === 401 && typeof window !== "undefined") {
      clearAuthSession();
      if (!window.location.pathname.includes("/login")) {
        const redirect = `${window.location.pathname}${window.location.search}${window.location.hash}`;
        window.location.replace(`/login?redirect=${encodeURIComponent(redirect)}`);
      }
    }
    return Promise.reject(err);
  }
);

export function getStoredToken() {
  return localStorage.getItem(tokenKey);
}

export function setStoredToken(token) {
  if (token) localStorage.setItem(tokenKey, token);
  else localStorage.removeItem(tokenKey);
}

export function setApiEnvironment(value) {
  apiEnvironment = value || "unknown";
}

/** 附帶於 catch，讓頁面顯示可讀訊息（含後端未啟動） */
export function getApiErrorMessage(err) {
  const msg = err?.message || "";
  const code = err?.code || "";
  if (code === "ENVIRONMENT_MISMATCH") return msg;
  if (!err?.response && (code === "ERR_NETWORK" || code === "ECONNREFUSED" || /Network Error/i.test(msg))) {
    return `無法連線到後端 API。請確認後端服務已在 ${baseURL} 啟動，再重新整理此頁。`;
  }
  if (err?.response?.status === 401) {
    return "未授權（401）。若已啟用登入，請先至登入頁取得 Token。";
  }
  if (err?.response?.status === 409) {
    return err?.response?.data?.message || "此資料已被其他記錄關聯，請先刪除或解除相關聯的子資料後再操作。";
  }
  if (err?.response?.status === 403) {
    return err?.response?.data?.message || "權限不足（403）。檢視者只能查看與查詢資料。";
  }
  const responseData = err?.response?.data;
  return (typeof responseData === "string" && responseData.trim()) || responseData?.title || responseData?.message || msg || "請求失敗";
}
