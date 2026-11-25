export const pageRegistry = Object.freeze([
  { permission: 'analysis.etch', path: '/etch-analysis', labelKey: 'spc.navigation.etchAnalysis', categoryKey: 'spc.navigation.processAnalysis', icon: 'FlaskConical', viewerDefault: true },
  { permission: 'analysis.spc', path: '/spc', labelKey: 'spc.navigation.spcAnalysis', categoryKey: 'spc.navigation.processAnalysis', icon: 'LineChart', viewerDefault: true },
  { permission: 'analysis.particles', path: '/particle-monitoring', labelKey: 'spc.navigation.particleMonitoring', categoryKey: 'spc.navigation.processAnalysis', icon: 'Wind', viewerDefault: true },
  { permission: 'analysis.capability', path: '/process-analysis/capability', labelKey: 'spc.navigation.processCapability', categoryKey: 'spc.navigation.processAnalysis', icon: 'LineChart', viewerDefault: true },
  { permission: 'analysis.trend', path: '/trend-chart', labelKey: 'spc.navigation.measurementTrends', categoryKey: 'spc.navigation.processAnalysis', icon: 'TrendingUp', viewerDefault: true },
  { permission: 'analysis.violations', path: '/process-analysis/violations', labelKey: 'spc.navigation.violationAnalysis', categoryKey: 'spc.navigation.processAnalysis', icon: 'AlertTriangle', viewerDefault: true },
  { permission: 'analysis.reports', path: '/monthly-control-chart', labelKey: 'spc.navigation.periodReports', categoryKey: 'spc.navigation.processAnalysis', icon: 'CalendarClock', viewerDefault: true },

  { permission: 'data.measurements', path: '/measurements', labelKey: 'spc.navigation.measurementEntry', categoryKey: 'spc.navigation.importSampling', icon: 'PlusCircle', editorOnly: true },
  { permission: 'data.upload', path: '/uploads', labelKey: 'spc.navigation.importBatches', categoryKey: 'spc.navigation.importSampling', icon: 'UploadCloud', editorOnly: true },

  { permission: 'master.spc', path: '/part-process-characteristics', labelKey: 'spc.navigation.controlItems', categoryKey: 'spc.navigation.qualityMaster', icon: 'FolderTree', editorOnly: true },
  { permission: 'master.analysis_limits', path: '/analysis-limit-overview', labelKey: 'spc.navigation.analysisLimits', categoryKey: 'spc.navigation.qualityMaster', icon: 'Sliders', editorOnly: true },
  { permission: 'master.chemical_overview', path: '/chemical-analysis-overview', labelKey: 'spc.navigation.lineAnalysis', categoryKey: 'spc.navigation.qualityMaster', icon: 'FlaskConical', editorOnly: true },
  { permission: 'master.chemical_f', path: '/chemical-f-table', labelKey: 'spc.navigation.chemicalTables', categoryKey: 'spc.navigation.qualityMaster', icon: 'FlaskConical', editorOnly: true },
  { permission: 'master.chemical_adjustment', path: '/chemical-adjustment-parameters', labelKey: 'spc.navigation.chemicalAdjustmentParameters', categoryKey: 'spc.navigation.qualityMaster', icon: 'FlaskConical', editorOnly: true },
  { permission: 'master.process', path: '/processes', labelKey: 'spc.navigation.processes', categoryKey: 'spc.navigation.qualityMaster', icon: 'Layers', editorOnly: true },
  { permission: 'master.parts', path: '/parts', labelKey: 'spc.navigation.parts', categoryKey: 'spc.navigation.qualityMaster', icon: 'Package', editorOnly: true },
  { permission: 'master.characteristics', path: '/characteristics', labelKey: 'spc.navigation.characteristics', categoryKey: 'spc.navigation.qualityMaster', icon: 'Sliders', editorOnly: true },
  { permission: 'master.traceability', path: '/traceability-master', labelKey: 'spc.navigation.linesTanks', categoryKey: 'spc.navigation.qualityMaster', icon: 'Layers', editorOnly: true },
  { permission: 'master.chart_groups', path: '/control-chart-groups', labelKey: 'spc.navigation.chartCategories', categoryKey: 'spc.navigation.qualityMaster', icon: 'Layers', editorOnly: true },
  { permission: 'master.spc_rules', path: '/spc-rule-groups', labelKey: 'spc.navigation.alertRules', categoryKey: 'spc.navigation.qualityMaster', icon: 'Activity', editorOnly: true },

  { permission: 'alerts.overview', path: '/alerts', labelKey: 'spc.navigation.alerts', categoryKey: 'spc.navigation.alertsTraceability', icon: 'AlertTriangle', editorOnly: true },
  { permission: 'alerts.workflow', path: '/alerts-workflow', labelKey: 'spc.navigation.alertWorkflow', categoryKey: 'spc.navigation.alertsTraceability', icon: 'Activity', editorOnly: true },
  { permission: 'trace.genealogy', path: '/genealogy', labelKey: 'spc.navigation.genealogy', categoryKey: 'spc.navigation.alertsTraceability', icon: 'FolderTree', editorOnly: true },
  { permission: 'trace.quality_history', path: '/spc/query', labelKey: 'spc.navigation.qualityHistory', categoryKey: 'spc.navigation.alertsTraceability', icon: 'Search', editorOnly: true },

  { permission: 'admin.guide', path: '/guide', labelKey: 'spc.navigation.guide', categoryKey: 'spc.navigation.administration', icon: 'BookOpen', editorOnly: true },
  { permission: 'calibration.manage', path: '/calibration-instruments', labelKey: 'spc.navigation.calibration', categoryKey: 'spc.navigation.administration', icon: 'CalendarClock', editorOnly: true },
  { permission: 'admin.smtp', path: '/settings/smtp', labelKey: 'spc.navigation.smtp', categoryKey: 'spc.navigation.administration', icon: 'Sliders', editorOnly: true },
  { permission: 'admin.reports', path: '/settings/spc-reports', labelKey: 'spc.navigation.reportSettings', categoryKey: 'spc.navigation.administration', icon: 'CalendarClock', editorOnly: true },
  { permission: 'admin.operators', path: '/operators', labelKey: 'spc.navigation.users', categoryKey: 'spc.navigation.administration', icon: 'Users', editorOnly: true },
  { permission: 'admin.audit.read', path: '/audit-events', labelKey: 'spc.navigation.auditEvents', categoryKey: 'spc.navigation.administration', icon: 'ShieldCheck', explicitOnly: true }
]);

const exactPermissionByPath = new Map(pageRegistry.map(page => [page.path, page.permission]));

export function permissionForPath(path) {
  if (exactPermissionByPath.has(path)) return exactPermissionByPath.get(path);
  if (/^\/spc\/control-chart(?:\/|$)/.test(path)) return 'analysis.spc';
  if (/^\/spc\/trend(?:\/|$)/.test(path)) return 'analysis.trend';
  if (/^\/uploads\/[^/]+\/preview$/.test(path)) return 'data.upload';
  return null;
}

export function firstAccessiblePath(user) {
  const permissions = new Set(Array.isArray(user?.permissions) ? user.permissions : []);
  const isEditor = user?.role === 'Editor';
  return pageRegistry.find(page => (!page.editorOnly || isEditor) && permissions.has(page.permission))?.path ?? null;
}
