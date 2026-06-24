# 05 SPC 運算與規則引擎設計 (SPC Engine Design)

## 核心設計原則
- **模組解耦**：`SpcEngine` 是一個高內聚的純 C# 統計運算庫，不依賴於 Entity Framework Core 或資料庫。
- **輸入模型**：所有資料必須先轉換為 `SpcDataPoint` 或 `Subgroup` 模型，再傳入計算器。
- **統計基礎**：使用 `MathNet.Numerics` 進行平均值、標準差與全距的精確計算。

## 管制圖計算器 (Calculators)
系統根據檢驗特性的設定與樣本數自動套用對應的計算器：
1. **Xbar-R (平均數-全距圖)**：
   - 用於子群樣本數 $n \le 10$ 的計量型資料。
   - 管制界限計算公式：
     - $\text{CL} = \bar{\bar{X}}$
     - $\text{UCL/LCL} = \bar{\bar{X}} \pm A_2 \bar{R}$
     - 區間標準差估算使用：$\hat{\sigma}_{within} = \bar{R} / d_2$
2. **Xbar-S (平均數-標準差圖)**：
   - 用於子群樣本數 $n > 10$ 的計量型資料。
   - 管制界限計算公式：
     - $\text{CL} = \bar{\bar{X}}$
     - $\text{UCL/LCL} = \bar{\bar{X}} \pm A_3 \bar{S}$
3. **I-MR (單值-移動全距圖)**：
   - 用於單值抽樣（子群數 = 1）的計量型資料。
   - 區間標準差估算使用：$\hat{\sigma}_{within} = \overline{MR} / d_2$
4. **計數型管制圖 (P, NP, C, U)**：
   - 分別針對固定/變動樣本數的不良數與缺點數進行管制界限計算。

## 異常判定引擎 (Western Electric Rules)
在 `SpcEngine` 產出計算點後，會將其傳入 `WesternElectricRulesValidator` 進行統計失控判定，支援以下黃金四大規則：
- **Rule 1**：任一點落在管制界限（$\pm 3\sigma$）之外。
- **Rule 2**：連續 3 點中有 2 點落在同側的 Zone A ($\pm 2\sigma \sim \pm 3\sigma$) 或之外。
- **Rule 3**：連續 5 點中有 4 點落在同側的 Zone B ($\pm 1\sigma \sim \pm 3\sigma$) 或之外。
- **Rule 4**：連續 8 點落在中心線的同一側（Zone C 或之外）。

## 製程能力指數 (Capability Indices)
系統在返回 SPC 資料時會自動計算：
- **$C_p$ / $C_{pk}$**：基於組內標準差（Within Sigma）計算短期製程能力。
- **$P_p$ / $P_{pk}$**：基於全體標準差（Overall Sigma）計算長期製程能力。
- **$\hat{\sigma}_{within}$ (組內標準差)**：依據圖表類型（如 $\bar{R}/d_2$）估算。
- **$\sigma_{overall}$ (整體標準差)**：全體樣本數的樣本標準差。

## 常態性檢定與常態曲線生成 (Normality Test)
為了驗證計量型數據是否符合常態分佈假設，系統整合了以下統計運算：
1. **Jarque-Bera 檢定**：
   - 計算樣本偏態 (Skewness) $S = m_3 / m_2^{1.5}$ 與峰態 (Kurtosis) $K = m_4 / m_2^2$，其中 $m_r$ 為樣本的 $r$ 階中心動差。
   - 計算 JB 統計量 $JB = \frac{n}{6} (S^2 + \frac{(K - 3)^2}{4})$。
   - 利用卡方分佈的生存函數計算 p-value：$p = e^{-JB / 2}$。
   - 顯著性判定：若 $p \ge 0.05$，則判定數據在統計上符合常態分佈。
2. **常態分佈 PDF 曲線點生成**：
   - 生成覆蓋 $[\mu - 3\sigma, \mu + 3\sigma]$ 區間內均勻分布的 100 個數據點。
   - 使用常態機率密度函數 (PDF) 公式計算各點的密度值，並乘以總點數與直方圖組寬 $ScaledPdf = PDF \times n \times binWidth$，使其高度與直方圖的「筆數」量值完全對齊，在前端圖表中完美重疊。
