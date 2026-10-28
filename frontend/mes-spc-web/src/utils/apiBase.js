const formalApi = "https://PMR-SPC-SRV.pmr.com.tw:8443/api";
const formalApiOrigin = "https://pmr-spc-srv.pmr.com.tw:8443";

export function resolveApiBase(environment, productionApiBase) {
  // Test remains same-origin even when legacy build variables are present.
  if (environment !== "production" || !productionApiBase || productionApiBase === "/api") return "/api";
  try {
    const url = new URL(productionApiBase);
    if (url.username || url.password || url.search || url.hash ||
        url.origin !== formalApiOrigin || !["/api", "/api/"].includes(url.pathname)) {
      throw new Error();
    }
    return formalApi;
  } catch {
    throw new Error("正式 API 位址不在允許清單，請檢查發布設定。");
  }
}
