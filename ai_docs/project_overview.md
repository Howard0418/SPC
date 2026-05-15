# Project Overview: MES + SPC MVP

## Goal
The primary objective of this project is to build a "usable" Quality Measurement and Statistical Process Control (SPC) system with a Minimum Viable Product (MVP) scope.

## Technical Stack
- **Frontend**: Vue 3 + Vite + Tailwind CSS + ECharts (for SPC charts).
- **Backend**: ASP.NET Core Web API.
- **Database**: SQLite (default for development, supports SQL Server).
- **ORM**: Entity Framework Core.

## Key Features
- **Product & Station Management**: CRUD operations for basic manufacturing entities.
- **Inspection Items**: Definition of measurement points with specification (USL/LSL) and control (UCL/LCL) limits.
- **Data Collection**: Manual entry and CSV import for measurement batches and values.
- **SPC Analysis**: Calculation of X̄-R control limits and chart generation.
- **Alerting System**: Automated detection of Out-of-Spec (OOS) and Out-of-Control (OOC) events.
- **Dashboard**: Real-time summary of production quality and recent alerts.

## Infrastructure
- **Proxy**: Vite is configured to proxy API requests to the backend (default: `http://localhost:5243`).
- **Auth**: Optional JWT-based authentication (configurable via `Auth:Enabled`).
- **Seeding**: Automatic database migration and data seeding on backend startup.
