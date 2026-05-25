# SPC (Statistical Process Control) Domain Skill

## Core Understanding for AI Agents
When working on SPC features in this MES project, the AI must deeply understand and apply the following domain concepts:

1. **Control Charts (XBar-R)**:
   - Understand the difference between X-bar (Sample Mean) and R (Sample Range).
   - Know how to calculate Control Limits (UCL, LCL, CL) vs Specification Limits (USL, LSL).

2. **Process Capability (Cp, Cpk, Pp, Ppk)**:
   - **Cp/Cpk**: Short-term capability (using estimated within-subgroup standard deviation). Target usually >= 1.33.
   - **Pp/Ppk**: Long-term performance (using overall sample standard deviation).

3. **Out of Control (OOC) Detection Rules**:
   - **Western Electric Rules**: The standard 4 rules (Zone A, B, C violations).
   - **Nelson Rules**: Extended rules for detecting trends, shifts, and oscillation.

4. **Real-time SPC Monitoring**:
   - The system must evaluate OOC/OOS status *immediately* upon measurement data import.
   - Must support real-time Dashboard rendering (Vue 3 + ECharts).

5. **Dynamic Formula Engine**:
   - The backend must flexibly compute X-bar, MR (Moving Range), or R based on configuration (e.g., `SIGMA_METHOD`, `MR_METHOD`).

6. **Measurement Data Import**:
   - Support CSV and API ingestion.
   - Accurately map data to `PartProcessCharacteristicId`.

7. **Multi-station SPC Analysis**:
   - Ability to query and compare process capability across different routing steps (Stations) for the same product.
