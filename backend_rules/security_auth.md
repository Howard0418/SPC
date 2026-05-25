# Security & Authentication

## Rules for AI Agents
1. **JWT**: Use JWT Bearer authentication. Protect endpoints with `[Authorize]` attributes.
2. **Standard Response**: All endpoints must return a predictable JSON format: `{ success: bool, data: any, message: string }`.
