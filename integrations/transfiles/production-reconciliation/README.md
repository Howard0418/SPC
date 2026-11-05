# 核准來源優先規則

此模組只供本次2026年9／10月重核，不是已打包GUI或一般全年排序器。[規格](../../../specs/20261105-chemical-source-precedence/spec.md)。

- 9/2 N1（開）依工作表歸N1，N1（收）及其他日期不擴張。
- 已確認9/30、10/6、10/27一般檔→更槽後檔；同鍵後檔取代前檔，保留舊來源，不用mtime選勝者。
- 未知檔案拒絕選順位，日期／實際時間／公式F／正式唯一ID仍須各自驗證。
- `python -m unittest discover -s integrations/transfiles/production-reconciliation -p 'test_*.py'`。

本模組不連DB、不覆寫來源Excel；正式修訂須使用核准固定清單的受控工具，缺時間不新增。
