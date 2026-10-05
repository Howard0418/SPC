# 計畫與實作
SPC僅CHEM/N1/N2依日期、OPEN/CLOSE/GENERAL、班別、時間、ID排序後送入既有計算；管制界限試算採同順序。I-MR回傳samplingStage。
前端依管制項目線別選單線模式，直接使用API全部點，不經日期/班別Map；保留每筆meta/異常/排除樣式、階段與班別提示。MR軸排除第一筆對齊相鄰差。
只發布SPC測試API及前端，保留設定與備份，不動Portal、資料庫或正式站。回復本批備份即可，無schema變更。
