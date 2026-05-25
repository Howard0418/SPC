# Backend Coding Standards (.NET 8)

## Rules for AI Agents
1. **Async/Await**: ALL I/O and DB operations MUST be asynchronous (`ToListAsync()`, `FirstOrDefaultAsync()`). Never use `.Result` or `.Wait()`.
2. **Logging**: Inject `ILogger<T>` into constructors. Log errors and critical paths.
3. **Global Exception Handling**: Do not write `try/catch` in every controller. Rely on the Global Exception Middleware unless specific fallback logic is needed.
4. **XML Comments**: Public APIs and complex methods MUST have `/// <summary>` comments for Swagger generation.
