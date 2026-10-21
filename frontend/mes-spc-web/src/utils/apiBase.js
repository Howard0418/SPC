const formalApi = "http://172.16.119.140:8081/api";

export function resolveApiBase(environment, productionApiBase) {
  // Test remains same-origin even when legacy build variables are present.
  if (environment !== "production" || !productionApiBase || productionApiBase === "/api") return "/api";
  try {
    const url = new URL(productionApiBase);
    if (url.username || url.password || url.search || url.hash ||
        url.origin !== "http://172.16.119.140:8081" || !["/api", "/api/"].includes(url.pathname)) {
      throw new Error();
    }
    return formalApi;
  } catch {
    throw new Error("正式 API 位址不在允許清單，請檢查發布設定。");
  }
}
