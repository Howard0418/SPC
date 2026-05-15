<script setup>
import * as echarts from "echarts";
import { onBeforeUnmount, onMounted, ref } from "vue";
import { api, getApiErrorMessage } from "../api/client";

const chartEl = ref(null);
const itemId = ref(1);
const productId = ref(1);
const stationId = ref(1);
const chartType = ref("IMR");
const error = ref("");
let chartInstance = null;

function limitMarkLine(limits) {
  const data = [];
  const add = (y, name, color) => {
    if (y == null || Number.isNaN(y)) return;
    data.push({
      name,
      yAxis: y,
      lineStyle: { color, width: 1.5 },
      label: { formatter: name, color }
    });
  };
  add(limits?.usl, "USL", "#dc2626");
  add(limits?.lsl, "LSL", "#dc2626");
  add(limits?.ucl, "UCL (規格欄)", "#d97706");
  add(limits?.lcl, "LCL (規格欄)", "#d97706");
  add(limits?.target, "Target", "#16a34a");
  if (data.length === 0) return undefined;
  return { symbol: "none", data, animation: false };
}

/** 統計管制界限（A2/D3/D4），紫/紫色虛線風格 */
function controlMarkLines(ctrl, prefix) {
  if (!ctrl) return [];
  const data = [];
  const add = (y, name) => {
    if (y == null || Number.isNaN(y)) return;
    data.push({
      name,
      yAxis: y,
      lineStyle: { color: "#7c3aed", width: 1.5, type: "dashed" },
      label: { formatter: name, color: "#7c3aed" }
    });
  };
  add(ctrl.ucl, `${prefix} UCL*`);
  add(ctrl.lcl, `${prefix} LCL*`);
  add(ctrl.cl, `${prefix} CL*`);
  return data;
}

function pointData(values, pick, isBad) {
  return values.map((p) => {
    const v = pick(p);
    const bad = isBad ? isBad(p) : p.outOfSpec || p.outOfControl;
    return {
      value: v,
      itemStyle: { color: bad ? "#dc2626" : "#2563eb" }
    };
  });
}

const load = async () => {
  error.value = "";
  const ct = chartType.value === "XBAR_R" ? "XBAR_R" : "IMR";
  let data;
  try {
    ({ data } = await api.get(
      `/spc/chart?inspectionItemId=${itemId.value}&productId=${productId.value}&stationId=${stationId.value}&chartType=${ct}`
    ));
  } catch (e) {
    const s = e?.response?.status;
    if (s === 404) {
      error.value = "找不到檢測項目。";
      return;
    }
    error.value = getApiErrorMessage(e);
    return;
  }

  try {
    if (!chartEl.value) return;
    if (chartInstance) {
      chartInstance.dispose();
      chartInstance = null;
    }
    chartInstance = echarts.init(chartEl.value);

    const limits = data.limits ?? {};
    const mk = limitMarkLine(limits);

    if (ct === "IMR") {
      const iPts = data.iChart?.points ?? [];
      const mrPts = data.mrChart?.points ?? [];
      const iLabels = iPts.map((_, i) => String(i + 1));
      const mrLabels = mrPts.map((p) => String(p.index));
      const iSeriesData = pointData(iPts, (p) => p.value, null);
      const mrSeriesData = pointData(mrPts, (p) => p.value, null);

      const mkSpec = mk;
      const stat = data.statControl || {};
      const iStatCtrl = stat.iControlLimitsStat;
      const mrStatCtrl = stat.mrControlLimitsStat;
      const iStatLines = controlMarkLines(iStatCtrl, "I");
      const iMark = mkSpec?.data?.length
        ? {
            symbol: "none",
            data: [...(mkSpec.data ?? []), ...(iStatLines ?? [])],
            animation: false
          }
        : iStatLines?.length
          ? { symbol: "none", data: iStatLines, animation: false }
          : undefined;
      const mrMarkLines = controlMarkLines(mrStatCtrl, "MR");
      const mrMark = mrMarkLines.length ? { symbol: "none", data: mrMarkLines, animation: false } : undefined;

      chartInstance.setOption({
        tooltip: { trigger: "axis" },
        legend: { data: ["I chart", "MR"] },
        grid: [
          { left: "48", right: "24", top: "48", height: "38%" },
          { left: "48", right: "24", top: "58%", height: "28%" }
        ],
        xAxis: [
          { type: "category", boundaryGap: false, data: iLabels, gridIndex: 0, name: "序號 (I)" },
          { type: "category", boundaryGap: false, data: mrLabels, gridIndex: 1, name: "序號 (MR)" }
        ],
        yAxis: [
          { type: "value", gridIndex: 0, splitLine: { show: true }, name: "值" },
          { type: "value", gridIndex: 1, splitLine: { show: true }, name: "MR" }
        ],
        series: [
          {
            name: "I chart",
            type: "line",
            xAxisIndex: 0,
            yAxisIndex: 0,
            data: iSeriesData,
            showSymbol: true,
            symbolSize: 8,
            markLine: iMark
          },
          {
            name: "MR",
            type: "line",
            xAxisIndex: 1,
            yAxisIndex: 1,
            data: mrSeriesData,
            showSymbol: true,
            symbolSize: 6,
            itemStyle: { color: "#64748b" },
            markLine: mrMark
          }
        ]
      });
    } else {
      const xPts = data.xbarChart?.points ?? [];
      const rPts = data.rChart?.points ?? [];
      const labels = xPts.map((_, i) => String(i + 1));
      const xbarData = pointData(
        xPts,
        (p) => p.xbar,
        (p) => p.outOfSpec || p.outOfControlXbar
      );
      const rData = rPts.map((p) => ({
        value: p.value,
        itemStyle: { color: p.outOfControl ? "#dc2626" : "#64748b" }
      }));

      const mkSpec = limitMarkLine(limits);
      const xbarCtrl = data.xbarControlLimits;
      const rCtrl = data.rControlLimits;
      const xbarMarkData = [...(mkSpec?.data ?? []), ...controlMarkLines(xbarCtrl, "X̄")];
      const xbarMk =
        xbarMarkData.length > 0 ? { symbol: "none", data: xbarMarkData, animation: false } : mkSpec;
      const rMkData = controlMarkLines(rCtrl, "R");
      const rMk = rMkData.length
        ? { symbol: "none", data: rMkData, animation: false }
        : undefined;

      chartInstance.setOption({
        tooltip: { trigger: "axis" },
        legend: { data: ["Xbar", "R"] },
        grid: [
          { left: "48", right: "24", top: "48", height: "38%" },
          { left: "48", right: "24", top: "58%", height: "28%" }
        ],
        xAxis: [
          { type: "category", boundaryGap: false, data: labels, gridIndex: 0, name: "子組" },
          { type: "category", boundaryGap: false, data: labels, gridIndex: 1, name: "子組" }
        ],
        yAxis: [
          { type: "value", gridIndex: 0, splitLine: { show: true }, name: "Xbar" },
          { type: "value", gridIndex: 1, splitLine: { show: true }, name: "R" }
        ],
        series: [
          {
            name: "Xbar",
            type: "line",
            xAxisIndex: 0,
            yAxisIndex: 0,
            data: xbarData,
            showSymbol: true,
            symbolSize: 8,
            markLine: xbarMk
          },
          {
            name: "R",
            type: "line",
            xAxisIndex: 1,
            yAxisIndex: 1,
            data: rData,
            showSymbol: true,
            symbolSize: 6,
            markLine: rMk
          }
        ]
      });
    }

  } catch (e) {
    error.value = e?.message || "繪圖失敗";
  }
};

function resizeChart() {
  chartInstance?.resize();
}

onMounted(() => {
  load();
  window.addEventListener("resize", resizeChart);
});
onBeforeUnmount(() => {
  window.removeEventListener("resize", resizeChart);
  chartInstance?.dispose();
  chartInstance = null;
});
</script>

<template>
  <h2 class="text-2xl font-semibold mb-4">SPC 管制圖</h2>
  <div class="bg-white p-4 rounded shadow space-y-2">
    <div class="flex flex-wrap gap-2 items-end">
      <div>
        <label class="block text-xs text-gray-500 mb-1">檢測項目 ID</label>
        <input type="number" v-model.number="itemId" class="border p-2 rounded w-28" />
      </div>
      <div>
        <label class="block text-xs text-gray-500 mb-1">產品 ID</label>
        <input type="number" v-model.number="productId" class="border p-2 rounded w-28" />
      </div>
      <div>
        <label class="block text-xs text-gray-500 mb-1">工站 ID</label>
        <input type="number" v-model.number="stationId" class="border p-2 rounded w-28" />
      </div>
      <div>
        <label class="block text-xs text-gray-500 mb-1">圖別</label>
        <select v-model="chartType" class="border p-2 rounded">
          <option value="IMR">I-MR</option>
          <option value="XBAR_R">X̄-R</option>
        </select>
      </div>
      <button type="button" @click="load" class="bg-blue-600 text-white px-4 py-2 rounded h-10">查詢</button>
    </div>
    <p v-if="error" class="text-sm text-red-600">{{ error }}</p>
    <p v-else class="text-xs text-gray-500 space-y-1">
      <span
        >I／X̄ 圖：紅線 USL／LSL；橙線為主檔 UCL／LCL；綠線 Target；<strong class="text-violet-700">紫虛線</strong>為
        X̄／R 統計管制線（A2、D3、D4）。</span
      >
      <span class="block">異常著色：X̄ 點超出規格或 X̄ 統計界限；R 點超出 R 統計界限。</span>
    </p>
    <div ref="chartEl" class="h-[480px] w-full min-h-[360px]"></div>
  </div>
</template>
