import assert from "node:assert/strict";
import test from "node:test";

import { loadPrimaryAndSupporting } from "../src/utils/progressiveLoad.js";

function deferred() {
  let resolve;
  let reject;
  const promise = new Promise((ok, fail) => {
    resolve = ok;
    reject = fail;
  });
  return { promise, resolve, reject };
}

test("primary data becomes visible while supporting data is still pending", async () => {
  const primary = deferred();
  const supporting = deferred();
  const events = [];

  const completion = loadPrimaryAndSupporting({
    loadPrimary: () => primary.promise,
    loadSupporting: () => supporting.promise,
    onPrimaryLoaded: value => events.push(["primary", value]),
    onSupportingLoaded: value => events.push(["supporting", value]),
    onPrimarySettled: () => events.push(["primary-settled"]),
    onSupportingSettled: () => events.push(["supporting-settled"])
  });

  primary.resolve(["row-1"]);
  await new Promise(resolve => setImmediate(resolve));

  assert.deepEqual(events, [["primary", ["row-1"]], ["primary-settled"]]);

  supporting.resolve({ processes: ["P1"] });
  await completion;
  assert.deepEqual(events, [
    ["primary", ["row-1"]],
    ["primary-settled"],
    ["supporting", { processes: ["P1"] }],
    ["supporting-settled"]
  ]);
});

test("supporting failure does not discard successful primary data", async () => {
  const events = [];

  await loadPrimaryAndSupporting({
    loadPrimary: async () => ["row-1"],
    loadSupporting: async () => { throw new Error("lookup failed"); },
    onPrimaryLoaded: value => events.push(["primary", value]),
    onSupportingError: error => events.push(["supporting-error", error.message])
  });

  assert.deepEqual(events, [
    ["primary", ["row-1"]],
    ["supporting-error", "lookup failed"]
  ]);
});
