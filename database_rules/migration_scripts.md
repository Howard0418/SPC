# Migrations & Documentation

## Rules for AI Agents
1. **EF Core Migrations**: Use `dotnet ef migrations add` for schema changes. Do not manually edit the DB.
2. **Comments**: Use EF Core's `.HasComment()` in `OnModelCreating` to add descriptions to tables and columns.
