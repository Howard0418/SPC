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
  ["/part-process-characteristics", "master.ppc"], ["/analysis-limit-overview", "master.analysis.limits"], ["/chemical-analysis-overview", "chemical.analysis.overview"],
  ["/chemical-f-table", "chemical.ftable"], ["/control-chart-groups", "spc.control_chart.groups"], ["/spc-rule-groups", "spc.rule.groups"],
  ["/uploads", "data.upload"], ["/spc/query", "traceability.query"], ["/spc/alerts", "spc.alerts"],
  ["/settings/smtp", "settings.smtp"], ["/settings/spc-reports", "settings.spc_reports"], ["/operators", "admin.operators"], ["/audit-events", "admin.audit.events"],
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

const actionRules = [
  ['post', /^\/settings\/smtp$/, 'settings.save'],
  ['post', /^\/settings\/smtp\/(?:test|test-alert)$/, 'notification.test'],
  ['put', /^\/v1\/spc-report-settings$/, 'settings.save'],
  ['post', /^\/v1\/spc-report-settings\/send-now$/, 'report.send'],
  ['post', /^\/v1\/chemical-f-table\/apply$/, 'ftable.apply'],
  ['post', /^\/v1\/chemical-f-table\/versions\/\d+\/restore$/, 'ftable.restore'],
  ['post', /^\/(?:v1\/)?part-process-characteristics\/\d+\/chemical-analysis-formula-versions\/\d+\/restore$/, 'formula.restore'],
  ['put', /^\/(?:v1\/)?part-process-characteristics\/\d+$/, 'formula.save'],
  ['post', /^\/(?:v1\/)?uploads\/(?:variable|attribute|particle\/preview|variable\/(?:excel|csv)|attribute\/(?:excel|csv))$/, 'import.preview'],
  ['post', /^\/(?:v1\/)?uploads\/[0-9a-f-]+\/confirm$/i, 'import.confirm'],
  ['delete', /^\/(?:v1\/)?uploads\/[0-9a-f-]+$/i, 'import.delete'],
  ['post', /^\/(?:v1\/)?alerts\/\d+\/ack$/, 'alert.acknowledge'],
  ['put', /^\/(?:v1\/)?alerts\/\d+\/workflow$/, 'alert.disposition.save'],
  ['post', /^\/(?:v1\/)?operators$/, 'operator.save'],
  ['put', /^\/(?:v1\/)?operators\/\d+$/, 'operator.save'],
  ['delete', /^\/(?:v1\/)?operators\/\d+$/, 'operator.delete'],
  ['post', /^\/(?:v1\/)?(?:parts|processes|machines|characteristics|control-chart-groups|spc-rule-groups|spc-rules)$/, 'record.save'],
  ['put', /^\/(?:v1\/)?(?:parts|processes|machines|characteristics|control-chart-groups|spc-rule-groups|spc-rules)\/\d+$/, 'record.save'],
  ['delete', /^\/(?:v1\/)?(?:parts|processes|machines|characteristics|control-chart-groups|spc-rule-groups|spc-rules)\/\d+$/, 'record.delete'],
  ['post', /^\/v1\/manual-measurements$/, 'record.save'],
  ['put', /^\/v1\/manual-measurements\/(?:items\/\d+|[0-9a-f-]+)$/i, 'record.save'],
  ['delete', /^\/v1\/manual-measurements\/items\/\d+$/, 'record.delete']
];

export function resolveBusinessAction(config) {
  if (config?.auditActivityInternal) return null;
  const method = String(config?.method || 'get').toLowerCase();
  if (!['post', 'put', 'patch', 'delete'].includes(method)) return null;
  let path;
  try { path = new URL(String(config?.url || ''), 'https://spc.invalid').pathname.replace(/^\/api(?=\/)/, ''); }
  catch { return null; }
  return actionRules.find(([verb, pattern]) => verb === method && pattern.test(path))?.[2] || null;
}

export function recordBusinessAction(response, outcome = 'Succeeded') {
  const config = response?.config;
  const actionKey = resolveBusinessAction(config);
  const pageKey = typeof window === 'undefined' ? null : resolveAuditPage(window.location.pathname);
  const status = Number(response?.status);
  if (!actionKey || !pageKey || !Number.isInteger(status) || status < 100 || status > 599) return;
  recordActivity({
    activity: 'BusinessAction', pageKey, clientEventId: crypto.randomUUID(), actionKey, outcome,
    requestMethod: String(config.method).toUpperCase(), httpStatus: status
  });
}
