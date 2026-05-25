# Entity Framework Core Rules

## Rules for AI Agents
1. **No SQL Injection**: NEVER use string interpolation in raw SQL queries. Always use parameterized queries or LINQ.
2. **Tracking**: Use `.AsNoTracking()` for read-only queries to improve performance.
3. **Transactions**: Wrap multiple SaveChanges in `using var transaction = await db.Database.BeginTransactionAsync();`.
