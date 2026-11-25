# SPC English User Manual

- Work ID：SPC-DOC-GUIDE-EN-1125
- Date：2026-11-25
- Primary system：SPC
- Related system：Portal Quality area
- Database impact：None
- Release impact：None; local Word document only
- Rollback：Remove the English deliverable; the approved Chinese manual and all quick guides remain unchanged

## Purpose and scope

- Produce an English edition of the approved SPC full-function user manual for Editor, Viewer, and controlled administrative tasks.
- Reuse the 32 anonymized screenshots captured from the production environment and explain each screen in English.
- Keep screenshots faithful to the live Chinese interface. English steps identify the relevant function and, where helpful, retain the visible Chinese label in parentheses.
- Do not access or change production data, accounts, permissions, formulas, notifications, schedules, or databases.

## Requirements

- R-001：The manual shall explain role boundaries, Portal chemical entry, control item setup, control charts, trend charts, weekly and monthly reports, exports, alerts, master data, notifications, scheduling, permissions, and audit.
- R-002：All instructions, figure captions, tables, warnings, validation steps, and handoff guidance shall be written in clear operational English.
- R-003：Production screenshots shall remain anonymized and shall not expose personal names, accounts, email addresses, hosts, credentials, or local user paths.
- R-004：The document shall state that audit permissions require explicit approval by another security administrator and that users must never grant audit permissions to their own account.

## Acceptance criteria

- AC-001 Happy Path：An English-speaking Editor or Viewer can follow the documented workflow from sign-in through chart review and report export.
- AC-002 Boundary：The manual distinguishes Viewer, Editor, Administrator, security approval, and page-permission responsibilities.
- AC-003 Invalid Input：The manual explains when to stop and escalate for permission mismatch, validation failure, missing notification, schedule error, or data inconsistency.
- AC-004 Regression：The approved Chinese manual and screenshots remain unchanged; the English DOCX passes full-page render, content, sensitivity, image, external-link, and package checks.

## Tasks

- [x] T-001：Inventory the approved Chinese manual, layout, screenshots, and editable text slots.
- [x] T-002：Create the English edition while preserving the approved visual structure and anonymized screenshots.
- [x] T-003：Render and inspect every page, then complete content, sensitivity, link, image, and DOCX package checks.
- [x] T-004：Deliver the Word file, synchronize requirements and change records, commit, and push the current branch.

## Completion result

- Delivered a 26-page English Word manual with all 32 approved anonymized production screenshots and English operational explanations.
- Rendered and visually inspected every page. DOCX package, image count, required content, sensitivity, metadata, and external-link checks passed.
- The approved Chinese source remained unchanged. No site, account, permission, notification, schedule, formula, or database change was performed.
