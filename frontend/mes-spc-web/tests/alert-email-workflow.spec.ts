import { expect, test } from "@playwright/test";

test("email link keeps the standard workflow list, focuses the matching alert and persists disposition", async ({ page }) => {
  const alert = { id: 800, status: "Closed", isAcknowledged: true, ppcId: 73, chartDate: "2026-11-20", correctiveAction: "" };
  const listAlert = { id: 1, status: "Open", isAcknowledged: false, message: "另一筆警報", occurredAt: "2026-11-20T01:00:00Z" };
  let listReads = 0;
  await page.addInitScript(() => {
    localStorage.setItem("mes_spc_token", "alert-email-test-token");
    localStorage.setItem("mes_spc_user", JSON.stringify({ id: 1, role: "Editor" }));
  });
  await page.route(url => url.pathname.startsWith("/api/"), async route => {
    const { pathname } = new URL(route.request().url());
    if (pathname === "/api/version") return route.fulfill({ json: { version: "test", environment: "test" } });
    if (pathname === "/api/v1/alerts") {
      listReads += 1;
      return route.fulfill({ json: [listAlert] });
    }
    if (pathname === "/api/v1/alerts/800") return route.fulfill({ json: alert });
    if (pathname === "/api/v1/alerts/800/workflow") {
      expect(route.request().method()).toBe("PUT");
      Object.assign(alert, route.request().postDataJSON());
      return route.fulfill({ json: alert });
    }
    return route.fulfill({ json: [] });
  });

  await page.goto("/alerts-workflow");
  await expect(page.getByTestId("alert-row-1")).toBeVisible();

  await page.goto("/alerts-workflow?alertId=800");
  await expect(page.getByTestId("alert-linked-chart")).toHaveCount(0);
  await expect(page.getByTestId("alerts-all-filter")).toContainText("2");
  const targetRow = page.getByTestId("alert-row-800");
  await expect(targetRow).toBeVisible();
  await expect(targetRow).toHaveAttribute("aria-current", "true");
  await expect(targetRow.getByRole("link", { name: /SPC/ })).toHaveAttribute("href", "/spc?ppcId=73");
  expect(listReads).toBeGreaterThanOrEqual(2);

  await targetRow.getByTestId("alerts-openResolveModal").click();
  const dialog = page.getByTestId("alerts-workflow-dialog");
  await dialog.getByTestId("alerts-form.correctiveAction").fill("已完成處置");
  await dialog.locator('button[type="submit"]').click();
  await expect(dialog).toHaveCount(0);
  await page.reload();
  await page.getByTestId("alerts-openResolveModal").click();
  await expect(dialog.getByTestId("alerts-form.correctiveAction")).toHaveValue("已完成處置");
});

for (const scenario of [
  { name: "invalid id", query: "bad", expectedTargetReads: 0 },
  { name: "missing id", query: "999", expectedTargetReads: 1 }
]) {
  test(`${scenario.name} keeps the standard workflow list visible`, async ({ page }) => {
    let targetReads = 0;
    await page.addInitScript(() => {
      localStorage.setItem("mes_spc_token", "alert-email-test-token");
      localStorage.setItem("mes_spc_user", JSON.stringify({ id: 1, role: "Editor" }));
    });
    await page.route(url => url.pathname.startsWith("/api/"), async route => {
      const { pathname } = new URL(route.request().url());
      if (pathname === "/api/version") return route.fulfill({ json: { version: "test", environment: "test" } });
      if (pathname === "/api/v1/alerts") return route.fulfill({ json: [
        { id: 1, status: "Open", isAcknowledged: false, message: "保留的清單警報", occurredAt: "2026-11-20T01:00:00Z" }
      ] });
      if (pathname === "/api/v1/alerts/999") {
        targetReads += 1;
        return route.fulfill({ status: 404, json: { message: "not found" } });
      }
      return route.fulfill({ json: [] });
    });

    await page.goto(`/alerts-workflow?alertId=${scenario.query}`);
    await expect(page.getByTestId("alert-row-1")).toBeVisible();
    await expect(page.getByTestId("alerts-all-filter")).toContainText("1");
    await expect(page.getByTestId("alert-linked-chart")).toHaveCount(0);
    expect(targetReads).toBe(scenario.expectedTargetReads);
  });
}
