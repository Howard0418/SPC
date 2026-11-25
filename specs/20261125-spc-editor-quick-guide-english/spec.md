# SPC Editor Quick Guide English Edition

- Work ID：SPC-DOC-EDITOR-QUICK-EN-1125
- Date：2026-11-25
- Primary system：SPC
- Related system：Portal Quality area
- Database impact：None
- Release impact：None; local Word document only
- Rollback：Remove the English deliverable; retain the approved Chinese guide unchanged

## Purpose and scope

- Produce an English edition of the approved SPC Editor Quick Guide.
- Preserve the nine-page operating sequence and eight anonymized production screenshots.
- Explain Portal chemical entry, control charts, trends, weekly and monthly reports, export, OOS and OOC review, and shift handoff in operational English.
- Do not operate production functions or change site data, accounts, permissions, notifications, schedules, formulas, or databases.

## Acceptance criteria

- AC-001 Happy Path：An English-speaking Editor can follow the guide from daily report loading through chart review and report export.
- AC-002 Boundary：The guide states that sensitive saves, notifications, and production corrections require current authorization.
- AC-003 Invalid Input：The guide explains what to verify when a report, chart, trend, report export, or exception result is missing or inconsistent.
- AC-004 Regression：The Chinese source and screenshots remain unchanged; the English DOCX passes full-page render, sensitivity, image, external-link, metadata, and package checks.

## Tasks

- [x] T-001：Inventory the approved Chinese guide, layout, screenshots, and text slots.
- [x] T-002：Create the English edition with the approved visual structure and anonymized screenshots.
- [x] T-003：Render and inspect all pages and complete mechanical verification.
- [x] T-004：Deliver the Word file, synchronize records, commit, and push the current branch.

## Completion

- Completed on 2026-11-25 with nine English pages and eight retained anonymized production screenshots.
- Website build, database verification, and deployment are not applicable to this document-only task.
