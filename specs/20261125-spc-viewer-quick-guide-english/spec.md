# SPC Viewer Quick Guide English Edition

- Work ID：SPC-DOC-VIEWER-QUICK-EN-1125
- Date：2026-11-25
- Primary system：SPC
- Related system：Portal Quality area
- Database impact：None
- Release impact：None; local Word document only
- Rollback：Remove the English deliverable; retain the approved Chinese guide unchanged

## Purpose and scope

- Produce an English edition of the approved SPC Viewer Quick Guide.
- Preserve the eight-page operating sequence and seven anonymized production screenshots.
- Explain read-only role confirmation, control charts, trends, weekly and monthly reports, quality-data export, OOS and OOC review, and issue reporting in operational English.
- Keep the Viewer boundary explicit: do not save production data, change master data, alter permissions, acknowledge exceptions, or perform administrative actions.
- Do not operate production functions or change site data, accounts, permissions, notifications, schedules, formulas, or databases.

## Acceptance criteria

- AC-001 Happy Path：An English-speaking Viewer can confirm the role, query charts and trends, export authorized reports, review exceptions, and report an issue.
- AC-002 Boundary：The guide clearly distinguishes Viewer actions from Editor, exception-handler, and Administrator actions.
- AC-003 Invalid Input：The guide explains what to verify when a chart, trend, report, export, or exception result is missing or inconsistent.
- AC-004 Regression：The Chinese source and screenshots remain unchanged; the English DOCX passes full-page render, sensitivity, image, external-link, metadata, and package checks.

## Tasks

- [x] T-001：Inventory the approved Chinese guide, layout, screenshots, and text slots.
- [x] T-002：Create the English edition with the approved visual structure and anonymized screenshots.
- [x] T-003：Render and inspect all pages and complete mechanical verification.
- [x] T-004：Deliver the Word file, synchronize records, commit, and push the current branch.

## Completion

- Completed on 2026-11-25 with eight English pages and seven retained anonymized production screenshots.
- Website build, database verification, and deployment are not applicable to this document-only task.
