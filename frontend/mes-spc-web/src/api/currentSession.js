import { api, getStoredToken } from "./client";
import { setCurrentUser } from "../utils/auth";

let lastAppliedRequest = 0;
let requestSequence = 0;

export async function syncCurrentSession() {
  const token = getStoredToken();
  if (!token) return false;
  const request = ++requestSequence;
  const { data } = await api.get("/v1/auth/session", { timeout: 5000 });
  if (getStoredToken() !== token) return false;
  if (request < lastAppliedRequest) return true;

  const user = data?.user;
  if (!Number.isInteger(user?.id) || user.id <= 0 || !user.username
      || !["Editor", "Viewer"].includes(user.role) || !Array.isArray(user.permissions)
      || !user.permissions.every(code => typeof code === "string")) return false;

  setCurrentUser(user);
  lastAppliedRequest = request;
  return true;
}
