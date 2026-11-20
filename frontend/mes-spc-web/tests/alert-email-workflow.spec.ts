import { expect, test } from "@playwright/test";

test("email link locates a closed alert, opens its chart and persists disposition", async ({ page }) => {
  const alert = { id: 800, status: "Closed", isAcknowledged: true, ppcId: 73, chartDate: "2026-11-20", correctiveAction: "" };
  await page.addInitScript(() => {
    localStorage.setItem("mes_spc_token", "alert-email-test-token");
    localStorage.setItem("mes_spc_user", JSON.stringify({ id: 1, role: "Editor" }));
  });
  await page.route(url => url.pathname.startsWith("/api/"), async route => {
    const { pathname } = new URL(route.request().url());
    if (pathname === "/api/version") return route.fulfill({ json: { version: "test", environment: "test" } });
    if (pathname === "/api/v1/alerts/800") return route.fulfill({ json: alert });
    if (pathname === "/api/v1/alerts/800/workflow") {
      expect(route.request().method()).toBe("PUT");
      Object.assign(alert, route.request().postDataJSON());
      return route.fulfill({ json: alert });
    }
    return route.fulfill({ json: [] });
  });
  await page.route("**/spc?ppcId=73&startDate=2026-11-20&endDate=2026-11-20", route =>
    route.fulfill({ contentType: "text/html", body: "<h1>Control chart for PPC 73</h1>" }));
  await page.goto("/alerts-workflow?alertId=800");
  await expect(page.frameLocator('[data-testid="alert-linked-chart"]').getByRole("heading")).toHaveText("Control chart for PPC 73");
  await page.getByTestId("alerts-openResolveModal").click();
  const dialog = page.getByTestId("alerts-workflow-dialog");
  await dialog.getByTestId("alerts-form.correctiveAction").fill("已完成處置");
  await dialog.locator('button[type="submit"]').click();
  await expect(dialog).toHaveCount(0);
  await page.reload();
  await page.getByTestId("alerts-openResolveModal").click();
  await expect(dialog.getByTestId("alerts-form.correctiveAction")).toHaveValue("已完成處置");
});
