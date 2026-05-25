# API Lifecycle Flow

## Rules for AI Agents
1. **Request**: Client sends request -> Controller -> Validate DTO.
2. **Processing**: Controller calls `IService.DoWorkAsync()`.
3. **Database**: Service calls `DbContext` (Repository pattern if used, otherwise direct DbContext).
4. **Transaction**: Use `IDbContextTransaction` for multi-table writes.
5. **Response**: Return standard `ApiResponse<T>` object.
