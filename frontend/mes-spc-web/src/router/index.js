import { createRouter, createWebHistory } from "vue-router";
import DashboardView from "../views/DashboardView.vue";
import MeasurementEntryView from "../views/MeasurementEntryView.vue";
import SpcChartView from "../views/SpcChartView.vue";
import AlertsView from "../views/AlertsView.vue";
import LoginView from "../views/LoginView.vue";
import AlertsWorkflowView from "../views/AlertsWorkflowView.vue";
import PartsView from "../views/PartsView.vue";
import ProcessesView from "../views/ProcessesView.vue";
import CharacteristicsView from "../views/CharacteristicsView.vue";
import PartProcessCharacteristicsView from "../views/PartProcessCharacteristicsView.vue";
import SpcRuleGroupsView from "../views/SpcRuleGroupsView.vue";
import VariableUploadView from "../views/VariableUploadView.vue";
import AttributeUploadView from "../views/AttributeUploadView.vue";
import UploadPreviewView from "../views/UploadPreviewView.vue";
import SpcQueryView from "../views/SpcQueryView.vue";
import SpcAlertsView from "../views/SpcAlertsView.vue";
import SmtpSettingsView from "../views/SmtpSettingsView.vue";
import OperatorsView from "../views/OperatorsView.vue";
import TrendChartView from "../views/TrendChartView.vue";
import SpcReportSettingsView from "../views/SpcReportSettingsView.vue";

import GenealogyView from "../views/GenealogyView.vue";
import TraceabilityMasterView from "../views/TraceabilityMasterView.vue";
import { getCurrentUser } from "../utils/auth";

const authRequired = import.meta.env.VITE_AUTH_ENABLED === "true";

const routes = [
  { path: "/login", component: LoginView },
  { path: "/", component: DashboardView },
  { path: "/products", redirect: "/parts" },
  { path: "/stations", redirect: "/processes" },
  { path: "/inspection-items", redirect: "/characteristics" },
  { path: "/measurements", component: MeasurementEntryView, meta: { editorOnly: true } },
  { path: "/csv-import", redirect: "/uploads/variable" },
  { path: "/spc", component: SpcChartView },
  { path: "/spc/control-chart/:ppcId?", component: SpcChartView },
  { path: "/trend-chart", component: TrendChartView },
  { path: "/spc/trend/:ppcId?", component: TrendChartView },
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
  { path: "/control-chart-groups", redirect: "/part-process-characteristics?tab=groups" },
  { path: "/control-chart-categories", redirect: "/part-process-characteristics?tab=categories" },
  { path: "/control-chart-types", redirect: "/part-process-characteristics?tab=types" },
  { path: "/spc-rule-groups", component: SpcRuleGroupsView, meta: { editorOnly: true } },
  { path: "/uploads/variable", component: VariableUploadView, meta: { editorOnly: true } },
  { path: "/uploads/attribute", component: AttributeUploadView, meta: { editorOnly: true } },
  { path: "/uploads/:batchId/preview", component: UploadPreviewView, meta: { editorOnly: true } },
  { path: "/spc/query", component: SpcQueryView, meta: { editorOnly: true } },
  { path: "/spc/alerts", component: SpcAlertsView, meta: { editorOnly: true } },
  { path: "/settings/smtp", component: SmtpSettingsView, meta: { editorOnly: true } },
  { path: "/settings/spc-reports", component: SpcReportSettingsView, meta: { editorOnly: true } },
  { path: "/operators", component: OperatorsView, meta: { editorOnly: true } },
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
  if (to.path === "/login") return true;
  if (!localStorage.getItem("mes_spc_token")) {
    return { path: "/login", query: { redirect: to.fullPath } };
  }
  if (to.meta.editorOnly && getCurrentUser()?.role !== "Editor") {
    return { path: "/", query: { access: "viewer" } };
  }
  return true;
});

export default router;
