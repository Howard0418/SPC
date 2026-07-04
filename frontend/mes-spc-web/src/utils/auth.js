const tokenKey = "mes_spc_token";
const userKey = "mes_spc_user";

export function getCurrentUser() {
  try {
    return JSON.parse(localStorage.getItem(userKey) || "null");
  } catch {
    return null;
  }
}

export function setAuthSession(token, user) {
  localStorage.setItem(tokenKey, token);
  localStorage.setItem(userKey, JSON.stringify(user || null));
}

export function clearAuthSession() {
  localStorage.removeItem(tokenKey);
  localStorage.removeItem(userKey);
}

export function isEditor() {
  return getCurrentUser()?.role === "Editor";
}
