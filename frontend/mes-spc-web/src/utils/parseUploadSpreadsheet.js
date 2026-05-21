import * as XLSX from "xlsx";

/**
 * 解析使用者選取之 Excel / CSV 檔案為列資料（供上傳對照器使用）。
 * @param {File} file
 * @returns {Promise<{ headers: string[], rows: Record<string, unknown>[] }>}
 */
export function parseUploadSpreadsheet(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = (evt) => {
      try {
        const data = new Uint8Array(evt.target.result);
        const workbook = XLSX.read(data, { type: "array" });
        if (workbook.SheetNames.length === 0) {
          throw new Error("Excel 檔案中找不到任何工作表。");
        }
        const ws = workbook.Sheets[workbook.SheetNames[0]];
        const jsonData = XLSX.utils.sheet_to_json(ws, { defval: "" });
        if (jsonData.length === 0) {
          throw new Error("工作表中沒有任何數據列。");
        }
        const headersSet = new Set();
        jsonData.forEach((row) => {
          Object.keys(row).forEach((k) => headersSet.add(k));
        });
        resolve({
          headers: Array.from(headersSet),
          rows: jsonData
        });
      } catch (ex) {
        reject(new Error("解析 Excel 失敗：" + (ex?.message || ex)));
      }
    };
    reader.onerror = () => reject(new Error("讀取檔案失敗。"));
    reader.readAsArrayBuffer(file);
  });
}
