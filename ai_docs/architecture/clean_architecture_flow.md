# Clean Architecture & Layer Separation

## Rules for AI Agents
1. **Presentation Layer (Controllers)**: ONLY handle HTTP requests, routing, and returning DTOs. NEVER write business logic here.
2. **Application Layer (Services)**: Contains all business logic. Injected via Dependency Injection (DI).
3. **Domain Layer**: Contains Entities and Enums. No database dependencies here.
4. **Infrastructure Layer (Data/Repositories)**: DbContext and EF Core configurations.
5. **DTO Pattern**: NEVER return raw Entity models to the frontend. Always map Entities to DTOs.
