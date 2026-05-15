# Development Context

## Current Status
The project has completed Phase 0 through 6 of the MVP roadmap.
- Core MES entities and relationships are implemented.
- Data collection (Manual & CSV) is functional.
- SPC calculation engine (X̄-R with statistical constants) is active.
- Alerting system (OOS/OOC) and Dashboard are live.

## Key Logic: SPC Engine
- **X̄ Chart**: Plots subgroup means. Control limits are calculated using \(\overline{\bar x} \pm A_2 \bar R\).
- **R Chart**: Plots subgroup ranges. Control limits use \(D_3 \bar R\) and \(D_4 \bar R\).
- **Constants**: \(A_2, D_3, D_4\) vary based on `SampleSize` (n=2 to 10).

## Priorities for AI Assistance
1. **Maintenance**: Debugging API/Frontend proxy issues or EF Core migration tasks.
2. **Extensions**: Adding new SPC rules (e.g., Western Electric), new chart types (I-MR), or refining UX.
3. **Refinement**: Improving the formula engine or adding role-based access control.

## Running the Project
- **Backend**: `dotnet run --project .\backend\MesSpc.Api\MesSpc.Api.csproj`
- **Frontend**: `cd .\frontend\mes-spc-web` -> `npm run dev`
- **Frontend URL**: `http://localhost:5173`
