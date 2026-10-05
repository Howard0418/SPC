import { createRouter, createWebHistory } from "vue-router";
import MeasurementEntryView from "../views/MeasurementEntryView.vue";
import SpcChartView from "../views/SpcChartView.vue";
import AlertsView from "../views/AlertsView.vue";
import LoginView from "../views/LoginView.vue";
import AlertsWorkflowView from "../views/AlertsWorkflowView.vue";
import PartsView from "../views/PartsView.vue";
import ProcessesView from "../views/ProcessesView.vue";
import CharacteristicsView from "../views/CharacteristicsView.vue";
import PartProcessCharacteristicsView from "../views/PartProcessCharacteristicsView.vue";
import ControlChartGroupsView from "../views/ControlChartGroupsView.vue";
import SpcRuleGroupsView from "../views/SpcRuleGroupsView.vue";
import DataUploadView from "../views/DataUploadView.vue";
import UploadPreviewView from "../views/UploadPreviewView.vue";
import SpcQueryView from "../views/SpcQueryView.vue";
import SpcAlertsView from "../views/SpcAlertsView.vue";
import SmtpSettingsView from "../views/SmtpSettingsView.vue";
import OperatorsView from "../views/OperatorsView.vue";
import TrendChartView from "../views/TrendChartView.vue";
import SpcReportSettingsView from "../views/SpcReportSettingsView.vue";
import PortalSsoView from "../views/PortalSsoView.vue";
import ChemicalAnalysisOverviewView from "../views/ChemicalAnalysisOverviewView.vue";
import ChemicalFTableView from "../views/ChemicalFTableView.vue";
import EquipmentStatusView from "../views/EquipmentStatusView.vue";
import EquipmentPointsView from "../views/EquipmentPointsView.vue";
import EquipmentMonitorView from "../views/EquipmentMonitorView.vue";
import EtchAnalysisView from "../views/EtchAnalysisView.vue";
import InstrumentCalibrationsView from "../views/InstrumentCalibrationsView.vue";
import ParticleMonitoringView from "../views/ParticleMonitoringView.vue";

import GenealogyView from "../views/GenealogyView.vue";
import TraceabilityMasterView from "../views/TraceabilityMasterView.vue";
import { getCurrentUser } from "../utils/auth";

const authRequired = import.meta.env.VITE_AUTH_ENABLED === "true";
const pagePermissionByPath = {
  "/etch-analysis": "analysis.etch", "/spc": "analysis.spc", "/process-analysis/capability": "analysis.capability",
  "/trend-chart": "analysis.trend", "/process-analysis/violations": "analysis.violations", "/monthly-control-chart": "analysis.reports",
  "/equipment-status": "equipment.status", "/equipment-points": "equipment.points", "/equipment-monitor": "equipment.monitor",
  "/measurements": "data.measurements", "/uploads": "data.upload", "/operators": "admin.operators",
  "/calibration-instruments": "calibration.manage"
  , "/particle-monitoring": "analysis.spc"
};

const routes = [
  { path: "/login", component: LoginView },
  { path: "/portal-sso", component: PortalSsoView },
  { path: "/", redirect: "/spc" },
  { path: "/calibration-instruments", component: InstrumentCalibrationsView },
  { path: "/products", redirect: "/parts" },
  { path: "/stations", redirect: "/processes" },
  { path: "/inspection-items", redirect: "/characteristics" },
  { path: "/measurements", component: MeasurementEntryView, meta: { editorOnly: true } },
  { path: "/csv-import", redirect: "/uploads" },
  { path: "/spc", component: SpcChartView },
  { path: "/particle-monitoring", component: ParticleMonitoringView },
  { path: "/etch-analysis", component: EtchAnalysisView },
  { path: "/process-analysis/capability", component: SpcChartView },
  { path: "/process-analysis/violations", component: SpcChartView },
  { path: "/spc/control-chart/:ppcId?", component: SpcChartView },
  { path: "/trend-chart", component: TrendChartView },
  { path: "/spc/trend/:ppcId?", component: TrendChartView },
  { path: "/monthly-control-chart", component: SpcChartView },
  { path: "/alerts", component: AlertsView, meta: { editorOnly: true } },
  { path: "/v2/work-orders", redirect: "/measurements" },
  { path: "/v2/station-ops", redirect: "/measurements" },
  { path: "/v2/traceability", redirect: "/spc/query" },
  { path: "/alerts-workflow", component: AlertsWorkflowView, meta: { editorOnly: true } },
  { path: "/v2/alerts-workflow", redirect: "/alerts-workflow" },
  { path: "/parts", component: PartsView, meta: { editorOnly: true } },
  { path: "/processes", component: ProcessesView, meta: { editorOnly: true } },
  { path: "/machines", redirect: "/processes" },
  { path: "/characteristics", component: CharacteristicsView, meta: { editorOnly: true } },
  { path: "/part-process-characteristics", component: PartProcessCharacteristicsView, meta: { editorOnly: true } },
  { path: "/chemical-analysis-overview", component: ChemicalAnalysisOverviewView, meta: { editorOnly: true } },
  { path: "/chemical-f-table", component: ChemicalFTableView, meta: { editorOnly: true } },
  { path: "/equipment-status", component: EquipmentStatusView },
  { path: "/equipment-points", component: EquipmentPointsView },
  { path: "/equipment-monitor", component: EquipmentMonitorView },
  { path: "/control-chart-groups", component: ControlChartGroupsView, props: { groupsOnly: true }, meta: { editorOnly: true } },
  { path: "/control-chart-categories", redirect: "/part-process-characteristics" },
  { path: "/control-chart-types", redirect: "/part-process-characteristics" },
  { path: "/spc-rule-groups", component: SpcRuleGroupsView, meta: { editorOnly: true } },
  { path: "/uploads", component: DataUploadView, meta: { editorOnly: true } },
  { path: "/uploads/variable", redirect: "/uploads" },
  { path: "/uploads/attribute", redirect: "/uploads?tab=attribute" },
  { path: "/uploads/:batchId/preview", component: UploadPreviewView, meta: { editorOnly: true } },
  { path: "/spc/query", component: SpcQueryView, meta: { editorOnly: true } },
  { path: "/spc/alerts", component: SpcAlertsView, meta: { editorOnly: true } },
  { path: "/settings/smtp", component: SmtpSettingsView, meta: { editorOnly: true } },
  { path: "/settings/spc-reports", component: SpcReportSettingsView, meta: { editorOnly: true } },
  { path: "/operators", component: OperatorsView, meta: { editorOnly: true } },
  { path: "/chemicals", redirect: "/spc" },
  { path: "/genealogy", component: GenealogyView, meta: { editorOnly: true } },
  { path: "/traceability-master", component: TraceabilityMasterView, meta: { editorOnly: true } },
  { path: "/guide", component: () => import("../views/SystemGuideView.vue"), meta: { editorOnly: true } }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach((to) => {
  if (!authRequired) return true;
  if (to.path === "/login" || to.path === "/portal-sso") return true;
  if (!localStorage.getItem("mes_spc_token")) {
    return { path: "/login", query: { redirect: to.fullPath } };
  }
  const user = getCurrentUser();
  const requiredPermission = pagePermissionByPath[to.path];
  if (requiredPermission && Array.isArray(user?.permissions) && user.permissions.length && !user.permissions.includes(requiredPermission))
    return { path: "/", query: { access: "page" } };
  if (to.meta.editorOnly && user?.role !== "Editor") {
    return { path: "/", query: { access: "viewer" } };
  }
  return true;
});

export default router;
