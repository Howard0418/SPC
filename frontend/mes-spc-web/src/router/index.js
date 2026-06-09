import { createRouter, createWebHistory } from "vue-router";
import DashboardView from "../views/DashboardView.vue";
import MeasurementEntryView from "../views/MeasurementEntryView.vue";
import SpcChartView from "../views/SpcChartView.vue";
import AlertsView from "../views/AlertsView.vue";
import LoginView from "../views/LoginView.vue";
import AlertsWorkflowView from "../views/AlertsWorkflowView.vue";
import PartsView from "../views/PartsView.vue";
import ProcessesView from "../views/ProcessesView.vue";
import MachinesView from "../views/MachinesView.vue";
import CharacteristicsView from "../views/CharacteristicsView.vue";
import PartProcessCharacteristicsView from "../views/PartProcessCharacteristicsView.vue";
import ControlChartGroupsView from "../views/ControlChartGroupsView.vue";
import ControlChartCategoriesView from "../views/ControlChartCategoriesView.vue";
import ControlChartTypesView from "../views/ControlChartTypesView.vue";
import SpcRuleGroupsView from "../views/SpcRuleGroupsView.vue";
import VariableUploadView from "../views/VariableUploadView.vue";
import AttributeUploadView from "../views/AttributeUploadView.vue";
import UploadPreviewView from "../views/UploadPreviewView.vue";
import SpcQueryView from "../views/SpcQueryView.vue";
import SpcAlertsView from "../views/SpcAlertsView.vue";
import SmtpSettingsView from "../views/SmtpSettingsView.vue";
import OperatorsView from "../views/OperatorsView.vue";
import TrendChartView from "../views/TrendChartView.vue";

import GenealogyView from "../views/GenealogyView.vue";
import TraceabilityMasterView from "../views/TraceabilityMasterView.vue";

const authRequired = import.meta.env.VITE_AUTH_ENABLED === "true";

const routes = [
  { path: "/login", component: LoginView },
  { path: "/", component: DashboardView },
  { path: "/products", redirect: "/parts" },
  { path: "/stations", redirect: "/processes" },
  { path: "/inspection-items", redirect: "/characteristics" },
  { path: "/measurements", component: MeasurementEntryView },
  { path: "/csv-import", redirect: "/uploads/variable" },
  { path: "/spc", component: SpcChartView },
  { path: "/trend-chart", component: TrendChartView },
  { path: "/alerts", component: AlertsView },
  { path: "/v2/work-orders", redirect: "/measurements" },
  { path: "/v2/station-ops", redirect: "/measurements" },
  { path: "/v2/traceability", redirect: "/spc/query" },
  { path: "/alerts-workflow", component: AlertsWorkflowView },
  { path: "/v2/alerts-workflow", redirect: "/alerts-workflow" },
  { path: "/parts", component: PartsView },
  { path: "/processes", component: ProcessesView },
  { path: "/machines", component: MachinesView },
  { path: "/characteristics", component: CharacteristicsView },
  { path: "/part-process-characteristics", component: PartProcessCharacteristicsView },
  { path: "/control-chart-groups", component: ControlChartGroupsView },
  { path: "/control-chart-categories", component: ControlChartCategoriesView },
  { path: "/control-chart-types", component: ControlChartTypesView },
  { path: "/spc-rule-groups", component: SpcRuleGroupsView },
  { path: "/uploads/variable", component: VariableUploadView },
  { path: "/uploads/attribute", component: AttributeUploadView },
  { path: "/uploads/:batchId/preview", component: UploadPreviewView },
  { path: "/spc/query", component: SpcQueryView },
  { path: "/spc/alerts", component: SpcAlertsView },
  { path: "/settings/smtp", component: SmtpSettingsView },
  { path: "/operators", component: OperatorsView },
  { path: "/genealogy", component: GenealogyView },
  { path: "/traceability-master", component: TraceabilityMasterView },
  { path: "/guide", component: () => import("../views/SystemGuideView.vue") }
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
  return true;
});

export default router;
