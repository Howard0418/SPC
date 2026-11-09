const dictionaries = {
  pages: {
    "admin.audit.events": "auditEvents", "admin.operators": "operators",
    "analysis.spc.overall": "spcOverall", "analysis.spc.control_chart": "spcControlChart",
    "analysis.trend.overall": "trendOverall", "analysis.trend.ppc": "trendPpc",
    "measurement.entry": "measurementEntry", "chemical.analysis.overview": "chemicalOverview",
    "chemical.ftable": "chemicalFTable", "data.upload": "dataUpload"
  },
  events: {
    "navigation.page.enter": "pageEnter", "read.query.execute": "queryExecute",
    "read.filter.apply": "filterApply", "read.export.generate": "exportGenerate",
    "auth.sso.exchange": "ssoExchange", "auth.logout.request": "logoutRequest",
    "security.operator.change_permissions": "changePermissions",
    "audit.events.query": "auditQuery", "audit.events.export": "auditExport"
  },
  outcomes: { Succeeded: "succeeded", Denied: "denied", Failed: "failed" },
  sources: { WebRouter: "webRouter", Api: "api", PortalSso: "portalSso", System: "system" },
  targets: { Operator: "operator", AuditEvent: "auditEvent", Export: "export" }
};

export function describeAuditCode(kind, value, t) {
  if (value === null || value === undefined || value === "") return { label: "-", code: "" };
  const raw = String(value);
  const key = dictionaries[kind]?.[raw];
  return key ? { label: t(`spc.audit.labels.${kind}.${key}`), code: raw } : { label: raw, code: "" };
}

export function describeAuditTarget(type, id, t) {
  if ((type === null || type === undefined || type === "") && (id === null || id === undefined || id === ""))
    return { label: "-", code: "" };
  const rawType = type === null || type === undefined ? "" : String(type);
  const rawId = id === null || id === undefined ? "" : String(id);
  const key = dictionaries.targets[rawType];
  const labelType = key ? t(`spc.audit.labels.targets.${key}`) : rawType;
  return { label: [labelType, rawId].filter(Boolean).join(" / "), code: key ? rawType : "" };
}
