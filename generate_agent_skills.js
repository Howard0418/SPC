const fs = require('fs');
const path = require('path');

const files = {
  "ai_docs/architecture/clean_architecture_flow.md": `
# Clean Architecture & Layer Separation

## Rules for AI Agents
1. **Presentation Layer (Controllers)**: ONLY handle HTTP requests, routing, and returning DTOs. NEVER write business logic here.
2. **Application Layer (Services)**: Contains all business logic. Injected via Dependency Injection (DI).
3. **Domain Layer**: Contains Entities and Enums. No database dependencies here.
4. **Infrastructure Layer (Data/Repositories)**: DbContext and EF Core configurations.
5. **DTO Pattern**: NEVER return raw Entity models to the frontend. Always map Entities to DTOs.
  `,
  "ai_docs/architecture/api_flow.md": `
# API Lifecycle Flow

## Rules for AI Agents
1. **Request**: Client sends request -> Controller -> Validate DTO.
2. **Processing**: Controller calls \`IService.DoWorkAsync()\`.
3. **Database**: Service calls \`DbContext\` (Repository pattern if used, otherwise direct DbContext).
4. **Transaction**: Use \`IDbContextTransaction\` for multi-table writes.
5. **Response**: Return standard \`ApiResponse<T>\` object.
  `,
  "backend_rules/coding_standards.md": `
# Backend Coding Standards (.NET 8)

## Rules for AI Agents
1. **Async/Await**: ALL I/O and DB operations MUST be asynchronous (\`ToListAsync()\`, \`FirstOrDefaultAsync()\`). Never use \`.Result\` or \`.Wait()\`.
2. **Logging**: Inject \`ILogger<T>\` into constructors. Log errors and critical paths.
3. **Global Exception Handling**: Do not write \`try/catch\` in every controller. Rely on the Global Exception Middleware unless specific fallback logic is needed.
4. **XML Comments**: Public APIs and complex methods MUST have \`/// <summary>\` comments for Swagger generation.
  `,
  "backend_rules/ef_core_rules.md": `
# Entity Framework Core Rules

## Rules for AI Agents
1. **No SQL Injection**: NEVER use string interpolation in raw SQL queries. Always use parameterized queries or LINQ.
2. **Tracking**: Use \`.AsNoTracking()\` for read-only queries to improve performance.
3. **Transactions**: Wrap multiple `SaveChanges` in \`using var transaction = await db.Database.BeginTransactionAsync();\`.
  `,
  "backend_rules/security_auth.md": `
# Security & Authentication

## Rules for AI Agents
1. **JWT**: Use JWT Bearer authentication. Protect endpoints with \`[Authorize]\` attributes.
2. **Standard Response**: All endpoints must return a predictable JSON format: \`{ success: bool, data: any, message: string }\`.
  `,
  "frontend_rules/vue3_composition_rules.md": `
# Vue 3 Composition API Rules

## Rules for AI Agents
1. **<script setup>** is MANDATORY for all Vue components.
2. **Reactivity**: Use \`ref\` for primitives, \`reactive\` for complex objects only when necessary.
3. **Props/Emits**: Use \`defineProps\` and \`defineEmits\` clearly at the top of the script.
4. **Components**: Keep components highly cohesive and loosely coupled.
  `,
  "frontend_rules/api_axios_wrapper.md": `
# Frontend API wrapper

## Rules for AI Agents
1. **Centralized Axios**: All API calls MUST go through the \`api/client.js\` wrapper.
2. **Interceptors**: Handle 401 Unauthorized globally to redirect to login. Handle token expiration automatically if refresh tokens are used.
  `,
  "frontend_rules/ui_ux_standards.md": `
# UI/UX & Tailwind Standards

## Rules for AI Agents
1. **TailwindCSS**: Use utility classes. Do not write custom CSS unless absolutely necessary (like complex animations).
2. **Responsive**: Use \`md:\`, \`lg:\` prefixes. Design mobile-first.
3. **Dark Mode**: Support \`dark:\` variants for all components.
4. **Error Handling**: Show toast notifications or popups for API errors, do not just console.log.
  `,
  "database_rules/sql_server_naming.md": `
# SQL Server Standards

## Rules for AI Agents
1. **Tables**: PascalCase, Plural (e.g., \`Users\`, \`InspectionItems\`).
2. **Columns**: PascalCase (e.g., \`CreatedAt\`, \`IsActive\`).
3. **Primary Keys**: \`Id\` (int identity or uniqueidentifier).
4. **Foreign Keys**: \`[TableName]Id\` (e.g., \`ProductId\`).
  `,
  "database_rules/migration_scripts.md": `
# Migrations & Documentation

## Rules for AI Agents
1. **EF Core Migrations**: Use \`dotnet ef migrations add\` for schema changes. Do not manually edit the DB.
2. **Comments**: Use EF Core's \`.HasComment()\` in \`OnModelCreating\` to add descriptions to tables and columns.
  `,
  "testing_rules/unit_integration_strategy.md": `
# Testing Strategy

## Rules for AI Agents
1. **Unit Tests**: Test business logic in Services using xUnit/NUnit and Moq.
2. **Integration Tests**: Use \`WebApplicationFactory\` to test API endpoints with an in-memory or SQLite database.
3. **Test Data**: Auto-clean test data after integration runs.
  `,
  "deployment_rules/docker_compose.yml": `
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "\${MSSQL_SA_PASSWORD}"
    ports:
      - "1433:1433"
  
  backend:
    build: 
      context: ./backend
      dockerfile: Dockerfile
    ports:
      - "5243:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=MES;User Id=sa;Password=\${MSSQL_SA_PASSWORD};TrustServerCertificate=True;

  frontend:
    build:
      context: ./frontend/mes-spc-web
      dockerfile: Dockerfile
    ports:
      - "8080:80"
`,
  "deployment_rules/ci_cd_template.yml": `
# Example GitHub Actions CI Pipeline
name: MES CI

on:
  push:
    branches: [ "main" ]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Restore dependencies
      run: dotnet restore ./backend/MesSpc.Api/MesSpc.Api.csproj
    - name: Build
      run: dotnet build ./backend/MesSpc.Api/MesSpc.Api.csproj --no-restore
    - name: Test
      run: dotnet test ./backend/MesSpc.Api/MesSpc.Api.csproj --no-build --verbosity normal
`,
  "agent_skills/documentation_generator.md": `
# Documentation Generation Skill

## Prompt for AI
"When you complete a significant feature or architecture change, you MUST automatically update the markdown files in \`/ai_docs\`. Do not ask for permission, just update the documentation to reflect the current state of the codebase."
  `,
  "prompts/system_prompt_template.txt": `
You are an expert AI agent working on the MES+SPC MVP project.
Stack: Vue 3 (Composition API, Tailwind, ECharts) + .NET 8 Web API + SQL Server/EF Core.
Always follow the rules defined in the /ai_docs, /backend_rules, /frontend_rules, and /database_rules folders.
Always use async/await in C#. Always use script setup in Vue.
Before modifying architecture, check /ai_docs/architecture/clean_architecture_flow.md.
  `
};

for (const [filepath, content] of Object.entries(files)) {
  const fullPath = path.join(process.cwd(), filepath);
  fs.mkdirSync(path.dirname(fullPath), { recursive: true });
  fs.writeFileSync(fullPath, content.trim() + '\\n');
  console.log('Created: ' + filepath);
}
