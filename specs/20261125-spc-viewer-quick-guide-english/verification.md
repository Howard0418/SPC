# SPC Viewer Quick Guide English Edition Verification

- Work ID：SPC-DOC-VIEWER-QUICK-EN-1125
- Verification date：2026-11-25
- Deliverable：`SPC_Viewer_Quick_Guide_EN_V1.0.docx`
- SHA-256：`88866DEE75891640FF5CC325113B34A0B7B020F87EDCC4BD997B16115830BE81`

## Verification results

- PASS：Rendered all 8 pages with the canonical DOCX renderer and visually inspected every page at 100% scale; no clipping, unintended blank pages, overlaps, or broken page flow were found.
- PASS：The document contains 7 inline images and 7 packaged media files, matching the 7 anonymized screenshots in the approved Chinese guide.
- PASS：Required English sections cover role and page confirmation, control charts, trends, weekly and monthly reports, quality-data export, OOS/OOC review, issue reporting, and shift handoff.
- PASS：Viewer read-only boundaries are explicit for data changes, exception acknowledgement, notification settings, and permission grants.
- PASS：DOCX ZIP integrity passed, author metadata is blank, no external relationships were found, and no Chinese instructional text remained in the document text.
- PASS：Sensitive-string scan found no employee name, account, email-domain marker, mail host, or local user path.
- PASS：The approved Chinese source remained unchanged; source SHA-256 is `85194FD7B3B86E2C42B3E5A0B47F10DC95C814A227C9EC02A0122625BE6A2B62`.
- N/A：No website, API, database, account, permission, notification, schedule, formula, or production data was changed; build and deployment are not applicable.

## Rollback

- Remove the English deliverable and continue using the unchanged approved Chinese quick guide.
