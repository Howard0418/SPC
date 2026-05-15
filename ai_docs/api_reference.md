# API Reference Summary

Base Path: `/api/v1`

## Authentication
- `POST /auth/login`: Login to get JWT token.

## Master Data
- `GET/POST/PUT/DELETE /products`: Manage products.
- `GET/POST/PUT/DELETE /stations`: Manage stations.
- `GET/POST/PUT/DELETE /inspection-items`: Manage inspection items (USL/LSL/UCL/LCL).
- `GET/POST/PUT/DELETE /product-station-items`: Link products, stations, and inspection items.

## Measurements & Data Entry
- `GET /measurement-batches`: List recent batches.
- `POST /measurement-batches`: Create batch + multiple values (triggers SPC/Alerts).
- `POST /measurements/import-csv`: Upload CSV file for measurement data.

## SPC & Formulas
- `GET /spc/chart`: Get data for SPC charts (X̄-R, etc.) with calculated control limits.
- `GET /formulas`: List formula definitions (AVG, CPK, etc.).
- `POST /formulas/evaluate`: Calculate formulas based on input values.

## Monitoring
- `GET /alerts`: List OOS/OOC alert events.
- `POST /alerts/{id}/ack`: Acknowledge an alert.
- `GET /dashboard/summary`: Quick summary of counts and alert rates.

## Notes
- **Authentication**: When enabled, use `Authorization: Bearer <token>`.
- **Response Format**: Standard JSON.
- **Errors**: Standard HTTP status codes (400, 401, 404, 500).
