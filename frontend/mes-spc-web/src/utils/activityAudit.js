let activityApiClient = null;

/**
 * Configured by the shared Axios client after it has been created.
 * Keeping this one-way avoids a client <-> audit module initialization cycle.
 */
export function configureActivityAudit(client) {
  activityApiClient = client;
}

const exactPages = new Map([
  ["/calibration-instruments", "calibration.instruments"], ["/measurements", "measurement.entry"],
  ["/spc", "analysis.spc.overall"], ["/particle-monitoring", "analysis.particle.monitoring"],
  ["/etch-analysis", "analysis.etch"], ["/process-analysis/capability", "analysis.capability"],
  ["/process-analysis/violations", "analysis.violations"], ["/trend-chart", "analysis.trend.overall"],
  ["/monthly-control-chart", "report.monthly_control_chart"], ["/alerts", "alert.overview"], ["/alerts-workflow", "alert.workflow"],
  ["/parts", "master.parts"], ["/processes", "master.processes"], ["/characteristics", "master.characteristics"],
  ["/part-process-characteristics", "master.ppc"], ["/chemical-analysis-overview", "chemical.analysis.overview"],
  ["/chemical-f-table", "chemical.ftable"], ["/control-chart-groups", "spc.control_chart.groups"], ["/spc-rule-groups", "spc.rule.groups"],
  ["/uploads", "data.upload"], ["/spc/query", "traceability.query"], ["/spc/alerts", "spc.alerts"],
  ["/settings/smtp", "settings.smtp"], ["/settings/spc-reports", "settings.spc_reports"], ["/operators", "admin.operators"],
  ["/genealogy", "genealogy.overview"], ["/traceability-master", "traceability.master"], ["/guide", "help.guide"]
]);

export function resolveAuditPage(pathname) {
  if (exactPages.has(pathname)) return exactPages.get(pathname);
  if (/^\/spc\/control-chart(?:\/|$)/.test(pathname)) return "analysis.spc.control_chart";
  if (/^\/spc\/trend(?:\/|$)/.test(pathname)) return "analysis.trend.ppc";
  if (/^\/uploads\/[^/]+\/preview$/.test(pathname)) return "data.upload.preview";
  return null;
}

function retryOnce(payload) {
  window.setTimeout(() => {
    activityApiClient?.post("/v1/audit/activity", payload, { auditActivityInternal: true }).catch(() => {});
  }, 500);
}

export function recordActivity(payload) {
  if (!payload?.pageKey || !activityApiClient || !localStorage.getItem("mes_spc_token")) return;
  activityApiClient.post("/v1/audit/activity", payload, { auditActivityInternal: true }).catch(() => retryOnce(payload));
}

export function recordPageEnter(route, from, failure) {
  if (failure || !route?.matched?.length) return;
  const pageKey = resolveAuditPage(route.path);
  if (!pageKey) return;
  recordActivity({ activity: "PageEnter", pageKey, clientEventId: crypto.randomUUID(), navigationKind: "Push" });
}

function safeDate(value) {
  if (typeof value !== "string" && !(value instanceof Date)) return null;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

function responseCount(data) {
  if (Array.isArray(data)) return data.length;
  if (Array.isArray(data?.data)) return data.data.length;
  if (Array.isArray(data?.items)) return data.items.length;
  if (Array.isArray(data?.results)) return data.results.length;
  return 0;
}

export function recordSuccessfulRead(response) {
  const config = response?.config;
  if ((config?.method || "get").toLowerCase() !== "get" || config?.auditActivityInternal) return;
  const url = String(config?.url || "");
  if (!url.startsWith("/") || url.startsWith("/v1/audit/") || /\/(health|version)$/.test(url)) return;
  const pageKey = resolveAuditPage(window.location.pathname);
  if (!pageKey) return;
  const params = config?.params && typeof config.params === "object" ? config.params : {};
  const keys = Object.keys(params).filter(key => params[key] !== undefined && params[key] !== null && params[key] !== "");
  const lower = Object.fromEntries(keys.map(key => [key.toLowerCase(), params[key]]));
  const dateFrom = safeDate(lower.from ?? lower.start ?? lower.datefrom ?? lower.startdate);
  const dateTo = safeDate(lower.to ?? lower.end ?? lower.dateto ?? lower.enddate);
  const isExport = config?.responseType === "blob";
  const payload = isExport
    ? { activity: "Export", pageKey, exportFormat: "Xlsx", resultCount: responseCount(response.data), ...(dateFrom && dateTo ? { dateFromUtc: dateFrom, dateToUtc: dateTo } : {}) }
    : { activity: keys.length ? "Filter" : "Query", pageKey, filterFields: keys.sort(), resultCount: responseCount(response.data), ...(dateFrom && dateTo ? { dateFromUtc: dateFrom, dateToUtc: dateTo } : {}) };
  recordActivity(payload);
}
