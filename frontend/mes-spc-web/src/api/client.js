import axios from "axios";
import { clearAuthSession } from "../utils/auth";
import { resolveApiBase } from "../utils/apiBase";
import { i18n, apiMessageSource } from "../i18n";
import { normalizeLocale } from "../i18n/preferences";
import { formatApiErrorDescriptor, resolveLegacyApiDescriptor } from "../i18n/apiMessages";

const webEnvironment = import.meta.env.VITE_APP_ENV || "unknown";
const baseURL = resolveApiBase(webEnvironment, import.meta.env.VITE_PRODUCTION_API_BASE);
let apiEnvironment = "unknown";

export const api = axios.create({ baseURL });

const tokenKey = "mes_spc_token";
const redirectGuardKey = "mes_spc_auth_redirecting_until";
const ssoGraceUntilKey = "mes_spc_sso_grace_until";
const redirectCooldownMs = 5000;
const ssoGraceMs = 8000;

function nowMs() {
  return Date.now();
}

function readSessionTimestamp(key) {
  const value = Number(sessionStorage.getItem(key) || "0");
  return Number.isFinite(value) ? value : 0;
}

function isWithinSessionWindow(key) {
  return readSessionTimestamp(key) > nowMs();
}

api.interceptors.request.use((config) => {
  config.headers['X-SPC-UI-Locale'] = normalizeLocale(i18n.global.locale.value);
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
      if (isWithinSessionWindow(ssoGraceUntilKey) || isWithinSessionWindow(redirectGuardKey)) {
        return Promise.reject(err);
      }
      sessionStorage.setItem(redirectGuardKey, String(nowMs() + redirectCooldownMs));
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

export function markSsoGracePeriod() {
  sessionStorage.setItem(ssoGraceUntilKey, String(nowMs() + ssoGraceMs));
  sessionStorage.removeItem(redirectGuardKey);
}

/** 附帶於 catch，讓頁面顯示可讀訊息（含後端未啟動） */
export function getApiErrorMessage(err) {
  const msg = err?.message || "";
  const code = err?.code || "";
  const translate = (key,args) => i18n.global.t(key,args);
  if (code === "ENVIRONMENT_MISMATCH") return translate('spc.api.environmentMismatch',{webEnvironment,apiEnvironment});
  const knownMessage = formatApiErrorDescriptor(err?.response?.data,apiMessageSource,translate);
  if (knownMessage !== null) return knownMessage;
  const legacyDescriptor = resolveLegacyApiDescriptor(err?.response?.data,err?.response?.headers,apiMessageSource);
  if (legacyDescriptor) return translate(legacyDescriptor.key,legacyDescriptor.args);
  if (!err?.response && (code === "ERR_NETWORK" || code === "ECONNREFUSED" || /Network Error/i.test(msg))) {
    return translate('spc.api.network',{baseUrl:baseURL});
  }
  if (err?.response?.status === 401) {
    return translate('spc.api.unauthorized');
  }
  if (err?.response?.status === 409) {
    return err?.response?.data?.message || translate('spc.api.conflict');
  }
  if (err?.response?.status === 403) {
    return err?.response?.data?.message || translate('spc.api.forbidden');
  }
  const responseData = err?.response?.data;
  return (typeof responseData === "string" && responseData.trim()) || responseData?.title || responseData?.message || msg || translate('spc.api.requestFailed');
}
