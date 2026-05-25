# Testing Strategy

## Rules for AI Agents
1. **Unit Tests**: Test business logic in Services using xUnit/NUnit and Moq.
2. **Integration Tests**: Use `WebApplicationFactory` to test API endpoints with an in-memory or SQLite database.
3. **Test Data**: Auto-clean test data after integration runs.
