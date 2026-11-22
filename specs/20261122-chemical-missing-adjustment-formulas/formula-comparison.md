# Excel 與 SPC 目前調整公式逐欄比對

- 來源《藥液分析日報表.xlsx》，SHA-256 `F634868A1AD1B432C7AA9E107BE0D76B1F9851AE38A244BE3754D17EAC348C7C`。
- 對象為 PMR_SPC_TEST 目前啟用且與來源唯一配對的 110 筆，共 220 欄。4 筆來源未配對、3 筆網站獨有仍見原盤點。
- Excel 本列儲存格參照使用來源值。SPC 具名參數以目前設定解析後比較。運算式不同表示需業務語意或實際分支驗算。

| 結果 | 調整 | 調整量 |
|---|---:|---:|
| 比對相符 | 84 | 58 |
| 運算結構相同，輸出文字不同 | 0 | 0 |
| 運算式不同，待核對 | 9 | 20 |
| 網站未設定 | 14 | 7 |
| Excel 無公式，保留網站 | 3 | 3 |
| 兩邊皆空白 | 0 | 22 |
| 來源工作表公式不同 | 0 | 0 |

## 逐欄結果

| PPC | 線別／槽別／分析項目 | 欄位 | Excel 來源 | 比對 |
|---:|---|---|---|---|
| 2579 | C1／鍍銅1／S RF-A (ml/L) | 調整 | C1!10 | 比對相符 |
| 2579 | C1／鍍銅1／S RF-A (ml/L) | 調整量 | C1!10 | 兩邊皆空白 |
| 2580 | C1／鍍銅1／S RF-B (ml/L) | 調整 | C1!11 | 比對相符 |
| 2580 | C1／鍍銅1／S RF-B (ml/L) | 調整量 | C1!11 | 兩邊皆空白 |
| 2553 | C1／鍍銅2／硫酸 (g/L) | 調整 | C1!12 | 比對相符 |
| 2553 | C1／鍍銅2／硫酸 (g/L) | 調整量 | C1!12 | 比對相符 |
| 2533 | C1／鍍銅2／硫酸銅 (g/L) | 調整 | C1!13 | 比對相符 |
| 2533 | C1／鍍銅2／硫酸銅 (g/L) | 調整量 | C1!13 | 比對相符 |
| 2534 | C1／鍍銅2／氯離子 (ppm) | 調整 | C1!14 | 比對相符 |
| 2534 | C1／鍍銅2／氯離子 (ppm) | 調整量 | C1!14 | 比對相符 |
| 2581 | C1／鍍銅2／S RF-A (ml/L) | 調整 | C1!15 | 比對相符 |
| 2581 | C1／鍍銅2／S RF-A (ml/L) | 調整量 | C1!15 | 兩邊皆空白 |
| 2582 | C1／鍍銅2／S RF-B (ml/L) | 調整 | C1!16 | 比對相符 |
| 2582 | C1／鍍銅2／S RF-B (ml/L) | 調整量 | C1!16 | 兩邊皆空白 |
| 2551 | C1／鍍銅1／硫酸 (g/L) | 調整 | C1!7 | 比對相符 |
| 2551 | C1／鍍銅1／硫酸 (g/L) | 調整量 | C1!7 | 比對相符 |
| 2530 | C1／鍍銅1／硫酸銅 (g/L) | 調整 | C1!8 | 比對相符 |
| 2530 | C1／鍍銅1／硫酸銅 (g/L) | 調整量 | C1!8 | 比對相符 |
| 2531 | C1／鍍銅1／氯離子 (ppm) | 調整 | C1!9 | 比對相符 |
| 2531 | C1／鍍銅1／氯離子 (ppm) | 調整量 | C1!9 | 比對相符 |
| 2596 | C3／鍍銅／硫酸銅 (g/L) | 調整 | C3!10 | 比對相符 |
| 2596 | C3／鍍銅／硫酸銅 (g/L) | 調整量 | C3!10 | 比對相符 |
| 2525 | C3／鍍銅／氯離子 (ppm) | 調整 | C3!11 | 比對相符 |
| 2525 | C3／鍍銅／氯離子 (ppm) | 調整量 | C3!11 | 比對相符 |
| 2597 | C3／鍍銅／SF (ml/L) | 調整 | C3!12 | 比對相符 |
| 2597 | C3／鍍銅／SF (ml/L) | 調整量 | C3!12 | 比對相符 |
| 2598 | C3／鍍銅／STB (ml/L) | 調整 | C3!13 | 比對相符 |
| 2598 | C3／鍍銅／STB (ml/L) | 調整量 | C3!13 | 比對相符 |
| 2546 | C3／剝掛／硫酸 (g/L) | 調整 | C3!14 | 比對相符 |
| 2546 | C3／剝掛／硫酸 (g/L) | 調整量 | C3!14 | 運算式不同，待核對 |
| 2528 | C3／剝掛／銅離子 (g/L) | 調整 | C3!15 | 比對相符 |
| 2528 | C3／剝掛／銅離子 (g/L) | 調整量 | C3!15 | 兩邊皆空白 |
| 2547 | C3／剝掛／雙氧水 (g/L) | 調整 | C3!16 | 比對相符 |
| 2547 | C3／剝掛／雙氧水 (g/L) | 調整量 | C3!16 | 比對相符 |
| 2539 | C3／清潔／硫酸(ml/L) | 調整 | C3!7 | 比對相符 |
| 2539 | C3／清潔／硫酸(ml/L) | 調整量 | C3!7 | 比對相符 |
| 2544 | C3／酸浸／硫酸 (%) | 調整 | C3!8 | 比對相符 |
| 2544 | C3／酸浸／硫酸 (%) | 調整量 | C3!8 | 運算式不同，待核對 |
| 2545 | C3／鍍銅／硫酸 (g/L) | 調整 | C3!9 | 比對相符 |
| 2545 | C3／鍍銅／硫酸 (g/L) | 調整量 | C3!9 | 比對相符 |
| 2428 | C5／鍍銅／硫酸銅 (g/L) | 調整 | C5!10 | 比對相符 |
| 2428 | C5／鍍銅／硫酸銅 (g/L) | 調整量 | C5!10 | 比對相符 |
| 2429 | C5／鍍銅／氯離子 (ppm) | 調整 | C5!11 | 比對相符 |
| 2429 | C5／鍍銅／氯離子 (ppm) | 調整量 | C5!11 | 比對相符 |
| 2599 | C5／鍍銅／GAP-1 (ml/L) | 調整 | C5!12 | 比對相符 |
| 2599 | C5／鍍銅／GAP-1 (ml/L) | 調整量 | C5!12 | 運算式不同，待核對 |
| 2600 | C5／鍍銅／GAP-2 (ml/L) | 調整 | C5!13 | 比對相符 |
| 2600 | C5／鍍銅／GAP-2 (ml/L) | 調整量 | C5!13 | 運算式不同，待核對 |
| 2601 | C5／鍍銅／GAP-3 (ml/L) | 調整 | C5!14 | 比對相符 |
| 2601 | C5／鍍銅／GAP-3 (ml/L) | 調整量 | C5!14 | 運算式不同，待核對 |
| 2430 | C5／剝掛／硫酸 (g/L) | 調整 | C5!15 | 比對相符 |
| 2430 | C5／剝掛／硫酸 (g/L) | 調整量 | C5!15 | 比對相符 |
| 2431 | C5／剝掛／銅離子 (g/L) | 調整 | C5!16 | 比對相符 |
| 2431 | C5／剝掛／銅離子 (g/L) | 調整量 | C5!16 | 兩邊皆空白 |
| 2432 | C5／剝掛／雙氧水 (g/L) | 調整 | C5!17 | 比對相符 |
| 2432 | C5／剝掛／雙氧水 (g/L) | 調整量 | C5!17 | 比對相符 |
| 2425 | C5／清潔／硫酸(ml/L) | 調整 | C5!7 | 運算式不同，待核對 |
| 2425 | C5／清潔／硫酸(ml/L) | 調整量 | C5!7 | 運算式不同，待核對 |
| 2426 | C5／酸浸／硫酸 (%) | 調整 | C5!8 | 比對相符 |
| 2426 | C5／酸浸／硫酸 (%) | 調整量 | C5!8 | 比對相符 |
| 2427 | C5／鍍銅／硫酸 (g/L) | 調整 | C5!9 | 比對相符 |
| 2427 | C5／鍍銅／硫酸 (g/L) | 調整量 | C5!9 | 比對相符 |
| 2558 | CN／酸洗2／過硫酸鈉 (g/L) | 調整 | CN!10 | 比對相符 |
| 2558 | CN／酸洗2／過硫酸鈉 (g/L) | 調整量 | CN!10 | 比對相符 |
| 2555 | CN／酸洗1／硫酸 (g/L) | 調整 | CN!7 | 比對相符 |
| 2555 | CN／酸洗1／硫酸 (g/L) | 調整量 | CN!7 | 比對相符 |
| 2556 | CN／酸洗1／過硫酸鈉 (g/L) | 調整 | CN!8 | 比對相符 |
| 2556 | CN／酸洗1／過硫酸鈉 (g/L) | 調整量 | CN!8 | 比對相符 |
| 2557 | CN／酸洗2／硫酸 (g/L) | 調整 | CN!9 | 比對相符 |
| 2557 | CN／酸洗2／硫酸 (g/L) | 調整量 | CN!9 | 比對相符 |
| 2436 | DP／除鈀／PH | 調整 | DP!10 | 比對相符 |
| 2436 | DP／除鈀／PH | 調整量 | DP!10 | 運算式不同，待核對 |
| 2437 | DP／清潔／PARRARINSE (%) | 調整 | DP!11 | 比對相符 |
| 2437 | DP／清潔／PARRARINSE (%) | 調整量 | DP!11 | 比對相符 |
| 2438 | DP／酸洗／硫酸 (%) | 調整 | DP!12 | 比對相符 |
| 2438 | DP／酸洗／硫酸 (%) | 調整量 | DP!12 | 運算式不同，待核對 |
| 2439 | DP／抗氧化／PH | 調整 | DP!13 | 比對相符 |
| 2439 | DP／抗氧化／PH | 調整量 | DP!13 | 兩邊皆空白 |
| 2433 | DP／除鈀／PARRADELETE (%) | 調整 | DP!7 | 比對相符 |
| 2433 | DP／除鈀／PARRADELETE (%) | 調整量 | DP!7 | 比對相符 |
| 2434 | DP／除鈀／過硫酸鈉 (g/L) | 調整 | DP!8 | 比對相符 |
| 2434 | DP／除鈀／過硫酸鈉 (g/L) | 調整量 | DP!8 | 比對相符 |
| 2446 | DV1／顯影／碳酸鈉 (%) | 調整 | 舊線!13 | 比對相符 |
| 2446 | DV1／顯影／碳酸鈉 (%) | 調整量 | 舊線!13 | 比對相符 |
| 2447 | DV1／顯影／PH | 調整 | 舊線!14 | 網站未設定 |
| 2447 | DV1／顯影／PH | 調整量 | 舊線!14 | 兩邊皆空白 |
| 2448 | DV1／添加桶／碳酸鈉 (%) | 調整 | 舊線!15 | 比對相符 |
| 2448 | DV1／添加桶／碳酸鈉 (%) | 調整量 | 舊線!15 | 比對相符 |
| 2449 | DV1／添加桶／PH | 調整 | 舊線!16 | 網站未設定 |
| 2449 | DV1／添加桶／PH | 調整量 | 舊線!16 | 網站未設定 |
| 2465 | DV2／顯影1／碳酸鈉 (%) | 調整 | 新線!13 | 比對相符 |
| 2465 | DV2／顯影1／碳酸鈉 (%) | 調整量 | 新線!13 | 比對相符 |
| 2466 | DV2／顯影1／PH | 調整 | 新線!14 | 比對相符 |
| 2466 | DV2／顯影1／PH | 調整量 | 新線!14 | 兩邊皆空白 |
| 2467 | DV2／顯影2／碳酸鈉 (%) | 調整 | 新線!15 | 比對相符 |
| 2467 | DV2／顯影2／碳酸鈉 (%) | 調整量 | 新線!15 | 比對相符 |
| 2468 | DV2／顯影2／PH | 調整 | 新線!16 | 比對相符 |
| 2468 | DV2／顯影2／PH | 調整量 | 新線!16 | 兩邊皆空白 |
| 2469 | DV2／添加桶／碳酸鈉 (%) | 調整 | 新線!17 | 比對相符 |
| 2469 | DV2／添加桶／碳酸鈉 (%) | 調整量 | 新線!17 | 比對相符 |
| 2470 | DV2／添加桶／PH | 調整 | 新線!18 | 比對相符 |
| 2470 | DV2／添加桶／PH | 調整量 | 新線!18 | 比對相符 |
| 2564 | N1／調節／P200 (ml/L) | 調整 | N1 (開)!10、N1 (收)!10 | 網站未設定 |
| 2564 | N1／調節／P200 (ml/L) | 調整量 | N1 (開)!10、N1 (收)!10 | 網站未設定 |
| 2565 | N1／調節／PH | 調整 | N1 (開)!11、N1 (收)!11 | 網站未設定 |
| 2565 | N1／調節／PH | 調整量 | N1 (開)!11、N1 (收)!11 | 兩邊皆空白 |
| 2592 | N1／預浸／P300 (g/L) | 調整 | N1 (開)!12、N1 (收)!12 | 比對相符 |
| 2592 | N1／預浸／P300 (g/L) | 調整量 | N1 (開)!12、N1 (收)!12 | 比對相符 |
| 2619 | N1／催化／P400 (ml/L) | 調整 | N1 (開)!13、N1 (收)!13 | 運算式不同，待核對 |
| 2619 | N1／催化／P400 (ml/L) | 調整量 | N1 (開)!13、N1 (收)!13 | 運算式不同，待核對 |
| 2568 | N1／催化／氯化亞錫 (g/L) | 調整 | N1 (開)!14、N1 (收)!14 | 網站未設定 |
| 2568 | N1／催化／氯化亞錫 (g/L) | 調整量 | N1 (開)!14、N1 (收)!14 | 網站未設定 |
| 2570 | N1／促進／P500A (ml/L) | 調整 | N1 (開)!15、N1 (收)!15 | 網站未設定 |
| 2570 | N1／促進／P500A (ml/L) | 調整量 | N1 (開)!15、N1 (收)!15 | 網站未設定 |
| 2571 | N1／促進／氯化亞錫 (g/L) | 調整 | N1 (開)!16、N1 (收)!16 | 網站未設定 |
| 2571 | N1／促進／氯化亞錫 (g/L) | 調整量 | N1 (開)!16、N1 (收)!16 | 兩邊皆空白 |
| 2588 | N1／化鎳／鎳  (g/L) | 調整 | N1 (開)!17、N1 (收)!17 | 網站未設定 |
| 2588 | N1／化鎳／鎳  (g/L) | 調整量 | N1 (開)!17、N1 (收)!17 | 網站未設定 |
| 2589 | N1／化鎳／亞磷酸鈉 (g/L) | 調整 | N1 (開)!18、N1 (收)!18 | 運算式不同，待核對 |
| 2589 | N1／化鎳／亞磷酸鈉 (g/L) | 調整量 | N1 (開)!18、N1 (收)!18 | 運算式不同，待核對 |
| 2590 | N1／化鎳／次磷酸鈉 (g/L) | 調整 | N1 (開)!19、N1 (收)!19 | 運算式不同，待核對 |
| 2590 | N1／化鎳／次磷酸鈉 (g/L) | 調整量 | N1 (開)!19、N1 (收)!19 | 兩邊皆空白 |
| 2591 | N1／化鎳／PH | 調整 | N1 (開)!20、N1 (收)!20 | 網站未設定 |
| 2591 | N1／化鎳／PH | 調整量 | N1 (開)!20、N1 (收)!20 | 網站未設定 |
| 2587 | N1／表處／氫氧化鉀 (g/L) | 調整 | N1 (開)!7、N1 (收)!7 | 網站未設定 |
| 2587 | N1／表處／氫氧化鉀 (g/L) | 調整量 | N1 (開)!7、N1 (收)!7 | 網站未設定 |
| 2512 | N2／調節／P200 (ml/L) | 調整 | N2 (開)!10、N2 (收)!10 | 比對相符 |
| 2512 | N2／調節／P200 (ml/L) | 調整量 | N2 (開)!10、N2 (收)!10 | 比對相符 |
| 2513 | N2／調節／PH | 調整 | N2 (開)!11、N2 (收)!11 | 運算式不同，待核對 |
| 2513 | N2／調節／PH | 調整量 | N2 (開)!11、N2 (收)!11 | 兩邊皆空白 |
| 2514 | N2／預浸／P300 (g/L) | 調整 | N2 (開)!12、N2 (收)!12 | 比對相符 |
| 2514 | N2／預浸／P300 (g/L) | 調整量 | N2 (開)!12、N2 (收)!12 | 比對相符 |
| 2515 | N2／催化／P400 (ml/L) | 調整 | N2 (開)!13、N2 (收)!13 | 運算式不同，待核對 |
| 2515 | N2／催化／P400 (ml/L) | 調整量 | N2 (開)!13、N2 (收)!13 | 運算式不同，待核對 |
| 2516 | N2／催化／氯化亞錫 (g/L) | 調整 | N2 (開)!14、N2 (收)!14 | 運算式不同，待核對 |
| 2516 | N2／催化／氯化亞錫 (g/L) | 調整量 | N2 (開)!14、N2 (收)!14 | 運算式不同，待核對 |
| 2517 | N2／促進／P500A (ml/L) | 調整 | N2 (開)!15、N2 (收)!15 | 比對相符 |
| 2517 | N2／促進／P500A (ml/L) | 調整量 | N2 (開)!15、N2 (收)!15 | 比對相符 |
| 2518 | N2／促進／氯化亞錫 (g/L) | 調整 | N2 (開)!16、N2 (收)!16 | 網站未設定 |
| 2518 | N2／促進／氯化亞錫 (g/L) | 調整量 | N2 (開)!16、N2 (收)!16 | 兩邊皆空白 |
| 2519 | N2／化鎳／鎳  (g/L) | 調整 | N2 (開)!17、N2 (收)!17 | 比對相符 |
| 2519 | N2／化鎳／鎳  (g/L) | 調整量 | N2 (開)!17、N2 (收)!17 | 比對相符 |
| 2594 | N2／化鎳／亞磷酸鈉 (g/L) | 調整 | N2 (開)!18、N2 (收)!18 | 運算式不同，待核對 |
| 2594 | N2／化鎳／亞磷酸鈉 (g/L) | 調整量 | N2 (開)!18、N2 (收)!18 | 運算式不同，待核對 |
| 2595 | N2／化鎳／次磷酸鈉 (g/L) | 調整 | N2 (開)!19、N2 (收)!19 | 運算式不同，待核對 |
| 2595 | N2／化鎳／次磷酸鈉 (g/L) | 調整量 | N2 (開)!19、N2 (收)!19 | 兩邊皆空白 |
| 2520 | N2／化鎳／PH | 調整 | N2 (開)!20、N2 (收)!20 | 比對相符 |
| 2520 | N2／化鎳／PH | 調整量 | N2 (開)!20、N2 (收)!20 | 比對相符 |
| 2550 | N2／表處／氫氧化鉀 (g/L) | 調整 | N2 (開)!7、N2 (收)!7 | 比對相符 |
| 2550 | N2／表處／氫氧化鉀 (g/L) | 調整量 | N2 (開)!7、N2 (收)!7 | 比對相符 |
| 2443 | PT1／酸洗／鹽酸 (%) | 調整 | 舊線!10 | 比對相符 |
| 2443 | PT1／酸洗／鹽酸 (%) | 調整量 | 舊線!10 | 運算式不同，待核對 |
| 2444 | PT1／酸洗／銅離子 (mg/L) | 調整 | 舊線!11 | 網站未設定 |
| 2444 | PT1／酸洗／銅離子 (mg/L) | 調整量 | 舊線!11 | 兩邊皆空白 |
| 2445 | PT1／抗氧化／PH | 調整 | 舊線!12 | 網站未設定 |
| 2445 | PT1／抗氧化／PH | 調整量 | 舊線!12 | 兩邊皆空白 |
| 2440 | PT1／脫脂／硫酸 (%) | 調整 | 舊線!7 | 比對相符 |
| 2440 | PT1／脫脂／硫酸 (%) | 調整量 | 舊線!7 | 運算式不同，待核對 |
| 2441 | PT1／粗化／銅離子 (g/L) | 調整 | 舊線!8 | 比對相符 |
| 2441 | PT1／粗化／銅離子 (g/L) | 調整量 | 舊線!8 | 比對相符 |
| 2442 | PT1／粗化／比重 | 調整 | 舊線!9 | 比對相符 |
| 2442 | PT1／粗化／比重 | 調整量 | 舊線!9 | 比對相符 |
| 2462 | PT2／鹽酸洗／鹽酸 (%) | 調整 | 新線!10 | 比對相符 |
| 2462 | PT2／鹽酸洗／鹽酸 (%) | 調整量 | 新線!10 | 運算式不同，待核對 |
| 2463 | PT2／鹽酸洗／銅離子 (mg/L) | 調整 | 新線!11 | 比對相符 |
| 2463 | PT2／鹽酸洗／銅離子 (mg/L) | 調整量 | 新線!11 | 兩邊皆空白 |
| 2464 | PT2／抗氧化／PH | 調整 | 新線!12 | 比對相符 |
| 2464 | PT2／抗氧化／PH | 調整量 | 新線!12 | 兩邊皆空白 |
| 2459 | PT2／脫脂／硫酸 (%) | 調整 | 新線!7 | 比對相符 |
| 2459 | PT2／脫脂／硫酸 (%) | 調整量 | 新線!7 | 運算式不同，待核對 |
| 2460 | PT2／粗化／銅離子 (g/L) | 調整 | 新線!8 | 比對相符 |
| 2460 | PT2／粗化／銅離子 (g/L) | 調整量 | 新線!8 | 比對相符 |
| 2461 | PT2／粗化／比重 | 調整 | 新線!9 | 比對相符 |
| 2461 | PT2／粗化／比重 | 調整量 | 新線!9 | 比對相符 |
| 2454 | QE1／蝕刻／硫酸 (%) | 調整 | 舊線!22 | 比對相符 |
| 2454 | QE1／蝕刻／硫酸 (%) | 調整量 | 舊線!22 | 運算式不同，待核對 |
| 2455 | QE1／蝕刻／銅離子 (g/L) | 調整 | 舊線!23 | 比對相符 |
| 2455 | QE1／蝕刻／銅離子 (g/L) | 調整量 | 舊線!23 | 比對相符 |
| 2456 | QE1／蝕刻／雙氧水 (%) | 調整 | 舊線!24 | 比對相符 |
| 2456 | QE1／蝕刻／雙氧水 (%) | 調整量 | 舊線!24 | 比對相符 |
| 2457 | QE1／酸洗／硫酸 (%) | 調整 | 舊線!25 | 比對相符 |
| 2457 | QE1／酸洗／硫酸 (%) | 調整量 | 舊線!25 | 運算式不同，待核對 |
| 2458 | QE1／抗氧化／PH | 調整 | 舊線!26 | 網站未設定 |
| 2458 | QE1／抗氧化／PH | 調整量 | 舊線!26 | 兩邊皆空白 |
| 2474 | QE2／蝕刻(銅)／硫酸 (%) | 調整 | 新線!25 | Excel 無公式，保留網站 |
| 2474 | QE2／蝕刻(銅)／硫酸 (%) | 調整量 | 新線!25 | Excel 無公式，保留網站 |
| 2475 | QE2／蝕刻(銅)／銅離子 (g/L) | 調整 | 新線!26 | Excel 無公式，保留網站 |
| 2475 | QE2／蝕刻(銅)／銅離子 (g/L) | 調整量 | 新線!26 | Excel 無公式，保留網站 |
| 2476 | QE2／蝕刻(銅)／雙氧水 (%) | 調整 | 新線!27 | Excel 無公式，保留網站 |
| 2476 | QE2／蝕刻(銅)／雙氧水 (%) | 調整量 | 新線!27 | Excel 無公式，保留網站 |
| 2477 | QE2／蝕刻(鎳)／硝酸 (%) | 調整 | 新線!30 | 比對相符 |
| 2477 | QE2／蝕刻(鎳)／硝酸 (%) | 調整量 | 新線!30 | 運算式不同，待核對 |
| 2478 | QE2／蝕刻(鎳)／銅離子 (g/L) | 調整 | 新線!31 | 比對相符 |
| 2478 | QE2／蝕刻(鎳)／銅離子 (g/L) | 調整量 | 新線!31 | 兩邊皆空白 |
| 2479 | QE2／蝕刻(鎳)／雙氧水 (%) | 調整 | 新線!32 | 比對相符 |
| 2479 | QE2／蝕刻(鎳)／雙氧水 (%) | 調整量 | 新線!32 | 比對相符 |
| 2480 | QE2／酸洗／硫酸 (%) | 調整 | 新線!33 | 比對相符 |
| 2480 | QE2／酸洗／硫酸 (%) | 調整量 | 新線!33 | 比對相符 |
| 2450 | ST1／剝膜1／氫氧化鉀 (%) | 調整 | 舊線!17 | 比對相符 |
| 2450 | ST1／剝膜1／氫氧化鉀 (%) | 調整量 | 舊線!17 | 比對相符 |
| 2451 | ST1／剝膜2／氫氧化鉀 (%) | 調整 | 舊線!18 | 比對相符 |
| 2451 | ST1／剝膜2／氫氧化鉀 (%) | 調整量 | 舊線!18 | 比對相符 |
| 2481 | ST1／酸洗／硫酸 (%) | 調整 | 舊線!19 | 比對相符 |
| 2481 | ST1／酸洗／硫酸 (%) | 調整量 | 舊線!19 | 比對相符 |
| 2452 | ST1／添加桶／氫氧化鉀 (%) | 調整 | 舊線!20 | 比對相符 |
| 2452 | ST1／添加桶／氫氧化鉀 (%) | 調整量 | 舊線!20 | 比對相符 |
| 2453 | ST1／小槽／氫氧化鉀 (%) | 調整 | 舊線!21 | 比對相符 |
| 2453 | ST1／小槽／氫氧化鉀 (%) | 調整量 | 舊線!21 | 比對相符 |
| 2602 | ST2／剝膜1／氫氧化鉀 (%) | 調整 | 新線!19 | 比對相符 |
| 2602 | ST2／剝膜1／氫氧化鉀 (%) | 調整量 | 新線!19 | 比對相符 |
| 2471 | ST2／剝膜2／氫氧化鉀 (%) | 調整 | 新線!20 | 比對相符 |
| 2471 | ST2／剝膜2／氫氧化鉀 (%) | 調整量 | 新線!20 | 比對相符 |
| 2472 | ST2／剝膜3／氫氧化鉀 (%) | 調整 | 新線!21 | 比對相符 |
| 2472 | ST2／剝膜3／氫氧化鉀 (%) | 調整量 | 新線!21 | 比對相符 |
| 2473 | ST2／酸洗／硫酸 (%) | 調整 | 新線!22 | 比對相符 |
| 2473 | ST2／酸洗／硫酸 (%) | 調整量 | 新線!22 | 比對相符 |
| 2603 | ST2／添加桶1／氫氧化鉀 (%) | 調整 | 新線!23 | 比對相符 |
| 2603 | ST2／添加桶1／氫氧化鉀 (%) | 調整量 | 新線!23 | 比對相符 |
| 2604 | ST2／添加桶2／氫氧化鉀 (%) | 調整 | 新線!24 | 比對相符 |
| 2604 | ST2／添加桶2／氫氧化鉀 (%) | 調整量 | 新線!24 | 比對相符 |

## 待複核公式明細

### PPC 2546 C3／剝掛／硫酸 (g/L) 調整量

```text
Excel 1  IFERROR(IF(Concentration>200,"排液:"&ROUND(813*(1-150/Concentration),0)&" L"&"\n補水:"&ROUND(813*(1-150/Concentration),0)&" L",IF(Concentration<100,"硫酸:"&ROUND((150-Concentration)*813/1400/0.5/1.4,1)&" L","")),"")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE,"排液："&ROUND(TANK_CAPACITY_LITERS*(1-150/Concentration),0)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-150/Concentration),0)&" L",IF(Concentration<ADDITION_CONTROL_VALUE,"硫酸："&ROUND((150-Concentration)*TANK_CAPACITY_LITERS/1400/0.5,1)&" L","")),"")
```

### PPC 2544 C3／酸浸／硫酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>6,"排液:"&ROUND(664*(1-5/Concentration),0)&" L"&"\n補水:"&ROUND(664*(1-5/Concentration),0)&" L",IF(Concentration<4,"硫酸:"&ROUND((5-Concentration)*664/100/0.5/1.4,1)&" L","")),"")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE,"排液："&ROUND(TANK_CAPACITY_LITERS*(1-5/Concentration),0)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-5/Concentration),0)&" L",IF(Concentration<ADDITION_CONTROL_VALUE,"硫酸："&ROUND((5-Concentration)*TANK_CAPACITY_LITERS/100/0.5,1)&" L","")),"")
```

### PPC 2599 C5／鍍銅／GAP-1 (ml/L) 調整量

```text
Excel 1  IF(K12="","",IF(Concentration>6,"排液:"&ROUND(14700*(1-4/Concentration),0)&" L"&"\n補水:"&ROUND(14700*(1-4/Concentration),0)&" L",IF(Concentration<2,"GAP-1:"&ROUND((4-Concentration)*14700/1000,1)&" L","")))
SPC      IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"排液："&ROUND(TANK_CAPACITY_LITERS*(1-4/Concentration),0)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-4/Concentration),0)&" L",IF(Concentration<ADDITION_CONTROL_VALUE,"GAP-1："&ROUND((4-Concentration)*TANK_CAPACITY_LITERS/1000,1)&" L","")))
```

### PPC 2600 C5／鍍銅／GAP-2 (ml/L) 調整量

```text
Excel 1  IF(K13="","",IF(Concentration>4,"排液:"&ROUND(14700*(1-3/Concentration),0)&" L"&"\n補水:"&ROUND(14700*(1-3/Concentration),0)&" L",IF(Concentration<2,"GAP-2:"&ROUND((3-Concentration)*14700/1000,1)&" L","")))
SPC      IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"排液："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration),0)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration),0)&" L",IF(Concentration<ADDITION_CONTROL_VALUE,"GAP-2："&ROUND((3-Concentration)*TANK_CAPACITY_LITERS/1000,1)&" L","")))
```

### PPC 2601 C5／鍍銅／GAP-3 (ml/L) 調整量

```text
Excel 1  IF(K14="","",IF(Concentration>14,"排液:"&ROUND(14700*(1-12/Concentration),0)&" L"&"\n補水:"&ROUND(14700*(1-12/Concentration),0)&" L",IF(Concentration<9,"GAP-3:"&ROUND((12-Concentration)*14700/1000,1)&" L","")))
SPC      IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"排液："&ROUND(TANK_CAPACITY_LITERS*(1-12/Concentration),0)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-12/Concentration),0)&" L",IF(Concentration<ADDITION_CONTROL_VALUE,"GAP-3："&ROUND((12-Concentration)*TANK_CAPACITY_LITERS/1000,1)&" L","")))
```

### PPC 2425 C5／清潔／硫酸(ml/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration<48,"添加",IF(Concentration>72,"稀釋","")))
SPC      IF(Concentration<ADDITION_CONTROL_VALUE,"添加",IF(Concentration>DILUTION_CONTROL_VALUE,"稀釋",""))
```

### PPC 2425 C5／清潔／硫酸(ml/L) 調整量

```text
Excel 1  IFERROR(IF(Concentration>72,"排液:"&ROUND(1100*(1-100/Concentration),0)&" L"&"\n補水:"&ROUND(1100*(1-100/Concentration),0)&" L",IF(Concentration<48,"OPC:"&ROUND((100-Concentration)*1100/1000,1)&" L","")),"")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE,"排液："&ROUND(TANK_CAPACITY_LITERS*(1-Target/Concentration),0)&" L"&" 補水："&ROUND(TANK_CAPACITY_LITERS*(1-Target/Concentration),0)&" L",IF(Concentration<ADDITION_CONTROL_VALUE,"DP333："&ROUND((Target-Concentration)*TANK_CAPACITY_LITERS/1000,1)&" L","")),"")
```

### PPC 2436 DP／除鈀／PH 調整量

```text
Excel 1  IF(Concentration="","",IF(Concentration>10.15,"通知工程師",IF(Concentration<9.9,"NaOH:"&ROUND((10-Concentration)*300*10,1)&" ml","")))
SPC      IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"通知工程師",IF(Concentration<ADDITION_CONTROL_VALUE,"NaOH："&ROUND((10-Concentration)*TANK_CAPACITY_LITERS*10,1)&" ml","")))
```

### PPC 2438 DP／酸洗／硫酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>5.9,"排液:"&ROUND(60*(1-5.9/Concentration),0)&" L"&"\n補水:"&ROUND(60*(1-5.9/Concentration)/0.5/1.4,0)&" L",IF(Concentration<4.5,"硫酸(50%):"&ROUND(60*(5-Concentration)/100/0.5/1.4,1)&"L","")),"")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE,"排液："&ROUND(TANK_CAPACITY_LITERS*(1-5.9/Concentration),0)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-5.9/Concentration),0)&" L",IF(Concentration<ADDITION_CONTROL_VALUE,"硫酸(50%)："&ROUND((5-Concentration)/0.5,0)&"L","")),"")
```

### PPC 2619 N1／催化／P400 (ml/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration<31,"添加",IF(Concentration>NA,"稀釋","")))
SPC      IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",""))
```

### PPC 2619 N1／催化／P400 (ml/L) 調整量

```text
Excel 1  IFERROR(IF(Concentration<31,"P400:"&ROUND(((31-Concentration)*380/1000),1)&" L",IF(Concentration>NA,"排藥:"&ROUND((Concentration-NA)/Concentration*380,1)&" L"&"\n補水:"&ROUND((Concentration-NA)/Concentration*380,1)&" L","")),"")
SPC      IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"P400："&ROUND(((31-Concentration)*TANK_CAPACITY_LITERS/1000),1)&" L",""),"")
```

### PPC 2589 N1／化鎳／亞磷酸鈉 (g/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration<18.5,"添加",IF(Concentration>NA,"稀釋","")))
SPC      IF(Concentration="","",IF(Concentration<18.5,"添加",IF(Concentration>20,"稀釋","")))
```

### PPC 2589 N1／化鎳／亞磷酸鈉 (g/L) 調整量

```text
Excel 1  IF(Concentration<18.5,"A劑:"&ROUND(((18.5-Concentration)*900*1.62/1000),1)&" L",IF(Concentration>NA,"排藥:"&ROUND((Concentration-NA)/Concentration*900,1)&" L"&"\n補水:"&ROUND((Concentration-NA)/Concentration*900,1)&" L",""))
SPC      IF(Concentration<18.5,"A劑："&ROUND(((18.5-Concentration)*900*1.62/1000),1)&" L",IF(Concentration>20,"排藥："&ROUND((Concentration-20)/Concentration*900,1)&" L"&" 補水："&ROUND((Concentration-20)/Concentration*900,1)&" L",""))
```

### PPC 2590 N1／化鎳／次磷酸鈉 (g/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration>NA,"更槽",""))
SPC      IF(Concentration="","",IF(Concentration>100,"更槽",""))
```

### PPC 2513 N2／調節／PH 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration>NA,"更槽",""))
SPC      IF(Concentration="","",IF(Concentration>LSL,"更槽",""))
```

### PPC 2515 N2／催化／P400 (ml/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration<31,"添加",IF(Concentration>NA,"稀釋","")))
SPC      IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",""))
```

### PPC 2515 N2／催化／P400 (ml/L) 調整量

```text
Excel 1  IFERROR(IF(Concentration<31,"P400:"&ROUND(((31-Concentration)*630/1000),1)&" L",IF(Concentration>NA,"排藥:"&ROUND((Concentration-NA)/Concentration*630,1)&" L"&"\n補水:"&ROUND((Concentration-NA)/Concentration*630,1)&" L","")),"")
SPC      IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"P400："&ROUND(((31-Concentration)*TANK_CAPACITY_LITERS/1000),1)&" L",""),"")
```

### PPC 2516 N2／催化／氯化亞錫 (g/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration<5,"添加",IF(Concentration>NA,"稀釋","")))
SPC      IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",""))
```

### PPC 2516 N2／催化／氯化亞錫 (g/L) 調整量

```text
Excel 1  IFERROR(IF(Concentration<5,"安定劑:"&ROUND(((5-Concentration)*630/1000),1)&" L",IF(Concentration>NA,"排藥:"&ROUND((Concentration-NA)/Concentration*630,1)&" L"&"\n補水:"&ROUND((Concentration-NA)/Concentration*630,1)&" L","")),"")
SPC      IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"安定劑："&ROUND(((5-Concentration)*TANK_CAPACITY_LITERS/1000),1)&" L",""),"")
```

### PPC 2594 N2／化鎳／亞磷酸鈉 (g/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration<18.5,"添加",IF(Concentration>NA,"稀釋","")))
SPC      IF(Concentration="","",IF(Concentration<18.5,"添加",IF(Concentration>20,"稀釋","")))
```

### PPC 2594 N2／化鎳／亞磷酸鈉 (g/L) 調整量

```text
Excel 1  IF(Concentration<18.5,"A劑:"&ROUND(((18.5-Concentration)*1200*1.62/1000),1)&" L",IF(Concentration>NA,"排藥:"&ROUND((Concentration-NA)/Concentration*1200,1)&" L"&"\n補水:"&ROUND((Concentration-NA)/Concentration*1200,1)&" L",""))
SPC      IF(Concentration<18.5,"A劑："&ROUND(((18.5-Concentration)*1200*1.62/1000),1)&" L",IF(Concentration>20,"排藥："&ROUND((Concentration-20)/Concentration*1200,1)&" L"&" 補水："&ROUND((Concentration-20)/Concentration*1200,1)&" L",""))
```

### PPC 2595 N2／化鎳／次磷酸鈉 (g/L) 調整

```text
Excel 1  IF(Concentration="","",IF(Concentration>NA,"更槽",""))
SPC      IF(Concentration="","",IF(Concentration>100,"更槽",""))
```

### PPC 2443 PT1／酸洗／鹽酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>3.9, "排液:"&ROUND(70*(1-3/Concentration)*0.32,1)&" L"&"\n補水:"&ROUND(70*(1-3/Concentration)*0.32,1)&" L", IF(Concentration<2.5, "鹽酸(32%):"&ROUND(((3-Concentration)*70/100)/0.32/1.16, 1)&" L", "")), "")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE, "排液："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.32,1)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.32,1)&" L", IF(Concentration<ADDITION_CONTROL_VALUE, "鹽酸(32%)："&ROUND(((3-Concentration)*TANK_CAPACITY_LITERS/100)/0.32, 2)&" L", "")), "")
```

### PPC 2440 PT1／脫脂／硫酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>3.9, "排液:"&ROUND(70*(1-3/Concentration)*0.5/1.4,1)&" L"&"\n補水:"&ROUND(70*(1-3/Concentration)*0.5/1.4,1)&" L", IF(Concentration<2.5, "硫酸(50%):"&ROUND(((3-Concentration)*70/100)/0.5/1.4, 2)&" L", "")), "")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE, "排液："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.5,1)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.5,1)&" L", IF(Concentration<ADDITION_CONTROL_VALUE, "藥液(50%)："&ROUND(((3-Concentration)*TANK_CAPACITY_LITERS/100)/0.5, 2)&" L", "")), "")
```

### PPC 2462 PT2／鹽酸洗／鹽酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>3.9, "排液:"&ROUND(120*(1-3/Concentration)*0.32,1)&" L"&"\n補水:"&ROUND(120*(1-3/Concentration)*0.32,1)&" L", IF(Concentration<2.5, "鹽酸32%:"&ROUND(((3-Concentration)*120/100)/0.32/1.16, 2)&" L", "")), "")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE, "排液："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.32,1)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.32,1)&" L", IF(Concentration<ADDITION_CONTROL_VALUE, "鹽酸50%："&ROUND(((3-Concentration)*TANK_CAPACITY_LITERS/100)/0.32, 2)&" L", "")), "")
```

### PPC 2459 PT2／脫脂／硫酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>3.9, "排液:"&ROUND(120*(1-3/Concentration)*0.5,1)&" L"&"\n補水:"&ROUND(120*(1-3/Concentration)*0.5,1)&" L", IF(Concentration<2.5, "硫酸50%:"&ROUND(((3-Concentration)*120/100)/0.5/1.4, 2)&" L", "")), "")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE, "排液："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.5,1)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-3/Concentration)*0.5,1)&" L", IF(Concentration<ADDITION_CONTROL_VALUE, "硫酸50%："&ROUND(((3-Concentration)*TANK_CAPACITY_LITERS/100)/0.5, 2)&" L", "")), "")
```

### PPC 2454 QE1／蝕刻／硫酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>8.8, "排液:"&ROUND(650*(1-7/Concentration)*0.5,1)&" L"&"\n補水:"&ROUND(650*(1-7/Concentration)*0.5,1)&" L", IF(Concentration<6.5, "硫酸(50%):"&ROUND(((7-Concentration)*650/100)/0.5/1.4, 2)&" L", "")), "")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE, "排液："&ROUND(TANK_CAPACITY_LITERS*(1-7/Concentration)*0.5,1)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-7/Concentration)*0.5,1)&" L", IF(Concentration<ADDITION_CONTROL_VALUE, "硫酸(50%)："&ROUND(((7-Concentration)*TANK_CAPACITY_LITERS/100)/0.5, 2)&" L", "")), "")
```

### PPC 2457 QE1／酸洗／硫酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>5.9, "排液:"&ROUND(55*(1-5/Concentration)*0.5,1)&" L"&"\n補水:"&ROUND(55*(1-5/Concentration)*0.5,1)&" L", IF(Concentration<4.5, "硫酸(50%):"&ROUND(((5-Concentration)*55/100)/0.5/1.4, 1)&" L", "")), "")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE, "排液："&ROUND(TANK_CAPACITY_LITERS*(1-5/Concentration)*0.5,1)&" L"&"\n補水："&ROUND(TANK_CAPACITY_LITERS*(1-5/Concentration)*0.5,1)&" L", IF(Concentration<ADDITION_CONTROL_VALUE, "硫酸(50%)："&ROUND(((5-Concentration)*TANK_CAPACITY_LITERS/100)/0.5, 2)&" L", "")), "")
```

### PPC 2477 QE2／蝕刻(鎳)／硝酸 (%) 調整量

```text
Excel 1  IFERROR(IF(Concentration>4.8, "排液:"&ROUND(200*(1-4/Concentration)*0.5,1)&" L"&"\n補水:"&ROUND(200*(1-4/Concentration)*0.5,1)&" L", IF(Concentration<3.5, "硝酸:"&ROUND(((4-Concentration)/0.5/1.31), 1)&" L", "")), "")
SPC      IFERROR(IF(Concentration>DILUTION_CONTROL_VALUE, "排液："&ROUND(TANK_CAPACITY_LITERS*(1-4/Concentration)*0.5,1)&" L"&"補水："&ROUND(TANK_CAPACITY_LITERS*(1-4/Concentration)*0.5,1)&" L", IF(Concentration<ADDITION_CONTROL_VALUE, "硝酸："&ROUND(((4-Concentration)*0.5/1.31), 2)&" L", "")), "")
```


## Excel 有公式、網站未設定的候選

候選以 SPC 既有的濃度、槽容及管制值變數表示。需要的設定參數為空白時保留待確認。

### PPC 2447 DV1／顯影／PH

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"更槽",""))
```

### PPC 2449 DV1／添加桶／PH

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"更槽",""))
```


- 調整量（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"通知工程師",""))
```

### PPC 2564 N1／調節／P200 (ml/L)

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",IF(Concentration>DILUTION_CONTROL_VALUE,"稀釋","")))
```


- 調整量（完成試算，待備份驗證）

```text
IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"P200:"&ROUND(((ADDITION_CONTROL_VALUE-Concentration)*TANK_CAPACITY_LITERS/1000),1)&" L",IF(Concentration>DILUTION_CONTROL_VALUE,"排藥:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L"&"
補水:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L","")),"")
```

### PPC 2565 N1／調節／PH

- 調整（待確認稀釋管制值）

```text
IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"更槽",""))
```

### PPC 2568 N1／催化／氯化亞錫 (g/L)

- 調整（待確認稀釋管制值）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",IF(Concentration>DILUTION_CONTROL_VALUE,"稀釋","")))
```


- 調整量（待確認稀釋管制值）

```text
IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"安定劑:"&ROUND(((ADDITION_CONTROL_VALUE-Concentration)*TANK_CAPACITY_LITERS/1000),1)&" L",IF(Concentration>DILUTION_CONTROL_VALUE,"排藥:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L"&"
補水:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L","")),"")
```

### PPC 2570 N1／促進／P500A (ml/L)

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",IF(Concentration>DILUTION_CONTROL_VALUE,"稀釋","")))
```


- 調整量（完成試算，待備份驗證）

```text
IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"P500A:"&ROUND(((ADDITION_CONTROL_VALUE-Concentration)*TANK_CAPACITY_LITERS/1000),1)&" L",IF(Concentration>DILUTION_CONTROL_VALUE,"排藥:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L"&"
補水:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L","")),"")
```

### PPC 2571 N1／促進／氯化亞錫 (g/L)

- 調整（待確認稀釋管制值）

```text
IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"更槽",""))
```

### PPC 2588 N1／化鎳／鎳  (g/L)

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",IF(Concentration>DILUTION_CONTROL_VALUE,"稀釋","")))
```


- 調整量（完成試算，待備份驗證）

```text
IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"A劑:"&ROUND(((ADDITION_CONTROL_VALUE-Concentration)*TANK_CAPACITY_LITERS*9.2/1000),1)&" L",IF(Concentration>DILUTION_CONTROL_VALUE,"排藥:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L"&"
補水:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L","")),"")
```

### PPC 2591 N1／化鎳／PH

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration>ADDITION_CONTROL_VALUE,"","添加"))
```


- 調整量（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"氨水:"&ROUND(((ADDITION_CONTROL_VALUE-Concentration)*TANK_CAPACITY_LITERS*16.5/1000),1)&" L",""))
```

### PPC 2587 N1／表處／氫氧化鉀 (g/L)

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"添加",IF(Concentration>DILUTION_CONTROL_VALUE,"稀釋","")))
```


- 調整量（完成試算，待備份驗證）

```text
IFERROR(IF(Concentration<ADDITION_CONTROL_VALUE,"KOH:"&ROUND(((ADDITION_CONTROL_VALUE-Concentration)*TANK_CAPACITY_LITERS/0.6525/1000),1)&" L",IF(Concentration>DILUTION_CONTROL_VALUE,"排藥:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L"&"
補水:"&ROUND((Concentration-DILUTION_CONTROL_VALUE)/Concentration*TANK_CAPACITY_LITERS,1)&" L","")),"")
```

### PPC 2518 N2／促進／氯化亞錫 (g/L)

- 調整（待確認稀釋管制值）

```text
IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"更槽",""))
```

### PPC 2444 PT1／酸洗／銅離子 (mg/L)

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration>DILUTION_CONTROL_VALUE,"更槽",""))
```

### PPC 2445 PT1／抗氧化／PH

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"更槽",""))
```

### PPC 2458 QE1／抗氧化／PH

- 調整（完成試算，待備份驗證）

```text
IF(Concentration="","",IF(Concentration<ADDITION_CONTROL_VALUE,"更槽",""))
```
