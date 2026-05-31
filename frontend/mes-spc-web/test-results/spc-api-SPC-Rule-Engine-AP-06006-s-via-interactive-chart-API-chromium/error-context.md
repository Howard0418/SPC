# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: spc-api.spec.ts >> SPC Rule Engine API Validation >> should accurately identify Rule 5 and Rule 6 violations via interactive-chart API
- Location: tests\spc-api.spec.ts:11:3

# Error details

```
Error: apiRequestContext.post: connect ECONNREFUSED ::1:5000
Call log:
  - → POST http://localhost:5000/api/v2/migration/seed-sample-measurements?ppcId=1&count=25
    - user-agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.7778.96 Safari/537.36
    - accept: */*
    - accept-encoding: gzip,deflate,br

```