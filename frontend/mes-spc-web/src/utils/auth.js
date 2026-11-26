const tokenKey = "mes_spc_token";
const userKey = "mes_spc_user";
export const authUserChanged = "spc-auth-user-changed";

export function getCurrentUser() {
  try {
    return JSON.parse(localStorage.getItem(userKey) || "null");
  } catch {
    return null;
  }
}

export function setAuthSession(token, user) {
  localStorage.setItem(tokenKey, token);
  setCurrentUser(user);
}

export function setCurrentUser(user) {
  const json = JSON.stringify(user || null);
  if (localStorage.getItem(userKey) === json) return;
  localStorage.setItem(userKey, json);
  window.dispatchEvent(new Event(authUserChanged));
}

export function clearAuthSession() {
  localStorage.removeItem(tokenKey);
  localStorage.removeItem(userKey);
  window.dispatchEvent(new Event(authUserChanged));
}

export function isEditor() {
  return getCurrentUser()?.role === "Editor";
}
