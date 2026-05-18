# SPC 計算引擎架構設計 (SPC Engine Architecture)

本文件詳細闡述 SPC Engine 的系統架構與數學運算邏輯。SPC Engine 被設計為一個**無狀態 (Stateless)**、**純內存運算 (In-Memory)**、**零外部依賴 (Zero I/O Dependencies)** 的核心統計運算模組。

---

## 1. 引擎架構模組劃分 (Module Breakdown)

```
MesSpc.Api/SpcEngine/
  ├── Models/
  │    ├── SpcDataPoint.cs      (通用測量點資料結構)
  │    ├── Subgroup.cs          (計量型子組資料模型)
  │    ├── AttributeDataPoint.cs(計數型批次資料模型)
  │    ├── ControlLimits.cs     (界限規格封裝)
  │    ├── ControlChartResult.cs(管制圖輸出結果模型)
  │    └── CapabilityResult.cs  (製程能力指標輸出結果模型)
  ├── Calculators/
  │    ├── XbarRChartCalculator.cs
  │    ├── ImrChartCalculator.cs
  │    ├── AttributeChartCalculator.cs
  │    └── ProcessCapabilityCalculator.cs
  ├── Rules/
  │    ├── IRuleValidator.cs
  │    └── WesternElectricRulesValidator.cs
  └── SpcConstants.cs           (統計常數表 n=2~25: A2, D3, D4, d2, c4)
```

---

## 2. 核心運算器設計與公式推導 (Core Calculators & Formulas)

### 2.1 製程能力計算器 (`ProcessCapabilityCalculator`)
在給定一組來自相同製程條件的子組資料或單值資料後，需精確算出製程能力指標。

#### 短期標準差估計 ($\hat{\sigma}_{within}$)
- 基於平均全距 ($\bar{R}$ 法，適用 $n \le 10$)：
  $$\hat{\sigma}_{within} = \frac{\bar{R}}{d_2}$$
- 基於平均樣本標準差 ($\bar{s}$ 法，適用 $n > 10$)：
  $$\hat{\sigma}_{within} = \frac{\bar{s}}{c_4}$$

#### 總體標準差 ($\sigma_{overall}$)
將所有觀測值 $X_i$ ($i=1\sim N_{total}$) 視為單一母群：
$$\sigma_{overall} = \sqrt{\frac{\sum_{i=1}^{N_{total}} (X_i - \bar{\bar{X}})^2}{N_{total} - 1}}$$

#### 製程能力指標公式
- **短期潛在能力 ($C_p$)**：判斷製程變異寬度與規格寬度的比例。
  $$C_p = \frac{USL - LSL}{6\hat{\sigma}_{within}}$$
- **短期實際表現 ($C_{pk}$)**：考慮製程中心偏移的實際能力。
  $$C_{pk} = \min\left(\frac{USL - \bar{\bar{X}}}{3\hat{\sigma}_{within}}, \frac{\bar{\bar{X}} - LSL}{3\hat{\sigma}_{within}}\right)$$
- **長期整體表現 ($P_p, P_{pk}$)**：
  $$P_p = \frac{USL - LSL}{6\sigma_{overall}}, \quad P_{pk} = \min\left(\frac{USL - \bar{\bar{X}}}{3\sigma_{overall}}, \frac{\bar{\bar{X}} - LSL}{3\sigma_{overall}}\right)$$

### 2.2 Xbar-R 管制圖計算器 (`XbarRChartCalculator`)
給定 $M$ 個子組，每個子組大小為 $n$：
1. **各子組指標**：第 $j$ 組平均 $\bar{X}_j = \frac{1}{n}\sum_{i=1}^n X_{ji}$，全距 $R_j = \max(X_j) - \min(X_j)$。
2. **中心線 (Center Lines)**：總平均 $\bar{\bar{X}} = \frac{1}{M}\sum_{j=1}^M \bar{X}_j$，平均全距 $\bar{R} = \frac{1}{M}\sum_{j=1}^M R_j$。
3. **$\bar{X}$ 圖界限**：$UCL_{\bar{X}} = \bar{\bar{X}} + A_2 \bar{R}$，$LCL_{\bar{X}} = \bar{\bar{X}} - A_2 \bar{R}$。
4. **$R$ 圖界限**：$UCL_R = D_4 \bar{R}$，$LCL_R = D_3 \bar{R}$。

---

## 3. 異常判定規則庫設計 (Rule Engine Architecture)

為了提供高擴充性與可配置性，規則判定引擎設計如下：

```csharp
public interface IRuleValidator
{
    string RuleCode { get; }
    string RuleName { get; }
    bool Evaluate(List<SpcDataPoint> points, ControlLimits limits, int currentIndex);
}
```

透過將西方電氣規則解耦為獨立的類別實作，可在計算器中透過依賴注入或動態載入規則清單，並依據 `PartProcessCharacteristic` 中設定的開關進行動態執行。

---

## 4. 統計常數表查表機制 (`SpcConstants.cs`)
系統內嵌標準統計品質管制常數表，以 Dictionary 高速索引：

| 樣本大小 $n$ | $A_2$ | $D_3$ | $D_4$ | $d_2$ | $c_4$ |
| :---: | :---: | :---: | :---: | :---: | :---: |
| **2** | 1.880 | 0.000 | 3.267 | 1.128 | 0.7979 |
| **3** | 1.023 | 0.000 | 2.574 | 1.693 | 0.8862 |
| **4** | 0.729 | 0.000 | 2.282 | 2.059 | 0.9213 |
| **5** | 0.577 | 0.000 | 2.114 | 2.326 | 0.9400 |
| **10**| 0.308 | 0.223 | 1.777 | 3.078 | 0.9727 |

若傳入樣本大小 $n > 25$，引擎將拋出 `ArgumentOutOfRangeException` 或自動轉用近似公式計算。
