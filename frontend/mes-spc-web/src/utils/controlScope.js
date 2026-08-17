export const CONTROL_SCOPE = Object.freeze({
  PRODUCT: "PRODUCT",
  PROCESS: "PROCESS",
  CHEM: "CHEM"
});

export function normalizeControlScope(scope, fallback = CONTROL_SCOPE.PRODUCT) {
  const value = String(scope || fallback).trim().toUpperCase();
  if (value === "PROD") return CONTROL_SCOPE.PRODUCT;
  if (value === "PROC") return CONTROL_SCOPE.PROCESS;
  if (value === "CHEMICAL") return CONTROL_SCOPE.CHEM;
  return value || fallback;
}

export const isChemicalScope = scope => normalizeControlScope(scope) === CONTROL_SCOPE.CHEM;
